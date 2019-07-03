using System;
using System.Linq.Expressions;
using Xunit;

namespace Qx.UnitTests
{
    public class ScannerTests
    {
        [Fact]
        public void Should_exclude_bound_parameters()
        {
            Expression<Func<int, int, int>> expr = (x, y) => x + y;

            var parameters = QxAsyncQueryScanner.FindUnboundParameters(expr);

            Assert.Empty(parameters);
        }

        [Fact]
        public void Should_include_unbound_parameters()
        {
            var param1 = Expression.Parameter(typeof(int));
            var expr =Expression.Lambda(
                Expression.Add(
                    left: Expression.Parameter(typeof(int), "unbound"),
                    right: param1),
                param1);

            var parameters = QxAsyncQueryScanner.FindUnboundParameters(expr);

            Assert.Single(parameters, p => p.Name == "unbound");
        }
    }
}
