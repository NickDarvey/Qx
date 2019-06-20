using Microsoft.AspNetCore.SignalR;
using Qx;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QxServer
{
    public class MyHub : QueryableHub
    {
        [HubMethodName("Range")]
        public IAsyncQueryable<int> Range(int start, int count) => AsyncEnumerable.Range(start, count).AsAsyncQueryable();
    }

}
