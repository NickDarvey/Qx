using System.Linq.Expressions;
using static Qx.Internals.KnownResourceExtensions;

namespace Qx.Client.Rewriters
{
    // TODO: (Optionally, opt-in?) support extensions of the client class, e.g. an extension which offers a strongly-typed Range method
    internal class ClientCallRewriter
    {
        public static Expression Rewrite(Expression expression) => new Impl().Visit(expression);

        private class Impl : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var expression = base.VisitMethodCall(node);

                // If the method call expression's object is our client
                // and it's one of the 'Get' functions
                // with one argument (the 'name')

                if (!(expression is MethodCallExpression methodCallExpression)
                    || methodCallExpression.Object == null
                    || typeof(IAsyncQueryClient).IsAssignableFrom(methodCallExpression.Object.Type) == false
                    || methodCallExpression.Method.Name != nameof(IAsyncQueryClient.GetEnumerable)
                    || methodCallExpression.Arguments.Count != 1
                    || !(methodCallExpression.Arguments[0] is ConstantExpression argumentExpression)
                    || !(argumentExpression.Value is string name)
                    || TryGetKnownResourceType(methodCallExpression.Type, out var resourceType, out var isResourceInvocation) == false)
                    return expression;

                if (isResourceInvocation == true) return Expression.Parameter(methodCallExpression.Type, name);
                // TOTHINK: Do I really need to hoist it to a func? The execution side could do this if it needs it.
                else return Expression.Invoke(Expression.Parameter(Expression.GetFuncType(methodCallExpression.Type), name));
            }
        }
    }
}
