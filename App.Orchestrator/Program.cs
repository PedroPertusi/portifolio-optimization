using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;    // ← necessário para JObject

namespace App.Orchestrator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var apiKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY")
                         ?? throw new Exception("Defina ALPHAVANTAGE_API_KEY");

            var api  = new AlphaVantageApi(apiKey);
            string json;
            try
            {
                json = await api.GetGlobalQuoteAsync("AAPL");  // só AAPL por enquanto
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na requisição: {ex.Message}");
                return;                                       // sai imediatamente
            }

            // primeiro, veja o JSON bruto para debug
            Console.WriteLine("JSON RECEBIDO:\n" + json);

            var root = JObject.Parse(json);

            // 1) checa se veio um "Error Message"
            if (root["Error Message"] != null)
            {
                Console.WriteLine("Erro da API: " + (string)root["Error Message"]);
                return;
            }

            // 2) pega o nó Global Quote
            var gq = root["Global Quote"];
            if (gq == null)
            {
                Console.WriteLine("Resposta inesperada: campo 'Global Quote' não encontrado.");
                return;
            }

            // 3) extrai os campos
            var symbol    = (string)gq["01. symbol"]!;           
            var price     = (string)gq["05. price"]!;            
            var tradeDate = (string)gq["07. latest trading day"]!;

            Console.WriteLine($"Symbol: {symbol}, Price: {price}, Date: {tradeDate}");
        }
    }
}
