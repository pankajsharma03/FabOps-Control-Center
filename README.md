# FabOps Control Center

Production-style semiconductor manufacturing operations and equipment simulation system.

## System Architecture & Projects

- **`EquipmentSimulator.Core`**: Domain layer class library (.NET Framework 4.8.1). Houses equipment entities (`Equipment`), operational states (`EquipmentState`), decoupled connection status (`ConnectionState`), Result-pattern based state transition validation (`StateTransitionMatrix`), and domain events.
- **`EquipmentSimulator.Core.Tests`**: Automated unit test suite (.NET Framework 4.8.1 / MSTest) providing comprehensive coverage over all valid transitions, rejected illegal transitions, domain event notifications, and entity state invariants.
- **`EquipmentSimulator.Desktop`**: WPF / MVVM desktop application providing real-time equipment telemetry, state transition controls with dynamic `CanExecute` enforcement, and live chronological state change audit logging.

## Current Status

### Milestone 1 — Project Foundation (Completed)
- Visual Studio solution created
- Repository structure established
- Target framework: .NET Framework 4.8.1

### Milestone 2 — MVVM Foundation (Completed)
- Presentation separation with `ViewModelBase` (`INotifyPropertyChanged`)
- `RelayCommand` implementation for UI action boundaries
- Initial UI DataContext bindings

### Milestone 3 — Equipment Domain + State Modeling (Completed)
- Clean Architecture domain separation (`EquipmentSimulator.Core`)
- SEMI E30 (GEM) aligned state modeling (`Offline`, `Idle`, `Setup`, `Executing`, `Paused`, `Alarm`, `Maintenance`)
- Deterministic state transition validation matrix with `StateTransitionResult` pattern
- Decoupled operational state from transport connection state (`Disconnected`, `Connecting`, `Connected`)
- 46 automated unit tests verifying all state paths and domain invariants
- Rich, modern equipment simulator UI with telemetry cards, dynamic state controls, and event audit trail

### Milestone 4 — Equipment State Machine / Brain (Next)
- Autonomous execution loop and recipe process step simulation
- Live simulated telemetry (chamber vacuum pressure, temperature, RF power)
- Background timer sequencing and automatic fault trigger simulation

## Planned Communication Architecture

```
Equipment Simulator (WPF + Core) ↔ TCP/IP ↔ XML ↔ FabOps Operations (WPF) ↔ HTTPS/REST ↔ ASP.NET Core API ↔ Oracle Database
```

Future protocol evolution will integrate SEMI standards concepts (SEMI E5 SECS-II, E30 GEM, E37 HSMS).

## Technical & Engineering Scope

- **Desktop**: C#, .NET Framework 4.8.1, WPF, XAML, MVVM, Data Binding, Commands, Clean Architecture
- **Testing**: MSTest, xUnit, Unit Test Coverage of Domain Invariants
- **Standards & Domain**: SEMI E30 GEM, Semiconductor Fab Operations, Recipes, Lots, Alarms, OCAP Workflows
- **Backend & DB (Upcoming)**: ASP.NET Core Web API, REST, Structured Logging, Oracle Database, SQL, ODP.NET
