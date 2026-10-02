using System;
using EquipmentSimulator.Core.Events;
using EquipmentSimulator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EquipmentSimulator.Core.Tests
{
    [TestClass]
    public class EquipmentEntityTests
    {
        [TestMethod]
        public void Constructor_InitializesDefaultStateAndConnection()
        {
            // Act
            var eq = new Equipment("EQ-LITH-01", "Litho-Scanner-300mm", "SN-12345");

            // Assert
            Assert.AreEqual("EQ-LITH-01", eq.EquipmentId);
            Assert.AreEqual("Litho-Scanner-300mm", eq.ModelType);
            Assert.AreEqual("SN-12345", eq.SerialNumber);
            Assert.AreEqual(EquipmentState.Offline, eq.CurrentState);
            Assert.AreEqual(ConnectionState.Disconnected, eq.ConnectionStatus);
            Assert.AreEqual(string.Empty, eq.ActiveRecipeId);
            Assert.AreEqual(string.Empty, eq.ActiveLotId);
            Assert.AreEqual(string.Empty, eq.LastAlarmMessage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_EmptyId_ThrowsArgumentException()
        {
            // Act
            new Equipment("   ");
        }

        [TestMethod]
        public void TransitionTo_ValidTransition_UpdatesStateAndFiresEvent()
        {
            // Arrange
            var eq = new Equipment("EQ-01");
            EquipmentStateChangedEventArgs eventPayload = null;
            eq.StateChanged += (sender, e) => eventPayload = e;

            // Act
            var result = eq.TransitionTo(EquipmentState.Idle, "System startup");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(EquipmentState.Idle, eq.CurrentState);
            Assert.IsNotNull(eventPayload);
            Assert.AreEqual(EquipmentState.Offline, eventPayload.PreviousState);
            Assert.AreEqual(EquipmentState.Idle, eventPayload.NewState);
            Assert.AreEqual("System startup", eventPayload.Reason);
        }

        [TestMethod]
        public void TransitionTo_InvalidTransition_DoesNotChangeStateOrFireEvent()
        {
            // Arrange
            var eq = new Equipment("EQ-01");
            bool eventFired = false;
            eq.StateChanged += (sender, e) => eventFired = true;

            // Act: Attempt Offline -> Executing (illegal)
            var result = eq.TransitionTo(EquipmentState.Executing);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(EquipmentState.Offline, eq.CurrentState);
            Assert.IsFalse(eventFired);
        }

        [TestMethod]
        public void TransitionTo_Alarm_StoresAlarmMessage_AndClearingRestoresIdle()
        {
            // Arrange
            var eq = new Equipment("EQ-01");
            eq.TransitionTo(EquipmentState.Idle);

            // Act 1: Trip alarm
            var alarmResult = eq.TransitionTo(EquipmentState.Alarm, "Chamber Vacuum Pressure Limit Exceeded");

            // Assert 1
            Assert.IsTrue(alarmResult.IsSuccess);
            Assert.AreEqual(EquipmentState.Alarm, eq.CurrentState);
            Assert.AreEqual("Chamber Vacuum Pressure Limit Exceeded", eq.LastAlarmMessage);

            // Act 2: Clear alarm back to Idle
            var clearResult = eq.TransitionTo(EquipmentState.Idle, "Alarm acknowledged and cleared by operator");

            // Assert 2
            Assert.IsTrue(clearResult.IsSuccess);
            Assert.AreEqual(EquipmentState.Idle, eq.CurrentState);
            Assert.AreEqual(string.Empty, eq.LastAlarmMessage);
        }

        [TestMethod]
        public void SetConnectionStatus_UpdatesStatus_AndFiresConnectionStatusChangedEvent()
        {
            // Arrange
            var eq = new Equipment("EQ-01");
            ConnectionState newStatus = ConnectionState.Disconnected;
            eq.ConnectionStatusChanged += (sender, status) => newStatus = status;

            // Act
            eq.SetConnectionStatus(ConnectionState.Connected);

            // Assert
            Assert.AreEqual(ConnectionState.Connected, eq.ConnectionStatus);
            Assert.AreEqual(ConnectionState.Connected, newStatus);
        }

        [TestMethod]
        public void LoadRecipeAndLot_SetsProperties_AndClearResetsThem()
        {
            // Arrange
            var eq = new Equipment("EQ-01");

            // Act 1: Load
            eq.LoadRecipeAndLot("RECIPE-POLY-ETCH-300", "LOT-998241");

            // Assert 1
            Assert.AreEqual("RECIPE-POLY-ETCH-300", eq.ActiveRecipeId);
            Assert.AreEqual("LOT-998241", eq.ActiveLotId);

            // Act 2: Clear
            eq.ClearRecipeAndLot();

            // Assert 2
            Assert.AreEqual(string.Empty, eq.ActiveRecipeId);
            Assert.AreEqual(string.Empty, eq.ActiveLotId);
        }
    }
}
