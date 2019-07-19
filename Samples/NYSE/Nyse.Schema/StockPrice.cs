namespace Nyse.Schema
{
    public class StockPrice
    {
        public string Symbol { get; }
        public double Price { get; }

        public StockPrice(string symbol, double price) =>
            (Symbol, Price) = (symbol, price);
    }
}
