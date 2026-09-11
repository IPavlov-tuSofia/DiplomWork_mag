using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiplomWork_Ivan_2026.Devices;
using DiplomWork_Ivan_2026.Models;

namespace DiplomWork_Ivan_2026.Controllers
{
    public class OnOffTemperatureController
    {
        public void Update(VacuumDryerState state, ProcessSettings settings, Heater heater)
        {
            if (state.MeasuredTemperature < settings.TemperatureSetpoint - settings.TemperatureHysteresis)
            {
                heater.TurnOn();
            }
            else if (state.MeasuredTemperature > settings.TemperatureSetpoint + settings.TemperatureHysteresis)
            {
                heater.TurnOff();
            }

            state.HeaterPower = heater.Power;
        }
    }
}
