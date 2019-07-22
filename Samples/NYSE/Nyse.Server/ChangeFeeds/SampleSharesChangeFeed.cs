using Nyse.Schema;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nyse.Server.ChangeFeeds
{
    public class SampleSharesChangeFeed : ISharesChangeFeed
    {
        private readonly Random _random = new Random();

        public async IAsyncEnumerable<SharePrice> GetSharePriceChanges()
        {
            while (true)
            {
                yield return new SharePrice("AAPL", _random.Next(200, 225));
                await Task.Delay(_random.Next(3000));
                yield return new SharePrice("MSFT", _random.Next(130, 150));
                await Task.Delay(_random.Next(3000));
            }
        }

        public async IAsyncEnumerable<SharesOutstanding> GetSharesOutstandingChanges()
        {
            while (true)
            {
                yield return new SharesOutstanding("AAPL", 4_701_000_000 + _random.Next(1_000_000, 1_000_000_000));
                await Task.Delay(_random.Next(3000));
                yield return new SharesOutstanding("MSFT", 7_794_000_000 + _random.Next(1_000_000, 1_000_000_000));
                await Task.Delay(_random.Next(3000));
            }
        }
    }
}
