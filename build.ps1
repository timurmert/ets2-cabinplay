# Builds everything into dist\: the .scs mod, the game plugin, the companion app, and the
# release package people download (dist\CabinPlay-<version>.zip).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'
$version = (Get-Content (Join-Path $root 'VERSION') -TotalCount 1).Trim()
Write-Host "CabinPlay $version"

Write-Host '== mod (.scs)'
$env:CABINPLAY_VERSION = $version
& python -I (Join-Path $root 'tools\build_mod.py')
if ($LASTEXITCODE -ne 0) { throw 'mod build failed' }

Write-Host '== plugin (dll)'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'plugin\build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'plugin build failed' }

Write-Host '== companion app'
# Self-contained: people do not have to install .NET first.
$appOut = Join-Path $dist 'app'
if (Test-Path $appOut) { Remove-Item $appOut -Recurse -Force }
& dotnet publish (Join-Path $root 'app\CabinPlay.csproj') -c Release -r win-x64 --self-contained true `
    -p:Version=$version -p:DebugType=none -o $appOut -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'app build failed' }

Write-Host '== release package'
$name = "CabinPlay-$version"
$stage = Join-Path $dist $name
$zip = Join-Path $dist "$name.zip"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
$data = Join-Path $stage 'data'
New-Item -ItemType Directory -Force $data | Out-Null
Copy-Item (Join-Path $root 'packaging\*') $stage
Copy-Item (Join-Path $root 'install.ps1') $data
Copy-Item (Join-Path $dist 'cabinplay.scs') $data
Copy-Item (Join-Path $dist 'plugin') $data -Recurse
Copy-Item $appOut $data -Recurse
# The zip holds one folder, so extracting it never scatters files.
Compress-Archive -Path $stage -DestinationPath $zip
Remove-Item $stage -Recurse -Force

Write-Host ''
Write-Host ("Done: {0} ({1:N1} MB). Run install.ps1 to install this build on this PC." -f $zip, ((Get-Item $zip).Length / 1MB))
