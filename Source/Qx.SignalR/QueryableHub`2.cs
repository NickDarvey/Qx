using Microsoft.AspNetCore.SignalR;
using Remote.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Qx.SignalR.Hubs;
using static Qx.SignalR.Queries;

namespace Qx.SignalR
{
    public abstract class QueryableHub<THub, THubClient> : Hub<THubClient> where THubClient : class
    {
        private static readonly IReadOnlyDictionary<string, HubMethodDescription> _hubMethods = FindHubMethodDescriptions<THub>();
        private readonly IQxService _service;

        public QueryableHub(IQxService service) => _service = service;

        [HubMethodName("qx`n")]
        public async Task<IAsyncEnumerable<object>> GetEnumerable(Remote.Linq.Expressions.MethodCallExpression expression, CancellationToken cancellationToken)
        {
            var query = await CompileEnumerableQuery(
                expression: expression.ToLinqExpression(),
                verify: _service.GetVerifier(),
                authorize: _service.GetAuthorizer(Context),
                bindings: _hubMethods.WithInstance(this));
            return query.OrThrowHubException()(cancellationToken);
        }

        [HubMethodName("qx`1")]
        public async Task<object> GetResult(Remote.Linq.Expressions.MethodCallExpression expression)
        {
            var query = await CompileExecutableQuery(
                expression: expression.ToLinqExpression(),
                verify: _service.GetVerifier(),
                authorize: _service.GetAuthorizer(Context),
                bindings: _hubMethods.WithInstance(this));
            return await query.OrThrowHubException()(Context.ConnectionAborted);
        }
    }
}
