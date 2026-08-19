using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ExoplanetHunter
{
    public static class LightCurveDataLoader
    {
        public static List<LightCurveData> LoadCsv(string filePath, int? maxRows = null)
        {
            var results = new List<LightCurveData>();

            using (var reader = new StreamReader(filePath))
            {
                string? headerLine = reader.ReadLine(); // skip the header row (LABEL, FLUX.1, FLUX.2...)
                int id = 0;

                string? line;
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

        // Shuffles the dataset randomly — important because in this CSV,
        // all exoplanet rows are grouped at the top, so taking the "first N"
        // rows would badly skew our training data.
        public static List<LightCurveData> Shuffle(List<LightCurveData> data, int seed = 42)
        {
            var random = new Random(seed);
            return data.OrderBy(_ => random.Next()).ToList();
        }

        // Splits shuffled data into training and testing sets.
        // testFraction=0.2 means 20% held out for testing, 80% for training.
        public static (List<LightCurveData> train, List<LightCurveData> test) SplitTrainTest(
            List<LightCurveData> data, double testFraction = 0.2, int seed = 42)
        {
            var shuffled = Shuffle(data, seed);
            int testCount = (int)(shuffled.Count * testFraction);

            return (
                train: shuffled.Skip(testCount).ToList(),
                test: shuffled.Take(testCount).ToList()
            );
        }
    }
}