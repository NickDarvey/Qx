namespace Nyse.Schema
{
    public class SharePrice
    {
        public string Symbol { get; }
        public decimal Price { get; }

        public SharePrice(string symbol, decimal price) =>
            (Symbol, Price) = (symbol, price);
    }
}
