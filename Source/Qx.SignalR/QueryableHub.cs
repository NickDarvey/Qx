using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // TODO: Support passing cancellation token to source queryables
            //       Where do I get my token from and where do I shove it?
            //       Get: From above
            //       Shove: Source queryables might have a 'cancellationtoken' parameter
            //       Weave: Via FindQueryables? It builds the expressions.
            //              Ideally we could cahce the queryables and only supply the cancel as neexed during invocation.
            // TODO: Cache, but don't hold onto a reference to the Hub
            var queryables = FindQueryables(this, m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name);

            var query = QxAsyncQueryRewriter.Rewrite<CancellationToken, IAsyncQueryable<object>>(
                SignalRQxAsyncQueryRewriter.RewriteManyResultsType(expression.ToExpression()), queryables);
            var invoke = query.Compile();
            return invoke(cancellationToken);
            //var query = new QxAsyncQueryRewriter(queryables).Visit(expression.ToExpression());


            //var func = Expression.Lambda<Func<IAsyncQueryable<TElement>>>(query).Compile()

            //var objectQuery = ConvertQueryableElements<object>(query);
            //var invoke = CompileQuery<object>(objectQuery);
            //return invoke();
        }

        [HubMethodName("qx`1")]
        public Task<object> GetResult(ExpressionNode expression)
        {
            // TODO: Cache, but don't hold onto a reference to the Hub
            var queryables = FindQueryables(this, m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name);
            var query = QxAsyncQueryRewriter.Rewrite<CancellationToken, Task<object>>(
                SignalRQxAsyncQueryRewriter.RewriteSingleResultsType(expression.ToExpression()), queryables);
            var invoke = query.Compile();
            return invoke(this.Context.ConnectionAborted);
        }

    }
}
