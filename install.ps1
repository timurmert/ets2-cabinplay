# Installs CabinPlay, or removes it with -Uninstall.
#
#   mod      -> Documents\Euro Truck Simulator 2\mod\cabinplay.scs
#   plugin   -> <game>\bin\win_x64\plugins\cabinplay.dll (+ .ini)
#   app      -> %LocalAppData%\Programs\CabinPlay
#
# Runs from a release package (the files sit next to this script; people start it
# through Install.cmd) and from a source checkout after build.ps1 (the files are in dist\).
param(
    [string]$GameDir,
    [switch]$Uninstall
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = if (Test-Path (Join-Path $here 'cabinplay.scs')) { $here } else { Join-Path $here 'dist' }

function Stop-WithMessage([string]$message) {
    Write-Host ''
    Write-Host "  $message" -ForegroundColor Red
    exit 1
}

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

function Test-GameDir([string]$dir) {
    return $dir -and (Test-Path (Join-Path $dir 'bin\win_x64\eurotrucks2.exe'))
}

Write-Host ''
Write-Host '  CabinPlay' -ForegroundColor Cyan
Write-Host ''

if (Get-Process eurotrucks2 -ErrorAction SilentlyContinue) {
    Stop-WithMessage 'Euro Truck Simulator 2 is running. Close the game and start this again.'
}

if (-not $GameDir) { $GameDir = Find-GameDir }
if (-not (Test-GameDir $GameDir)) {
    Write-Host '  Could not find Euro Truck Simulator 2 automatically.'
    Write-Host '  In Steam: right-click the game > Manage > Browse local files, then copy the folder path.'
    $GameDir = (Read-Host '  Paste the game folder here').Trim().Trim('"')
    if (-not (Test-GameDir $GameDir)) { Stop-WithMessage 'That folder does not contain Euro Truck Simulator 2.' }
}

# Our own app may be running in the background (it starts with the game).
$app = Get-Process CabinPlay -ErrorAction SilentlyContinue
if ($app) {
    $app | ForEach-Object { [void]$_.CloseMainWindow() }
    Start-Sleep -Seconds 2
    Get-Process CabinPlay -ErrorAction SilentlyContinue | Stop-Process -Force
}

$documents = [Environment]::GetFolderPath('MyDocuments')
$modDir = Join-Path $documents 'Euro Truck Simulator 2\mod'
$pluginDir = Join-Path $GameDir 'bin\win_x64\plugins'
$appDir = Join-Path $env:LOCALAPPDATA 'Programs\CabinPlay'
$dataDir = Join-Path $env:LOCALAPPDATA 'CabinPlay'
$modFile = Join-Path $modDir 'cabinplay.scs'
$pluginDll = Join-Path $pluginDir 'cabinplay.dll'
$pluginIni = Join-Path $pluginDir 'cabinplay.ini'
$pluginLog = Join-Path $pluginDir 'cabinplay.log'
$shortcuts = @(
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'CabinPlay.lnk'),
    (Join-Path ([Environment]::GetFolderPath('Programs')) 'CabinPlay.lnk')
)

if ($Uninstall) {
    foreach ($f in @($modFile, $pluginDll, $pluginIni, $pluginLog) + $shortcuts) {
        if (Test-Path $f) { Remove-Item $f -Force }
    }
    if (Test-Path $appDir) { Remove-Item $appDir -Recurse -Force }
    # Leave the plugins folder alone if other plugins live there.
    if ((Test-Path $pluginDir) -and -not (Get-ChildItem $pluginDir -Force)) { Remove-Item $pluginDir }
    Write-Host '  CabinPlay was removed.' -ForegroundColor Green
    Write-Host '  Your sign-ins and settings were kept in' $dataDir
    Write-Host '  In the game, sell the screen in the truck workshop and switch the mod off in the Mod Manager.'
    exit 0
}

foreach ($needed in 'cabinplay.scs', 'plugin\cabinplay.dll', 'app\CabinPlay.exe') {
    if (-not (Test-Path (Join-Path $source $needed))) {
        Stop-WithMessage "The package is incomplete: $needed is missing. Download it again, or run build.ps1 in a source checkout."
    }
}

# --- app
if (Test-Path $appDir) { Remove-Item $appDir -Recurse -Force }
New-Item -ItemType Directory -Force $appDir | Out-Null
Copy-Item (Join-Path $source 'app\*') $appDir -Recurse -Force
$appExe = Join-Path $appDir 'CabinPlay.exe'

