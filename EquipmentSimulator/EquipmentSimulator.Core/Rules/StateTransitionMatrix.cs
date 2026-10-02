using System;
using System.Collections.Generic;
using EquipmentSimulator.Core.Models;

namespace EquipmentSimulator.Core.Rules
{
    /// <summary>
    /// Enforces valid and invalid state transitions for semiconductor equipment.
    /// Encapsulates the core domain transition matrix according to SEMI E30 (GEM) operational state guidelines.
    /// </summary>
    public static class StateTransitionMatrix
    {
        private static readonly Dictionary<EquipmentState, HashSet<EquipmentState>> AllowedTransitions =
            new Dictionary<EquipmentState, HashSet<EquipmentState>>
            {
                // Offline -> Can power up/initialize to Idle
                [EquipmentState.Offline] = new HashSet<EquipmentState>
                {
                    EquipmentState.Idle
                },

                // Idle -> Can load recipe/lot (Setup), enter Maintenance, shutdown to Offline, or trip Alarm
                [EquipmentState.Idle] = new HashSet<EquipmentState>
                {
                    EquipmentState.Setup,
                    EquipmentState.Maintenance,
                    EquipmentState.Offline,
                    EquipmentState.Alarm
                },

                // Setup -> Can start execution (Executing), cancel back to Idle, or trip Alarm
                [EquipmentState.Setup] = new HashSet<EquipmentState>
                {
                    EquipmentState.Executing,
                    EquipmentState.Idle,
                    EquipmentState.Alarm
                },

                // Executing -> Can pause (Paused), complete run (Idle), or trip Alarm
                [EquipmentState.Executing] = new HashSet<EquipmentState>
                {
                    EquipmentState.Paused,
                    EquipmentState.Idle,
                    EquipmentState.Alarm
                },

                // Paused -> Can resume (Executing), abort run (Idle), or trip Alarm
                [EquipmentState.Paused] = new HashSet<EquipmentState>
                {
                    EquipmentState.Executing,
                    EquipmentState.Idle,
                    EquipmentState.Alarm
                },

                // Alarm -> Can clear/reset to Idle or transition to Maintenance for technician intervention
                [EquipmentState.Alarm] = new HashSet<EquipmentState>
                {
                    EquipmentState.Idle,
                    EquipmentState.Maintenance
                },

                // Maintenance -> Can complete service back to Idle, trip Alarm during testing, or power down to Offline
                [EquipmentState.Maintenance] = new HashSet<EquipmentState>
                {
                    EquipmentState.Idle,
                    EquipmentState.Alarm,
                    EquipmentState.Offline
                }
            };

        /// <summary>
        /// Checks whether transitioning from <paramref name="currentState"/> to <paramref name="targetState"/> is permitted.
        /// </summary>
        public static bool CanTransition(EquipmentState currentState, EquipmentState targetState)
        {
            if (AllowedTransitions.TryGetValue(currentState, out var validTargets))
            {
                return validTargets.Contains(targetState);
            }
            return false;
        }

        /// <summary>
        /// Returns all valid target states that can be transitioned to from the specified state.
        /// </summary>
        public static IReadOnlyCollection<EquipmentState> GetAllowedTransitions(EquipmentState currentState)
        {
            if (AllowedTransitions.TryGetValue(currentState, out var validTargets))
            {
                return new List<EquipmentState>(validTargets).AsReadOnly();
            }
            return new List<EquipmentState>().AsReadOnly();
        }

        /// <summary>
        /// Validates a state transition and returns a descriptive <see cref="StateTransitionResult"/>.
        /// </summary>
        public static StateTransitionResult ValidateAndTransition(EquipmentState currentState, EquipmentState targetState)
        {
            if (currentState == targetState)
            {
                return StateTransitionResult.Failure(
                    currentState,
                    targetState,
                    $"Equipment is already in the '{currentState}' state.");
            }

            if (CanTransition(currentState, targetState))
            {
                return StateTransitionResult.Success(currentState, targetState);
            }

            return StateTransitionResult.Failure(
                currentState,
                targetState,
                $"Invalid state transition: Cannot transition from '{currentState}' to '{targetState}'.");
        }
    }
}
