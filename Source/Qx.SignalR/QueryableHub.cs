using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using static Qx.QxAsyncQuery;

namespace Qx
{
    public abstract class QueryableHub : Hub
    {
        [HubMethodName("qx")]
        public IAsyncEnumerable<object> GetEnumerable(ExpressionNode expression)
        {
            // TODO: Cache, but don't hold onto a reference to the Hub
            var queryables = FindQueryables(this, m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name);
            var query = new QxAsyncQueryRewriter(queryables).Visit(expression.ToExpression());
            var objectQuery = ConvertQueryElements<object>(query);
            var invoke = CompileQuery<object>(objectQuery);
            return invoke();
        }
    }
}
