using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Qx
{
    /// <summary>
    /// A collection of functions that walk expression trees to collect information.
    /// </summary>
    public static class Scanners
    {
        // TODO: Return set, no duplicates?
        public static IEnumerable<ParameterExpression> FindUnboundParameters(Expression expression)
        {
            var visitor = new Impl();
            _ = visitor.Visit(expression);
            return visitor.Unbound;
        }

        /// <summary>
        /// Collects references to parameter expressions.
        /// </summary>
        // TOTHINK: This is really finding free variables like https://en.wikipedia.org/wiki/Free_variables_and_bound_variables.
        // Perhaps it would be useful to have a visitor which builds a symbol table which could be used for this an other things.
        private class Impl : ExpressionVisitor
        {
            private readonly List<ParameterExpression> _bound = new List<ParameterExpression>();
            private readonly List<ParameterExpression> _all = new List<ParameterExpression>();

            public IEnumerable<ParameterExpression> Unbound { get => _all.Except(_bound); }

            // TODO: Find all the other places that might count as binding a parameter

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                _bound.AddRange(node.Parameters);
                return base.VisitLambda(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                _all.Add(node);
                return base.VisitParameter(node);
            }

        }
    }
}
