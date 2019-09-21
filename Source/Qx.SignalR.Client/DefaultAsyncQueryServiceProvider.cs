using Microsoft.AspNetCore.SignalR.Client;
using Remote.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Qx.Client.SignalR
{
    public class DefaultAsyncQueryServiceProvider : IAsyncQueryServiceProvider
    {
        private readonly HubConnection _connection;

        public DefaultAsyncQueryServiceProvider(HubConnection connection)
        {
            _connection = connection;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token) =>
            _connection.StreamAsync<T>("qx`n", expression.ToRemoteLinqExpression(), token).GetAsyncEnumerator(/*cancel?*/);

        public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
            new ValueTask<T>(_connection.InvokeAsync<T>("qx`1", expression.ToRemoteLinqExpression(), token));
    }
}
