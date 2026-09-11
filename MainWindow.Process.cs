using System;
using System.Reflection;
using System.Windows;
using DiplomWork_Ivan_2026.Controllers;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Services;

namespace DiplomWork_Ivan_2026
{
    public partial class MainWindow
    {
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UpdateSettingsFromUi())
                return;

            if (_process.State.SafetyInterlockActive)
            {
                MessageBox.Show(L(
                    "Reset the safety interlock before starting the process.",
                    "Нулирайте защитната блокировка преди стартиране на процеса."));
                return;
            }

            if (!_processStarted)
            {
                if (MaterialComboBox.SelectedItem is not DryingMaterial material)
                {
                    MessageBox.Show(L("Please select a material.", "Моля, изберете материал."));
                    return;
                }

                if (_settings.TemperatureSetpoint > material.MaxTemperature)
                {
                    MessageBox.Show(L(
                        $"Temperature setpoint {_settings.TemperatureSetpoint:F1} °C exceeds " +
                        $"the {material.Name} limit {material.MaxTemperature:F1} °C.",
                        $"Заданието за температура {_settings.TemperatureSetpoint:F1} °C надвишава " +
                        $"границата за {material}: {material.MaxTemperature:F1} °C."));
                    return;
                }

                _process.LoadMaterial(material);
                _process.SetLeakMultiplier(1.0);
                _automaticProcessController.Reset(_process.State, _settings);
                _trendBuffer.BeginExperiment(CreateExperimentMetadata(material));
                ResetDisturbanceTracking();

                _processStarted = true;
            }

            // Execute the first controller calculation immediately. Subsequent
            // outputs are held until the next controller sampling instant.
            _controllerElapsedSeconds = _controllerStepSeconds;
            _isRunning = true;
            _timer.Start();

            UpdateUi();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _isRunning = false;
            _timer.Stop();
            _pidTemperatureController.Reset();
            _pressureController.Reset();
            TurnOffDevices();

            _process.State.HeaterPower = 0;
            _process.State.VacuumPumpPower = 0;
            _process.State.VentValveOpening = 0;
            _process.State.FanSpeed = 0;

            UpdateUi();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if ((_processStarted || _process.State.ElapsedTime > 0.0) &&
                MessageBox.Show(
                    L("Reset the process? Current progress and trend history will be cleared.",
                      "Да се нулира ли процесът? Текущият ход и историята на графиките ще бъдат изтрити."),
                    L("Reset process", "Нулиране на процеса"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            _timer.Stop();
            _pidTemperatureController.Reset();
            _pressureController.Reset();
            _automaticProcessController.Reset(_process.State, _settings);
            _isRunning = false;
            _processStarted = false;

            TurnOffDevices();

            if (MaterialComboBox.SelectedItem is DryingMaterial material)
            {
                _process.LoadMaterial(material);
            }

            _process.SetLeakMultiplier(1.0);
            ResetDisturbanceTracking();

            _safetyInterlockService.Clear(_process);

            _trendBuffer.Clear();
            _alarmService.CheckAlarms(_process, _settings);

            UpdateUi();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isRunning)
                return;

            if (!UpdateSettingsFromUi())
            {
                _isRunning = false;
                _timer.Stop();
                return;
            }

            for (int sample = 0; sample < _simulationSpeedMultiplier; sample++)
            {
                // The physical model and safety evaluation use the model step.
                // Controller outputs are updated at their own slower sampling
                // interval and held between calculations. Trends still receive
                // exactly one sample per simulated second.
                for (int substep = 0;
                    substep < IntegrationSubstepsPerTrendSample;
                    substep++)
                {
                    _safetyInterlockService.Evaluate(_process, _settings);

                    if (_controllerElapsedSeconds + 0.000000001 >=
                        _controllerStepSeconds)
                    {
                        UpdateControllers(_controllerStepSeconds);
                        _controllerElapsedSeconds = 0.0;
                    }

                    _process.Update(
                        _modelStepSeconds,
                        _settings);
                    _controllerElapsedSeconds += _modelStepSeconds;
                    _safetyInterlockService.Evaluate(_process, _settings);

                    if (_process.State.IsCompleted)
                        break;
                }

                TrackSensorFaultDisturbances();
                _trendBuffer.AddPoint(
                    _process.State,
                    _settings,
                    _simulationSpeedMultiplier);
                _alarmService.CheckAlarms(_process, _settings);

                if (_process.State.IsCompleted)
                    break;
            }

            UpdateChartData();

            if (_process.State.IsCompleted)
            {
                _isRunning = false;
                _processStarted = false;
                _timer.Stop();
            }

            UpdateUi();
        }

        private void SimulationSpeedComboBox_SelectionChanged(
            object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SimulationSpeedComboBox?.SelectedItem is not
                System.Windows.Controls.ComboBoxItem selectedItem)
            {
                return;
            }

            if (int.TryParse(selectedItem.Tag?.ToString(), out int multiplier) &&
                multiplier > 0)
            {
                _simulationSpeedMultiplier = multiplier;
            }
        }

