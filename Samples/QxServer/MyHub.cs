using Microsoft.AspNetCore.SignalR;
using Qx;
using System.Linq;

namespace QxServer
{
    public class MyHub : QueryableHub<MyHub>
    {
        [HubMethodName("Range")]
        public IAsyncQueryable<int> Range(int start, int count) => AsyncEnumerable.Range(start, count).AsAsyncQueryable();
    }

}
