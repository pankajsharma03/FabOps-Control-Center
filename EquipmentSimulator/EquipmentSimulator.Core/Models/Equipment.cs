using System;
using EquipmentSimulator.Core.Events;
using EquipmentSimulator.Core.Rules;

namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Aggregate root entity representing semiconductor manufacturing equipment in the fab.
    /// Encapsulates equipment identification, operational state, connection status, recipes, and transitions.
    /// </summary>
    public class Equipment
    {
        public string EquipmentId { get; }
        public string ModelType { get; }
        public string SerialNumber { get; }
        public EquipmentState CurrentState { get; private set; }
        public ConnectionState ConnectionStatus { get; private set; }
        public DateTime LastStateChangeUtc { get; private set; }
        public string ActiveRecipeId { get; private set; }
        public string ActiveLotId { get; private set; }
        public string LastAlarmMessage { get; private set; }

        public event EventHandler<EquipmentStateChangedEventArgs> StateChanged;
        public event EventHandler<ConnectionState> ConnectionStatusChanged;

        public Equipment(
            string equipmentId,
            string modelType = "Litho-Scanner-300mm",
            string serialNumber = "SN-LITH-2026-001")
        {
            if (string.IsNullOrWhiteSpace(equipmentId))
                throw new ArgumentException("EquipmentId cannot be null or empty.", nameof(equipmentId));

            EquipmentId = equipmentId.Trim();
            ModelType = modelType ?? "Generic-Equipment";
            SerialNumber = serialNumber ?? "SN-UNKNOWN";
            CurrentState = EquipmentState.Offline;
            ConnectionStatus = ConnectionState.Disconnected;
            LastStateChangeUtc = DateTime.UtcNow;
            ActiveRecipeId = string.Empty;
            ActiveLotId = string.Empty;
            LastAlarmMessage = string.Empty;
        }

        /// <summary>
        /// Attempts to transition the equipment to a new operational state.
        /// </summary>
        public StateTransitionResult TransitionTo(EquipmentState targetState, string reason = null)
        {
            var validation = StateTransitionMatrix.ValidateAndTransition(CurrentState, targetState);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            var previousState = CurrentState;
            CurrentState = targetState;
            LastStateChangeUtc = DateTime.UtcNow;

            if (targetState == EquipmentState.Alarm && !string.IsNullOrEmpty(reason))
            {
                LastAlarmMessage = reason;
            }
            else if (targetState == EquipmentState.Idle && previousState == EquipmentState.Alarm)
            {
                LastAlarmMessage = string.Empty;
            }

            // Fire domain event
            StateChanged?.Invoke(this, new EquipmentStateChangedEventArgs(
                previousState,
                targetState,
                reason,
                LastStateChangeUtc));

            return validation;
        }

        /// <summary>
        /// Checks if a transition to the specified target state is currently allowed.
        /// </summary>
        public bool CanTransitionTo(EquipmentState targetState)
        {
            return StateTransitionMatrix.CanTransition(CurrentState, targetState);
        }

        /// <summary>
        /// Sets communication status (decoupled from equipment operational state).
        /// </summary>
        public void SetConnectionStatus(ConnectionState status)
        {
            if (ConnectionStatus == status)
                return;

            ConnectionStatus = status;
            ConnectionStatusChanged?.Invoke(this, status);
        }

        /// <summary>
        /// Loads a production recipe and lot onto the equipment.
        /// </summary>
        public void LoadRecipeAndLot(string recipeId, string lotId)
        {
            ActiveRecipeId = recipeId ?? string.Empty;
            ActiveLotId = lotId ?? string.Empty;
        }

        /// <summary>
        /// Clears the currently loaded recipe and lot from the equipment.
        /// </summary>
        public void ClearRecipeAndLot()
        {
            ActiveRecipeId = string.Empty;
            ActiveLotId = string.Empty;
        }
    }
}
