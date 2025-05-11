// DataLoader.cs
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using DotNetEnv;

namespace App.Orchestrator
{
    /// Simple model for a single date/price point.
    public class EquityPrice
    {
        public DateTime Date   { get; set; }
        public string   Ticker { get; set; } = default!;
        public double   Price  { get; set; }
    }

    /// Two ways to load equity data in one file:
    ///  • LoadFromCsv: parses a “wide” CSV into Dictionary[ticker→series].
    ///  • LoadFromApiAsync: fetches & parses one symbol from AlphaVantage.
    ///  • LoadAllAsync: CSV-first, otherwise API for each symbol.
    public static class DataLoader
    {
        private static readonly HttpClient _http = new();

        /// Reads a CSV where the first column is Date and the header row
        /// is “Date,TICKER1,TICKER2,...”. Returns a map of ticker→List<EquityPrice>.
        public static Dictionary<string, List<EquityPrice>> LoadFromCsv(string path)
        {
            var lines  = File.ReadAllLines(path);
            if (lines.Length < 2) return new Dictionary<string, List<EquityPrice>>();

            var header = lines[0].Split(',').Skip(1).ToArray();
            var result = new Dictionary<string, List<EquityPrice>>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in header)
                result[t] = new List<EquityPrice>();

            foreach (var row in lines.Skip(1))
            {
                var cols = row.Split(',');
                if (!DateTime.TryParse(cols[0], out var dt))
                    continue;

                for (int i = 1; i < cols.Length; i++)
                {
                    if (double.TryParse(cols[i],
                                        NumberStyles.Any,
                                        CultureInfo.InvariantCulture,
                                        out var price))
                    {
                        result[header[i - 1]].Add(new EquityPrice
                        {
                            Date   = dt,
                            Ticker = header[i - 1],
                            Price  = price
                        });
                    }
                }
            }

            foreach (var series in result.Values)
                series.Sort((a, b) => a.Date.CompareTo(b.Date));

            return result;
        }

        /// Fetches the full daily series for one symbol from AlphaVantage,
        /// filters by [start,end], parses “4. close” into a List<EquityPrice>.
        /// Requires ALPHAVANTAGE_API_KEY in your .env or environment.
        public static async Task<List<EquityPrice>> LoadFromApiAsync(
            string symbol,
            DateTime start,
            DateTime end)
        {
            Env.Load();
            var key = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY")
                      ?? throw new InvalidOperationException(
                          "Please set ALPHAVANTAGE_API_KEY in your environment.");

            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY" +
                      $"&symbol={symbol}&outputsize=full&apikey={key}";

            var resp = await _http.GetAsync(url).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var root = JObject.Parse(json);

            if (root["Error Message"]   != null) throw new InvalidOperationException((string)root["Error Message"]!);
            if (root["Note"]            != null) throw new InvalidOperationException((string)root["Note"]!);
            if (root["Information"]     != null) throw new InvalidOperationException((string)root["Information"]!);

            var ts = (JObject?)root["Time Series (Daily)"]
                     ?? throw new InvalidOperationException("Time Series data not found.");

            var list = new List<EquityPrice>();
            foreach (var p in ts.Properties())
            {
                if (!DateTime.TryParse(p.Name, out var date) || date < start || date > end)
                    continue;

                var tok = p.Value["4. close"]?.ToString();
                if (double.TryParse(tok,
                                    NumberStyles.Any,
                                    CultureInfo.InvariantCulture,
                                    out var price))
                {
                    list.Add(new EquityPrice
                    {
                        Date   = date,
                        Ticker = symbol,
                        Price  = price
                    });
                }
            }

            list.Sort((a, b) => a.Date.CompareTo(b.Date));
            return list;
        }

        /// Try CSV first; if missing, fetch each symbol from the API.
        public static async Task<Dictionary<string, List<EquityPrice>>> LoadAllAsync(
            string[] symbols,
            DateTime start,
            DateTime end,
            string csvPath)
        {
            if (File.Exists(csvPath))
                return LoadFromCsv(csvPath);

            var dict = new Dictionary<string, List<EquityPrice>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sym in symbols)
                dict[sym] = await LoadFromApiAsync(sym, start, end).ConfigureAwait(false);

            return dict;
        }
    }
}
