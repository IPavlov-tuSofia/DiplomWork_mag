using System;
using DiplomWork_Ivan_2026.Devices;
using DiplomWork_Ivan_2026.Models;
using DiplomWork_Ivan_2026.Sensors;

namespace DiplomWork_Ivan_2026.Simulation
{
    public class VacuumDryerProcess
    {
        private const double UniversalGasConstantJPerMolK = 8.314462618;
        private const double WaterVaporGasConstantJPerKgK = 461.5;

        private const double MinimumSimulationTemperatureC = -50.0;
        private const double MaximumSimulationTemperatureC = 250.0;

        public VacuumDryerState State { get; private set; } = new VacuumDryerState();
        public Heater Heater { get; } = new Heater();
        public VacuumPump Pump { get; } = new VacuumPump();
        public VentValve VentValve { get; } = new VentValve();
        public Fan Fan { get; } = new Fan();
        public VacuumDryerModelParameters Parameters { get; }
        public VirtualAnalogSensor ChamberTemperatureSensor { get; } = new VirtualAnalogSensor(3.0, 0.05, 0.1, -50.0, 250.0, 101);
        public VirtualAnalogSensor MaterialTemperatureSensor { get; } = new VirtualAnalogSensor(5.0, 0.03, 0.1, -50.0, 250.0, 202);
        public VirtualAnalogSensor PressureSensor { get; } = new VirtualAnalogSensor(1.0, 0.03, 0.1, 0.0, 120.0, 303);
        public DryingMaterial? SelectedMaterial { get; private set; }
        public double LeakMultiplier { get; private set; } = 1.0;

        public VacuumDryerProcess(VacuumDryerModelParameters? parameters = null)
        {
            Parameters = parameters ?? new VacuumDryerModelParameters();
        }

        public bool HasSensorFault =>
            ChamberTemperatureSensor.FaultMode != Enums.SensorFaultMode.None ||
            MaterialTemperatureSensor.FaultMode != Enums.SensorFaultMode.None ||
            PressureSensor.FaultMode != Enums.SensorFaultMode.None;

        public void SetLeakMultiplier(double value)
        {
            LeakMultiplier = Math.Clamp(value, 1.0, 20.0);
        }

        public void LoadMaterial(DryingMaterial material)
        {
            SelectedMaterial = material;

            double initialVaporPressure = SaturationVaporPressureKPa(20.0) *
                    Math.Clamp(
                    Parameters.ReferenceRelativeHumidityPercent / 100.0,
                    0.0,
                    1.0);
            double initialVaporMass = VaporMassFromPartialPressure( initialVaporPressure,20.0);

            State = new VacuumDryerState
            {
                Temperature = 20.0,
                MaterialTemperature = 20.0,
                Pressure = 101.3,
                MeasuredTemperature = 20.0,
                MeasuredMaterialTemperature = 20.0,
                MeasuredPressure = 101.3,
                AirHumidity = Parameters.ReferenceRelativeHumidityPercent,
                WaterVaporPartialPressureKPa = initialVaporPressure,
                WaterVaporMassKg = initialVaporMass,
                InitialWaterVaporMassKg = initialVaporMass,
                DynamicEquilibriumMoistureDryBasis =
                    material.EquilibriumMoistureDryBasis,
                MaterialMoistureDryBasis = material.InitialMoistureDryBasis,
                ElapsedTime = 0.0,
                TotalEnergyKWh = 0.0,
                EvaporatedWaterKg = 0.0,
                EfficiencyKgPerKWh = 0.0,
                MoistureRatio = 1.0,
                EstimatedRemainingTimeSeconds = null,
                ProcessStage = Enums.ProcessStage.Idle,
                ActiveTemperatureSetpoint = 20.0,
                ActivePressureSetpoint = 101.3,
                MoistureTargetReached = false,
                IsCompleted = false
            };

            Heater.TurnOff();
            Pump.TurnOff();
            VentValve.Close();
            Fan.TurnOff();
            ChamberTemperatureSensor.Initialize(State.Temperature);
            MaterialTemperatureSensor.Initialize(State.MaterialTemperature);
            PressureSensor.Initialize(State.Pressure);
        }

