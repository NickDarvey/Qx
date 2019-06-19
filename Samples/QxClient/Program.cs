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
                .WithUrl("http://localhost:5000/qx")

                .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects)
                .Build();

            await Task.Delay(5000);
            await connection.StartAsync();

            var x = new QxAsyncQueryClient(new SignalrAsyncQueryServiceProvider(connection));

            var q = x.GetEnumerable<int>("Range").Where(n => n % 2 == 0);

            await q.ForEachAsync(n => Console.WriteLine("Hello you: " + n));
        }
    }
}