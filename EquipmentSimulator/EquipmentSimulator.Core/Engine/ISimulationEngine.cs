using System;
using System.Threading;
using System.Threading.Tasks;
using EquipmentSimulator.Core.Events;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Engine
{
    /// <summary>
    /// Contract for the autonomous semiconductor process simulation engine / brain.
    /// Coordinates recipe step sequencing, physics telemetry generation, and fault injection.
    /// </summary>
    public interface ISimulationEngine : IDisposable
    {
        Equipment Equipment { get; }
        Recipe ActiveRecipe { get; }
        string ActiveLotId { get; }
        int CurrentWaferNumber { get; }
        int TotalWafersInLot { get; }
        bool IsRunning { get; }
        bool IsPaused { get; }
        double SpeedMultiplier { get; }
        TelemetrySnapshot CurrentTelemetry { get; }

        event EventHandler<TelemetryUpdatedEventArgs> TelemetryUpdated;
        event EventHandler<ProcessStepChangedEventArgs> StepChanged;
        event EventHandler<WaferProcessedEventArgs> WaferCompleted;
        event EventHandler<LotCompletedEventArgs> LotCompleted;
        event EventHandler<FaultInjectedEventArgs> FaultTriggered;

        Task StartAsync(Recipe recipe, string lotId, int totalWafers = 25, CancellationToken cancellationToken = default);
        void Pause();
        void Resume();
        void Stop();
        void InjectFault(SimulationFaultType faultType, string customMessage = null);
        void SetSpeedMultiplier(double multiplier);
        void ResetToStandby();
    }
}
