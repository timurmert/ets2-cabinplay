# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · **Polski** · [Français](README.fr.md) · [Español](README.es.md) · [Русский](README.ru.md)

Działający ekran multimedialny w kabinie ciężarówki w **Euro Truck Simulator 2**. Oglądaj YouTube, słuchaj muzyki i śledź mapę GPS z gry na tablecie przy przedniej szybie, podczas jazdy.

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
| `Ctrl+Alt+C` | Obsługa ekranu z poziomu gry myszą i klawiaturą (wyjście: `Esc`) |
| `Ctrl+Alt+N` | Mapa nawigacji |
| `Ctrl+Alt+H` | Ekran główny |
| `Ctrl+Alt+Spacja` | Odtwarzaj / wstrzymaj |
| `Ctrl+Alt+→` `←` | Następny / poprzedni |
| `Ctrl+Alt+↑` `↓` | 10 sekund do przodu / do tyłu |
| `Ctrl+Alt+F` | Wideo na pełnym ekranie |

Odtwarzanie wstrzymuje się, gdy zatrzymasz grę lub wyłączysz zapłon. Język zmienisz w **Ustawieniach** na ekranie.

Jeśli kierujesz myszą, ustaw `control_mouse=0` w `cabinplay.ini` (w folderze gry `bin\win_x64\plugins`): mysz zostaje wtedy przy ciężarówce, a kursor na ekranie przesuwają klawisze strzałek.

## Wymagania

- Euro Truck Simulator 2 **1.61**, Windows 10 lub 11
- DLC **Cabin Accessories** (zapewnia gniazdo montażowe)
- Tryb jednoosobowy: nie działa z TruckersMP
- Pasuje do 23 ciężarówek; nie do DAF XF Electric, Renault E-Tech T i Scania S 2024e

## Pomoc i społeczność

Pytania, problemy i nowości: **[dołącz do naszego Discorda](https://discord.gg/rhGEbsu3zy)**.

Aby zgłosić problem, otwórz **Ustawienia** na ekranie, naciśnij **Utwórz raport o błędzie** i wyślij plik zip zapisany na pulpicie.

## Kontakt

**Współpraca i sponsoring:** napisz na **[contact@hydrabon.com](mailto:contact@hydrabon.com)** albo odezwij się na [Discordzie](https://discord.gg/rhGEbsu3zy).

**Skargi i sugestie:** napisz na ten sam adres albo daj nam znać na [Discordzie](https://discord.gg/rhGEbsu3zy).

## Autorzy i licencja

Stworzone przez **HydRaboN**, nasz serwer społeczności i zespół programistów. CabinPlay jest darmowy i otwartoźródłowy na [licencji MIT](LICENSE). Zawiera [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay to nieoficjalny projekt fanowski. Nie jest powiązany z SCS Software, Google/YouTube, Twitch ani Apple i nie jest przez nie wspierany.
