namespace Nyse.Schema
{
    public class SharesOutstanding
    {
        public string Symbol { get; }
        public long Count { get; }

        public SharesOutstanding(string symbol, long count) =>
            (Symbol, Count) = (symbol, count);
    }
}