        public void Update(double deltaTime, ProcessSettings settings)
        {
            if (SelectedMaterial == null || State.IsCompleted || deltaTime <= 0.0)
                return;

            State.ElapsedTime += deltaTime;

            UpdatePhysicalModel(deltaTime, settings, SelectedMaterial);
            UpdateVirtualSensors(deltaTime);

            State.HeaterPower = Heater.Power;
            State.VacuumPumpPower = Pump.Power;
            State.VentValveOpening = VentValve.Opening;
            State.FanSpeed = Fan.Speed;

            UpdateCalculatedValues(settings);
            UpdateEnergyConsumption(deltaTime);

            if (State.MaterialMoistureDryBasis <= SelectedMaterial.TargetMoistureDryBasis)
            {
                State.MaterialMoistureDryBasis = SelectedMaterial.TargetMoistureDryBasis;
                State.DryingRateDryBasisPerSecond = 0.0;
                State.EvaporationRateKgPerSecond = 0.0;
                State.MoistureTargetReached = true;
            }

            UpdateEvaporatedWaterAndEfficiency(SelectedMaterial);
        }

        private void UpdatePhysicalModel(
            double deltaTime,
            ProcessSettings settings,
            DryingMaterial material)
        {
            // All derivatives are evaluated from the same state and then applied
            // together with one explicit euler step.
            double chamberTemperature = State.Temperature;
            double materialTemperature = State.MaterialTemperature;
            double pressure = State.Pressure;
            double moistureDryBasis = State.MaterialMoistureDryBasis;

            double heaterInput = Heater.Power / 100.0;
            double pumpInput = Pump.Power / 100.0;
            double ventValveInput = VentValve.Opening / 100.0;
            double fanInput = Fan.Speed / 100.0;

            double dryingRate = CalculateDryingRateDryBasisPerSecond(
                material,
                materialTemperature,
                pressure,
                settings.AmbientPressure,
                moistureDryBasis,
                State.WaterVaporPartialPressureKPa,
                fanInput,
                out double dynamicEquilibriumMoistureDryBasis);

            State.DynamicEquilibriumMoistureDryBasis =
                dynamicEquilibriumMoistureDryBasis;

            double removableMoisture = Math.Max(
                0.0,
                moistureDryBasis - material.TargetMoistureDryBasis);
            dryingRate = Math.Min(dryingRate, removableMoisture / deltaTime);

            if (State.MoistureTargetReached)
                dryingRate = 0.0;

            double evaporationRateKgPerSecond = material.DryMassKg * dryingRate;

            double heaterPowerW =
                Parameters.HeaterEfficiency *
                Parameters.HeaterNominalPowerKw * 1_000.0 * heaterInput;
            double effectiveHeatTransferWPerK =
                Parameters.ChamberToMaterialHeatTransferWPerK *
                (1.0 + Parameters.FanHeatTransferGain * fanInput);
            double chamberToMaterialHeatW =
                effectiveHeatTransferWPerK *
                (chamberTemperature - materialTemperature);
            double ambientHeatLossW =
                Parameters.ChamberAmbientHeatLossWPerK *
                (chamberTemperature - settings.AmbientTemperature);

            double chamberTemperatureDerivative =
                (heaterPowerW - chamberToMaterialHeatW - ambientHeatLossW) /
                Parameters.ChamberHeatCapacityJPerK;

            double waterMassKg = material.DryMassKg * moistureDryBasis;
            double materialHeatCapacityJPerK =
                material.DryMassKg * Parameters.DryMaterialSpecificHeatJPerKgK +
                waterMassKg * Parameters.WaterSpecificHeatJPerKgK;
            double evaporationHeatW =
                Parameters.WaterLatentHeatJPerKg * evaporationRateKgPerSecond;
            double materialTemperatureDerivative =
                (chamberToMaterialHeatW - evaporationHeatW) /
                Math.Max(1.0, materialHeatCapacityJPerK);

            double pumpPressureDerivative =
                -pumpInput / Parameters.VacuumTimeConstantSeconds *
                (pressure - Parameters.MinimumPressureKPa);
            double effectiveLeakageCoefficient =
                Parameters.LeakageCoefficientPerSecond * LeakMultiplier;
            double leakagePressureDerivative =
                effectiveLeakageCoefficient *
                (settings.AmbientPressure - pressure);
            double ventValvePressureDerivative =
                ventValveInput / Parameters.VentValveTimeConstantSeconds *
                (settings.AmbientPressure - pressure);

            double materialTemperatureK = Math.Max(1.0, materialTemperature + 273.15);
            double evaporationPressureDerivative =
                WaterVaporGasConstantJPerKgK * materialTemperatureK /
                Parameters.ChamberVolumeM3 *
                evaporationRateKgPerSecond / 1_000.0;
            double pressureDerivative =
                pumpPressureDerivative +
                evaporationPressureDerivative +
                leakagePressureDerivative +
                ventValvePressureDerivative;

            State.Temperature = Math.Clamp(
                chamberTemperature + chamberTemperatureDerivative * deltaTime,
                MinimumSimulationTemperatureC,
                MaximumSimulationTemperatureC);
            State.MaterialTemperature = Math.Clamp(
                materialTemperature + materialTemperatureDerivative * deltaTime,
                MinimumSimulationTemperatureC,
                MaximumSimulationTemperatureC);
            State.Pressure = Math.Clamp(
                pressure + pressureDerivative * deltaTime,
                Parameters.MinimumPressureKPa,
                settings.AmbientPressure);
            State.MaterialMoistureDryBasis = Math.Max(
                0.0,
                moistureDryBasis - dryingRate * deltaTime);

            State.DryingRateDryBasisPerSecond = dryingRate;
            State.EvaporationRateKgPerSecond = evaporationRateKgPerSecond;

            UpdateWaterVaporBalance(
                deltaTime,
                settings,
                pumpInput,
                ventValveInput,
                evaporationRateKgPerSecond,
                State.Temperature,
                State.Pressure);
        }

