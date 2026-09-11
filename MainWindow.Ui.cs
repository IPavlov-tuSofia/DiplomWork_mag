using System.Linq;
using System.Windows;
using System.Windows.Media;
using DiplomWork_Ivan_2026.Enums;
using DiplomWork_Ivan_2026.Services;

namespace DiplomWork_Ivan_2026
{
    public partial class MainWindow
    {
        private static readonly Brush SemanticRed =
            new SolidColorBrush(Color.FromRgb(211, 47, 47));
        private static readonly Brush SemanticAmber =
            new SolidColorBrush(Color.FromRgb(249, 168, 37));
        private static readonly Brush SemanticGreen =
            new SolidColorBrush(Color.FromRgb(0, 255, 0));
        private static readonly Brush SemanticBlue =
            new SolidColorBrush(Color.FromRgb(25, 118, 210));
        private static readonly Brush SemanticNeutral =
            new SolidColorBrush(Color.FromRgb(176, 190, 197));
        private string _selectedProcessObject = "Chamber";
        private static string L(string english, string bulgarian) =>
            LocalizationService.Text(english, bulgarian);

        private void MaterialComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_processStarted)
                ApplyDryingMode(GetSelectedDryingMode());

            UpdateUi();
        }

        private void UpdateUi()
        {
            var state = _process.State;

            if (state.SafetyInterlockActive)
            {
                StatusValueRun.Text = L("SAFETY TRIP", "АВАРИЙНА ЗАЩИТА");
                StatusValueRun.Foreground = SemanticRed;
            }
            else if (state.IsCompleted)
            {
                StatusValueRun.Text = L("Process completed", "Процесът е завършен");
                StatusValueRun.Foreground = SemanticBlue;
            }
            else if (_isRunning)
            {
                StatusValueRun.Text = L("Running", "Работи");
                StatusValueRun.Foreground = SemanticGreen;
            }
            else
            {
                StatusValueRun.Text = _processStarted
                    ? L("Paused", "Пауза")
                    : L("Ready", "Готовност");
                StatusValueRun.Foreground = SemanticNeutral;
            }

            TemperatureTextBlock.Text =
                $"{L("Chamber T", "T камера")}   {state.MeasuredTemperature:F1} °C";
            MaterialTemperatureTextBlock.Text =
                $"{L("Material T", "T материал")}   {state.MeasuredMaterialTemperature:F1} °C";
            PressureTextBlock.Text = $"{L("Pressure", "Налягане")}   {state.MeasuredPressure:F1} kPa";
            MoistureTextBlock.Text =
                $"{L("Moisture", "Влага")}   {state.MaterialMoistureWetBasisPercent:F1} % wb";
            HeaterOutputIndicatorTextBlock.Text = $"{L("Power", "Мощност")}   {state.HeaterPower:F0} %";
            PumpOutputIndicatorTextBlock.Text = $"{L("Power", "Мощност")}   {state.VacuumPumpPower:F0} %";
            FanOutputIndicatorTextBlock.Text = $"{L("Speed", "Скорост")}   {state.FanSpeed:F0} %";
            VentOutputIndicatorTextBlock.Text = $"{L("Open", "Отворен")}   {state.VentValveOpening:F0} %";
            TimeTextBlock.Text = $"{L("Elapsed", "Изминало време")}: {FormatElapsedTime(state.ElapsedTime)}";
            ProcessStageTextBlock.Text = $"{L("Stage", "Етап")}: {FormatProcessStage(state.ProcessStage)}";
            MoistureRatioTextBlock.Text = $"{L("Moisture Ratio", "Отношение на влагата")}: {state.MoistureRatio:F3}";
            RemainingTimeTextBlock.Text =
                $"{L("Estimated Remaining", "Оставащо време")}: {FormatRemainingTime(state.EstimatedRemainingTimeSeconds)}";
            VacuumLevelTextBlock.Text = $"{L("Vacuum Level", "Ниво на вакуум")}: {state.VacuumLevel:F1} %";
            TotalEnergyTextBlock.Text = $"{L("Total Energy", "Обща енергия")}: {state.TotalEnergyKWh:F3} kWh";
            EfficiencyTextBlock.Text = $"{L("Efficiency", "Ефективност")}: {state.EfficiencyKgPerKWh:F3} kg/kWh";
            SensorStatusTextBlock.Text = _process.HasSensorFault
                ? L("Sensors: FAULT", "Датчици: ПОВРЕДА")
                : L("Sensors: OK", "Датчици: OK");
            SensorStatusTextBlock.Foreground = _process.HasSensorFault
                ? SemanticRed
                : SemanticGreen;

            bool isManualMode = OperationModeComboBox.SelectedIndex == 1;
            ContextPanelModeTextBlock.Text = isManualMode
                ? L("Manual mode: adjustable outputs", "Ръчен режим: регулируеми изходи")
                : L("Auto mode: controller outputs (read-only)", "Автоматичен режим: изходи от регулаторите (само за четене)");

            AutoHeaterProgressBar.Value = state.HeaterPower;
            AutoPumpProgressBar.Value = state.VacuumPumpPower;
            AutoVentValveProgressBar.Value = state.VentValveOpening;
            AutoFanProgressBar.Value = state.FanSpeed;
            AutoHeaterValueTextBlock.Text = $"{state.HeaterPower:F0} %";
            AutoPumpValueTextBlock.Text = $"{state.VacuumPumpPower:F0} %";
            AutoVentValveValueTextBlock.Text = $"{state.VentValveOpening:F0} %";
            AutoFanValueTextBlock.Text = $"{state.FanSpeed:F0} %";
            if (!isManualMode)
            {
                ManualHeaterSlider.Value = state.HeaterPower;
                ManualPumpSlider.Value = state.VacuumPumpPower;
                ManualVentValveSlider.Value = state.VentValveOpening;
                ManualFanSlider.Value = state.FanSpeed;
            }

            HeaterLamp.Fill = state.HeaterPower > 0.0 ? SemanticGreen : Brushes.Gray;
            PumpLamp.Fill = state.VacuumPumpPower > 0.0 ? SemanticGreen : Brushes.Gray;
            FanLamp.Fill = state.FanSpeed > 0.0 ? SemanticGreen : Brushes.Gray;
            VentValveLamp.Fill = state.VentValveOpening > 0.0 ? SemanticGreen : Brushes.Gray;

            SafetyStateTextBlock.Text = state.SafetyInterlockActive
                ? L("Safety state: TRIPPED", "Състояние на защитата: ЗАДЕЙСТВАНА")
                : L("Safety state: Normal", "Състояние на защитата: Нормално");
            SafetyStateTextBlock.Foreground = state.SafetyInterlockActive
                ? SemanticRed
                : SemanticGreen;

            UpdateSelectedObjectPanel();
            UpdateAlarmsUi();
            UpdateStartButtonState();
        }

        private void UpdateStartButtonState()
        {
            var state = _process.State;
            bool recipeCanBeChanged = !_processStarted && !_isRunning;
            MaterialComboBox.IsEnabled = recipeCanBeChanged;
            DryingModeComboBox.IsEnabled = recipeCanBeChanged;

            bool startIsAvailable =
                !state.SafetyInterlockActive && !_isRunning;
            StartButton.Visibility = startIsAvailable
                ? Visibility.Visible
                : Visibility.Collapsed;
            StartButton.IsEnabled = startIsAvailable;

            if (_processStarted)
            {
                StartButton.Content = L("Resume", "Продължи");
            }
            else
            {
                StartButton.Content = L("Start", "Старт");
            }

            StopButton.Visibility = _isRunning
                ? Visibility.Visible
                : Visibility.Collapsed;
            StopButton.IsEnabled = _isRunning;

            bool resetIsAvailable =
                _processStarted ||
                state.ElapsedTime > 0.0 ||
                state.IsCompleted;
            ResetButton.Visibility = resetIsAvailable
                ? Visibility.Visible
                : Visibility.Collapsed;
            ResetButton.IsEnabled = resetIsAvailable;

            ResetSafetyButton.Visibility = state.SafetyInterlockActive
                ? Visibility.Visible
                : Visibility.Collapsed;
            ResetSafetyButton.IsEnabled = state.SafetyInterlockActive;
        }

        private void OperationModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateManualControlsState();
        }

        private void UpdateManualControlsState()
        {
            if (ManualHeaterSlider == null ||
                ManualPumpSlider == null ||
                ManualVentValveSlider == null ||
                ManualFanSlider == null)
            {
                return;
            }

            bool isManualMode = OperationModeComboBox.SelectedIndex == 1;

            AutoOutputsPanel.Visibility = isManualMode
                ? Visibility.Collapsed
                : Visibility.Visible;
            ManualControlsPanel.Visibility = isManualMode
                ? Visibility.Visible
                : Visibility.Collapsed;
            ManualHeaterSlider.IsEnabled = isManualMode;
            ManualPumpSlider.IsEnabled = isManualMode;
            ManualVentValveSlider.IsEnabled = isManualMode;
            ManualFanSlider.IsEnabled = isManualMode;
        }

        private void ManualSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ManualHeaterValueTextBlock == null ||
                ManualPumpValueTextBlock == null ||
                ManualVentValveValueTextBlock == null ||
                ManualFanValueTextBlock == null ||
                ManualHeaterSlider == null ||
                ManualPumpSlider == null ||
                ManualVentValveSlider == null ||
                ManualFanSlider == null)
                return;

            ManualHeaterValueTextBlock.Text = $"{ManualHeaterSlider.Value:F0} %";
            ManualPumpValueTextBlock.Text = $"{ManualPumpSlider.Value:F0} %";
            ManualVentValveValueTextBlock.Text = $"{ManualVentValveSlider.Value:F0} %";
            ManualFanValueTextBlock.Text = $"{ManualFanSlider.Value:F0} %";
        }

        private void ProcessObject_MouseLeftButtonDown(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string objectName)
            {
                _selectedProcessObject = objectName;
                UpdateSelectedObjectPanel();
                e.Handled = true;
            }
        }

        private void UpdateSelectedObjectPanel()
        {
            if (ContextPanelTitleTextBlock == null)
                return;

            var state = _process.State;
            ClearContextValues();
            ControllerSettingsButton.Visibility = Visibility.Collapsed;
            ControllerSettingsButton.IsEnabled = true;
            bool controllerSettingsAreAvailable =
                !_processStarted && !_isRunning;
            string controllerMode = TemperatureControlComboBox?.SelectedIndex == 1
                ? "PID"
                : "ON/OFF";

            switch (_selectedProcessObject)
            {
                case "Heater":
                    ContextPanelTitleTextBlock.Text = L("Heater", "Нагревател");
                    SelectedObjectStatusTextBlock.Text = state.HeaterPower > 0.0
                        ? L("State: Active", "Състояние: Активен")
                        : L("State: Off", "Състояние: Изключен");
                    SelectedObjectStatusTextBlock.Foreground = state.HeaterPower > 0.0
                        ? SemanticGreen
                        : SemanticNeutral;
                    SetContextValue(ContextValue1TextBlock,
                        $"{L("Heater output", "Изход на нагревателя")}: {state.HeaterPower:F0} %", Brushes.OrangeRed);
                    SetContextValue(ContextValue2TextBlock,
                        $"{L("Control", "Управление")}: {controllerMode}", SemanticNeutral);
                    SetContextValue(ContextValue3TextBlock,
                        $"{L("Chamber temperature", "Температура в камерата")}: {state.MeasuredTemperature:F1} °C", Brushes.OrangeRed);
                    SetContextValue(ContextValue4TextBlock,
                        $"{L("Temperature setpoint", "Задание за температура")}: {state.ActiveTemperatureSetpoint:F1} °C", Brushes.DeepSkyBlue);
                    SetContextValue(ContextValue5TextBlock,
                        $"Kp={_pidTemperatureController.Kp:0.###}; " +
                        $"Ki={_pidTemperatureController.Ki:0.###}; " +
                        $"Kd={_pidTemperatureController.Kd:0.###}", SemanticNeutral);
                    if (controllerSettingsAreAvailable)
                        ConfigureControllerSettingsButton("PID SETTINGS", "PID НАСТРОЙКИ");
                    break;

                case "Pump":
                    ContextPanelTitleTextBlock.Text = L("Vacuum Pump", "Вакуумна помпа");
                    SelectedObjectStatusTextBlock.Text = state.VacuumPumpPower > 0.0
                        ? L("State: Active", "Състояние: Активна")
                        : L("State: Off", "Състояние: Изключена");
                    SelectedObjectStatusTextBlock.Foreground = state.VacuumPumpPower > 0.0
                        ? SemanticGreen
                        : SemanticNeutral;
                    SetContextValue(ContextValue1TextBlock,
                        $"{L("Pump output", "Изход на помпата")}: {state.VacuumPumpPower:F0} %", Brushes.DeepSkyBlue);
                    SetContextValue(ContextValue2TextBlock,
                        $"{L("Pressure", "Налягане")}: {state.MeasuredPressure:F1} kPa", Brushes.DeepSkyBlue);
                    SetContextValue(ContextValue3TextBlock,
                        $"{L("Pressure setpoint", "Задание за налягане")}: {state.ActivePressureSetpoint:F1} kPa", Brushes.Gold);
                    SetContextValue(ContextValue4TextBlock,
                        $"{L("Vacuum level", "Ниво на вакуум")}: {state.VacuumLevel:F1} %", Brushes.DeepSkyBlue);
                    SetContextValue(ContextValue5TextBlock,
                        $"Kp={_pressureController.Kp:0.###}; " +
                        $"Ki={_pressureController.Ki:0.###}", SemanticNeutral);
                    if (controllerSettingsAreAvailable)
                        ConfigureControllerSettingsButton("PI SETTINGS", "PI НАСТРОЙКИ");
                    break;

                case "Fan":
                    ContextPanelTitleTextBlock.Text = L("Circulation Fan", "Циркулационен вентилатор");
                    SelectedObjectStatusTextBlock.Text = state.FanSpeed > 0.0
                        ? L("State: Active", "Състояние: Активен")
                        : L("State: Off", "Състояние: Изключен");
                    SelectedObjectStatusTextBlock.Foreground = state.FanSpeed > 0.0
                        ? SemanticGreen
                        : SemanticNeutral;
                    SetContextValue(ContextValue1TextBlock,
                        $"{L("Fan output", "Изход на вентилатора")}: {state.FanSpeed:F0} %", Brushes.LimeGreen);
                    SetContextValue(ContextValue2TextBlock,
                        $"{L("Air flow", "Въздушен дебит")}: {state.AirFlowRate:F1} m³/h", Brushes.LimeGreen);
                    break;

                case "Vent":
                    ContextPanelTitleTextBlock.Text = L("Vent Valve", "Вентилационен клапан");
                    SelectedObjectStatusTextBlock.Text = state.VentValveOpening > 0.0
                        ? L("State: Open", "Състояние: Отворен")
                        : L("State: Closed", "Състояние: Затворен");
                    SelectedObjectStatusTextBlock.Foreground = state.VentValveOpening > 0.0
                        ? SemanticGreen
                        : SemanticNeutral;
                    SetContextValue(ContextValue1TextBlock,
                        $"{L("Valve opening", "Отваряне на клапана")}: {state.VentValveOpening:F0} %", Brushes.Gold);
                    SetContextValue(ContextValue2TextBlock,
                        $"{L("Chamber pressure", "Налягане в камерата")}: {state.MeasuredPressure:F1} kPa", Brushes.DeepSkyBlue);
                    break;

                default:
                    ContextPanelTitleTextBlock.Text = L("Vacuum Chamber", "Вакуумна камера");
                    SelectedObjectStatusTextBlock.Text = state.SafetyInterlockActive
                        ? L("State: Safety trip", "Състояние: Задействана защита")
                        : $"{L("Stage", "Етап")}: {FormatProcessStage(state.ProcessStage)}";
                    SelectedObjectStatusTextBlock.Foreground = state.SafetyInterlockActive
                        ? SemanticRed
                        : _isRunning ? SemanticGreen : SemanticNeutral;
                    string moistureTarget = _process.SelectedMaterial == null
                        ? "-"
                        : $"{_process.SelectedMaterial.TargetMoistureWetBasisPercent:F1} % wb";
                    SetContextValue(ContextValue1TextBlock,
                        $"{L("Chamber temperature", "Температура в камерата")}: {state.MeasuredTemperature:F1} °C", Brushes.OrangeRed);
                    SetContextValue(ContextValue2TextBlock,
                        $"{L("Temperature setpoint", "Задание за температура")}: {state.ActiveTemperatureSetpoint:F1} °C", Brushes.DeepSkyBlue);
                    SetContextValue(ContextValue3TextBlock,
                        $"{L("Material temperature", "Температура на материала")}: {state.MeasuredMaterialTemperature:F1} °C", Brushes.Gold);
                    SetContextValue(ContextValue4TextBlock,
                        $"{L("Pressure", "Налягане")}: {state.MeasuredPressure:F1} kPa", Brushes.DeepSkyBlue);
                    SetContextValue(ContextValue5TextBlock,
                        $"{L("Pressure setpoint", "Задание за налягане")}: {state.ActivePressureSetpoint:F1} kPa", Brushes.Gold);
                    SetContextValue(ContextValue6TextBlock,
                        $"{L("Material moisture", "Влага на материала")}: {state.MaterialMoistureWetBasisPercent:F1} % wb", Brushes.LimeGreen);
                    SetContextValue(ContextValue7TextBlock,
                        $"{L("Moisture target", "Целева влага")}: {moistureTarget}", Brushes.Gold);
                    break;
            }
        }

        private void ConfigureControllerSettingsButton(
            string englishContent,
            string bulgarianContent)
        {
            ControllerSettingsButton.Content = L(englishContent, bulgarianContent);
            ControllerSettingsButton.Visibility = Visibility.Visible;
            ControllerSettingsButton.ToolTip = L(
                "Edit the controller coefficients.",
                "Промяна на коефициентите на регулатора.");
        }

        private void ControllerSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_processStarted || _isRunning)
            {
                MessageBox.Show(
                    L(
                        "Controller coefficients can be changed before starting a batch.",
                        "Коефициентите могат да се променят преди стартиране на партида."),
                    L("Controller settings", "Настройки на регулатора"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ControllerSettingsWindow settingsWindow;

            if (_selectedProcessObject == "Heater")
            {
                settingsWindow = new ControllerSettingsWindow(
                    ControllerSettingsMode.TemperaturePid,
                    _pidTemperatureController.Kp,
                    _pidTemperatureController.Ki,
                    _pidTemperatureController.Kd);
            }
            else if (_selectedProcessObject == "Pump")
            {
                settingsWindow = new ControllerSettingsWindow(
                    ControllerSettingsMode.PressurePi,
                    _pressureController.Kp,
                    _pressureController.Ki);
            }
            else
            {
                return;
            }

            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() != true)
                return;

            if (_selectedProcessObject == "Heater")
            {
                _pidTemperatureController.Kp = settingsWindow.Kp;
                _pidTemperatureController.Ki = settingsWindow.Ki;
                _pidTemperatureController.Kd = settingsWindow.Kd;
                _pidTemperatureController.Reset();
            }
            else
            {
                _pressureController.Kp = settingsWindow.Kp;
                _pressureController.Ki = settingsWindow.Ki;
                _pressureController.Reset();
            }

            UpdateSelectedObjectPanel();
        }

        private void ClearContextValues()
        {
            System.Windows.Controls.TextBlock[] valueBlocks =
            {
                ContextValue1TextBlock,
                ContextValue2TextBlock,
                ContextValue3TextBlock,
                ContextValue4TextBlock,
                ContextValue5TextBlock,
                ContextValue6TextBlock,
                ContextValue7TextBlock
            };

            foreach (var block in valueBlocks)
            {
                block.Text = "";
                block.Visibility = Visibility.Collapsed;
            }
        }

        private static void SetContextValue(
            System.Windows.Controls.TextBlock block,
            string text,
            Brush foreground)
        {
            block.Text = text;
            block.Foreground = foreground;
            block.Visibility = Visibility.Visible;
        }

        private static string FormatProcessStage(Enums.ProcessStage stage)
        {
            return stage switch
            {
                Enums.ProcessStage.Preheating => L("Preheating", "Предварително нагряване"),
                Enums.ProcessStage.Evacuation => L("Evacuation", "Вакуумиране"),
                Enums.ProcessStage.Drying => L("Drying", "Сушене"),
                Enums.ProcessStage.FinalDrying => L("Final drying", "Финално сушене"),
                Enums.ProcessStage.Venting => L("Pressure recovery", "Възстановяване на налягането"),
                Enums.ProcessStage.SafetyShutdown => L("Safety shutdown", "Аварийно изключване"),
                Enums.ProcessStage.Completed => L("Completed", "Завършен"),
                Enums.ProcessStage.Manual => L("Manual control", "Ръчно управление"),
                _ => L("Idle", "Готовност")
            };
        }

        private static string FormatRemainingTime(double? seconds)
        {
            if (!seconds.HasValue ||
                double.IsNaN(seconds.Value) ||
                double.IsInfinity(seconds.Value))
            {
                return L("calculating...", "изчислява се...");
            }

            TimeSpan remaining = TimeSpan.FromSeconds(Math.Max(0.0, seconds.Value));
            if (remaining.TotalDays >= 1.0)
                return LocalizationService.IsBulgarian
                    ? $"{(int)remaining.TotalDays}д {remaining.Hours:D2}ч"
                    : $"{(int)remaining.TotalDays}d {remaining.Hours:D2}h";
            if (remaining.TotalHours >= 1.0)
                return $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

            return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private static string FormatElapsedTime(double seconds)
        {
            TimeSpan elapsed = TimeSpan.FromSeconds(Math.Max(0.0, seconds));
            if (elapsed.TotalDays >= 1.0)
                return $"{(int)elapsed.TotalDays}{L("d", "д")} {elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

            return $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        private void UpdateAlarmsUi()
        {
            if (AlarmsTextBlock == null)
                return;

            if (_alarmService.ActiveAlarms.Count == 0)
            {
                AlarmsTextBlock.Text = L("No active alarms", "Няма активни аларми");
                AlarmsTextBlock.Foreground = SemanticGreen;
                ShowAlarmsButton.Content = L("ALARMS", "АЛАРМИ");
                ShowAlarmsButton.Background = SemanticBlue;
                return;
            }

            var highestAlarm = _alarmService.ActiveAlarms[0];
            string additionalAlarmText = _alarmService.ActiveAlarms.Count > 1
                ? $"\n+{_alarmService.ActiveAlarms.Count - 1} " +
                  L("more active alarm(s)", "още активни аларми")
                : "";
            string severityText = highestAlarm.Severity switch
            {
                AlarmSeverity.Critical => L("CRITICAL", "КРИТИЧНА"),
                AlarmSeverity.Warning => L("WARNING", "ПРЕДУПРЕЖДЕНИЕ"),
                _ => L("INFO", "ИНФОРМАЦИЯ")
            };
            AlarmsTextBlock.Text =
                $"{highestAlarm.Time:HH:mm:ss}  {severityText}\n" +
                highestAlarm.LocalizedMessage +
                (string.IsNullOrWhiteSpace(highestAlarm.LocalizedRecommendedAction)
                    ? ""
                    : $"\n{L("Action", "Действие")}: {highestAlarm.LocalizedRecommendedAction}") +additionalAlarmText;

            bool hasCritical = _alarmService.ActiveAlarms.Any(a => a.Severity == AlarmSeverity.Critical);
            bool hasWarning = _alarmService.ActiveAlarms.Any(a => a.Severity == AlarmSeverity.Warning);

            AlarmsTextBlock.Foreground = hasCritical
                ? SemanticRed
                : hasWarning ? SemanticAmber : SemanticBlue;
            ShowAlarmsButton.Content =$"{L("ALARMS", "АЛАРМИ")} ({_alarmService.ActiveAlarms.Count})";
            ShowAlarmsButton.Background = hasCritical
                ? SemanticRed
                : hasWarning ? SemanticAmber : SemanticBlue;
        }
    }
}
