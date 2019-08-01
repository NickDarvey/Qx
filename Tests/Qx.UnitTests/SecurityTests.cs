using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;
using static Qx.Security;

namespace Qx.UnitTests
{
    public class SecurityTests
    {
        /// <remarks>
        /// If only a typeof(List<DateTime>) is allowed then another closing of the generic like List<string> should not be allowed.
        /// </remarks>
        [Fact]
        public void Should_allow_closed_generic_type()
        {
            var expr = Expression.Constant(null, typeof(List<string>));
            var verify = CreateVerifier(
                knownMethods: DefaultKnownMethods,
                knownTypes: new[] { typeof(List<string>) },
                knownExtendedPrimitiveTypes: DefaultKnownExtendedPrimitiveTypes);

            var verified = verify(expr, out var errors);

            Assert.True(verified);
            Assert.Null(errors);
        }

        /// <remarks>
        /// If typeof(List<>) is allowed and typeof(DateTime) is allowed, then typeof(List<DateTime>) should be allowed.
        /// </remarks>
        [Fact]
        public void Should_allow_known_open_generics_to_be_closed_with_other_known_types()
        {
            var expr = Expression.Constant(null, typeof(List<TestKnownType>));
            var verify = CreateVerifier(
                knownMethods: DefaultKnownMethods,
                knownTypes: new[] { typeof(List<>), typeof(TestKnownType) },
                knownExtendedPrimitiveTypes: DefaultKnownExtendedPrimitiveTypes);

            var verified = verify(expr, out var errors);

            Assert.True(verified);
            Assert.Null(errors);
        }

        /// <remarks>
        /// If only a typeof(List<DateTime>) is allowed then another closing of the generic like List<string> should not be allowed.
        /// </remarks>
        [Fact]
        public void Should_disallow_different_closings_of_generics()
        {
            var expr = Expression.Constant(null, typeof(List<string>));
            var verify = CreateVerifier(
                knownMethods: DefaultKnownMethods,
                knownTypes: new[] { typeof(List<TestKnownType>) },
                knownExtendedPrimitiveTypes: DefaultKnownExtendedPrimitiveTypes);

            var verified = verify(expr, out var errors);

            Assert.False(verified);
            Assert.Single(errors);
        }

        private class TestKnownType { }
    }
}
