using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Nyse.Schema;
using System;
using System.Threading.Tasks;

namespace Nyse.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/stocks")
                .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects)
                .Build();

            await Task.Delay(3000);
            await connection.StartAsync();


            await foreach (var stockPrice in connection.StreamAsync<StockPrice>("GetStockPrices"))
            {
                Console.WriteLine($"{stockPrice.Symbol}: ${stockPrice.Price}");
            }
        }
    }
}
