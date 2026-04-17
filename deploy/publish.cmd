@echo off
REM ============================================================================
REM  Publishes HTVision.exe as a single-file self-contained Windows x64 binary.
REM  Output: publish\win-x64\HTVision.exe  (plus appsettings.json next to it)
REM  The Lenovo ThinkCentre does NOT need the .NET 8 SDK/Runtime installed.
REM ============================================================================
setlocal
set SCRIPT_DIR=%~dp0
set REPO_ROOT=%SCRIPT_DIR%..
set PROJECT=%REPO_ROOT%\src\HyundaiTransys.VisionInspection.UI\HyundaiTransys.VisionInspection.UI.csproj
set OUT=%REPO_ROOT%\publish\win-x64

if exist "%OUT%" rmdir /S /Q "%OUT%"

dotnet publish "%PROJECT%" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:PublishReadyToRun=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:IncludeAllContentForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=embedded ^
  -o "%OUT%"

if errorlevel 1 (
    echo.
    echo [ERROR] Publish failed.
    exit /b 1
)

echo.
echo [OK] Published to: %OUT%
echo      Ship "HTVision.exe" and "appsettings.json" to the line-side PC.
endlocal
