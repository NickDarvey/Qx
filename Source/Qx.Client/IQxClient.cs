using Qx.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qx.Client
{
    public interface IQxClient
    {
        IQxservable<T> GetObservable<T>(string observableName);
        IQxserver<T> GetObserver<T>(string observerName);
    }
}
