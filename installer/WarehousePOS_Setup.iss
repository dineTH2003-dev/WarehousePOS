; Inno Setup Script for WarehousePOS Single-PC Offline Desktop Application

#define MyAppName "WarehousePOS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "WarehousePOS Solutions"
#define MyAppExeName "WarehousePOS.Desktop.exe"

[Setup]
AppId={{D37E6B9A-5821-4B2E-8E48-64E71B3E0010}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={commonpf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=WarehousePOS_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copy all self-contained build artifacts from dist/win-x64 directory
Source: "..\dist\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; Ensure ProgramData directories are created with write access for standard users
Name: "{commonappdata}\WarehousePOS\Data"; Permissions: users-full
Name: "{commonappdata}\WarehousePOS\Backups"; Permissions: users-full
Name: "{commonappdata}\WarehousePOS\Logs"; Permissions: users-full

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
