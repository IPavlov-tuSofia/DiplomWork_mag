namespace DiplomWork_Ivan_2026.Trends
{
    public class TrendPoint
    {
        public double Time { get; set; }
        public Enums.ProcessStage ProcessStage { get; set; }
        public double StageElapsedTime { get; set; }
        public int SimulationSpeedMultiplier { get; set; }

        // Temperature group
        public double Temperature { get; set; }                 // Chamber temperature
        public double MaterialTemperature { get; set; }
        public double TemperatureSetpoint { get; set; }
        public double ModelTemperature { get; set; }
        public double ModelMaterialTemperature { get; set; }

        // Pressure group
        public double Pressure { get; set; }
        public double PressureSetpoint { get; set; }
        public double VacuumLevel { get; set; }
        public double ModelPressure { get; set; }

        // Moisture / humidity group
        public double MaterialMoisture { get; set; }          // wet-basis [%]
        public double MaterialMoistureDryBasis { get; set; }  // kg water / kg dry matter
        public double EquilibriumMoisture { get; set; }       // wet-basis [%]
        public double AirHumidity { get; set; }
        public double MoistureRatio { get; set; }

        // Drying group
        public double DryingRate { get; set; }               // wet-basis [%/min]
        public double AirFlowRate { get; set; }
        public double EvaporationRateKgPerSecond { get; set; }

        // Energy group
        public double TotalEnergyKWh { get; set; }
        public double EvaporatedWaterKg { get; set; }
        public double EfficiencyKgPerKWh { get; set; }

        // Water-vapour balance group
        public double WaterVaporPartialPressureKPa { get; set; }
        public double WaterVaporMassKg { get; set; }
        public double PumpedWaterVaporKg { get; set; }
        public double CondensedWaterKg { get; set; }
        public double AmbientWaterVaporIngressKg { get; set; }
        public double WaterVaporMassBalanceResidualKg { get; set; }

        // Actuator group
        public double HeaterPower { get; set; }
        public double PumpPower { get; set; }
        public double VentValveOpening { get; set; }
        public double FanSpeed { get; set; }

        // Process flags
        public bool MoistureTargetReached { get; set; }
        public bool SafetyInterlockActive { get; set; }
        public bool IsCompleted { get; set; }
    }
}