# --- mod and plugin
New-Item -ItemType Directory -Force $modDir, $pluginDir | Out-Null

# Before it was renamed the project installed these; two copies would both load.
$oldIni = Join-Path $pluginDir 'ets2_carplay.ini'
if ((Test-Path $oldIni) -and -not (Test-Path $pluginIni)) {
    (Get-Content $oldIni) -replace '^\[carplay\]', '[cabinplay]' | Set-Content -Path $pluginIni -Encoding ascii
}
$old = @(
    (Join-Path $modDir 'ets2_carplay.scs'),
    (Join-Path $pluginDir 'ets2_carplay.dll'),
    (Join-Path $pluginDir 'ets2_carplay.log'),
    $oldIni,
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'ETS2 CarPlay.lnk')
)
foreach ($f in $old) {
    if (Test-Path $f) { Remove-Item $f -Force }
}

Copy-Item (Join-Path $source 'cabinplay.scs') $modFile -Force
Copy-Item (Join-Path $source 'plugin\cabinplay.dll') $pluginDll -Force

$defaultIni = Join-Path $source 'plugin\cabinplay.ini'
if (-not (Test-Path $pluginIni)) {
    Copy-Item $defaultIni $pluginIni
} else {
    # Keep the user's values; add settings that are new in this version, with their comments.
    $have = Get-Content $pluginIni
    $pending = @()
    foreach ($line in Get-Content $defaultIni) {
        if ($line -match '^\s*;') { $pending += $line; continue }
        if ($line -match '^\s*([A-Za-z_]+)\s*=' -and -not ($have -match ('^\s*' + $Matches[1] + '\s*='))) {
            Add-Content -Path $pluginIni -Encoding ascii -Value ($pending + $line)
        }
        $pending = @()
    }
}
# The plugin starts the app with the game; tell it where the app is.
$ansi = [Text.Encoding]::Default
$lines = [IO.File]::ReadAllLines($pluginIni, $ansi) | ForEach-Object {
    if ($_ -match '^\s*app_path\s*=') { "app_path=$appExe" } else { $_ }
}
[IO.File]::WriteAllLines($pluginIni, $lines, $ansi)

# Files from a downloaded zip are marked as coming from the internet, which makes
# Windows hesitate to start them.
Get-ChildItem $appDir -Recurse -File | Unblock-File
Unblock-File $pluginDll, $modFile

# --- data folder (keeps site sign-ins); lets the app find the plugin's log for error reports
$oldDataDir = Join-Path $env:LOCALAPPDATA 'ETS2CarPlay'
if ((Test-Path $oldDataDir) -and -not (Test-Path $dataDir)) { Rename-Item $oldDataDir 'CabinPlay' }
New-Item -ItemType Directory -Force $dataDir | Out-Null
@{ gameDir = $GameDir } | ConvertTo-Json | Set-Content -Path (Join-Path $dataDir 'install.json') -Encoding utf8

# --- shortcuts
$shell = New-Object -ComObject WScript.Shell
foreach ($path in $shortcuts) {
    $link = $shell.CreateShortcut($path)
    $link.TargetPath = $appExe
    $link.WorkingDirectory = $appDir
    $link.Description = 'CabinPlay'
    $link.Save()
}

Write-Host '  Installed.' -ForegroundColor Green
Write-Host "    game    $GameDir"
Write-Host "    mod     $modFile"
Write-Host "    app     $appDir"

# The screen's pages are shown with Microsoft's WebView2, part of Windows 11 and most Windows 10 PCs.
$webview = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}'
$hasWebView = @(
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\$webview",
    "HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\$webview",
    "HKCU:\SOFTWARE\Microsoft\EdgeUpdate\Clients\$webview"
) | Where-Object { Test-Path $_ }
if (-not $hasWebView) {
    Write-Host ''
    Write-Host '  One more thing: the "WebView2 Runtime" from Microsoft is missing on this PC.' -ForegroundColor Yellow
    Write-Host '  Install it from https://go.microsoft.com/fwlink/p/?LinkId=2124703 before starting the game.'
}

Write-Host ''
Write-Host '  What to do now:'
Write-Host '    1. Start the game. Accept the "SDK features" notice it shows once.'
Write-Host '    2. In the Mod Manager, switch on "CabinPlay Screen".'
Write-Host '    3. In a truck workshop, fit "CabinPlay Screen" in the left windscreen accessory slot.'
Write-Host ''
