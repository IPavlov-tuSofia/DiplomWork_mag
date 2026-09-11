using System.Collections.Generic;
using System.Windows.Media;

namespace DiplomWork_Ivan_2026.Trends
{
    public class ChartSeries
    {
        public string Name { get; set; } = "";
        public List<double> Values { get; set; } = new List<double>();
        public Brush Brush { get; set; } = Brushes.White;
        public bool AllowSmoothing { get; set; } = true;
    }
}
