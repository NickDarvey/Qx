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

            var result = Rewriters.BindingRewriter(query.Expression, bindings);

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

            var result = Rewriters.BindingRewriter(query.Expression, bindings);

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

            var result = Rewriters.BindingRewriter(query.Expression, bindings);

            var invoke = Expression.Lambda<Func<IAsyncQueryable<int>>>(result).Compile();
            Assert.Equal(range(start, count).ToEnumerable(), invoke().ToEnumerable());
        }

        private static IReadOnlyDictionary<ParameterExpression, Rewriters.InvocationFactory> CreateBindings(params (IAsyncQueryable Query, LambdaExpression Impl)[] bindings) =>
            bindings.ToDictionary<(IAsyncQueryable Query, LambdaExpression Impl), ParameterExpression, Rewriters.InvocationFactory>(
                keySelector: binding => (ParameterExpression)((InvocationExpression)binding.Query.Expression).Expression,
                elementSelector: binding => args => Expression.Invoke(binding.Impl, args));

        private class NotImplementedAsyncQueryServiceProvider : IAsyncQueryServiceProvider
        {
            public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token) =>
                throw new NotImplementedException();

            public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
                throw new NotImplementedException();
        }
    }
}
