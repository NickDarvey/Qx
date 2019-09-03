using BenchmarkDotNet.Attributes;
using Qx.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qx.Benchmarks
{
    [MemoryDiagnoser]
    public class QueriesBenchmarks
    {
        private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;
        private readonly Func<int, CancellationToken, int> _echo = (i, _) => i;
        private readonly Expression _query = Expression.Invoke(
            Expression.Parameter(typeof(Func<int, int>), "Echo"),
            Expression.Constant(42));

        [Benchmark(Baseline = true)]
        public async Task<int> CompileQuery()
        {
            var invoke = await Qx.SignalR.Queries.CompileQuery<QueryableSourceDescription, int>(
                expression: _query,
                verify: _ => Verification.Verified,
                authorize: _ => Authorization.AuthorizedTask,
                bindings: new Dictionary<string, QueryableSourceDescription> { { "Echo", new QueryableSourceDescription(_echo.Method, _echo.Target) } },
                boxingRewriter: expr => expr);
            var result = invoke.Match(i => i(cancellationToken), e => throw new InvalidOperationException("Nope"));

            return result;
        }

        [Benchmark]
        public async Task<int> CompileQuery2()
        {
            var invoke = await CompileQuery2<QueryableSourceDescription, int>(
                expression: _query,
                verify: _ => Verification.Verified,
                authorize: _ => Authorization.AuthorizedTask,
                bindings: new Dictionary<string, QueryableSourceDescription> { { "Echo", new QueryableSourceDescription(_echo.Method, _echo.Target) } },
                boxingRewriter: expr => expr);
            var result = invoke(cancellationToken);

            return result;
        }

        private class QueryableSourceDescription : IQueryableSourceDescription
        {
            public QueryableSourceDescription(MethodInfo method, object instance)
            {
                Method = method;
                Instance = instance;
            }

            public MethodInfo Method { get; }

            public object Instance { get; }
        }

        /// <remarks>
        /// The prior implementation of CompileQuery.
        /// Imperative, less safe and about the same perf.
        /// </remarks>
        private static async Task<Func<CancellationToken, TResult>> CompileQuery2<TSourceDescription, TResult>(
            Expression expression,
            Verifier verify,
            Authorizer<TSourceDescription> authorize,
            IReadOnlyDictionary<string, TSourceDescription> bindings,
            Func<Expression, Expression> boxingRewriter) where TSourceDescription : IQueryableSourceDescription
        {
            static void ThrowHubException(string message, IEnumerable<string> errors) =>
                throw new InvalidOperationException($"{message}.{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");

            var verification = verify(expression);
            verification.Match(Valid: _ => { }, Invalid: errors => ThrowHubException("Failed to verify query", errors));

            var unboundParameters = Scanners.FindUnboundParameters(expression);

            var isMethodsBound = Qx.SignalR.Binders.TryBindMethods(unboundParameters, bindings, out var methodBindings, out var methodBindingErrors);
            if (isMethodsBound == false) ThrowHubException("Failed to bind query to hub methods", methodBindingErrors);

            var authorization = await authorize(methodBindings.Values);
            authorization.Match(Valid: _ => { }, Invalid: errors => ThrowHubException("Failed to authorize query", errors));

            var expressionBindings = methodBindings.ToDictionary(b => b.Key, b =>
            {
                var args = b.Value.Method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
                var call = Expression.Call(Expression.Constant(b.Value.Instance), b.Value.Method, args);
                return Expression.Lambda(call, args);
            });

            var syntheticParameters = new[] { Expression.Parameter(typeof(CancellationToken)) };

            var isInvocationsBound = Qx.SignalR.Binders.TryBindInvocations(expressionBindings, syntheticParameters, out var invocationBindings, out var invocationBindingErrors);
            if (isInvocationsBound == false) ThrowHubException("Failed to bind query to hub methods", invocationBindingErrors);

            var boundQuery = Qx.Rewriters.BindingRewriter.Rewrite(expression, invocationBindings);
            var boxedQuery = boxingRewriter(boundQuery);

            var invoke = Expression.Lambda<Func<CancellationToken, TResult>>(boxedQuery, syntheticParameters).Compile();

            return invoke;
        }
    }
}
