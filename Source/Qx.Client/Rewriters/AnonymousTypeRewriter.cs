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
                    Func<object, ConstantExpression> constantExpressionFactory)
                {
                    Type = type;
                    GetNewExpression = newExpressionFactory;
                    GetConstantExpression = constantExpressionFactory;
                }

                public Type Type { get; }
                public Func<ReadOnlyCollection<Expression>, NewExpression> GetNewExpression { get; }
                public Func<object, ConstantExpression> GetConstantExpression { get; }
            }

            private readonly Dictionary<Type, TupleInfo> _tuples = new Dictionary<Type, TupleInfo>();

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (!TryUpdateAnonymousType(node.Type, out var tupleInfo)) return base.VisitConstant(node);

                return tupleInfo.GetConstantExpression(node.Value);
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                if (!TryUpdateAnonymousType(node.ReturnType, out var tupleInfo)) return base.VisitLambda(node);

                var arguments = node.Type.GetGenericArguments();
                arguments[Array.LastIndexOf(arguments, node.ReturnType)] = tupleInfo.Type;
                var type = node.Type.GetGenericTypeDefinition().MakeGenericType(arguments);

                return Expression.Lambda(type, Visit(node.Body), node.Name, node.TailCall, VisitAndConvert(node.Parameters, nameof(VisitLambda)));
            }

            protected override Expression VisitNew(NewExpression node)
            {
                if (!TryUpdateAnonymousType(node.Type, out var tupleInfo)) return base.VisitNew(node);

                var arguments = Visit(node.Arguments);

                return tupleInfo.GetNewExpression(arguments); ;
            }

            private bool TryUpdateAnonymousType(Type type, [NotNullWhen(true)] out TupleInfo? tupleInfo )
            {
                if (_tuples.TryGetValue(type, out var existingTupleInfo))
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

                var constructorParametersTypes = type.GetConstructors().Single().GetParameterTypes();

                if (constructorParametersTypes.Length >= TupleTypes.Length)
                    throw new NotImplementedException($"Anonymous types with  >{TupleTypes.Length} properties are not supported, yet.");

                if (constructorParametersTypes.Length == 0)
                    throw new NotImplementedException($"Anonymous types with 0 properties are not supported, yet.");

                if (constructorParametersTypes.Any(t => t.IsAnonymousType()))
                    throw new NotImplementedException($"Anonymous types with nested anonymous types are not supported, yet.");

                var tupleType = TupleTypes[constructorParametersTypes.Length].MakeGenericType(constructorParametersTypes);
                var tupleConstructorInfo = tupleType.GetConstructor(constructorParametersTypes);
                var tupleConversionExpression = CreateTupleConversionExpression(type, tupleConstructorInfo);
                var tupleConversionDelegate = default(Delegate); // For caching, closed over in CreateConstant so it is only compiled once and only if needed

                NewExpression CreateNew(ReadOnlyCollection<Expression> arguments) => Expression.New(tupleConstructorInfo, arguments);
                ConstantExpression CreateConstant(object value) => Expression.Constant(
                    (tupleConversionDelegate ??= tupleConversionExpression.Compile()).DynamicInvoke(value), tupleType);

                var tupleInfo_ = new TupleInfo(tupleType, CreateNew, CreateConstant);
                
                _tuples[type] = tupleInfo_;
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
        }
    }
}
