; AutoSSL installer script for NSIS
!include "MUI2.nsh"

Name "AutoSSL"
OutFile "AutoSSLSetup.exe"
InstallDir "$PROGRAMFILES\AutoSSL"
RequestExecutionLevel admin

Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "..\..\bin\*"

  CreateDirectory "$PROGRAMDATA\AutoSSL"
  CreateDirectory "$PROGRAMDATA\AutoSSL\logs"
  CreateDirectory "$PROGRAMDATA\AutoSSL\config"
  CreateDirectory "$PROGRAMDATA\AutoSSL\plugins"

  nsExec::Exec '"$INSTDIR\AutoSSL.Service.exe" install'
  nsExec::Exec 'sc start AutoSSL.Service'

  CreateShortCut "$SMPROGRAMS\AutoSSL\AutoSSL UI.lnk" "$INSTDIR\AutoSSL.UI.exe"
  CreateShortCut "$SMPROGRAMS\AutoSSL\AutoSSL Update.lnk" "$INSTDIR\update.bat"
SectionEnd

Section "Uninstall"
  nsExec::Exec 'sc stop AutoSSL.Service'
  nsExec::Exec 'sc delete AutoSSL.Service'

  Delete "$SMPROGRAMS\AutoSSL\AutoSSL UI.lnk"
  Delete "$SMPROGRAMS\AutoSSL\AutoSSL Update.lnk"
  RMDir "$SMPROGRAMS\AutoSSL"

  Delete "$INSTDIR\*.*"
  RMDir "$INSTDIR"

  MessageBox MB_YESNO "Deseja remover dados em $PROGRAMDATA\AutoSSL?" IDNO +2
    RMDir /r "$PROGRAMDATA\AutoSSL"
SectionEnd
