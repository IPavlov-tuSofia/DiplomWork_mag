using System.Collections.Generic;

namespace DiplomWork_Ivan_2026.Models
{
    public sealed class ExperimentMetadata
    {
        private readonly List<ExperimentDisturbance> _disturbances = new();

        public string ExperimentId { get; init; } = "";
        public string RunLabel { get; init; } = "";
        public DateTimeOffset StartedAt { get; init; }
        public string ProgramVersion { get; init; } = "";

        public string MaterialName { get; init; } = "";
        public string RecipeName { get; init; } = "";
        public string OperationMode { get; init; } = "";
        public string TemperatureControlMode { get; init; } = "";

        public double TemperatureSetpointC { get; init; }
        public double PressureSetpointKPa { get; init; }
        public double AutomaticFanSetpointPercent { get; init; }
        public double RecipeTemperatureSetpointC { get; init; }
        public double RecipePressureSetpointKPa { get; init; }
        public double RecipeFanSpeedPercent { get; init; }

        public double MaximumAllowedTemperatureC { get; init; }
        public double InitialWetMassKg { get; init; }
        public double DryMassKg { get; init; }
        public double InitialMoistureWetBasisPercent { get; init; }
        public double TargetMoistureWetBasisPercent { get; init; }
        public double DryingCoefficient { get; init; }

        public double TemperaturePidKp { get; init; }
        public double TemperaturePidKi { get; init; }
        public double TemperaturePidKd { get; init; }
        public double TemperaturePidDerivativeFilterSeconds { get; init; }
        public double PressurePiKp { get; init; }
        public double PressurePiKi { get; init; }

        public double ModelStepSeconds { get; init; }
        public double ControllerStepSeconds { get; init; }
        public double TrendSampleIntervalSeconds { get; init; }
        public int SimulationSpeedAtStart { get; init; }

        public double AmbientTemperatureC { get; init; }
        public double AmbientPressureKPa { get; init; }
        public double AmbientRelativeHumidityPercent { get; init; }

        public IReadOnlyList<ExperimentDisturbance> Disturbances => _disturbances;

        public void AddDisturbance(string type, double elapsedTimeSeconds)
        {
            _disturbances.Add(new ExperimentDisturbance
            {
                Type = type,
                ElapsedTimeSeconds = elapsedTimeSeconds
            });
        }

        public ExperimentMetadata CreateSnapshot()
        {
            ExperimentMetadata snapshot = new ExperimentMetadata
            {
                ExperimentId = ExperimentId,
                RunLabel = RunLabel,
                StartedAt = StartedAt,
                ProgramVersion = ProgramVersion,
                MaterialName = MaterialName,
                RecipeName = RecipeName,
                OperationMode = OperationMode,
                TemperatureControlMode = TemperatureControlMode,
                TemperatureSetpointC = TemperatureSetpointC,
                PressureSetpointKPa = PressureSetpointKPa,
                AutomaticFanSetpointPercent = AutomaticFanSetpointPercent,
                RecipeTemperatureSetpointC = RecipeTemperatureSetpointC,
                RecipePressureSetpointKPa = RecipePressureSetpointKPa,
                RecipeFanSpeedPercent = RecipeFanSpeedPercent,
                MaximumAllowedTemperatureC = MaximumAllowedTemperatureC,
                InitialWetMassKg = InitialWetMassKg,
                DryMassKg = DryMassKg,
                InitialMoistureWetBasisPercent = InitialMoistureWetBasisPercent,
                TargetMoistureWetBasisPercent = TargetMoistureWetBasisPercent,
                DryingCoefficient = DryingCoefficient,
                TemperaturePidKp = TemperaturePidKp,
                TemperaturePidKi = TemperaturePidKi,
                TemperaturePidKd = TemperaturePidKd,
                TemperaturePidDerivativeFilterSeconds = TemperaturePidDerivativeFilterSeconds,
                PressurePiKp = PressurePiKp,
                PressurePiKi = PressurePiKi,
                ModelStepSeconds = ModelStepSeconds,
                ControllerStepSeconds = ControllerStepSeconds,
                TrendSampleIntervalSeconds = TrendSampleIntervalSeconds,
                SimulationSpeedAtStart = SimulationSpeedAtStart,
                AmbientTemperatureC = AmbientTemperatureC,
                AmbientPressureKPa = AmbientPressureKPa,
                AmbientRelativeHumidityPercent = AmbientRelativeHumidityPercent
            };

            foreach (ExperimentDisturbance disturbance in _disturbances)
            {
                snapshot._disturbances.Add(new ExperimentDisturbance
                {
                    Type = disturbance.Type,
                    ElapsedTimeSeconds = disturbance.ElapsedTimeSeconds
                });
            }

            return snapshot;
        }
    }

    public sealed class ExperimentDisturbance
    {
        public string Type { get; init; } = "";
        public double ElapsedTimeSeconds { get; init; }
    }
}
