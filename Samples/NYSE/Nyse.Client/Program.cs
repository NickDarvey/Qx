using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Nyse.Schema;
using Qx;
using Qx.SignalR;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nyse.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {


            //await RunEnumerable();
            await RunQueryable();
        }

        private static async Task<HubConnection> Connect(string endpoint)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(endpoint)
                .AddNewtonsoftJsonProtocol(s => s.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects)
                .Build();

            while (connection.State == HubConnectionState.Disconnected) // Wait for server to start
            {
                try { await connection.StartAsync(); }
                catch (HttpRequestException ex) { Console.WriteLine("Error connecting to server: " + ex.Message); }
            }

            return connection;
        }


        private static async Task RunEnumerable()
        {
            var connection = await Connect("http://localhost:60591/shares");

            var stream = connection.StreamAsync<SharePrice>("GetSharePrices");

            await foreach (var stockPrice in stream)
            {
                Console.WriteLine($"{stockPrice.Symbol}: ${stockPrice.Price}");
            }
        }

        private static async Task RunQueryable()
        {
            var connection = await Connect("http://localhost:60591/queryable-shares");

            var client = new QxAsyncQueryClient(new DefaultAsyncQueryServiceProvider(connection));

            var query = client.GetEnumerable<SharePrice>("SharePrices")
                .Where(sp => sp.Symbol == "MSFT")

                // Get the market cap
                .SelectMany(sp => client.GetEnumerable<SharesOutstanding>("SharesOutstanding")
                    .Where(so => so.Symbol == sp.Symbol)
                    .Select(so => Tuple.Create(so.Symbol, sp.Price * so.Count)))

                // Find the name
                .SelectMany(mc => client.GetEnumerable<Listing>("Listings")
                    .Where(ls => ls.Symbol == mc.Item1)
                    .Select(ls => Tuple.Create(mc.Item1, ls.Name, mc.Item2)));

            await foreach (var element in query)
            {
                Console.WriteLine($"{element.Item2} ({element.Item1}): {element.Item3.ToString("C")}");
            }
        }
    }
}
