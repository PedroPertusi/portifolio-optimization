using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using DotNetEnv;

namespace App.Orchestrator
{
    public class AlphaVantageApi
    {
        private static readonly HttpClient _http = new();
        private readonly string _apiKey;
        private readonly DateTime _start, _end;

        private AlphaVantageApi(string apiKey, DateTime start, DateTime end)
        {
            _apiKey = apiKey;
            _start  = start;
            _end    = end;
        }

        /// Load the .env file and read the API key 
        public static AlphaVantageApi CreateFromEnv(DateTime start, DateTime end)
        {
            Env.Load();
            var key = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY")
                   ?? throw new InvalidOperationException("Defina ALPHAVANTAGE_API_KEY");
            return new AlphaVantageApi(key, start, end);
        }

        /// Fetches & parses the full daily series for one stock.
        public async Task<List<EquityPrice>> GetDailySeriesAsync(string symbol)
        {
            var url =
                $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY" +
                $"&symbol={symbol}&outputsize=full&apikey={_apiKey}";

            var resp = await _http.GetAsync(url).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Parse(json, symbol);
        }

        private List<EquityPrice> Parse(string json, string symbol)
        {
            var root = JObject.Parse(json);
            if (root["Error Message"] is not null)  throw new InvalidOperationException((string)root["Error Message"]!);
            if (root["Note"] is not null)           throw new InvalidOperationException((string)root["Note"]!);
            if (root["Information"] is not null)    throw new InvalidOperationException((string)root["Information"]!);

            var ts = (JObject?)root["Time Series (Daily)"]
                     ?? throw new InvalidOperationException("Time Series not found.");

            var list = new List<EquityPrice>();
            foreach (var p in ts.Properties())
            {
                if (!DateTime.TryParse(p.Name, out var d) ||
                    d < _start || d > _end) continue;

                var tok = p.Value["4. close"]?.ToString();
                if (double.TryParse(tok,
                                    NumberStyles.Any,
                                    CultureInfo.InvariantCulture,
                                    out var price))
                {
                    list.Add(new EquityPrice {
                        Date   = d,
                        Ticker = symbol,
                        Price  = price
                    });
                }
            }

            list.Sort((a, b) => a.Date.CompareTo(b.Date));
            return list;
        }
    }
}
