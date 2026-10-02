namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Represents the communication connection status between the equipment simulator
    /// and the host/FabOps operations center. Decoupled from equipment operational state.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }
}