        private double CalculateDryingRateDryBasisPerSecond(
            DryingMaterial material,
            double materialTemperatureC,
            double pressureKPa,
            double ambientPressureKPa,
            double moistureDryBasis,
            double waterVaporPartialPressureKPa,
            double fanInput,
            out double dynamicEquilibriumMoistureDryBasis)
        {
            double materialTemperatureK = Math.Max(1.0, materialTemperatureC + 273.15);
            double temperatureCoefficient =
                Parameters.DryingPreExponentialFactorPerSecond *
                Math.Exp(
                    -Parameters.DryingActivationEnergyJPerMol /
                    (UniversalGasConstantJPerMolK * materialTemperatureK));

            double pressureInfluence = CalculatePressureInfluence(
                pressureKPa,
                ambientPressureKPa);
            double effectiveDryingCoefficient =
                material.DryingCoefficient *
                temperatureCoefficient *
                pressureInfluence *
                (1.0 + Parameters.FanMassTransferGain * fanInput);

            double chamberRelativeHumidity = CalculateRelativeHumidityPercent(
                waterVaporPartialPressureKPa,
                materialTemperatureC);
            dynamicEquilibriumMoistureDryBasis =
                CalculateDynamicEquilibriumMoistureDryBasis(
                    material,
                    chamberRelativeHumidity,
                    materialTemperatureC);

            double moistureDrivingForce = Math.Max(
                0.0,
                moistureDryBasis - dynamicEquilibriumMoistureDryBasis);

            //  The vapor pressure deficitt closes the mass-transfer model:
            // evaporation tends to zero as chamber vapor approaches the
            // saturation pressure at the material surface temperature.
            double surfaceSaturationPressureKPa =
                SaturationVaporPressureKPa(materialTemperatureC);
            double vaporPressureDrivingForce = Math.Clamp(
                (surfaceSaturationPressureKPa - waterVaporPartialPressureKPa) /
                    Math.Max(0.001, surfaceSaturationPressureKPa),
                0.0,
                1.0);

            return effectiveDryingCoefficient * moistureDrivingForce * vaporPressureDrivingForce;
        }

