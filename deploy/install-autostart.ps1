<#
.SYNOPSIS
    Registers HTVision to launch automatically at Windows startup for the current user.

.DESCRIPTION
    Writes HKCU\Software\Microsoft\Windows\CurrentVersion\Run so the HMI
    starts when the operator account signs in. No elevation is required.

.PARAMETER ExePath
    Full path to HTVision.exe on the line-side PC (default: C:\HTVision\HTVision.exe).

.EXAMPLE
    pwsh ./install-autostart.ps1 -ExePath "C:\HTVision\HTVision.exe"
#>
[CmdletBinding()]
param(
    [string]$ExePath = 'C:\HTVision\HTVision.exe'
)

if (-not (Test-Path $ExePath)) {
    throw "Executable not found: $ExePath"
}

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
New-ItemProperty -Path $runKey -Name 'HTVision' -Value "`"$ExePath`"" `
    -PropertyType String -Force | Out-Null

Write-Host "[OK] Registered HTVision to run at logon for $env:USERNAME." -ForegroundColor Green
Write-Host "     To remove: Remove-ItemProperty -Path '$runKey' -Name 'HTVision'"
