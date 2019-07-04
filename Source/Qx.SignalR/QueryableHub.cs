using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Qx
{
    public abstract class QueryableHub<THub> : Hub
    {
        private static readonly IReadOnlyDictionary<string, HubMethodDescription> _hubMethods = FindQueryables2<THub>();
        //private static readonly IEnumerable<Type> _synthethicParameters = new[] { typeof(CancellationToken) };

        [HubMethodName("qx`n")]
        public async Task<IAsyncEnumerable<object>> GetEnumerable(ExpressionNode expression)
        {
            // Till https://github.com/aspnet/AspNetCore/issues/11495
            var cancellationToken = Context.ConnectionAborted;

            var query = await CompileQuery<IAsyncQueryable<object>>(expression, SignalRQxAsyncQueryRewriter.RewriteManyResultsType);

            return query(cancellationToken);
        }

        [HubMethodName("qx`1")]
        public async Task<object> GetResult(ExpressionNode expression)
        {
            var query = await CompileQuery<Task<object>>(expression, SignalRQxAsyncQueryRewriter.RewriteSingleResultsType);
            return await query(Context.ConnectionAborted);
        }

        private async Task<Func<CancellationToken, TResult>> CompileQuery<TResult>(ExpressionNode expression, Func<Expression, Expression> boxingRewriter)
        {
            var expr = expression.ToExpression();

            var unboundParameters = QxAsyncQueryScanner.FindUnboundParameters(expr);

            var isMethodsBound = TryBindMethods(unboundParameters, _hubMethods, out var methodBindings, out var methodBindingErrors);
            if (isMethodsBound == false) throw new HubException($"Failed to bind query to hub methods. {string.Join("; ", methodBindingErrors)}");

            var isAuthorized = await Authorize(Context.User, null, null, methodBindings.Values.SelectMany(v => v.AuthorizationPolicies));
            if (isAuthorized == false) throw new HubException("Some helpful message about authorization");

            var lambdaBindings = methodBindings.ToDictionary(kv => kv.Key, kv => kv.Value.GetMethod(this));

            var syntheticParameters = new[] { Expression.Parameter(typeof(CancellationToken)) };

            var isInvocationsBound = TryBindInvocations(lambdaBindings, syntheticParameters, out var invocationBindings, out var invocationBindingErrors);
            if (isInvocationsBound == false) throw new HubException($"Failed to bind query to hub methods. {string.Join("; ", invocationBindingErrors)}");

            var boundQuery = QxAsyncQueryRewriter.Rewrite(expr, invocationBindings);
            var boxedQuery = boxingRewriter(boundQuery);

            var invoke = Expression.Lambda<Func<CancellationToken, TResult>>(boxedQuery, syntheticParameters).Compile();

            return invoke;
        }

        /// <summary>
        /// Finds methods which returns the IAsyncQueryables on a Hub.
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="nameSelector"></param>
        /// <returns>A dictionary with the name of the queryable and a lambda expression which returns the queryable when invoked.</returns>
        private static IReadOnlyDictionary<string, HubMethodDescription> FindQueryables2<T>() =>
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

        private static async ValueTask<bool> Authorize(ClaimsPrincipal user, IAuthorizationService service, IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> policies)
        {
            if (policies.Any() == false) return true;
            var combinedPolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            var result = await service.AuthorizeAsync(user, combinedPolicy);
            return result.Succeeded;
        }

        private static bool TryBindMethods(IEnumerable<ParameterExpression> parameters, IReadOnlyDictionary<string, HubMethodDescription> methods, out IReadOnlyDictionary<ParameterExpression, HubMethodDescription> bindings, out IEnumerable<string> errors)
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

        private static bool TryBindInvocations(IReadOnlyDictionary<ParameterExpression, LambdaExpression> lambdaBindings, IEnumerable<ParameterExpression> syntheticParameters, out IReadOnlyDictionary<ParameterExpression, QxAsyncQueryRewriter.InvocationFactory> bindings, out IEnumerable<string> errors)
        {
            var bindings_ = new Dictionary<ParameterExpression, QxAsyncQueryRewriter.InvocationFactory>();
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
                    var originalAndSyntheticParameterTypes = binding.Key.Type.GetGenericArguments().SkipLast(1).Concat(syntheticParameters.Select(p => p.Type));
                    var boundParameterTypes = binding.Value.Parameters.Select(p => p.Type);

                    if (originalAndSyntheticParameterTypes.SequenceEqual(boundParameterTypes) == false)
                    {
                        errors_ ??= new List<string>();
                        errors_.Add($"Specified parameters ({string.Join(", ", originalAndSyntheticParameterTypes)}) for query source named '{binding.Key.Name}' do not match the bound parameters ({string.Join(", ", boundParameterTypes)})");
                    }

                    bindings_[binding.Key] = args => Expression.Invoke(binding.Value, args.Concat(syntheticParameters));
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

        private class HubMethodDescription
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