        private void UpdateControllers(double deltaTime)
        {
            if (_process.State.SafetyInterlockActive)
            {
                SafetyInterlockService.ApplySafeOutputs(_process);
                _pidTemperatureController.Reset();
                _pressureController.Reset();
                return;
            }

            bool isAutoMode = OperationModeComboBox.SelectedIndex == 0;

            if (isAutoMode)
            {
                AutomaticControlTargets targets =
                    _automaticProcessController.Update(
                        _process,
                        _settings,
                        _automaticFanSpeedSetpoint,
                        deltaTime);

                if (_process.State.IsCompleted)
                {
                    TurnOffDevices();
                    return;
                }

                bool usePidTemperatureControl =
                    TemperatureControlComboBox != null &&
                    TemperatureControlComboBox.SelectedIndex == 1;

                if (!targets.TemperatureControlEnabled)
                {
                    _process.Heater.TurnOff();
                    _pidTemperatureController.Reset();
                }
                else if (usePidTemperatureControl)
                {
                    double heaterPower = _pidTemperatureController.Update(
                        targets.TemperatureSetpoint,
                        _process.State.MeasuredTemperature,
                        deltaTime);

                    _process.Heater.SetPower(heaterPower);
                    _process.State.HeaterPower = _process.Heater.Power;
                }
                else
                {
                    _temperatureController.Update(_process.State, _settings, _process.Heater);
                }

                if (targets.PressureControlEnabled)
                {
                    double pumpPower = _pressureController.Update(
                        targets.PressureSetpoint,
                        _process.State.MeasuredPressure,
                        deltaTime);
                    _process.Pump.SetPower(pumpPower);
                }
                else
                {
                    _process.Pump.TurnOff();
                    _pressureController.Reset();
                }

                _process.VentValve.SetOpening(targets.VentValveOpening);
                _process.Fan.SetSpeed(targets.FanSpeed);
            }
            else
            {
                _pidTemperatureController.Reset();
                _pressureController.Reset();

                _process.State.ProcessStage = Enums.ProcessStage.Manual;
                _process.State.StageElapsedTime = 0.0;
                _process.State.ActiveTemperatureSetpoint = _settings.TemperatureSetpoint;
                _process.State.ActivePressureSetpoint = _settings.PressureSetpoint;

                _process.Heater.SetPower(ManualHeaterSlider.Value);
                _process.Pump.SetPower(ManualPumpSlider.Value);
                _process.VentValve.SetOpening(ManualVentValveSlider.Value);
                _process.Fan.SetSpeed(ManualFanSlider.Value);
            }
        }

