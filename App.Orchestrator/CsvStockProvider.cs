using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace App.Orchestrator
{
    public static class CsvStockProvider
    {
        /// Reads CSV with header and returns a Dictionary<symbol, series>.
        public static Dictionary<string, List<EquityPrice>> Load(string path)
        {
            var lines  = File.ReadAllLines(path);
            var header = lines[0].Split(',').Skip(1).ToArray();
            var dict   = header.ToDictionary(t => t, _ => new List<EquityPrice>());

            foreach (var row in lines.Skip(1))
            {
                var cols = row.Split(',');
                if (!DateTime.TryParse(cols[0], out var dt)) continue;

                for (int i = 1; i < cols.Length; i++)
                {
                    if (double.TryParse(cols[i],
                                        NumberStyles.Any,
                                        CultureInfo.InvariantCulture,
                                        out var price))
                    {
                        dict[header[i - 1]].Add(new EquityPrice {
                            Date   = dt,
                            Ticker = header[i - 1],
                            Price  = price
                        });
                    }
                }
            }

            // sort each series by date
            foreach (var list in dict.Values)
                list.Sort((a, b) => a.Date.CompareTo(b.Date));

            return dict;
        }
    }
}
