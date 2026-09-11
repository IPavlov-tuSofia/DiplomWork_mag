using DiplomWork_Ivan_2026.Enums;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Simulation;

namespace DiplomWork_Ivan_2026.Services
{
    public class SafetyInterlockService
    {
        private const double MinimumSafePressureKPa = 5.1;
        private const double VacuumEstablishmentTimeoutSeconds = 600.0;
        private static string L(string en, string bg) => LocalizationService.Text(en, bg);

        public bool Evaluate(
            VacuumDryerProcess process,
            ProcessSettings settings)
        {
            VacuumDryerState state = process.State;
            DryingMaterial? material = process.SelectedMaterial;

            if (state.SafetyInterlockActive || state.IsCompleted || material == null)
                return state.SafetyInterlockActive;

            if (process.HasSensorFault)
            {
                Trip(process, L(
                    "A critical virtual sensor is in a fault state.",
                    "Критичен виртуален датчик е в състояние на повреда."));
            }
            else if (settings.TemperatureSetpoint > material.MaxTemperature)
            {
                Trip(
                    process,
                    L(
                        $"Temperature setpoint {settings.TemperatureSetpoint:F1} °C exceeds the {material.Name} limit {material.MaxTemperature:F1} °C.",
                        $"Заданието за температура {settings.TemperatureSetpoint:F1} °C надвишава границата за {material}: {material.MaxTemperature:F1} °C."));
            }
            else if (state.MeasuredMaterialTemperature > material.MaxTemperature)
            {
                Trip(
                    process,
                    L(
                        $"Material temperature {state.MeasuredMaterialTemperature:F1} °C exceeds the limit {material.MaxTemperature:F1} °C.",
                        $"Температурата на материала {state.MeasuredMaterialTemperature:F1} °C надвишава границата {material.MaxTemperature:F1} °C."));
            }
            else if (state.MeasuredPressure <= MinimumSafePressureKPa)
            {
                Trip(
                    process,
                    L(
                        $"Absolute pressure {state.MeasuredPressure:F1} kPa is below the safe limit.",
                        $"Абсолютното налягане {state.MeasuredPressure:F1} kPa е под безопасната граница."));
            }
            else if (state.ProcessStage == ProcessStage.Evacuation &&
                state.StageElapsedTime >= VacuumEstablishmentTimeoutSeconds &&
                state.MeasuredPressure > state.ActivePressureSetpoint + 5.0)
            {
                Trip(
                    process,
                    L(
                        "The requested vacuum was not established within the allowed time.",
                        "Зададеният вакуум не е достигнат в допустимото време."));
            }

            return state.SafetyInterlockActive;
        }

        public void Trip(
            VacuumDryerProcess process,
            string reason,
            bool isEmergencyStop = false)
        {
            VacuumDryerState state = process.State;
            state.SafetyInterlockActive = true;
            state.EmergencyStopActive |= isEmergencyStop;
            state.SafetyInterlockReason = reason;
            state.ProcessStage = ProcessStage.SafetyShutdown;
            ApplySafeOutputs(process);
        }

        public bool TryReset(
            VacuumDryerProcess process,
            ProcessSettings settings,
            out string reason)
        {
            VacuumDryerState state = process.State;
            DryingMaterial? material = process.SelectedMaterial;

            if (process.HasSensorFault)
            {
                reason = L(
                    "Clear the virtual sensor fault before resetting safety.",
                    "Отстранете повредата на виртуалния датчик преди нулиране на защитата.");
                return false;
            }

            if (material != null &&
                state.MeasuredMaterialTemperature > material.MaxTemperature)
            {
                reason = L(
                    "Material temperature is still above the safe limit.",
                    "Температурата на материала все още е над безопасната граница.");
                return false;
            }

            if (state.MeasuredPressure <= MinimumSafePressureKPa)
            {
                reason = L(
                    "Pressure is still below the safe reset limit.",
                    "Налягането все още е под границата за безопасно нулиране.");
                return false;
            }

            if (material != null &&
                settings.TemperatureSetpoint > material.MaxTemperature)
            {
                reason = L(
                    "Reduce the temperature setpoint before resetting safety.",
                    "Намалете заданието за температура преди нулиране на защитата.");
                return false;
            }

            state.SafetyInterlockActive = false;
            state.EmergencyStopActive = false;
            state.SafetyInterlockReason = "";
            state.ProcessStage = ProcessStage.Idle;
            state.StageElapsedTime = 0.0;
            ApplySafeOutputs(process);

            reason = "";
            return true;
        }

        public void Clear(VacuumDryerProcess process)
        {
            process.State.SafetyInterlockActive = false;
            process.State.EmergencyStopActive = false;
            process.State.SafetyInterlockReason = "";
        }

        public static void ApplySafeOutputs(VacuumDryerProcess process)
        {
            process.Heater.TurnOff();
            process.Pump.TurnOff();
            process.VentValve.Close();
            process.Fan.TurnOff();

            process.State.HeaterPower = 0.0;
            process.State.VacuumPumpPower = 0.0;
            process.State.VentValveOpening = 0.0;
            process.State.FanSpeed = 0.0;
        }
    }
}
