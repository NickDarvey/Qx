using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Qx
{
    /// <summary>
    /// Rewrites a Qx query (an expression tree with unbound AsyncQueryable<> parameters) binding it to the provided factories.
    /// </summary>
    /// 
    // TODO: Move ExpressionVisitor into an encapsulated Impl class
    public class QxAsyncQueryRewriter : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<string, LambdaExpression> _queryables;
        private readonly IEnumerable<ParameterExpression> _parameters;

        private QxAsyncQueryRewriter(IReadOnlyDictionary<string, LambdaExpression> queryables, IEnumerable<ParameterExpression> parameters)
        {
            _queryables = queryables;
            _parameters = parameters;
        }

        /// <summary>
        /// Rewrites a Qx query (an expression tree with unbound <see cref="IAsyncQueryable{T}"/> parameters) binding it to the provided queryable factories
        /// and creating an expression to which you can pass synthetic arguments to the factories.
        /// </summary>
        /// <typeparam name="TArg">A synthetic argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expression"></param>
        /// <param name="queryables"></param>
        /// <returns></returns>
        public static Expression<Func<TArg, TResult>> Rewrite<TArg, TResult>(Expression expression, IReadOnlyDictionary<string, LambdaExpression> queryables) =>
            Rewrite<Func<TArg, TResult>>(expression, queryables, new[] { Expression.Parameter(typeof(TArg)) });

        public static Expression<Func<TResult>> Rewrite<TResult>(Expression expression, IReadOnlyDictionary<string, LambdaExpression> queryables) =>
            Rewrite<Func<TResult>>(expression, queryables, Enumerable.Empty<ParameterExpression>());

        // TODO: etc
 
        private static Expression<TDelegate> Rewrite<TDelegate>(Expression expression, IReadOnlyDictionary<string, LambdaExpression> queryables, IEnumerable<ParameterExpression> parameters) =>
            Expression.Lambda<TDelegate>(new QxAsyncQueryRewriter(queryables, parameters).Visit(expression), parameters);

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            // TODO: Ensure the parameter is unbound?
            // TODO: Look into beta reduction to see if we can lose the LambdaExpr, reduce to underlying CallExpr
            if (node.Type.IsGenericType
                && node.Type.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>)
                && node.Expression is ParameterExpression unboundParameterExpression)
            {
                if (_queryables.TryGetValue(unboundParameterExpression.Name, out var queryable))
                {
                    var hasArgumentsMatchingParameters = node.Arguments
                        .Zip(queryable.Parameters, (arg, param) => arg.Type == param.Type)
                        .All(x => x);

                    if (hasArgumentsMatchingParameters == false) throw new ArgumentException(/* TODO: Be helpful */);

                    var expression = Visit(queryable);
                    var syntheticArguments = queryable.Parameters // TODO: Error when our queryable wants more parameters than we have
                        .Skip(node.Arguments.Count)
                        .Zip(_parameters, (synthetic, supplied) => supplied);
                    var arguments = node.Arguments
                        .Concat(syntheticArguments)
                        .Select(Visit);

                    return Expression.Invoke(expression, arguments);
                }
                else throw new InvalidOperationException($"No known queryable named '{unboundParameterExpression.Name}'");
            }
            return base.VisitInvocation(node);
        }
    }
}
