using Microsoft.AspNetCore.Authorization;
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
        private readonly ISharesRepository _sharesRepository;
        private readonly ISharesChangeFeed _sharesChangeFeed;

        public StocksHub(
            ISharesRepository sharesRepository,
            ISharesChangeFeed sharesChangeFeed,
            IAuthorizationService authorizationService,
            IAuthorizationPolicyProvider authorizationPolicyProvider)

        {
            _sharesRepository = sharesRepository;
            _sharesChangeFeed = sharesChangeFeed;
        }

        public IAsyncEnumerable<SharePrice> GetSharePrices() =>
            // Fetch the latest stock prices,
            // then continue with any updates.

            // TODO: traditional linq against database fetching the latest

            _sharesRepository.GetSharePrices().Concat(_sharesChangeFeed.GetSharePriceChanges());

        public IAsyncEnumerable<SharesOutstanding> GetSharesOutstanding() =>
            _sharesRepository.GetSharesOutstanding().Concat(_sharesChangeFeed.GetSharesOutstandingChanges());

        public IAsyncEnumerable<Listing> GetListings() =>
            _sharesRepository.GetListings();

    }
}
