using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomWork_Ivan_2026.Models
{
    public class ProcessSettings
    {
        public double TemperatureSetpoint { get; set; } = 60.0;  // °C
        public double PressureSetpoint { get; set; } = 30.0;     // kPa
        public double TemperatureHysteresis { get; set; } = 2.0;

        public double AmbientTemperature { get; set; } = 20.0;
        public double AmbientPressure { get; set; } = 101.3;
        public double AmbientRelativeHumidityPercent { get; set; } = 50.0;
    }
}
