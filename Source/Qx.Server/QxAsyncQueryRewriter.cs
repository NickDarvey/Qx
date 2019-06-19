using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Qx
{
    /// <summary>
    /// Rewrites a Qx query (an expression tree with unbound AsyncQueryable<> parameters) binding it to the provided factories.
    /// </summary>
    public class AsyncQueryableRewriter : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<string, Expression> _queryableFactories;

        public AsyncQueryableRewriter(IReadOnlyDictionary<string, Expression> queryableFactories)
        {
            _queryableFactories = queryableFactories;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // TODO: Check if parameter is unbound?
            // TODO: Check if types in parameter match the types in our factory

            if (TryGetDelegateType(node.Type, out var type) && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>))
            {
                if (_queryableFactories.TryGetValue(node.Name, out var factory)) return factory;
                else throw new InvalidOperationException($"No known queryable named '{node.Name}'");
            }

            else return node;
        }

        private static bool TryGetDelegateType(Type type, out Type returnType)
        {
            if (type != null && typeof(Delegate).IsAssignableFrom(type))
            {
                var method = type.GetMethod("Invoke");
                returnType = method.ReturnType;
                return true;
            }
            returnType = null;
            return false;
        }
    }
}
