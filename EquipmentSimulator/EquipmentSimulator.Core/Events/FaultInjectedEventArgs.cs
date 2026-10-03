using System;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Events
{
    /// <summary>
    /// Event arguments fired when an anomaly or fault is injected into the simulation engine.
    /// </summary>
    public class FaultInjectedEventArgs : EventArgs
    {
        public SimulationFaultType FaultType { get; }
        public string AlarmCode { get; }
        public string Message { get; }
        public DateTime TimestampUtc { get; }

        public FaultInjectedEventArgs(
            SimulationFaultType faultType,
            string alarmCode,
            string message,
            DateTime timestampUtc)
        {
            FaultType = faultType;
            AlarmCode = alarmCode ?? "ALARM-GEN-999";
            Message = message ?? string.Empty;
            TimestampUtc = timestampUtc;
        }

        public override string ToString()
        {
            return $"[Fault Injected] [{AlarmCode}] {FaultType}: {Message}";
        }
    }
}
