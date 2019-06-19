using System;
using System.Linq;

namespace Qx
{
    public interface IAsyncQueryClient
    {
        IAsyncQueryable<TElement> GetEnumerable<TElement>(string name);
        Func<TArg, IAsyncQueryable<TElement>> GetEnumerable<TArg, TElement>(string name);
    }
}
