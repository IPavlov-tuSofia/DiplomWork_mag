using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiplomWork_Ivan_2026.Enums;
using DiplomWork_Ivan_2026.Services;

namespace DiplomWork_Ivan_2026.Models
{
    public class DryingMaterial
    {
        public string Name { get; set; } = "";

        public double InitialMoistureWetBasisPercent { get; set; }
        public double TargetMoistureWetBasisPercent { get; set; }
        // Reference equilibrium moisture at 20 °C and 50% relative humidity.
        // The process model adjusts it dynamically for the actual chamber state.
        public double EquilibriumMoistureWetBasisPercent { get; set; }
        public double MaxTemperature { get; set; }       // [°C]

        // Dimensionless material correction applied to the common kinetic model.
        // These values are illustrative and must be calibrated with experimental data.
        public double DryingCoefficient { get; set; }

        public double InitialWetMassKg { get; set; } = 10.0;

        public DryingRecipe SoftRecipe { get; set; } = new DryingRecipe();
        public DryingRecipe NormalRecipe { get; set; } = new DryingRecipe();
        public DryingRecipe HardRecipe { get; set; } = new DryingRecipe();

        public DryingRecipe GetRecipe(DryingMode mode)
        {
            return mode switch
            {
                DryingMode.Soft => SoftRecipe,
                DryingMode.Hard => HardRecipe,
                _ => NormalRecipe
            };
        }

        public double InitialMoistureDryBasis =>
            WetBasisPercentToDryBasis(InitialMoistureWetBasisPercent);

        public double TargetMoistureDryBasis =>
            WetBasisPercentToDryBasis(TargetMoistureWetBasisPercent);

        public double EquilibriumMoistureDryBasis =>
            WetBasisPercentToDryBasis(EquilibriumMoistureWetBasisPercent);

        public double DryMassKg =>
            InitialWetMassKg * (1.0 - InitialMoistureWetBasisPercent / 100.0);

        public static double WetBasisPercentToDryBasis(double wetBasisPercent)
        {
            double wetBasisFraction = Math.Clamp(wetBasisPercent / 100.0, 0.0, 0.999999);
            return wetBasisFraction / (1.0 - wetBasisFraction);
        }

        public static double DryBasisToWetBasisPercent(double dryBasis)
        {
            double nonNegativeDryBasis = Math.Max(0.0, dryBasis);
            return 100.0 * nonNegativeDryBasis / (1.0 + nonNegativeDryBasis);
        }
        public override string ToString()
        {
            if (!LocalizationService.IsBulgarian)
                return Name;

            return Name switch
            {
                "Herbs" => "Билки",
                "Grain" => "Зърно",
                "Wood" => "Дървесина",
                "Fruits" => "Плодове",
                _ => Name
            };
        }
    }
}
