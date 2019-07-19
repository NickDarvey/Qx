using Microsoft.AspNetCore.SignalR;
using Nyse.Schema;
using Nyse.Server.ChangeFeeds;
using Nyse.Server.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Nyse.Server
{
    public class StocksHub : Hub
    {
        private readonly IStockPriceRepository _stockPriceRepository;
        private readonly IStockPriceChangeFeed _stockPriceChangeFeed;

        public StocksHub(IStockPriceRepository stockPriceRepository, IStockPriceChangeFeed stockPriceChangeFeed)
        {
            _stockPriceRepository = stockPriceRepository;
            _stockPriceChangeFeed = stockPriceChangeFeed;
        }

        public IAsyncEnumerable<StockPrice> GetStockPrices() =>
            // Fetch the latest stock prices,
            // then continue with any updates.

            // TODO: traditional linq against database fetching the latest

            _stockPriceRepository.GetStockPrices().Concat(_stockPriceChangeFeed.GetStockPriceChanges());
    }
}
