using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Qx.SignalR.UnitTests
{
    public class SignalRQxAsyncQueryRewriterTests
    {
        [Theory]
        [InlineData(typeof(Func<ValueTask<int>>), typeof(Task<object>))]
        [InlineData(typeof(Func<Task<int>>), typeof(Task<object>))]
        public void RewriteSingleResultsType_should_convert_return_type(Type sourceType, Type expectedReturnType)
        {
            var expression = Expression.Invoke(
                Expression.Parameter(sourceType));

            var result = SignalRRewriters.RewriteSingleResultsType(expression);

            Assert.Equal(expectedReturnType, result.Type);
        }

        [Theory]
        [InlineData(typeof(Func<Task<IAsyncQueryable<int>>>), typeof(Task<IAsyncQueryable<object>>))]
        [InlineData(typeof(Func<IAsyncQueryable<int>>), typeof(IAsyncQueryable<object>))]
        public void RewriteManyResultsType_should_convert_return_type(Type sourceType, Type expectedReturnType)
        {
            var expression = Expression.Invoke(
                Expression.Parameter(sourceType));

            var result = SignalRRewriters.RewriteManyResultsType(expression);

            Assert.Equal(expectedReturnType, result.Type);
        }
    }
}
