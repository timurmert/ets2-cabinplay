# Builds everything into dist\: the .scs mod, the game plugin, the companion app, and what
# people download from the release page:
#   dist\CabinPlay-Setup-<version>.exe   the installer (Inno Setup)
#   dist\Uninstall.exe                   starts the uninstaller the setup registered
#   dist\CabinPlay-<version>.zip         the same files, to copy into place by hand
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'
$build = Join-Path $root 'build'
$version = (Get-Content (Join-Path $root 'VERSION') -TotalCount 1).Trim()
Write-Host "CabinPlay $version"

# Inno Setup 6 makes the installer: winget install JRSoftware.InnoSetup
$iscc = @(
    $env:ISCC,
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 is missing (winget install JRSoftware.InnoSetup), or set ISCC to its ISCC.exe' }

Write-Host '== icon and setup pictures'
$pictures = Join-Path $build 'setup'
& python -I (Join-Path $root 'tools\make_icon.py') --wizard $pictures
if ($LASTEXITCODE -ne 0) { throw 'icon build failed' }

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

Write-Host '== uninstaller'
$parts = ($version -split '\.') + @('0', '0', '0')
$uninstallTmp = Join-Path $build 'uninstall'
New-Item -ItemType Directory -Force $uninstallTmp | Out-Null
$res = Join-Path $uninstallTmp 'uninstall.res'
Push-Location (Join-Path $root 'packaging\uninstall')
try {
    & windres "-DVER_MAJOR=$($parts[0])" "-DVER_MINOR=$($parts[1])" "-DVER_PATCH=$($parts[2])" `
        "-DVER_TEXT=$version" uninstall.rc -O coff -o $res
    if ($LASTEXITCODE -ne 0) { throw 'uninstaller resources failed' }
    & gcc -O2 -Wall -Wextra -Wno-unused-parameter -municode -mwindows -static -s `
        -o (Join-Path $dist 'Uninstall.exe') uninstall.c $res -ladvapi32
    if ($LASTEXITCODE -ne 0) { throw 'uninstaller build failed' }
} finally {
    Pop-Location
}

Write-Host '== setup'
& $iscc /Q "/DAppVersion=$version" "/DDist=$dist" "/DPictures=$pictures" (Join-Path $root 'packaging\setup.iss')
if ($LASTEXITCODE -ne 0) { throw 'setup build failed' }
$setup = Join-Path $dist "CabinPlay-Setup-$version.exe"

Write-Host '== zip for installing by hand'
$name = "CabinPlay-$version"
$stage = Join-Path $dist $name
$zip = Join-Path $dist "$name.zip"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item (Join-Path $root 'packaging\*.txt') $stage
Copy-Item (Join-Path $root 'LICENSE') (Join-Path $stage 'LICENSE.txt')
# The plugin contains MinHook, whose licence asks for its notice to travel with binaries.
Copy-Item (Join-Path $root 'plugin\minhook\LICENSE.txt') (Join-Path $stage 'THIRD-PARTY-LICENSES.txt')
Copy-Item (Join-Path $dist 'cabinplay.scs') $stage
Copy-Item (Join-Path $dist 'plugin') $stage -Recurse
Copy-Item $appOut $stage -Recurse
# The zip holds one folder, so extracting it never scatters files.
Compress-Archive -Path $stage -DestinationPath $zip
Remove-Item $stage -Recurse -Force

Write-Host ''
foreach ($f in $setup, (Join-Path $dist 'Uninstall.exe'), $zip) {
    Write-Host ("Done: {0} ({1:N1} MB)" -f $f, ((Get-Item $f).Length / 1MB))
}
Write-Host 'Run the setup (or install.ps1) to install this build on this PC.'
