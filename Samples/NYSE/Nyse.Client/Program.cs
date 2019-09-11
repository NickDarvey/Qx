using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Nyse.Schema;
using Qx.Client;
using Qx.Client.SignalR;
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
                    .Select(so => new { so.Symbol, MarketCap = sp.Price * so.Count }))

                // Find the name
                .SelectMany(mc => client.GetEnumerable<Listing>("Listings")
                    .Where(ls => ls.Symbol == mc.Symbol)
                    .Select(ls => ValueTuple.Create(mc.Symbol, ls.Name, mc.MarketCap)));

            var query2 = from sp in client.GetEnumerable<SharePrice>("SharePrices")
                         where sp.Symbol == "MSFT"
                         from so in client.GetEnumerable<SharesOutstanding>("SharesOutstanding")
                         where so.Symbol == sp.Symbol
                         from ls in client.GetEnumerable<Listing>("Listings")
                         where ls.Symbol == sp.Symbol
                         select ValueTuple.Create(ls.Symbol, ls.Name, sp.Price * so.Count);

            await foreach (var element in query)
            {
                Console.WriteLine($"{element.Item2} ({element.Item1}): {element.Item3.ToString("C")}");
            }
        }
    }
}
