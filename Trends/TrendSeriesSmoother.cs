using System;
using System.Collections.Generic;
using System.Linq;

namespace DiplomWork_Ivan_2026.Trends
{
    /// <summary>
    /// Creates chart-only copies of trend series. The stored trend history is
    /// never changed, so operators can switch back to the raw measurements.
    /// </summary>
    public static class TrendSeriesSmoother
    {
        private const int SmoothingRadius = 2;

        public static List<ChartSeries> CreateDisplaySeries(
            IEnumerable<ChartSeries> source,
            bool smooth)
        {
            return source.Select(series => new ChartSeries
            {
                Name = series.Name,
                Brush = series.Brush,
                AllowSmoothing = series.AllowSmoothing,
                Values = smooth && series.AllowSmoothing
                    ? SmoothValues(series.Values)
                    : new List<double>(series.Values)
            }).ToList();
        }

        private static List<double> SmoothValues(IReadOnlyList<double> values)
        {
            if (values.Count < 3)
                return new List<double>(values);

            List<double> result = new List<double>(values.Count);

            for (int index = 0; index < values.Count; index++)
            {
                double weightedSum = 0.0;
                double weightSum = 0.0;

                int first = Math.Max(0, index - SmoothingRadius);
                int last = Math.Min(values.Count - 1, index + SmoothingRadius);

                for (int sample = first; sample <= last; sample++)
                {
                    // Triangular weights preserve the local shape better than
                    // an unweighted moving average while suppressing noise.
                    double weight = SmoothingRadius + 1 - Math.Abs(sample - index);
                    weightedSum += values[sample] * weight;
                    weightSum += weight;
                }

                result.Add(weightedSum / weightSum);
            }

            return result;
        }
    }
}
