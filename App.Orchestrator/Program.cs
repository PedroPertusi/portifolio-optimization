using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace App.Orchestrator
{
    class Program
    {
        static readonly DateTime StartDate = new(2024, 8, 1);
        static readonly DateTime EndDate   = new(2024, 12, 31);
        static readonly string[] Dow30     = {
            "AAPL","AMGN","AXP","BA","CAT","CRM","CSCO","CVX","DIS","DOW",
            "GS","HD","HON","IBM","INTC","JNJ","JPM","KO","MCD","MMM",
            "MRK","MSFT","NKE","PG","TRV","UNH","V","VZ","WBA","WMT"
        };

        static async Task Main()
        {
            var service   = new StockDataService(Dow30, StartDate, EndDate, "../data/dow30.csv");
            var allStocks = await service.FetchOrLoadAsync();

            foreach (var kv in allStocks)
            {
                Console.WriteLine($"{kv.Value.Count} dias retornados para {kv.Key}:");
                foreach (var p in kv.Value)
                    Console.WriteLine($"{p.Date:yyyy-MM-dd}: {p.Price}");
            }
        }
    }
}
