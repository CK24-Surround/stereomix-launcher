#define MyAppName "StereoMix Launcher"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Surround"
#define MyAppURL "https://discord.gg/bPCr4sy7QR"
#define MyAppExeName "StereoMix-Launcher.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".myp"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
AppId={{E3DDE306-2A5C-4ED3-9F33-D5D156A7F324}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
ChangesAssociations=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputBaseFilename=StereoMix-Launcher-Installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=.\StereoMix-Launcher\resources\Surround.ico

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\resources\Surround.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\resources\Surround.ico"

[Languages]
;Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ".\StereoMix-Launcher\bin\Release\net8.0-windows\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\StereoMix-Launcher\bin\Release\net8.0-windows\win-x64\StereoMix-Launcher.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\StereoMix-Launcher\bin\Release\net8.0-windows\win-x64\StereoMix-Launcher.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\StereoMix-Launcher\bin\Release\net8.0-windows\win-x64\StereoMix-Launcher.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\StereoMix-Launcher\bin\Release\net8.0-windows\win-x64\StereoMix-Launcher.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\StereoMix-Launcher\resources\Surround.ico"; DestDir: "{app}\resources"; Flags: ignoreversion

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".myp"; ValueData: ""

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

