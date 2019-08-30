using System;
using System.Linq;

namespace Qx.Client
{
    public interface IAsyncQueryClient
    {
        IAsyncQueryable<TElement> GetEnumerable<TElement>(string name);
        Func<TArg, IAsyncQueryable<TElement>> GetEnumerable<TArg, TElement>(string name);
    }
}
