using System;

namespace DiplomWork_Ivan_2026.Devices
{
    public class VentValve
    {
        public double Opening { get; private set; }

        public bool IsOpen => Opening > 0.0;

        public void Open()
        {
            Opening = 100.0;
        }

        public void Close()
        {
            Opening = 0.0;
        }

        public void SetOpening(double opening)
        {
            Opening = Math.Clamp(opening, 0.0, 100.0);
        }
    }
}
