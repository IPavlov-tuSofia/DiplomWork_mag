# Vacuum dryer model parameters

The simulation is a reduced, lumped-parameter model intended for control-system
development. `VacuumDryerModelParameters` is the single calibration surface.

## Current basis

- Chamber volume: 0.50 m³ (pilot-scale engineering assumption).
- Heater/pump/fan ratings: 5.0/1.5/0.5 kW (installed-power assumptions).
- Heat capacities and transfer coefficients: initial engineering estimates.
- Water properties: 4.18 kJ/(kg·K) heat capacity and 2.30 MJ/kg latent heat.
- Saturation pressure: Buck equation for water over liquid water.
- Drying kinetics: first-order moisture driving force with an Arrhenius
  temperature factor; the common kinetic constants and material multipliers
  require identification against drying tests.
- Equilibrium moisture: each material value is the reference at 20 °C and 50%
  RH; temperature and RH corrections are explicit in the process model.
- Fan: increases effective heat and mass-transfer coefficients. It does not
  remove vapour from the sealed chamber.
- Vacuum pump: removes total gas and its water-vapour share. Leakage and the
  vent valve drive the chamber vapour inventory toward ambient conditions.

## Calibration procedure

1. Run an empty-chamber heating step to identify chamber heat capacity and heat
   loss.
2. Heat a known wet load without vacuum to identify chamber-to-material heat
   transfer.
3. Run a pump-down test without a wet load to identify the vacuum time constant
   and leakage coefficient.
4. Dry each material at two or more temperatures and pressures; fit the common
   Arrhenius constants and the material multiplier to moisture-versus-time data.
5. Validate with a separate run and report temperature/pressure tracking error,
   final-moisture error, drying time and energy consumption.

Until those tests are available, the UI/results must be described as simulated
engineering estimates rather than predictions for a specific industrial dryer.
