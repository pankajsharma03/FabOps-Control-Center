namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Result object encapsulating the outcome of an equipment state transition attempt.
    /// Follows the Result Pattern to provide explicit failure reasons without relying on exceptions.
    /// </summary>
    public class StateTransitionResult
    {
        public bool IsSuccess { get; }
        public EquipmentState FromState { get; }
        public EquipmentState ToState { get; }
        public string Message { get; }

        private StateTransitionResult(bool isSuccess, EquipmentState fromState, EquipmentState toState, string message)
        {
            IsSuccess = isSuccess;
            FromState = fromState;
            ToState = toState;
            Message = message;
        }

        public static StateTransitionResult Success(EquipmentState from, EquipmentState to, string message = null)
        {
            return new StateTransitionResult(
                true,
                from,
                to,
                message ?? $"Successfully transitioned from {from} to {to}.");
        }

        public static StateTransitionResult Failure(EquipmentState from, EquipmentState to, string reason)
        {
            return new StateTransitionResult(
                false,
                from,
                to,
                reason);
        }

        public override string ToString()
        {
            return $"[StateTransition: {(IsSuccess ? "Success" : "Failed")}] {FromState} -> {ToState} | {Message}";
        }
    }
}
