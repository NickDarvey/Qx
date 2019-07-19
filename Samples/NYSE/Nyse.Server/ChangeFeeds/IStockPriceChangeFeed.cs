using Nyse.Schema;
using System.Collections.Generic;

namespace Nyse.Server.ChangeFeeds
{
    public interface IStockPriceChangeFeed
    {
        IAsyncEnumerable<StockPrice> GetStockPriceChanges();
    }
}
