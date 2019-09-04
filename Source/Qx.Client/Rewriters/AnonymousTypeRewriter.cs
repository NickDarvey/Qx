using Qx.Internals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
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
                public TupleInfo(Type type, Func<ReadOnlyCollection<Expression>, NewExpression> newExpressionFactory)
                {
                    Type = type;
                    GetNewExpression = newExpressionFactory;
                }

                public Type Type { get; }
                public Func<ReadOnlyCollection<Expression>, NewExpression> GetNewExpression { get; }
            }

            private readonly Dictionary<Type, TupleInfo> _tuples = new Dictionary<Type, TupleInfo>();

            protected override Expression VisitNew(NewExpression node)
            {
                if (!TryUpdateAnonymousType(node.Type, out var tupleInfo)) return base.VisitNew(node);

                var arguments = Visit(node.Arguments);
                var newTuple = tupleInfo.GetNewExpression(arguments);

                return newTuple;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                if (!TryUpdateAnonymousType(node.ReturnType, out var tupleInfo)) return base.VisitLambda(node);

                var arguments = node.Type.GetGenericArguments();
                arguments[Array.LastIndexOf(arguments, node.ReturnType)] = tupleInfo.Type;
                var type = node.Type.GetGenericTypeDefinition().MakeGenericType(arguments);

                return Expression.Lambda(type, Visit(node.Body), node.Name, node.TailCall, VisitAndConvert(node.Parameters, nameof(VisitLambda)));
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

                var tupleType = TupleTypes[constructorParametersTypes.Length].MakeGenericType(constructorParametersTypes);
                var tupleConstructor = tupleType.GetConstructor(constructorParametersTypes);

                NewExpression CreateNew(ReadOnlyCollection<Expression> arguments) => Expression.New(tupleConstructor, arguments);

                var tupleInfo_ = new TupleInfo(tupleType, CreateNew);
                
                _tuples[type] = tupleInfo_;
                tupleInfo = tupleInfo_;
                return true;
            }

            
        }
    }
}
