using Microsoft.AspNetCore.SignalR;
using Qx.Security;
using static Qx.SignalR.Hubs;
using static Qx.SignalR.Queries;

namespace Qx.SignalR
{
    public interface IQxService
    {
        Verifier GetVerifier();

        /// <summary>
        /// Get an <see cref="Authorizer{TMethodDescription}"/> for a specific caller context.
        /// </summary>
        /// <param name="context">The specific caller context.</param>
        /// <returns>A contextual authorizer.</returns>
        Authorizer<HubQueryableSourceDescription> GetAuthorizer(HubCallerContext context);
    }
}
