using System;
using System.Collections.Generic;
using System.Linq;

namespace ExoplanetHunter
{
    // These 6 numbers are what the ML model will actually train on —
    // not the raw 200-point curve, but a summary of its "shape."
    public class LightCurveFeatures
    {
        public float MeanFlux { get; set; }
        public float MinFlux { get; set; }
        public float StdDevFlux { get; set; }
        public float DipDepth { get; set; }         // how far the deepest dip drops below average
        public int DipCount { get; set; }            // how many separate dip events were found
        public float DipSymmetryScore { get; set; }  // how symmetric the deepest dip is
        public bool Label { get; set; }               // true = exoplanet, false = not (what we're trying to predict)

        public static LightCurveFeatures Extract(float[] processedFlux, bool label)
        {
            float mean = processedFlux.Average();
            float min = processedFlux.Min();

            // Standard deviation: measures how "spread out" the values are.
            // A noisy, jittery curve has high std dev; a flat, stable one has low std dev.
            float stdDev = (float)Math.Sqrt(
                processedFlux.Select(v => Math.Pow(v - mean, 2)).Average()
            );

            // A "dip" = any point sitting more than 1.5 standard deviations below the mean.
            // This threshold is a judgment call — too strict and we miss subtle dips,
            // too loose and normal noise gets counted as a dip.
            float dipThreshold = mean - 1.5f * stdDev;
            var dipIndices = processedFlux
                .Select((v, i) => (value: v, index: i))
                .Where(p => p.value < dipThreshold)
                .Select(p => p.index)
                .ToList();

            float dipDepth = mean - min;
            int dipCount = CountDipGroups(dipIndices);
            float symmetryScore = CalculateSymmetry(processedFlux, min);

            return new LightCurveFeatures
            {
                MeanFlux = mean,
                MinFlux = min,
                StdDevFlux = stdDev,
                DipDepth = dipDepth,
                DipCount = dipCount,
                DipSymmetryScore = symmetryScore,
                Label = label
            };
        }

        // Groups consecutive dip indices together so one wide dip doesn't
        // get miscounted as several separate dips.
        private static int CountDipGroups(List<int> dipIndices)
        {
            if (dipIndices.Count == 0) return 0;

            int groups = 1;
            for (int i = 1; i < dipIndices.Count; i++)
            {
                if (dipIndices[i] - dipIndices[i - 1] > 1) groups++;
            }
            return groups;
        }

        // Compares the average flux just BEFORE the deepest point vs just AFTER it.
        // A real transit dips down and comes back up in a roughly mirrored pattern,
        // so a high score (close to 1) suggests a real transit; a low score suggests
        // random noise or an asymmetric event (like a flare).
        private static float CalculateSymmetry(float[] flux, float minValue)
        {
            if (flux.Length <= 10) return 0f;

            int minIdx = Array.IndexOf(flux, minValue);
            int span = Math.Min(5, Math.Min(minIdx, flux.Length - 1 - minIdx));

            if (span <= 0) return 0f;

            float before = flux.Skip(minIdx - span).Take(span).Average();
            float after = flux.Skip(minIdx + 1).Take(span).Average();

            return 1f - Math.Abs(before - after);
        }
    }
}