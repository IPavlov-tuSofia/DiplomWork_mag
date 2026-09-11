using System;
using System.Globalization;
using System.Windows;
using DiplomWork_Ivan_2026.Enums;

namespace DiplomWork_Ivan_2026
{
    public partial class MainWindow
    {
        private bool CanInjectExperimentalDisturbance() =>
            _processStarted &&
            !_process.State.IsCompleted &&
            !_process.State.SafetyInterlockActive;

        private bool CanConfigureDiscretizationSteps() =>
            !_processStarted && !_isRunning;

        private double GetModelStep() => _modelStepSeconds;

        private double GetControllerStep() => _controllerStepSeconds;

        private void ApplyDiscretizationSteps(
            double modelStepSeconds,
            double controllerStepSeconds)
        {
            if (!CanConfigureDiscretizationSteps())
            {
                MessageBox.Show(
                    L(
                        "The discretization steps can be changed before starting a batch.",
                        "Тактовете на дискретизация могат да се променят преди стартиране на партида."),
                    L("Discretization steps", "Тактове на дискретизация"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!IsSupportedModelStep(modelStepSeconds) ||
                !IsSupportedControllerStep(controllerStepSeconds) ||
                controllerStepSeconds < modelStepSeconds ||
                !IsIntegerMultiple(
                    controllerStepSeconds,
                    modelStepSeconds))
            {
                MessageBox.Show(
                    L(
                        "Controller step must be greater than or equal to the model step and an integer multiple of it.",
                        "Тактът на регулаторите трябва да е по-голям или равен на такта на модела и да бъде негово цяло кратно."),
                    L("Invalid discretization steps", "Невалидни тактове на дискретизация"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _modelStepSeconds = modelStepSeconds;
            _controllerStepSeconds = controllerStepSeconds;
            _controllerElapsedSeconds = controllerStepSeconds;
        }

        private static bool IsSupportedModelStep(double value) =>
            value is 0.05 or 0.1 or 0.2 or 0.5 or 1.0;

        private static bool IsSupportedControllerStep(double value) =>
            value is 0.05 or 0.1 or 0.2 or 0.5 or 1.0 or 2.0 or 5.0;

        private static bool IsIntegerMultiple(double value, double divisor)
        {
            double ratio = value / divisor;
            return Math.Abs(ratio - Math.Round(ratio)) < 0.000000001;
        }

        private void ApplyLeakDisturbance(double multiplier)
        {
            if (!EnsureDisturbanceCanBeInjected())
                return;

            double previousMultiplier = _process.LeakMultiplier;
            _process.SetLeakMultiplier(multiplier);

            if (Math.Abs(previousMultiplier - _process.LeakMultiplier) < 0.0001)
                return;

            string disturbanceType = _process.LeakMultiplier <= 1.0
                ? "VacuumLeak:Cleared"
                : "VacuumLeak:x" + _process.LeakMultiplier.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture);
            _trendBuffer.Metadata?.AddDisturbance(
                disturbanceType,
                _process.State.ElapsedTime);

            UpdateUi();
        }

        private void ApplySensorFaultDisturbance(
            ExperimentalSensorTarget sensorTarget,
            SensorFaultMode faultMode)
        {
            if (!EnsureDisturbanceCanBeInjected())
                return;

            switch (sensorTarget)
            {
                case ExperimentalSensorTarget.ChamberTemperature:
                    _process.ChamberTemperatureSensor.SetFaultMode(faultMode);
                    _process.State.MeasuredTemperature =
                        _process.ChamberTemperatureSensor.Update(
                            _process.State.Temperature,
                            0.0);
                    break;

                case ExperimentalSensorTarget.MaterialTemperature:
                    _process.MaterialTemperatureSensor.SetFaultMode(faultMode);
                    _process.State.MeasuredMaterialTemperature =
                        _process.MaterialTemperatureSensor.Update(
                            _process.State.MaterialTemperature,
                            0.0);
                    break;

                case ExperimentalSensorTarget.Pressure:
                    _process.PressureSensor.SetFaultMode(faultMode);
                    _process.State.MeasuredPressure =
                        _process.PressureSensor.Update(
                            _process.State.Pressure,
                            0.0);
                    break;
            }

            TrackSensorFaultDisturbances();
            _safetyInterlockService.Evaluate(_process, _settings);

            if (_process.State.SafetyInterlockActive)
            {
                _isRunning = false;
                _timer.Stop();
            }

            if (_trendBuffer.Metadata != null)
            {
                _trendBuffer.AddPoint(
                    _process.State,
                    _settings,
                    _simulationSpeedMultiplier);
                UpdateChartData();
            }

            _alarmService.CheckAlarms(_process, _settings);
            UpdateUi();
        }

        private void ClearExperimentalDisturbances()
        {
            double previousLeakMultiplier = _process.LeakMultiplier;
            _process.SetLeakMultiplier(1.0);
            if (previousLeakMultiplier > 1.0)
            {
                _trendBuffer.Metadata?.AddDisturbance(
                    "VacuumLeak:Cleared",
                    _process.State.ElapsedTime);
            }

            _process.ChamberTemperatureSensor.SetFaultMode(SensorFaultMode.None);
            _process.MaterialTemperatureSensor.SetFaultMode(SensorFaultMode.None);
            _process.PressureSensor.SetFaultMode(SensorFaultMode.None);
            TrackSensorFaultDisturbances();

            _process.State.MeasuredTemperature =
                _process.ChamberTemperatureSensor.Update(
                    _process.State.Temperature,
                    0.0);
            _process.State.MeasuredMaterialTemperature =
                _process.MaterialTemperatureSensor.Update(
                    _process.State.MaterialTemperature,
                    0.0);
            _process.State.MeasuredPressure =
                _process.PressureSensor.Update(
                    _process.State.Pressure,
                    0.0);

            _alarmService.CheckAlarms(_process, _settings);
            UpdateUi();
        }

        private bool EnsureDisturbanceCanBeInjected()
        {
            if (CanInjectExperimentalDisturbance())
                return true;

            MessageBox.Show(
                L(
                    "Experimental disturbances can be introduced only during an active batch without a safety trip.",
                    "Експериментални смущения могат да се внасят само по време на активна партида без задействана защита."),
                L("Experiment disturbances", "Експериментални смущения"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }
    }
}
