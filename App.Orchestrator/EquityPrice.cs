using System;

namespace App.Orchestrator
{
    public class EquityPrice
    {
        public DateTime Date   { get; set; }
        public string   Ticker { get; set; } = default!;
        public double   Price  { get; set; }
    }
}
