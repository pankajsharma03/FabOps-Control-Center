using System.Linq;
using EquipmentSimulator.Core.Models;
using EquipmentSimulator.Core.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EquipmentSimulator.Core.Tests
{
    [TestClass]
    public class StateTransitionMatrixTests
    {
        [TestMethod]
        [DataRow(EquipmentState.Offline, EquipmentState.Idle)]
        [DataRow(EquipmentState.Idle, EquipmentState.Setup)]
        [DataRow(EquipmentState.Idle, EquipmentState.Maintenance)]
        [DataRow(EquipmentState.Idle, EquipmentState.Offline)]
        [DataRow(EquipmentState.Idle, EquipmentState.Alarm)]
        [DataRow(EquipmentState.Setup, EquipmentState.Executing)]
        [DataRow(EquipmentState.Setup, EquipmentState.Idle)]
        [DataRow(EquipmentState.Setup, EquipmentState.Alarm)]
        [DataRow(EquipmentState.Executing, EquipmentState.Paused)]
        [DataRow(EquipmentState.Executing, EquipmentState.Idle)]
        [DataRow(EquipmentState.Executing, EquipmentState.Alarm)]
        [DataRow(EquipmentState.Paused, EquipmentState.Executing)]
        [DataRow(EquipmentState.Paused, EquipmentState.Idle)]
        [DataRow(EquipmentState.Paused, EquipmentState.Alarm)]
        [DataRow(EquipmentState.Alarm, EquipmentState.Idle)]
        [DataRow(EquipmentState.Alarm, EquipmentState.Maintenance)]
        [DataRow(EquipmentState.Maintenance, EquipmentState.Idle)]
        [DataRow(EquipmentState.Maintenance, EquipmentState.Alarm)]
        [DataRow(EquipmentState.Maintenance, EquipmentState.Offline)]
        public void CanTransition_ValidTransitions_ReturnsTrue(EquipmentState from, EquipmentState to)
        {
            // Act
            bool canTransition = StateTransitionMatrix.CanTransition(from, to);

            // Assert
            Assert.IsTrue(canTransition, $"Expected transition from {from} to {to} to be permitted.");
        }

        [TestMethod]
        [DataRow(EquipmentState.Offline, EquipmentState.Executing)]
        [DataRow(EquipmentState.Offline, EquipmentState.Setup)]
        [DataRow(EquipmentState.Offline, EquipmentState.Paused)]
        [DataRow(EquipmentState.Offline, EquipmentState.Maintenance)]
        [DataRow(EquipmentState.Idle, EquipmentState.Paused)]
        [DataRow(EquipmentState.Idle, EquipmentState.Executing)]
        [DataRow(EquipmentState.Setup, EquipmentState.Paused)]
        [DataRow(EquipmentState.Setup, EquipmentState.Maintenance)]
        [DataRow(EquipmentState.Executing, EquipmentState.Setup)]
        [DataRow(EquipmentState.Executing, EquipmentState.Offline)]
        [DataRow(EquipmentState.Executing, EquipmentState.Maintenance)]
        [DataRow(EquipmentState.Paused, EquipmentState.Setup)]
        [DataRow(EquipmentState.Paused, EquipmentState.Offline)]
        [DataRow(EquipmentState.Alarm, EquipmentState.Executing)]
        [DataRow(EquipmentState.Alarm, EquipmentState.Setup)]
        [DataRow(EquipmentState.Alarm, EquipmentState.Paused)]
        public void CanTransition_InvalidTransitions_ReturnsFalse(EquipmentState from, EquipmentState to)
        {
            // Act
            bool canTransition = StateTransitionMatrix.CanTransition(from, to);

            // Assert
            Assert.IsFalse(canTransition, $"Expected transition from {from} to {to} to be rejected.");
        }

        [TestMethod]
        public void ValidateAndTransition_SameState_ReturnsFailure()
        {
            // Act
            var result = StateTransitionMatrix.ValidateAndTransition(EquipmentState.Idle, EquipmentState.Idle);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("already in the 'Idle' state"));
        }

        [TestMethod]
        public void ValidateAndTransition_ValidTarget_ReturnsSuccess()
        {
            // Act
            var result = StateTransitionMatrix.ValidateAndTransition(EquipmentState.Idle, EquipmentState.Setup);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(EquipmentState.Idle, result.FromState);
            Assert.AreEqual(EquipmentState.Setup, result.ToState);
        }

        [TestMethod]
        public void ValidateAndTransition_InvalidTarget_ReturnsFailureWithDescription()
        {
            // Act
            var result = StateTransitionMatrix.ValidateAndTransition(EquipmentState.Offline, EquipmentState.Executing);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("Cannot transition from 'Offline' to 'Executing'"));
        }

        [TestMethod]
        public void GetAllowedTransitions_Idle_ReturnsExpectedStates()
        {
            // Act
            var targets = StateTransitionMatrix.GetAllowedTransitions(EquipmentState.Idle);

            // Assert
            Assert.AreEqual(4, targets.Count);
            CollectionAssert.Contains(targets.ToList(), EquipmentState.Setup);
            CollectionAssert.Contains(targets.ToList(), EquipmentState.Maintenance);
            CollectionAssert.Contains(targets.ToList(), EquipmentState.Offline);
            CollectionAssert.Contains(targets.ToList(), EquipmentState.Alarm);
        }
    }
}
