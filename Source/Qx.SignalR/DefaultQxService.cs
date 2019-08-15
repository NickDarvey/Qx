using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Qx.Security;

namespace Qx.SignalR
{
    public class DefaultQxService : IQxService
    {
        private readonly Verifier _verifier;
        private readonly IAuthorizationService _authorizationService;
        private readonly IAuthorizationPolicyProvider _authorizationPolicyProvider;

        public DefaultQxService(QxOptions options, IAuthorizationService authorizationService, IAuthorizationPolicyProvider authorizationPolicyProvider)
        {
            var featuresVerifier = AllowedFeatures.Create(options.AllowedFeatures);
            var membersVerifier = AllowedMembers.Create(AllowedMembers.CreateDeclaredMembersVerifier(options.AllowedMembers));
            _verifier = featuresVerifier.And(membersVerifier);
            _authorizationService = authorizationService;
            _authorizationPolicyProvider = authorizationPolicyProvider;
        }

        public Queries.Authorizer<Hubs.HubQueryableSourceDescription> GetAuthorizer(HubCallerContext context) =>
            Hubs.CreateHubAuthorizer(context.User, _authorizationService, _authorizationPolicyProvider);

        public Verifier GetVerifier() => _verifier;
    }
}
