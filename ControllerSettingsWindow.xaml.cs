using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using DiplomWork_Ivan_2026.Controllers;
using DiplomWork_Ivan_2026.Services;

namespace DiplomWork_Ivan_2026
{
    public enum ControllerSettingsMode
    {
        TemperaturePid,
        PressurePi
    }

    public partial class ControllerSettingsWindow : Window
    {
        private const double MaximumCoefficient = 1000.0;
        private readonly ControllerSettingsMode _mode;

        public double Kp { get; private set; }
        public double Ki { get; private set; }
        public double Kd { get; private set; }

        public ControllerSettingsWindow(
            ControllerSettingsMode mode,
            double kp,
            double ki,
            double kd = 0.0)
        {
            InitializeComponent();

            _mode = mode;
            Kp = kp;
            Ki = ki;
            Kd = kd;

            ConfigureMode();
            SetTextBoxValues(kp, ki, kd);
            LocalizationService.ApplyStaticText(this);

            Loaded += (_, _) =>
            {
                KpTextBox.Focus();
                KpTextBox.SelectAll();
            };
        }

        private static string L(string english, string bulgarian) =>
            LocalizationService.Text(english, bulgarian);

        private void ConfigureMode()
        {
            bool isTemperaturePid =
                _mode == ControllerSettingsMode.TemperaturePid;

            TitleTextBlock.Text = isTemperaturePid
                ? "Temperature PID settings"
                : "Pressure PI settings";
            Title = TitleTextBlock.Text;

            KpUnitTextBlock.Text = isTemperaturePid ? "%/°C" : "%/kPa";
            KiUnitTextBlock.Text = isTemperaturePid
                ? "%/(°C·s)"
                : "%/(kPa·s)";
            KdUnitTextBlock.Text = "%·s/°C";

            Visibility kdVisibility = isTemperaturePid
                ? Visibility.Visible
                : Visibility.Collapsed;
            KdLabelTextBlock.Visibility = kdVisibility;
            KdTextBox.Visibility = kdVisibility;
            KdUnitTextBlock.Visibility = kdVisibility;
        }

        private void SetTextBoxValues(double kp, double ki, double kd)
        {
            KpTextBox.Text = FormatValue(kp);
            KiTextBox.Text = FormatValue(ki);
            KdTextBox.Text = FormatValue(kd);
        }

        private static string FormatValue(double value) =>
            value.ToString("0.###", CultureInfo.CurrentCulture);

        private static bool TryReadCoefficient(
            string text,
            out double value)
        {
            bool parsed = double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out value);

            if (!parsed)
            {
                parsed = double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            return parsed &&
                double.IsFinite(value) &&
                value >= 0.0 &&
                value <= MaximumCoefficient;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadCoefficient(KpTextBox.Text, out double kp) ||
                !TryReadCoefficient(KiTextBox.Text, out double ki) ||
                (_mode == ControllerSettingsMode.TemperaturePid &&
                 !TryReadCoefficient(KdTextBox.Text, out _)))
            {
                MessageBox.Show(
                    L(
                        $"Enter numeric coefficient values from 0 to {MaximumCoefficient:0}.",
                        $"Въведете числови стойности на коефициентите от 0 до {MaximumCoefficient:0}."),
                    L("Invalid coefficient", "Невалиден коефициент"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Kp = kp;
            Ki = ki;
            Kd = _mode == ControllerSettingsMode.TemperaturePid
                ? ReadValidatedKd()
                : 0.0;

            DialogResult = true;
        }

        private double ReadValidatedKd()
        {
            TryReadCoefficient(KdTextBox.Text, out double kd);
            return kd;
        }

        private void DefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == ControllerSettingsMode.TemperaturePid)
            {
                SetTextBoxValues(
                    PidTemperatureController.DefaultKp,
                    PidTemperatureController.DefaultKi,
                    PidTemperatureController.DefaultKd);
            }
            else
            {
                SetTextBoxValues(
                    PiPressureController.DefaultKp,
                    PiPressureController.DefaultKi,
                    0.0);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) =>
            Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
