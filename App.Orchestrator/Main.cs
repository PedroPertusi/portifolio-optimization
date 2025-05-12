// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using App.Orchestrator;             
using static Library.Func.Helper;     
using Library.Func;                   

namespace App.Orchestrator
{
    class Program
    {
        static readonly DateTime StartDate = new DateTime(2024, 8, 1);
        static readonly DateTime EndDate = new DateTime(2024, 12, 31);
        static readonly DateTime startQ1 = new DateTime(2025, 1, 1);
        static readonly DateTime endQ1 = new DateTime(2025, 3, 31);
        static readonly string[] Dow30 = {
            "AAPL","AMGN","AMZN","AXP","BA","CAT","CRM","CSCO","CVX","DIS",
            "GS","HD","HON","IBM","JNJ","JPM","KO","MCD","MMM","MRK",
            "MSFT","NKE","NVDA","PG","SHW","TRV","UNH","V","VZ","WMT"
        };

        static async Task Main()
        {
            // collect all console outputs here
            var logs = new List<string>();
            Console.WriteLine($"\n=== Initial Simulation {StartDate:yyyy-MM-dd} → {EndDate:yyyy-MM-dd} ===");
            logs.Add($"\n=== Initial Simulation {StartDate:yyyy-MM-dd} → {EndDate:yyyy-MM-dd} ===");

            // 1) Fetch or load all price data
            var allStocks = await DataLoader.LoadAllAsync(
                Dow30,
                StartDate,
                EndDate,
                "../data/dow30.csv"
            );

            // 2) Build the daily returns matrix
            var dailyReturnsList = Dow30
                .Select(t => dailyReturns(
                    allStocks[t].Select(ep => ep.Price).ToArray()))
                .ToArray();

            int days = dailyReturnsList[0].Length;
            double[][] returnsMatrix = Enumerable.Range(0, days)
                .Select(d => dailyReturnsList.Select(arr => arr[d]).ToArray())
                .ToArray();

            // 3) Simulation parameters
            int assetCount = Dow30.Length;
            int comboSize = 25;
            int comboLimit = 142506; // C(30,25)
            double maxPct = 0.20;   // 20% cap

            // 4) Run & time the simulation
            var sw = Stopwatch.StartNew();
            var results = Simulate.simulateSomeCombinations(
                returnsMatrix,
                assetCount,
                comboSize,
                comboLimit,
                maxPct
            );
            sw.Stop();

            // 5) Extract best-Sharpe result
            var best = results.OrderByDescending(r => r.BestSharpe).First();

            // 5a) Find its 0-based index in the results list
            int bestIdx = results
                .Select((r, idx) => new { r, idx })
                .First(pair => pair.r.Equals(best))
                .idx;

            int bestLine = bestIdx + 1;

            Console.WriteLine($"Best Sharpe (in-sample): {best.BestSharpe:0.000} (CSV line: {bestLine})");
            logs.Add($"Best Sharpe (in-sample): {best.BestSharpe:0.000} (CSV line: {bestLine})");

            // 6) Build ticker→weight map...
            var weightMap = best.Combination
                .Zip(best.BestWeights, (idx, w) => new { idx, w })
                .ToDictionary(x => Dow30[x.idx], x => x.w);

            // 7) (Optional) rewrite CSV with all results
            var resultsDir = Path.Combine("..", "results");
            Directory.CreateDirectory(resultsDir);
            var csvPath = Path.Combine(resultsDir, "bestPortfolios.csv");
            File.WriteAllLines(csvPath,
                results.Select(r =>
                    string.Join(";", 
                        r.Combination.Zip(r.BestWeights, (idx, w) =>
                            $"{Dow30[idx]} = {w * 100:0.0}%"
                        )
                    ) + $";Sharpe = {r.BestSharpe:0.000}"
                )
            );

            Console.WriteLine($"Wrote detailed portfolios to {csvPath}");
            logs.Add($"Wrote detailed portfolios to {csvPath}");

            Console.WriteLine($"Simulation took: {sw.Elapsed.TotalSeconds:F0} seconds");
            logs.Add($"Simulation took: {sw.Elapsed.TotalSeconds:F0} seconds");

            // ── Now test on Q1 2025 ──

            Console.WriteLine($"\n=== Backtest {startQ1:yyyy-MM-dd} → {endQ1:yyyy-MM-dd} ===");
            logs.Add($"\n=== Backtest {startQ1:yyyy-MM-dd} → {endQ1:yyyy-MM-dd} ===");

            // 9) Load only the tickers we actually hold
            var q1Data = await DataLoader.LoadAllAsync(
                weightMap.Keys.ToArray(), startQ1, endQ1,
                "../data/dow_jones_q1.csv"
            );

            // 10) Compute daily returns per asset
            var tickersQ1 = weightMap.Keys.ToList();
            var returnsPerAssetQ1 = tickersQ1
                .Select(t => dailyReturns(q1Data[t].Select(ep => ep.Price).ToArray()))
                .ToArray();

            // 11) Build a new returns‐matrix for Q1
            int daysQ1 = returnsPerAssetQ1[0].Length;
            double[][] returnsMatrixQ1 = Enumerable.Range(0, daysQ1)
                .Select(d => returnsPerAssetQ1.Select(arr => arr[d]).ToArray())
                .ToArray();

            // 12) Build the weight vector in the same order
            double[] weightsQ1 = tickersQ1.Select(t => weightMap[t]).ToArray();

            // 13) Compute Sharpe only, using pure helpers
            var portDailyQ1 = portfolioDailyReturn(returnsMatrixQ1, weightsQ1);
            var annRetQ1 = annualizedReturn(portDailyQ1);
            var annVolQ1 = annualizedVolatility(portDailyQ1);
            var sharpeQ1 = sharpeRatio(annRetQ1, annVolQ1);

            Console.WriteLine($"Sharpe Ratio (Q1 2025): {sharpeQ1:0.000}");
            logs.Add($"Sharpe Ratio (Q1 2025): {sharpeQ1:0.000}");

            // 14) Save outputs to results/results.txt  
            var summaryPath = Path.Combine(resultsDir, "results.txt");
            File.WriteAllLines(summaryPath, logs);
            Console.WriteLine($"Wrote summary to {summaryPath}");
        }
    }
}
