using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Qx;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QxClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/sample")
                .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects)
                .Build();

            await Task.Delay(5000);
            await connection.StartAsync();



            var x = new QxAsyncQueryClient(new SignalRAsyncQueryServiceProvider(connection));

            var range = x.GetEnumerable<int, int, int>("Range");

            var r1 = range(0, 10);
            var r2 = range(10, 10);

            //var q = r1.Zip(r2);
            var q = await r1.Zip(r2).Skip(1).FirstAsync();

            //var q = x.GetEnumerable<int, int, int>("Range")(0,20).Where(n => n % 2 == 0).Select(x => x * 2);

            //await q.ForEachAsync(n => Console.WriteLine("Hello you: " + n));

            Console.WriteLine("Hello you: " + q);
        }

        //static async Task Main(string[] args)
        //{
        //    var connection = new HubConnectionBuilder()
        //        .WithUrl("http://localhost:5000/qx")

        //        .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects)
        //        .Build();

        //    await Task.Delay(5000);
        //    await connection.StartAsync();

        //    await connection.StreamAsync<int>("Thing`1", 10).ForEachAsync(n => Console.WriteLine("1: " + n));
        //    await connection.StreamAsync<int>("Thing`2", 10, 10).ForEachAsync(n => Console.WriteLine("2: " + n));
        //}
    }
}