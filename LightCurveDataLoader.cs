using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace ExoplanetHunter
{
    public static class LightCurveDataLoader
    {
        public static List<LightCurveData> LoadCsv(string filePath, int? maxRows = null)
        {
            var results = new List<LightCurveData>();

            using (var reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine(); // skip the header row (LABEL, FLUX.1, FLUX.2...)
                int id = 0;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (maxRows.HasValue && id >= maxRows.Value) break;

                    var parts = line.Split(',');

                    int label = int.Parse(parts[0], CultureInfo.InvariantCulture);

                    var flux = new float[parts.Length - 1];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        flux[i - 1] = float.Parse(parts[i], CultureInfo.InvariantCulture);
                    }

                    results.Add(new LightCurveData
                    {
                        Id = id++,
                        Flux = flux,
                        IsExoplanet = label == 2
                    });
                }
            }

            return results;
        }
    }
}