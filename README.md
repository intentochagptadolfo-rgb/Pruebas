# Hyundai Transys — Vision Inspection Orchestrator

Mission-critical software that orchestrates communication between the plant MES and the Keyence IV4-500CA vision camera on a Hyundai Transys production line. Runs on a Lenovo ThinkCentre PC (Windows 10/11 x64).

## Solution overview

```
HyundaiTransys.VisionInspection.sln
├── src/
│   ├── HyundaiTransys.VisionInspection.Core/            ← Pure domain. No deps.
│   │   ├── Abstractions/          ← IMesClient, IKeyenceClient, IJobResolver, IInspectionRepository, IImageStorage, IConfigurationService, IUserService
│   │   ├── Domain/
│   │   │   ├── Entities/          ← InspectionRecord, User, JobMapping, ModelDefinition
│   │   │   ├── Enums/             ← InspectionResult, SystemState, UserRole
│   │   │   └── ValueObjects/      ← MesFrame, JobId, ModelCode
│   │   ├── Configuration/         ← AppSettings + POCOs (MesOptions, KeyenceOptions, …)
│   │   └── Messaging/             ← MesMessage, KeyenceCommand, KeyenceResponse
│   │
│   ├── HyundaiTransys.VisionInspection.Infrastructure/  ← Adapters (TCP, EF, FS, Auth)
│   │   ├── Mes/                   ← MesTcpClient (watchdog + auto-reconnect), MesMessageParser
│   │   ├── Keyence/               ← KeyenceTcpClient, KeyenceProtocol (JOB change, trigger, parse result)
│   │   ├── Persistence/           ← InspectionDbContext (EF Core), repositories
│   │   ├── Storage/               ← LocalImageStorage (retention policy)
│   │   ├── Configuration/         ← JsonConfigurationService (encrypted at rest with DPAPI)
│   │   ├── Security/              ← PasswordHasher (PBKDF2), RoleAuthorizer
│   │   └── Logging/               ← Serilog sinks setup
│   │
│   ├── HyundaiTransys.VisionInspection.Application/     ← Use-cases + orchestration
│   │   ├── StateMachine/          ← InspectionOrchestrator (Stateless-based FSM)
│   │   ├── Services/              ← JobResolver, UserService, RetentionService
│   │   └── DTOs/
│   │
│   └── HyundaiTransys.VisionInspection.UI/              ← WPF + MVVM (.NET 8)
│       ├── App.xaml(.cs)          ← DI composition root (Microsoft.Extensions.Hosting)
│       ├── Views/                 ← MainView, ConfigurationView, LoginView, AlertOverlay
│       ├── ViewModels/            ← MainViewModel, ConfigurationViewModel, LoginViewModel (+ Base/ViewModelBase)
│       ├── Services/              ← DialogService, NavigationService, UiDispatcher
│       └── Converters/            ← ResultToColorConverter, BoolToVisibilityConverter
│
├── tests/
│   ├── …Core.Tests/               ← xUnit + FluentAssertions
│   ├── …Infrastructure.Tests/     ← Testcontainers for PostgreSQL, fake TCP servers
│   └── …Application.Tests/        ← State-machine transitions, JobResolver truth-table
│
├── deploy/                        ← MSIX / Inno Setup scripts, Windows Service wrapper
└── docs/                          ← ADRs, MES protocol spec, Keyence command reference
```

## Architecture

**Style**: Clean/Onion Architecture with **MVVM** at the presentation layer.

- **Core** is framework-agnostic (no WPF, no EF, no sockets). It defines the domain and the *ports* (interfaces).
- **Infrastructure** implements the ports using concrete technologies (TCP, EF Core, filesystem, DPAPI).
- **Application** hosts the **finite state machine** that drives the production flow and coordinates the ports.
- **UI** binds to ViewModels; ViewModels call Application services. Views are "dumb" (XAML + bindings).

### Runtime state machine

```
         ┌──────────────┐  MES frame (STX…ETX)   ┌─────────────┐
         │    Idle      │ ─────────────────────▶ │   Parsing   │
         └──────────────┘                        └─────────────┘
                ▲                                       │
                │ ack                                   ▼ JobResolver(truth table)
                │                                ┌─────────────┐
                │                                │ JobSwitching│ ──▶ Keyence: CHANGE JOB
                │                                └─────────────┘
                │                                       │ ok
                │                                       ▼
                │                                ┌─────────────┐
                │                                │  Triggering │ ──▶ Keyence: TRIGGER
                │                                └─────────────┘
                │                                       │
                │                                       ▼ async result
                │              OK                ┌─────────────┐           NG
                ├──────── persist + image ◀──────│ Evaluating  │────────────┐
                │                                └─────────────┘            ▼
                │                                                    ┌─────────────┐
                │                                                    │  NgAlert    │
                │                                RETEST button       └─────────────┘
                │                                (operator)                 │
                └────────────────────────────────────────────────────◀─────┘
```

Implemented with **Stateless** (small FSM library) to make transitions and guards explicit and unit-testable.

## Recommended libraries

