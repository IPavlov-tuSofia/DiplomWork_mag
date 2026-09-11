using DiplomWork_Ivan_2026.Models;

namespace DiplomWork_Ivan_2026.Trends
{
    public class TrendBuffer
    {
        public const int HistoryDurationHours = 24;
        public const double HistoryDurationSeconds = HistoryDurationHours * 60.0 * 60.0;
        public const int DefaultMaxPoints = (int)HistoryDurationSeconds + 1;

        private readonly TrendPointRingBuffer _points;

        public IReadOnlyList<TrendPoint> Points => _points;
        public ExperimentMetadata? Metadata { get; private set; }

        public TrendBuffer(int maxPoints = DefaultMaxPoints)
        {
            _points = new TrendPointRingBuffer(maxPoints);
        }

        public void BeginExperiment(ExperimentMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            _points.Clear();
            Metadata = metadata;
        }

        public void AddPoint(
            VacuumDryerState state,
            ProcessSettings settings,
            int simulationSpeedMultiplier)
        {
            _points.Add(new TrendPoint
            {
                Time = state.ElapsedTime,
                ProcessStage = state.ProcessStage,
                StageElapsedTime = state.StageElapsedTime,
                SimulationSpeedMultiplier = simulationSpeedMultiplier,

                // Temperature group
                Temperature = state.MeasuredTemperature,
                MaterialTemperature = state.MeasuredMaterialTemperature,
                TemperatureSetpoint = state.ActiveTemperatureSetpoint,
                ModelTemperature = state.Temperature,
                ModelMaterialTemperature = state.MaterialTemperature,

                // Pressure group
                Pressure = state.MeasuredPressure,
                PressureSetpoint = state.ActivePressureSetpoint,
                VacuumLevel = state.VacuumLevel,
                ModelPressure = state.Pressure,

                // Moisture / humidity group
                MaterialMoisture = state.MaterialMoistureWetBasisPercent,
                MaterialMoistureDryBasis = state.MaterialMoistureDryBasis,
                EquilibriumMoisture =
                    DryingMaterial.DryBasisToWetBasisPercent(
                        state.DynamicEquilibriumMoistureDryBasis),
                AirHumidity = state.AirHumidity,
                MoistureRatio = state.MoistureRatio,

                // Drying group
                DryingRate = state.DryingRateWetBasisPercentPerMinute,
                AirFlowRate = state.AirFlowRate,
                EvaporationRateKgPerSecond = state.EvaporationRateKgPerSecond,

                // Energy group
                TotalEnergyKWh = state.TotalEnergyKWh,
                EvaporatedWaterKg = state.EvaporatedWaterKg,
                EfficiencyKgPerKWh = state.EfficiencyKgPerKWh,

                // Water-vapour balance group
                WaterVaporPartialPressureKPa = state.WaterVaporPartialPressureKPa,
                WaterVaporMassKg = state.WaterVaporMassKg,
                PumpedWaterVaporKg = state.PumpedWaterVaporKg,
                CondensedWaterKg = state.CondensedWaterKg,
                AmbientWaterVaporIngressKg = state.AmbientWaterVaporIngressKg,
                WaterVaporMassBalanceResidualKg = state.WaterVaporMassBalanceResidualKg,

                // Actuator group
                HeaterPower = state.HeaterPower,
                PumpPower = state.VacuumPumpPower,
                VentValveOpening = state.VentValveOpening,
                FanSpeed = state.FanSpeed,

                // Process flags
                MoistureTargetReached = state.MoistureTargetReached,
                SafetyInterlockActive = state.SafetyInterlockActive,
                IsCompleted = state.IsCompleted
            });

        }

        public void Clear()
        {
            _points.Clear();
            Metadata = null;
        }
    }
}
