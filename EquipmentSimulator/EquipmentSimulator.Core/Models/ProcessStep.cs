using System;

namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Encapsulates the recipe-driven engineering parameters and nominal duration for an individual process step.
    /// </summary>
    public class ProcessStep
    {
        public ProcessStepType StepType { get; }
        public string Name { get; }
        public string Description { get; }
        public double DurationSeconds { get; }
        public double TargetVacuumTorr { get; }
        public double TargetDoseMilliJoules { get; }
        public double TargetTemperatureCelsius { get; }

        public ProcessStep(
            ProcessStepType stepType,
            string name,
            string description,
            double durationSeconds,
            double targetVacuumTorr = 1.0e-6,
            double targetDoseMilliJoules = 45.0,
            double targetTemperatureCelsius = 21.50)
        {
            if (durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be greater than zero seconds.");

            StepType = stepType;
            Name = name ?? stepType.ToString();
            Description = description ?? string.Empty;
            DurationSeconds = durationSeconds;
            TargetVacuumTorr = targetVacuumTorr;
            TargetDoseMilliJoules = targetDoseMilliJoules;
            TargetTemperatureCelsius = targetTemperatureCelsius;
        }

        public override string ToString() => $"[Step {(int)StepType}] {Name} ({DurationSeconds:F1}s)";
    }
}
