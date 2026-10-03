namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Categorization of simulated industrial hardware and process anomalies for fault injection testing.
    /// </summary>
    public enum SimulationFaultType
    {
        /// <summary>
        /// Normal operational parameters; no active fault injected.
        /// </summary>
        None = 0,

        /// <summary>
        /// Chamber vacuum failure / turbo molecular pump interlock trip.
        /// Vacuum pressure suddenly spikes toward atmospheric range.
        /// </summary>
        VacuumLeak = 1,

        /// <summary>
        /// Wafer stage / chuck temperature exceeds upper critical control limit (> 23.0°C).
        /// </summary>
        ThermalExcursion = 2,

        /// <summary>
        /// Laser optical dose uniformity / beam homogenizer degradation below critical threshold (< 98.0%).
        /// </summary>
        LaserUniformityDegradation = 3,

        /// <summary>
        /// Atmospheric or vacuum robotic transfer end-effector optical sensor lost wafer grip.
        /// </summary>
        WaferTransferError = 4
    }
}
