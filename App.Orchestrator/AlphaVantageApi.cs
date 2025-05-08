using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace App.Orchestrator
{
    public class AlphaVantageApi
    {
        private static readonly HttpClient _http = new();
        private readonly string _apiKey;
        private readonly DateTime _start;
        private readonly DateTime _end;

        public AlphaVantageApi(string apiKey, DateTime startDate, DateTime endDate)
        {
            _apiKey = apiKey;
            _start  = startDate;
            _end    = endDate;
        }

        /// <summary>
        /// Fetches & parses the daily series for ONE symbol.
        /// </summary>
        public async Task<List<EquityPrice>> GetDailySeriesAsync(string symbol)
        {
            var url = 
              $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY" +
              $"&symbol={symbol}&outputsize=full&apikey={_apiKey}";

            var resp = await _http.GetAsync(url).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            return ParseDailySeries(json, symbol);
        }

        /// <summary>
        /// Fetches & parses the daily series for *all* symbols in one call.
        /// </summary>
        public async Task<Dictionary<string, List<EquityPrice>>> GetAllDailySeriesAsync(IEnumerable<string> symbols)
        {
            var result = new Dictionary<string, List<EquityPrice>>(StringComparer.OrdinalIgnoreCase);

            foreach (var sym in symbols)
            {
                var series = await GetDailySeriesAsync(sym).ConfigureAwait(false);
                result[sym] = series;
            }

            return result;
        }

        #region ──── Parsing Helpers ──────────────────────────────────

        private List<EquityPrice> ParseDailySeries(string json, string symbol)
        {
            var root = JObject.Parse(json);
            ValidateResponse(root);

            var ts = (JObject?)root["Time Series (Daily)"]
                     ?? throw new InvalidOperationException("Série diária não encontrada.");

            var list = new List<EquityPrice>();
            foreach (var prop in ts.Properties())
            {
                if (!DateTime.TryParse(prop.Name, out var date) ||
                    date < _start || date > _end)
                    continue;

                var close = prop.Value["4. close"]?.ToString();
                if (double.TryParse(close,
                                    NumberStyles.Any,
                                    CultureInfo.InvariantCulture,
                                    out var price))
                {
                    list.Add(new EquityPrice {
                      Date   = date,
                      Ticker = symbol,
                      Price  = price
                    });
                }
            }

            list.Sort((a, b) => a.Date.CompareTo(b.Date));
            return list;
        }

        private static void ValidateResponse(JObject root)
        {
            if (root["Error Message"] is not null)     throw new InvalidOperationException((string)root["Error Message"]!);
            if (root["Note"] is not null)              throw new InvalidOperationException((string)root["Note"]!);
            if (root["Information"] is not null)       throw new InvalidOperationException((string)root["Information"]!);
        }

        #endregion
    }
}
