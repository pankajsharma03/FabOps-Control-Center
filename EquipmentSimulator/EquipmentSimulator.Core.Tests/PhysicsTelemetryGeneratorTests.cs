using System;
using EquipmentSimulator.Core.Engine;
using EquipmentSimulator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EquipmentSimulator.Core.Tests
{
    [TestClass]
    public class PhysicsTelemetryGeneratorTests
    {
        [TestMethod]
        public void CalculateVacuumPressure_AtAtmosphere_ReturnsNear760Torr()
        {
            var p1 = PhysicsTelemetryGenerator.CalculateVacuumPressure(ProcessStepType.WaferLoad, 0.0, 1.0e-6);
            var p2 = PhysicsTelemetryGenerator.CalculateVacuumPressure(ProcessStepType.WaferUnload, 1.0, 1.0e-6);

            Assert.IsTrue(p1 >= 758.0 && p1 <= 762.0, $"Expected atmospheric pressure, got {p1}");
            Assert.IsTrue(p2 >= 758.0 && p2 <= 762.0, $"Expected atmospheric pressure, got {p2}");
        }

        [TestMethod]
        public void CalculateVacuumPressure_PumpDown_DecaysExponentiallyToTarget()
        {
            var pStart = PhysicsTelemetryGenerator.CalculateVacuumPressure(ProcessStepType.PumpDown, 0.0, 1.0e-6);
            var pMid = PhysicsTelemetryGenerator.CalculateVacuumPressure(ProcessStepType.PumpDown, 0.5, 1.0e-6);
            var pEnd = PhysicsTelemetryGenerator.CalculateVacuumPressure(ProcessStepType.PumpDown, 1.0, 1.0e-6);

            Assert.IsTrue(pStart >= 700.0, $"Start pressure should be near atm, got {pStart}");
            Assert.IsTrue(pMid < pStart && pMid > pEnd, $"Mid pressure {pMid} should be between start {pStart} and end {pEnd}");
            Assert.IsTrue(pEnd <= 2.0e-6, $"End pressure should reach near 1e-6 Torr, got {pEnd}");
        }

        [TestMethod]
        public void CalculateVacuumPressure_ChamberVent_RepressurizesToAtmosphere()
        {
            var pStart = PhysicsTelemetryGenerator.CalculateVacuumPressure(ProcessStepType.ChamberVent, 0.0, 1.0e-6);
            var pEnd = PhysicsTelemetryGenerator.CalculateVacuumPressure(ProcessStepType.ChamberVent, 1.0, 1.0e-6);

            Assert.IsTrue(pStart < 1.0e-4, $"Vent start pressure should be low, got {pStart}");
            Assert.IsTrue(pEnd >= 750.0, $"Vent end pressure should reach atm, got {pEnd}");
        }

        [TestMethod]
        public void CalculateVacuumPressure_VacuumLeakFault_SpikesPressure()
        {
            var pFault = PhysicsTelemetryGenerator.CalculateVacuumPressure(
                ProcessStepType.Exposure,
                0.5,
                1.0e-6,
                SimulationFaultType.VacuumLeak);

            Assert.IsTrue(pFault >= 1.0e-3, $"Vacuum leak fault should spike pressure, got {pFault}");
        }

        [TestMethod]
        public void CalculateTemperature_NormalExposure_ShowsSlightLaserThermalLoad()
        {
            var tempLoad = PhysicsTelemetryGenerator.CalculateTemperature(ProcessStepType.WaferLoad, 0.5, 21.50);
            var tempExpose = PhysicsTelemetryGenerator.CalculateTemperature(ProcessStepType.Exposure, 0.5, 21.50);

            Assert.IsTrue(tempLoad >= 21.45 && tempLoad <= 21.55, $"Standby temp out of range: {tempLoad}");
            Assert.IsTrue(tempExpose > 21.55, $"Laser exposure should increase chuck temp, got {tempExpose}");
        }

        [TestMethod]
        public void CalculateTemperature_ThermalExcursionFault_ExceedsSafetyLimit()
        {
            var tempFault = PhysicsTelemetryGenerator.CalculateTemperature(
                ProcessStepType.Exposure,
                0.5,
                21.50,
                SimulationFaultType.ThermalExcursion);

            Assert.IsTrue(tempFault >= 23.0, $"Thermal fault should exceed 23.0°C limit, got {tempFault}");
        }

        [TestMethod]
        public void CalculateLaserFlux_OnlyActiveDuringExposureStep()
        {
            var fluxLoad = PhysicsTelemetryGenerator.CalculateLaserFlux(ProcessStepType.WaferLoad, 0.5, 45.0);
            var fluxPump = PhysicsTelemetryGenerator.CalculateLaserFlux(ProcessStepType.PumpDown, 0.5, 45.0);
            var fluxExpose = PhysicsTelemetryGenerator.CalculateLaserFlux(ProcessStepType.Exposure, 0.5, 45.0);
            var fluxVent = PhysicsTelemetryGenerator.CalculateLaserFlux(ProcessStepType.ChamberVent, 0.5, 45.0);

            Assert.AreEqual(0.0, fluxLoad);
            Assert.AreEqual(0.0, fluxPump);
            Assert.IsTrue(fluxExpose >= 44.0 && fluxExpose <= 46.0, $"Laser flux during exposure should be near 45.0, got {fluxExpose}");
            Assert.AreEqual(0.0, fluxVent);
        }

        [TestMethod]
        public void CalculateLaserFlux_LaserFault_DropsUniformityAndDose()
        {
            var fluxFault = PhysicsTelemetryGenerator.CalculateLaserFlux(
                ProcessStepType.Exposure,
                0.5,
                45.0,
                SimulationFaultType.LaserUniformityDegradation);

            Assert.IsTrue(fluxFault < 35.0, $"Degraded optics should deliver significantly reduced dose, got {fluxFault}");
        }

        [TestMethod]
        public void DetermineVacuumPhase_ReturnsCorrectRegimes()
        {
            var phaseAtm = PhysicsTelemetryGenerator.DetermineVacuumPhase(ProcessStepType.WaferLoad, 760.0);
            var phaseRough = PhysicsTelemetryGenerator.DetermineVacuumPhase(ProcessStepType.PumpDown, 5.0);
            var phaseHighVac = PhysicsTelemetryGenerator.DetermineVacuumPhase(ProcessStepType.Exposure, 1.0e-6);
            var phaseVent = PhysicsTelemetryGenerator.DetermineVacuumPhase(ProcessStepType.ChamberVent, 100.0);

            Assert.IsTrue(phaseAtm.Contains("Atmospheric"));
            Assert.IsTrue(phaseRough.Contains("Roughing"));
            Assert.IsTrue(phaseHighVac.Contains("High Vacuum"));
            Assert.IsTrue(phaseVent.Contains("Purge") || phaseVent.Contains("Venting"));
        }
    }
}