        private double CalculatePressureInfluence(
            double pressureKPa,
            double ambientPressureKPa)
        {
            // Fp(p) = 0.5 + (pa - p) / (pa - pmin)
            // Fp(pa) = 0.5 and Fp(pmin) = 1.5.
            double pressureRange = Math.Max(
                1.0,
                ambientPressureKPa - Parameters.MinimumPressureKPa);
            double normalizedVacuum = Math.Clamp(
                (ambientPressureKPa - pressureKPa) / pressureRange,
                0.0,
                1.0);

            return Parameters.MinimumPressureInfluence + normalizedVacuum;
        }

        private void UpdateWaterVaporBalance(
            double deltaTime,
            ProcessSettings settings,
            double pumpInput,
            double ventValveInput,
            double evaporationRateKgPerSecond,
            double chamberTemperatureC,
            double totalPressureKPa)
        {
            double currentVaporMassKg = Math.Max(0.0, State.WaterVaporMassKg);

            // A well-mixed chamber is assumed, so the pump removes the same
            // vapour fraction as is present in the chamber gas.
            double pumpRemovalRateKgPerSecond = pumpInput / Parameters.VacuumTimeConstantSeconds * currentVaporMassKg;

            // Leakage and an open vent admit ambient gas while chamber pressure
            // is below ambient. The incoming vapour follows ambient absolute
            // humidity; the circulation fan is deliberately absent here.
            double ambientVaporPressureKPa =
                SaturationVaporPressureKPa(settings.AmbientTemperature) *
                Math.Clamp(
                    settings.AmbientRelativeHumidityPercent / 100.0,
                    0.0,
                    1.0);
            double ambientVaporMassInChamberVolumeKg =
                VaporMassFromPartialPressure(
                    ambientVaporPressureKPa,
                    settings.AmbientTemperature);
            double pressureDeficitFraction = Math.Clamp(
                (settings.AmbientPressure - totalPressureKPa) /
                    Math.Max(1.0, settings.AmbientPressure),
                0.0,
                1.0);
            double gasIngressCoefficientPerSecond =
                Parameters.LeakageCoefficientPerSecond * LeakMultiplier +
                ventValveInput / Parameters.VentValveTimeConstantSeconds;
            double ambientVaporIngressRateKgPerSecond =
                gasIngressCoefficientPerSecond *
                pressureDeficitFraction *
                ambientVaporMassInChamberVolumeKg;

            double nextVaporMassKg = Math.Max(
                0.0,
                currentVaporMassKg + deltaTime *
                    (evaporationRateKgPerSecond +
                     ambientVaporIngressRateKgPerSecond -
                     pumpRemovalRateKgPerSecond));

            double maximumVaporPartialPressureKPa = Math.Min(
                SaturationVaporPressureKPa(chamberTemperatureC),
                Math.Max(0.0, totalPressureKPa * 0.98));
            double maximumVaporMassKg = VaporMassFromPartialPressure(
                maximumVaporPartialPressureKPa,
                chamberTemperatureC);

            if (nextVaporMassKg > maximumVaporMassKg)
            {
                State.CondensedWaterKg += nextVaporMassKg - maximumVaporMassKg;
                nextVaporMassKg = maximumVaporMassKg;
            }

            State.PumpedWaterVaporKg +=
                Math.Min(
                    currentVaporMassKg,
                    pumpRemovalRateKgPerSecond * deltaTime);
            State.AmbientWaterVaporIngressKg +=
                ambientVaporIngressRateKgPerSecond * deltaTime;
            State.WaterVaporMassKg = nextVaporMassKg;
            State.WaterVaporPartialPressureKPa =
                VaporPartialPressureFromMass(
                    nextVaporMassKg,
                    chamberTemperatureC);
            State.AirHumidity = CalculateRelativeHumidityPercent(
                State.WaterVaporPartialPressureKPa,
                chamberTemperatureC);
        }

