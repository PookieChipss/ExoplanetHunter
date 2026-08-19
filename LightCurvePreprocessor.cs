using System;
using System.Linq;

namespace ExoplanetHunter
{
    public static class LightCurvePreprocessor
    {
        // Scales flux values to a 0-1 range so different stars' brightness
        // scales don't confuse the model — we only care about the SHAPE
        // of the curve, not the absolute brightness.
        public static float[] Normalize(float[] flux)
        {
            float min = flux.Min();
            float max = flux.Max();
            float range = max - min;

            if (range == 0) return flux.Select(_ => 0f).ToArray();

            return flux.Select(v => (v - min) / range).ToArray();
        }

        // Simple moving-average smoothing to reduce instrument noise
        // while preserving the overall dip shape.
        public static float[] SmoothMovingAverage(float[] flux, int windowSize = 5)
        {
            if (windowSize <= 1 || flux.Length <= windowSize) return flux;

            var result = new float[flux.Length];
            int half = windowSize / 2;

            for (int i = 0; i < flux.Length; i++)
            {
                int start = Math.Max(0, i - half);
                int end = Math.Min(flux.Length - 1, i + half);
                float sum = 0;
                int count = 0;

                for (int j = start; j <= end; j++)
                {
                    sum += flux[j];
                    count++;
                }

                result[i] = sum / count;
            }

            return result;
        }

        // Shrinks a long series by averaging chunks together —
        // 3197 points is more detail than we need for a first working model.
        public static float[] Downsample(float[] flux, int targetLength)
        {
            if (flux.Length <= targetLength) return flux;

            var result = new float[targetLength];
            double chunkSize = (double)flux.Length / targetLength;

            for (int i = 0; i < targetLength; i++)
            {
                int start = (int)(i * chunkSize);
                int end = (int)Math.Min(flux.Length, (i + 1) * chunkSize);
                if (end <= start) end = start + 1;

                result[i] = flux.Skip(start).Take(end - start).Average();
            }

            return result;
        }

        // Runs the full pipeline in order: smooth first (reduce noise),
        // then normalize (scale 0-1), then downsample (shrink size).
        public static float[] Process(float[] rawFlux, int windowSize = 5, int targetLength = 200)
        {
            var smoothed = SmoothMovingAverage(rawFlux, windowSize);
            var normalized = Normalize(smoothed);
            var downsampled = Downsample(normalized, targetLength);
            return downsampled;
        }
    }
}