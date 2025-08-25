; AutoSSL installer script for Inno Setup
[Setup]
AppName=AutoSSL
AppVersion=1.0
DefaultDirName={pf}\\AutoSSL
DefaultGroupName=AutoSSL
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=AutoSSLSetup

[Files]
Source: "..\\..\\bin\\*"; DestDir: "{app}"; Flags: recursesubdirs

[Dirs]
Name: "{commonappdata}\\AutoSSL";
Name: "{commonappdata}\\AutoSSL\\logs";
Name: "{commonappdata}\\AutoSSL\\config";
Name: "{commonappdata}\\AutoSSL\\plugins";

[Run]
; Install and start the Windows service
Filename: "{app}\\AutoSSL.Service.exe"; Parameters: "install"; Flags: runhidden
Filename: "sc.exe"; Parameters: "start AutoSSL.Service"; Flags: runhidden

[UninstallRun]
; Stop and delete the Windows service during uninstall
Filename: "sc.exe"; Parameters: "stop AutoSSL.Service"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete AutoSSL.Service"; Flags: runhidden

[Icons]
Name: "{group}\\AutoSSL UI"; Filename: "{app}\\AutoSSL.UI.exe"
Name: "{group}\\AutoSSL Update"; Filename: "{app}\\update.bat"
Name: "{commondesktop}\\AutoSSL UI"; Filename: "{app}\\AutoSSL.UI.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    if MsgBox('Deseja remover dados em {commonappdata}\\AutoSSL?', mbConfirmation, MB_YESNO) = idYes then
      DelTree(ExpandConstant('{commonappdata}\\AutoSSL'), True, True, True);
end;
