namespace Nyse.Schema
{
    public class Listing
    {
        public string Symbol { get; }
        public string Name { get; }

        public Listing(string symbol, string name) =>
            (Symbol, Name) = (symbol, name);
    }
}
