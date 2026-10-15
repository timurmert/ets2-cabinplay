# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · **Deutsch** · [Polski](README.pl.md) · [Français](README.fr.md) · [Español](README.es.md) · [Русский](README.ru.md)

Ein funktionierender Medienbildschirm für deine Lkw-Kabine in **Euro Truck Simulator 2**. Schau YouTube, höre Musik und folge der GPS-Karte des Spiels auf einem Tablet an der Windschutzscheibe, während du fährst.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/releases/latest"><img src="https://img.shields.io/github/downloads/timurmert/ets2-cabinplay/total?style=for-the-badge&color=2ea44f&label=Downloads" alt="Downloads"></a>
  <a href="https://www.virustotal.com/gui/file/f00ab36187b23f011cbd462f89e62cddf2b321ec3e3c60b960e188139d847610"><img src="https://img.shields.io/static/v1?style=for-the-badge&color=394EFF&logo=virustotal&logoColor=white&message=VirusTotal&label=Virenscan" alt="Virenscan: VirusTotal"></a>
</p>

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4"><img src="docs/media/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4">Das ganze Video mit Ton ansehen</a></sub>
</p>

<p align="center">
  <img src="docs/media/driving-day.jpg" width="49%" alt="">
  <img src="docs/media/home-screen.jpg" width="49%" alt="">
</p>
<p align="center">
  <img src="docs/media/cabin-night.jpg" width="32.5%" alt="">
  <img src="docs/media/navigation.jpg" width="32.5%" alt="">
  <img src="docs/media/driving-night.jpg" width="32.5%" alt="">
</p>
<p align="center">
  <img src="docs/media/cabin-places.jpg" width="32.5%" alt="">
  <img src="docs/media/cabin-places-side.jpg" width="32.5%" alt="">
  <img src="docs/media/bunk-tv.jpg" width="32.5%" alt="">
</p>
<p align="center">
  <img src="docs/media/truck-app.jpg" width="32.5%" alt="">
  <img src="docs/media/mirrors.jpg" width="32.5%" alt="">
  <img src="docs/media/driving-football.jpg" width="32.5%" alt="">
</p>

## Download

**[Lade die neueste Version herunter](https://github.com/timurmert/ets2-cabinplay/releases/latest)** und nimm die Datei `CabinPlay-x.y.z.zip`.

> 🛡️ **Virenscan:** CabinPlay wurde auf VirusTotal von Dutzenden Antivirenprogrammen geprüft. **[Zum Prüfbericht](https://www.virustotal.com/gui/file/f00ab36187b23f011cbd462f89e62cddf2b321ec3e3c60b960e188139d847610)**

## Installation

Schließe das Spiel und entpacke die Datei. Wähle dann eine Variante:

**A) Automatisch.** Doppelklicke auf **Install.cmd**. Meldet Windows, der Computer wurde geschützt, klicke auf *Weitere Informationen* und dann auf *Trotzdem ausführen* (die Dateien sind nicht signiert). `Install.cmd` startet nur `data\install.ps1`, ein Skript in Klartext, das du lesen kannst.

**B) Von Hand.** Es wird kein Skript ausgeführt; du kopierst drei Dinge aus dem Ordner `data`:

1. `cabinplay.scs` nach `Dokumente\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` und `plugin\cabinplay.ini` in den Ordner `bin\win_x64\plugins` des Spiels (lege `plugins` an, falls er fehlt). So findest du den Spielordner: in Steam Rechtsklick auf das Spiel > *Verwalten* > *Lokale Dateien durchsuchen*.
3. den Ordner `app` an einen beliebigen Ort; starte dann einmal `CabinPlay.exe` darin. Danach startet es von selbst mit dem Spiel.

**Danach im Spiel:**

1. Starte das Spiel und bestätige den Hinweis zu den "SDK-Funktionen", der einmal erscheint.
2. Aktiviere im **Mod-Manager** **CabinPlay Screen**.
3. Baue in einer Lkw-Werkstatt **CabinPlay Screen** in einen Zubehörplatz an der Windschutzscheibe ein. Links gibt es zwei Varianten, *Large* (11", am Arm) und *Compact* (10", an der Scheibe); im mittleren Platz gibt es *Centre*, zum Fahrer gedreht (mit einer eigenen Variante für Rechtslenker).

Das war's. Der Bildschirm schaltet sich mit der Zündung ein. Zum Entfernen doppelklicke auf **Uninstall.cmd** oder lösche die von Hand kopierten Dateien.

## Bedienung

