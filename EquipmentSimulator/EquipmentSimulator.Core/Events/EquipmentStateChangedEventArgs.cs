using System;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Events
{
    /// <summary>
    /// Event arguments containing domain metadata when an equipment state transition occurs.
    /// </summary>
    public class EquipmentStateChangedEventArgs : EventArgs
    {
        public EquipmentState PreviousState { get; }
        public EquipmentState NewState { get; }
        public string Reason { get; }
        public DateTime TimestampUtc { get; }

        public EquipmentStateChangedEventArgs(
            EquipmentState previousState,
            EquipmentState newState,
            string reason,
            DateTime timestampUtc)
        {
            PreviousState = previousState;
            NewState = newState;
            Reason = reason ?? string.Empty;
            TimestampUtc = timestampUtc;
        }

        public override string ToString()
        {
            return $"[{TimestampUtc:HH:mm:ss.fff}] State changed from {PreviousState} to {NewState}. Reason: {(string.IsNullOrEmpty(Reason) ? "Normal transition" : Reason)}";
        }
    }
}
