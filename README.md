# CabinPlay

A working media screen for your truck cabin in **Euro Truck Simulator 2**. Watch YouTube, listen
to music and follow the game's GPS map on a tablet mounted at the windscreen, while you drive.

[Türkçe](README.tr.md)

## Download

**[Download the latest version](https://github.com/timurmert/ets2-cabinplay/releases/latest)** and
get the file named `CabinPlay-x.y.z.zip`.

## Install

1. Close the game.
2. Unzip the file and double-click **Install.cmd**.
   If Windows says it protected your PC, click *More info*, then *Run anyway* (the files are not
   code-signed).
3. Start the game and accept the "SDK features" notice it shows once.
4. In the **Mod Manager**, switch on **CabinPlay Screen**.
5. In a truck workshop, fit **CabinPlay Screen** in the left windscreen accessory slot. There are
   two versions: *Large* (11", on an arm) and *Compact* (10", at the glass).

That's it. The screen switches on with the truck's ignition.

To remove it, double-click **Uninstall.cmd**.

## Use

| Shortcut | What it does |
|---|---|
| `Ctrl+Alt+C` | Use the screen from inside the game with mouse and keyboard (`Esc` to leave) |
| `Ctrl+Alt+N` | Navigation map |
| `Ctrl+Alt+H` | Home screen |
| `Ctrl+Alt+Space` | Play / pause |
| `Ctrl+Alt+→` `←` | Next / previous |
| `Ctrl+Alt+↑` `↓` | 10 seconds forward / back |
| `Ctrl+Alt+F` | Video fullscreen |

Playback pauses when you pause the game or switch the ignition off. The language (English,
Turkish, German, Polish, French, Spanish, Russian) is under **Settings** on the screen.

If you steer with the mouse, set `control_mouse=0` in `cabinplay.ini` (in the game's
`bin\win_x64\plugins` folder): the mouse then stays with the truck and the arrow keys move the
screen's cursor.

## What you need

- Euro Truck Simulator 2 **1.61**, Windows 10 or 11
- The **Cabin Accessories** DLC (it provides the mounting slot)
- Single player: it does not work with TruckersMP
- Fits 23 trucks; not the DAF XF Electric, Renault E-Tech T or Scania S 2024e

## Help

Open **Settings** on the screen and press **Create error report**. It saves a zip to your desktop;
share that file when you ask for help.

## Building from source

Needs Python 3, MinGW-w64 gcc and the .NET 8 SDK on Windows.

```powershell
.\build.ps1      # builds the mod, plugin, app and dist\CabinPlay-<version>.zip
.\install.ps1    # installs that build on this PC
```

`tools\` generates the mod, `plugin\` is the game plugin, `app\` is the companion app with the
screen's interface in `app\ui\`. The version number lives in `VERSION`.

---

CabinPlay is an unofficial fan project. It is not affiliated with or endorsed by SCS Software,
Google/YouTube, Twitch or Apple.