        private double CalculateDynamicEquilibriumMoistureDryBasis(
            DryingMaterial material,
            double relativeHumidityPercent,
            double materialTemperatureC)
        {
            double referenceHumidityFraction = Math.Max(
                0.01,
                Parameters.ReferenceRelativeHumidityPercent / 100.0);
            double humidityFraction = Math.Clamp(
                relativeHumidityPercent / 100.0,
                0.001,
                0.999);
            double humidityCorrection = Math.Pow(
                humidityFraction / referenceHumidityFraction,
                Parameters.EquilibriumMoistureHumidityExponent);
            double temperatureCorrection = Math.Exp(
                -Parameters.EquilibriumMoistureTemperatureCoefficientPerC *
                (materialTemperatureC - Parameters.ReferenceTemperatureC));
            double minimumEquilibriumMoisture =
                Parameters.MinimumEquilibriumMoistureFraction *
                material.EquilibriumMoistureDryBasis;

            return Math.Clamp(
                material.EquilibriumMoistureDryBasis *
                    humidityCorrection * temperatureCorrection,
                minimumEquilibriumMoisture,
                Math.Max(
                    minimumEquilibriumMoisture,
                    material.InitialMoistureDryBasis));
        }

        private static double SaturationVaporPressureKPa(double temperatureC)
        {
            // Buck equation over liquid water; sufficiently accurate for the
            // temperature range used by the dryer simulation.
            double boundedTemperatureC = Math.Clamp(temperatureC, -40.0, 100.0);
            return 0.61121 * Math.Exp(
                (18.678 - boundedTemperatureC / 234.5) *
                (boundedTemperatureC / (257.14 + boundedTemperatureC)));
        }

        private static double CalculateRelativeHumidityPercent(
            double vaporPartialPressureKPa,
            double temperatureC)
        {
            return Math.Clamp(
                100.0 * vaporPartialPressureKPa /
                    Math.Max(0.001, SaturationVaporPressureKPa(temperatureC)),
                0.0,
                100.0);
        }

        private double VaporMassFromPartialPressure(
            double vaporPartialPressureKPa,
            double temperatureC)
        {
            double temperatureK = Math.Max(1.0, temperatureC + 273.15);
            return Math.Max(0.0, vaporPartialPressureKPa) * 1_000.0 *
                Parameters.ChamberVolumeM3 /
                (WaterVaporGasConstantJPerKgK * temperatureK);
        }

        private double VaporPartialPressureFromMass(
            double vaporMassKg,
            double temperatureC)
        {
            double temperatureK = Math.Max(1.0, temperatureC + 273.15);
            return Math.Max(0.0, vaporMassKg) *
                WaterVaporGasConstantJPerKgK * temperatureK /
                Parameters.ChamberVolumeM3 / 1_000.0;
        }

        private void UpdateCalculatedValues(ProcessSettings settings)
        {
            State.VacuumLevel =
                (settings.AmbientPressure - State.MeasuredPressure) /
                settings.AmbientPressure * 100.0;
            State.VacuumLevel = Math.Clamp(State.VacuumLevel, 0.0, 100.0);
            State.AirFlowRate = State.FanSpeed / 100.0 *
                Parameters.MaxAirFlowRateM3PerHour;

            UpdateMoistureRatioAndRemainingTime(settings);
        }

        private void UpdateVirtualSensors(double deltaTime)
        {
            State.MeasuredTemperature = ChamberTemperatureSensor.Update(
                State.Temperature,
                deltaTime);
            State.MeasuredMaterialTemperature = MaterialTemperatureSensor.Update(
                State.MaterialTemperature,
                deltaTime);
            State.MeasuredPressure = PressureSensor.Update(
                State.Pressure,
                deltaTime);
        }

