using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Simulation;
using DiplomWork_Ivan_2026.Services;

namespace DiplomWork_Ivan_2026
{
    public partial class ProcessDetailsWindow : Window
    {
        private readonly VacuumDryerProcess _process;
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();

        public ProcessDetailsWindow(VacuumDryerProcess process)
        {
            InitializeComponent();

            _process = process;

            _refreshTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
            ApplyLocalization();

            UpdateDetails();
        }

        private static string L(string en, string bg) => LocalizationService.Text(en, bg);

        private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
        {
            ApplyLocalization();
            UpdateDetails();
        }

        private void ApplyLocalization() => LocalizationService.ApplyStaticText(this);

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            UpdateDetails();
        }

        private void UpdateDetails()
        {
            var state = _process.State;
            var material = _process.SelectedMaterial;

            ChamberTemperatureTextBlock.Text =
                $"{L("Chamber Temperature", "Температура в камерата")}: {state.MeasuredTemperature:F1} °C";

            MaterialTemperatureTextBlock.Text =
                $"{L("Material Temperature", "Температура на материала")}: {state.MeasuredMaterialTemperature:F1} °C";

            TemperatureSetpointTextBlock.Text =
                $"{L("Temperature Setpoint", "Задание за температура")}: {state.ActiveTemperatureSetpoint:F1} °C";

            PressureTextBlock.Text =
                $"{L("Pressure", "Налягане")}: {state.MeasuredPressure:F1} kPa";

            PressureSetpointTextBlock.Text =
                $"{L("Pressure Setpoint", "Задание за налягане")}: {state.ActivePressureSetpoint:F1} kPa";

            VacuumLevelTextBlock.Text =
                $"{L("Vacuum Level", "Ниво на вакуум")}: {state.VacuumLevel:F1} %";

            AirHumidityTextBlock.Text =
                $"{L("Relative Humidity", "Относителна влажност")}: {state.AirHumidity:F1} %";

            VaporPressureTextBlock.Text =
                $"{L("Water Vapor Partial Pressure", "Парциално налягане на водните пари")}: {state.WaterVaporPartialPressureKPa:F2} kPa";

            MaterialMoistureTextBlock.Text =
                $"{L("Material Moisture", "Влага на материала")}: {state.MaterialMoistureWetBasisPercent:F1} % wb " +
                $"(X = {state.MaterialMoistureDryBasis:F3} kg/kg db)";

            EquilibriumMoistureTextBlock.Text =
                $"{L("Dynamic Equilibrium Moisture", "Динамична равновесна влага")}: " +
                $"{DryingMaterial.DryBasisToWetBasisPercent(state.DynamicEquilibriumMoistureDryBasis):F1} % wb";

            MoistureRatioTextBlock.Text =
                $"{L("Moisture Ratio", "Отношение на влагата")}: {state.MoistureRatio:F3}";

            DryingRateTextBlock.Text =
                $"{L("Drying Rate", "Скорост на сушене")}: {state.DryingRateWetBasisPercentPerMinute:F3} % wb/min";

            AirFlowRateTextBlock.Text =
                $"{L("Air Flow Rate", "Въздушен дебит")}: {state.AirFlowRate:F1} m³/h";

            TotalEnergyTextBlock.Text =
                $"{L("Total Energy", "Обща енергия")}: {state.TotalEnergyKWh:F3} kWh";

            EvaporatedWaterTextBlock.Text =
                $"{L("Evaporated Water", "Изпарена вода")}: {state.EvaporatedWaterKg:F2} kg";

            PumpedVaporTextBlock.Text =
                $"{L("Pumped Water Vapor", "Отведени водни пари")}: {state.PumpedWaterVaporKg:F2} kg";

            CondensedWaterTextBlock.Text =
                $"{L("Condensed Water", "Кондензирана вода")}: {state.CondensedWaterKg:F2} kg";

            VaporBalanceTextBlock.Text =
                $"{L("Water Vapor Balance Residual", "Остатък от баланса на водните пари")}: " +
                $"{state.WaterVaporMassBalanceResidualKg:F4} kg";

            EfficiencyTextBlock.Text =
                $"{L("Efficiency", "Ефективност")}: {state.EfficiencyKgPerKWh:F2} kg/kWh";

            ElapsedTimeTextBlock.Text =
                $"{L("Elapsed Time", "Изминало време")}: {state.ElapsedTime:F0} s";

            RemainingTimeTextBlock.Text =
                $"{L("Estimated Remaining Time", "Оставащо време")}: {FormatRemainingTime(state.EstimatedRemainingTimeSeconds)}";

            HeaterPowerTextBlock.Text =
                $"{L("Heater Power", "Мощност на нагревателя")}: {state.HeaterPower:F0} %";

            PumpPowerTextBlock.Text =
                $"{L("Vacuum Pump Power", "Мощност на вакуумната помпа")}: {state.VacuumPumpPower:F0} %";

            VentValveTextBlock.Text =
                $"{L("Vent Valve Opening", "Отваряне на вентилационния клапан")}: {state.VentValveOpening:F0} %";

            FanSpeedTextBlock.Text =
                $"{L("Fan Speed", "Скорост на вентилатора")}: {state.FanSpeed:F0} %";

            ProcessStageTextBlock.Text =
                $"{L("Process Stage", "Етап на процеса")}: {LocalizeStage(state.ProcessStage)}";

            SensorStatusTextBlock.Text =
                _process.HasSensorFault
                    ? L("Sensors: FAULT", "Датчици: ПОВРЕДА")
                    : L("Sensors: OK", "Датчици: OK");
            SensorStatusTextBlock.Foreground = _process.HasSensorFault
                ? Brushes.Red
                : Brushes.Lime;

            ProcessStatusTextBlock.Text =
                state.SafetyInterlockActive
                    ? L("Process Status: Safety trip", "Статус на процеса: Задействана защита")
                    : state.IsCompleted
                        ? L("Process Status: Completed", "Статус на процеса: Завършен")
                        : L("Process Status: Available", "Статус на процеса: Готов");
            ProcessStatusTextBlock.Foreground = state.SafetyInterlockActive
                ? Brushes.Red
                : state.IsCompleted ? Brushes.DeepSkyBlue : Brushes.Lime;

            if (material != null)
            {
                MaterialNameTextBlock.Text =
                    $"{L("Material", "Материал")}: {material}";

                InitialMoistureTextBlock.Text =
                    $"{L("Initial Moisture", "Начална влага")}: {material.InitialMoistureWetBasisPercent:F1} % wb";

                TargetMoistureTextBlock.Text =
                    $"{L("Target Moisture", "Целева влага")}: {material.TargetMoistureWetBasisPercent:F1} % wb";

                MaxTemperatureTextBlock.Text =
                    $"{L("Max Temperature", "Максимална температура")}: {material.MaxTemperature:F1} °C";

                DryingCoefficientTextBlock.Text =
                    $"{L("Drying Coefficient", "Коефициент на сушене")}: {material.DryingCoefficient:F2}";

                MaterialMassTextBlock.Text =
                    $"{L("Initial Wet Mass", "Начална мокра маса")}: {material.InitialWetMassKg:F1} kg " +
                    $"({L("Dry Mass", "Суха маса")}: {material.DryMassKg:F2} kg)";
            }
            else
            {
                MaterialNameTextBlock.Text = L("Material: -", "Материал: -");
                InitialMoistureTextBlock.Text = L("Initial Moisture: -", "Начална влага: -");
                TargetMoistureTextBlock.Text = L("Target Moisture: -", "Целева влага: -");
                MaxTemperatureTextBlock.Text = L("Max Temperature: -", "Максимална температура: -");
                DryingCoefficientTextBlock.Text = L("Drying Coefficient: -", "Коефициент на сушене: -");
                MaterialMassTextBlock.Text = L("Material Mass: -", "Маса на материала: -");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string FormatRemainingTime(double? seconds)
        {
            if (!seconds.HasValue)
                return L("calculating...", "изчислява се...");

            TimeSpan remaining = TimeSpan.FromSeconds(Math.Max(0.0, seconds.Value));
            if (remaining.TotalDays >= 1.0)
                return LocalizationService.IsBulgarian
                    ? $"{(int)remaining.TotalDays}д {remaining.Hours:D2}ч"
                    : $"{(int)remaining.TotalDays}d {remaining.Hours:D2}h";

            return $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private static string LocalizeStage(Enums.ProcessStage stage) => stage switch
        {
            Enums.ProcessStage.Preheating => L("Preheating", "Предварително нагряване"),
            Enums.ProcessStage.Evacuation => L("Evacuation", "Вакуумиране"),
            Enums.ProcessStage.Drying => L("Drying", "Сушене"),
            Enums.ProcessStage.FinalDrying => L("Final drying", "Финално сушене"),
            Enums.ProcessStage.Venting => L("Pressure recovery", "Възстановяване на налягането"),
            Enums.ProcessStage.Manual => L("Manual control", "Ръчно управление"),
            Enums.ProcessStage.SafetyShutdown => L("Safety shutdown", "Аварийно изключване"),
            Enums.ProcessStage.Completed => L("Completed", "Завършен"),
            _ => L("Idle", "Готовност")
        };

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer.Stop();
            LocalizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            base.OnClosed(e);
        }

    }
}
