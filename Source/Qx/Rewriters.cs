using ExpressionToString;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Qx
{
    /// <summary>
    /// A collection of functions for rewrite expression trees.
    /// </summary>
    public static class Rewriters
    {
        public delegate InvocationExpression InvocationFactory(IEnumerable<Expression> args);

        /// <summary>
        /// Rewrites an expression tree, binding unbound parameters.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="bindings"></param>
        /// <returns></returns>
        public static Expression BindingRewriter(Expression expression, IReadOnlyDictionary<ParameterExpression, InvocationFactory> bindings) =>
            new Impl(bindings).Visit(expression);

        private class Impl : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<ParameterExpression, InvocationFactory> _bindings;

            public Impl(IReadOnlyDictionary<ParameterExpression, InvocationFactory> bindings) =>
                _bindings = bindings;

            protected override Expression VisitInvocation(InvocationExpression node)
            {
                if (!(node.Expression is ParameterExpression parameter))
                    return base.VisitInvocation(node);

                if (!(_bindings.TryGetValue(parameter, out var createInvocation)))
                    throw new InvalidOperationException($"No binding provided for parameter '{parameter}'");

                return createInvocation(node.Arguments);
            }
        }
    }
}
