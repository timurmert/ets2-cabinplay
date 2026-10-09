# Installs (or with -Uninstall removes) the three parts of CabinPlay:
#   mod      -> Documents\Euro Truck Simulator 2\mod\cabinplay.scs
#   plugin   -> <game>\bin\win_x64\plugins\cabinplay.dll (+ .ini)
#   app      -> desktop shortcut to dist\app\CabinPlay.exe
param(
    [string]$GameDir,
    [switch]$Uninstall
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'

function Find-GameDir {
    $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    if (-not $steam) { return $null }
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    if (-not (Test-Path $vdf)) { return $null }
    foreach ($line in Get-Content $vdf) {
        if ($line -match '"path"\s+"(.+)"') {
            $candidate = Join-Path ($Matches[1] -replace '\\\\', '\') 'steamapps\common\Euro Truck Simulator 2'
            if (Test-Path (Join-Path $candidate 'bin\win_x64\eurotrucks2.exe')) { return $candidate }
        }
    }
    return $null
}

if (-not $GameDir) { $GameDir = Find-GameDir }
if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir 'bin\win_x64\eurotrucks2.exe'))) {
    throw 'Euro Truck Simulator 2 was not found. Pass its folder with -GameDir.'
}
if (Get-Process eurotrucks2 -ErrorAction SilentlyContinue) {
    throw 'Close Euro Truck Simulator 2 first: the game locks the plugin while it runs.'
}

$modDir = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Euro Truck Simulator 2\mod'
$pluginDir = Join-Path $GameDir 'bin\win_x64\plugins'
$shortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'CabinPlay.lnk'
$modFile = Join-Path $modDir 'cabinplay.scs'
$pluginFiles = 'cabinplay.dll', 'cabinplay.ini', 'cabinplay.log' | ForEach-Object { Join-Path $pluginDir $_ }

if ($Uninstall) {
    foreach ($f in @($modFile, $shortcut) + $pluginFiles) {
        if (Test-Path $f) { Remove-Item $f -Force; Write-Host "removed  $f" }
    }
    # Leave the plugins folder alone if other plugins live there.
    if ((Test-Path $pluginDir) -and -not (Get-ChildItem $pluginDir -Force)) { Remove-Item $pluginDir }
    Write-Host 'Uninstalled. Sell the screen in the truck workshop before removing the mod from your profile.'
    return
}

# build.ps1 leaves a fresh app build next to the live one, which may have been running.
$staged = Join-Path $dist 'app.new'
$live = Join-Path $dist 'app'
if (Test-Path (Join-Path $staged 'CabinPlay.exe')) {
    if (Get-Process CabinPlay -ErrorAction SilentlyContinue) {
        throw 'Close the CabinPlay app first (tray icon > Exit): it is being updated.'
    }
    if (Test-Path $live) { Remove-Item $live -Recurse -Force }
    Rename-Item $staged 'app'
    Write-Host "app      $live (updated)"
}

foreach ($needed in 'cabinplay.scs', 'plugin\cabinplay.dll', 'app\CabinPlay.exe') {
    if (-not (Test-Path (Join-Path $dist $needed))) { throw "dist\$needed is missing. Run build.ps1 first." }
}

New-Item -ItemType Directory -Force $modDir, $pluginDir | Out-Null

# Before it was renamed the project installed these; two copies would both load.
$oldIni = Join-Path $pluginDir 'ets2_carplay.ini'
if ((Test-Path $oldIni) -and -not (Test-Path $pluginFiles[1])) {
    (Get-Content $oldIni) -replace '^\[carplay\]', '[cabinplay]' | Set-Content -Path $pluginFiles[1] -Encoding ascii
}
$old = @(
    (Join-Path $modDir 'ets2_carplay.scs'),
    (Join-Path $pluginDir 'ets2_carplay.dll'),
    (Join-Path $pluginDir 'ets2_carplay.log'),
    $oldIni,
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'ETS2 CarPlay.lnk')
)
foreach ($f in $old) {
    if (Test-Path $f) { Remove-Item $f -Force; Write-Host "removed  $f (old name)" }
}

Copy-Item (Join-Path $dist 'cabinplay.scs') $modFile -Force
Write-Host "mod      $modFile"
Copy-Item (Join-Path $dist 'plugin\cabinplay.dll') $pluginFiles[0] -Force
$defaultIni = Join-Path $dist 'plugin\cabinplay.ini'
if (-not (Test-Path $pluginFiles[1])) {
    Copy-Item $defaultIni $pluginFiles[1]
} else {
    # Keep the user's values; add settings that are new in this version, with their comments.
    $have = Get-Content $pluginFiles[1]
    $pending = @()
    foreach ($line in Get-Content $defaultIni) {
        if ($line -match '^\s*;') { $pending += $line; continue }
        if ($line -match '^\s*([A-Za-z_]+)\s*=' -and -not ($have -match ('^\s*' + $Matches[1] + '\s*='))) {
            Add-Content -Path $pluginFiles[1] -Encoding ascii -Value ($pending + $line)
        }
        $pending = @()
    }
}

# The plugin starts the app with the game; tell it where the app is.
$appExe = Join-Path $dist 'app\CabinPlay.exe'
$ansi = [Text.Encoding]::Default
$lines = [IO.File]::ReadAllLines($pluginFiles[1], $ansi) | ForEach-Object {
    if ($_ -match '^\s*app_path\s*=') { "app_path=$appExe" } else { $_ }
}
[IO.File]::WriteAllLines($pluginFiles[1], $lines, $ansi)
Write-Host "plugin   $($pluginFiles[0])"

# Lets the app find the plugin's log when it collects diagnostics.
$dataDir = Join-Path $env:LOCALAPPDATA 'CabinPlay'
$oldDataDir = Join-Path $env:LOCALAPPDATA 'ETS2CarPlay'
if ((Test-Path $oldDataDir) -and -not (Test-Path $dataDir)) {
    Rename-Item $oldDataDir 'CabinPlay'   # keeps site sign-ins and the window position
}
New-Item -ItemType Directory -Force $dataDir | Out-Null
@{ gameDir = $GameDir } | ConvertTo-Json | Set-Content -Path (Join-Path $dataDir 'install.json') -Encoding utf8

$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut)
$link.TargetPath = Join-Path $dist 'app\CabinPlay.exe'
$link.WorkingDirectory = Join-Path $dist 'app'
$link.Description = 'CabinPlay'
$link.Save()
Write-Host "shortcut $shortcut"
