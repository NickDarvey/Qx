using Nyse.Schema;
using System.Linq;

namespace Nyse.Server.Repositories
{
    public class SampleStockPriceRepository : IStockPriceRepository
    {
        public IAsyncQueryable<StockPrice> GetStockPrices() => new[]
        {
            new StockPrice("MSFT", 1.00),
            new StockPrice("AAPL", 1.00),
        }
        .ToAsyncEnumerable()
        .AsAsyncQueryable();
    }
}
