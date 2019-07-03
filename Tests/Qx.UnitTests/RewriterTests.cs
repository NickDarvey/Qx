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
            var rangeSourceEnumerable = AsyncEnumerable.Range(0, 10);
            Expression<Func<IAsyncQueryable<int>>> range = () => rangeSourceEnumerable.AsAsyncQueryable();
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var query = client.GetEnumerable<int>("Range");
            var factories = new Dictionary<string, LambdaExpression> { { "Range", range } };

            var result = QxAsyncQueryRewriter.Rewrite<IAsyncQueryable<int>>(query.Expression, factories);

            Assert.Equal(rangeSourceEnumerable.ToEnumerable(), result.Compile()().ToEnumerable());
        }

        [Fact]
        public void Should_rewrite_multiple_parameter_expressions_to_invocations()
        {
            var range1SourceEnumerable = AsyncEnumerable.Range(0, 10);
            var range2SourceEnumerable = AsyncEnumerable.Range(10, 10);
            Expression<Func<IAsyncQueryable<int>>> range1 = () => range1SourceEnumerable.AsAsyncQueryable();
            Expression<Func<IAsyncQueryable<int>>> range2 = () => range2SourceEnumerable.AsAsyncQueryable();
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var range1SourceQueryable = client.GetEnumerable<int>("Range1");
            var range2SourceQueryable = client.GetEnumerable<int>("Range2");
            var factories = new Dictionary<string, LambdaExpression> { { "Range1", range1 }, { "Range2", range2 } };

            var query = range1SourceQueryable.Join(range2SourceQueryable, x => x, y => y, (x, y) => x + y);
            var expected = range1SourceEnumerable.Join(range2SourceEnumerable, x => x, y => y, (x, y) => x + y);

            var result = QxAsyncQueryRewriter.Rewrite<IAsyncQueryable<int>>(query.Expression, factories);

            Assert.Equal(expected.ToEnumerable(), result.Compile()().ToEnumerable());
        }


        [Fact]
        public void Should_rewrite_parameter_expressions_with_arguments_to_invocations()
        {
            var rangeCount = 10;
            Expression<Func<int, IAsyncQueryable<int>>> range = (count) => AsyncEnumerable.Range(0, count).AsAsyncQueryable();
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var query = client.GetEnumerable<int, int>("Range")(rangeCount);
            var factories = new Dictionary<string, LambdaExpression> { { "Range", range } };

            var result = QxAsyncQueryRewriter.Rewrite<IAsyncQueryable<int>>(query.Expression, factories);

            Assert.Equal(Enumerable.Range(0, rangeCount), result.Compile()().ToEnumerable());

        }


        /// <summary>
        /// Implementations of source enumerables might require cancellation.
        /// (We're doing what the compiler does when it sees a [EnumerationCancellation] but at runtime.)
        /// </summary>
        [Fact]
        public void Should_inject_synthetic_cancellation_token_argument()
        {
            var capturingQueryableObject = new CapturingQueryableObject();
            var expectedCancellationToken = new CancellationTokenSource().Token;
            Expression<Func<int, CancellationToken, IAsyncQueryable<int>>> range = (count, token) => capturingQueryableObject.Count(count, token);
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var query = client.GetEnumerable<int, int>("Range")(10);
            var factories = new Dictionary<string, LambdaExpression> { { "Range", range } };

            var result = QxAsyncQueryRewriter.Rewrite<CancellationToken, IAsyncQueryable<int>>(query.Expression, factories).Compile()(expectedCancellationToken);

            Assert.Equal(expectedCancellationToken, capturingQueryableObject.CapturedToken);
            Assert.Equal(range.Compile()(10, default).ToEnumerable(), result.ToEnumerable());
        }

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
