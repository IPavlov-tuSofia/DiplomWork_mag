using System;
using DiplomWork_Ivan_2026.Enums;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Simulation;

namespace DiplomWork_Ivan_2026.Controllers
{
    public sealed class AutomaticControlTargets
    {
        public bool TemperatureControlEnabled { get; init; }
        public bool PressureControlEnabled { get; init; }
        public double TemperatureSetpoint { get; init; }
        public double PressureSetpoint { get; init; }
        public double FanSpeed { get; init; }
        public double VentValveOpening { get; init; }
    }

    public class AutomaticProcessController
    {
        public double PressureRampDurationSeconds { get; set; } = 300.0;
        public double PreheatMaterialTemperatureMarginC { get; set; } = 5.0;
        public double PreheatChamberToleranceC { get; set; } = 1.0;
        public double MaximumPreheatTimeSeconds { get; set; } = 1_800.0;
        public double FinalDryingFraction { get; set; } = 0.20;
        public double VentingPressureToleranceKPa { get; set; } = 1.0;

        public AutomaticControlTargets Update(
            VacuumDryerProcess process,
            ProcessSettings settings,
            double requestedFanSpeed,
            double deltaTime)
        {
            VacuumDryerState state = process.State;
            DryingMaterial? material = process.SelectedMaterial;

            if (material == null || state.IsCompleted)
                return CreateStoppedTargets(state, settings);

            if (state.ProcessStage is ProcessStage.Idle or ProcessStage.Manual)
                TransitionTo(state, ProcessStage.Preheating);
            else
                state.StageElapsedTime += Math.Max(0.0, deltaTime);

            switch (state.ProcessStage)
            {
                case ProcessStage.Preheating:
                {
                    double materialPreheatTarget = Math.Min(
                        settings.TemperatureSetpoint -
                            PreheatMaterialTemperatureMarginC,
                        material.MaxTemperature - 1.0);
                    bool chamberIsHot =
                        state.MeasuredTemperature >= settings.TemperatureSetpoint -
                            PreheatChamberToleranceC;
                    bool materialIsWarm =
                        state.MeasuredMaterialTemperature >= materialPreheatTarget;
                    bool maximumPreheatTimeReached =
                        state.StageElapsedTime >= MaximumPreheatTimeSeconds;

                    if (chamberIsHot &&
                        (materialIsWarm || maximumPreheatTimeReached))
                    {
                        TransitionTo(state, ProcessStage.Evacuation);
                    }

                    break;
                }

                case ProcessStage.Evacuation:
                {
                    double rampFraction = Math.Clamp(
                        state.StageElapsedTime /
                            Math.Max(1.0, PressureRampDurationSeconds),
                        0.0,
                        1.0);
                    double rampSetpoint = settings.AmbientPressure +
                        (settings.PressureSetpoint - settings.AmbientPressure) *
                        rampFraction;

                    state.ActivePressureSetpoint = rampSetpoint;

                    bool rampIsComplete = rampFraction >= 1.0;
                    bool pressureIsEstablished =
                        Math.Abs(state.MeasuredPressure - settings.PressureSetpoint) <= 2.0;

                    if (rampIsComplete && pressureIsEstablished)
                        TransitionTo(state, ProcessStage.Drying);

                    break;
                }

                case ProcessStage.Drying:
                {
                    double moistureRange = Math.Max(
                        0.0,
                        material.InitialMoistureDryBasis -
                            material.TargetMoistureDryBasis);
                    double finalDryingThreshold =
                        material.TargetMoistureDryBasis +
                        FinalDryingFraction * moistureRange;

                    if (state.MaterialMoistureDryBasis <= finalDryingThreshold)
                        TransitionTo(state, ProcessStage.FinalDrying);

                    break;
                }

                case ProcessStage.FinalDrying:
                    if (state.MoistureTargetReached)
                        TransitionTo(state, ProcessStage.Venting);
                    break;

                case ProcessStage.Venting:
                    if (state.MeasuredPressure >=
                        settings.AmbientPressure - VentingPressureToleranceKPa)
                    {
                        process.CompleteProcess();
                    }
                    break;
            }

            return CreateTargetsForCurrentStage(
                state,
                settings,
                requestedFanSpeed);
        }

        public void Reset(VacuumDryerState state, ProcessSettings settings)
        {
            state.ProcessStage = ProcessStage.Idle;
            state.StageElapsedTime = 0.0;
            state.ActiveTemperatureSetpoint = settings.AmbientTemperature;
            state.ActivePressureSetpoint = settings.AmbientPressure;
        }

        private static AutomaticControlTargets CreateTargetsForCurrentStage(
            VacuumDryerState state,
            ProcessSettings settings,
            double requestedFanSpeed)
        {
            double temperatureSetpoint = settings.TemperatureSetpoint;
            double pressureSetpoint = settings.PressureSetpoint;

            AutomaticControlTargets targets = state.ProcessStage switch
            {
                ProcessStage.Preheating => new AutomaticControlTargets
                {
                    TemperatureControlEnabled = true,
                    TemperatureSetpoint = temperatureSetpoint,
                    PressureSetpoint = settings.AmbientPressure,
                    FanSpeed = requestedFanSpeed
                },
                ProcessStage.Evacuation => new AutomaticControlTargets
                {
                    TemperatureControlEnabled = true,
                    PressureControlEnabled = true,
                    TemperatureSetpoint = temperatureSetpoint,
                    PressureSetpoint = state.ActivePressureSetpoint,
                    FanSpeed = requestedFanSpeed
                },
                ProcessStage.Drying or ProcessStage.FinalDrying =>
                    new AutomaticControlTargets
                    {
                        TemperatureControlEnabled = true,
                        PressureControlEnabled = true,
                        TemperatureSetpoint = temperatureSetpoint,
                        PressureSetpoint = pressureSetpoint,
                        FanSpeed = requestedFanSpeed
                    },
                ProcessStage.Venting => new AutomaticControlTargets
                {
                    TemperatureSetpoint = temperatureSetpoint,
                    PressureSetpoint = settings.AmbientPressure,
                    VentValveOpening = CalculateVentingOpening(
                        state.MeasuredPressure,
                        settings.AmbientPressure)
                },
                _ => CreateStoppedTargets(state, settings)
            };

            state.ActiveTemperatureSetpoint = targets.TemperatureSetpoint;
            state.ActivePressureSetpoint = targets.PressureSetpoint;
            return targets;
        }

        private static double CalculateVentingOpening(
            double pressure,
            double ambientPressure)
        {
            double pressureDifference = Math.Max(0.0, ambientPressure - pressure);
            return Math.Clamp(2.0 * pressureDifference, 10.0, 40.0);
        }

        private static AutomaticControlTargets CreateStoppedTargets(
            VacuumDryerState state,
            ProcessSettings settings)
        {
            return new AutomaticControlTargets
            {
                TemperatureSetpoint = state.ActiveTemperatureSetpoint,
                PressureSetpoint = state.IsCompleted
                    ? settings.AmbientPressure
                    : state.ActivePressureSetpoint
            };
        }

        private static void TransitionTo(
            VacuumDryerState state,
            ProcessStage stage)
        {
            if (state.ProcessStage == stage)
                return;

            state.ProcessStage = stage;
            state.StageElapsedTime = 0.0;
        }
    }
}
