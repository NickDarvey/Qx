using Microsoft.AspNetCore.Authorization;
using Nyse.Schema;
using Nyse.Server.ChangeFeeds;
using Nyse.Server.Repositories;
using Qx.SignalR;
using System.Linq;
using static Nyse.Server.Security;
using static Qx.SignalR.HubSources;

namespace Nyse.Server
{
    public class QueryableStocksHub : QueryableHub<QueryableStocksHub>
    {
        private readonly ISharesRepository _sharesRepository;
        private readonly ISharesChangeFeed _sharesChangeFeed;

        public QueryableStocksHub(
            ISharesRepository sharesRepository,
            ISharesChangeFeed sharesChangeFeed,
            IAuthorizationService authorizationService,
            IAuthorizationPolicyProvider authorizationPolicyProvider)
            : base(verifier: Verify, createAuthorizer: ctx => CreateHubAuthorizer(ctx.User, authorizationService, authorizationPolicyProvider))
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
