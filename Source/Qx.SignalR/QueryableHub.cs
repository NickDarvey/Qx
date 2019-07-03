using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Qx.QxAsyncQuery;

namespace Qx
{
    public abstract class QueryableHub : Hub
    {
        [HubMethodName("qx`n")]
        public IAsyncEnumerable<object> GetEnumerable(ExpressionNode expression)
        {
            // Till https://github.com/aspnet/AspNetCore/issues/11495
            var cancellationToken = default(CancellationToken);

            // TODO: Cache, but don't hold onto a reference to the Hub
            var queryables = FindQueryables(this);

            var expr = expression.ToExpression();

            var unboundParameters = QxAsyncQueryScanner.FindUnboundParameters(expr);


            // queryables --> parameters --> ( , )
            // ideally 
            // queryables -> param -> InvocationExpression
            // queryables -> synthparams -> param -> InvocationExpression
            // but probably the authz checks need to happen before the last bit
            // actually we have found the unboundparams already so really we
            // want to pass in a premapped mapping of (param, queryable) in and the rewriter just replaces blindly
            // and soemthing about synth params and building the invoke itself

            // queryables -> unboudnparams -> [(param, queryable)]
            // [(param, queryable)] -> Task<bool>  isComplete if all are bound
            // [(param, queryable)] -> Task<bool>  isAuthorized

            // synth?
            // queryable :: args -> invocationexpression 

            // unboundparams = bleh
            // map to queryables, 'binding' pipelien
            //   match up by name and types
            //   if any param doesn't get assigned an impl, die
            //   authorize

            // TOTHINK: Consider chaining visitors so intermediate trees aren't created
            var query = QxAsyncQueryRewriter.Rewrite<CancellationToken, IAsyncQueryable<object>>(
                SignalRQxAsyncQueryRewriter.RewriteManyResultsType(expression.ToExpression()), queryables);
            var invoke = query.Compile();
            return invoke(cancellationToken);

            // Authorization might be async so we can either,
            //   build it into the rewriting of unbound params (instead of Func<IAsyncQ<>> it'd be a Func<Task<IAsyncQ>>)
            //   which would also resolve the support for hub methods returning that anyway
            //   but the rewriting becomes more complex, would need to do rewriting like:
            //   Task.WhenAll(taskReturningQueryable1, taskReturningQueryable2).ContinueWith(t =>
            //      t.Result[0].Join(t.Result[1], (x, y) => x + y))

            // Or resolve them in the outer... somehow, but how do we know which queryables we need?
            // we could first find the unbound parameter expressions, then resolve, then rewrite?
            //   var allQueryablesDescription = SomeCachingThingWhichFetchesAllOfThePossibleQueryables(this)
            //   var unboundParameters = FindUnboundParameters(expression)
            //   var selectedQueryables = unboundParameters.Join(allQueryables, (l,r) => l.Name == r.Name)
            //   var isAuthorized = await Authorize(authzprovider, selectedqueryables.SelectMany(q => q.Policies))
            //   
            //   ohhh the problem is we need to apply the invoke constants and know the return type in order to actual eval it for the Task<IAQ<>> case
            //   this would still work for authz.
        }

        [HubMethodName("qx`1")]
        public Task<object> GetResult(ExpressionNode expression)
        {
            // TODO: Cache, but don't hold onto a reference to the Hub
            var queryables = FindQueryables(this);
            var query = QxAsyncQueryRewriter.Rewrite<CancellationToken, Task<object>>(
                SignalRQxAsyncQueryRewriter.RewriteSingleResultsType(expression.ToExpression()), queryables);
            var invoke = query.Compile();
            return invoke(this.Context.ConnectionAborted);
        }

        /// <summary>
        /// Finds methods which returns the IAsyncQueryables on a Hub.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="nameSelector"></param>
        /// <returns>A dictionary with the name of the queryable and a lambda expression which returns the queryable when invoked.</returns>
        private static IReadOnlyDictionary<string, LambdaExpression> FindQueryables(Hub hub) =>
            hub.GetType().GetMethods()
            .Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>))
            .ToDictionary(
                keySelector: m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name,
                elementSelector: m =>
                {
                    var args = m.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray(/* generate params once */);
                    var call = Expression.Call(Expression.Constant(hub), m, args);
                    return Expression.Lambda(call, args);
                });

        // should i be binding synthetic params here? i can't really validate that the type of the ParameterExpression is valid against the impl without knowing the synth



    }
}
