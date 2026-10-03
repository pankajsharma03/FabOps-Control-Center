using System;
using System.Globalization;

namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Immutable telemetry snapshot representing physical sensor readouts,
    /// vacuum status, temperature metrics, and process execution progression.
    /// </summary>
    public class TelemetrySnapshot
    {
        public double VacuumPressureTorr { get; }
        public double ChamberTemperatureCelsius { get; }
        public double LaserEnergyFlux { get; }
        public ProcessStepType CurrentStep { get; }
        public string CurrentStepName { get; }
        public int CurrentStepIndex { get; }
        public int TotalSteps { get; }
        public double StepProgressPercent { get; }
        public int CurrentWaferNumber { get; }
        public int TotalWafersInLot { get; }
        public double OverallLotProgressPercent { get; }
        public string VacuumPhase { get; }
        public bool IsLaserActive { get; }
        public bool IsPumpingActive { get; }
        public bool IsVentingActive { get; }
        public DateTime TimestampUtc { get; }

        public TelemetrySnapshot(
            double vacuumPressureTorr,
            double chamberTemperatureCelsius,
            double laserEnergyFlux,
            ProcessStepType currentStep,
            string currentStepName,
            int currentStepIndex,
            int totalSteps,
            double stepProgressPercent,
            int currentWaferNumber,
            int totalWafersInLot,
            double overallLotProgressPercent,
            string vacuumPhase,
            bool isLaserActive,
            bool isPumpingActive,
            bool isVentingActive,
            DateTime timestampUtc)
        {
            VacuumPressureTorr = vacuumPressureTorr;
            ChamberTemperatureCelsius = chamberTemperatureCelsius;
            LaserEnergyFlux = laserEnergyFlux;
            CurrentStep = currentStep;
            CurrentStepName = currentStepName ?? currentStep.ToString();
            CurrentStepIndex = currentStepIndex;
            TotalSteps = totalSteps > 0 ? totalSteps : 5;
            StepProgressPercent = Math.Max(0.0, Math.Min(100.0, stepProgressPercent));
            CurrentWaferNumber = Math.Max(0, currentWaferNumber);
            TotalWafersInLot = Math.Max(1, totalWafersInLot);
            OverallLotProgressPercent = Math.Max(0.0, Math.Min(100.0, overallLotProgressPercent));
            VacuumPhase = vacuumPhase ?? "Atmospheric";
            IsLaserActive = isLaserActive;
            IsPumpingActive = isPumpingActive;
            IsVentingActive = isVentingActive;
            TimestampUtc = timestampUtc;
        }

        #region Formatted Strings for Presentation & Logs

        public string PressureFormatted
        {
            get
            {
                if (VacuumPressureTorr >= 100.0)
                    return $"{VacuumPressureTorr:F1} Torr";
                if (VacuumPressureTorr >= 1.0)
                    return $"{VacuumPressureTorr:F2} Torr";
                return VacuumPressureTorr.ToString("0.00E-00", CultureInfo.InvariantCulture) + " Torr";
            }
        }

        public string TemperatureFormatted => $"{ChamberTemperatureCelsius:F2} °C";

        public string LaserDoseFormatted => IsLaserActive
            ? $"{LaserEnergyFlux:F1} mJ/cm² (Active Beam)"
            : "0.0 mJ/cm² (Standby)";

        public string StepProgressFormatted => $"{StepProgressPercent:F1}%";

        public string LotProgressFormatted => $"{OverallLotProgressPercent:F1}%";

        public string WaferStatusFormatted => TotalWafersInLot > 0 && CurrentWaferNumber > 0
            ? $"Wafer {CurrentWaferNumber} of {TotalWafersInLot}"
            : "Idle / Standby";

        #endregion

        /// <summary>
        /// Creates a safe atmospheric baseline standby snapshot.
        /// </summary>
        public static TelemetrySnapshot CreateStandby(double setpointTemp = 21.50)
        {
            return new TelemetrySnapshot(
                vacuumPressureTorr: 760.0,
                chamberTemperatureCelsius: setpointTemp,
                laserEnergyFlux: 0.0,
                currentStep: ProcessStepType.WaferLoad,
                currentStepName: "Standby / Atmospheric",
                currentStepIndex: 0,
                totalSteps: 5,
                stepProgressPercent: 0.0,
                currentWaferNumber: 0,
                totalWafersInLot: 25,
                overallLotProgressPercent: 0.0,
                vacuumPhase: "Atmospheric (760 Torr)",
                isLaserActive: false,
                isPumpingActive: false,
                isVentingActive: false,
                timestampUtc: DateTime.UtcNow);
        }

        public override string ToString()
        {
            return $"[Telemetry @ {TimestampUtc:HH:mm:ss.fff}] P: {PressureFormatted} | T: {TemperatureFormatted} | Dose: {LaserDoseFormatted} | Step: {CurrentStepName} ({StepProgressFormatted}) | {WaferStatusFormatted}";
        }
    }
}
