using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static Qx.Rewriters;
using static Qx.SignalRBinders;
using static Qx.SignalRRewriters;

namespace Qx
{
    public abstract class QueryableHub<THub> : Hub
    {
        private static readonly IReadOnlyDictionary<string, HubMethodDescription> _hubMethods = FindQueryables<THub>();

        [HubMethodName("qx`n")]
        public async Task<IAsyncEnumerable<object>> GetEnumerable(ExpressionNode expression)
        {
            // Till https://github.com/aspnet/AspNetCore/issues/11495
            var cancellationToken = Context.ConnectionAborted;

            var query = await CompileQuery<IAsyncQueryable<object>>(expression, RewriteManyResultsType);

            return query(cancellationToken);
        }

        [HubMethodName("qx`1")]
        public async Task<object> GetResult(ExpressionNode expression)
        {
            var query = await CompileQuery<Task<object>>(expression, RewriteSingleResultsType);
            return await query(Context.ConnectionAborted);
        }

        private async Task<Func<CancellationToken, TResult>> CompileQuery<TResult>(ExpressionNode expression, Func<Expression, Expression> boxingRewriter)
        {
            var expr = expression.ToExpression();

            var unboundParameters = Scanners.FindUnboundParameters(expr);

            var isMethodsBound = TryBindMethods(unboundParameters, _hubMethods, out var methodBindings, out var methodBindingErrors);
            if (isMethodsBound == false) throw new HubException($"Failed to bind query to hub methods. {string.Join("; ", methodBindingErrors)}");

            var isAuthorized = await Authorize(Context.User, null, null, methodBindings.Values.SelectMany(v => v.AuthorizationPolicies));
            if (isAuthorized == false) throw new HubException("Some helpful message about authorization");

            var lambdaBindings = methodBindings.ToDictionary(kv => kv.Key, kv => kv.Value.GetMethod(this));

            var syntheticParameters = new[] { Expression.Parameter(typeof(CancellationToken)) };

            var isInvocationsBound = TryBindInvocations(lambdaBindings, syntheticParameters, out var invocationBindings, out var invocationBindingErrors);
            if (isInvocationsBound == false) throw new HubException($"Failed to bind query to hub methods. {string.Join("; ", invocationBindingErrors)}");

            var boundQuery = Rewrite(expr, invocationBindings);
            var boxedQuery = boxingRewriter(boundQuery);

            var invoke = Expression.Lambda<Func<CancellationToken, TResult>>(boxedQuery, syntheticParameters).Compile();

            return invoke;
        }
    }
}
