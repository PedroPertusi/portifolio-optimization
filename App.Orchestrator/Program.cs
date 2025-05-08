using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;       // for Stopwatch
using System.Collections.Generic;
using Library.Func;

namespace App.Orchestrator
{
    class Program
    {
        static readonly DateTime StartDate = new DateTime(2024, 8, 1);
        static readonly DateTime EndDate   = new DateTime(2024, 12, 31);
        static readonly string[] Dow30     = {
            "AAPL","AMGN","AMZN","AXP","BA","CAT","CRM","CSCO","CVX","DIS",
            "GS","HD","HON","IBM","JNJ","JPM","KO","MCD","MMM","MRK",
            "MSFT","NKE","NVDA","PG","SHW","TRV","UNH","V","VZ","WMT"
        };

        static async Task Main()
        {
            // 1) Fetch or load all price data
            var service   = new StockDataService(Dow30, StartDate, EndDate, "../data/dow30.csv");
            var allStocks = await service.FetchOrLoadAsync();

            // 2) Filter valid tickers
            var validTickers = Dow30.Where(t => allStocks.ContainsKey(t)).ToArray();
            var missing      = Dow30.Except(Dow30).ToArray();
            if (missing.Length > 0)
                Console.WriteLine($"Warning: Missing data for {missing.Length} tickers: {string.Join(", ", missing)}");

            // 3) Build the daily returns matrix (days × valid assets)
            var dailyReturnsList = Dow30.Select(ticker => Portfolio.dailyReturns(allStocks[ticker]
                                    .Select(ep => ep.Price).ToArray())).ToArray();

            int days = dailyReturnsList[0].Length;
            double[][] returnsMatrix = Enumerable.Range(0, days)
                .Select(d => dailyReturnsList.Select(arr => arr[d]).ToArray())
                .ToArray();

            // 4) Simulation parameters
            int assetCount = Dow30.Length;
            int comboSize  = 25;
            int comboLimit = 10;    // or int.MaxValue for all combos
            double maxPct  = 0.20;     // 20% cap per asset

            // 5) Run & time the simulation
            var sw = Stopwatch.StartNew();
            var results = PortfolioSimulation.simulateSomeCombinations(
                returnsMatrix,
                assetCount,
                comboSize,
                comboLimit,
                maxPct
            );

            // 6) Write detailed CSV blocks
            var resultsDir = Path.Combine("..", "results");
            Directory.CreateDirectory(resultsDir);
            var csvPath = Path.Combine(resultsDir, "bestPortfolios.csv");

            var csvLines = new List<string>();
            foreach (var r in results)
            {
                var string_line = new string("");
                // 25 lines: "TICKER = 15.3%"
                for (int i = 0; i < r.Combination.Count(); i++)
                {
                    var ticker = Dow30[r.Combination[i]];
                    var weight = r.BestWeights[i];
                    string_line += $"{ticker} = {weight * 100:0.0}%;";
                    // csvLines.Add($"{ticker} = {weight:P1}");
                }
                // 26th line: Sharpe
                string_line += $"Sharpe = {r.BestSharpe:0.000}";

                csvLines.Add(string_line);
            }

            File.WriteAllLines(csvPath, csvLines);
            Console.WriteLine($"Wrote detailed portfolios to {csvPath}");

            sw.Stop();
            Console.WriteLine($"\nSimulation took: {sw.Elapsed.TotalSeconds:F0} seconds");
        }
    }
}
