using Qx.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using static Qx.Client.Rewriters.AnonymousTypeRewriter;
using static Qx.Helpers.ExpressionHelpers;
using static Qx.Internals.ReflectionExtensions;

namespace Qx.Client.UnitTests.Rewriters
{
    public class AnonymousTypeRewriterTests
    {
        [Fact]
        public void Should_rewrite_new()
        {
            var original = Expr(() => new { Name = "Cat" });
            var expected = Expr(() => new Tuple<string>("Cat"));

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_new_with_args()
        {
            var original = Expr((string name) => new { Name = name });
            var expected = Expr((string name) => new Tuple<string>(name));

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_constant()
        {
            var original = Expression.Constant(new { Name = "Cat" });
            var expected = Expression.Constant(new Tuple<string>("Cat"));

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_constant_with_nested_type()
        {
            var original = Expression.Constant(new { Name = "Cat", Place = new { Location = "House", Room = "Unknown" } });
            var expected = Expression.Constant(new Tuple<string, Tuple<string, string>>("Cat", new Tuple<string, string>("House", "Unknown")));

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_generic_call()
        {
            var method = GetMethodInfo((int arg) => TestClass.Echo(arg)).GetGenericMethodDefinition();
            var originalConstant = Expression.Constant(new { Name = "Cat" });
            var originalMethod = method.MakeGenericMethod(originalConstant.Type);
            var original = Expression.Call(originalMethod, originalConstant);
            var expectedConstant = Expression.Constant(new Tuple<string>("Cat"));
            var expectedMethod = method.MakeGenericMethod(expectedConstant.Type);
            var expected = Expression.Call(expectedMethod, expectedConstant);

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_generic_call_with_generic_lambda()
        {
            var method = GetMethodInfo((int arg) => TestClass.Select(arg, a => a)).GetGenericMethodDefinition();
            var constant = Expression.Constant("Cat");
            var originalLambda = Expr((string name) => new { Name = name });
            var originalMethod = method.MakeGenericMethod(originalLambda.Type.GetGenericArguments());
            var original = Expression.Call(originalMethod, constant, originalLambda);
            var expectedLambda = Expr((string name) => new Tuple<string>(name));
            var expectedMethod = method.MakeGenericMethod(expectedLambda.Type.GetGenericArguments());
            var expected = Expression.Call(expectedMethod, constant, expectedLambda);

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
        }

        [Fact]
        public void Should_rewrite_lambda_with_anonymous_type_parameter()
        {
            var originalValue = new { Name = "Cat" };
            var originalConstant = Expression.Constant(originalValue);
            var originalLambdaParameter = Expression.Parameter(originalConstant.Type);
            var original = Expression.Lambda(Expression.MakeMemberAccess(originalLambdaParameter, GetMemberInfo(() => originalValue.Name)), originalLambdaParameter);
            var expectedValue = new Tuple<string>("Cat");
            var expectedConstant = Expression.Constant(expectedValue);
            var expectedLambdaParameter = Expression.Parameter(expectedConstant.Type);
            var expected = Expression.Lambda(Expression.MakeMemberAccess(expectedLambdaParameter, GetMemberInfo(() => expectedValue.Item1)), expectedLambdaParameter);

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
            Assert.Equal(expected.Compile().DynamicInvoke(expectedValue), ((LambdaExpression)result).Compile().DynamicInvoke(expectedValue));
        }

        [Fact]
        public void Should_rewrite_lambda_with_nested_anonymous_type_parameter()
        {
            var original = Expr(() => TestClass.Select(new { Name = "Cat", Place = new { Location = "House", Room = "Unknown" } }, v => v.Place.Room));
            var expected = Expr(() => TestClass.Select(new Tuple<string, Tuple<string, string>>("Cat", new Tuple<string, string>("House", "Unknown")), v => v.Item2.Item2));

            var result = Rewrite(original);

            Assert.Equal(expected.ToString(), result.ToString());
            Assert.Equal(expected.Compile().DynamicInvoke(), ((LambdaExpression)result).Compile().DynamicInvoke());

        }

        private static class TestClass
        {
            public static T Echo<T>(T whatever) => whatever;
            public static R Select<T, R>(T value, Func<T, R> select) => select(value);
        }

        private static class TestClass<T>
        {
            public static T Echo(T whatever) => whatever;
        }
    }
}
