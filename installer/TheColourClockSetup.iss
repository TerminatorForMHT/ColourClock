; The Colour Clock - Windows screen saver installer (Inno Setup 6)
; Build: "C:\Users\wangc\AppData\Local\Programs\Inno Setup 6\ISCC.exe" installer\TheColourClockSetup.iss

#define AppName "The Colour Clock"
#define AppVersion "1.0"
#define AppExe "TheColourClock.exe"
#define ScrName "The Colour Clock.scr"
#define PublishDir "..\src\ColourClock.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
#define StubScr "build\TheColourClock.scr"

[Setup]
AppId={{8A4B3E26-9F1D-4C8B-A7D2-6C3F1E9B0A55}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=The Colour Clock
DefaultDirName={autopf}\The Colour Clock
DefaultGroupName=The Colour Clock
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\src\ColourClock.App\Assets\app.ico
OutputDir=..\dist
OutputBaseFilename=TheColourClockSetup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
DisableWelcomePage=no
VersionInfoVersion=1.0.0.0
VersionInfoProductName={#AppName}

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "screensaver"; Description: "Set The Colour Clock as the current screen saver"; GroupDescription: "Screen saver:"
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Windows only enumerates screen savers placed in System32; the launcher forwards to {app}.
Source: "{#StubScr}"; DestDir: "{sys}"; DestName: "{#ScrName}"; Flags: ignoreversion

[Icons]
Name: "{group}\The Colour Clock"; Filename: "{app}\{#AppExe}"; Comment: "Open the desktop clock"
Name: "{group}\Screen Saver Settings"; Filename: "{app}\{#AppExe}"; Parameters: "/c"
Name: "{group}\Uninstall The Colour Clock"; Filename: "{uninstallexe}"
Name: "{autodesktop}\The Colour Clock"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "SOFTWARE\The Colour Clock"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Control Panel\Desktop"; ValueType: string; ValueName: "SCRNSAVE.EXE"; ValueData: "{sys}\{#ScrName}"; Flags: uninsdeletevalue; Tasks: screensaver
Root: HKCU; Subkey: "Control Panel\Desktop"; ValueType: string; ValueName: "ScreenSaveActive"; ValueData: "1"; Tasks: screensaver

[Run]
Filename: "{app}\{#AppExe}"; Parameters: "/c"; Description: "Open screen saver settings"; Flags: nowait postinstall skipifsilent; Tasks: screensaver

[UninstallDelete]
Type: files; Name: "{app}\{#ScrName}"
Type: files; Name: "{sys}\{#ScrName}"

[Code]
const
  SPIF_UPDATEINIFILE = $0001;
  SPIF_SENDCHANGE = $0002;
  SPI_SETSCREENSAVEACTIVE = $0011;

function SystemParametersInfo(uiAction, uiParam: Integer; pvParam: Integer; fWinIni: Integer): Boolean;
  external 'SystemParametersInfoW@user32.dll stdcall';

function ScreenSaverSelected: Boolean;
begin
  Result := WizardIsTaskSelected('screensaver');
end;

procedure RefreshScreenSaver;
begin
  SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 1, 0, SPIF_UPDATEINIFILE or SPIF_SENDCHANGE);
end;

procedure EnsureTimeout;
var
  Value: String;
begin
  if not RegQueryStringValue(HKCU, 'Control Panel\Desktop', 'ScreenSaveTimeOut', Value) then
    RegWriteStringValue(HKCU, 'Control Panel\Desktop', 'ScreenSaveTimeOut', '600');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if ScreenSaverSelected then
    begin
      EnsureTimeout;
      RefreshScreenSaver;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, 1, 0, SPIF_UPDATEINIFILE or SPIF_SENDCHANGE);
end;
