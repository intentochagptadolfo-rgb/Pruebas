<#
.SYNOPSIS
    Publishes HTVision as a single-file self-contained Windows x64 executable.

.DESCRIPTION
    Output goes to publish\win-x64\HTVision.exe alongside appsettings.json.
    The target PC does NOT need the .NET 8 SDK or runtime installed.

.EXAMPLE
    pwsh ./deploy/publish.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime       = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot  = Resolve-Path (Join-Path $PSScriptRoot '..')
$project   = Join-Path $repoRoot 'src/HyundaiTransys.VisionInspection.UI/HyundaiTransys.VisionInspection.UI.csproj'
$outputDir = Join-Path $repoRoot "publish/$Runtime"

if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }

& dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:IncludeAllContentForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=embedded `
    -o $outputDir

if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }

Write-Host ""
Write-Host "[OK] Published to: $outputDir" -ForegroundColor Green
Write-Host "     Ship HTVision.exe and appsettings.json to the line-side PC."
