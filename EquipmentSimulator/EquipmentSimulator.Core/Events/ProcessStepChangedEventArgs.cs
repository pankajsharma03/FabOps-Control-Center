using System;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Events
{
    /// <summary>
    /// Event arguments fired when the process sequencer transitions between recipe steps.
    /// </summary>
    public class ProcessStepChangedEventArgs : EventArgs
    {
        public ProcessStepType PreviousStep { get; }
        public ProcessStepType NewStep { get; }
        public string StepName { get; }
        public int WaferNumber { get; }
        public int TotalWafers { get; }
        public DateTime TimestampUtc { get; }

        public ProcessStepChangedEventArgs(
            ProcessStepType previousStep,
            ProcessStepType newStep,
            string stepName,
            int waferNumber,
            int totalWafers,
            DateTime timestampUtc)
        {
            PreviousStep = previousStep;
            NewStep = newStep;
            StepName = stepName ?? newStep.ToString();
            WaferNumber = waferNumber;
            TotalWafers = totalWafers;
            TimestampUtc = timestampUtc;
        }

        public override string ToString()
        {
            return $"[Step Transition] Wafer {WaferNumber}/{TotalWafers} -> Step {(int)NewStep}: {StepName}";
        }
    }
}
