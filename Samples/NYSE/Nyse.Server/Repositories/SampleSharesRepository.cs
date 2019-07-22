using Nyse.Schema;
using System.Linq;

namespace Nyse.Server.Repositories
{
    public class SampleSharesRepository : ISharesRepository
    {
        public IAsyncQueryable<SharePrice> GetSharePrices() => new[]
        {
            new SharePrice("MSFT", 136.00m),
            new SharePrice("AAPL", 202.00m),
        }
        .ToAsyncEnumerable()
        .AsAsyncQueryable();

        public IAsyncQueryable<SharesOutstanding> GetSharesOutstanding() => new[]
        {
            new SharesOutstanding("MSFT", 7_794_000_000),
            new SharesOutstanding("AAPL", 4_701_000_000),
        }
        .ToAsyncEnumerable()
        .AsAsyncQueryable();

        public IAsyncQueryable<Listing> GetListings() => new[]
        {
            new Listing("MSFT", "Microsoft"),
            new Listing("AAPL", "Apple"),
        }
        .ToAsyncEnumerable()
        .AsAsyncQueryable();
    }
}
