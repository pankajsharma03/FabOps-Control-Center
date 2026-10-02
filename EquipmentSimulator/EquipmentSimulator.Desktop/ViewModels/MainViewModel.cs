using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using EquipmentSimulator.Core.Events;
using EquipmentSimulator.Core.Models;
using EquipmentSimulator.Desktop.Commands;
using DomainState = EquipmentSimulator.Core.Models.EquipmentState;
using DomainConnectionState = EquipmentSimulator.Core.Models.ConnectionState;

namespace EquipmentSimulator.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for the Equipment Simulator Main Window.
    /// Acts as the presentation adapter for the underlying <see cref="Equipment"/> domain model.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly Equipment _equipment;
        private string _statusMessage;
        private string _recipeInput;
        private string _lotInput;

        public MainViewModel()
        {
            // Initialize Domain Model (Clean Architecture: presentation adapts domain)
            _equipment = new Equipment("EQ-LITH-001", "Litho-Scanner-300mm", "SN-LITH-2026-001");
            _equipment.StateChanged += OnEquipmentStateChanged;
            _equipment.ConnectionStatusChanged += OnConnectionStatusChanged;

            AuditLogs = new ObservableCollection<string>();
            RecipeInput = "RECIPE-POLY-GATE-N5";
            LotInput = "LOT-2026-W09-A";

            // State Transition Commands with CanExecute rules backed by domain logic
            PowerOnCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Idle, "System boot & diagnostic initialization"),
                () => _equipment.CanTransitionTo(DomainState.Idle));

            PowerOffCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Offline, "Controlled shutdown sequence"),
                () => _equipment.CanTransitionTo(DomainState.Offline));

            SetupCommand = new RelayCommand(
                () =>
                {
                    _equipment.LoadRecipeAndLot(RecipeInput, LotInput);
                    OnPropertyChanged(nameof(ActiveRecipeId));
                    OnPropertyChanged(nameof(ActiveLotId));
                    ExecuteTransition(DomainState.Setup, $"Prepared recipe '{RecipeInput}' for lot '{LotInput}'");
                },
                () => _equipment.CanTransitionTo(DomainState.Setup));

            StartRunCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Executing, $"Exposure run started for lot '{_equipment.ActiveLotId}'"),
                () => _equipment.CanTransitionTo(DomainState.Executing));

            PauseCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Paused, "Operator hold / chamber stabilization pause"),
                () => _equipment.CanTransitionTo(DomainState.Paused));

            ResumeCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Executing, "Resumed processing after hold"),
                () => _equipment.CanTransitionTo(DomainState.Executing) && _equipment.CurrentState == DomainState.Paused);

            StopCommand = new RelayCommand(
                () =>
                {
                    _equipment.ClearRecipeAndLot();
                    OnPropertyChanged(nameof(ActiveRecipeId));
                    OnPropertyChanged(nameof(ActiveLotId));
                    ExecuteTransition(DomainState.Idle, "Run completed / process cycle returned to idle");
                },
                () => _equipment.CanTransitionTo(DomainState.Idle) && _equipment.CurrentState != DomainState.Offline && _equipment.CurrentState != DomainState.Alarm);

            TripAlarmCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Alarm, "Chamber Vacuum Pressure Deviation [ALARM-302]"),
                () => _equipment.CanTransitionTo(DomainState.Alarm));

            ClearAlarmCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Idle, "Alarm acknowledged, pressure interlock normalized"),
                () => _equipment.CanTransitionTo(DomainState.Idle) && _equipment.CurrentState == DomainState.Alarm);

            MaintenanceCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Maintenance, "Scheduled preventative maintenance & sensor calibration"),
                () => _equipment.CanTransitionTo(DomainState.Maintenance));

            // Transport Connection Commands (Decoupled from operational state)
            ConnectCommand = new RelayCommand(
                () => _equipment.SetConnectionStatus(DomainConnectionState.Connected),
                () => _equipment.ConnectionStatus != DomainConnectionState.Connected);

            DisconnectCommand = new RelayCommand(
                () => _equipment.SetConnectionStatus(DomainConnectionState.Disconnected),
                () => _equipment.ConnectionStatus != DomainConnectionState.Disconnected);

            StatusMessage = "Equipment initialized in Offline state. Ready for startup.";
            AddAuditLog($"System initialized. Equipment [{_equipment.EquipmentId}] ready.");
        }

        #region Domain Bound Properties

        public string EquipmentId => _equipment.EquipmentId;

        public string ModelType => _equipment.ModelType;

        public string SerialNumber => _equipment.SerialNumber;

        public string EquipmentState => _equipment.CurrentState.ToString().ToUpperInvariant();

        public DomainState CurrentEquipmentState => _equipment.CurrentState;

        public string ConnectionStatus => _equipment.ConnectionStatus.ToString();

        public DomainConnectionState CurrentConnectionStatus => _equipment.ConnectionStatus;

        public string LastStateChangeTime => _equipment.LastStateChangeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        public string ActiveRecipeId => string.IsNullOrEmpty(_equipment.ActiveRecipeId) ? "(None)" : _equipment.ActiveRecipeId;

        public string ActiveLotId => string.IsNullOrEmpty(_equipment.ActiveLotId) ? "(None)" : _equipment.ActiveLotId;

        public string LastAlarmMessage => string.IsNullOrEmpty(_equipment.LastAlarmMessage) ? "None (Normal Operation)" : _equipment.LastAlarmMessage;

        public bool HasAlarm => _equipment.CurrentState == DomainState.Alarm;

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string RecipeInput
        {
            get => _recipeInput;
            set
            {
                if (_recipeInput == value) return;
                _recipeInput = value;
                OnPropertyChanged();
            }
        }

        public string LotInput
        {
            get => _lotInput;
            set
            {
                if (_lotInput == value) return;
                _lotInput = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> AuditLogs { get; }

        #endregion

        #region Commands

        public ICommand PowerOnCommand { get; }
        public ICommand PowerOffCommand { get; }
        public ICommand SetupCommand { get; }
        public ICommand StartRunCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand TripAlarmCommand { get; }
        public ICommand ClearAlarmCommand { get; }
        public ICommand MaintenanceCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        #endregion

        #region Helper Methods

        private void ExecuteTransition(DomainState targetState, string reason)
        {
            var result = _equipment.TransitionTo(targetState, reason);
            if (result.IsSuccess)
            {
                StatusMessage = $"State transitioned to {targetState}. {reason}";
            }
            else
            {
                StatusMessage = $"Transition failed: {result.Message}";
            }
        }

        private void OnEquipmentStateChanged(object sender, EquipmentStateChangedEventArgs e)
        {
            OnPropertyChanged(nameof(EquipmentState));
            OnPropertyChanged(nameof(CurrentEquipmentState));
            OnPropertyChanged(nameof(LastStateChangeTime));
            OnPropertyChanged(nameof(LastAlarmMessage));
            OnPropertyChanged(nameof(HasAlarm));

            AddAuditLog(e.ToString());
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnConnectionStatusChanged(object sender, DomainConnectionState status)
        {
            OnPropertyChanged(nameof(ConnectionStatus));
            OnPropertyChanged(nameof(CurrentConnectionStatus));

            AddAuditLog($"[{DateTime.Now:HH:mm:ss.fff}] Host connection changed to '{status}'.");
            CommandManager.InvalidateRequerySuggested();
        }

        private void AddAuditLog(string message)
        {
            AuditLogs.Insert(0, message);
            if (AuditLogs.Count > 50)
            {
                AuditLogs.RemoveAt(AuditLogs.Count - 1);
            }
        }

        #endregion
    }
}