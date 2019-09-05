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

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (!TryUpdateAnonymousType(node.Type, out var tupleInfo)) return base.VisitConstant(node);

                return tupleInfo.GetConstantExpression(node.Value);
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                if(!node.Type.IsGenericType) return base.VisitLambda(node);
                var arguments = node.Type.GetGenericArguments();
                var updated = false;
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (!TryUpdateAnonymousType(arguments[i], out var tupleInfo)) continue;
                    arguments[i] = tupleInfo.Type;
                    updated = true;
                }

                if (!updated) return base.VisitLambda(node);

                var type = node.Type.GetGenericTypeDefinition().MakeGenericType(arguments);

                return Expression.Lambda(type, Visit(node.Body), node.Name, node.TailCall, VisitAndConvert(node.Parameters, nameof(VisitLambda)));
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (!TryUpdateAnonymousType(node.Expression.Type, out var tupleInfo)) return base.VisitMember(node);

                var expression = Visit(node.Expression);

                return tupleInfo.GetMemberExpression(expression, node.Member);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (!node.Method.IsGenericMethod) return base.VisitMethodCall(node);
                var arguments = node.Method.GetGenericArguments();
                var updated = false;
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (!TryUpdateAnonymousType(arguments[i], out var tupleInfo)) continue;
                    arguments[i] = tupleInfo.Type;
                    updated = true;
                }

                if (!updated) return base.VisitMethodCall(node);

                var method = node.Method.GetGenericMethodDefinition().MakeGenericMethod(arguments);

                return Expression.Call(Visit(node.Object), method, Visit(node.Arguments));
            }

            protected override Expression VisitNew(NewExpression node)
            {
                if (!TryUpdateAnonymousType(node.Type, out var tupleInfo)) return base.VisitNew(node);

                var arguments = Visit(node.Arguments);

                return tupleInfo.GetNewExpression(arguments); ;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (ReplacedParameters.TryGetValue(node, out var existingParameter)) return existingParameter;
                if (!TryUpdateAnonymousType(node.Type, out var tupleInfo)) return base.VisitParameter(node);

                var parameter = Expression.Parameter(tupleInfo.Type, node.Name);
                ReplacedParameters[node] = parameter;
                return parameter;
            }

            private bool TryUpdateAnonymousType(Type type, [NotNullWhen(true)] out TupleInfo? tupleInfo)
            {
                if (Tuples.TryGetValue(type, out var existingTupleInfo))
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
                    throw new NotImplementedException($"Anonymous types with nested anonymous types are not supported, yet.");

                var tupleType = TupleTypes[propertyTypes.Length].MakeGenericType(propertyTypes);
                var tupleConstructorInfo = tupleType.GetConstructor(propertyTypes);
                var tupleMemberAccessors = CreateTupleMemberAccessors(type, tupleType); // TODO: Some smarts so that we only fetch the properties once
                var tupleConversionDelegateCache = default(Delegate); // For caching, closed over in CreateConstant so it is only compiled once and only if needed

                NewExpression CreateNew(ReadOnlyCollection<Expression> arguments) => Expression.New(tupleConstructorInfo, arguments);
                ConstantExpression CreateConstant(object value) => Expression.Constant(
                    (tupleConversionDelegateCache ??= CreateTupleConversionExpression(type, tupleConstructorInfo).Compile()).DynamicInvoke(value), tupleType);
                MemberExpression CreateMember(Expression expression, MemberInfo member) => Expression.MakeMemberAccess(expression, tupleMemberAccessors[member]);

                var tupleInfo_ = new TupleInfo(tupleType, CreateNew, CreateConstant, CreateMember);

                Tuples[type] = tupleInfo_;
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
