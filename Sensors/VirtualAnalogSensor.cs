using System;
using DiplomWork_Ivan_2026.Enums;

namespace DiplomWork_Ivan_2026.Sensors
{
    public class VirtualAnalogSensor
    {
        private readonly Random _random;
        private bool _isInitialized;
        private double _filteredValue;
        private double _frozenValue;

        public VirtualAnalogSensor(
            double timeConstantSeconds,
            double noiseStandardDeviation,
            double resolution,
            double minimum,
            double maximum,
            int randomSeed)
        {
            TimeConstantSeconds = Math.Max(0.0, timeConstantSeconds);
            NoiseStandardDeviation = Math.Max(0.0, noiseStandardDeviation);
            Resolution = Math.Max(0.0, resolution);
            Minimum = minimum;
            Maximum = maximum;
            _random = new Random(randomSeed);
        }

        public double TimeConstantSeconds { get; }
        public double NoiseStandardDeviation { get; }
        public double Resolution { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double Value { get; private set; }
        public SensorFaultMode FaultMode { get; private set; }

        public void Initialize(double actualValue)
        {
            _filteredValue = Math.Clamp(actualValue, Minimum, Maximum);
            Value = Quantize(_filteredValue);
            _frozenValue = Value;
            _isInitialized = true;
            FaultMode = SensorFaultMode.None;
        }

        public double Update(double actualValue, double deltaTime)
        {
            if (!_isInitialized)
                Initialize(actualValue);

            if (FaultMode == SensorFaultMode.Frozen)
            {
                Value = _frozenValue;
                return Value;
            }

            if (FaultMode == SensorFaultMode.FailedLow)
            {
                Value = Minimum;
                return Value;
            }

            if (FaultMode == SensorFaultMode.FailedHigh)
            {
                Value = Maximum;
                return Value;
            }

            double safeDeltaTime = Math.Max(0.0, deltaTime);
            double filterCoefficient = TimeConstantSeconds <= 0.0
                ? 1.0
                : 1.0 - Math.Exp(-safeDeltaTime / TimeConstantSeconds);

            _filteredValue += filterCoefficient *
                (Math.Clamp(actualValue, Minimum, Maximum) - _filteredValue);

            double noisyValue = _filteredValue +
                NoiseStandardDeviation * NextGaussian();
            Value = Quantize(Math.Clamp(noisyValue, Minimum, Maximum));
            return Value;
        }

        public void SetFaultMode(SensorFaultMode faultMode)
        {
            if (faultMode == SensorFaultMode.Frozen &&
                FaultMode != SensorFaultMode.Frozen)
            {
                _frozenValue = Value;
            }

            FaultMode = faultMode;
        }

        private double Quantize(double value)
        {
            if (Resolution <= 0.0)
                return value;

            return Math.Clamp(
                Math.Round(value / Resolution) * Resolution,
                Minimum,
                Maximum);
        }

        private double NextGaussian()
        {
            double first = 1.0 - _random.NextDouble();
            double second = 1.0 - _random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(first)) *
                Math.Cos(2.0 * Math.PI * second);
        }
    }
}
