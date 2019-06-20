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
        private class NotImplementedAsyncQueryServiceProvider : IAsyncQueryServiceProvider
        {
            public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token) =>
                throw new NotImplementedException();

            public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
                throw new NotImplementedException();
        }

        [Fact]
        public void Should_rewrite_simple_parameter_expression_to_invocation()
        {
            Expression<Func<IAsyncQueryable<int>>> range = () => AsyncEnumerable.Range(0, 50).AsAsyncQueryable();
            var factories = new Dictionary<string, LambdaExpression> { { "Range", range } };
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var query = client.GetEnumerable<int>("Range");

            var result = new QxAsyncQueryRewriter(factories).Visit(query.Expression);

            Assert.Equal(ExpressionType.Invoke, result.NodeType);
            Assert.Equal(range, ((InvocationExpression)result).Expression);
        }

        [Fact]
        public void Should_rewrite_multiple_parameter_expressions_to_invocations()
        {
            Expression<Func<IAsyncQueryable<int>>> range1 = () => AsyncEnumerable.Range(0, 50).AsAsyncQueryable();
            Expression<Func<IAsyncQueryable<int>>> range2 = () => AsyncEnumerable.Range(50, 50).AsAsyncQueryable();
            var factories = new Dictionary<string, LambdaExpression> { { "Range1", range1 }, { "Range2", range2 } };
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var source1 = client.GetEnumerable<int>("Range1");
            var source2 = client.GetEnumerable<int>("Range2");
            var query = source1.Join(source2, x => x, y => y, (x, y) => x + y);

            var result = new QxAsyncQueryRewriter(factories).Visit(query.Expression);

            Assert.Equal(ExpressionType.Call, result.NodeType);
            var joinExpression = (MethodCallExpression)result;
            var joinExpressionArg1 = joinExpression.Arguments.First();
            var joinExpressionArg2 = joinExpression.Arguments.Skip(1).First();

            Assert.Equal(ExpressionType.Invoke, joinExpressionArg1.NodeType);
            Assert.Equal(range1, ((InvocationExpression)joinExpressionArg1).Expression);

            Assert.Equal(ExpressionType.Invoke, joinExpressionArg2.NodeType);
            Assert.Equal(range2, ((InvocationExpression)joinExpressionArg2).Expression);
        }


        [Fact]
        public void Should_rewrite_parameter_expressions_with_arguments_to_invocations()
        {
            Expression<Func<int, IAsyncQueryable<int>>> range = (count) => AsyncEnumerable.Range(0, count).AsAsyncQueryable();
            var factories = new Dictionary<string, LambdaExpression> { { "Range", range } };
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var query = client.GetEnumerable<int, int>("Range")(10);



            var result = new QxAsyncQueryRewriter(factories).Visit(query.Expression);

            Assert.Equal(ExpressionType.Invoke, result.NodeType);
            Assert.Equal(range, ((InvocationExpression)result).Expression);
        }

        [Fact]
        public async Task Should_evaluate() // TODO: Implement this
        {
            Expression<Func<int, IAsyncQueryable<int>>> range = (count) => AsyncEnumerable.Range(0, count).AsAsyncQueryable();
            var factories = new Dictionary<string, LambdaExpression> { { "Range", range } };
            var client = new QxAsyncQueryClient(new NotImplementedAsyncQueryServiceProvider());
            var source = client.GetEnumerable<int, int>("Range")(10);
            var query = source.Join(source, x => x, y => y, (x, y) => x + y);

            var result = await Expression.Lambda<Func<IAsyncQueryable<int>>>(new QxAsyncQueryRewriter(factories).Visit(query.Expression)).Compile()().ToArrayAsync();

            Assert.Equal(new[] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18 }, result);
        }

    }
}
