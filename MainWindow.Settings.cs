using System.Globalization;
using System.Windows;
using DiplomWork_Ivan_2026.Enums;
using DiplomWork_Ivan_2026.Models;

namespace DiplomWork_Ivan_2026
{
    public partial class MainWindow
    {
        private void ApplyDryingMode(DryingMode mode)
        {
            if (MaterialComboBox?.SelectedItem is not DryingMaterial material)
                return;

            DryingRecipe recipe = material.GetRecipe(mode);
            double maximumRecommendedTemperature = Math.Max(
                _settings.AmbientTemperature + 1.0,
                material.MaxTemperature - 1.0);
            double safeTemperatureSetpoint = Math.Clamp(
                recipe.TemperatureSetpointC,
                _settings.AmbientTemperature + 1.0,
                maximumRecommendedTemperature);
            double safePressureSetpoint = Math.Clamp(
                recipe.PressureSetpointKPa,
                _process.Parameters.MinimumPressureKPa + 0.1,
                _settings.AmbientPressure);
            double safeFanSpeed = Math.Clamp(
                recipe.FanSpeedPercent,
                0.0,
                100.0);

            TemperatureSetpointTextBox.Text =
                safeTemperatureSetpoint.ToString(
                    "0.#",
                    CultureInfo.CurrentCulture);
            PressureSetpointTextBox.Text =
                safePressureSetpoint.ToString(
                    "0.#",
                    CultureInfo.CurrentCulture);
            ManualFanSlider.Value = safeFanSpeed;
            _automaticFanSpeedSetpoint = safeFanSpeed;

            if (DryingRecipeInfoTextBlock != null)
            {
                DryingRecipeInfoTextBlock.Text = Services.LocalizationService.Text(
                    $"Suggested simulation recipe for {material.Name}: " +
                    $"{safeTemperatureSetpoint:F0} °C, {safePressureSetpoint:F0} kPa, " +
                    $"fan {safeFanSpeed:F0}%. Material limit: {material.MaxTemperature:F0} °C.",
                    $"Препоръчителна симулационна рецепта за {material}: " +
                    $"{safeTemperatureSetpoint:F0} °C, {safePressureSetpoint:F0} kPa, " +
                    $"вентилатор {safeFanSpeed:F0}%. Граница на материала: {material.MaxTemperature:F0} °C.");
            }
        }

        private DryingMode GetSelectedDryingMode()
        {
            return DryingModeComboBox?.SelectedIndex switch
            {
                0 => DryingMode.Soft,
                2 => DryingMode.Hard,
                _ => DryingMode.Normal
            };
        }

        private void DryingModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DryingModeComboBox == null ||
                TemperatureSetpointTextBox == null ||
                PressureSetpointTextBox == null ||
                ManualFanSlider == null)
            {
                return;
            }

            ApplyDryingMode(GetSelectedDryingMode());
        }

        private bool UpdateSettingsFromUi()
        {
            if (!double.TryParse(TemperatureSetpointTextBox.Text, out double temperatureSetpoint))
            {
                MessageBox.Show(L("Invalid temperature setpoint.", "Невалидно задание за температура."));
                return false;
            }

            if (!double.TryParse(PressureSetpointTextBox.Text, out double pressureSetpoint))
            {
                MessageBox.Show(L("Invalid pressure setpoint.", "Невалидно задание за налягане."));
                return false;
            }

            if (pressureSetpoint <= 5.0 ||
                pressureSetpoint > _settings.AmbientPressure)
            {
                MessageBox.Show(L(
                    $"Pressure setpoint must be above 5.0 kPa and not greater than {_settings.AmbientPressure:F1} kPa.",
                    $"Заданието за налягане трябва да е над 5.0 kPa и не по-голямо от {_settings.AmbientPressure:F1} kPa."));
                return false;
            }

            if (temperatureSetpoint <= _settings.AmbientTemperature ||
                temperatureSetpoint > 200.0)
            {
                MessageBox.Show(L(
                    $"Temperature setpoint must be above {_settings.AmbientTemperature:F1} °C and not greater than 200 °C.",
                    $"Заданието за температура трябва да е над {_settings.AmbientTemperature:F1} °C и не по-голямо от 200 °C."));
                return false;
            }

            if (MaterialComboBox.SelectedItem is DryingMaterial material &&
                temperatureSetpoint > material.MaxTemperature)
            {
                MessageBox.Show(L(
                    $"Temperature setpoint {temperatureSetpoint:F1} °C exceeds " +
                    $"the safe limit for {material.Name}: " +
                    $"{material.MaxTemperature:F1} °C. " +
                    "Select a recommended drying mode or reduce the setpoint.",
                    $"Заданието за температура {temperatureSetpoint:F1} °C надвишава " +
                    $"безопасната граница за {material}: {material.MaxTemperature:F1} °C. " +
                    "Изберете препоръчителен режим или намалете заданието."));
                return false;
            }

            _settings.TemperatureSetpoint = temperatureSetpoint;
            _settings.PressureSetpoint = pressureSetpoint;

            return true;
        }
    }
}
