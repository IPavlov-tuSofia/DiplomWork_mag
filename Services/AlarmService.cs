using DiplomWork_Ivan_2026.Enums;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Simulation;

using System.Linq;

namespace DiplomWork_Ivan_2026.Services
{
    public class AlarmService
    {
        public List<AlarmInfo> ActiveAlarms { get; } = new List<AlarmInfo>();
        public List<AlarmInfo> AlarmHistory { get; } = new List<AlarmInfo>();

        public void CheckAlarms(VacuumDryerProcess process, ProcessSettings settings)
        {
            ActiveAlarms.Clear();

            var state = process.State;
            var material = process.SelectedMaterial;

            if (material == null)
            {
                UpdateHistoryStatus();
                return;
            }

            if (state.MeasuredMaterialTemperature > material.MaxTemperature)
            {
                AddAlarm(new AlarmInfo
                {
                    Type = AlarmType.HighTemperature,
                    Severity = AlarmSeverity.Critical,
                    Message = $"Material temperature is above its safe limit: {state.MeasuredMaterialTemperature:F1} / {material.MaxTemperature:F1} °C.",
                    MessageBulgarian = $"Температурата на материала е над безопасната граница: {state.MeasuredMaterialTemperature:F1} / {material.MaxTemperature:F1} °C.",
                    RecommendedAction = "Stop heating, keep circulation if safe, and inspect the temperature measurement.",
                    RecommendedActionBulgarian = "Спрете нагряването, запазете циркулацията, ако е безопасно, и проверете измерването на температурата."
                });

            }

            if (state.SafetyInterlockActive)
            {
                AddAlarm(new AlarmInfo
                {
                    Type = state.EmergencyStopActive
                        ? AlarmType.EmergencyStop
                        : AlarmType.SafetyInterlock,
                    Severity = AlarmSeverity.Critical,
                    Message = state.SafetyInterlockReason,
                    MessageBulgarian = "Задействана е защитната блокировка.",
                    RecommendedAction = "Remove the cause, verify safe process values, then use Reset Safety.",
                    RecommendedActionBulgarian = "Отстранете причината, проверете безопасните стойности и използвайте Нулиране на защитата."
                });
            }

            if (process.HasSensorFault)
            {
                AddAlarm(new AlarmInfo
                {
                    Type = AlarmType.SensorFault,
                    Severity = AlarmSeverity.Critical,
                    Message = "A critical virtual sensor is in a fault state.",
                    MessageBulgarian = "Критичен виртуален датчик е в състояние на повреда.",
                    RecommendedAction = "Inspect the sensor status and restore a valid signal before restarting.",
                    RecommendedActionBulgarian = "Проверете датчика и възстановете валиден сигнал преди повторно стартиране."
                });
            }

            if (settings.TemperatureSetpoint > material.MaxTemperature)
            {
                AddAlarm(new AlarmInfo
                {
                    Type = AlarmType.SetpointAboveMaterialLimit,
                    Severity = AlarmSeverity.Warning,
                    Message = $"Temperature setpoint exceeds the material limit: {settings.TemperatureSetpoint:F1} / {material.MaxTemperature:F1} °C.",
                    MessageBulgarian = $"Заданието за температура надвишава границата на материала: {settings.TemperatureSetpoint:F1} / {material.MaxTemperature:F1} °C.",
                    RecommendedAction = $"Reduce the setpoint to {material.MaxTemperature:F1} °C or below.",
                    RecommendedActionBulgarian = $"Намалете заданието до {material.MaxTemperature:F1} °C или по-ниско."
                });
            }

            bool pressureControlStage = state.ProcessStage is
                ProcessStage.Evacuation or
                ProcessStage.Drying or
                ProcessStage.FinalDrying;
            const double pressureDeviationWarningKPa = 5.0;

            bool pressureDeviationHasPersistedLongEnough =
                state.ProcessStage != ProcessStage.Evacuation ||
                state.StageElapsedTime >= 30.0;

            if (pressureControlStage &&
                pressureDeviationHasPersistedLongEnough &&
                state.MeasuredPressure >
                    state.ActivePressureSetpoint + pressureDeviationWarningKPa)
            {
                AddAlarm(new AlarmInfo
                {
                    Type = AlarmType.PressureTooHigh,
                    Severity = AlarmSeverity.Warning,
                    Message = $"Pressure is above the active target: {state.MeasuredPressure:F1} / {state.ActivePressureSetpoint:F1} kPa.",
                    MessageBulgarian = $"Налягането е над активното задание: {state.MeasuredPressure:F1} / {state.ActivePressureSetpoint:F1} kPa.",
                    RecommendedAction = "Check the vacuum pump output, vent position, chamber seal, and pressure sensor. If the value is still moving toward SP, continue monitoring.",
                    RecommendedActionBulgarian = "Проверете помпата, клапана, уплътнението и датчика за налягане. Ако стойността се приближава към заданието, продължете наблюдението."
                });
            }

            if (state.MeasuredPressure <= process.Parameters.MinimumPressureKPa + 0.1)
            {
                AddAlarm(new AlarmInfo
                {
                    Type = AlarmType.PressureTooLow,
                    Severity = AlarmSeverity.Critical,
                    Message = $"Pressure is at or below the safe model limit: {state.MeasuredPressure:F1} kPa.",
                    MessageBulgarian = $"Налягането е на или под безопасната моделна граница: {state.MeasuredPressure:F1} kPa.",
                    RecommendedAction = "Stop evacuation and verify the pressure sensor and vacuum control.",
                    RecommendedActionBulgarian = "Спрете вакуумирането и проверете датчика и управлението на вакуума."
                });
            }

            if (state.IsCompleted)
            {
                AddAlarm(new AlarmInfo
                {
                    Type = AlarmType.ProcessCompleted,
                    Severity = AlarmSeverity.Info,
                    Message = "Drying process completed.",
                    MessageBulgarian = "Процесът на сушене е завършен.",
                    RecommendedAction = "Review the final values and reset the process before loading a new batch.",
                    RecommendedActionBulgarian = "Прегледайте крайните стойности и нулирайте процеса преди нова партида."
                });
            }

            ActiveAlarms.Sort((left, right) =>
            {
                int severityOrder = right.Severity.CompareTo(left.Severity);
                return severityOrder != 0
                    ? severityOrder
                    : right.Time.CompareTo(left.Time);
            });
            UpdateHistoryStatus();
        }
        private void AddAlarm(AlarmInfo alarm)
        {
            AlarmInfo? existingOccurrence = AlarmHistory.FirstOrDefault(a =>
                a.Type == alarm.Type &&
                a.IsActive);

            if (existingOccurrence != null)
            {
                existingOccurrence.Severity = alarm.Severity;
                existingOccurrence.Message = alarm.Message;
                existingOccurrence.MessageBulgarian = alarm.MessageBulgarian;
                existingOccurrence.RecommendedAction = alarm.RecommendedAction;
                existingOccurrence.RecommendedActionBulgarian = alarm.RecommendedActionBulgarian;
                ActiveAlarms.Add(existingOccurrence);
                return;
            }

            alarm.Time = DateTime.Now;
            alarm.IsActive = true;
            ActiveAlarms.Add(alarm);
            AlarmHistory.Insert(0, alarm);
        }
        private void UpdateHistoryStatus()
        {
            foreach (var historyAlarm in AlarmHistory)
            {
                historyAlarm.IsActive = ActiveAlarms.Contains(historyAlarm);
            }
        }
    }
}
