using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ExoplanetHunter
{
    public class LightCurveFeatures
    {
        public float MeanFlux { get; set; }
        public float MinFlux { get; set; }
        public float StdDevFlux { get; set; }
        public float DipDepth { get; set; }
        public float DipCount { get; set; }
        public float DipSymmetryScore { get; set; }
        public float PeriodicityScore { get; set; }
        public bool Label { get; set; }
        public float Weight { get; set; } = 1f;

        public static LightCurveFeatures Extract(float[] processedFlux, bool label)
        {
            float mean = processedFlux.Average();
            float min = processedFlux.Min();

            float stdDev = (float)Math.Sqrt(
                processedFlux.Select(v => Math.Pow(v - mean, 2)).Average()
            );

            float dipThreshold = mean - 1.5f * stdDev;
            var dipIndices = processedFlux
                .Select((v, i) => (value: v, index: i))
                .Where(p => p.value < dipThreshold)
                .Select(p => p.index)
                .ToList();

            float dipDepth = mean - min;
            int dipCount = CountDipGroups(dipIndices);
            float symmetryScore = CalculateSymmetry(processedFlux, min);
            float periodicityScore = CalculatePeriodicity(dipIndices);

            return new LightCurveFeatures
            {
                MeanFlux = mean,
                MinFlux = min,
                StdDevFlux = stdDev,
                DipDepth = dipDepth,
                DipCount = dipCount,
                DipSymmetryScore = symmetryScore,
                PeriodicityScore = periodicityScore,
                Label = label
            };
        }

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

        // Measures how REGULARLY SPACED the dips are. Real planetary transits
        // repeat at a consistent interval (the orbital period); random noise
        // dips don't. High score = very regular spacing = more planet-like.
        private static float CalculatePeriodicity(List<int> dipIndices)
        {
            if(dipIndices.Count < 3) return 0f; // need at least 2 gaps to compare

            var gaps = new List<int>();
            for (int i = 1; i < dipIndices.Count; i++)
            {
                int gap = dipIndices[i] - dipIndices[i - 1];
                if (gap > 1) gaps.Add(gap); // ignore adjacent points within the same dip group
            }

            if (gaps.Count < 2) return 0f;

            float meanGap =  (float)gaps.Average();
            float gapStdDev = (float)Math.Sqrt(gaps.Select(g =>Math.Pow( g - meanGap, 2)).Average());

            // Low relative variation in gap size = high periodicity score.
            // We invert it so higher score = more regular = more planet-like.
            float coefficientOfVariation = meanGap > 0 ? gapStdDev / meanGap : 1f;
            return 1f / (1f + coefficientOfVariation);
        }
        
    }
}