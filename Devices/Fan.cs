using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomWork_Ivan_2026.Devices
{
    public class Fan
    {
        public double Speed { get; private set; } = 0.0;

        public bool IsOn => Speed > 0;

        public void TurnOn()
        {
            Speed = 70.0;
        }

        public void TurnOff()
        {
            Speed = 0.0;
        }

        public void SetSpeed(double speed)
        {
            Speed = Math.Clamp(speed, 0.0, 100.0);
        }
    }
}
