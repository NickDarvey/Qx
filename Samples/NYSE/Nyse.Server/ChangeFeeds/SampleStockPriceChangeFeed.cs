using Nyse.Schema;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nyse.Server.ChangeFeeds
{
    public class SampleStockPriceChangeFeed : IStockPriceChangeFeed
    {
        private readonly Random _random = new Random();
        public async IAsyncEnumerable<StockPrice> GetStockPriceChanges()
        {
            while (true)
            {
                yield return new StockPrice("AAPL", _random.NextDouble());
                await Task.Delay(_random.Next(3000));
                yield return new StockPrice("MSFT", _random.NextDouble());
                await Task.Delay(_random.Next(3000));
            }
        }
    }
}
