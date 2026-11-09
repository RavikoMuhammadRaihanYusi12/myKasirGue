; Inno Setup script — WarungApp
; Build: dotnet publish src/WarungApp.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
; Lalu: iscc installer\WarungApp.iss

#define AppName "WarungApp"
#define AppVersion "1.0.0"
#define AppPublisher "WarungApp"
#define ExeName "WarungApp.Desktop.exe"

[Setup]
AppId={{9C6F1A24-6E2B-4B7C-9E31-2A6B7E4D8C51}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\installer-output
OutputBaseFilename=WarungApp-Setup-{#AppVersion}
SetupIconFile=..\src\WarungApp.Desktop\Assets\WarungApp.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#ExeName}

[Languages]
; Inno Setup tidak menyertakan Indonesian.isl; installer memakai bahasa Inggris,
; aplikasi tetap berbahasa Indonesia.
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "..\dist\{#ExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#ExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

; Catatan: data pengguna ada di %LOCALAPPDATA%\WarungApp sehingga TIDAK terhapus saat uninstall/upgrade.
