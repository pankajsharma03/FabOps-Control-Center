namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Represents the discrete sequential physical processing steps in a semiconductor manufacturing process cycle.
    /// Aligned with standard photolithography, etch, and deposition sequence architectures.
    /// </summary>
    public enum ProcessStepType
    {
        /// <summary>
        /// Step 1: Robotic atmospheric wafer transfer from FOUP to pre-aligner and loading into chamber chuck.
        /// </summary>
        WaferLoad = 1,

        /// <summary>
        /// Step 2: Chamber evacuation from atmospheric pressure (760 Torr) to high vacuum (1.0e-6 Torr).
        /// </summary>
        PumpDown = 2,

        /// <summary>
        /// Step 3: Laser exposure / RF plasma processing and reticle scanning dose delivery.
        /// </summary>
        Exposure = 3,

        /// <summary>
        /// Step 4: High-purity Nitrogen (N2) purge and chamber repressurization back to 760 Torr.
        /// </summary>
        ChamberVent = 4,

        /// <summary>
        /// Step 5: Robotic wafer unload from chuck back to FOUP carrier cassette and lot index advance.
        /// </summary>
        WaferUnload = 5
    }
}
