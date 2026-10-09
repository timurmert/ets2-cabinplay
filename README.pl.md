# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · **Polski** · [Français](README.fr.md) · [Español](README.es.md) · [Русский](README.ru.md)

Działający ekran multimedialny w kabinie ciężarówki w **Euro Truck Simulator 2**. Oglądaj YouTube, słuchaj muzyki i śledź mapę GPS z gry na tablecie przy przedniej szybie, podczas jazdy.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4"><img src="docs/media/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4">Obejrzyj cały film z dźwiękiem</a></sub>
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

## Pobieranie

**[Pobierz najnowszą wersję](https://github.com/timurmert/ets2-cabinplay/releases/latest)** i weź plik `CabinPlay-x.y.z.zip`.

## Instalacja

Zamknij grę i rozpakuj plik. Następnie wybierz jeden sposób:

**A) Automatycznie.** Kliknij dwukrotnie **Install.cmd**. Jeśli system Windows poinformuje, że ochronił komputer, kliknij *Więcej informacji*, a potem *Uruchom mimo to* (pliki nie są podpisane). `Install.cmd` uruchamia tylko `data\install.ps1`, zwykły skrypt tekstowy, który możesz przeczytać.

**B) Ręcznie.** Żaden skrypt nie jest uruchamiany; kopiujesz trzy rzeczy z folderu `data`:

1. `cabinplay.scs` do `Dokumenty\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` i `plugin\cabinplay.ini` do folderu gry `bin\win_x64\plugins` (utwórz `plugins`, jeśli go nie ma). Aby znaleźć folder gry: w Steam kliknij grę prawym przyciskiem > *Zarządzaj* > *Przeglądaj pliki lokalne*.
3. folder `app` w dowolne miejsce, a potem uruchom w nim raz `CabinPlay.exe`. Od tej pory będzie się uruchamiać razem z grą.

**Potem, w grze:**

1. Uruchom grę i zaakceptuj komunikat o "funkcjach SDK", który pojawia się raz.
2. W **Menedżerze modów** włącz **CabinPlay Screen**.
3. W warsztacie zamontuj **CabinPlay Screen** w gnieździe akcesoriów po lewej stronie przedniej szyby. Są dwie wersje: *Large* (11", na ramieniu) i *Compact* (10", przy szybie).

To wszystko. Ekran włącza się razem z zapłonem ciężarówki. Aby usunąć, kliknij dwukrotnie **Uninstall.cmd** albo usuń pliki skopiowane ręcznie.

## Obsługa

| Skrót | Co robi |
|---|---|
| `Ctrl+Alt+C` | Obsługa ekranu z poziomu gry myszą i klawiaturą; gra jest w tym czasie wstrzymana (wyjście: `Esc`) |
| `Ctrl+Alt+N` | Mapa nawigacji |
| `Ctrl+Alt+H` | Ekran główny |
| `Ctrl+Alt+Spacja` | Odtwarzaj / wstrzymaj |
| `Ctrl+Alt+→` `←` | Następny / poprzedni |
| `Ctrl+Alt+↑` `↓` | 10 sekund do przodu / do tyłu |
| `Ctrl+Alt+F` | Wideo na pełnym ekranie |

Odtwarzanie wstrzymuje się, gdy zatrzymasz grę lub wyłączysz zapłon. Język zmienisz w **Ustawieniach** na ekranie.

## Wymagania

- Euro Truck Simulator 2 **1.61**, Windows 10 lub 11
- Nie wymaga DLC
- Tryb jednoosobowy: nie działa z TruckersMP
- Pasuje do 25 ciężarówek; nie do Renault E-Tech T

## Pomoc i społeczność

Pytania, problemy i nowości: **[dołącz do naszego Discorda](https://discord.gg/rhGEbsu3zy)**.

Aby zgłosić problem, otwórz **Ustawienia** na ekranie, naciśnij **Utwórz raport o błędzie** i wyślij plik zip zapisany na pulpicie.

**Wideo się zacina lub zamiera?** Gra zajmuje całą kartę graficzną. W ustawieniach grafiki gry zmniejsz **Skalowanie** do 200% lub mniej (najczęstszą przyczyną jest 400%).

## Kontakt

**Współpraca i sponsoring:** napisz na **[contact@hydrabon.com](mailto:contact@hydrabon.com)** albo odezwij się na [Discordzie](https://discord.gg/rhGEbsu3zy).

**Skargi i sugestie:** napisz na ten sam adres albo daj nam znać na [Discordzie](https://discord.gg/rhGEbsu3zy).

## Autorzy i licencja

Stworzone przez **HydRaboN**, nasz serwer społeczności i zespół programistów. CabinPlay jest darmowy i otwartoźródłowy na [licencji MIT](LICENSE). Zawiera [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay to nieoficjalny projekt fanowski. Nie jest powiązany z SCS Software, Google/YouTube, Twitch ani Apple i nie jest przez nie wspierany.
