using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DiplomWork_Ivan_2026.Services;

namespace DiplomWork_Ivan_2026.Trends
{
    public static class TrendChartDataBuilder
    {
        public const int MaxRenderedPoints = 1_200;
        private static string L(string en, string bg) => LocalizationService.Text(en, bg);

        public static TrendChartData Build(
            IReadOnlyList<TrendPoint> allPoints,
            int selectedTrendIndex,
            double? timeRangeSeconds)
        {
            if (allPoints.Count == 0)
                return new TrendChartData();

            List<TrendPoint> rangePoints = TrendPointDownsampler.SelectTimeRange(
                allPoints,
                timeRangeSeconds);
            List<TrendPoint> points = TrendPointDownsampler.DownsampleMinMax(
                rangePoints,
                GetValueSelectors(selectedTrendIndex),
                MaxRenderedPoints);

            TrendPoint last = allPoints[allPoints.Count - 1];

            TrendChartData data = new TrendChartData
            {
                XValues = points.Select(p => p.Time).ToList()
            };

            switch (selectedTrendIndex)
            {
                case 0:
                    data.Title = L("Temperature [°C]", "Температура [°C]");
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Chamber Temperature", "Температура в камерата"),
                        Values = points.Select(p => p.Temperature).ToList(),
                        Brush = Brushes.OrangeRed
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Material Temperature", "Температура на материала"),
                        Values = points.Select(p => p.MaterialTemperature).ToList(),
                        Brush = Brushes.Gold
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Temperature Setpoint", "Задание за температура"),
                        Values = points.Select(p => p.TemperatureSetpoint).ToList(),
                        Brush = Brushes.DeepSkyBlue,
                        AllowSmoothing = false
                    });
                    data.CurrentText =
                        $"{L("Chamber", "Камера")}: {last.Temperature:F1} °C   " +
                        $"{L("Material", "Материал")}: {last.MaterialTemperature:F1} °C   " +
                        $"{L("Setpoint", "Задание")}: {last.TemperatureSetpoint:F1} °C";
                    break;

                case 1:
                    data.Title = L("Pressure [kPa]", "Налягане [kPa]");
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Pressure", "Налягане"),
                        Values = points.Select(p => p.Pressure).ToList(),
                        Brush = Brushes.DeepSkyBlue
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Pressure Setpoint", "Задание за налягане"),
                        Values = points.Select(p => p.PressureSetpoint).ToList(),
                        Brush = Brushes.Gold,
                        AllowSmoothing = false
                    });
                    data.CurrentText =
                        $"{L("Pressure", "Налягане")}: {last.Pressure:F1} kPa   " +
                        $"{L("Setpoint", "Задание")}: {last.PressureSetpoint:F1} kPa";
                    break;

                case 2:
                    data.Title = L("Moisture (wet basis) / Humidity [%]", "Влага (мокра база) / Влажност [%]");
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Material Moisture", "Влага на материала"),
                        Values = points.Select(p => p.MaterialMoisture).ToList(),
                        Brush = Brushes.LimeGreen
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Dynamic Equilibrium Moisture", "Динамична равновесна влага"),
                        Values = points.Select(p => p.EquilibriumMoisture).ToList(),
                        Brush = Brushes.Gold
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Chamber Relative Humidity", "Относителна влажност в камерата"),
                        Values = points.Select(p => p.AirHumidity).ToList(),
                        Brush = Brushes.DeepSkyBlue
                    });
                    data.CurrentText =
                        $"{L("Material Moisture", "Влага на материала")}: {last.MaterialMoisture:F1} % wb   " +
                        $"{L("Equilibrium", "Равновесна")}: {last.EquilibriumMoisture:F1} % wb   " +
                        $"{L("RH", "ОВ")}: {last.AirHumidity:F1} %";
                    break;

                case 3:
                    data.Title = L("Drying Rate [% wb/min]", "Скорост на сушене [% wb/min]");
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Drying Rate", "Скорост на сушене"),
                        Values = points.Select(p => p.DryingRate).ToList(),
                        Brush = Brushes.Gold
                    });
                    data.CurrentText = $"{L("Drying Rate", "Скорост на сушене")}: {last.DryingRate:F3} % wb/min";
                    break;

                case 4:
                    data.Title = L("Actuators [%]", "Изпълнителни механизми [%]");
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Heater Power", "Мощност на нагревателя"),
                        Values = points.Select(p => p.HeaterPower).ToList(),
                        Brush = Brushes.OrangeRed,
                        AllowSmoothing = false
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Pump Power", "Мощност на помпата"),
                        Values = points.Select(p => p.PumpPower).ToList(),
                        Brush = Brushes.DeepSkyBlue,
                        AllowSmoothing = false
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Vent Valve", "Вентилационен клапан"),
                        Values = points.Select(p => p.VentValveOpening).ToList(),
                        Brush = Brushes.Gold,
                        AllowSmoothing = false
                    });
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Fan Speed", "Скорост на вентилатора"),
                        Values = points.Select(p => p.FanSpeed).ToList(),
                        Brush = Brushes.LimeGreen,
                        AllowSmoothing = false
                    });
                    data.CurrentText =
                        $"{L("Heater", "Нагревател")}: {last.HeaterPower:F0} %   " +
                        $"{L("Pump", "Помпа")}: {last.PumpPower:F0} %   " +
                        $"{L("Vent", "Клапан")}: {last.VentValveOpening:F0} %   " +
                        $"{L("Fan", "Вентилатор")}: {last.FanSpeed:F0} %";
                    break;

                case 5:
                    data.Title = L("Energy", "Енергия");
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Total Energy", "Обща енергия"),
                        Values = points.Select(p => p.TotalEnergyKWh).ToList(),
                        Brush = Brushes.MediumPurple
                    });
                    data.CurrentText = $"{L("Total Energy", "Обща енергия")}: {last.TotalEnergyKWh:F3} kWh";
                    break;

                default:
                    data.Title = L("Temperature [°C]", "Температура [°C]");
                    data.Series.Add(new ChartSeries
                    {
                        Name = L("Chamber Temperature", "Температура в камерата"),
                        Values = points.Select(p => p.Temperature).ToList(),
                        Brush = Brushes.OrangeRed
                    });
                    data.CurrentText = $"{L("Chamber", "Камера")}: {last.Temperature:F1} °C";
                    break;
            }

            return data;
        }

        private static IReadOnlyList<Func<TrendPoint, double>> GetValueSelectors(
            int selectedTrendIndex)
        {
            return selectedTrendIndex switch
            {
                0 => new Func<TrendPoint, double>[]
                {
                    point => point.Temperature,
                    point => point.MaterialTemperature,
                    point => point.TemperatureSetpoint
                },
                1 => new Func<TrendPoint, double>[]
                {
                    point => point.Pressure,
                    point => point.PressureSetpoint
                },
                2 => new Func<TrendPoint, double>[]
                {
                    point => point.MaterialMoisture,
                    point => point.EquilibriumMoisture,
                    point => point.AirHumidity
                },
                3 => new Func<TrendPoint, double>[]
                {
                    point => point.DryingRate
                },
                4 => new Func<TrendPoint, double>[]
                {
                    point => point.HeaterPower,
                    point => point.PumpPower,
                    point => point.VentValveOpening,
                    point => point.FanSpeed
                },
                5 => new Func<TrendPoint, double>[]
                {
                    point => point.TotalEnergyKWh
                },
                _ => new Func<TrendPoint, double>[]
                {
                    point => point.Temperature
                }
            };
        }
    }
}
