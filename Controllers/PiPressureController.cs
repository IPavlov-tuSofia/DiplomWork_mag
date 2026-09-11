using System;
using DiplomWork_Ivan_2026.Devices;
using DiplomWork_Ivan_2026.Models;

namespace DiplomWork_Ivan_2026.Controllers
{
    public class PiPressureController
    {
        public const double DefaultKp = 4.0;
        public const double DefaultKi = 0.15;

        // Controller output is pump power [%]. A positive error means that the
        // pressure is above the setpoint and more vacuum-pump power is needed.
        public double Kp { get; set; } = DefaultKp;
        public double Ki { get; set; } = DefaultKi;

        public double MinOutput { get; set; } = 0.0;
        public double MaxOutput { get; set; } = 100.0;

        private double _integral;

        public double Update(
            double setpoint,
            double currentValue,
            double deltaTime)
        {
            if (deltaTime <= 0.0)
                deltaTime = 1.0;

            double error = currentValue - setpoint;
            double candidateIntegral = _integral + error * deltaTime;
            double output = Kp * error + Ki * candidateIntegral;
            double clampedOutput = Math.Clamp(output, MinOutput, MaxOutput);

            // Conditional integration prevents windup while the pump is
            // saturated, but permits the integral to move out of saturation.
            bool outputIsNotSaturated =
                Math.Abs(output - clampedOutput) < 0.0001;
            bool outputIsAtMaximumAndErrorIsNegative =
                clampedOutput >= MaxOutput && error < 0.0;
            bool outputIsAtMinimumAndErrorIsPositive =
                clampedOutput <= MinOutput && error > 0.0;

            if (outputIsNotSaturated ||
                outputIsAtMaximumAndErrorIsNegative ||
                outputIsAtMinimumAndErrorIsPositive)
            {
                _integral = candidateIntegral;
            }

            return Math.Clamp(
                Kp * error + Ki * _integral,
                MinOutput,
                MaxOutput);
        }

        public void Update(
            VacuumDryerState state,
            ProcessSettings settings,
            VacuumPump pump,
            double deltaTime)
        {
            double pumpPower = Update(
                settings.PressureSetpoint,
                state.Pressure,
                deltaTime);

            pump.SetPower(pumpPower);
            state.VacuumPumpPower = pump.Power;
        }

        public void Reset()
        {
            _integral = 0.0;
        }
    }
}
