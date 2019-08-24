using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Qx.Security;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using static Qx.SignalR.Queries;

namespace Qx.SignalR
{
    /// <summary>
    /// A collection of functions for using a <see cref="Hub"/> as a queryable source.
    /// </summary>
    public static class Hubs
    {
        /// <summary>
        /// Finds methods which return <see cref="IAsyncQueryable{T}"/> on a <see cref="Hub"/>.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="nameSelector"></param>
        /// <returns>A dictionary with the name of the queryable and a lambda expression which returns the queryable when invoked.</returns>
        public static IReadOnlyDictionary<string, HubMethodDescription> FindHubMethodDescriptions<T>() =>
            typeof(T).GetMethods()
            .Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>))
            .ToDictionary(
                m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name,
                m => new HubMethodDescription(m, m.GetCustomAttributes<AuthorizeAttribute>(inherit: true)));

        public static IReadOnlyDictionary<string, HubQueryableSourceDescription> WithInstance(this IReadOnlyDictionary<string, HubMethodDescription> descriptions, Hub hub) =>
            descriptions.ToDictionary(d => d.Key, d => new HubQueryableSourceDescription(hub, d.Value.Method, d.Value.Policies));

        public static Authorizer<HubQueryableSourceDescription> CreateHubAuthorizer(
            ClaimsPrincipal user,
            IAuthorizationService authorizationService,
            IAuthorizationPolicyProvider authorizationPolicyProvider) =>
            async bindings =>
            {
                var policies = bindings.SelectMany(binding => binding.Policies).ToArray();
                if (policies.Length == 0) return Authorization.Authorized;
                var combinedPolicy = await AuthorizationPolicy.CombineAsync(authorizationPolicyProvider, policies);
                var result = await authorizationService.AuthorizeAsync(user, combinedPolicy);
                return result.Succeeded
                    ? Authorization.Authorized
                    : Authorization.Forbid(reasons: result.Failure.FailedRequirements.Select(x => x.GetType().Name));
            };

        public class HubQueryableSourceDescription : IQueryableSourceDescription
        {
            public HubQueryableSourceDescription(object instance, MethodInfo method, IEnumerable<IAuthorizeData> policies)
            {
                Instance = instance;
                Method = method;
                Policies = policies;
            }

            public MethodInfo Method { get; }

            public object Instance { get; }

            public IEnumerable<IAuthorizeData> Policies { get; }
        }

        public class HubMethodDescription
        {
            public HubMethodDescription(MethodInfo method, IEnumerable<IAuthorizeData> policies)
            {
                Method = method;
                Policies = policies;
            }

            public MethodInfo Method { get; }

            public IEnumerable<IAuthorizeData> Policies { get; }
        }
    }
}