| Concern                      | Library                                                  | Why                                                       |
| ---                          | ---                                                      | ---                                                       |
| UI / MVVM                    | **WPF (.NET 8)** + **CommunityToolkit.Mvvm**             | `[ObservableProperty]`, `[RelayCommand]` source generators |
| Dependency Injection         | **Microsoft.Extensions.Hosting** + `…DependencyInjection`| Same container pattern as ASP.NET; Generic Host lifecycle |
| Configuration                | **Microsoft.Extensions.Configuration.Json**              | `appsettings.json` + env overrides                         |
| Logging                      | **Serilog** (File + EventLog + Seq sinks)                | Rolling files; structured logs for audits                 |
| State machine                | **Stateless**                                            | Tiny, deterministic, great for FSMs                       |
| Persistence                  | **Entity Framework Core 8** (+ `Npgsql` or `Sqlite`)     | Migrations, LINQ, supports both DB choices                |
| Password hashing             | `System.Security.Cryptography` (**PBKDF2**)              | Built-in, FIPS-friendly                                   |
| Secrets-at-rest              | **DPAPI** (`ProtectedData`)                              | Machine/user-scoped encryption for config file            |
| TCP client                   | `System.Net.Sockets.TcpClient` + **Polly**               | Polly retries + circuit breakers for watchdog             |
| Image handling               | **ImageSharp** (SixLabors) or **WPF BitmapImage**        | Format conversion + thumbnails                            |
| Testing                      | **xUnit**, **FluentAssertions**, **NSubstitute**         | Readable tests, easy mocks                                |
| Integration tests (DB)       | **Testcontainers for .NET**                              | Real PostgreSQL in CI without installing it               |
| Packaging / install          | **Inno Setup** or **MSIX**                               | Standard for Windows line-side PCs                        |
| Service wrapper (optional)   | **Microsoft.Extensions.Hosting.WindowsServices**         | Run headless worker alongside UI                          |

> Avoid: heavyweight frameworks (Prism, Caliburn) — overkill for a single-screen operator HMI. CommunityToolkit.Mvvm gives 95% of the value with zero boilerplate.

## Non-functional requirements

- **Availability**: watchdog reconnects MES and Keyence sockets with exponential backoff (Polly).
- **Determinism**: every state transition is logged with correlation id = MES frame sequence.
- **Traceability**: every inspection persists `{timestamp, model, job, result, imagePath, operator, stationId}`.
- **Security**: operator/admin roles; admin screen gated by PBKDF2-hashed password; config file encrypted with DPAPI.
- **Performance budget**: MES frame → trigger ≤ 150 ms on the local network.
- **Image retention**: configurable `retentionDays` and `maxDiskGb`; nightly cleanup job.
- **Observability**: Serilog + Windows Event Log; optional Seq for central aggregation.

## Industrial desktop hardening

The UI behaves like a line-side kiosk — not a desktop app:

| Concern                  | Implementation                                                                       |
| ---                      | ---                                                                                  |
| Single instance          | Named `Mutex` (`Local\HyundaiTransys.VisionInspection.SingleInstance`) acquired in `App.OnStartup` **before any TCP port is opened**. A second launch brings the existing window to the foreground and exits. |
| Kiosk mode               | `IKioskModeService` sets `WindowState=Maximized`, `WindowStyle=None`, `Topmost=true`, `ShowInTaskbar=false`, blocks `Alt+F4`/`Alt+Tab`/Win keys, cancels minimize and `Closing` while locked. |
| Password-gated exit      | "Exit (Admin)" button calls `RequestAdminAccess()` → admin login → unlock → `Shutdown`. Operators cannot close the HMI. |
| Global crash logs        | `CrashLogger` writes to `%ProgramData%\HTVision\crash\crash-YYYYMMDD.log` via three hooks: `AppDomain.UnhandledException`, `Application.DispatcherUnhandledException`, `TaskScheduler.UnobservedTaskException`. The dispatcher hook sets `Handled=true` so the HMI survives. |
| Structured logs          | Serilog → `C:\HTVision\logs\vision-YYYYMMDD.log` (rolling, 30-day retention) + Windows Event Log. |
| Auto-start at logon      | `HKCU\...\Run` value registered by `deploy\install-autostart.ps1` (no elevation required). |

## Building and publishing

### Developer loop (engineering PC only)

```powershell
dotnet restore
dotnet build -c Release
dotnet test
dotnet run --project src/HyundaiTransys.VisionInspection.UI
```

### Production build — single-file self-contained .exe

The Lenovo ThinkCentre does **not** need the .NET SDK or runtime. One command:

```cmd
deploy\publish.cmd
```

or explicitly:

```
dotnet publish src/HyundaiTransys.VisionInspection.UI/HyundaiTransys.VisionInspection.UI.csproj ^
    -c Release -r win-x64 --self-contained true ^
    /p:PublishSingleFile=true ^
    /p:PublishReadyToRun=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:IncludeAllContentForSelfExtract=true ^
    /p:EnableCompressionInSingleFile=true ^
    /p:DebugType=embedded
```

Output: `publish\win-x64\HTVision.exe` (~80–130 MB, includes the .NET 8 runtime)
plus `appsettings.json`. Copy both to `C:\HTVision\` on the line PC.

See [`deploy/README.md`](deploy/README.md) for full installation, auto-start and
update procedures.

Target framework: **net8.0-windows** (UI), **net8.0** (libraries).
