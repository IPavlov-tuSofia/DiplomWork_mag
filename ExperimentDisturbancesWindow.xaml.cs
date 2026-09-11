using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DiplomWork_Ivan_2026.Enums;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Services;
using DiplomWork_Ivan_2026.Simulation;
using DiplomWork_Ivan_2026.Trends;

namespace DiplomWork_Ivan_2026
{
    public partial class ExperimentDisturbancesWindow : Window
    {
        private static readonly Brush ActiveBrush =
            new SolidColorBrush(Color.FromRgb(255, 209, 102));
        private static readonly Brush NormalBrush =
            new SolidColorBrush(Color.FromRgb(0, 255, 0));

        private readonly VacuumDryerProcess _process;
        private readonly TrendBuffer _trendBuffer;
        private readonly Func<double> _getModelStep;
        private readonly Func<double> _getControllerStep;
        private readonly Func<bool> _canConfigureDiscretization;
        private readonly Action<double, double> _applyDiscretizationSteps;
        private readonly Func<bool> _canInject;
        private readonly Action<double> _applyLeak;
        private readonly Action<ExperimentalSensorTarget, SensorFaultMode>
            _applySensorFault;
        private readonly Action _clearDisturbances;
        private readonly DispatcherTimer _refreshTimer = new();

        public ExperimentDisturbancesWindow(
            VacuumDryerProcess process,
            TrendBuffer trendBuffer,
            Func<double> getModelStep,
            Func<double> getControllerStep,
            Func<bool> canConfigureDiscretization,
            Action<double, double> applyDiscretizationSteps,
            Func<bool> canInject,
            Action<double> applyLeak,
            Action<ExperimentalSensorTarget, SensorFaultMode> applySensorFault,
            Action clearDisturbances)
        {
            InitializeComponent();

            _process = process;
            _trendBuffer = trendBuffer;
            _getModelStep = getModelStep;
            _getControllerStep = getControllerStep;
            _canConfigureDiscretization = canConfigureDiscretization;
            _applyDiscretizationSteps = applyDiscretizationSteps;
            _canInject = canInject;
            _applyLeak = applyLeak;
            _applySensorFault = applySensorFault;
            _clearDisturbances = clearDisturbances;

            _refreshTimer.Interval = TimeSpan.FromMilliseconds(500);
            _refreshTimer.Tick += (_, _) => UpdateState();
            _refreshTimer.Start();

            LocalizationService.LanguageChanged +=
                LocalizationService_LanguageChanged;
            Closed += Window_Closed;

            ApplyLocalization();
            SelectCurrentDiscretizationSteps();
            SelectCurrentLeakMultiplier();
            UpdateState();
        }

        private static string L(string english, string bulgarian) =>
            LocalizationService.Text(english, bulgarian);

        private void LocalizationService_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            ApplyLocalization();
            UpdateState();
        }

        private void ApplyLocalization() =>
            LocalizationService.ApplyStaticText(this);

