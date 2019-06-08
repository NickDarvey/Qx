using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qx.Interfaces
{
    public interface IQxservable<out T> : IAsyncObservable<T>
    {
        Task<IQxscription> SubscribeAsync(IQxserver<T> observer, string subscriptionName, CancellationToken token = default);
    }
}
