namespace App.Orchestrator
{
    public class StockQuote
    {
        public string Symbol    { get; set; } = default!;
        public string Price     { get; set; } = default!;
        public string Timestamp { get; set; } = default!;
    }
}
