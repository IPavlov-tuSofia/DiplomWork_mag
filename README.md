Vacuum Dryer SCADA
==
	A desktop SCADA application for monitoring, control, and simulation of an industrial vacuum drying process.

	The project is developed in C#, WPF, and .NET 8 and includes a virtual process model, automatic control, alarms, trends, safety logic, and disturbance simulation.

Features
==
Real-time process monitoring
Automatic and manual operation modes
PID temperature control
PI pressure control
Virtual vacuum dryer process model
Heater, fan, vacuum pump, and vent valve control
Chamber and material temperature monitoring
Pressure and moisture monitoring
Drying rate calculation
Alarm management
Safety interlocks and emergency stop
Process trends and visualization
CSV data export
Sensor fault and disturbance simulation
Bulgarian and English interface

Architecture
==
Assets/         HMI graphics and UI resources
Controllers/    Automatic, PID, PI, and On/Off control logic
Devices/        Simulated actuators
Enums/          Process and application enumerations
Models/         Process state, materials, recipes, and configuration
Sensors/        Virtual sensor models
Services/       Alarms, safety, localization, and data export
Simulation/     Vacuum dryer process model
Trends/         Trend processing and visualization

Technology Stack
==
C#
.NET 8
WPF
XAML
LiveChartsCore / SkiaSharp
Git / GitHub

Getting Started
==
Requirements
==
Windows
.NET 8 SDK
Visual Studio 2022 or newer
.NET Desktop Development workload

How to Run
==
Clone the repository:
git clone https://github.com/IPavlov-tuSofia/DiplomWork_mag.git

Open:
DiplomWork_Ivan_2026.sln

restore dependencies, build the solution, and run the application from Visual Studio.

Process Control
==
The simulated vacuum drying process is controlled using:

1. a PID controller for temperature regulation;
2. a PI controller for pressure regulation;
3. automatic sequencing of process stages;
4. safety interlocks for abnormal operating conditions.

Experimental Tools
==
The application supports controlled disturbances and simulated sensor faults for evaluating system behavior and controller performance.

Process data can be exported to CSV for further analysis.

License
==
See the LICENSE file for license information.
