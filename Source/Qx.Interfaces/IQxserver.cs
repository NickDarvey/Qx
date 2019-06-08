using System;

namespace Qx.Interfaces
{
    public interface IQxserver<in T> : IAsyncObserver<T> { }
}