        private void UpdateState()
        {
            bool canInject = _canInject();
            bool canConfigureDiscretization =
                _canConfigureDiscretization();
            ApplyDiscretizationButton.IsEnabled =
                canConfigureDiscretization;
            ApplyDiscretizationButton.Visibility =
                canConfigureDiscretization
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            ModelStepComboBox.IsEnabled =
                canConfigureDiscretization;
            ControllerStepComboBox.IsEnabled =
                canConfigureDiscretization;
            ApplyLeakButton.IsEnabled = canInject;
            ApplyLeakButton.Visibility = canInject
                ? Visibility.Visible
                : Visibility.Collapsed;
            ApplySensorFaultButton.IsEnabled = canInject;
            ApplySensorFaultButton.Visibility = canInject
                ? Visibility.Visible
                : Visibility.Collapsed;

            bool hasActiveDisturbance =
                _process.LeakMultiplier > 1.0 ||
                _process.HasSensorFault;
            ClearDisturbancesButton.IsEnabled = hasActiveDisturbance;
            ClearDisturbancesButton.Visibility = hasActiveDisturbance
                ? Visibility.Visible
                : Visibility.Collapsed;

            ActiveStateTextBlock.Foreground = hasActiveDisturbance
                ? ActiveBrush
                : NormalBrush;
            ActiveStateTextBlock.Text = BuildActiveStateText();

            CurrentDiscretizationTextBlock.Text =
                $"{L("Current model step", "Текущ такт на модела")}: " +
                $"{_getModelStep():0.00} s; " +
                $"{L("controller step", "такт на регулаторите")}: " +
                $"{_getControllerStep():0.00} s. " +
                L(
                    "Trend samples remain at 1.00 s.",
                    "Trend пробите остават през 1.00 s.") +
                (canConfigureDiscretization
                    ? " " + L(
                        "The values can be changed before the next batch.",
                        "Стойностите могат да се променят преди следващата партида.")
                    : " " + L(
                        "The values are locked for the current batch.",
                        "Стойностите са заключени за текущата партида."));

            if (_process.State.SafetyInterlockActive)
            {
                AvailabilityTextBlock.Text = L(
                    "Safety is tripped. Clear the experimental fault, then use Reset Safety on the main screen.",
                    "Защитата е задействана. Изчистете експерименталния отказ, след което използвайте Нулиране на защитата на главния екран.");
            }
            else if (canInject)
            {
                AvailabilityTextBlock.Text = L(
                    "Disturbance injection is enabled for the current batch.",
                    "Внасянето на смущения е разрешено за текущата партида.");
            }
            else
            {
                AvailabilityTextBlock.Text = L(
                    "Start a batch to enable disturbance injection.",
                    "Стартирайте партида, за да разрешите внасянето на смущения.");
            }
        }

        private string BuildActiveStateText()
        {
            string[] activeStates =
            {
                FormatLeakState(),
                FormatSensorState(
                    "ChamberTemperatureSensor",
                    L("Chamber temperature sensor", "Датчик за температурата в камерата"),
                    _process.ChamberTemperatureSensor.FaultMode),
                FormatSensorState(
                    "MaterialTemperatureSensor",
                    L("Material temperature sensor", "Датчик за температурата на материала"),
                    _process.MaterialTemperatureSensor.FaultMode),
                FormatSensorState(
                    "PressureSensor",
                    L("Pressure sensor", "Датчик за налягане"),
                    _process.PressureSensor.FaultMode)
            };

            string[] active = activeStates
                .Where(value => !string.IsNullOrEmpty(value))
                .ToArray();

            return active.Length == 0
                ? L(
                    "No active experimental disturbances.",
                    "Няма активни експериментални смущения.")
                : string.Join(Environment.NewLine, active);
        }

        private string FormatLeakState()
        {
            if (_process.LeakMultiplier <= 1.0)
                return "";

            string multiplier = _process.LeakMultiplier.ToString(
                "0.##",
                CultureInfo.InvariantCulture);
            double? activationTime = FindLastDisturbanceTime(
                $"VacuumLeak:x{multiplier}");
            return $"{L("Vacuum leak", "Вакуумен пропуск")} ×{multiplier}" +
                FormatActivationTime(activationTime);
        }

        private string FormatSensorState(
            string sensorCode,
            string sensorName,
            SensorFaultMode faultMode)
        {
            if (faultMode == SensorFaultMode.None)
                return "";

            double? activationTime = FindLastDisturbanceTime(
                $"{sensorCode}:{faultMode}");
            return $"{sensorName}: {FormatFaultMode(faultMode)}" +
                FormatActivationTime(activationTime);
        }

        private double? FindLastDisturbanceTime(string type)
        {
            IReadOnlyList<ExperimentDisturbance>? disturbances =
                _trendBuffer.Metadata?.Disturbances;
            if (disturbances == null)
                return null;

            for (int index = disturbances.Count - 1; index >= 0; index--)
            {
                if (string.Equals(
                    disturbances[index].Type,
                    type,
                    StringComparison.Ordinal))
                {
                    return disturbances[index].ElapsedTimeSeconds;
                }
            }

            return null;
        }

