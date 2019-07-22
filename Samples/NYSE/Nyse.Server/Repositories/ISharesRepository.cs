using Nyse.Schema;
using System.Linq;

namespace Nyse.Server.Repositories
{
    public interface ISharesRepository
    {
        IAsyncQueryable<Listing> GetListings();
        IAsyncQueryable<SharePrice> GetSharePrices();
        IAsyncQueryable<SharesOutstanding> GetSharesOutstanding();
    }
}
