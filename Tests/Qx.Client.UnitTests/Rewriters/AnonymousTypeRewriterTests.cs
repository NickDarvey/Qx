using System;
using Xunit;
using static Qx.Client.Rewriters.AnonymousTypeRewriter;
using static Qx.Helpers.ExpressionHelpers;

namespace Qx.Client.UnitTests.Rewriters
{
    public class AnonymousTypeRewriterTests
    {
        [Fact]
        public void Should_rewrite_constants()
        {
            var expr = Expr(() => new { Name = "Cat" });

            var result = Rewrite(expr);
        }

        [Fact]
        public void Should_rewrite_new()
        {
            var expr = Expr((string name) => new { Name = name });
            var expected = Expr((string name) => new Tuple<string>(name) );

            var result = Rewrite(expr);

            Assert.Equal(expected.ToString(), result.ToString());
        }
    }
}