| Tastenkürzel | Funktion |
|---|---|
| `Ctrl+Alt+C` | Den Bildschirm im Spiel mit Maus und Tastatur bedienen; das Spiel pausiert solange (verlassen mit `Esc`) |
| `Ctrl+Alt+N` | Navigationskarte |
| `Ctrl+Alt+H` | Startbildschirm |
| `Ctrl+Alt+Leertaste` | Wiedergabe / Pause |
| `Ctrl+Alt+→` `←` | Weiter / zurück |
| `Ctrl+Alt+↑` `↓` | 10 Sekunden vor / zurück |
| `Ctrl+Alt+F` | Video im Vollbild |
| `Ctrl+Alt+PgUp` `PgDn` | Vorherige / nächste offene App |
| `Ctrl+Alt+S` | Zwei Apps nebeneinander, ein / aus |
| `Ctrl+Alt+X` | Die beiden Seiten tauschen |

Die Wiedergabe pausiert, wenn du das Spiel pausierst oder die Zündung ausschaltest. Die Sprache findest du unter **Einstellungen** auf dem Bildschirm. Hast du CabinPlay in den Einstellungen beendet, startet `Ctrl+Alt+C` es wieder.

Die App **Lkw** zeigt die Schäden an Lkw und Auflieger, die Ladung, den Kraftstoff und die Anzeigen. Die Ctrl+Alt-Tastenkürzel funktionieren auch, wenn das Spiel im Vordergrund ist, und ihre Tasten erreichen das Spiel nicht: Ein Tastenkürzel hupt nicht und schaltet nichts am Lkw.

Die App **Spiegel** zeigt die Spiegel des Lkw auf dem Bildschirm: den linken Spiegel, seinen Weitwinkelspiegel und, falls vorhanden, den Frontspiegel. Tippe auf einen, um ihn zu vergrößern.

Die App **Browser** öffnet jede Website: Gib oben in der Leiste eine Adresse oder eine Suche ein und tippe auf den Stern, um eine Seite auf der Startseite zu behalten. Auf Websites bleibst du angemeldet, und `Ctrl+Alt+F` zeigt das Video einer Seite im Vollbild. Zum Tippen brauchst du den Steuermodus (`Ctrl+Alt+C`).

**Nebeneinander** zeigt zwei Apps gleichzeitig, zum Beispiel links die Karte und rechts Musik: Tippe im Dock auf die Teilen-Taste und wähle für jede Hälfte eine App. Unter **Einstellungen** kannst du einen Hintergrund wählen, auch aus eigenen Bildern, die Tasten der Tastenkürzel ändern und jedes davon auf einen Knopf deines Lenkrads oder Gamepads legen.

**Weitere Plätze:** Außer an der Windschutzscheibe gibt es den Bildschirm in der Werkstatt auch für das *Dashboard* (Armaturenbrett), den *Air vent* (Lüftungsgitter), *Overhead* (Dach), den *Passenger seat* (Beifahrersitz), die *Console* (Mittelkonsole), den *Cup holder* (Getränkehalter, neuere Lkw) und die *Passenger side* (Beifahrerseite der Scheibe). Zum Zuschauen von der Liege gibt es den 27"-**CabinPlay TV**: an der Rückwand (Platz des Lichtpaneels), flach oder zum Kopfkissen ausgeschwenkt, oder an der Windschutzscheibe, flach oder zur Liege gedreht. Im Scania S dreh den Kopf über den Blick nach hinten hinaus weiter nach rechts: Die Kamera wandert auf die Liege und bleibt am Kopfkissen stehen, mit Blick auf den Fernseher.

## Voraussetzungen

- Euro Truck Simulator 2 **1.61**, Windows 10 oder 11
- Kein DLC nötig
- Einzelspieler: funktioniert nicht mit TruckersMP
- Passt in 25 Lkw; nicht in den Renault E-Tech T

## Hilfe und Community

Fragen, Probleme und Neuigkeiten: **[tritt unserem Discord bei](https://discord.gg/rhGEbsu3zy)**.

Um ein Problem zu melden, öffne **Einstellungen** auf dem Bildschirm, drücke **Fehlerbericht erstellen** und poste die Zip-Datei, die auf dem Desktop gespeichert wird.

**Das Video ruckelt oder friert ein?** Das Spiel lastet deine Grafikkarte voll aus. Stelle in den Grafikeinstellungen des Spiels die **Skalierung** auf 200 % oder weniger (meist ist 400 % die Ursache).

## Kontakt

**Zusammenarbeit und Sponsoring:** schreib an **[contact@hydrabon.com](mailto:contact@hydrabon.com)** oder erreiche uns auf [Discord](https://discord.gg/rhGEbsu3zy).

**Beschwerden und Vorschläge:** schreib an dieselbe Adresse oder melde dich auf [Discord](https://discord.gg/rhGEbsu3zy).

## Mitwirkende und Lizenz

Entwickelt von **HydRaboN**, unserem Community-Server und Software-Team. CabinPlay ist kostenlos und quelloffen unter der [MIT-Lizenz](LICENSE). Enthält [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay ist ein inoffizielles Fanprojekt. Es steht in keiner Verbindung zu SCS Software, Google/YouTube, Twitch oder Apple und wird von ihnen nicht unterstützt.
