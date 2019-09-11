using Qx.Internals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Qx.Client.Rewriters
{
    internal static class AnonymousTypeRewriter
    {
        public static Expression Rewrite(Expression expression) => new Impl().Visit(expression);

        private class Impl : ExpressionVisitor
        {
            private delegate bool GenericArgumentUpdater<T>(Dictionary<Type, TupleInfo> tuples, T type, [NotNullWhen(true)] out T? replacedType) where T : class;
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
                    Func<ReadOnlyCollection<Expression>, NewExpression> newExpressionFactory,
                    Func<object, ConstantExpression> constantExpressionFactory,
                    Func<Expression, MemberInfo, MemberExpression> memberExpressionFactory)
                {
                    Type = type;
                    GetNewExpression = newExpressionFactory;
                    GetConstantExpression = constantExpressionFactory;
                    GetMemberExpression = memberExpressionFactory;
                }

                public Type Type { get; }
                public Func<ReadOnlyCollection<Expression>, NewExpression> GetNewExpression { get; }
                public Func<object, ConstantExpression> GetConstantExpression { get; }
                public Func<Expression, MemberInfo, MemberExpression> GetMemberExpression { get; }
            }

            //private class AnonymousTypeScanner : TypeVisitor
            //{
            //    public override Type Visit(Type type)
            //    {
            //        if (type.IsGenericTypeDefinition == false && type.IsAnonymousType()) AnonymousTypes.Add(type);
            //        return base.Visit(type);
            //    }

            //    public List<Type> AnonymousTypes { get; } = new List<Type>();
            //}

            private static readonly GenericArgumentUpdater<Type> TryUpdateTypeArguments =
                CreateGenericArgumentUpdater<Type>(t => t.IsGenericType, t => t.GetGenericArguments(), (t, args) => t.GetGenericTypeDefinition().MakeGenericType(args));

            private static readonly GenericArgumentUpdater<MethodInfo> TryUpdateMethodArguments =
                CreateGenericArgumentUpdater<MethodInfo>(m => m.IsGenericMethod, m => m.GetGenericArguments(), (m, args) => m.GetGenericMethodDefinition().MakeGenericMethod(args));

            /// <summary>
            /// Anonymous type to tuple mapping.
            /// </summary>
            private Dictionary<Type, TupleInfo> Tuples { get; } = new Dictionary<Type, TupleInfo>();

            /// <summary>
            /// Parameter to replaced parameter mapping.
            /// </summary>
            /// <remarks>
            /// ParameterExpressions need to be reference-equal so we need to re-use the ParameterExpressions we replace.
            /// </remarks>
            private Dictionary<ParameterExpression, ParameterExpression> ReplacedParameters { get; } = new Dictionary<ParameterExpression, ParameterExpression>();

            protected override Expression VisitConstant(ConstantExpression node) =>
                TryUpdateAnonymousType(Tuples, node.Type, out var tupleInfo)
                    ? tupleInfo.GetConstantExpression(node.Value)
                    : base.VisitConstant(node);

            protected override Expression VisitLambda<T>(Expression<T> node) =>
                TryUpdateTypeArguments(Tuples, node.Type, out var replacedType)
                    ? Expression.Lambda(replacedType, Visit(node.Body), node.Name, node.TailCall, VisitAndConvert(node.Parameters, nameof(VisitLambda)))
                    : base.VisitLambda(node);

            protected override Expression VisitMember(MemberExpression node) =>
                TryUpdateAnonymousType(Tuples, node.Expression.Type, out var tupleInfo)
                    ? tupleInfo.GetMemberExpression(Visit(node.Expression), node.Member)
                    : base.VisitMember(node);

            protected override Expression VisitMethodCall(MethodCallExpression node) =>
                TryUpdateMethodArguments(Tuples, node.Method, out var replacedMethod)
                    ? Expression.Call(Visit(node.Object), replacedMethod, Visit(node.Arguments))
                    : base.VisitMethodCall(node);

            protected override Expression VisitNew(NewExpression node) =>
                TryUpdateAnonymousType(Tuples, node.Type, out var tupleInfo)
                    ? tupleInfo.GetNewExpression(Visit(node.Arguments))
                    : base.VisitNew(node);

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (ReplacedParameters.TryGetValue(node, out var existingParameter)) return existingParameter;
                if (!TryUpdateAnonymousType(Tuples, node.Type, out var tupleInfo)) return base.VisitParameter(node);

                var parameter = Expression.Parameter(tupleInfo.Type, node.Name);
                ReplacedParameters[node] = parameter;
                return parameter;
            }

            private static GenericArgumentUpdater<T> CreateGenericArgumentUpdater<T>(Func<T, bool> isGeneric, Func<T, Type[]> getGenericArguments, Func<T, Type[], T> close) where T : class
            {
                bool Updater(Dictionary<Type, TupleInfo> tuples, T value, out T? replacedValue)
                {
                    if (!isGeneric(value))
                    {
                        replacedValue = default;
                        return false;
                    }

                    var arguments = getGenericArguments(value);
                    var updated = false;

                    for (int i = 0; i < arguments.Length; i++)
                    {
                        if (!TryUpdateAnonymousType(tuples, arguments[i], out var tupleInfo)) continue;
                        arguments[i] = tupleInfo.Type;
                        updated = true;
                    }

                    if (!updated)
                    {
                        replacedValue = default;
                        return false;
                    }

                    replacedValue = close(value, arguments);
                    return true;
                }

                return Updater;
            }

            // TODO
            // Update tuple dict to have an optional tuple info so we can mark when we already know the type is not to be replaced
            // create a type visitor to walk the type tree (e.g. dive into closed generic args) -- stop walking recursive types?
            // 
            private static bool TryUpdateAnonymousType(Dictionary<Type, TupleInfo> tuples, Type type, [NotNullWhen(true)] out TupleInfo? tupleInfo)
            {
                if (tuples.TryGetValue(type, out var existingTupleInfo))
                {
                    tupleInfo = existingTupleInfo;
                    return true;
                }

                if (type == null ||
                    type.IsGenericTypeDefinition ||
                    type.IsAnonymousType() == false)
                {
                    tupleInfo = default;
                    return false;
                }

                var constructorParameters = type.GetConstructors().Single().GetParameters();
                var propertyNames = new string[constructorParameters.Length];
                var propertyTypes = new Type[constructorParameters.Length];
                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    propertyNames[i] = constructorParameters[i].Name;
                    propertyTypes[i] = constructorParameters[i].ParameterType;
                }

                if (propertyTypes.Length >= TupleTypes.Length)
                    throw new NotImplementedException($"Anonymous types with  >{TupleTypes.Length} properties are not supported, yet.");

                if (propertyTypes.Length == 0)
                    throw new NotImplementedException($"Anonymous types with 0 properties are not supported, yet.");

                if (propertyTypes.Any(t => t.IsAnonymousType()))
                    throw new NotImplementedException(
                        "Anonymous types with nested anonymous types are not supported, yet." + Environment.NewLine +
                        "Offending type: " + type.Name + Environment.NewLine +
                        "with properties: " + Environment.NewLine +
                        string.Join(Environment.NewLine, propertyTypes.Select(t => t.Name)));

                var tupleType = TupleTypes[propertyTypes.Length].MakeGenericType(propertyTypes);
                var tupleConstructorInfo = tupleType.GetConstructor(propertyTypes);
                var tupleMemberAccessors = CreateTupleMemberAccessors(type, tupleType); // TODO: Some smarts so that we only fetch the properties once
                var tupleConversionDelegateCache = default(Delegate); // For caching, closed over in CreateConstant so it is only compiled once and only if needed

                NewExpression CreateNew(ReadOnlyCollection<Expression> arguments) => Expression.New(tupleConstructorInfo, arguments);
                ConstantExpression CreateConstant(object value) => Expression.Constant(
                    (tupleConversionDelegateCache ??= CreateTupleConversionExpression(type, tupleConstructorInfo).Compile()).DynamicInvoke(value), tupleType);
                MemberExpression CreateMember(Expression expression, MemberInfo member) => Expression.MakeMemberAccess(expression, tupleMemberAccessors[member]);

                var tupleInfo_ = new TupleInfo(tupleType, CreateNew, CreateConstant, CreateMember);

                tuples[type] = tupleInfo_;
                tupleInfo = tupleInfo_;
                return true;
            }

            private static LambdaExpression CreateTupleConversionExpression(Type anonymousType, ConstructorInfo tupleConstructor)
            {
                var existingConstantParameter = Expression.Parameter(anonymousType);
                var properties = anonymousType.GetProperties();
                var memberAccessors = new MemberExpression[properties.Length];
                for (var i = 0; i < properties.Length; i++) memberAccessors[i] = Expression.MakeMemberAccess(existingConstantParameter, properties[i]);
                return Expression.Lambda(Expression.New(tupleConstructor, memberAccessors), existingConstantParameter);
            }

            private static Dictionary<MemberInfo, PropertyInfo> CreateTupleMemberAccessors(Type anonymousType, Type tupleType)
            {
                var result = new Dictionary<MemberInfo, PropertyInfo>();
                var anonymousTypeProperties = anonymousType.GetProperties();
                var tupleTypeProperties = tupleType.GetProperties();
                for (int i = 0; i < anonymousTypeProperties.Length; i++) result.Add(anonymousTypeProperties[i], tupleTypeProperties[i]);
                return result;
            }
        }
    }
}
