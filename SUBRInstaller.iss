[Setup]
AppName=SUBR
AppVersion=1.0
DefaultDirName={autopf}\SUBR
DefaultGroupName=SUBR
OutputDir=.\Output
OutputBaseFilename=SUBR_Installer
Compression=lzma
SolidCompression=yes

[Files]
Source: "SUBR.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\SUBR"; Filename: "{app}\SUBR.exe"
Name: "{commondesktop}\SUBR"; Filename: "{app}\SUBR.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a Desktop Shortcut"; GroupDescription: "Additional icons:"
