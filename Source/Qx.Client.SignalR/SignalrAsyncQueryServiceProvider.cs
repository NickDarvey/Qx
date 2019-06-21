using Microsoft.AspNetCore.SignalR.Client;
using Serialize.Linq.Factories;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Qx
{
    public class SignalRAsyncQueryServiceProvider : IAsyncQueryServiceProvider
    {
        private readonly NodeFactory _factory = new NodeFactory(); // Creates ExpressionNodes, a serializable expression tree.
        private readonly HubConnection _connection;

        public SignalRAsyncQueryServiceProvider(HubConnection connection)
        {
            _connection = connection;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token) =>
            _connection.StreamAsync<T>("qx`n", _factory.Create(expression), token).GetAsyncEnumerator(/*cancel?*/);

        public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
            new ValueTask<T>(_connection.InvokeAsync<T>("qx`1", _factory.Create(expression), token));
    }
}
