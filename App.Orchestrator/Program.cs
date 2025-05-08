using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Library.Func;
using Microsoft.FSharp.Collections;

namespace App.Orchestrator
{
    class Program
    {
        static readonly DateTime StartDate = new(2024, 8, 1);
        static readonly DateTime EndDate   = new(2024, 12, 31);
        static readonly string[] Dow30     = {
            "AAPL","AMGN","AXP","BA","CAT","CRM","CSCO","CVX","DIS","DOW",
            "GS","HD","HON","IBM","INTC","JNJ","JPM","KO","MCD","MMM",
            "MRK","MSFT","NKE","PG","TRV","UNH","V","VZ","WBA","WMT"
        };

        static async Task Main()
        {
            var service   = new StockDataService(Dow30, StartDate, EndDate, "../data/dow30.csv");
            var allStocks = await service.FetchOrLoadAsync();

            if (!allStocks.TryGetValue("AAPL", out var aapl) ||
                !allStocks.TryGetValue("MSFT", out var msft))
            {
                Console.WriteLine("Could not find both AAPL and MSFT in the data.");
                return;
            }

            // ─── PORTFOLIO METRICS TEST ───────────────────────────────────
            Console.WriteLine("=== Portfolio Metrics Test (60% AAPL / 40% MSFT) ===");

            var aaplPrices = aapl.Select(ep => ep.Price).ToArray();
            var msftPrices = msft.Select(ep => ep.Price).ToArray();
            var aaplRet    = Portfolio.dailyReturns(aaplPrices);
            var msftRet    = Portfolio.dailyReturns(msftPrices);

            double[][] returnsMatrix = aaplRet
                .Select((r, i) => new[] { r, msftRet[i] })
                .ToArray();

            double[] fixedWeights = { 0.6, 0.4 };
            double[] portRets     = Portfolio.portfolioDailyReturn(returnsMatrix, fixedWeights);

            var annRet = Portfolio.annualizedReturn(portRets);
            var annVol = Portfolio.annualizedVolatility(portRets);
            var sharpe = Portfolio.sharpeRatio(annRet, annVol, 0.02);

            Console.WriteLine($"Annualized Return:    {annRet:P2}");
            Console.WriteLine($"Annualized Volatility:{annVol:P2}");
            Console.WriteLine($"Sharpe Ratio (@2%):   {sharpe:F2}");

            // ─── COMBINATIONS TEST ───────────────────────────────────────
            Console.WriteLine("\n=== Combinations Test (2 of [a, b, c, d]) ===");
            var items  = ListModule.OfSeq<string>(new[] { "a", "b", "c", "d" });
            var combos = Portfolio.combinations(2, items);
            foreach (var combo in combos)
            {
                // FSharpList<string> implements IEnumerable<string>
                Console.WriteLine($"[ {string.Join(", ", combo)} ]");
            }

            // ─── GENERATE WEIGHTS TEST ────────────────────────────────────
            Console.WriteLine("\n=== GenerateWeights Test (n=4, maxPct=0.5) ===");
            var rng           = new Random(42);
            var randomWeights = Portfolio.generateWeights(4, 0.5, rng);
            Console.WriteLine("Weights: " + string.Join(", ", randomWeights.Select(w => w.ToString("P2"))));
            Console.WriteLine($"Sum: {randomWeights.Sum():F3}");
        }
    }
}
