using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using DotNetEnv;

namespace App.Orchestrator
{
    class Program
    {
        private static readonly DateTime StartDate = new(2024, 8, 1);
        private static readonly DateTime EndDate = new(2024, 12, 31);
        private const string Symbol = "AAPL";
        static readonly string[] dow30 = new[]
        {
            "AAPL","AMGN","AXP","BA","CAT","CRM","CSCO","CVX","DIS","DOW",
            "GS","HD","HON","IBM","INTC","JNJ","JPM","KO","MCD","MMM",
            "MRK","MSFT","NKE","PG","TRV","UNH","V","VZ","WBA","WMT"
        };

        static async Task Main()
        {
            Env.Load();
            try
            {
                var jsonResults = new List<string>();
                var allStocks = new List<List<EquityPrice>>();
                var api = new AlphaVantageApi(GetApiKey());

                foreach (var stock in dow30)
                {
                    jsonResults.Add(await api.GetDailyTimeSeriesAsync(stock));
                }

                foreach (var json in jsonResults) {
                    allStocks.Add(ParseDailySeries(json));
                }
                

                for (int i = 0; i < dow30.Length; i++) {
                    var ticker = dow30[i];
                    var series = allStocks[i];
                    Console.WriteLine($"{series.Count} dias retornados para {ticker}:");
                    PrintPrices(series);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
        }

        private static string GetApiKey() =>
            Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY")
            ?? throw new InvalidOperationException("Defina ALPHAVANTAGE_API_KEY");

        private static List<EquityPrice> ParseDailySeries(string json)
        {
            var root = JObject.Parse(json);
            ValidateResponse(root);

            var series = root["Time Series (Daily)"] as JObject
                         ?? throw new InvalidOperationException("Série diária não encontrada.");

            var list = new List<EquityPrice>();
            foreach (var prop in series.Properties())
            {
                if (!DateTime.TryParse(prop.Name, out var date) ||
                    date < StartDate || date > EndDate)
                    continue;

                var closeToken = prop.Value["4. close"]?.ToString();
                if (double.TryParse(closeToken, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var price))
                {
                    list.Add(new EquityPrice
                    {
                        Date = date,
                        Ticker = Symbol,
                        Price = price
                    });
                }
            }

            list.Sort((a, b) => a.Date.CompareTo(b.Date));
            return list;
        }

        private static void ValidateResponse(JObject root)
        {
            if (root["Error Message"] is not null) throw new InvalidOperationException((string)root["Error Message"]!);
            if (root["Note"] is not null) throw new InvalidOperationException((string)root["Note"]!);
            if (root["Information"] is not null) throw new InvalidOperationException((string)root["Information"]!);
        }

        private static void PrintPrices(IEnumerable<EquityPrice> prices)
        {
            foreach (var p in prices)
                Console.WriteLine($"{p.Date:yyyy-MM-dd}: {p.Price}");
        }
    }
}
