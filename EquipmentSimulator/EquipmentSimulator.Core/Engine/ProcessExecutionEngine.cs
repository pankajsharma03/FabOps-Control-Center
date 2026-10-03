using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EquipmentSimulator.Core.Events;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Engine
{
    /// <summary>
    /// Autonomous semiconductor process execution engine / tool brain.
    /// Orchestrates wafer processing sequences, physics telemetry feeds, pause/resume loops,
    /// and dynamic fault injection with deterministic SEMI E30 state machine synchronization.
    /// </summary>
    public class ProcessExecutionEngine : ISimulationEngine
    {
        private readonly object _syncLock = new object();
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        private CancellationTokenSource _cts;
        private Task _runningTask;

        private volatile bool _isRunning;
        private volatile bool _isPaused;
        private double _speedMultiplier = 1.0;
        private volatile SimulationFaultType _pendingFault = SimulationFaultType.None;
        private volatile string _customFaultMessage;

        public Equipment Equipment { get; }
        public Recipe ActiveRecipe { get; private set; }
        public string ActiveLotId { get; private set; }
        public int CurrentWaferNumber { get; private set; }
        public int TotalWafersInLot { get; private set; }
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public double SpeedMultiplier => _speedMultiplier;
        public TelemetrySnapshot CurrentTelemetry { get; private set; }

        public event EventHandler<TelemetryUpdatedEventArgs> TelemetryUpdated;
        public event EventHandler<ProcessStepChangedEventArgs> StepChanged;
        public event EventHandler<WaferProcessedEventArgs> WaferCompleted;
        public event EventHandler<LotCompletedEventArgs> LotCompleted;
        public event EventHandler<FaultInjectedEventArgs> FaultTriggered;

        public ProcessExecutionEngine(Equipment equipment)
        {
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            CurrentTelemetry = TelemetrySnapshot.CreateStandby();
        }

        public async Task StartAsync(
            Recipe recipe,
            string lotId,
            int totalWafers = 25,
            CancellationToken cancellationToken = default)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            if (string.IsNullOrWhiteSpace(lotId))
                throw new ArgumentException("Lot ID cannot be null or empty.", nameof(lotId));

            lock (_syncLock)
            {
                if (_isRunning)
                    throw new InvalidOperationException("Simulation is already running.");

                ActiveRecipe = recipe;
                ActiveLotId = lotId;
                TotalWafersInLot = totalWafers > 0 ? totalWafers : recipe.DefaultWafersPerLot;
                CurrentWaferNumber = 1;
                _pendingFault = SimulationFaultType.None;
                _customFaultMessage = null;
                _isPaused = false;
                _pauseEvent.Set();

                Equipment.LoadRecipeAndLot(recipe.RecipeId, lotId);

                // SEMI E30 GEM standard: Idle -> Setup (loading recipe/lot) -> Executing (process run)
                if (Equipment.CurrentState == EquipmentState.Idle)
                {
                    var setupResult = Equipment.TransitionTo(EquipmentState.Setup, $"Recipe '{recipe.RecipeId}' and Lot '{lotId}' configured for processing.");
                    if (!setupResult.IsSuccess)
                        throw new InvalidOperationException($"Cannot transition to Setup: {setupResult.Message}");
                }

                if (Equipment.CurrentState == EquipmentState.Setup)
                {
                    var execResult = Equipment.TransitionTo(EquipmentState.Executing, $"Autonomous process run started for Lot '{lotId}' with Recipe '{recipe.RecipeId}'.");
                    if (!execResult.IsSuccess)
                        throw new InvalidOperationException($"Cannot start process run: {execResult.Message}");
                }
                else if (Equipment.CurrentState != EquipmentState.Executing)
                {
                    throw new InvalidOperationException($"Cannot start process run from state '{Equipment.CurrentState}'. Equipment must be in Idle, Setup, or Executing state.");
                }

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _isRunning = true;
            }

            _runningTask = Task.Run(() => RunSimulationLoopAsync(_cts.Token), _cts.Token);
            await Task.Yield();
        }

        public void Pause()
        {
            lock (_syncLock)
            {
                if (!_isRunning || _isPaused)
                    return;

                var result = Equipment.TransitionTo(EquipmentState.Paused, "Simulation execution paused by operator.");
                if (result.IsSuccess)
                {
                    _isPaused = true;
                    _pauseEvent.Reset();
                }
            }
        }

        public void Resume()
        {
            lock (_syncLock)
            {
                if (!_isRunning || !_isPaused)
                    return;

                var result = Equipment.TransitionTo(EquipmentState.Executing, "Simulation execution resumed from hold.");
                if (result.IsSuccess)
                {
                    _isPaused = false;
                    _pauseEvent.Set();
                }
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                if (!_isRunning)
                {
                    if (Equipment.CanTransitionTo(EquipmentState.Idle))
                    {
                        Equipment.ClearRecipeAndLot();
                        Equipment.TransitionTo(EquipmentState.Idle, "System returned to idle.");
                        CurrentTelemetry = TelemetrySnapshot.CreateStandby();
                        PublishTelemetry(CurrentTelemetry);
                    }
                    return;
                }

                _cts?.Cancel();
                _pauseEvent.Set();
                _isRunning = false;
                _isPaused = false;

                if (Equipment.CanTransitionTo(EquipmentState.Idle))
                {
                    Equipment.ClearRecipeAndLot();
                    Equipment.TransitionTo(EquipmentState.Idle, "Process execution stopped by operator.");
                }

                CurrentTelemetry = TelemetrySnapshot.CreateStandby();
                PublishTelemetry(CurrentTelemetry);
            }
        }

        public void InjectFault(SimulationFaultType faultType, string customMessage = null)
        {
            if (faultType == SimulationFaultType.None)
                return;

            lock (_syncLock)
            {
                _pendingFault = faultType;
                _customFaultMessage = customMessage;

                if (!_isRunning)
                {
                    // If not running, trip immediately
                    TriggerImmediateFault(faultType, customMessage);
                }
            }
        }

        public void SetSpeedMultiplier(double multiplier)
        {
            _speedMultiplier = Math.Max(0.1, Math.Min(20.0, multiplier));
        }

        public void ResetToStandby()
        {
            lock (_syncLock)
            {
                _pendingFault = SimulationFaultType.None;
                _customFaultMessage = null;
                CurrentTelemetry = TelemetrySnapshot.CreateStandby();
                PublishTelemetry(CurrentTelemetry);
            }
        }

        #region Private Execution Loop

        private async Task RunSimulationLoopAsync(CancellationToken token)
        {
            var rnd = new Random();
            var lotStopwatch = Stopwatch.StartNew();
            var totalStepsCount = ActiveRecipe.Steps.Count;

            try
            {
                for (int waferIdx = 1; waferIdx <= TotalWafersInLot; waferIdx++)
                {
                    token.ThrowIfCancellationRequested();
                    CurrentWaferNumber = waferIdx;
                    var waferStopwatch = Stopwatch.StartNew();

                    for (int stepIdx = 0; stepIdx < totalStepsCount; stepIdx++)
                    {
                        token.ThrowIfCancellationRequested();

                        var currentStepDef = ActiveRecipe.Steps[stepIdx];
                        var previousStepType = stepIdx > 0 ? ActiveRecipe.Steps[stepIdx - 1].StepType : ProcessStepType.WaferUnload;

                        StepChanged?.Invoke(this, new ProcessStepChangedEventArgs(
                            previousStepType,
                            currentStepDef.StepType,
                            currentStepDef.Name,
                            waferIdx,
                            TotalWafersInLot,
                            DateTime.UtcNow));

                        var stepDurationSec = currentStepDef.DurationSeconds;
                        var elapsedInStepSec = 0.0;
                        const int tickIntervalMs = 50;

                        while (elapsedInStepSec < stepDurationSec)
                        {
                            token.ThrowIfCancellationRequested();

                            // Handle Pause
                            if (_isPaused)
                            {
                                _pauseEvent.Wait(token);
                            }

                            // Check Fault Injection
                            if (_pendingFault != SimulationFaultType.None)
                            {
                                HandleFaultTriggered(_pendingFault, _customFaultMessage, currentStepDef, waferIdx);
                                return;
                            }

                            var progressFraction = Math.Min(1.0, elapsedInStepSec / stepDurationSec);
                            var stepProgressPercent = progressFraction * 100.0;

                            // Lot progress calculation
                            var completedWafersFraction = (double)(waferIdx - 1) / TotalWafersInLot;
                            var currentWaferStepFraction = ((double)stepIdx + progressFraction) / totalStepsCount;
                            var overallLotProgressPercent = (completedWafersFraction + (currentWaferStepFraction / TotalWafersInLot)) * 100.0;

                            // Physics generation
                            var vacuumPressure = PhysicsTelemetryGenerator.CalculateVacuumPressure(
                                currentStepDef.StepType,
                                progressFraction,
                                currentStepDef.TargetVacuumTorr,
                                _pendingFault,
                                rnd);

                            var chamberTemp = PhysicsTelemetryGenerator.CalculateTemperature(
                                currentStepDef.StepType,
                                progressFraction,
                                currentStepDef.TargetTemperatureCelsius,
                                _pendingFault,
                                rnd);

                            var laserFlux = PhysicsTelemetryGenerator.CalculateLaserFlux(
                                currentStepDef.StepType,
                                progressFraction,
                                currentStepDef.TargetDoseMilliJoules,
                                _pendingFault,
                                rnd);

                            var vacuumPhase = PhysicsTelemetryGenerator.DetermineVacuumPhase(currentStepDef.StepType, vacuumPressure);

                            var snapshot = new TelemetrySnapshot(
                                vacuumPressureTorr: vacuumPressure,
                                chamberTemperatureCelsius: chamberTemp,
                                laserEnergyFlux: laserFlux,
                                currentStep: currentStepDef.StepType,
                                currentStepName: currentStepDef.Name,
                                currentStepIndex: stepIdx + 1,
                                totalSteps: totalStepsCount,
                                stepProgressPercent: stepProgressPercent,
                                currentWaferNumber: waferIdx,
                                totalWafersInLot: TotalWafersInLot,
                                overallLotProgressPercent: overallLotProgressPercent,
                                vacuumPhase: vacuumPhase,
                                isLaserActive: currentStepDef.StepType == ProcessStepType.Exposure,
                                isPumpingActive: currentStepDef.StepType == ProcessStepType.PumpDown,
                                isVentingActive: currentStepDef.StepType == ProcessStepType.ChamberVent,
                                timestampUtc: DateTime.UtcNow);

                            CurrentTelemetry = snapshot;
                            PublishTelemetry(snapshot);

                            // Delta time step based on speed multiplier
                            var effectiveDelayMs = (int)Math.Max(10, tickIntervalMs / _speedMultiplier);
                            await Task.Delay(effectiveDelayMs, token);

                            elapsedInStepSec += (tickIntervalMs / 1000.0);
                        }
                    }

                    waferStopwatch.Stop();
                    WaferCompleted?.Invoke(this, new WaferProcessedEventArgs(
                        waferIdx,
                        TotalWafersInLot,
                        ActiveLotId,
                        ActiveRecipe.RecipeId,
                        waferStopwatch.Elapsed,
                        isSuccess: true,
                        DateTime.UtcNow));
                }

                // All wafers finished successfully!
                lotStopwatch.Stop();
                lock (_syncLock)
                {
                    _isRunning = false;
                    Equipment.TransitionTo(EquipmentState.Idle, $"Production Lot '{ActiveLotId}' successfully completed {TotalWafersInLot} wafers in {lotStopwatch.Elapsed.TotalSeconds:F1}s.");
                    CurrentTelemetry = TelemetrySnapshot.CreateStandby();
                    PublishTelemetry(CurrentTelemetry);
                }

                LotCompleted?.Invoke(this, new LotCompletedEventArgs(
                    ActiveLotId,
                    ActiveRecipe.RecipeId,
                    TotalWafersInLot,
                    lotStopwatch.Elapsed,
                    DateTime.UtcNow));
            }
            catch (OperationCanceledException)
            {
                // Clean cancellation when stopped or paused
            }
            finally
            {
                lock (_syncLock)
                {
                    _isRunning = false;
                }
            }
        }

        private void HandleFaultTriggered(
            SimulationFaultType faultType,
            string customMessage,
            ProcessStep currentStep,
            int waferIdx)
        {
            var (alarmCode, defaultMsg) = GetAlarmCodeAndMessage(faultType);
            var fullMessage = string.IsNullOrWhiteSpace(customMessage)
                ? $"[{alarmCode}] {defaultMsg} during step '{currentStep.Name}' on Wafer {waferIdx}/{TotalWafersInLot}."
                : $"[{alarmCode}] {customMessage}";

            lock (_syncLock)
            {
                _isRunning = false;
                _isPaused = false;
                _pendingFault = SimulationFaultType.None;

                // Trip equipment to Alarm state
                Equipment.TransitionTo(EquipmentState.Alarm, fullMessage);

                // Fire event
                FaultTriggered?.Invoke(this, new FaultInjectedEventArgs(faultType, alarmCode, fullMessage, DateTime.UtcNow));
            }
        }

        private void TriggerImmediateFault(SimulationFaultType faultType, string customMessage)
        {
            var (alarmCode, defaultMsg) = GetAlarmCodeAndMessage(faultType);
            var fullMessage = string.IsNullOrWhiteSpace(customMessage)
                ? $"[{alarmCode}] {defaultMsg}"
                : $"[{alarmCode}] {customMessage}";

            _pendingFault = SimulationFaultType.None;
            Equipment.TransitionTo(EquipmentState.Alarm, fullMessage);
            FaultTriggered?.Invoke(this, new FaultInjectedEventArgs(faultType, alarmCode, fullMessage, DateTime.UtcNow));
        }

        private static (string AlarmCode, string Message) GetAlarmCodeAndMessage(SimulationFaultType faultType)
        {
            switch (faultType)
            {
                case SimulationFaultType.VacuumLeak:
                    return ("ALARM-VAC-301", "Chamber vacuum breach: Turbo pump interlock tripped / Pressure deviation detected");
                case SimulationFaultType.ThermalExcursion:
                    return ("ALARM-TH-402", "Wafer chuck thermal runaway: Temperature exceeded critical control limit (> 23.0°C)");
                case SimulationFaultType.LaserUniformityDegradation:
                    return ("ALARM-OPT-505", "Laser optical degradation: Beam homogenizer uniformity fell below 98.0%");
                case SimulationFaultType.WaferTransferError:
                    return ("ALARM-ROB-104", "Wafer handling error: Robotic end-effector vacuum sensor lost wafer grip");
                default:
                    return ("ALARM-GEN-999", "General equipment safety interlock trip");
            }
        }

        private void PublishTelemetry(TelemetrySnapshot telemetry)
        {
            TelemetryUpdated?.Invoke(this, new TelemetryUpdatedEventArgs(telemetry));
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _pauseEvent?.Dispose();
        }

        #endregion
    }
}
