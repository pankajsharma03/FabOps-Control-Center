namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Represents the high-level operational state of semiconductor manufacturing equipment,
    /// aligned with SEMI E30 (GEM) state model concepts.
    /// </summary>
    public enum EquipmentState
    {
        /// <summary>
        /// Equipment is powered down, offline, or disconnected from the control system.
        /// </summary>
        Offline,

        /// <summary>
        /// Equipment is fully operational, initialized, and waiting for recipe/lot setup.
        /// </summary>
        Idle,

        /// <summary>
        /// Equipment is loading recipes, positioning reticles/wafers, or performing pre-run calibration.
        /// </summary>
        Setup,

        /// <summary>
        /// Equipment is actively executing a process run (e.g. lithography exposure, etching).
        /// </summary>
        Executing,

        /// <summary>
        /// Equipment execution is temporarily halted (operator pause, feed hold, or chamber stabilization).
        /// </summary>
        Paused,

        /// <summary>
        /// Equipment has encountered an abnormal condition, safety interlock trip, or hardware fault.
        /// </summary>
        Alarm,

        /// <summary>
        /// Equipment is offline for preventive maintenance, calibration, or technician diagnosis.
        /// </summary>
        Maintenance
    }
}
