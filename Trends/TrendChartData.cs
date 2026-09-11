using System.Collections.Generic;

namespace DiplomWork_Ivan_2026.Trends
{
    public class TrendChartData
    {
        public string Title { get; set; } = "";
        public string CurrentText { get; set; } = "";
        public List<double> XValues { get; set; } = new List<double>();
        public List<ChartSeries> Series { get; set; } = new List<ChartSeries>();
    }
}
