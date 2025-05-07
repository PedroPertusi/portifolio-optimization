using System.Net.Http;
using System.Threading.Tasks;

namespace App.Orchestrator
{
    public class AlphaVantageApi
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string _apiKey;
        public AlphaVantageApi(string apiKey) => _apiKey = apiKey;

        public async Task<string> GetDailyTimeSeriesAsync(string symbol)
        {
            // agora usamos TIME_SERIES_DAILY (gratuito)
            var url =
            $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY" +
            $"&symbol={symbol}&outputsize=full&apikey={_apiKey}";
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }
    }
}
