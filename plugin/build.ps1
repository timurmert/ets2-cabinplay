# Builds dist\plugin\cabinplay.dll and runs it against the test host.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$out = Join-Path $root 'dist\plugin'
$tmp = Join-Path $root 'build\plugin'
New-Item -ItemType Directory -Force $out, $tmp | Out-Null

$mh = Join-Path $here 'minhook'
$sources = @(
    (Join-Path $here 'cabinplay_plugin.c'),
    (Join-Path $mh 'src\hook.c'),
    (Join-Path $mh 'src\buffer.c'),
    (Join-Path $mh 'src\trampoline.c'),
    (Join-Path $mh 'src\hde\hde64.c')
)
$dll = Join-Path $out 'cabinplay.dll'
& gcc -O2 -Wall -Wextra -Wno-unused-parameter -shared -static -s `
    -I (Join-Path $mh 'include') -o $dll @sources -ld3d11 -ldxgi -ldxguid -luuid -ladvapi32
if ($LASTEXITCODE -ne 0) { throw 'plugin build failed' }

$ini = Join-Path $out 'cabinplay.ini'
if (-not (Test-Path $ini)) {
    Set-Content -Path $ini -Encoding ascii -Value @(
        '[cabinplay]',
        '; 0 switches the plugin off without removing it',
        'enabled=1',
        '; set to 1 if the picture on the in-cabin screen is upside down',
        'flip_v=0',
        '; Ctrl+Alt+<this key> toggles in-game control mode (virtual key code, 67 = C)',
        'control_key=67',
        '; cursor speed in control mode, percent',
        'cursor_speed=100',
        '; width of the screen shown in control mode, percent of the game window',
        'overlay_size=60',
        '; what moves the CabinPlay cursor in control mode:',
        ';   1 = the mouse (the game does not see the mouse meanwhile)',
        ';   0 = the arrow keys; the mouse stays with the game, e.g. for mouse steering.',
        ';       Enter clicks, Page Up / Page Down scroll, Shift+Enter types Enter.',
        'control_mouse=1',
        '; start the CabinPlay app together with the game (it closes again with the game)',
        'autostart=1',
        '; where the app is; filled in by the installer. If empty, the place the app was last',
        '; started from is used.',
        'app_path='
    )
}

$test = Join-Path $tmp 'test_host.exe'
& gcc -O1 -Wall -static -o $test (Join-Path $here 'test_host.c') -ld3d11 -ldxgi -ldinput8 -ldxguid -luuid
if ($LASTEXITCODE -ne 0) { throw 'test host build failed' }

# Test against a private copy so the log lands in build\, not next to the shipped DLL.
Copy-Item $dll (Join-Path $tmp 'cabinplay.dll') -Force
& $test (Join-Path $tmp 'cabinplay.dll')
if ($LASTEXITCODE -ne 0) { throw 'plugin self-test failed' }
Get-Item $dll | Select-Object Name, Length
