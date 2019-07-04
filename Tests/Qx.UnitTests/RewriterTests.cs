using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Qx.UnitTests
{
    public class RewriterTests
    {
        [Fact]
        public void Should_rewrite_simple_parameter_expression_to_invocation()
        {
            var range = AsyncEnumerable.Range(0, 10);
            Expression<Func<IAsyncQueryable<int>>> source = () => range.AsAsyncQueryable();
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var query = client.GetEnumerable<int>("Range");
            var bindings = CreateBindings((query, source));

            var result = QxAsyncQueryRewriter.Rewrite(query.Expression, bindings);

            var invoke = Expression.Lambda<Func<IAsyncQueryable<int>>>(result).Compile();
            Assert.Equal(range.ToEnumerable(), invoke().ToEnumerable());
        }

        [Fact]
        public void Should_rewrite_multiple_parameter_expressions_to_invocations()
        {
            var range1 = AsyncEnumerable.Range(0, 10);
            var range2 = AsyncEnumerable.Range(10, 10);
            Expression<Func<IAsyncQueryable<int>>> source1 = () => range1.AsAsyncQueryable();
            Expression<Func<IAsyncQueryable<int>>> source2 = () => range2.AsAsyncQueryable();
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var querySource1 = client.GetEnumerable<int>("Range1");
            var querySource2 = client.GetEnumerable<int>("Range2");
            var bindings = CreateBindings((querySource1, source1), (querySource2, source2));
            var query = querySource1.Join(querySource2, x => x, y => y, (x, y) => x + y);
            var expected = range1.Join(range2, x => x, y => y, (x, y) => x + y);

            var result = QxAsyncQueryRewriter.Rewrite(query.Expression, bindings);

            var invoke = Expression.Lambda<Func<IAsyncQueryable<int>>>(result).Compile();
            Assert.Equal(expected.ToEnumerable(), invoke().ToEnumerable());
        }


        [Fact]
        public void Should_rewrite_parameter_expressions_with_arguments_to_invocations()
        {
            var start = 0; var count = 10;
            Func<int, int, IAsyncEnumerable<int>> range = (start, count) => AsyncEnumerable.Range(start, count);
            Expression<Func<int, int, IAsyncQueryable<int>>> source = (start, count) => range(start, count).AsAsyncQueryable();
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var query = client.GetEnumerable<int, int, int>("Range")(start, count);
            var bindings = CreateBindings((query, source));

            var result = QxAsyncQueryRewriter.Rewrite(query.Expression, bindings);

            var invoke = Expression.Lambda<Func<IAsyncQueryable<int>>>(result).Compile();
            Assert.Equal(range(start, count).ToEnumerable(), invoke().ToEnumerable());
        }

        // TODO: Move tests into SignalR project

        ///// <summary>
        ///// Implementations of source enumerables might require cancellation.
        ///// (We're doing what the compiler does when it sees a [EnumerationCancellation] but at runtime.)
        ///// </summary>
        //[Fact]
        //public void Should_inject_synthetic_cancellation_token_argument()
        //{
        //    var capturingQueryableObject = new CapturingQueryableObject();
        //    var expectedCancellationToken = new CancellationTokenSource().Token;
        //    Expression<Func<int, CancellationToken, IAsyncQueryable<int>>> range = (count, token) => capturingQueryableObject.Count(count, token);
        //    var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
        //    var query = client.GetEnumerable<int, int>("Range")(10);
        //    var factories = new Dictionary<ParameterExpression, LambdaExpression> { { GetParam(query), range } };

        //    var result = QxAsyncQueryRewriter.Rewrite<CancellationToken, IAsyncQueryable<int>>(query.Expression, factories).Compile()(expectedCancellationToken);

        //    Assert.Equal(expectedCancellationToken, capturingQueryableObject.CapturedToken);
        //    Assert.Equal(range.Compile()(10, default).ToEnumerable(), result.ToEnumerable());
        //}

        ///// <summary>
        ///// Implementations of source enumerables might require cancellation, or might not.
        ///// We allow users of the rewriter to supply a token, but the source might not accept it.
        ///// </summary>
        //[Fact]
        //public void Should_not_inject_synthetic_cancellation_token_argument_if_there_is_no_param_in_source()
        //{
        //    Expression<Func<int, IAsyncQueryable<int>>> range = (count) => AsyncEnumerable.Range(0, count).AsAsyncQueryable();
        //    var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
        //    var query = client.GetEnumerable<int, int>("Range")(10);
        //    var factories = new Dictionary<ParameterExpression, LambdaExpression> { { GetParam(query), range } };

        //    var result = QxAsyncQueryRewriter.Rewrite<CancellationToken, IAsyncQueryable<int>>(query.Expression, factories).Compile()(default);

        //    Assert.Equal(range.Compile()(10).ToEnumerable(), result.ToEnumerable());
        //}

        private static IReadOnlyDictionary<ParameterExpression, QxAsyncQueryRewriter.InvocationFactory> CreateBindings(params (IAsyncQueryable Query, LambdaExpression Impl)[] bindings) =>
            bindings.ToDictionary<(IAsyncQueryable Query, LambdaExpression Impl), ParameterExpression, QxAsyncQueryRewriter.InvocationFactory>(
                keySelector: binding => (ParameterExpression)((InvocationExpression)binding.Query.Expression).Expression,
                elementSelector: binding => args => Expression.Invoke(binding.Impl, args));

        private class NotImplementedAsyncQueryServiceProvider : IAsyncQueryServiceProvider
        {
            public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token) =>
                throw new NotImplementedException();

            public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
                throw new NotImplementedException();
        }

        /// <summary>
        /// Artifact for <see cref="Should_inject_synthetic_cancellation_token_argument"/>.
        /// </summary>
        private class CapturingQueryableObject
        {
            public CancellationToken CapturedToken { get; private set; }

            public IAsyncQueryable<int> Count(int count, CancellationToken token)
            {
                CapturedToken = token;
                return AsyncEnumerable.Range(0, count).AsAsyncQueryable();
            }
        }
    }
}
