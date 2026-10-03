using System;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Events
{
    /// <summary>
    /// Event arguments fired on high-frequency live physics telemetry sample updates.
    /// </summary>
    public class TelemetryUpdatedEventArgs : EventArgs
    {
        public TelemetrySnapshot Telemetry { get; }

        public TelemetryUpdatedEventArgs(TelemetrySnapshot telemetry)
        {
            Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }
    }
}
