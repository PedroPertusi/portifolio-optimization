using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace App.Orchestrator
{
    public class StockDataService
    {
        private readonly AlphaVantageApi _api;
        private readonly string[] _symbols;
        private readonly string _csvPath;

        public StockDataService(string[] symbols, DateTime start, DateTime end, string csvPath)
        {
            _symbols = symbols;
            _csvPath = csvPath;
            _api     = AlphaVantageApi.CreateFromEnv(start, end);
        }

        /// Try CSV first; if missing or API errors, fetch via API.
        public async Task<Dictionary<string, List<EquityPrice>>> FetchOrLoadAsync()
        {
            if (File.Exists(_csvPath))
                return CsvStockProvider.Load(_csvPath);

            try
            {
                var result = new Dictionary<string, List<EquityPrice>>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in _symbols)
                    result[s] = await _api.GetDailySeriesAsync(s).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API failed ({ex.Message}), falling back to CSV");
                return File.Exists(_csvPath)
                    ? CsvStockProvider.Load(_csvPath)
                    : new Dictionary<string, List<EquityPrice>>();
            }
        }
    }
}
