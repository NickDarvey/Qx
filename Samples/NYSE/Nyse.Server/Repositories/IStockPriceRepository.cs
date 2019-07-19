using Nyse.Schema;
using System.Linq;

namespace Nyse.Server.Repositories
{
    public interface IStockPriceRepository
    {
        IAsyncQueryable<StockPrice> GetStockPrices();
    }
}
