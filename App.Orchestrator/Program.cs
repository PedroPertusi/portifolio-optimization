using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DotNetEnv;

namespace App.Orchestrator
{
    class Program
    {
        static readonly DateTime StartDate = new(2024, 8, 1);
        static readonly DateTime EndDate = new(2024, 12, 31);
        static readonly string[] dow30 = new[]
        {
            "AAPL","AMGN","AXP","BA","CAT","CRM","CSCO","CVX","DIS","DOW",
            "GS","HD","HON","IBM","INTC","JNJ","JPM","KO","MCD","MMM",
            "MRK","MSFT","NKE","PG","TRV","UNH","V","VZ","WBA","WMT"
        };


        static async Task Main()
        {
            Env.Load();
            var api = new AlphaVantageApi(GetApiKey(), StartDate, EndDate);

            // one line to fetch from API or fall back to CSV:
            var allStocks = await FetchOrLoadAsync(api, "../data/dow30.csv");

            // print as before
            foreach (var kv in allStocks)
            {
                Console.WriteLine($"{kv.Value.Count} dias para {kv.Key}:");
                PrintPrices(kv.Value);
            }
        }

        static async Task<Dictionary<string, List<EquityPrice>>> FetchOrLoadAsync(
            AlphaVantageApi api, string csvPath)
        {
            if (File.Exists(csvPath))
                return LoadFromCsv(csvPath);

            try
            {
                return await api.GetAllDailySeriesAsync(Dow30);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro API, lendo CSV: {ex.Message}");
                return LoadFromCsv(csvPath);
            }
        }

        static Dictionary<string, List<EquityPrice>> LoadFromCsv(string path)
        {
            var lines   = File.ReadAllLines(path);
            var header  = lines[0].Split(',').Skip(1).ToArray(); // [ "AAPL", "AMGN", ... ]
            var dict    = header.ToDictionary(t => t, _ => new List<EquityPrice>());

            foreach (var row in lines.Skip(1))
            {
                var cols = row.Split(',');
                var date = DateTime.Parse(cols[0]);
                for (int i = 1; i < cols.Length; i++)
                {
                    if (double.TryParse(cols[i],
                                        System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        out var price))
                    {
                        dict[header[i - 1]].Add(new EquityPrice { Date = date, Ticker = header[i - 1], Price = price });
                    }
                }
            }

            // sort each series
            foreach (var list in dict.Values)
                list.Sort((a, b) => a.Date.CompareTo(b.Date));

            return dict;
        }

        private static string GetApiKey() =>
            Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY")
            ?? throw new InvalidOperationException("Defina ALPHAVANTAGE_API_KEY");

        private static void PrintPrices(IEnumerable<EquityPrice> prices)
        {
            foreach (var p in prices)
                Console.WriteLine($"{p.Date:yyyy-MM-dd}: {p.Price}");
        }
    }
}
