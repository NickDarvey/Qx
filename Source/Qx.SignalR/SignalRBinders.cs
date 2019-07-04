using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Qx
{
    /// <summary>
    /// A collection of functions for runtime binding to SignalR bits.
    /// </summary>
    internal static class SignalRBinders
    {
        /// <summary>
        /// Finds methods which returns the IAsyncQueryables on a Hub.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="nameSelector"></param>
        /// <returns>A dictionary with the name of the queryable and a lambda expression which returns the queryable when invoked.</returns>
        public static IReadOnlyDictionary<string, HubMethodDescription> FindQueryables<T>() =>
            typeof(T).GetMethods()
            .Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncQueryable<>))
            .ToDictionary(
                keySelector: m => m.GetCustomAttribute<HubMethodNameAttribute>()?.Name ?? m.Name,
                elementSelector: m => new HubMethodDescription(
                    getMethod: hub =>
                    {
                        var args = m.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray(/* generate params once */);
                        var call = Expression.Call(Expression.Constant(hub), m, args);
                        return Expression.Lambda(call, args);
                    },
                    authorizationPolicies: m.GetCustomAttributes<AuthorizeAttribute>(inherit: true)));

        public static async ValueTask<bool> Authorize(ClaimsPrincipal user, IAuthorizationService service, IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> policies)
        {
            if (policies.Any() == false) return true;
            var combinedPolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            var result = await service.AuthorizeAsync(user, combinedPolicy);
            return result.Succeeded;
        }

        public static bool TryBindMethods(IEnumerable<ParameterExpression> parameters, IReadOnlyDictionary<string, HubMethodDescription> methods, out IReadOnlyDictionary<ParameterExpression, HubMethodDescription> bindings, out IEnumerable<string> errors)
        {
            var bindings_ = new Dictionary<ParameterExpression, HubMethodDescription>();
            var errors_ = default(List<string>);
            foreach (var parameter in parameters)
            {
                // We don't test if the parameters match yet, because there could be synthetic parameters used,
                // we just ensure that such a method exists.
                if (methods.TryGetValue(parameter.Name, out var method))
                {
                    bindings_[parameter] = method;
                }
                else
                {
                    errors_ ??= new List<string>();
                    errors_.Add($"No hub method found for query source named '{parameter.Name}'");
                }
            }

            if (errors_?.Count > 0)
            {
                bindings = default;
                errors = errors_;
                return false;
            }

            else
            {
                bindings = bindings_;
                errors = default;
                return true;
            }
        }

        /// <summary>
        /// Tries to converts lambda bindings to invocation (factory) bindings, injecting optional synthetic parameters if needed.
        /// </summary>
        /// <remarks>
        /// The results can be used as bindings when rewriting a Qx query to replace its unbound parameters,
        /// <see cref="Rewriters.Rewrite(Expression, IReadOnlyDictionary{ParameterExpression, Rewriters.InvocationFactory})"/>.
        /// </remarks>
        /// <param name="lambdaBindings">A set of unbound <see cref="ParameterExpression"/> and <see cref="LambdaExpression"/> bindings.</param>
        /// <param name="optionalSyntheticParameters">Optional synthetic parameters to supply should a <see cref="LambdaExpression"/> require them.</param>
        /// <param name="bindings">If success, the resulting bindings.</param>
        /// <param name="errors">If failure, the errors which caused the failure.</param>
        /// <returns>True, if success. False, if failure</returns>
        public static bool TryBindInvocations(IReadOnlyDictionary<ParameterExpression, LambdaExpression> lambdaBindings, IEnumerable<ParameterExpression> optionalSyntheticParameters, out IReadOnlyDictionary<ParameterExpression, Rewriters.InvocationFactory> bindings, out IEnumerable<string> errors)
        {
            var bindings_ = new Dictionary<ParameterExpression, Rewriters.InvocationFactory>();
            var errors_ = default(List<string>);
            foreach (var binding in lambdaBindings)
            {
                if (binding.Value == default) throw new InvalidOperationException($"No binding for query source named '{binding.Key}'");

                if (binding.Key.Type == binding.Value.Type)
                {
                    bindings_[binding.Key] = args => Expression.Invoke(binding.Value, args);
                }

                else // with synthetic params
                {
                    //if (binding.Key.Type.IsGenericType == false || binding.Key.Type.GetGenericTypeDefinition()) // TODO: Some kind of check to make sure we're actually dealing with a Func of whatever arity
                    var originalAndSyntheticParameterTypes = binding.Key.Type.GetGenericArguments().SkipLast(1).Concat(optionalSyntheticParameters.Select(p => p.Type));
                    var boundParameterTypes = binding.Value.Parameters.Select(p => p.Type);

                    if (originalAndSyntheticParameterTypes.SequenceEqual(boundParameterTypes) == false)
                    {
                        errors_ ??= new List<string>();
                        errors_.Add($"Specified parameters ({string.Join(", ", originalAndSyntheticParameterTypes)}) for query source named '{binding.Key.Name}' do not match the bound parameters ({string.Join(", ", boundParameterTypes)})");
                    }

                    bindings_[binding.Key] = args => Expression.Invoke(binding.Value, args.Concat(optionalSyntheticParameters));
                }
            }

            if (errors_?.Count > 0)
            {
                bindings = default;
                errors = errors_;
                return false;
            }

            else
            {
                bindings = bindings_;
                errors = default;
                return true;
            }
        }

        public class HubMethodDescription
        {
            public HubMethodDescription(Func<Hub, LambdaExpression> getMethod, IEnumerable<IAuthorizeData> authorizationPolicies)
            {
                GetMethod = getMethod;
                AuthorizationPolicies = authorizationPolicies;
            }

            public Func<Hub, LambdaExpression> GetMethod { get; }
            public IEnumerable<IAuthorizeData> AuthorizationPolicies { get; }
        }
    }
}
