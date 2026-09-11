using System.Globalization;
using System.IO;
using System.Text;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Trends;

namespace DiplomWork_Ivan_2026.Services
{
    public static class TrendCsvExporter
    {
        private const char Separator = ',';

        private static readonly string[] Headers =
        {
            "SampleNumber",
            "ElapsedTime_s",
            "ProcessStage",
            "StageElapsedTime_s",
            "SimulationSpeedMultiplier",
            "ChamberTemperatureMeasured_C",
            "ChamberTemperatureModel_C",
            "MaterialTemperatureMeasured_C",
            "MaterialTemperatureModel_C",
            "TemperatureSetpoint_C",
            "PressureMeasured_kPa",
            "PressureModel_kPa",
            "PressureSetpoint_kPa",
            "VacuumLevel_pct",
            "MaterialMoisture_wb_pct",
            "MaterialMoisture_db_kg_per_kg",
            "EquilibriumMoisture_wb_pct",
            "AirHumidity_pct",
            "MoistureRatio",
            "AirFlowRate_m3_per_h",
            "DryingRate_wb_pct_per_min",
            "EvaporationRate_kg_per_s",
            "TotalEnergy_kWh",
            "EvaporatedWater_kg",
            "Efficiency_kg_per_kWh",
            "WaterVaporPartialPressure_kPa",
            "WaterVaporMass_kg",
            "PumpedWaterVapor_kg",
            "CondensedWater_kg",
            "AmbientWaterVaporIngress_kg",
            "WaterVaporMassBalanceResidual_kg",
            "HeaterPower_pct",
            "VacuumPumpPower_pct",
            "VentValveOpening_pct",
            "FanSpeed_pct",
            "MoistureTargetReached",
            "SafetyInterlockActive",
            "IsCompleted"
        };

        public static void Export(
            string filePath,
            IReadOnlyList<TrendPoint> points,
            ExperimentMetadata? metadata)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(points);

            using StreamWriter writer = new StreamWriter(
                filePath,
                false,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            WriteMetadata(writer, metadata);
            writer.WriteLine();
            writer.WriteLine("Process Data");
            writer.WriteLine(string.Join(Separator, Headers));

            for (int index = 0; index < points.Count; index++)
            {
                TrendPoint point = points[index];
                string[] values =
                {
                    (index + 1).ToString(CultureInfo.InvariantCulture),
                    Number(point.Time),
                    Escape(point.ProcessStage.ToString()),
                    Number(point.StageElapsedTime),
                    point.SimulationSpeedMultiplier.ToString(
                        CultureInfo.InvariantCulture),
                    Number(point.Temperature),
                    Number(point.ModelTemperature),
                    Number(point.MaterialTemperature),
                    Number(point.ModelMaterialTemperature),
                    Number(point.TemperatureSetpoint),
                    Number(point.Pressure),
                    Number(point.ModelPressure),
                    Number(point.PressureSetpoint),
                    Number(point.VacuumLevel),
                    Number(point.MaterialMoisture),
                    Number(point.MaterialMoistureDryBasis),
                    Number(point.EquilibriumMoisture),
                    Number(point.AirHumidity),
                    Number(point.MoistureRatio),
                    Number(point.AirFlowRate),
                    Number(point.DryingRate),
                    Number(point.EvaporationRateKgPerSecond),
                    Number(point.TotalEnergyKWh),
                    Number(point.EvaporatedWaterKg),
                    Number(point.EfficiencyKgPerKWh),
                    Number(point.WaterVaporPartialPressureKPa),
                    Number(point.WaterVaporMassKg),
                    Number(point.PumpedWaterVaporKg),
                    Number(point.CondensedWaterKg),
                    Number(point.AmbientWaterVaporIngressKg),
                    Number(point.WaterVaporMassBalanceResidualKg),
                    Number(point.HeaterPower),
                    Number(point.PumpPower),
                    Number(point.VentValveOpening),
                    Number(point.FanSpeed),
                    Boolean(point.MoistureTargetReached),
                    Boolean(point.SafetyInterlockActive),
                    Boolean(point.IsCompleted)
                };

                writer.WriteLine(string.Join(Separator, values));
            }
        }

