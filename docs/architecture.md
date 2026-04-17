# Architecture notes

## Layered dependencies (allowed references)

```
UI ──▶ Application ──▶ Core
UI ──▶ Infrastructure ──▶ Core
Application ──▶ Core
```

Core has **no references** (pure domain). Infrastructure references Core only.
Application references Core only. UI is the composition root.

## Why the orchestrator lives in Application

The state machine is pure business rules: it coordinates ports (`IMesClient`,
`IKeyenceClient`, `IJobResolver`, `IInspectionRepository`, `IImageStorage`) and
knows nothing about TCP or EF or WPF. That makes transitions unit-testable with
test doubles for every port.

## Threading model

- TCP clients run their own background tasks; they raise events off the UI thread.
- The UI uses `IUiDispatcher` (WPF's `Dispatcher`) to marshal event updates onto
  the UI thread before touching bound properties.
- The `InspectionOrchestrator` uses `Stateless` which is thread-safe for
  `FireAsync`; all side effects happen inside `OnEntry*` handlers.

## Reliability

| Concern         | Mechanism                                                |
| ---             | ---                                                      |
| MES drops       | Auto-reconnect loop with exponential backoff             |
| Camera timeouts | Polly policies wrapping `ChangeJobAsync` / `TriggerAsync`|
| DB failures     | EF retrying execution strategy; state machine → Faulted  |
| Disk full       | Retention service enforces quota nightly                 |
| Power loss      | Every state transition logged with correlation id        |

## Security

- Admin screen gated by role (Administrator only).
- Passwords stored as PBKDF2(SHA-256) with 210k iterations + 16-byte salt.
- Config file encrypted at rest with DPAPI (LocalMachine scope).
- Audit log (Serilog) retains user/action/timestamp for changes to JOB mapping
  or configuration.

## Testing strategy

- **Core.Tests**: value objects, enums, parser edge cases (empty frame, wrong separator, missing STX/ETX).
- **Application.Tests**: every FSM transition with fake ports; JobResolver truth-table scenarios.
- **Infrastructure.Tests**: MES TCP client against a fake TCP server; Keyence protocol round-trip; EF against SQLite in-memory; PostgreSQL via Testcontainers.
- **UI**: smoke tests with WPF UI Automation (optional, low ROI).
