using Microsoft.AspNetCore.SignalR;
using Nyse.Schema;
using Nyse.Server.ChangeFeeds;
using Nyse.Server.Repositories;
using Qx.SignalR;
using System.Linq;

namespace Nyse.Server
{
    public class QueryableStocksHub : QueryableHub<QueryableStocksHub>
    {
        private readonly ISharesRepository _sharesRepository;
        private readonly ISharesChangeFeed _sharesChangeFeed;

        public QueryableStocksHub(
            ISharesRepository sharesRepository,
            ISharesChangeFeed sharesChangeFeed,
            IQxService qxService) : base(qxService)
        {
            _sharesRepository = sharesRepository;
            _sharesChangeFeed = sharesChangeFeed;
        }

        public IAsyncQueryable<SharePrice> SharePrices() =>
            // Fetch the latest stock prices,
            // then continue with any updates.

            // TODO: traditional linq against database fetching the latest

            _sharesRepository.GetSharePrices().Concat(_sharesChangeFeed.GetSharePriceChanges());

        public IAsyncQueryable<SharesOutstanding> SharesOutstanding() =>
            _sharesRepository.GetSharesOutstanding().Concat(_sharesChangeFeed.GetSharesOutstandingChanges());

        public IAsyncQueryable<Listing> Listings() =>
            _sharesRepository.GetListings();
    }
}
