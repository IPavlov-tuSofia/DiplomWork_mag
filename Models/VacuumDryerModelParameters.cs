namespace DiplomWork_Ivan_2026.Models
{
    /// <summary>
    /// Calibratable parameters of the reduced vacuum-dryer model.
    /// Values are engineering estimates for a 0.5 m³ laboratory/pilot chamber;
    /// they are kept here (instead of inside the equations) so they can be
    /// replaced by identified values without changing the model structure.
    /// </summary>
    public sealed class VacuumDryerModelParameters
    {
        public double ChamberVolumeM3 { get; init; } = 0.50;

        public double HeaterNominalPowerKw { get; init; } = 5.0;
        public double PumpNominalPowerKw { get; init; } = 1.5;
        public double FanNominalPowerKw { get; init; } = 0.5;
        public double HeaterEfficiency { get; init; } = 0.90;

        public double ChamberHeatCapacityJPerK { get; init; } = 60_000.0;
        public double ChamberToMaterialHeatTransferWPerK { get; init; } = 90.0;
        public double ChamberAmbientHeatLossWPerK { get; init; } = 30.0;
        public double DryMaterialSpecificHeatJPerKgK { get; init; } = 1_600.0;
        public double WaterSpecificHeatJPerKgK { get; init; } = 4_180.0;
        public double WaterLatentHeatJPerKg { get; init; } = 2_300_000.0;

        public double DryingPreExponentialFactorPerSecond { get; init; } = 0.12;
        public double DryingActivationEnergyJPerMol { get; init; } = 16_000.0;
        public double ReferenceTemperatureC { get; init; } = 20.0;
        public double ReferenceRelativeHumidityPercent { get; init; } = 50.0;
        public double EquilibriumMoistureHumidityExponent { get; init; } = 0.55;
        public double EquilibriumMoistureTemperatureCoefficientPerC { get; init; } = 0.008;
        public double MinimumEquilibriumMoistureFraction { get; init; } = 0.20;

        // At 100% fan command the effective coefficients are multiplied by
        // (1 + gain). The fan circulates gas; it does not destroy water vapour.
        public double FanHeatTransferGain { get; init; } = 0.60;
        public double FanMassTransferGain { get; init; } = 0.50;

        public double MinimumPressureInfluence { get; init; } = 0.5;
        public double MinimumPressureKPa { get; init; } = 5.0;
        public double VacuumTimeConstantSeconds { get; init; } = 25.0;
        public double VentValveTimeConstantSeconds { get; init; } = 20.0;
        public double LeakageCoefficientPerSecond { get; init; } = 0.0015;

        public double MaxAirFlowRateM3PerHour { get; init; } = 200.0;
    }
}
