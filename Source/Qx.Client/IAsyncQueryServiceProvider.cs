using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Qx.Client
{
    public interface IAsyncQueryServiceProvider
    {
        ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token);
        IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token);
    }
}