        private static void WriteMetadata(
            StreamWriter writer,
            ExperimentMetadata? metadata)
        {
            writer.WriteLine("Experiment Metadata");
            writer.WriteLine("Parameter,Value,Unit");
            WriteMetadataRow(writer, "CsvSchemaVersion", "2", "");
            WriteMetadataRow(
                writer,
                "ExportedAtLocal",
                DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                "");

            if (metadata == null)
            {
                WriteMetadataRow(writer, "MetadataStatus", "Unavailable", "");
                WriteMetadataRow(writer, "DisturbanceType", "Unknown", "");
                WriteMetadataRow(writer, "DisturbanceTime", "Unknown", "s");
                return;
            }

            WriteMetadataRow(writer, "ExperimentId", metadata.ExperimentId, "");
            WriteMetadataRow(writer, "RunLabel", metadata.RunLabel, "");
            WriteMetadataRow(
                writer,
                "ExperimentStartedAtLocal",
                metadata.StartedAt.ToString("O", CultureInfo.InvariantCulture),
                "");
            WriteMetadataRow(writer, "ProgramVersion", metadata.ProgramVersion, "");
            WriteMetadataRow(writer, "Material", metadata.MaterialName, "");
            WriteMetadataRow(writer, "Recipe", metadata.RecipeName, "");
            WriteMetadataRow(writer, "OperationMode", metadata.OperationMode, "");
            WriteMetadataRow(
                writer,
                "TemperatureControlMode",
                metadata.TemperatureControlMode,
                "");

            WriteMetadataRow(writer, "TemperatureSetpoint", metadata.TemperatureSetpointC, "degC");
            WriteMetadataRow(writer, "PressureSetpoint", metadata.PressureSetpointKPa, "kPa");
            WriteMetadataRow(writer, "AutomaticFanSetpoint", metadata.AutomaticFanSetpointPercent, "%");
            WriteMetadataRow(writer, "RecipeTemperatureSetpoint", metadata.RecipeTemperatureSetpointC, "degC");
            WriteMetadataRow(writer, "RecipePressureSetpoint", metadata.RecipePressureSetpointKPa, "kPa");
            WriteMetadataRow(writer, "RecipeFanSpeed", metadata.RecipeFanSpeedPercent, "%");

            WriteMetadataRow(writer, "MaximumAllowedTemperature", metadata.MaximumAllowedTemperatureC, "degC");
            WriteMetadataRow(writer, "InitialWetMass", metadata.InitialWetMassKg, "kg");
            WriteMetadataRow(writer, "DryMass", metadata.DryMassKg, "kg");
            WriteMetadataRow(writer, "InitialMoistureWetBasis", metadata.InitialMoistureWetBasisPercent, "% wb");
            WriteMetadataRow(writer, "TargetMoistureWetBasis", metadata.TargetMoistureWetBasisPercent, "% wb");
            WriteMetadataRow(writer, "DryingCoefficient", metadata.DryingCoefficient, "-");

            WriteMetadataRow(writer, "TemperaturePID_Kp", metadata.TemperaturePidKp, "%/°C");
            WriteMetadataRow(writer, "TemperaturePID_Ki", metadata.TemperaturePidKi, "%/(°C·s)");
            WriteMetadataRow(writer, "TemperaturePID_Kd", metadata.TemperaturePidKd, "%·s/°C");
            WriteMetadataRow(
                writer,
                "TemperaturePID_DerivativeFilterTimeConstant",
                metadata.TemperaturePidDerivativeFilterSeconds,
                "s");
            WriteMetadataRow(writer, "PressurePI_Kp", metadata.PressurePiKp, "%/kPa");
            WriteMetadataRow(writer, "PressurePI_Ki", metadata.PressurePiKi, "%/(kPa·s)");

            WriteMetadataRow(writer, "ModelStep", metadata.ModelStepSeconds, "s");
            WriteMetadataRow(writer, "ControllerStep", metadata.ControllerStepSeconds, "s");
            WriteMetadataRow(writer, "TrendSampleInterval", metadata.TrendSampleIntervalSeconds, "s");
            WriteMetadataRow(
                writer,
                "SimulationSpeedAtStart",
                metadata.SimulationSpeedAtStart.ToString(CultureInfo.InvariantCulture),
                "x");

            WriteMetadataRow(writer, "AmbientTemperature", metadata.AmbientTemperatureC, "degC");
            WriteMetadataRow(writer, "AmbientPressure", metadata.AmbientPressureKPa, "kPa");
            WriteMetadataRow(
                writer,
                "AmbientRelativeHumidity",
                metadata.AmbientRelativeHumidityPercent,
                "%");

            WriteMetadataRow(
                writer,
                "DisturbanceCount",
                metadata.Disturbances.Count.ToString(CultureInfo.InvariantCulture),
                "");

            if (metadata.Disturbances.Count == 0)
            {
                WriteMetadataRow(writer, "DisturbanceType", "None", "");
                WriteMetadataRow(writer, "DisturbanceTime", "NotApplicable", "s");
                return;
            }

            for (int index = 0; index < metadata.Disturbances.Count; index++)
            {
                ExperimentDisturbance disturbance = metadata.Disturbances[index];
                string prefix = $"Disturbance_{index + 1}";
                WriteMetadataRow(writer, $"{prefix}_Type", disturbance.Type, "");
                WriteMetadataRow(
                    writer,
                    $"{prefix}_Time",
                    disturbance.ElapsedTimeSeconds,
                    "s");
            }
        }

        private static void WriteMetadataRow(
            StreamWriter writer,
            string parameter,
            double value,
            string unit) =>
            WriteMetadataRow(writer, parameter, Number(value), unit);

        private static void WriteMetadataRow(
            StreamWriter writer,
            string parameter,
            string value,
            string unit)
        {
            writer.WriteLine(string.Join(
                Separator,
                Escape(parameter),
                Escape(value),
                Escape(unit)));
        }

        private static string Number(double value) =>
            value.ToString("0.##########", CultureInfo.InvariantCulture);

        private static string Boolean(bool value) => value ? "true" : "false";

        private static string Escape(string value)
        {
            if (!value.Contains(Separator) &&
                !value.Contains('"') &&
                !value.Contains('\r') &&
                !value.Contains('\n'))
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
