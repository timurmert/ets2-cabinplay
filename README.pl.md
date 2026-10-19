# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · **Polski** · [Français](README.fr.md) · [Español](README.es.md) · [Русский](README.ru.md)

Działający ekran multimedialny w kabinie ciężarówki w **Euro Truck Simulator 2**. Oglądaj YouTube, słuchaj muzyki i śledź mapę GPS z gry na tablecie przy przedniej szybie, podczas jazdy.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/releases/latest"><img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Ftimurmert%2Fets2-cabinplay%2Fbadges%2Fdownloads.json&style=for-the-badge&logo=github&logoColor=white&labelColor=24292f&color=2ea44f&label=POBRANIA" alt="Pobrania"></a>
  <a href="https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56"><img src="https://img.shields.io/static/v1?style=for-the-badge&logo=virustotal&logoColor=white&labelColor=394EFF&color=2ea44f&label=VirusTotal&message=0/68%20CZYSTY" alt="VirusTotal: 0/68 czysty"></a>
</p>

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

<p align="center">
  <a href="https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56"><img src="docs/media/virustotal.png" width="100%" alt="VirusTotal: 0/68 czysty"></a><br>
  <sub>🛡️ Żaden z 68 silników antywirusowych na VirusTotal nie oznaczył CabinPlay. <a href="https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56">Zobacz raport</a></sub>
</p>

## Pobieranie

**[Pobierz najnowszą wersję](https://github.com/timurmert/ets2-cabinplay/releases/latest)** i weź plik `CabinPlay-Setup-x.y.z.exe`.

> 🛡️ **Skan antywirusowy:** instalator został sprawdzony na VirusTotal przez 68 silników antywirusowych i żaden go nie oznaczył. **[Zobacz raport](https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56)**

## Instalacja

Zamknij grę, a potem wybierz jeden sposób:

**A) Instalator (zalecane).** Pobierz **CabinPlay-Setup-x.y.z.exe** i uruchom go. Sam znajdzie grę i nie wymaga uprawnień administratora. Jeśli system Windows poinformuje, że ochronił komputer, kliknij *Więcej informacji*, a potem *Uruchom mimo to* (instalator nie jest podpisany).

**B) Ręcznie.** Zamiast tego pobierz **CabinPlay-x.y.z-manual-install.zip**, rozpakuj go i skopiuj z niego trzy rzeczy:

1. `cabinplay.scs` do `Dokumenty\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` i `plugin\cabinplay.ini` do folderu gry `bin\win_x64\plugins` (utwórz `plugins`, jeśli go nie ma). Aby znaleźć folder gry: w Steam kliknij grę prawym przyciskiem > *Zarządzaj* > *Przeglądaj pliki lokalne*.
3. folder `app` w dowolne miejsce, a potem uruchom w nim raz `CabinPlay.exe`. Od tej pory będzie się uruchamiać razem z grą.

**Potem, w grze:**

