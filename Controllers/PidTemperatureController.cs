using System;

namespace DiplomWork_Ivan_2026.Controllers
{
    public class PidTemperatureController
    {
        public const double DefaultKp = 8.5;
        public const double DefaultKi = 1.2;
        public const double DefaultKd = 0.7;

        public double Kp { get; set; } = DefaultKp;
        public double Ki { get; set; } = DefaultKi;
        public double Kd { get; set; } = DefaultKd;

        // First order lowpass filter for the derivative term.
        // The derivative is calculated from the measured temperature to avoid derivative kick
        // when the setpoint is changed
        public double DerivativeFilterTimeConstantSeconds { get; set; } = 5.0;

        public double MinOutput { get; set; } = 0.0;
        public double MaxOutput { get; set; } = 100.0;

        private double _integral;
        private double _previousMeasurement;
        private double _filteredDerivative;
        private bool _hasPreviousMeasurement;

        public double Update(double setpoint, double currentValue, double deltaTime)
        {
            if (deltaTime <= 0)
                deltaTime = 1.0;

            double error = setpoint - currentValue;

            if (_hasPreviousMeasurement)
            {
                double measurementDerivative = (currentValue - _previousMeasurement) / deltaTime;
                double filterTimeConstant = Math.Max(
                    0.0,
                    DerivativeFilterTimeConstantSeconds);
                double filterCoefficient = deltaTime / (filterTimeConstant + deltaTime);

                _filteredDerivative += filterCoefficient * (measurementDerivative - _filteredDerivative);
            }

            // D on measurement: a rising temperature must reduce heater output
            double derivativeTerm = -Kd * _filteredDerivative;

            double candidateIntegral = _integral + error * deltaTime;

            double output = Kp * error + Ki * candidateIntegral + derivativeTerm;

            double clampedOutput = Math.Clamp(output, MinOutput, MaxOutput);

            // Anti windup
            // Интегралната съставка се обновява само ако изходът не е в насищане, или ако грешката помага да се излезе от насищането
            bool outputIsNotSaturated = Math.Abs(output - clampedOutput) < 0.0001;
            bool outputIsAtMaximumAndErrorIsNegative = clampedOutput >= MaxOutput && error < 0;
            bool outputIsAtMinimumAndErrorIsPositive = clampedOutput <= MinOutput && error > 0;

            if (outputIsNotSaturated ||
                outputIsAtMaximumAndErrorIsNegative ||
                outputIsAtMinimumAndErrorIsPositive)
            {
                _integral = candidateIntegral;
            }

            output = Kp * error + Ki * _integral + derivativeTerm;

            _previousMeasurement = currentValue;
            _hasPreviousMeasurement = true;

            return Math.Clamp(output, MinOutput, MaxOutput);
        }

        public void Reset()
        {
            _integral = 0.0;
            _previousMeasurement = 0.0;
            _filteredDerivative = 0.0;
            _hasPreviousMeasurement = false;
        }
    }
}
