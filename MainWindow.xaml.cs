using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using DiplomWork_Ivan_2026.Controllers;
using DiplomWork_Ivan_2026.Services;
using DiplomWork_Ivan_2026.Simulation;
using DiplomWork_Ivan_2026.Trends;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DiplomWork_Ivan_2026
{
    public partial class MainWindow : Window
    {
        //Main
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly VacuumDryerProcess _process = new VacuumDryerProcess();
        private readonly Models.ProcessSettings _settings = new Models.ProcessSettings();
        private readonly AlarmService _alarmService = new AlarmService();
        private readonly SafetyInterlockService _safetyInterlockService = new SafetyInterlockService();
        private readonly TrendBuffer _trendBuffer = new TrendBuffer();

        private readonly OnOffTemperatureController _temperatureController = new OnOffTemperatureController();
        private readonly PiPressureController _pressureController = new PiPressureController();
        private readonly PidTemperatureController _pidTemperatureController = new PidTemperatureController();
        private readonly AutomaticProcessController _automaticProcessController = new AutomaticProcessController();
        private readonly List<double> _temperatureValues = new List<double>();
        private readonly List<double> _pressureValues = new List<double>();
        private readonly List<double> _moistureValues = new List<double>();

        private bool _isRunning = false;
        private bool _processStarted = false;
        private int _simulationSpeedMultiplier = 1;
        private double _automaticFanSpeedSetpoint = 70.0;
        private Enums.SensorFaultMode _lastChamberTemperatureFaultMode;
        private Enums.SensorFaultMode _lastMaterialTemperatureFaultMode;
        private Enums.SensorFaultMode _lastPressureFaultMode;

        private const double FixedTrendSampleIntervalSeconds = 1.0;
        private double _modelStepSeconds = 0.1;
        private double _controllerStepSeconds = 0.1;
        private double _controllerElapsedSeconds = 0.1;
        private int IntegrationSubstepsPerTrendSample => Math.Max(
            1,
            (int)Math.Round(
                FixedTrendSampleIntervalSeconds / _modelStepSeconds));

        public ISeries[] ProcessSeries { get; set; } = System.Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = System.Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = System.Array.Empty<Axis>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeChart();

            DataContext = this;

            MaterialComboBox.ItemsSource = MaterialLibrary.GetMaterials();
            MaterialComboBox.SelectedIndex = 0;

            OperationModeComboBox.SelectedIndex = 0;
            UpdateManualControlsState();

            _timer.Interval = System.TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
            Closed += (_, _) =>
                LocalizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            ApplyLocalization();
        }

        private void InitializeChart()
        {
            ProcessSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Temperature",
                    Values = _temperatureValues,
                    GeometrySize = 0,
                    Fill = null
                },
                new LineSeries<double>
                {
                    Name = "Pressure",
                    Values = _pressureValues,
                    GeometrySize = 0,
                    Fill = null
                },
                new LineSeries<double>
                {
                    Name = "Moisture",
                    Values = _moistureValues,
                    GeometrySize = 0,
                    Fill = null
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Time",
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    NamePaint = new SolidColorPaint(SKColors.White)
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Value",
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    NamePaint = new SolidColorPaint(SKColors.White)
                }
            };
        }
        private void TemperatureControlComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_pidTemperatureController == null)
                return;

            _pidTemperatureController.Reset();
        }
    }
}
