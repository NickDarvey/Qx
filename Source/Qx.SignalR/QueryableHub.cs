using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
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
            // TODO: Cache, but don't hold onto a reference to the Hub
            // TODO: Support passing cancellation token to source queryables
            var queryables = FindQueryables(this, m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name);
            var query = new QxAsyncQueryRewriter(queryables).Visit(expression.ToExpression());
            var objectQuery = ConvertQueryableElements<object>(query);
            var invoke = CompileQuery<object>(objectQuery);
            return invoke();
        }

        [HubMethodName("qx`1")]
        public Task<object> GetResult(ExpressionNode expression)
        {
            // TODO: Cache, but don't hold onto a reference to the Hub
            var queryables = FindQueryables(this, m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name);
            var query = new QxAsyncQueryRewriter(queryables).Visit(expression.ToExpression());
            var objectQuery = ConvertValueTaskResult<object>(query);
            var invoke = Expression.Lambda<Func<Task<object>>>(objectQuery).Compile();
            return invoke(/* this.Context.ConnectionAborted */);
        }
    }
}
