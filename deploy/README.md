# Deployment — Hyundai Transys Vision Inspection

The target PC is a **Lenovo ThinkCentre (Windows 10/11 x64)**. The operator must
never see a terminal or run `dotnet` commands. Shipping model:

- A single **self-contained `HTVision.exe`** (the .NET 8 runtime is embedded).
- A sibling `appsettings.json` for per-station configuration.
- An entry under `HKCU\...\Run` so the HMI launches at logon.
- A desktop shortcut for manual relaunch.

## 1 — Build the executable (on the engineering PC)

Requires .NET 8 SDK only on the engineering PC. From the repo root:

```cmd
deploy\publish.cmd
```

or, equivalently:

```powershell
pwsh ./deploy/publish.ps1
```

Output: `publish\win-x64\HTVision.exe` + `appsettings.json`.

The raw `dotnet` command that both scripts run is:

```
dotnet publish src/HyundaiTransys.VisionInspection.UI/HyundaiTransys.VisionInspection.UI.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    /p:PublishSingleFile=true ^
    /p:PublishReadyToRun=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:IncludeAllContentForSelfExtract=true ^
    /p:EnableCompressionInSingleFile=true ^
    /p:DebugType=embedded
```

> WPF is **not trim-safe**, so we ship with `PublishTrimmed=false`. The
> resulting `.exe` is ~80–130 MB — expected for a self-contained WPF app.

## 2 — Install on the Lenovo ThinkCentre

1. Copy the contents of `publish\win-x64\` into `C:\HTVision\`.
2. Edit `C:\HTVision\appsettings.json` with the station's MES IP/port, Keyence IP,
   DB provider, etc.
3. Create the log/image folders (or let the app create them on first start):
   - `C:\HTVision\logs\`
   - `C:\HTVision\Images\OK\`, `C:\HTVision\Images\NG\`
4. Right-click `HTVision.exe` → **Send to → Desktop (create shortcut)**.

## 3 — Auto-start at Windows logon

Run, as the operator user:

```powershell
pwsh C:\HTVision\deploy\install-autostart.ps1 -ExePath "C:\HTVision\HTVision.exe"
```

This adds the `HTVision` value under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. No elevation required.

Alternative with the classic Startup folder:

```cmd
copy "C:\HTVision\HTVision.exe.lnk" "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
```

For fully unattended kiosks, consider configuring **Windows Assigned Access**
or a dedicated operator account with auto-logon (ask IT before enabling auto-logon).

## 4 — What the operator sees

- HMI starts **maximized**, **always on top**, **no taskbar icon**.
- `Alt+F4`, `Alt+Tab`, and the Win key are suppressed.
- The only way out is the **"Exit (Admin)"** button, which prompts for an
  Administrator login before closing.

## 5 — Crash logs

Written to `%ProgramData%\HTVision\crash\crash-YYYYMMDD.log` (always, even if
Serilog / the DI host didn't come up). Structured logs (Serilog) live in
`C:\HTVision\logs\vision-YYYYMMDD.log`.

## 6 — Updating

Replace `HTVision.exe` in place (keep `appsettings.json` and the `config\`
folder). No installer, no registry changes required for an update.