        private void EmergencyStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_process.State.EmergencyStopActive)
                return;

            _trendBuffer.Metadata?.AddDisturbance(
                "EmergencyStop",
                _process.State.ElapsedTime);

            _safetyInterlockService.Trip(
                _process,
                L("Emergency stop activated by the operator.",
                  "Аварийното спиране е задействано от оператора."),
                true);

            if (_trendBuffer.Metadata != null)
            {
                _trendBuffer.AddPoint(
                    _process.State,
                    _settings,
                    _simulationSpeedMultiplier);
                UpdateChartData();
            }

            _isRunning = false;
            _timer.Stop();
            _alarmService.CheckAlarms(_process, _settings);
            UpdateUi();
        }

        private void ResetSafetyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UpdateSettingsFromUi())
                return;

            if (!_safetyInterlockService.TryReset(
                _process,
                _settings,
                out string reason))
            {
                MessageBox.Show(reason, L("Safety reset", "Нулиране на защитата"));
                return;
            }

            _pidTemperatureController.Reset();
            _pressureController.Reset();
            _automaticProcessController.Reset(_process.State, _settings);
            _alarmService.CheckAlarms(_process, _settings);
            UpdateUi();
        }

        private void TurnOffDevices()
        {
            _process.Heater.TurnOff();
            _process.Pump.TurnOff();
            _process.VentValve.Close();
            _process.Fan.TurnOff();
        }

        private ExperimentMetadata CreateExperimentMetadata(DryingMaterial material)
        {
            Enums.DryingMode dryingMode = GetSelectedDryingMode();
            DryingRecipe recipe = material.GetRecipe(dryingMode);

            return new ExperimentMetadata
            {
                ExperimentId = Guid.NewGuid().ToString("D"),
                RunLabel = RunLabelGenerator.Create(material.Name, dryingMode),
                StartedAt = DateTimeOffset.Now,
                ProgramVersion = GetProgramVersion(),
                MaterialName = material.Name,
                RecipeName = dryingMode.ToString(),
                OperationMode = OperationModeComboBox.SelectedIndex == 0
                    ? "Auto"
                    : "Manual",
                TemperatureControlMode = TemperatureControlComboBox.SelectedIndex == 1
                    ? "PID"
                    : "OnOff",
                TemperatureSetpointC = _settings.TemperatureSetpoint,
                PressureSetpointKPa = _settings.PressureSetpoint,
                AutomaticFanSetpointPercent = _automaticFanSpeedSetpoint,
                RecipeTemperatureSetpointC = recipe.TemperatureSetpointC,
                RecipePressureSetpointKPa = recipe.PressureSetpointKPa,
                RecipeFanSpeedPercent = recipe.FanSpeedPercent,
                MaximumAllowedTemperatureC = material.MaxTemperature,
                InitialWetMassKg = material.InitialWetMassKg,
                DryMassKg = material.DryMassKg,
                InitialMoistureWetBasisPercent = material.InitialMoistureWetBasisPercent,
                TargetMoistureWetBasisPercent = material.TargetMoistureWetBasisPercent,
                DryingCoefficient = material.DryingCoefficient,
                TemperaturePidKp = _pidTemperatureController.Kp,
                TemperaturePidKi = _pidTemperatureController.Ki,
                TemperaturePidKd = _pidTemperatureController.Kd,
                TemperaturePidDerivativeFilterSeconds =
                    _pidTemperatureController.DerivativeFilterTimeConstantSeconds,
                PressurePiKp = _pressureController.Kp,
                PressurePiKi = _pressureController.Ki,
                ModelStepSeconds = _modelStepSeconds,
                ControllerStepSeconds = _controllerStepSeconds,
                TrendSampleIntervalSeconds = FixedTrendSampleIntervalSeconds,
                SimulationSpeedAtStart = _simulationSpeedMultiplier,
                AmbientTemperatureC = _settings.AmbientTemperature,
                AmbientPressureKPa = _settings.AmbientPressure,
                AmbientRelativeHumidityPercent =
                    _settings.AmbientRelativeHumidityPercent
            };
        }

        private static string GetProgramVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ??
                assembly.GetName().Version?.ToString() ??
                "Unknown";
        }

        private void ResetDisturbanceTracking()
        {
            _lastChamberTemperatureFaultMode =
                _process.ChamberTemperatureSensor.FaultMode;
            _lastMaterialTemperatureFaultMode =
                _process.MaterialTemperatureSensor.FaultMode;
            _lastPressureFaultMode = _process.PressureSensor.FaultMode;
        }

        private void TrackSensorFaultDisturbances()
        {
            TrackSensorFaultDisturbance(
                "ChamberTemperatureSensor",
                _process.ChamberTemperatureSensor.FaultMode,
                ref _lastChamberTemperatureFaultMode);
            TrackSensorFaultDisturbance(
                "MaterialTemperatureSensor",
                _process.MaterialTemperatureSensor.FaultMode,
                ref _lastMaterialTemperatureFaultMode);
            TrackSensorFaultDisturbance(
                "PressureSensor",
                _process.PressureSensor.FaultMode,
                ref _lastPressureFaultMode);
        }

        private void TrackSensorFaultDisturbance(
            string sensorName,
            Enums.SensorFaultMode currentMode,
            ref Enums.SensorFaultMode previousMode)
        {
            if (currentMode == previousMode)
                return;

            string disturbanceType = currentMode == Enums.SensorFaultMode.None
                ? $"{sensorName}:Cleared"
                : $"{sensorName}:{currentMode}";
            _trendBuffer.Metadata?.AddDisturbance(
                disturbanceType,
                _process.State.ElapsedTime);

            previousMode = currentMode;
        }
    }
}
