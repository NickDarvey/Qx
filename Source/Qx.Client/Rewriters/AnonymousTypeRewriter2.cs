using Qx.Internals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Qx.Client.Rewriters
{
    public static class AnonymousTypeRewriter2
    {
        public static Expression Rewrite(Expression expression) =>
            new TuplifyingExpressionVisitor().Visit(expression);

        private class EmptyTuple { }

        private static readonly Type[] TupleTypes = new[]
        {
            typeof(EmptyTuple), // So we can index by the length of type parameters
            typeof(Tuple<>),    // e.g. this is 1 type parameter and it is at index 1
            typeof(Tuple<,>),
            typeof(Tuple<,,>),
            typeof(Tuple<,,,>),
            typeof(Tuple<,,,,>),
            typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>),
        };

        private class TupleInfo
        {
            public TupleInfo(
                Type type,
                LambdaExpression conversionDelegate,
                Func<object, ConstantExpression> constantExpressionFactory,
                Func<ReadOnlyCollection<Expression>, NewExpression> newExpressionFactory,
                Func<Expression, MemberInfo, MemberExpression> memberExpressionFactory)
            {
                Type = type;
                ConversionDelegate = conversionDelegate;
                GetConstantExpression = constantExpressionFactory;
                GetNewExpression = newExpressionFactory;
                GetMemberExpression = memberExpressionFactory;
            }

            public Type Type { get; }
            public LambdaExpression ConversionDelegate { get; }
            public Func<object, ConstantExpression> GetConstantExpression { get; }
            public Func<ReadOnlyCollection<Expression>, NewExpression> GetNewExpression { get; }
            public Func<Expression, MemberInfo, MemberExpression> GetMemberExpression { get; }

        }

        /// <summary>
        /// Visits a type and collects <see cref="TupleInfo"/> for any nested anonymous types.
        /// </summary>
        private class TuplifyingTypeVisitor : TypeVisitor
        {
            public TuplifyingTypeVisitor() => Tuples = new Dictionary<Type, TupleInfo>();

            /// <param name="tuples"><see cref="Tuples"/></param>
            public TuplifyingTypeVisitor(Dictionary<Type, TupleInfo> tuples) => Tuples = tuples;

            /// <summary>
            /// Maps an anonymous type to tuple info.
            /// </summary>
            public Dictionary<Type, TupleInfo> Tuples { get; }

            public override Type Visit(Type type)
            {
                if (Tuples.TryGetValue(type, out var tupleInfo)) return tupleInfo.Type;
                if (type.IsAnonymousType() == false) return base.Visit(type);

                var existingConstantParameter = Expression.Parameter(type);

                var anonymousTypeProperties = type.GetProperties();
                var tuplePropertyTypes = new Type[anonymousTypeProperties.Length];
                var anonymousTypePropertyAccessors = new Expression[anonymousTypeProperties.Length];
                var tuplePropertyAccessors = new Dictionary<MemberInfo, PropertyInfo>(anonymousTypeProperties.Length);
                for (var i = 0; i < anonymousTypeProperties.Length; i++)
                {
                    var anonymousTypeProperty = anonymousTypeProperties[i];
                    tuplePropertyTypes[i] = Visit(anonymousTypeProperty.PropertyType);

                    var propertyAccessor = Expression.MakeMemberAccess(existingConstantParameter, anonymousTypeProperty);
                    anonymousTypePropertyAccessors[i] = Tuples.TryGetValue(anonymousTypeProperty.PropertyType, out var propertyTupleInfo) // We just visited, can we remove this look-up?
                        ? Expression.Invoke(propertyTupleInfo.ConversionDelegate, propertyAccessor)
                        : (Expression)propertyAccessor;
                }


                var tupleType = TupleTypes[tuplePropertyTypes.Length].MakeGenericType(tuplePropertyTypes);
                var tupleProperties = tupleType.GetProperties();
                for (int i = 0; i < tupleProperties.Length; i++) tuplePropertyAccessors[anonymousTypeProperties[i]] = tupleProperties[i];
                var tupleConstructorInfo = tupleType.GetConstructor(tuplePropertyTypes);
                var tupleConversionDelegate = Expression.Lambda(Expression.New(tupleConstructorInfo, anonymousTypePropertyAccessors), existingConstantParameter);
                var tupleConversionDelegateCache = default(Delegate); // For caching, closed over in CreateConstant so it is only compiled once and only if needed

                ConstantExpression CreateConstant(object value) =>
                    Expression.Constant((tupleConversionDelegateCache ??= tupleConversionDelegate.Compile()).DynamicInvoke(value));

                NewExpression CreateNew(ReadOnlyCollection<Expression> arguments) =>
                    Expression.New(tupleConstructorInfo, arguments);

                MemberExpression CreateMember(Expression expression, MemberInfo member) =>
                     Expression.MakeMemberAccess(expression, tuplePropertyAccessors[member]);

                Tuples[type] = new TupleInfo(tupleType, tupleConversionDelegate, CreateConstant, CreateNew, CreateMember);

                return tupleType;
            }
        }

        private class TuplifyingExpressionVisitor : ExpressionVisitor
        {
            /// <summary>
            /// Maps a type containing an anonymous type to a type containing a tuple.
            /// </summary>
            /// <example>
            /// IEnumerable{AnonymousType1} -> IEnumerable{Tuple1}
            /// AnonymousType2 -> Tuple2
            /// </example>
            private readonly Dictionary<Type, Type?> _types = new Dictionary<Type, Type?>();

            /// <summary>
            /// Maps an existing parameter to a replacement parameter.
            /// </summary>
            /// <remarks>
            /// ParameterExpressions need to be reference-equal so we need to re-use the ParameterExpressions we replace.
            /// </remarks>
            private readonly Dictionary<ParameterExpression, ParameterExpression> _parameters = new Dictionary<ParameterExpression, ParameterExpression>();

            private readonly TuplifyingTypeVisitor _visitor = new TuplifyingTypeVisitor();

            protected override Expression VisitConstant(ConstantExpression node) =>
                TryGetTupleInfo(node.Type, out var tupleInfo)
                    ? tupleInfo.GetConstantExpression(node.Value)
                    : base.VisitConstant(node);

            protected override Expression VisitLambda<T>(Expression<T> node) =>
                TryGetTupleType(node.Type, out var tupleType)
                    ? Expression.Lambda(tupleType, Visit(node.Body), node.Name, node.TailCall, VisitAndConvert(node.Parameters, nameof(VisitLambda)))
                    : base.VisitLambda(node);

            protected override Expression VisitMember(MemberExpression node) =>
                TryGetTupleInfo(node.Expression.Type, out var tupleInfo)
                    ? tupleInfo.GetMemberExpression(Visit(node.Expression), node.Member)
                    : base.VisitMember(node);

            protected override Expression VisitMethodCall(MethodCallExpression node) =>
                TryGetTupleMethod(node.Method, out var tupleMethod)
                    ? Expression.Call(Visit(node.Object), tupleMethod, Visit(node.Arguments))
                    : base.VisitMethodCall(node);

            protected override Expression VisitNew(NewExpression node) =>
                TryGetTupleInfo(node.Type, out var tupleInfo)
                    ? tupleInfo.GetNewExpression(Visit(node.Arguments))
                    : base.VisitNew(node);

            protected override Expression VisitParameter(ParameterExpression node) =>
                _parameters.TryGetValue(node, out var existingParameter) ? existingParameter
                : TryGetTupleType(node.Type, out var replacedType) ? _parameters[node] = Expression.Parameter(replacedType, node.Name)
                : base.VisitParameter(node);

            private bool TryGetTupleType(Type type, [NotNullWhen(true)] out Type? tupleType)
            {
                if (_types.TryGetValue(type, out tupleType)) return tupleType != default;

                var visited = _visitor.Visit(type);

                if (type == visited)
                {
                    _types[type] = default;
                    return false;
                }

                else
                {
                    _types[type] = visited;
                    tupleType = visited;
                    return true;
                }
            }

            private bool TryGetTupleInfo(Type type, [NotNullWhen(true)] out TupleInfo? tupleInfo)
            {
                if (TryGetTupleType(type, out _) && _visitor.Tuples.TryGetValue(type, out tupleInfo)) return true;
                else
                {
                    tupleInfo = default;
                    return false;
                }
            }

            private bool TryGetTupleMethod(MethodInfo method, [NotNullWhen(true)] out MethodInfo? tupleMethod)
            {
                if (method.IsGenericMethod)
                {
                    var arguments = method.GetGenericArguments();
                    var visitedArguments = _visitor.Visit(arguments);
                    if (visitedArguments != arguments)
                    {
                        tupleMethod = method.GetGenericMethodDefinition().MakeGenericMethod(visitedArguments);
                        return true;
                    }
                }

                tupleMethod = default;
                return false;
            }
        }
    }
}
