using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomWork_Ivan_2026.Enums;

namespace DiplomWork_Ivan_2026.Models
{
    public class VacuumDryerState
    {
        public double Temperature { get; set; } = 20.0;          // Chamber temperature [°C]
        public double MaterialTemperature { get; set; } = 20.0;  // Material temperature [°C]
        public double MaterialMoistureDryBasis { get; set; } = 1.5; // kg water/kg dry matter
        public double AirHumidity { get; set; } = 50.0;          // %
        public double WaterVaporPartialPressureKPa { get; set; } = 1.17;
        public double WaterVaporMassKg { get; set; } = 0.0043;
        public double InitialWaterVaporMassKg { get; set; } = 0.0043;
        public double PumpedWaterVaporKg { get; set; } = 0.0;
        public double CondensedWaterKg { get; set; } = 0.0;
        public double AmbientWaterVaporIngressKg { get; set; } = 0.0;
        public double DynamicEquilibriumMoistureDryBasis { get; set; } = 0.0;
        public double Pressure { get; set; } = 101.3;            // kPa
        public double MeasuredTemperature { get; set; } = 20.0;
        public double MeasuredMaterialTemperature { get; set; } = 20.0;
        public double MeasuredPressure { get; set; } = 101.3;

        public double MaterialMoistureWetBasisPercent =>
            DryingMaterial.DryBasisToWetBasisPercent(MaterialMoistureDryBasis);

        public double HeaterPower { get; set; } = 0.0;           // %
        public double FanSpeed { get; set; } = 0.0;              // %
        public double VacuumPumpPower { get; set; } = 0.0;       // %
        public double VentValveOpening { get; set; } = 0.0;      // %
        public double VacuumLevel { get; set; } = 0.0;           // [%]
        public double AirFlowRate { get; set; } = 0.0;           // [m³/h]
        public double DryingRateDryBasisPerSecond { get; set; } = 0.0; // kg/(kg dry matter·s)
        public double EvaporationRateKgPerSecond { get; set; } = 0.0;

        public double DryingRateWetBasisPercentPerMinute
        {
            get
            {
                double denominator = 1.0 + Math.Max(0.0, MaterialMoistureDryBasis);
                return 60.0 * 100.0 * DryingRateDryBasisPerSecond /
                       (denominator * denominator);
            }
        }

        public double TotalEnergyKWh { get; set; } = 0.0;
        public double EvaporatedWaterKg { get; set; } = 0.0;
        public double WaterVaporMassBalanceResidualKg =>
            InitialWaterVaporMassKg + EvaporatedWaterKg +
            AmbientWaterVaporIngressKg - WaterVaporMassKg -
            PumpedWaterVaporKg - CondensedWaterKg;
        public double EfficiencyKgPerKWh { get; set; } = 0.0;
        public double MoistureRatio { get; set; } = 1.0;
        public double? EstimatedRemainingTimeSeconds { get; set; }

        public double ElapsedTime { get; set; } = 0.0;           // seconds
        public ProcessStage ProcessStage { get; set; } = ProcessStage.Idle;
        public double StageElapsedTime { get; set; } = 0.0;
        public double ActiveTemperatureSetpoint { get; set; } = 20.0;
        public double ActivePressureSetpoint { get; set; } = 101.3;
        public bool MoistureTargetReached { get; set; } = false;
        public bool SafetyInterlockActive { get; set; } = false;
        public bool EmergencyStopActive { get; set; } = false;
        public string SafetyInterlockReason { get; set; } = "";
        public bool IsCompleted { get; set; } = false;

    }
}