        private void UpdateMoistureRatioAndRemainingTime(ProcessSettings settings)
        {
            if (SelectedMaterial == null)
                return;

            double moistureRatioDenominator = Math.Max(
                0.000001,
                SelectedMaterial.InitialMoistureDryBasis -
                    SelectedMaterial.EquilibriumMoistureDryBasis);
            State.MoistureRatio = Math.Clamp(
                (State.MaterialMoistureDryBasis -
                    SelectedMaterial.EquilibriumMoistureDryBasis) /
                moistureRatioDenominator,
                0.0,
                1.0);

            if (State.IsCompleted)
            {
                State.EstimatedRemainingTimeSeconds = 0.0;
                return;
            }

            if (State.ProcessStage == Enums.ProcessStage.Venting)
            {
                double pressureDifference = Math.Max(
                    1.0,
                    settings.AmbientPressure - State.Pressure);
                double valveInput = Math.Max(0.01, VentValve.Opening / 100.0);
                double recoveryCoefficient =
                    Parameters.LeakageCoefficientPerSecond * LeakMultiplier +
                    valveInput / Parameters.VentValveTimeConstantSeconds;
                State.EstimatedRemainingTimeSeconds = Math.Max(
                    0.0,
                    Math.Log(pressureDifference) / recoveryCoefficient);
                return;
            }

            double remainingMoisture = Math.Max(
                0.0,
                State.MaterialMoistureDryBasis -
                    SelectedMaterial.TargetMoistureDryBasis);
            if (State.DryingRateDryBasisPerSecond <= 0.000000001)
            {
                State.EstimatedRemainingTimeSeconds = null;
                return;
            }

            double rawEstimate = Math.Clamp(
                remainingMoisture / State.DryingRateDryBasisPerSecond,
                0.0,
                7.0 * 24.0 * 3600.0);
            State.EstimatedRemainingTimeSeconds =
                State.EstimatedRemainingTimeSeconds.HasValue
                    ? 0.95 * State.EstimatedRemainingTimeSeconds.Value +
                        0.05 * rawEstimate
                    : rawEstimate;
        }

        private void UpdateEnergyConsumption(double deltaTime)
        {
            double deltaTimeHours = deltaTime / 3600.0;

            double heaterEnergy =
                Parameters.HeaterNominalPowerKw *
                (Heater.Power / 100.0) * deltaTimeHours;
            double pumpEnergy =
                Parameters.PumpNominalPowerKw *
                (Pump.Power / 100.0) * deltaTimeHours;
            double fanEnergy =
                Parameters.FanNominalPowerKw *
                (Fan.Speed / 100.0) * deltaTimeHours;

            State.TotalEnergyKWh += heaterEnergy + pumpEnergy + fanEnergy;
        }

        private void UpdateEvaporatedWaterAndEfficiency(DryingMaterial material)
        {
            State.EvaporatedWaterKg = material.DryMassKg * Math.Max(
                0.0,
                material.InitialMoistureDryBasis - State.MaterialMoistureDryBasis);

            State.EfficiencyKgPerKWh = State.TotalEnergyKWh > 0.001
                ? State.EvaporatedWaterKg / State.TotalEnergyKWh
                : 0.0;
        }

        public void CompleteProcess()
        {
            State.IsCompleted = true;
            State.ProcessStage = Enums.ProcessStage.Completed;

            Heater.TurnOff();
            Pump.TurnOff();
            VentValve.Close();
            Fan.TurnOff();

            State.HeaterPower = 0.0;
            State.VacuumPumpPower = 0.0;
            State.VentValveOpening = 0.0;
            State.FanSpeed = 0.0;
            State.DryingRateDryBasisPerSecond = 0.0;
            State.EvaporationRateKgPerSecond = 0.0;
            State.EstimatedRemainingTimeSeconds = 0.0;
        }
    }
}
