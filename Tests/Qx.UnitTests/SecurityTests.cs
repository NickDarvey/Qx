using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
                knownTypes: new[] { typeof(List<string>) });

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
                knownTypes: new[] { typeof(List<>), typeof(TestKnownType) });

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
                knownTypes: new[] { typeof(List<TestKnownType>) });

            var verified = verify(expr, out var errors);

            Assert.False(verified);
            Assert.Single(errors);
        }

        [Fact]
        public void Should_allow_declaring_type_methods_if_type_is_known()
        {
            var expr = Expression.Call(
                Expression.Constant(DateTime.Now),
                GetMethodInfo<DateTime, DateTime>(dt => dt.AddDays(1)),
                Expression.Constant(1d));
            var verify = CreateVerifier(
                knownMethods: DefaultKnownMethods,
                knownTypes: new[] { typeof(DateTime) });

            var verified = verify(expr, out var errors);

            Assert.True(verified);
            Assert.Null(errors);
        }

        [Fact]
        public void Should_allow_methods_if_method_is_known()
        {
            var addDays = GetMethodInfo<DateTime, DateTime>(dt => dt.AddDays(1));
            var expr = Expression.Call(
                Expression.Constant(DateTime.Now),
                addDays,
                Expression.Constant(1d));
            var verify = CreateVerifier(
                knownMethods: new[] { addDays },
                knownTypes: Enumerable.Empty<Type>());

            var verified = verify(expr, out var errors);

            Assert.True(verified);
            Assert.Null(errors);
        }

        private class TestKnownType
        {
            public int Get42() => 42;
        }

        private static MethodInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> expression) =>
            expression.Body is MethodCallExpression methodCallExpression ? methodCallExpression.Method
            : throw new NotImplementedException();
    }
}
