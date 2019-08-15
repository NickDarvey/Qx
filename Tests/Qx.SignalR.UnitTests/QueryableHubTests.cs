using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Qx.SignalR.Queries;

namespace Qx.SignalR.UnitTests
{
    public class QueryableHubTests
    {
        [Fact]
        public async Task Should_pass_cancellation_token_to_sources()
        {
            var capturingQueryableObject = new CapturingQueryableObject();
            var expectedCancellationToken = new CancellationTokenSource().Token;
            Func<int, CancellationToken, int> echo = capturingQueryableObject.Echo;
            var query = Expression.Invoke(
                Expression.Parameter(typeof(Func<int, int>), "Echo"),
                Expression.Constant(42));

            var invoke = await CompileQuery<QueryableSourceDescription, int>(
                expression: query,
                authorizer: _ => Task.FromResult(true),
                bindings: new Dictionary<string, QueryableSourceDescription> { { "Echo", new QueryableSourceDescription(echo.Target, echo.Method) } },
                boxingRewriter: expr => expr);
            var result = invoke(expectedCancellationToken);

            Assert.Equal(42, result);
            Assert.Equal(expectedCancellationToken, capturingQueryableObject.CapturedToken);
        }

        /// <summary>
        /// Artifact for <see cref="Should_pass_cancellation_token_to_sources"/>.
        /// </summary>
        private class CapturingQueryableObject
        {
            public CancellationToken CapturedToken { get; private set; }

            public int Echo(int count, CancellationToken token)
            {
                CapturedToken = token;
                return count;
            }
        }

        private class QueryableSourceDescription : IQueryableSourceDescription
        {
            public QueryableSourceDescription(object instance, MethodInfo method)
            {
                Instance = instance;
                Method = method;
            }

            public MethodInfo Method { get; }

            public object Instance { get; }
        }
    }
}
