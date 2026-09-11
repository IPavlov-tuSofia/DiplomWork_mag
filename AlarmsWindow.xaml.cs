using System.Windows;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using DiplomWork_Ivan_2026.Services;
using DiplomWork_Ivan_2026.Enums;

namespace DiplomWork_Ivan_2026
{
    public partial class AlarmsWindow : Window
    {
        private readonly AlarmService _alarmService;
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();

        public AlarmsWindow(AlarmService alarmService)
        {
            InitializeComponent();

            _alarmService = alarmService;

            _refreshTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
            ApplyLocalization();

            UpdateAlarmTable();
        }

        private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
        {
            ApplyLocalization();
            UpdateAlarmTable();
        }

        private void ApplyLocalization() => LocalizationService.ApplyStaticText(this);

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            UpdateAlarmTable();
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer.Stop();
            LocalizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            base.OnClosed(e);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateAlarmTable();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        private void UpdateAlarmTable()
        {
            var rows = _alarmService.AlarmHistory
                .Select(a => new AlarmRow
                {
                    Status = a.IsActive
                        ? LocalizationService.Text("ACTIVE", "АКТИВНА")
                        : LocalizationService.Text("CLEARED", "ИЗЧИСТЕНА"),
                    StatusForeground = a.IsActive ? Brushes.OrangeRed : Brushes.LimeGreen,
                    Priority = LocalizeSeverity(a.Severity),
                    PriorityForeground = GetSeverityBrush(a.Severity),
                    Date = a.Time.ToString("dd.MM.yyyy"),
                    Time = a.Time.ToString("HH:mm:ss"),
                    Type = LocalizeAlarmType(a.Type),
                    Description = a.LocalizedMessage,
                    RecommendedAction = a.LocalizedRecommendedAction
                })
                .ToList();

            AlarmHistoryDataGrid.ItemsSource = rows;

            ActiveAlarmsCountTextBlock.Text =
                $"{LocalizationService.Text("Active alarms", "Активни аларми")}: {_alarmService.ActiveAlarms.Count}";

            TotalAlarmsCountTextBlock.Text =
                $"{LocalizationService.Text("Total alarms", "Общо аларми")}: {_alarmService.AlarmHistory.Count}";
        }

        private static string LocalizeSeverity(AlarmSeverity severity) => severity switch
        {
            AlarmSeverity.Critical => LocalizationService.Text("Critical", "Критична"),
            AlarmSeverity.Warning => LocalizationService.Text("Warning", "Предупреждение"),
            _ => LocalizationService.Text("Info", "Информация")
        };

        private static Brush GetSeverityBrush(AlarmSeverity severity) => severity switch
        {
            AlarmSeverity.Critical => Brushes.OrangeRed,
            AlarmSeverity.Warning => Brushes.Gold,
            _ => Brushes.DeepSkyBlue
        };

        private static string LocalizeAlarmType(AlarmType type) => type switch
        {
            AlarmType.HighTemperature => LocalizationService.Text("High Temperature", "Висока температура"),
            AlarmType.SetpointAboveMaterialLimit => LocalizationService.Text("Setpoint Above Material Limit", "Задание над границата на материала"),
            AlarmType.PressureTooHigh => LocalizationService.Text("Pressure Too High", "Твърде високо налягане"),
            AlarmType.PressureTooLow => LocalizationService.Text("Pressure Too Low", "Твърде ниско налягане"),
            AlarmType.VacuumTimeout => LocalizationService.Text("Vacuum Timeout", "Изтекло време за вакуумиране"),
            AlarmType.SafetyInterlock => LocalizationService.Text("Safety Interlock", "Защитна блокировка"),
            AlarmType.EmergencyStop => LocalizationService.Text("Emergency Stop", "Аварийно спиране"),
            AlarmType.SensorFault => LocalizationService.Text("Sensor Fault", "Повреда на датчик"),
            AlarmType.ProcessCompleted => LocalizationService.Text("Process Completed", "Процесът е завършен"),
            _ => type.ToString()
        };

        private class AlarmRow
        {
            public string Status { get; set; } = "";
            public Brush StatusForeground { get; set; } = Brushes.White;
            public string Priority { get; set; } = "";
            public Brush PriorityForeground { get; set; } = Brushes.White;
            public string Date { get; set; } = "";
            public string Time { get; set; } = "";
            public string Type { get; set; } = "";
            public string Description { get; set; } = "";
            public string RecommendedAction { get; set; } = "";
        }
    }
}
