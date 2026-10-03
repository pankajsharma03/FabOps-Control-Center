using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EquipmentSimulator.Core.Engine;
using EquipmentSimulator.Core.Events;
using EquipmentSimulator.Core.Models;
using EquipmentSimulator.Desktop.Commands;
using DomainState = EquipmentSimulator.Core.Models.EquipmentState;
using DomainConnectionState = EquipmentSimulator.Core.Models.ConnectionState;

namespace EquipmentSimulator.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel for the Equipment Simulator Main Window.
    /// Acts as the presentation adapter for the underlying <see cref="Equipment"/> domain model
    /// and the <see cref="ProcessExecutionEngine"/> physics simulation brain.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly Equipment _equipment;
        private readonly ISimulationEngine _engine;

        private string _statusMessage;
        private string _recipeInput;
        private string _lotInput;
        private int _totalWafersInput = 25;
        private Recipe _selectedRecipe;
        private double _speedMultiplier = 1.0;

        // Live Telemetry Cache
        private TelemetrySnapshot _telemetry;

        public MainViewModel()
        {
            // Initialize Domain Model & Simulation Engine
            _equipment = new Equipment("EQ-LITH-001", "Litho-Scanner-300mm", "SN-LITH-2026-001");
            _engine = new ProcessExecutionEngine(_equipment);

            _equipment.StateChanged += OnEquipmentStateChanged;
            _equipment.ConnectionStatusChanged += OnConnectionStatusChanged;

            _engine.TelemetryUpdated += OnTelemetryUpdated;
            _engine.StepChanged += OnProcessStepChanged;
            _engine.WaferCompleted += OnWaferCompleted;
            _engine.LotCompleted += OnLotCompleted;
            _engine.FaultTriggered += OnFaultTriggered;

            AuditLogs = new ObservableCollection<string>();
            AvailableRecipes = new List<Recipe>
            {
                Recipe.CreateStandardLithographyRecipe(),
                Recipe.CreateEuvHighNumericalApertureRecipe(),
                Recipe.CreateDeepReactiveIonEtchRecipe()
            };

            SelectedRecipe = AvailableRecipes.First();
            LotInput = "LOT-2026-W09-A";
            _telemetry = TelemetrySnapshot.CreateStandby();

            // Commands Initialization
            PowerOnCommand = new RelayCommand(
                () => ExecuteTransition(DomainState.Idle, "System boot & diagnostic initialization"),
                () => _equipment.CanTransitionTo(DomainState.Idle) && !_engine.IsRunning);

            PowerOffCommand = new RelayCommand(
                () =>
                {
                    if (_engine.IsRunning) _engine.Stop();
                    ExecuteTransition(DomainState.Offline, "Controlled shutdown sequence");
                },
                () => _equipment.CanTransitionTo(DomainState.Offline) && !_engine.IsRunning);

            SetupCommand = new RelayCommand(
                () =>
                {
                    _equipment.LoadRecipeAndLot(RecipeInput, LotInput);
                    OnPropertyChanged(nameof(ActiveRecipeId));
                    OnPropertyChanged(nameof(ActiveLotId));
                    ExecuteTransition(DomainState.Setup, $"Prepared recipe '{RecipeInput}' for lot '{LotInput}'");
                },
                () => _equipment.CanTransitionTo(DomainState.Setup) && !_engine.IsRunning);

            StartRunCommand = new RelayCommand(
                async () => await StartProcessExecutionAsync(),
                () => (_equipment.CanTransitionTo(DomainState.Executing) || _equipment.CurrentState == DomainState.Idle || _equipment.CurrentState == DomainState.Setup)
                      && !_engine.IsRunning
                      && _equipment.CurrentState != DomainState.Offline
                      && _equipment.CurrentState != DomainState.Alarm);

            PauseCommand = new RelayCommand(
                () => _engine.Pause(),
                () => _engine.IsRunning && !_engine.IsPaused);

            ResumeCommand = new RelayCommand(
                () => _engine.Resume(),
                () => _engine.IsRunning && _engine.IsPaused);

            StopCommand = new RelayCommand(
                () =>
                {
                    _engine.Stop();
                    OnPropertyChanged(nameof(ActiveRecipeId));
                    OnPropertyChanged(nameof(ActiveLotId));
                },
                () => _engine.IsRunning || (_equipment.CanTransitionTo(DomainState.Idle) && _equipment.CurrentState != DomainState.Offline && _equipment.CurrentState != DomainState.Alarm));

            TripAlarmCommand = new RelayCommand(
                () => _engine.InjectFault(SimulationFaultType.VacuumLeak),
                () => _equipment.CurrentState != DomainState.Alarm && _equipment.CurrentState != DomainState.Offline);

            ClearAlarmCommand = new RelayCommand(
                () =>
                {
                    _engine.ResetToStandby();
                    ExecuteTransition(DomainState.Idle, "Alarm acknowledged, interlocks cleared, restored to idle");
                },
                () => _equipment.CanTransitionTo(DomainState.Idle) && _equipment.CurrentState == DomainState.Alarm);

            MaintenanceCommand = new RelayCommand(
                () =>
                {
                    if (_engine.IsRunning) _engine.Stop();
                    ExecuteTransition(DomainState.Maintenance, "Scheduled preventative maintenance & calibration");
                },
                () => _equipment.CanTransitionTo(DomainState.Maintenance) && !_engine.IsRunning);

            ConnectCommand = new RelayCommand(
                () => _equipment.SetConnectionStatus(DomainConnectionState.Connected),
                () => _equipment.ConnectionStatus != DomainConnectionState.Connected);

            DisconnectCommand = new RelayCommand(
                () => _equipment.SetConnectionStatus(DomainConnectionState.Disconnected),
                () => _equipment.ConnectionStatus != DomainConnectionState.Disconnected);

            // Fault Injection Specific Commands
            InjectVacuumLeakCommand = new RelayCommand(
                () => _engine.InjectFault(SimulationFaultType.VacuumLeak),
                () => _equipment.CurrentState != DomainState.Alarm && _equipment.CurrentState != DomainState.Offline);

            InjectThermalExcursionCommand = new RelayCommand(
                () => _engine.InjectFault(SimulationFaultType.ThermalExcursion),
                () => _equipment.CurrentState != DomainState.Alarm && _equipment.CurrentState != DomainState.Offline);

            InjectLaserFaultCommand = new RelayCommand(
                () => _engine.InjectFault(SimulationFaultType.LaserUniformityDegradation),
                () => _equipment.CurrentState != DomainState.Alarm && _equipment.CurrentState != DomainState.Offline);

            InjectWaferTransferErrorCommand = new RelayCommand(
                () => _engine.InjectFault(SimulationFaultType.WaferTransferError),
                () => _equipment.CurrentState != DomainState.Alarm && _equipment.CurrentState != DomainState.Offline);

            // Simulation Speed Multiplier Command
            SetSpeedCommand = new RelayCommand(
                param =>
                {
                    if (double.TryParse(param?.ToString(), out var speed))
                    {
                        SpeedMultiplier = speed;
                    }
                });

            StatusMessage = "Equipment initialized in Offline state. Ready for startup.";
            AddAuditLog($"System initialized. Equipment [{_equipment.EquipmentId}] ready.");
        }

        #region Domain & Simulation Bound Properties

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

        public int TotalWafersInput
        {
            get => _totalWafersInput;
            set
            {
                if (_totalWafersInput == value) return;
                _totalWafersInput = Math.Max(1, Math.Min(100, value));
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<Recipe> AvailableRecipes { get; }

        public Recipe SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (_selectedRecipe == value) return;
                _selectedRecipe = value;
                if (_selectedRecipe != null)
                {
                    RecipeInput = _selectedRecipe.RecipeId;
                    TotalWafersInput = _selectedRecipe.DefaultWafersPerLot;
                }
                OnPropertyChanged();
            }
        }

        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set
            {
                if (Math.Abs(_speedMultiplier - value) < 0.001) return;
                _speedMultiplier = value;
                _engine.SetSpeedMultiplier(value);
                OnPropertyChanged();
                AddAuditLog($"Simulation speed multiplier set to {value:F0}x.");
            }
        }

        public ObservableCollection<string> AuditLogs { get; }

        #endregion

        #region Telemetry Readouts

        public string VacuumPressureFormatted => _telemetry.PressureFormatted;
        public string ChamberTemperatureFormatted => _telemetry.TemperatureFormatted;
        public string LaserDoseFormatted => _telemetry.LaserDoseFormatted;
        public string VacuumPhase => _telemetry.VacuumPhase;
        public bool IsLaserActive => _telemetry.IsLaserActive;
        public bool IsPumpingActive => _telemetry.IsPumpingActive;
        public bool IsVentingActive => _telemetry.IsVentingActive;
        public string CurrentStepName => _telemetry.CurrentStepName;
        public ProcessStepType CurrentStepType => _telemetry.CurrentStep;
        public int CurrentStepIndex => _telemetry.CurrentStepIndex;
        public int TotalSteps => _telemetry.TotalSteps;
        public double StepProgressPercent => _telemetry.StepProgressPercent;
        public double LotProgressPercent => _telemetry.OverallLotProgressPercent;
        public string WaferStatusFormatted => _telemetry.WaferStatusFormatted;
        public bool IsSimulationRunning => _engine.IsRunning;
        public bool IsSimulationPaused => _engine.IsPaused;

        // Step active indicators for UI breadcrumbs
        public bool IsStep1Active => _engine.IsRunning && _telemetry.CurrentStep == ProcessStepType.WaferLoad;
        public bool IsStep2Active => _engine.IsRunning && _telemetry.CurrentStep == ProcessStepType.PumpDown;
        public bool IsStep3Active => _engine.IsRunning && _telemetry.CurrentStep == ProcessStepType.Exposure;
        public bool IsStep4Active => _engine.IsRunning && _telemetry.CurrentStep == ProcessStepType.ChamberVent;
        public bool IsStep5Active => _engine.IsRunning && _telemetry.CurrentStep == ProcessStepType.WaferUnload;

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

        // Fault injection
        public ICommand InjectVacuumLeakCommand { get; }
        public ICommand InjectThermalExcursionCommand { get; }
        public ICommand InjectLaserFaultCommand { get; }
        public ICommand InjectWaferTransferErrorCommand { get; }

        // Speed control
        public ICommand SetSpeedCommand { get; }

        #endregion

        #region Helper & Event Handlers

        private async Task StartProcessExecutionAsync()
        {
            try
            {
                var recipe = SelectedRecipe ?? Recipe.CreateStandardLithographyRecipe();
                var lotId = string.IsNullOrWhiteSpace(LotInput) ? "LOT-2026-RUN-01" : LotInput.Trim();
                var totalWafers = TotalWafersInput > 0 ? TotalWafersInput : 25;

                StatusMessage = $"Starting autonomous execution: Recipe '{recipe.RecipeId}' ({totalWafers} wafers)...";
                await _engine.StartAsync(recipe, lotId, totalWafers);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Execution error: {ex.Message}";
                AddAuditLog($"[ERROR] Start failed: {ex.Message}");
            }
        }

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

        private void OnTelemetryUpdated(object sender, TelemetryUpdatedEventArgs e)
        {
            RunOnUi(() =>
            {
                _telemetry = e.Telemetry;

                OnPropertyChanged(nameof(VacuumPressureFormatted));
                OnPropertyChanged(nameof(ChamberTemperatureFormatted));
                OnPropertyChanged(nameof(LaserDoseFormatted));
                OnPropertyChanged(nameof(VacuumPhase));
                OnPropertyChanged(nameof(IsLaserActive));
                OnPropertyChanged(nameof(IsPumpingActive));
                OnPropertyChanged(nameof(IsVentingActive));
                OnPropertyChanged(nameof(CurrentStepName));
                OnPropertyChanged(nameof(CurrentStepType));
                OnPropertyChanged(nameof(CurrentStepIndex));
                OnPropertyChanged(nameof(StepProgressPercent));
                OnPropertyChanged(nameof(LotProgressPercent));
                OnPropertyChanged(nameof(WaferStatusFormatted));
                OnPropertyChanged(nameof(IsSimulationRunning));
                OnPropertyChanged(nameof(IsSimulationPaused));

                OnPropertyChanged(nameof(IsStep1Active));
                OnPropertyChanged(nameof(IsStep2Active));
                OnPropertyChanged(nameof(IsStep3Active));
                OnPropertyChanged(nameof(IsStep4Active));
                OnPropertyChanged(nameof(IsStep5Active));
            });
        }

        private void OnProcessStepChanged(object sender, ProcessStepChangedEventArgs e)
        {
            RunOnUi(() =>
            {
                AddAuditLog($"[STEP {(int)e.NewStep}] Wafer {e.WaferNumber}/{e.TotalWafers} -> {e.StepName}");
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void OnWaferCompleted(object sender, WaferProcessedEventArgs e)
        {
            RunOnUi(() =>
            {
                AddAuditLog($"[WAFER DONE] Wafer {e.WaferNumber}/{e.TotalWafers} finished in {e.Duration.TotalSeconds:F1}s.");
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void OnLotCompleted(object sender, LotCompletedEventArgs e)
        {
            RunOnUi(() =>
            {
                StatusMessage = $"Lot '{e.LotId}' completed! {e.TotalWafersProcessed} wafers processed in {e.TotalElapsedTime.TotalSeconds:F1}s.";
                AddAuditLog($"[LOT COMPLETED] Lot '{e.LotId}' successfully finished ({e.TotalWafersProcessed} wafers, {e.TotalElapsedTime.TotalSeconds:F1}s).");
                OnPropertyChanged(nameof(ActiveRecipeId));
                OnPropertyChanged(nameof(ActiveLotId));
                OnPropertyChanged(nameof(IsSimulationRunning));
                OnPropertyChanged(nameof(IsSimulationPaused));
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void OnFaultTriggered(object sender, FaultInjectedEventArgs e)
        {
            RunOnUi(() =>
            {
                StatusMessage = $"FAULT TRIPPED: [{e.AlarmCode}] {e.Message}";
                AddAuditLog($"[FAULT TRIPPED] [{e.AlarmCode}] {e.Message}");
                OnPropertyChanged(nameof(EquipmentState));
                OnPropertyChanged(nameof(CurrentEquipmentState));
                OnPropertyChanged(nameof(HasAlarm));
                OnPropertyChanged(nameof(LastAlarmMessage));
                OnPropertyChanged(nameof(IsSimulationRunning));
                OnPropertyChanged(nameof(IsSimulationPaused));
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void OnEquipmentStateChanged(object sender, EquipmentStateChangedEventArgs e)
        {
            RunOnUi(() =>
            {
                OnPropertyChanged(nameof(EquipmentState));
                OnPropertyChanged(nameof(CurrentEquipmentState));
                OnPropertyChanged(nameof(LastStateChangeTime));
                OnPropertyChanged(nameof(LastAlarmMessage));
                OnPropertyChanged(nameof(HasAlarm));
                OnPropertyChanged(nameof(ActiveRecipeId));
                OnPropertyChanged(nameof(ActiveLotId));
                OnPropertyChanged(nameof(IsSimulationRunning));
                OnPropertyChanged(nameof(IsSimulationPaused));

                AddAuditLog(e.ToString());
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void OnConnectionStatusChanged(object sender, DomainConnectionState status)
        {
            RunOnUi(() =>
            {
                OnPropertyChanged(nameof(ConnectionStatus));
                OnPropertyChanged(nameof(CurrentConnectionStatus));

                AddAuditLog($"[{DateTime.Now:HH:mm:ss.fff}] Host connection changed to '{status}'.");
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void AddAuditLog(string message)
        {
            RunOnUi(() =>
            {
                AuditLogs.Insert(0, message);
                if (AuditLogs.Count > 100)
                {
                    AuditLogs.RemoveAt(AuditLogs.Count - 1);
                }
            });
        }

        private static void RunOnUi(Action action)
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(action);
            }
            else
            {
                action?.Invoke();
            }
        }

        #endregion
    }
}