using System;

namespace EquipmentSimulator.Core.Events
{
    /// <summary>
    /// Event arguments fired when an individual wafer has successfully completed all recipe steps.
    /// </summary>
    public class WaferProcessedEventArgs : EventArgs
    {
        public int WaferNumber { get; }
        public int TotalWafers { get; }
        public string LotId { get; }
        public string RecipeId { get; }
        public TimeSpan Duration { get; }
        public bool IsSuccess { get; }
        public DateTime TimestampUtc { get; }

        public WaferProcessedEventArgs(
            int waferNumber,
            int totalWafers,
            string lotId,
            string recipeId,
            TimeSpan duration,
            bool isSuccess,
            DateTime timestampUtc)
        {
            WaferNumber = waferNumber;
            TotalWafers = totalWafers;
            LotId = lotId ?? string.Empty;
            RecipeId = recipeId ?? string.Empty;
            Duration = duration;
            IsSuccess = isSuccess;
            TimestampUtc = timestampUtc;
        }

        public override string ToString()
        {
            return $"[Wafer Complete] Wafer {WaferNumber}/{TotalWafers} in Lot '{LotId}' processed in {Duration.TotalSeconds:F1}s (Success: {IsSuccess})";
        }
    }
}