        private static string FormatActivationTime(double? elapsedSeconds)
        {
            if (!elapsedSeconds.HasValue)
                return "";

            TimeSpan elapsed = TimeSpan.FromSeconds(
                Math.Max(0.0, elapsedSeconds.Value));
            string formatted = elapsed.TotalDays >= 1.0
                ? $"{(int)elapsed.TotalDays}d {elapsed:hh\\:mm\\:ss}"
                : $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            return $" — {L("activated at", "задействано в")} {formatted}";
        }

        private static string FormatFaultMode(SensorFaultMode mode) =>
            mode switch
            {
                SensorFaultMode.Frozen => L("Frozen", "Замразен"),
                SensorFaultMode.FailedLow => L("Failed low", "Отказ ниско"),
                SensorFaultMode.FailedHigh => L("Failed high", "Отказ високо"),
                _ => L("Normal", "Нормален")
            };

        private void ApplyLeakButton_Click(object sender, RoutedEventArgs e)
        {
            if (LeakMultiplierComboBox.SelectedItem is not ComboBoxItem item ||
                !double.TryParse(
                    item.Tag?.ToString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double multiplier))
            {
                return;
            }

            _applyLeak(multiplier);
            UpdateState();
        }

        private void ApplyDiscretizationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ModelStepComboBox.SelectedItem is not
                    ComboBoxItem modelItem ||
                ControllerStepComboBox.SelectedItem is not
                    ComboBoxItem controllerItem ||
                !double.TryParse(
                    modelItem.Tag?.ToString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double modelStepSeconds) ||
                !double.TryParse(
                    controllerItem.Tag?.ToString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double controllerStepSeconds))
            {
                return;
            }

            _applyDiscretizationSteps(
                modelStepSeconds,
                controllerStepSeconds);
            SelectCurrentDiscretizationSteps();
            UpdateState();
        }

        private void ApplySensorFaultButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (SensorComboBox.SelectedItem is not ComboBoxItem sensorItem ||
                FaultModeComboBox.SelectedItem is not ComboBoxItem modeItem ||
                !Enum.TryParse(
                    sensorItem.Tag?.ToString(),
                    out ExperimentalSensorTarget sensorTarget) ||
                !Enum.TryParse(
                    modeItem.Tag?.ToString(),
                    out SensorFaultMode faultMode))
            {
                return;
            }

            _applySensorFault(sensorTarget, faultMode);
            UpdateState();
        }

        private void ClearDisturbancesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _clearDisturbances();
            LeakMultiplierComboBox.SelectedIndex = 0;
            UpdateState();
        }

        private void SelectCurrentLeakMultiplier()
        {
            int selectedIndex = _process.LeakMultiplier switch
            {
                >= 9.5 => 2,
                >= 4.5 => 1,
                _ => 0
            };
            LeakMultiplierComboBox.SelectedIndex = selectedIndex;
        }

        private void SelectCurrentDiscretizationSteps()
        {
            ModelStepComboBox.SelectedIndex =
                GetStepSelectionIndex(_getModelStep(), false);
            ControllerStepComboBox.SelectedIndex =
                GetStepSelectionIndex(_getControllerStep(), true);
        }

        private static int GetStepSelectionIndex(
            double step,
            bool includeLongControllerSteps) =>
            step switch
            {
                < 0.075 => 0,
                < 0.15 => 1,
                < 0.35 => 2,
                < 0.75 => 3,
                < 1.5 => 4,
                < 3.5 when includeLongControllerSteps => 5,
                _ when includeLongControllerSteps => 6,
                _ => 4
            };

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            _refreshTimer.Stop();
            LocalizationService.LanguageChanged -=
                LocalizationService_LanguageChanged;
        }
    }
}
