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
            _verifier = Verification.Combine(
                FeaturesVerification.Create(options.AllowedFeatures),
                MembersVerification.Create(MembersVerification.CreateDeclaredMembersVerifier(options.AllowedMembers)));
            _authorizationService = authorizationService;
            _authorizationPolicyProvider = authorizationPolicyProvider;
        }

        public Authorizer<Hubs.HubQueryableSourceDescription> GetAuthorizer(HubCallerContext context) =>
            Hubs.CreateHubAuthorizer(context.User, _authorizationService, _authorizationPolicyProvider);

        public Verifier GetVerifier() => _verifier;
    }
}
