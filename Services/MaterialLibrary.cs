using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiplomWork_Ivan_2026.Models;

namespace DiplomWork_Ivan_2026.Services
{
    public static class MaterialLibrary
    {
            public static List<DryingMaterial> GetMaterials()
            {
                return new List<DryingMaterial>
               {
                new DryingMaterial
                {
                    Name = "Herbs",
                    InitialMoistureWetBasisPercent = 70,
                    TargetMoistureWetBasisPercent = 10,
                    EquilibriumMoistureWetBasisPercent = 5,
                    MaxTemperature = 45,
                    DryingCoefficient = 0.8,
                    InitialWetMassKg = 5,
                    SoftRecipe = Recipe(35, 55, 50),
                    NormalRecipe = Recipe(40, 45, 70),
                    HardRecipe = Recipe(43, 35, 90)
                },

                new DryingMaterial
                {
                    Name = "Grain",
                    InitialMoistureWetBasisPercent = 25,
                    TargetMoistureWetBasisPercent = 13,
                    EquilibriumMoistureWetBasisPercent = 8,
                    MaxTemperature = 60,
                    DryingCoefficient = 0.5,
                    InitialWetMassKg = 20,
                    SoftRecipe = Recipe(45, 50, 50),
                    NormalRecipe = Recipe(52, 35, 70),
                    HardRecipe = Recipe(58, 25, 90)
                },

                new DryingMaterial
                {
                    Name = "Wood",
                    InitialMoistureWetBasisPercent = 50,
                    TargetMoistureWetBasisPercent = 12,
                    EquilibriumMoistureWetBasisPercent = 6,
                    MaxTemperature = 80,
                    DryingCoefficient = 0.3,
                    InitialWetMassKg = 15,
                    SoftRecipe = Recipe(45, 50, 50),
                    NormalRecipe = Recipe(60, 30, 70),
                    HardRecipe = Recipe(75, 20, 100)
                },

                new DryingMaterial
                {
                    Name = "Fruits",
                    InitialMoistureWetBasisPercent = 80,
                    TargetMoistureWetBasisPercent = 15,
                    EquilibriumMoistureWetBasisPercent = 8,
                    MaxTemperature = 55,
                    DryingCoefficient = 0.6,
                    InitialWetMassKg = 8,
                    SoftRecipe = Recipe(38, 55, 50),
                    NormalRecipe = Recipe(47, 40, 70),
                    HardRecipe = Recipe(53, 30, 90)
                }
               };
            }

            private static DryingRecipe Recipe(
                double temperatureSetpointC,
                double pressureSetpointKPa,
                double fanSpeedPercent)
            {
                return new DryingRecipe
                {
                    TemperatureSetpointC = temperatureSetpointC,
                    PressureSetpointKPa = pressureSetpointKPa,
                    FanSpeedPercent = fanSpeedPercent
                };
            }
    }
    
}
