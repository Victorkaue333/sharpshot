; SharpShot — Inno Setup script (installer is the primary distribution channel).
; Compiled by tools/release/make-release.ps1:
;   ISCC.exe /DMyAppVersion=0.1.1 /DSourceDir=<publish folder> /DOutputDir=<artifacts/release> installer\SharpShot.iss

#define MyAppName "SharpShot"
#ifndef MyAppVersion
  #define MyAppVersion "0.1.1"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\release"
#endif
#define MyAppPublisher "Victor Alves"
#define MyAppURL "https://github.com/Victorkaue333/sharpshot"
#define MyAppExeName "SharpShot.exe"

[Setup]
AppId={{B7A2D9F4-6C1E-4B8A-9F3D-5E2C7A1F0D42}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
VersionInfoVersion={#MyAppVersion}.0
VersionInfoDescription=Instalador do SharpShot
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir={#OutputDir}
OutputBaseFilename=SharpShot-Setup-{#MyAppVersion}
SetupIconFile=..\src\SharpShot.App\Assets\SharpShot.ico
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
; Per-user by default (no UAC); the wizard lets the user pick "all users" (Program Files, admin).
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=no

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Iniciar o SharpShot junto com o Windows (fica na bandeja)"; GroupDescription: "Inicialização:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar o {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Same value the app writes from Configurações › Iniciar com o Windows; removed on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SharpShot"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startup
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "SharpShot"; Flags: uninsdeletevalue dontcreatekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The app lives in the tray; make sure it is not running before files are removed.
Filename: "{cmd}"; Parameters: "/C taskkill /IM {#MyAppExeName} /F"; Flags: runhidden; RunOnceId: "KillSharpShot"

[Code]
// Offer to remove settings and logs (%LOCALAPPDATA%\SharpShot) when uninstalling.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\SharpShot');
    if DirExists(DataDir) then
    begin
      if UninstallSilent or (MsgBox('Remover também as configurações e os logs do SharpShot (' + DataDir + ')?' + #13#10 + 'As capturas em Imagens\SharpShot não são apagadas.', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES) then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
