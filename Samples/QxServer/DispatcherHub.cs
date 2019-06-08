using Microsoft.AspNetCore.SignalR;
using Serialize.Linq.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace QxServer
{
    public class RandomHub : Hub
    {
        public async IAsyncEnumerable<int> Range(int start, int count)
        {
            for (int i = start; i < count; i++)
            {
                yield return i;
            }
        }

    }

    public class DispatcherHub : Hub
    {
        [HubMethodName("qx")]
        public IAsyncEnumerable<int> GetEnumerable(ExpressionNode expression)
        {
            // Could turn it into a lambda expression which accepts a IAsyncEnum
            var y = AsyncEnumerable.Range(0, 10).AsAsyncQueryable();
            var x = expression.ToString();
            var excpr = expression.ToExpression();
            var outer = (MethodCallExpression)excpr;
            // At this point I have an Expr.Parameter which I think I can just
            // replace with a call to get my actual IAsyncEnum or turn into a lambda thingy

            // I've also got a tree with a quoted thing, i don't lknow what that's about
            Console.WriteLine("Got exprtree: " + expression);



            var start = 0;
            var count = 50;

            using var hub = new RandomHub
            {
                Context = this.Context
            };
            return hub.Range(start, count);
        }
    }

}
