using System;

namespace EquipmentSimulator.Core.Events
{
    /// <summary>
    /// Event arguments fired when the entire production lot has finished processing all wafers.
    /// </summary>
    public class LotCompletedEventArgs : EventArgs
    {
        public string LotId { get; }
        public string RecipeId { get; }
        public int TotalWafersProcessed { get; }
        public TimeSpan TotalElapsedTime { get; }
        public DateTime TimestampUtc { get; }

        public LotCompletedEventArgs(
            string lotId,
            string recipeId,
            int totalWafersProcessed,
            TimeSpan totalElapsedTime,
            DateTime timestampUtc)
        {
            LotId = lotId ?? string.Empty;
            RecipeId = recipeId ?? string.Empty;
            TotalWafersProcessed = totalWafersProcessed;
            TotalElapsedTime = totalElapsedTime;
            TimestampUtc = timestampUtc;
        }

        public override string ToString()
        {
            return $"[Lot Complete] Lot '{LotId}' (Recipe: {RecipeId}) finished {TotalWafersProcessed} wafers in {TotalElapsedTime.TotalSeconds:F1}s.";
        }
    }
}
