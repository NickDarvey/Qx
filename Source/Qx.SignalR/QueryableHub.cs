using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Qx.Rewriters;
using static Qx.SignalR.Binders;
using static Qx.SignalR.Rewriters;

namespace Qx.SignalR
{
    /// <summary>
    /// A collection of functions for creating a queryable hub.
    /// </summary>
    public static class QueryableHub
    {
        public delegate Task<bool> Authorizer<TMethodDescription>(IEnumerable<TMethodDescription> bindings) where TMethodDescription : IQueryableSourceDescription;

        public interface IQueryableSourceDescription
        {
            MethodInfo Method { get; }
            object Instance { get; }
        }

        public static Task<Func<CancellationToken, IAsyncQueryable<object>>> CompileEnumerableQuery<TSourceDescription>(
            Expression query,
            Authorizer<TSourceDescription> authorizer,
            IReadOnlyDictionary<string, TSourceDescription> bindings) where TSourceDescription : IQueryableSourceDescription =>
            CompileQuery<TSourceDescription, IAsyncQueryable<object>>(query, authorizer, bindings, RewriteManyResultsType);

        public static Task<Func<CancellationToken, Task<object>>> CompileExecutableQuery<TSourceDescription>(
            Expression query,
            Authorizer<TSourceDescription> authorizer,
            IReadOnlyDictionary<string, TSourceDescription> bindings) where TSourceDescription : IQueryableSourceDescription =>
            CompileQuery<TSourceDescription, Task<object>>(query, authorizer, bindings, RewriteSingleResultsType);

        internal static async Task<Func<CancellationToken, TResult>> CompileQuery<TSourceDescription, TResult>(
            Expression expression,
            Authorizer<TSourceDescription> authorizer,
            IReadOnlyDictionary<string, TSourceDescription> bindings,
            Func<Expression, Expression> boxingRewriter) where TSourceDescription : IQueryableSourceDescription
        {
            // from _ in security.Verify(expr)
            // let unboundParameters = Find(expr)
            // from methodBindings in BindMethods(unboundParameters, bindings) // lift to Task
            // let expressionBindings = methodBindings.ToDictionary(...)
            // from invocationBindings = BindInvocations(expressionBindings)
            // from query in BindingRewriter(expr, invocationBindings)
            // let boxedQuery = boxingRewriter(boundQuery)

            var unboundParameters = Scanners.FindUnboundParameters(expression);

            var isMethodsBound = TryBindMethods(unboundParameters, bindings, out var methodBindings, out var methodBindingErrors);
            if (isMethodsBound == false) throw new HubException($"Failed to bind query to hub methods. {string.Join("; ", methodBindingErrors)}");

            var isAuthorized = await authorizer(methodBindings.Values);
            if (isAuthorized == false) throw new HubException("Some helpful message about authorization");

            var expressionBindings = methodBindings.ToDictionary(b => b.Key, b =>
            {
                var args = b.Value.Method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
                var call = Expression.Call(Expression.Constant(b.Value.Instance), b.Value.Method, args);
                return Expression.Lambda(call, args);
            });

            var syntheticParameters = new[] { Expression.Parameter(typeof(CancellationToken)) };

            var isInvocationsBound = TryBindInvocations(expressionBindings, syntheticParameters, out var invocationBindings, out var invocationBindingErrors);
            if (isInvocationsBound == false) throw new HubException($"Failed to bind query to hub methods. {string.Join("; ", invocationBindingErrors)}");

            var boundQuery = BindingRewriter(expression, invocationBindings);
            var boxedQuery = boxingRewriter(boundQuery);

            var invoke = Expression.Lambda<Func<CancellationToken, TResult>>(boxedQuery, syntheticParameters).Compile();

            return invoke;
        }
    }
}
