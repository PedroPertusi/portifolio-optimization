// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Library.Func;   // for Portfolio.dailyReturns & PortfolioSimulation

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
            var allStocks = await DataLoader.LoadAllAsync(
                Dow30,
                StartDate,
                EndDate,
                "../data/dow30.csv"
            );

            // 2) Build the daily returns matrix (days × valid assets)
            var dailyReturnsList = Dow30
                .Select(ticker => Portfolio.dailyReturns(
                    allStocks[ticker].Select(ep => ep.Price).ToArray()))
                .ToArray();

            int days = dailyReturnsList[0].Length;
            double[][] returnsMatrix = Enumerable.Range(0, days)
                .Select(d => dailyReturnsList.Select(arr => arr[d]).ToArray())
                .ToArray();

            // 3) Simulation parameters
            int assetCount = Dow30.Length;
            int comboSize  = 25;
            int comboLimit = 142506; // C(30,25)
            double maxPct  = 0.20;   // 20% cap per asset

            // 4) Run & time the simulation
            var sw = Stopwatch.StartNew();
            var results = PortfolioSimulation.simulateSomeCombinations(
                returnsMatrix,
                assetCount,
                comboSize,
                comboLimit,
                maxPct
            );
            sw.Stop();

            // 5) Write detailed CSV blocks
            var resultsDir = Path.Combine("..", "results");
            Directory.CreateDirectory(resultsDir);
            var csvPath = Path.Combine(resultsDir, "test_bestPortfolios.csv");

            var csvLines = new List<string>();
            foreach (var r in results)
            {
                var line = "";
                for (int i = 0; i < r.Combination.Count(); i++)
                {
                    var ticker = Dow30[r.Combination[i]];
                    var weight = r.BestWeights[i];
                    line += $"{ticker} = {weight * 100:0.0}%;";
                }
                line += $"Sharpe = {r.BestSharpe:0.000}";
                csvLines.Add(line);
            }
            File.WriteAllLines(csvPath, csvLines);
            Console.WriteLine($"Wrote detailed portfolios to {csvPath}");

            // 6) Report timing & best Sharpe
            Console.WriteLine($"\nSimulation took: {sw.Elapsed.TotalSeconds:F0} seconds");
            var bestSharpe = PortfolioSimulation.bestSharpeRatio(results);
            Console.WriteLine($"Best overall Sharpe: {bestSharpe:0.000}");
        }
    }
}
