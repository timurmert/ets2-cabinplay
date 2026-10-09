# Builds everything into dist\: the .scs mod, the game plugin and the companion app.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host '== mod (.scs)'
& python -I (Join-Path $root 'tools\build_mod.py')
if ($LASTEXITCODE -ne 0) { throw 'mod build failed' }

Write-Host '== plugin (dll)'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'plugin\build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'plugin build failed' }

Write-Host '== companion app'
# Published next to the live copy, which may be running; install.ps1 swaps it in.
& dotnet publish (Join-Path $root 'app\CabinPlay.csproj') -c Release -r win-x64 --self-contained false `
    -o (Join-Path $root 'dist\app.new') -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'app build failed' }

Write-Host ''
Write-Host 'Done. Close the game and the CabinPlay app, then run install.ps1 to put the files in place.'
