using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Qx.HubSources;
using static Qx.QueryableHub;

namespace Qx
{
    public abstract class QueryableHub<THub, THubClient> : Hub<THubClient> where THubClient : class
    {
        private static readonly IReadOnlyDictionary<string, HubMethodDescription> _hubMethods = FindHubMethodDescriptions<THub>();

        private readonly IAuthorizationService _authorizationService;
        private readonly IAuthorizationPolicyProvider _authorizationPolicyProvider;

        public QueryableHub(IAuthorizationService authorizationService, IAuthorizationPolicyProvider authorizationPolicyProvider)
        {
            _authorizationService = authorizationService;
            _authorizationPolicyProvider = authorizationPolicyProvider;
        }

        [HubMethodName("qx`n")]
        public async Task<IAsyncEnumerable<object>> GetEnumerable(ExpressionNode expression)
        {
            // Till https://github.com/aspnet/AspNetCore/issues/11495
            var cancellationToken = Context.ConnectionAborted;
            var authorizer = CreateHubAuthorizer(Context.User, _authorizationService, _authorizationPolicyProvider);
            var query = await CompileEnumerableQuery(expression, authorizer, _hubMethods.WithInstance(this));
            return query(cancellationToken);
        }

        [HubMethodName("qx`1")]
        public async Task<object> GetResult(ExpressionNode expression)
        {
            var authorizer = CreateHubAuthorizer(Context.User, _authorizationService, _authorizationPolicyProvider);
            var query = await CompileExecutableQuery(expression, authorizer, _hubMethods.WithInstance(this));
            return await query(Context.ConnectionAborted);
        }
    }
}
