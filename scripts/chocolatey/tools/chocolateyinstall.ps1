$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url64      = 'https://autossl.s3.amazonaws.com/downloads/archive/AutoSSLSetup_V6.1.9.exe'

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'exe'
  url64bit      = $url64
  softwareName  = 'AutoSSL*'
  checksum64    = 'REPLACE_WITH_ACTUAL_CHECKSUM'
  checksumType64= 'sha256'
  validExitCodes= @(0)
  silentArgs   = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
}

Install-ChocolateyPackage @packageArgs

