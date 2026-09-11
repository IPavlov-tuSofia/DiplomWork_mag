using System;
using System.Collections.Generic;
using System.Linq;

namespace DiplomWork_Ivan_2026.Trends
{
    public static class TrendPointDownsampler
    {
        public static List<TrendPoint> SelectTimeRange(
            IReadOnlyList<TrendPoint> points,
            double? timeRangeSeconds)
        {
            if (points.Count == 0)
                return new List<TrendPoint>();

            if (timeRangeSeconds == null)
                return points.ToList();

            double cutoffTime = points[points.Count - 1].Time - timeRangeSeconds.Value;
            int firstIndex = FindFirstIndexAtOrAfter(points, cutoffTime);

            List<TrendPoint> result = new List<TrendPoint>(points.Count - firstIndex);
            for (int i = firstIndex; i < points.Count; i++)
            {
                result.Add(points[i]);
            }

            return result;
        }

        public static List<TrendPoint> DownsampleMinMax(
            IReadOnlyList<TrendPoint> points,
            IReadOnlyList<Func<TrendPoint, double>> valueSelectors,
            int maxPoints)
        {
            if (maxPoints < 2)
                throw new ArgumentOutOfRangeException(nameof(maxPoints));

            if (points.Count <= maxPoints)
                return points.ToList();

            int selectorCount = Math.Max(1, valueSelectors.Count);
            int bucketCount = Math.Max(1, (maxPoints - 2) / (2 * selectorCount));
            double bucketSize = (points.Count - 2) / (double)bucketCount;

            SortedSet<int> selectedIndices = new SortedSet<int>
            {
                0,
                points.Count - 1
            };

            for (int bucket = 0; bucket < bucketCount; bucket++)
            {
                int start = 1 + (int)Math.Floor(bucket * bucketSize);
                int endExclusive = 1 + (int)Math.Floor((bucket + 1) * bucketSize);
                endExclusive = Math.Min(endExclusive, points.Count - 1);

                if (endExclusive <= start)
                    endExclusive = Math.Min(start + 1, points.Count - 1);

                foreach (Func<TrendPoint, double> selector in valueSelectors)
                {
                    int minIndex = start;
                    int maxIndex = start;
                    double minValue = selector(points[start]);
                    double maxValue = minValue;

                    for (int i = start + 1; i < endExclusive; i++)
                    {
                        double value = selector(points[i]);

                        if (value < minValue)
                        {
                            minValue = value;
                            minIndex = i;
                        }

                        if (value > maxValue)
                        {
                            maxValue = value;
                            maxIndex = i;
                        }
                    }

                    selectedIndices.Add(minIndex);
                    selectedIndices.Add(maxIndex);
                }
            }

            return selectedIndices
                .Take(maxPoints)
                .Select(index => points[index])
                .ToList();
        }

        private static int FindFirstIndexAtOrAfter(
            IReadOnlyList<TrendPoint> points,
            double targetTime)
        {
            int low = 0;
            int high = points.Count;

            while (low < high)
            {
                int middle = low + (high - low) / 2;

                if (points[middle].Time < targetTime)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }
    }
}
