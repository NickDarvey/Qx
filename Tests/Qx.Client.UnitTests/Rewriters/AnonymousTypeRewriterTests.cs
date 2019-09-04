using System;
using System.Linq.Expressions;
using Xunit;
using static Qx.Client.Rewriters.AnonymousTypeRewriter;
using static Qx.Helpers.ExpressionHelpers;

namespace Qx.Client.UnitTests.Rewriters
{
    public class AnonymousTypeRewriterTests
    {
        [Fact]
        public void Should_rewrite_new()
        {
            var expr = Expr(() => new { Name = "Cat" });
            var expected = Expr(() => new Tuple<string>("Cat"));

            var result = Rewrite(expr);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_new_with_args()
        {
            var expr = Expr((string name) => new { Name = name });
            var expected = Expr((string name) => new Tuple<string>(name) );

            var result = Rewrite(expr);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_constant()
        {
            var expr = Expression.Constant(new { Name = "Cat" });
            var expected = Expression.Constant(new Tuple<string>("Cat"));

            var result = Rewrite(expr);

            Assert.Equal(expected.ToString(), result.ToString());
        }
    }
}
