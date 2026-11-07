#Requires -Version 5.1
# Builds everything needed for a working screen saver:
#   1. installer\build\TheColourClock.scr  - tiny launcher that lives in System32
#   2. the WinUI 3 app (self-contained, win-x64)
#   3. dist\TheColourClockSetup.exe        - the Inno Setup installer
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$stubOut = Join-Path $root "installer\build\TheColourClock.scr"
$icon = Join-Path $root "src\ColourClock.App\Assets\app.ico"
$project = Join-Path $root "src\ColourClock.App\ColourClock.App.csproj"
$iss = Join-Path $root "installer\TheColourClockSetup.iss"

New-Item -ItemType Directory -Force -Path (Split-Path $stubOut) | Out-Null

# 1. Launcher stub (Windows only enumerates screen savers that sit in System32,
#    while the WinUI app must stay in Program Files for its Assets to resolve).
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "Cannot find the .NET Framework compiler: $csc" }

& $csc /nologo /target:winexe /platform:anycpu /optimize+ "/win32icon:$icon" `
    "/out:$stubOut" (Join-Path $root "installer\stub\ColourClockStub.cs")
if ($LASTEXITCODE -ne 0) { throw "Stub compilation failed" }

# 2. The app.  A machine-wide dotnet without an SDK is common, so the user-local
#    SDK installation is preferred when it exists.
$dotnet = $null
$local = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (Test-Path $local) { $dotnet = $local }
if (-not $dotnet) {
    $dotnet = (Get-Command dotnet.exe -ErrorAction SilentlyContinue).Source
}
if (-not $dotnet) { throw "Cannot find the .NET 8 SDK (dotnet)." }

& $dotnet publish $project -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# 3. Installer.
$iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { $iscc = $candidate; break }
    }
}
if (-not $iscc) { throw "Cannot find Inno Setup 6 (ISCC.exe). Install it from https://jrsoftware.org/isdl.php" }

& $iscc $iss
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed" }

Write-Host "Done: $(Join-Path $root 'dist\TheColourClockSetup.exe')"
