using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using static Qx.Security.AllowedMembersVerification;
using static Qx.Internals.ReflectionExtensions;

namespace Qx.UnitTests
{
    public class AllowedMembersVerificationTests
    {
        /// <remarks>
        /// If only a typeof(List<DateTime>) is allowed then another closing of the generic like List<string> should not be allowed.
        /// </remarks>
        [Fact]
        public void DeclaredMembersVerifier_should_allow_closed_generic_types()
        {
            var expr = Expression.Constant(null, typeof(List<string>));
            var verify = Create(CreateDeclaredMembersVerifier(typeof(List<string>)));

            var verified = verify(expr, out var errors);

            AssertAllowed(verified, errors);
        }

        /// <remarks>
        /// If typeof(List<>) is allowed and typeof(DateTime) is allowed, then typeof(List<DateTime>) should be allowed,
        /// but typeof(List<TimeSpan>) should not be allowed.
        /// </remarks>
        [Fact]
        public void DeclaredMembersVerifier_should_allow_open_generic_types_to_be_closed_with_other_types()
        {
            var allowedExpr = Expression.Constant(null, typeof(List<string>));
            var disallowedExpr = Expression.Constant(null, typeof(List<int>));
            var verify = Create(CreateDeclaredMembersVerifier(typeof(List<>), typeof(string)));

            var allowed = verify(allowedExpr, out var noErrors);
            var disallowed = verify(disallowedExpr, out var errors);

            AssertAllowed(allowed, noErrors);
            AssertDisallowed(disallowed, errors);
        }

        ///// <remarks>
        ///// If only a typeof(List<DateTime>) is allowed then another closing of the generic like List<string> should not be allowed.
        ///// </remarks>
        //[Fact]
        //public void Should_disallow_different_closings_of_generics()
        //{
        //    var expr = Expression.Constant(null, typeof(List<string>));
        //    var verify = CreateVerifier(
        //        knownMethods: DefaultKnownOperatorMethods,
        //        knownTypes: new[] { typeof(List<TestKnownType>) });

        //    var verified = verify(expr, out var errors);

        //    Assert.False(verified);
        //    Assert.Single(errors);
        //}

        //[Fact]
        //public void Should_allow_declaring_type_methods_if_type_is_known()
        //{
        //    var expr = Expression.Call(
        //        Expression.Constant(DateTime.Now),
        //        GetMethodInfo<DateTime, DateTime>(dt => dt.AddDays(1)),
        //        Expression.Constant(1d));
        //    var verify = CreateVerifier(
        //        knownMethods: DefaultKnownOperatorMethods,
        //        knownTypes: new[] { typeof(DateTime) });

        //    var verified = verify(expr, out var errors);

        //    Assert.True(verified);
        //    Assert.Null(errors);
        //}
        [Fact]
        public void Should_disallow_constants_if_type_is_unknown()
        {
            var expr = Expression.Constant("woo");
            var verify = Create();

            var verified = verify(expr, out var errors);

            AssertDisallowed(verified, errors);
        }

        [Fact]
        public void Should_allow_constants_if_type_is_known()
        {
            var expr = Expression.Constant("hoo");
            var verify = Create(m => m == typeof(string));

            var verified = verify(expr, out var errors);

            AssertAllowed(verified, errors);
        }

        [Fact]
        public void Should_disallow_methods_if_method_is_unknown()
        {
            var method = GetMethodInfo<int>(() => TestKnownStaticType.Get42());
            var expr = Expression.Call(method);
            var verify = Create();

            var verified = verify(expr, out var errors);

            AssertDisallowed(verified, errors);

        }

        [Fact]
        public void Should_allow_methods_if_method_is_known()
        {
            var method = GetMethodInfo(() => TestKnownStaticType.Get42());
            var expr = Expression.Call(method);
            var verify = Create(m => m == method);

            var verified = verify(expr, out var errors);

            AssertAllowed(verified, errors);
        }

        [Fact]
        public void Should_allow_new_arrays()
        {
            var expr = Expression.NewArrayInit(typeof(string), Expression.Constant("a"), Expression.Constant("b"));
            var verify = Create(m => m == typeof(string));

            var verified = verify(expr, out var errors);

            AssertAllowed(verified, errors);
        }

        [Fact]
        public void Should_disallow_construction_with_unknown_constructors()
        {
            var expr = Expression.New(typeof(List<string>));
            var verify = Create();

            var verified = verify(expr, out var errors);

            AssertDisallowed(verified, errors);
        }

        /// <remarks>
        /// Only one of the <see cref="MemberVerifier"/>s need to return true.
        /// </summary>
        [Fact]
        public void Should_use_any_semantics()
        {
            var expr = Expression.Constant("woo");
            var verifyOne = Create(m => m == typeof(string), m => m == typeof(int));
            var verifyNone = Create();

            var verifiedOne = verifyOne(expr, out var errorsOne);
            var verifiedNone = verifyNone(expr, out var errorsNone);

            AssertAllowed(verifiedOne, errorsOne);
            AssertDisallowed(verifiedNone, errorsNone);
        }

        [Fact]
        public void DefaultPrimitiveTypes_should_allow_string_constants()
        {
            var expr = Expression.Constant("woo");
            var verify = Create(CreateDeclaredMembersVerifier(DefaultPrimitiveTypes));

            var verified = verify(expr, out var errors);

            AssertAllowed(verified, errors);
        }

        [Fact]
        public void DefaultPrimitiveMembers_should_allow_string_methods()
        {
            var expr = Expression.NotEqual(Expression.Constant("woo"), Expression.Constant("hoo"));
            var verify = Create(CreateDeclaredMembersVerifier(DefaultPrimitiveTypes, DefaultPrimitiveMembers));

            var verified = verify(expr, out var errors);

            AssertAllowed(verified, errors);
        }

        [Fact]
        public void DefaultPrimitiveTypes_should_disallow_string_methods()
        {
            var expr = Expression.NotEqual(Expression.Constant("woo"), Expression.Constant("hoo"));
            var verify = Create(CreateDeclaredMembersVerifier(DefaultPrimitiveMembers));

            var verified = verify(expr, out var errors);

            AssertDisallowed(verified, errors);
        }

        private static class TestKnownStaticType
        {
            public static int Get42() => 42;
        }

        private class TestKnownType
        {
            public int Get42() => 42;
        }

        private static void AssertAllowed(bool verified, IEnumerable<string> errors)
        {
            Assert.True(verified);
            Assert.Null(errors);
        }

        private static void AssertDisallowed(bool verified, IEnumerable<string> errors)
        {
            Assert.False(verified);
            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
        }
    }
}
