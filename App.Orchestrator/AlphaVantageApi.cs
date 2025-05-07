using System.Net.Http;
using System.Threading.Tasks;

namespace App.Orchestrator
{
    public class AlphaVantageApi
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string _apiKey;

        public AlphaVantageApi(string apiKey) => _apiKey = apiKey;

        public async Task<string> GetGlobalQuoteAsync(string symbol)
        {
            var url =
                $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }
    }
}