1. Uruchom grę i zaakceptuj komunikat o "funkcjach SDK", który pojawia się raz.
2. W **Menedżerze modów** włącz **CabinPlay Screen**.
3. W warsztacie zamontuj **CabinPlay Screen** w gnieździe akcesoriów na przedniej szybie. Po lewej są trzy wersje: *Large* (13") i *Medium* (11") na ramieniu oraz *Compact* (10", przy szybie); w środkowym gnieździe jest *Centre*, zwrócony w stronę kierowcy (z osobną wersją dla kabin z kierownicą po prawej).

To wszystko. Ekran włącza się razem z zapłonem ciężarówki. Zainstalowany instalatorem CabinPlay sam się aktualizuje: nową wersję pobiera w trakcie gry i instaluje po jej zamknięciu i pokazuje to na ekranie; w *Ustawienia* > *Aktualizacje* możesz kazać mu najpierw pytać albo to wyłączyć. Aby usunąć CabinPlay, otwórz w Windows *Ustawienia* > *Aplikacje* albo uruchom `Uninstall.exe` w `%LocalAppData%\Programs\CabinPlay`; jeśli instalacja była ręczna, usuń skopiowane pliki.

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
| `Ctrl+Alt+PgUp` `PgDn` | Poprzednia / następna otwarta aplikacja |
| `Ctrl+Alt+S` | Dwie aplikacje obok siebie, wł. / wył. |
| `Ctrl+Alt+X` | Zamień strony miejscami |

Odtwarzanie wstrzymuje się, gdy zatrzymasz grę lub wyłączysz zapłon. Język zmienisz w **Ustawieniach** na ekranie. Jeśli zamkniesz CabinPlay w ustawieniach, `Ctrl+Alt+C` uruchomi go ponownie.

Aplikacja **Ciężarówka** pokazuje uszkodzenia ciężarówki i naczepy, ładunek, paliwo i wskaźniki. Skróty Ctrl+Alt działają także wtedy, gdy gra jest na pierwszym planie, a ich klawisze nie trafiają do gry, więc skrót nie włączy klaksonu ani niczego w ciężarówce.

Aplikacja **Lusterka** pokazuje na ekranie lusterka ciężarówki: lewe, lewe szerokokątne oraz, jeśli jest, przednie. Dotknij jednego, aby je powiększyć.

Aplikacja **Przeglądarka** otwiera dowolną stronę: wpisz adres lub wyszukiwanie w pasku u góry i naciśnij gwiazdkę, aby zachować stronę na stronie startowej. W serwisach pozostajesz zalogowany, a `Ctrl+Alt+F` włącza pełny ekran dla wideo na stronie. Do pisania potrzebny jest tryb sterowania (`Ctrl+Alt+C`).

Widok **obok siebie** pokazuje dwie aplikacje naraz, na przykład mapę po lewej i muzykę lub przeglądarkę po prawej: naciśnij przycisk podziału w doku i wybierz aplikację dla każdej połowy. W **Ustawieniach** możesz wybrać tło, także z własnych obrazów, zmienić klawisze skrótów i przypisać każdy z nich do przycisku na kierownicy lub padzie.

**Więcej miejsc:** poza przednią szybą warsztat ma ekran także na *Dashboard* (deska rozdzielcza), *Air vent* (kratka nawiewu), *Overhead* (sufit), *Passenger seat* (fotel pasażera), *Console* (konsola), *Cup holder* (uchwyt na kubek, nowsze ciężarówki) i *Passenger side* (strona pasażera na szybie). Do oglądania z leżanki jest 27-calowy **CabinPlay TV**: na tylnej ścianie (gniazdo panelu świetlnego), płasko lub wysunięty w stronę poduszki, albo na przedniej szybie, płasko lub zwrócony do leżanki. W Scanii S obracaj głowę w prawo dalej, za widok do tyłu: kamera przesunie się na leżankę i zatrzyma przy poduszce, zwrócona do telewizora.

## Wymagania

- Euro Truck Simulator 2 **1.61**, Windows 10 lub 11
- Nie wymaga DLC
- Tryb jednoosobowy: nie działa z TruckersMP
- Pasuje do 25 ciężarówek; nie do Renault E-Tech T

## Pomoc i społeczność

Pytania, problemy i nowości: **[dołącz do naszego Discorda](https://discord.gg/hydrabon)**.

Aby zgłosić problem, otwórz **Ustawienia** na ekranie, naciśnij **Utwórz raport o błędzie** i wyślij plik zip zapisany na pulpicie.

**Wideo się zacina lub zamiera?** Gra zajmuje całą kartę graficzną. W ustawieniach grafiki gry zmniejsz **Skalowanie** do 200% lub mniej (najczęstszą przyczyną jest 400%).

## Kontakt

**Współpraca i sponsoring:** napisz na **[contact@hydrabon.com](mailto:contact@hydrabon.com)** albo odezwij się na [Discordzie](https://discord.gg/hydrabon).

**Skargi i sugestie:** napisz na ten sam adres albo daj nam znać na [Discordzie](https://discord.gg/hydrabon).

## Autorzy i licencja

Stworzone przez **HydRaboN**, nasz serwer społeczności i zespół programistów. CabinPlay jest darmowy i otwartoźródłowy na [licencji GNU GPL v3.0 lub nowszej](LICENSE): możesz go udostępniać i zmieniać, ale wszystko, co z niego powstanie, musi pozostać otwartoźródłowe na tej samej licencji i wskazywać CabinPlay jako źródło. Zgodnie z sekcją 7 licencji nazwa *CabinPlay* i jej logo nie są objęte licencją; zmieniona wersja musi nosić inną nazwę. Zawiera [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay to nieoficjalny projekt fanowski. Nie jest powiązany z SCS Software, Google/YouTube, Twitch ani Apple i nie jest przez nie wspierany.
