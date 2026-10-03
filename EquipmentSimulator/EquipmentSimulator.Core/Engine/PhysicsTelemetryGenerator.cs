using System;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Engine
{
    /// <summary>
    /// Pure physics computation module simulating high-vacuum pump-down curves,
    /// thermal stabilization loops, and optical laser exposure energy flux in semiconductor tools.
    /// </summary>
    public static class PhysicsTelemetryGenerator
    {
        private const double AtmosphericPressureTorr = 760.0;

        /// <summary>
        /// Calculates chamber vacuum pressure based on the active process step, execution progress, and target pressure.
        /// </summary>
        public static double CalculateVacuumPressure(
            ProcessStepType step,
            double progressFraction,
            double targetVacuumTorr,
            SimulationFaultType faultType = SimulationFaultType.None,
            Random random = null)
        {
            var rnd = random ?? new Random();
            var jitter = (rnd.NextDouble() - 0.5) * 0.03; // ±1.5% sensor noise

            if (faultType == SimulationFaultType.VacuumLeak)
            {
                // Fault: Sudden chamber breach or turbo failure causes pressure spike
                return 2.5e-2 * (1.0 + (rnd.NextDouble() - 0.5) * 0.1);
            }

            progressFraction = Math.Max(0.0, Math.Min(1.0, progressFraction));

            switch (step)
            {
                case ProcessStepType.WaferLoad:
                case ProcessStepType.WaferUnload:
                    return AtmosphericPressureTorr + (rnd.NextDouble() - 0.5) * 0.4;

                case ProcessStepType.PumpDown:
                {
                    // Logarithmic evacuation curve from 760 Torr to high-vacuum base pressure
                    var logStart = Math.Log10(AtmosphericPressureTorr);
                    var logTarget = Math.Log10(Math.Max(1.0e-9, targetVacuumTorr));
                    // Smooth S-curve transition
                    var smoothProgress = Math.Pow(progressFraction, 1.2);
                    var currentLog = logStart + (logTarget - logStart) * smoothProgress;
                    var basePressure = Math.Pow(10, currentLog);
                    return Math.Max(targetVacuumTorr, basePressure * (1.0 + jitter));
                }

                case ProcessStepType.Exposure:
                    // Stabilized high-vacuum with molecular flow jitter
                    return targetVacuumTorr * (1.0 + jitter * 0.8);

                case ProcessStepType.ChamberVent:
                {
                    // Controlled N2 purge repressurization curve back to 760 Torr
                    var logStart = Math.Log10(Math.Max(1.0e-9, targetVacuumTorr));
                    var logTarget = Math.Log10(AtmosphericPressureTorr);
                    var smoothProgress = Math.Pow(progressFraction, 0.8);
                    var currentLog = logStart + (logTarget - logStart) * smoothProgress;
                    var basePressure = Math.Pow(10, currentLog);
                    return Math.Min(AtmosphericPressureTorr, basePressure * (1.0 + jitter));
                }

                default:
                    return AtmosphericPressureTorr;
            }
        }

        /// <summary>
        /// Calculates chamber chuck temperature with PID thermal jitter and laser exposure heat absorption.
        /// </summary>
        public static double CalculateTemperature(
            ProcessStepType step,
            double progressFraction,
            double setpointCelsius,
            SimulationFaultType faultType = SimulationFaultType.None,
            Random random = null)
        {
            var rnd = random ?? new Random();
            var thermalJitter = (rnd.NextDouble() - 0.5) * 0.02; // ±0.01°C thermal jitter

            if (faultType == SimulationFaultType.ThermalExcursion)
            {
                // Fault: Chiller loop failure / thermal runaway
                return setpointCelsius + 1.85 + (rnd.NextDouble() * 0.2);
            }

            progressFraction = Math.Max(0.0, Math.Min(1.0, progressFraction));

            var currentTemp = setpointCelsius + thermalJitter;

            if (step == ProcessStepType.Exposure)
            {
                // Pulsed laser exposure transfers thermal energy to wafer chuck (e.g. +0.12°C peak)
                var laserHeatLoad = 0.12 * Math.Sin(Math.PI * progressFraction);
                currentTemp += laserHeatLoad;
            }

            return Math.Round(currentTemp, 3);
        }

        /// <summary>
        /// Calculates laser optical energy flux / RF dose delivery.
        /// </summary>
        public static double CalculateLaserFlux(
            ProcessStepType step,
            double progressFraction,
            double targetDoseMilliJoules,
            SimulationFaultType faultType = SimulationFaultType.None,
            Random random = null)
        {
            if (step != ProcessStepType.Exposure)
                return 0.0;

            var rnd = random ?? new Random();
            var fluxNoise = (rnd.NextDouble() - 0.5) * 0.6;

            if (faultType == SimulationFaultType.LaserUniformityDegradation)
            {
                // Fault: Optics degradation drops dose and introduces severe variance
                return Math.Max(0.0, (targetDoseMilliJoules * 0.55) + (rnd.NextDouble() - 0.5) * 4.0);
            }

            return Math.Max(0.0, targetDoseMilliJoules + fluxNoise);
        }

        /// <summary>
        /// Determines human-readable vacuum regime / phase description.
        /// </summary>
        public static string DetermineVacuumPhase(ProcessStepType step, double pressureTorr)
        {
            if (step == ProcessStepType.ChamberVent)
                return "Nitrogen Purge / Repressurizing";

            if (pressureTorr >= 700.0)
                return "Atmospheric (760 Torr)";

            if (pressureTorr >= 1.0e-3)
                return "Roughing Vacuum (Mechanical Pump)";

            return "High Vacuum (Turbo Molecular Pump)";
        }
    }
}
