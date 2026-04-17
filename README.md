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

## Running the application — step by step

The application has **two ways to run**, controlled by the `"IsDeveloperMode"`
flag in `appsettings.json`:

| Mode               | When                         | How the HMI behaves                                      |
| ---                | ---                          | ---                                                      |
| **Developer Mode** | Your laptop, debugging       | Maximized with title bar + X; Alt+F4/Alt+Tab/Win all work; Exit button closes without password |
| **Production Kiosk** | Lenovo ThinkCentre on the line | Borderless, Topmost, taskbar hidden; keyboard shortcuts suppressed; Exit requires admin login |

---

### Prerequisites (once per machine)

1. **Install the .NET 8 SDK** on the engineering PC:
   - Download: <https://dotnet.microsoft.com/download/dotnet/8.0> → *SDK 8.0.x · Windows x64*.
   - Verify:
     ```powershell
     dotnet --version
     # must print 8.0.x
     ```
2. **(Optional) Install an IDE** for a nicer debugging experience:
   - Visual Studio 2022 (17.8+) with the *".NET desktop development"* workload, or
   - JetBrains Rider 2024.x, or
   - VS Code with the *C# Dev Kit* extension.
3. **Clone the repository**:
   ```powershell
   git clone <repo-url> HyundaiTransys.VisionInspection
   cd HyundaiTransys.VisionInspection
   ```
4. **Restore NuGet packages** (first time only; `build` and `run` will do it automatically afterwards):
   ```powershell
   dotnet restore
   ```

---

### Option A — Run on your laptop (Developer Mode)

Use this while developing / testing. The HMI behaves like a normal window.

1. Open `src/HyundaiTransys.VisionInspection.UI/appsettings.json` and set:
   ```json
   "App": {
     "IsDeveloperMode": true,
     ...
   }
   ```
2. From the repo root, launch the app:
   ```powershell
   dotnet run --project src/HyundaiTransys.VisionInspection.UI
   ```
3. The window opens maximized with the title `Hyundai Transys — Vision Inspection  [DEVELOPER MODE]`. Close it with **the X button** or **Alt+F4** — no password needed.

> The MES and camera will log connection errors until you provide real endpoints
> or a simulator — this is expected on a laptop.

#### Running from Visual Studio / Rider

1. Open `HyundaiTransys.VisionInspection.sln`.
2. In Solution Explorer, right-click **HyundaiTransys.VisionInspection.UI → Set as Startup Project**.
3. Make sure `appsettings.json` has `"IsDeveloperMode": true`.
4. Press **F5** (debug) or **Ctrl+F5** (no debug).

#### Running the unit tests

```powershell
dotnet test
```

Runs every test project under `tests/` and prints a pass/fail summary.

---

### Option B — Build the single-file `.exe` (for the Lenovo)

Use this when you're ready to ship to the line. The output is a single
self-contained executable — the target PC does **not** need .NET installed.

1. Make sure `appsettings.json` has `"IsDeveloperMode": false`.
2. From the repo root, run:
   ```cmd
   deploy\publish.cmd
   ```
   Equivalent one-liner:
   ```powershell
   dotnet publish src/HyundaiTransys.VisionInspection.UI/HyundaiTransys.VisionInspection.UI.csproj `
       -c Release -r win-x64 --self-contained true `
       /p:PublishSingleFile=true `
       /p:PublishReadyToRun=true `
       /p:IncludeNativeLibrariesForSelfExtract=true `
       /p:IncludeAllContentForSelfExtract=true `
       /p:EnableCompressionInSingleFile=true `
       /p:DebugType=embedded
   ```
3. Output appears in `publish\win-x64\`:
   - `HTVision.exe` (~80–130 MB, includes the .NET 8 runtime)
   - `appsettings.json`

---

### Option C — Deploy and run on the Lenovo ThinkCentre

On the line-side PC (no SDK required):

1. Copy the two files from `publish\win-x64\` to `C:\HTVision\`:
   - `HTVision.exe`
   - `appsettings.json`
2. Edit `C:\HTVision\appsettings.json` with the real station values:
   - `App.Mes.IpAddress` / `Port` — the MES endpoint.
   - `App.Keyence.IpAddress` / `Port` — the IV4-500CA camera.
   - `App.Storage.OkImagesPath` / `NgImagesPath`.
   - Keep `"IsDeveloperMode": false`.
3. Double-click `HTVision.exe` (or use the desktop shortcut). The HMI starts
   in full-screen kiosk mode.
4. To auto-start at Windows logon, run once as the operator user:
   ```powershell
   pwsh C:\HTVision\deploy\install-autostart.ps1 -ExePath "C:\HTVision\HTVision.exe"
   ```
5. To exit the kiosk, click **Exit (Admin)** and authenticate with an
   Administrator account.

Full deployment and update procedures (auto-start alternatives, troubleshooting,
log locations) are in [`deploy/README.md`](deploy/README.md).

---

### Quick reference

| Task                              | Command                                                                          |
| ---                               | ---                                                                              |
| Restore packages                  | `dotnet restore`                                                                 |
| Build (Release)                   | `dotnet build -c Release`                                                        |
| Run tests                         | `dotnet test`                                                                    |
| Run on laptop (dev mode)          | `dotnet run --project src/HyundaiTransys.VisionInspection.UI`                    |
| Publish single-file `.exe`        | `deploy\publish.cmd`                                                             |
| Register auto-start on Lenovo     | `pwsh deploy\install-autostart.ps1 -ExePath "C:\HTVision\HTVision.exe"`          |

Target framework: **net8.0-windows** (UI), **net8.0** (libraries).
