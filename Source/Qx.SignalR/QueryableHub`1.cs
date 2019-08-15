using Microsoft.AspNetCore.SignalR;
using Qx.Security;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Qx.SignalR.HubSources;
using static Qx.SignalR.QueryableHub;

namespace Qx.SignalR
{
    public abstract class QueryableHub<THub> : Hub
    {
        private static readonly IReadOnlyDictionary<string, HubMethodDescription> _hubMethods = FindHubMethodDescriptions<THub>();
        private readonly Verifier _verifier;
        private readonly Func<HubCallerContext, Authorizer<HubQueryableSourceDescription>> _createAuthorizer;

        public QueryableHub(
            Verifier verifier,
            Func<HubCallerContext, Authorizer<HubQueryableSourceDescription>> createAuthorizer)
        {
            _verifier = verifier;
            _createAuthorizer = createAuthorizer;
        }

        [HubMethodName("qx`n")]
        public async Task<IAsyncEnumerable<object>> GetEnumerable(ExpressionNode expression)
        {
            // Till https://github.com/aspnet/AspNetCore/issues/11495
            var cancellationToken = Context.ConnectionAborted;
            var authorizer = _createAuthorizer(Context);
            var query = await CompileEnumerableQuery(
                query: expression.ToExpression(),
                verify: _verifier,
                authorize: _createAuthorizer(Context),
                bindings: _hubMethods.WithInstance(this));
            return query(cancellationToken);
        }

        [HubMethodName("qx`1")]
        public async Task<object> GetResult(ExpressionNode expression)
        {
            var query = await CompileExecutableQuery(
                query: expression.ToExpression(),
                verify: _verifier,
                authorize: _createAuthorizer(Context),
                bindings: _hubMethods.WithInstance(this));
            return await query(Context.ConnectionAborted);
        }
    }
}
