$ErrorActionPreference = "Stop"

$machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
$env:Path = "$machinePath;$userPath"
$env:COCOS2DX_ROOT = [Environment]::GetEnvironmentVariable("COCOS2DX_ROOT", "User")

function Test-Command($Name) {
  $command = Get-Command $Name -ErrorAction SilentlyContinue
  if ($null -eq $command) {
    Write-Host "missing: $Name"
    return $false
  }
  Write-Host "found: $Name -> $($command.Source)"
  return $true
}

$ok = $true
if (-not (Test-Command "cmake")) { $ok = $false }
if (-not (Test-Command "cocos")) { $ok = $false }

if ([string]::IsNullOrWhiteSpace($env:COCOS2DX_ROOT)) {
  Write-Host "missing: COCOS2DX_ROOT"
  $ok = $false
} else {
  Write-Host "found: COCOS2DX_ROOT -> $env:COCOS2DX_ROOT"
}

if (-not $ok) {
  exit 1
}
