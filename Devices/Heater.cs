using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomWork_Ivan_2026.Devices
{
    public class Heater
    {
        public double Power { get; private set; } = 0.0;

        public bool IsOn => Power > 0;

        public void TurnOn()
        {
            Power = 100.0;
        }

        public void TurnOff()
        {
            Power = 0.0;
        }

        public void SetPower(double power)
        {
            Power = Math.Clamp(power, 0.0, 100.0);
        }
    }
}
