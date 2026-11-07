# Installs The Colour Clock into the current user's profile and registers it as
# the Windows screen saver (.scr).  Run from PowerShell in the repository root:
#   powershell -ExecutionPolicy Bypass -File .\install.ps1
#
# Publish the app first (or pass -PublishDir):
#   dotnet publish src\ColourClock.App -c Release

param(
    [string]$PublishDir = ""
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($PublishDir -eq "") {
    $PublishDir = Join-Path $root "src\ColourClock.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
}

$exe = Join-Path $PublishDir "TheColourClock.exe"
if (-not (Test-Path $exe)) {
    throw "找不到发布输出: $exe`n请先运行 dotnet publish src\ColourClock.App -c Release"
}

$target = Join-Path $env:LOCALAPPDATA "TheColourClock\App"
if (Test-Path $target) {
    Remove-Item $target -Recurse -Force
}

New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item -Path (Join-Path $PublishDir "*") -Destination $target -Recurse -Force

# Same executable serves as the desktop clock (/no args), settings (/c) and saver (/s).
$scr = Join-Path $target "The Colour Clock.scr"
Copy-Item -Path (Join-Path $target "TheColourClock.exe") -Destination $scr -Force

# Register the screen saver for the current user (no elevation required).
$desktop = "HKCU:\Control Panel\Desktop"
Set-ItemProperty -Path $desktop -Name "SCRNSAVE.EXE" -Value $scr
Set-ItemProperty -Path $desktop -Name "ScreenSaveActive" -Value "1"

$timeout = Get-ItemProperty -Path $desktop -Name "ScreenSaveTimeOut" -ErrorAction SilentlyContinue
if ($null -eq $timeout) {
    Set-ItemProperty -Path $desktop -Name "ScreenSaveTimeOut" -Value "600"
}

# Notify the shell so the Screen Saver tab picks the new file up immediately.
Add-Type -Namespace Win32 -Name User32 -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public static extern int SystemParametersInfo(int uiAction, int uiParam, string pvParam, int fWinIni);
'@
[Win32.User32]::SystemParametersInfo(0x0017, 0, $scr, 0x0001 -bor 0x0002) | Out-Null

Write-Host "已安装:"
Write-Host "  程序目录 : $target"
Write-Host "  屏保文件 : $scr"
Write-Host "  桌面时钟 : $(Join-Path $target 'TheColourClock.exe')"
Write-Host "  设置     : $scr /c"
