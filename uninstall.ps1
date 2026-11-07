# Removes The Colour Clock from the current user's profile and unregisters the
# screen saver.  Settings (%LOCALAPPDATA%\TheColourClock\settings.json) are kept
# unless -PurgeSettings is passed.

param(
    [switch]$PurgeSettings
)

$ErrorActionPreference = "Stop"

Get-Process -Name "TheColourClock" -ErrorAction SilentlyContinue | Stop-Process -Force

$desktop = "HKCU:\Control Panel\Desktop"
$current = (Get-ItemProperty -Path $desktop -Name "SCRNSAVE.EXE" -ErrorAction SilentlyContinue)."SCRNSAVE.EXE"
$target = Join-Path $env:LOCALAPPDATA "TheColourClock\App"

if ($null -ne $current -and $current -like "*The Colour Clock.scr") {
    Remove-ItemProperty -Path $desktop -Name "SCRNSAVE.EXE" -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $desktop -Name "ScreenSaveActive" -Value "0"
}

if (Test-Path $target) {
    Remove-Item $target -Recurse -Force
}

if ($PurgeSettings) {
    $settings = Join-Path $env:LOCALAPPDATA "TheColourClock\settings.json"
    if (Test-Path $settings) {
        Remove-Item $settings -Force
    }
}

Write-Host "已卸载。"
