using System.Windows;
using DiplomWork_Ivan_2026.Services;

namespace DiplomWork_Ivan_2026
{
    public partial class MainWindow
    {
        private ExperimentDisturbancesWindow? _disturbancesWindow;

        private void ShowTrendsButton_Click(object sender, RoutedEventArgs e)
        {
            TrendsWindow trendsWindow = new TrendsWindow(_trendBuffer);
            trendsWindow.Show();
        }

        private void ShowAlarmsButton_Click(object sender, RoutedEventArgs e)
        {
            AlarmsWindow alarmsWindow = new AlarmsWindow(_alarmService);
            alarmsWindow.Show();
        }

        private void ShowDisturbancesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_disturbancesWindow?.IsVisible == true)
            {
                _disturbancesWindow.Activate();
                return;
            }

            _disturbancesWindow = new ExperimentDisturbancesWindow(
                _process,
                _trendBuffer,
                GetModelStep,
                GetControllerStep,
                CanConfigureDiscretizationSteps,
                ApplyDiscretizationSteps,
                CanInjectExperimentalDisturbance,
                ApplyLeakDisturbance,
                ApplySensorFaultDisturbance,
                ClearExperimentalDisturbances)
            {
                Owner = this
            };
            _disturbancesWindow.Closed += (_, _) =>
                _disturbancesWindow = null;
            _disturbancesWindow.Show();
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessDetailsWindow detailsWindow = new ProcessDetailsWindow(_process);
            detailsWindow.Show();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                LocalizationService.Text(
                    "Are you sure you want to exit the application?",
                    "Сигурни ли сте, че искате да излезете от приложението?"),
                LocalizationService.Text("Exit", "Изход"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _timer.Stop();
                TurnOffDevices();

                Close();
            }
        }
    }
}
