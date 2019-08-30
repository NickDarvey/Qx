using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Qx.Client.UnitTests
{
    // TODO: Consider implementing these tests in a way that just asserts the expression can be built and compiled on the other side.
    public class QxAsyncQueryClientTests
    {
        /// <summary>
        /// Tests to ensure <see cref="QxAsyncQueryClient.GetEnumerable{TArg, TElement}(string)"/> calls result in an unbound parameter added to the expression tree.
        /// </summary>
        /// <remarks>
        /// This ensures query contract is not broken.
        /// </remarks>
        [Fact]
        public void GetEnumerable1_should_refer_to_enumerables_as_unbound_parameters()
        {
            var serviceProvider = new CapturingAsyncQueryServiceProvider();
            var client = new QxAsyncQueryClient(serviceProvider: serviceProvider);

            _ = client.GetEnumerable<int>("Test").GetAsyncEnumerator();

            AssertIsKnownResourceInvocation(
                actualExpression: serviceProvider.CapturedExpression,
                expectedKnownResourceType: typeof(Func<IAsyncQueryable<int>>),
                expectedKnownResourceName: "Test");
        }

        /// <summary>
        /// Tests to ensure <see cref="QxAsyncQueryClient.GetEnumerable{TArg1, TArg2, TElement}(string)"/> calls result in an unbound parameter added to the expression tree.
        /// </summary>
        /// <remarks>
        /// This ensures query contract is not broken.
        /// </remarks>
        [Fact]
        public void GetEnumerable2_should_refer_to_enumerables_as_unbound_parameters()
        {
            var serviceProvider = new CapturingAsyncQueryServiceProvider();
            var client = new QxAsyncQueryClient(serviceProvider: serviceProvider);

            _ = client.GetEnumerable<string, int>("Test")("1").GetAsyncEnumerator();

            AssertIsKnownResourceInvocation(
                actualExpression: serviceProvider.CapturedExpression,
                expectedKnownResourceType: typeof(Func<string, IAsyncQueryable<int>>),
                expectedKnownResourceName: "Test",
                expectedArguments: (typeof(string), "1"));
        }

        /// <summary>
        /// Tests to ensure <see cref="QxAsyncQueryClient.GetEnumerable{TArg1, TArg2, TArg3, TElement}(string)"/> calls result in an unbound parameter added to the expression tree.
        /// </summary>
        /// <remarks>
        /// This ensures query contract is not broken.
        /// </remarks>
        [Fact]
        public void GetEnumerable3_should_refer_to_enumerables_as_unbound_parameters()
        {
            var serviceProvider = new CapturingAsyncQueryServiceProvider();
            var client = new QxAsyncQueryClient(serviceProvider: serviceProvider);

            _ = client.GetEnumerable<string, bool, int>("Test")("1", false).GetAsyncEnumerator();

            AssertIsKnownResourceInvocation(
                actualExpression: serviceProvider.CapturedExpression,
                expectedKnownResourceType: typeof(Func<string, bool, IAsyncQueryable<int>>),
                expectedKnownResourceName: "Test",
                /* expectedArguments: */ (typeof(string), "1"), (typeof(bool), false));
        }

        [Fact]
        public void GetEnumerable1_should_replace_closed_over_GetEnumerable1_calls_with_unbound_parameters()
        {
            var serviceProvider = new CapturingAsyncQueryServiceProvider();
            var client = new QxAsyncQueryClient(serviceProvider: serviceProvider);

            _ = client.GetEnumerable<int>("Test1").SelectMany(l => client.GetEnumerable<int>("Test2").Select(r => l + r)).GetAsyncEnumerator();

            Assert.Equal(ExpressionType.Call, serviceProvider.CapturedExpression.NodeType);
            var methodCallExpression = (MethodCallExpression)serviceProvider.CapturedExpression;
            var test1Expression = methodCallExpression.Arguments[0];
            var test2Expression = ((MethodCallExpression)((LambdaExpression)((UnaryExpression)methodCallExpression.Arguments[1]).Operand).Body).Arguments[0];

            AssertIsKnownResourceInvocation(
                actualExpression: test1Expression,
                expectedKnownResourceType: typeof(Func<IAsyncQueryable<int>>),
                expectedKnownResourceName: "Test1");

            AssertIsKnownResourceInvocation(
                actualExpression: test2Expression,
                expectedKnownResourceType: typeof(Func<IAsyncQueryable<int>>),
                expectedKnownResourceName: "Test2");
        }

        [Fact]
        public void GetEnumerable2_should_replace_closed_over_GetEnumerable2_calls_with_unbound_parameters()
        {
            var serviceProvider = new CapturingAsyncQueryServiceProvider();
            var client = new QxAsyncQueryClient(serviceProvider: serviceProvider);

            _ = client.GetEnumerable<string, int>("Test1")("A").SelectMany(l => client.GetEnumerable<string, int>("Test2")("B").Select(r => l + r)).GetAsyncEnumerator();

            Assert.Equal(ExpressionType.Call, serviceProvider.CapturedExpression.NodeType);
            var methodCallExpression = (MethodCallExpression)serviceProvider.CapturedExpression;
            var test1Expression = methodCallExpression.Arguments[0];
            var test2Expression = ((MethodCallExpression)((LambdaExpression)((UnaryExpression)methodCallExpression.Arguments[1]).Operand).Body).Arguments[0];

            AssertIsKnownResourceInvocation(
                actualExpression: test1Expression,
                expectedKnownResourceType: typeof(Func<string, IAsyncQueryable<int>>),
                expectedKnownResourceName: "Test1",
                expectedArguments: (typeof(string), "A"));

            AssertIsKnownResourceInvocation(
                actualExpression: test2Expression,
                expectedKnownResourceType: typeof(Func<string, IAsyncQueryable<int>>),
                expectedKnownResourceName: "Test2",
                expectedArguments: (typeof(string), "B"));
        }

        [Fact(Skip = "Not implemented")]
        // TODO: Support inlining of expressions so this passes
        // e.g. https://referencesource.microsoft.com/#system.data.linq/SqlClient/Query/Funcletizer.cs,ccef8437bc51e04e
        // e.g. https://github.com/aspnet/EntityFrameworkCore/blob/98f41f7cc483d1688c23017fbc495a709f308cfb/src/EFCore/Query/ExpressionVisitors/Internal/ParameterExtractingExpressionVisitor.cs#L101
        // e.g. https://github.com/RxDave/Qactive/blob/6cd5a058082562128d51c50e3ac8bd393ea6015e/Source/Qactive/LocalEvaluationVisitor.cs
        public void Should_inline_etcetera()
        {
            var serviceProvider = new CapturingAsyncQueryServiceProvider();
            var client = new QxAsyncQueryClient(serviceProvider: serviceProvider);

            _ = client.GetEnumerable<string, int>("Test1")("A").SelectMany(l => GetQueryableFromLocalMethod(client, "Test2", "B").Select(r => l + r)).GetAsyncEnumerator();

        }

        private static void AssertIsKnownResourceInvocation(
            Expression actualExpression,
            Type expectedKnownResourceType,
            string expectedKnownResourceName,
            params (Type Type, object Value)[] expectedArguments)
        {
            Assert.Equal(ExpressionType.Invoke, actualExpression.NodeType);
            Assert.Equal(GetDelegateReturnType(expectedKnownResourceType), actualExpression.Type);
            var invocationExpression = (InvocationExpression)actualExpression;
            Assert.Equal(ExpressionType.Parameter, invocationExpression.Expression.NodeType);
            Assert.Equal(expectedKnownResourceType, invocationExpression.Expression.Type);
            Assert.Equal(expectedKnownResourceName, ((ParameterExpression)invocationExpression.Expression).Name);
            Assert.Equal(expectedArguments.Length, invocationExpression.Arguments.Count);

            for (int i = 0; i < expectedArguments.Length; i++)
            {
                Assert.Equal(expectedArguments[i].Type, invocationExpression.Arguments[i].Type);
                Assert.Equal(ExpressionType.Constant, invocationExpression.Arguments[i].NodeType);
                Assert.Equal(expectedArguments[i].Value, ((ConstantExpression)invocationExpression.Arguments[i]).Value);
            }
        }

        private class CapturingAsyncQueryServiceProvider : IAsyncQueryServiceProvider
        {
            public Expression CapturedExpression { get; private set; }

            public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token)
            {
                CapturedExpression = expression;
                return new NotImplementedAsyncEnumerator<T>();
            }

            public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
                throw new NotImplementedException();

            private class NotImplementedAsyncEnumerator<T> : IAsyncEnumerator<T>
            {
                public T Current => throw new NotImplementedException();
                public ValueTask DisposeAsync() => throw new NotImplementedException();
                public ValueTask<bool> MoveNextAsync() => throw new NotImplementedException();
            }
        }

        // TODO: Extract
        private static Type GetDelegateReturnType(Type type) =>
            typeof(Delegate).IsAssignableFrom(type) == false ? throw new ArgumentException($"Type {type} is not a delegate type", nameof(type))
            : type.GetMethod("Invoke").ReturnType;

        private static IAsyncQueryable<int> GetQueryableFromLocalMethod(IAsyncQueryClient client, string name, string arg) =>
            client.GetEnumerable<string, int>(name)(arg);
    }
}
