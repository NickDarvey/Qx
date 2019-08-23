using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;
using static Qx.Internals.Prelude;
using static Qx.Internals.ReflectionExtensions;
using static Qx.Security.FeaturesVerification;
using static Qx.Security.MembersVerification;

namespace Qx.UnitTests
{
    public class MembersVerificationTests
    {
        /// <remarks>
        /// If only a typeof(List<DateTime>) is allowed then another closing of the generic like List<string> should not be allowed.
        /// </remarks>
        [Fact]
        public void DeclaredMembersVerifier_should_verify_closed_generic_types()
        {
            var allowedExpr = Expression.Constant(null, typeof(List<string>));
            var disallowedExpr = Expression.Constant(null, typeof(List<int>));
            var verify = Create(CreateDeclaredMembersVerifier(typeof(List<string>)));

            var verified = verify(allowedExpr);
            var refuted = verify(disallowedExpr);

            AssertVerified(verified);
            AssertRefuted(refuted);
        }

        /// <remarks>
        /// If typeof(List<>) is allowed and typeof(DateTime) is allowed, then typeof(List<DateTime>) should be allowed,
        /// but typeof(List<TimeSpan>) should not be allowed.
        /// </remarks>
        [Fact]
        public void DeclaredMembersVerifier_should_verify_open_generic_types_to_be_closed_with_other_types()
        {
            var allowedExpr = Expression.Constant(null, typeof(List<string>));
            var disallowedExpr = Expression.Constant(null, typeof(List<int>));
            var verify = Create(CreateDeclaredMembersVerifier(typeof(List<>), typeof(string)));

            var verified = verify(allowedExpr);
            var refuted = verify(disallowedExpr);

            AssertVerified(verified);
            AssertRefuted(refuted);
        }

        /// <remarks>
        /// If only a Class.Method<string> is allowed then another closing of the generic like Class.Method<int> should not be allowed.
        /// </remarks>
        [Fact]
        public void DeclaredMembersVerifier_should_verify_closed_generic_methods()
        {
            var allowedMethod = GetMethodInfo(() => TestKnownStaticType.GetTypeNameOf<string>());
            var disallowedMethod = GetMethodInfo(() => TestKnownStaticType.GetTypeNameOf<int>());
            var allowedExpr = Expression.Call(allowedMethod);
            var disallowedExpr = Expression.Call(disallowedMethod);
            var verify = Create(CreateDeclaredMembersVerifier(allowedMethod));

            var verified = verify(allowedExpr);
            var refuted = verify(disallowedExpr);

            AssertVerified(verified);
            AssertRefuted(refuted);
        }

        /// <remarks>
        /// If only a Class.Method<T> is allowed then any closing is allowed.
        /// </remarks>
        [Fact]
        public void DeclaredMembersVerifier_should_verify_open_generic_methods_to_be_closed_with_any_types()
        {
            var genericMethod = typeof(TestKnownStaticType).GetMethod(nameof(TestKnownStaticType.GetTypeNameOf));
            var allowedExpr1 = Expression.Call(GetMethodInfo(() => TestKnownStaticType.GetTypeNameOf<string>()));
            var allowedExpr2 = Expression.Call(GetMethodInfo(() => TestKnownStaticType.GetTypeNameOf<int>()));
            var verify = Create(CreateDeclaredMembersVerifier(genericMethod));

            var verified1 = verify(allowedExpr1);
            var verified2 = verify(allowedExpr2);

            AssertVerified(verified1);
            AssertVerified(verified2);
        }


        [Fact]
        public void Should_refute_constants_if_type_is_unknown()
        {
            var expr = Expression.Constant("woo");
            var verify = Create();

            var refuted = verify(expr);

            AssertRefuted(refuted);
        }

        [Fact]
        public void Should_verify_constants_if_type_is_known()
        {
            var expr = Expression.Constant("hoo");
            var verify = Create(m => m == typeof(string));

            var verified = verify(expr);

            AssertVerified(verified);
        }

        [Fact]
        public void Should_refute_methods_if_method_is_unknown()
        {
            var method = GetMethodInfo<int>(() => TestKnownStaticType.Get42());
            var expr = Expression.Call(method);
            var verify = Create();

            var refuted = verify(expr);

            AssertRefuted(refuted);

        }

        [Fact]
        public void Should_verify_methods_if_method_is_known()
        {
            var method = GetMethodInfo(() => TestKnownStaticType.Get42());
            var expr = Expression.Call(method);
            var verify = Create(m => m == method);

            var verified = verify(expr);

            AssertVerified(verified);
        }

        [Fact]
        public void Should_verify_new_arrays()
        {
            var expr = Expression.NewArrayInit(typeof(string), Expression.Constant("a"), Expression.Constant("b"));
            var verify = Create(m => m == typeof(string));

            var verified = verify(expr);

            AssertVerified(verified);
        }

        [Fact]
        public void Should_refute_construction_with_unknown_constructors()
        {
            var expr = Expression.New(typeof(List<string>));
            var verify = Create();

            var refuted = verify(expr);

            AssertRefuted(refuted);
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

            var verified = verifyOne(expr);
            var refuted = verifyNone(expr);

            AssertVerified(verified);
            AssertRefuted(refuted);
        }

        [Fact]
        public void DefaultPrimitiveTypes_should_verify_string_constants()
        {
            var expr = Expression.Constant("woo");
            var verify = Create(CreateDeclaredMembersVerifier(PrimitiveTypes));

            var verified = verify(expr);

            AssertVerified(verified);
        }

        [Fact]
        public void DefaultPrimitiveMembers_should_verify_string_methods()
        {
            var expr = Expression.NotEqual(Expression.Constant("woo"), Expression.Constant("hoo"));
            var verify = Create(CreateDeclaredMembersVerifier(PrimitiveTypes, PrimitiveMembers));

            var verified = verify(expr);

            AssertVerified(verified);
        }

        [Fact]
        public void DefaultPrimitiveTypes_should_refute_string_methods()
        {
            var expr = Expression.NotEqual(Expression.Constant("woo"), Expression.Constant("hoo"));
            var verify = Create(CreateDeclaredMembersVerifier(PrimitiveMembers));

            var refuted = verify(expr);

            AssertRefuted(refuted);
        }

        private static class TestKnownStaticType
        {
            public static int Get42() => 42;
            public static string GetTypeNameOf<T>() => typeof(T).Name;
        }

        private class TestKnownType
        {
            public int Get42() => 42;
        }

        private static void AssertVerified(Validation<string, Unit> verified) =>
            verified.Match(
                Valid: _ => Assert.True(true),
                Invalid: errors => Assert.True(false, string.Join(Environment.NewLine, errors)));

        private static void AssertRefuted(Validation<string, Unit> verified) =>
            verified.Match(
                Valid: _ => Assert.True(false, $"{nameof(Validation<string, Unit>)} was in a valid state"),
                Invalid: errors => Assert.True(true));
    }
}
