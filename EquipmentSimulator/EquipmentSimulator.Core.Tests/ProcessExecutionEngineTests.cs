using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EquipmentSimulator.Core.Engine;
using EquipmentSimulator.Core.Events;
using EquipmentSimulator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EquipmentSimulator.Core.Tests
{
    [TestClass]
    public class ProcessExecutionEngineTests
    {
        private Recipe CreateFastTestRecipe()
        {
            return new Recipe(
                "TEST-RECIPE-FAST",
                "Fast Test Recipe",
                "Ultra-fast recipe for automated test execution",
                "Test-Layer",
                new List<ProcessStep>
                {
                    new ProcessStep(ProcessStepType.WaferLoad, "Load", "Load", 0.05, 760.0, 0.0, 21.50),
                    new ProcessStep(ProcessStepType.PumpDown, "Pump", "Pump", 0.05, 1.0e-6, 0.0, 21.50),
                    new ProcessStep(ProcessStepType.Exposure, "Expose", "Expose", 0.05, 1.0e-6, 45.0, 21.50),
                    new ProcessStep(ProcessStepType.ChamberVent, "Vent", "Vent", 0.05, 760.0, 0.0, 21.50),
                    new ProcessStep(ProcessStepType.WaferUnload, "Unload", "Unload", 0.05, 760.0, 0.0, 21.50)
                },
                exposureDoseMilliJoules: 45.0,
                targetVacuumTorr: 1.0e-6,
                targetTemperatureCelsius: 21.50,
                defaultWafersPerLot: 2);
        }

        [TestMethod]
        public async Task StartAsync_FromIdle_TransitionsToExecutingAndCompletes()
        {
            var equipment = new Equipment("EQ-TEST-001");
            equipment.TransitionTo(EquipmentState.Idle);

            using (var engine = new ProcessExecutionEngine(equipment))
            {
                engine.SetSpeedMultiplier(10.0);
                var recipe = CreateFastTestRecipe();

                var lotCompletedFired = false;
                var wafersCompletedCount = 0;

                engine.WaferCompleted += (s, e) => wafersCompletedCount++;
                engine.LotCompleted += (s, e) => lotCompletedFired = true;

                await engine.StartAsync(recipe, "LOT-TEST-001", totalWafers: 2);
                Assert.AreEqual(EquipmentState.Executing, equipment.CurrentState);

                // Wait for completion (fast test recipe should complete within 3 seconds)
                var timeout = DateTime.UtcNow.AddSeconds(5);
                while (engine.IsRunning && DateTime.UtcNow < timeout)
                {
                    await Task.Delay(50);
                }

                Assert.IsFalse(engine.IsRunning, "Engine should finish running");
                Assert.IsTrue(lotCompletedFired, "LotCompleted event should have fired");
                Assert.AreEqual(2, wafersCompletedCount, "Should have processed 2 wafers");
                Assert.AreEqual(EquipmentState.Idle, equipment.CurrentState, "Equipment should return to Idle after lot completion");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task StartAsync_FromOfflineState_ThrowsInvalidOperationException()
        {
            var equipment = new Equipment("EQ-TEST-002"); // Starts in Offline
            using (var engine = new ProcessExecutionEngine(equipment))
            {
                var recipe = CreateFastTestRecipe();
                await engine.StartAsync(recipe, "LOT-TEST-002");
            }
        }

        [TestMethod]
        public async Task PauseAndResume_ControlsExecutionFlowCorrectly()
        {
            var equipment = new Equipment("EQ-TEST-003");
            equipment.TransitionTo(EquipmentState.Idle);

            using (var engine = new ProcessExecutionEngine(equipment))
            {
                engine.SetSpeedMultiplier(1.0); // slower so we can pause
                var recipe = CreateFastTestRecipe();

                await engine.StartAsync(recipe, "LOT-TEST-003", totalWafers: 2);
                Assert.AreEqual(EquipmentState.Executing, equipment.CurrentState);

                await Task.Delay(80);

                engine.Pause();
                Assert.IsTrue(engine.IsPaused, "Engine should be paused");
                Assert.AreEqual(EquipmentState.Paused, equipment.CurrentState, "Equipment state should be Paused");

                engine.Resume();
                Assert.IsFalse(engine.IsPaused, "Engine should no longer be paused");
                Assert.AreEqual(EquipmentState.Executing, equipment.CurrentState, "Equipment state should be Executing");

                engine.Stop();
                Assert.AreEqual(EquipmentState.Idle, equipment.CurrentState, "Equipment should be Idle after stop");
            }
        }

        [TestMethod]
        public async Task Stop_HaltsExecutionAndTransitionsToIdle()
        {
            var equipment = new Equipment("EQ-TEST-004");
            equipment.TransitionTo(EquipmentState.Idle);

            using (var engine = new ProcessExecutionEngine(equipment))
            {
                var recipe = CreateFastTestRecipe();
                await engine.StartAsync(recipe, "LOT-TEST-004", totalWafers: 5);

                await Task.Delay(60);
                Assert.IsTrue(engine.IsRunning);

                engine.Stop();

                Assert.IsFalse(engine.IsRunning);
                Assert.AreEqual(EquipmentState.Idle, equipment.CurrentState);
                Assert.AreEqual("(None)", string.IsNullOrEmpty(equipment.ActiveRecipeId) ? "(None)" : equipment.ActiveRecipeId);
            }
        }

        [TestMethod]
        public async Task InjectFault_VacuumLeak_TripsToAlarmStateWithCode()
        {
            var equipment = new Equipment("EQ-TEST-005");
            equipment.TransitionTo(EquipmentState.Idle);

            using (var engine = new ProcessExecutionEngine(equipment))
            {
                var faultFired = false;
                FaultInjectedEventArgs faultArgs = null;

                engine.FaultTriggered += (s, e) =>
                {
                    faultFired = true;
                    faultArgs = e;
                };

                var recipe = CreateFastTestRecipe();
                await engine.StartAsync(recipe, "LOT-TEST-005", totalWafers: 3);

                await Task.Delay(60);

                engine.InjectFault(SimulationFaultType.VacuumLeak);

                await Task.Delay(120);

                Assert.IsFalse(engine.IsRunning, "Engine should stop upon fault injection");
                Assert.AreEqual(EquipmentState.Alarm, equipment.CurrentState, "Equipment must be in Alarm state");
                Assert.IsTrue(faultFired, "FaultTriggered event should have fired");
                Assert.AreEqual("ALARM-VAC-301", faultArgs.AlarmCode);
                Assert.IsTrue(equipment.LastAlarmMessage.Contains("ALARM-VAC-301"));
            }
        }

        [TestMethod]
        public async Task InjectFault_ThermalExcursion_TripsToAlarmStateWithThermalCode()
        {
            var equipment = new Equipment("EQ-TEST-006");
            equipment.TransitionTo(EquipmentState.Idle);

            using (var engine = new ProcessExecutionEngine(equipment))
            {
                var recipe = CreateFastTestRecipe();
                await engine.StartAsync(recipe, "LOT-TEST-006", totalWafers: 2);

                await Task.Delay(60);

                engine.InjectFault(SimulationFaultType.ThermalExcursion);

                await Task.Delay(120);

                Assert.AreEqual(EquipmentState.Alarm, equipment.CurrentState);
                Assert.IsTrue(equipment.LastAlarmMessage.Contains("ALARM-TH-402"));
            }
        }

        [TestMethod]
        public async Task TelemetryUpdated_FiresDuringExecutionWithValidMetrics()
        {
            var equipment = new Equipment("EQ-TEST-007");
            equipment.TransitionTo(EquipmentState.Idle);

            using (var engine = new ProcessExecutionEngine(equipment))
            {
                var telemetryCount = 0;
                engine.TelemetryUpdated += (s, e) =>
                {
                    telemetryCount++;
                    Assert.IsNotNull(e.Telemetry);
                    Assert.IsTrue(e.Telemetry.ChamberTemperatureCelsius > 0);
                    Assert.IsTrue(e.Telemetry.VacuumPressureTorr > 0);
                };

                var recipe = CreateFastTestRecipe();
                await engine.StartAsync(recipe, "LOT-TEST-007", totalWafers: 1);

                await Task.Delay(200);

                engine.Stop();

                Assert.IsTrue(telemetryCount > 0, "TelemetryUpdated should fire multiple times during execution");
            }
        }
    }
}
