[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$Restart
)

$ErrorActionPreference = 'Stop'

$clientRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$buildRoot = Join-Path $clientRoot 'build\win32-x86'
$exePath = Join-Path $buildRoot 'bin\KingdomCocosClient\Debug\KingdomCocosClient.exe'

if (-not $env:COCOS2DX_ROOT) {
    $env:COCOS2DX_ROOT = 'C:\dev\cocos2d-x-3.17.2'
}
if (-not (Test-Path -LiteralPath $env:COCOS2DX_ROOT)) {
    throw "COCOS2DX_ROOT does not exist: $env:COCOS2DX_ROOT"
}

$cmakeCommand = Get-Command cmake -ErrorAction SilentlyContinue
$cmakePath = if ($null -ne $cmakeCommand) { $cmakeCommand.Source } else { $null }
if (-not $cmakePath) {
    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
    $visualStudioCmake = Join-Path $programFilesX86 'Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
    if (Test-Path -LiteralPath $visualStudioCmake) {
        $cmakePath = $visualStudioCmake
    }
}
if (-not $cmakePath) {
    throw 'CMake was not found. Install the Visual Studio CMake tools or add CMake to PATH.'
}

if (-not (Test-Path -LiteralPath (Join-Path $buildRoot 'CMakeCache.txt'))) {
    & $cmakePath -S $clientRoot -B $buildRoot -G 'Visual Studio 17 2022' -A Win32
    if ($LASTEXITCODE -ne 0) {
        throw "CMake configure failed with exit code $LASTEXITCODE."
    }
}

if (-not $NoBuild) {
    & $cmakePath --build $buildRoot --config Debug --target KingdomCocosClient
    if ($LASTEXITCODE -ne 0) {
        throw "Cocos client build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Cocos client executable was not found: $exePath"
}

if ($Restart) {
    Get-Process KingdomCocosClient -ErrorAction SilentlyContinue | Stop-Process -Force
}

$workingDirectory = Split-Path -Parent $exePath
Start-Process -FilePath $exePath -WorkingDirectory $workingDirectory
Write-Host "Started KingdomCocosClient from $workingDirectory"
