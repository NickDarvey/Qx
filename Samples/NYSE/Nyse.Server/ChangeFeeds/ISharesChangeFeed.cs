using Nyse.Schema;
using System.Collections.Generic;
using System.Linq;

namespace Nyse.Server.ChangeFeeds
{
    public interface ISharesChangeFeed
    {
        IAsyncEnumerable<SharePrice> GetSharePriceChanges();
        IAsyncEnumerable<SharesOutstanding> GetSharesOutstandingChanges();
    }
}
