"""Writes README.md and its translations (README.<code>.md) from one template.

Run it after changing a text below; the README files themselves are generated.
"""
import io
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = "https://github.com/timurmert/ets2-cabinplay"
DISCORD = "https://discord.gg/rhGEbsu3zy"
EMAIL = "contact@hydrabon.com"

# code -> (language name, file name)
LANGUAGES = {
    "en": ("English", "README.md"),
    "tr": ("Türkçe", "README.tr.md"),
    "de": ("Deutsch", "README.de.md"),
    "pl": ("Polski", "README.pl.md"),
    "fr": ("Français", "README.fr.md"),
    "es": ("Español", "README.es.md"),
    "ru": ("Русский", "README.ru.md"),
}

TEXT = {
    "en": dict(
        intro="A working media screen for your truck cabin in **Euro Truck Simulator 2**. Watch YouTube, listen to music and follow the game's GPS map on a tablet mounted at the windscreen, while you drive.",
        download="Download",
        release_download="Download **{zip}** below and unzip it.",
        download_body="**[Download the latest version]({repo}/releases/latest)** and get the file named `CabinPlay-x.y.z.zip`.",
        install="Install",
        install_intro="Close the game and unzip the file. Then pick one:",
        auto="**A) Automatic.** Double-click **Install.cmd**. If Windows says it protected your PC, click *More info*, then *Run anyway* (the files are not code-signed). `Install.cmd` only starts `data\\install.ps1`, a plain text script you can read.",
        manual="**B) By hand.** No script is run; you copy three things from the `data` folder:",
        manual_1="`cabinplay.scs` into `Documents\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` and `plugin\\cabinplay.ini` into the game's `bin\\win_x64\\plugins` folder (create `plugins` if it is not there). To find the game folder: in Steam, right-click the game > *Manage* > *Browse local files*.",
        manual_3="the `app` folder to anywhere you like, then start `CabinPlay.exe` in it once. From then on it starts with the game by itself.",
        then="**Then, in the game:**",
        game_1="Start the game and accept the \"SDK features\" notice it shows once.",
        game_2="In the **Mod Manager**, switch on **CabinPlay Screen**.",
        game_3="In a truck workshop, fit **CabinPlay Screen** in the left windscreen accessory slot. There are two versions: *Large* (11\", on an arm) and *Compact* (10\", at the glass).",
        done="That's it. The screen switches on with the truck's ignition. To remove it, double-click **Uninstall.cmd**, or delete the files you copied by hand.",
        use="Use",
        shortcut="Shortcut", does="What it does", space="Space",
        k_control="Use the screen from inside the game with mouse and keyboard; the game pauses meanwhile (`Esc` to leave)",
        k_nav="Navigation map", k_home="Home screen", k_play="Play / pause", k_next="Next / previous",
        k_seek="10 seconds forward / back", k_full="Video fullscreen",
        use_note="Playback pauses when you pause the game or switch the ignition off. The language is under **Settings** on the screen.",
        needs="What you need",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 or 11",
        need_2="No DLC needed",
        need_3="Single player: it does not work with TruckersMP",
        need_4="Fits 25 trucks; not the Renault E-Tech T",
        help="Help and community",
        help_body="Questions, problems and news: **[join our Discord]({discord})**.\n\nTo report a problem, open **Settings** on the screen, press **Create error report** and post the zip it saves to your desktop.",
        business="Contact",
        business_body="**Collaboration and sponsorship:** write to **[{email}](mailto:{email})** or reach us on [Discord]({discord}).\n\n**Complaints and suggestions:** write to the same address or tell us on [Discord]({discord}).",
        credits="Credits and licence",
        credits_body="Made by **HydRaboN**, our community server and software team. CabinPlay is free and open source under the [MIT licence](LICENSE). It includes [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay is an unofficial fan project. It is not affiliated with or endorsed by SCS Software, Google/YouTube, Twitch or Apple.",
    ),
    "tr": dict(
        intro="**Euro Truck Simulator 2** için tır kabininde gerçekten çalışan bir medya ekranı. Sürerken ön cama takılı tablette YouTube izle, müzik dinle ve oyunun GPS haritasını takip et.",
        download="İndir",
        release_download="Aşağıdaki **{zip}** dosyasını indir ve aç.",
        download_body="**[Son sürümü indir]({repo}/releases/latest)**; `CabinPlay-x.y.z.zip` adlı dosyayı al.",
        install="Kur",
        install_intro="Oyunu kapat ve zip'i aç. Sonra birini seç:",
        auto="**A) Otomatik.** **Install.cmd** dosyasına çift tıkla. Windows \"kişisel bilgisayarınızı korudu\" derse *Ek bilgi*, sonra *Yine de çalıştır* de (dosyalar imzalı değil). `Install.cmd` yalnızca `data\\install.ps1` dosyasını çalıştırır; bu, açıp okuyabileceğin düz metin bir betiktir.",
        manual="**B) Elle.** Hiçbir betik çalışmaz; `data` klasöründen üç şeyi kopyalarsın:",
        manual_1="`cabinplay.scs` dosyasını `Belgeler\\Euro Truck Simulator 2\\mod` klasörüne",
        manual_2="`plugin\\cabinplay.dll` ve `plugin\\cabinplay.ini` dosyalarını oyunun `bin\\win_x64\\plugins` klasörüne (`plugins` yoksa oluştur). Oyun klasörünü bulmak için: Steam'de oyuna sağ tıkla > *Yönet* > *Yerel dosyalara göz at*.",
        manual_3="`app` klasörünü istediğin bir yere; sonra içindeki `CabinPlay.exe` dosyasını bir kez çalıştır. Sonrasında oyunla birlikte kendiliğinden açılır.",
        then="**Sonra, oyunda:**",
        game_1="Oyunu başlat ve bir kez çıkan \"SDK özellikleri\" uyarısını onayla.",
        game_2="**Mod Yöneticisi**'nde **CabinPlay Screen**'i etkinleştir.",
        game_3="Serviste, sol ön cam aksesuar yuvasına **CabinPlay Screen**'i tak. İki sürüm var: *Large* (11\", kolda) ve *Compact* (10\", camın dibinde).",
        done="Bu kadar. Ekran tırın kontağıyla birlikte açılır. Kaldırmak için **Uninstall.cmd** dosyasına çift tıkla ya da elle kopyaladığın dosyaları sil.",
        use="Kullan",
        shortcut="Kısayol", does="Ne yapar", space="Boşluk",
        k_control="Ekranı oyunun içinden fare ve klavyeyle kullan; bu sırada oyun duraklar (çıkış: `Esc`)",
        k_nav="Navigasyon haritası", k_home="Ana ekran", k_play="Oynat / duraklat", k_next="Sonraki / önceki",
        k_seek="10 saniye ileri / geri", k_full="Videoyu tam ekran yap",
        use_note="Oyunu duraklattığında veya kontağı kapattığında çalan şey durur. Dil, ekrandaki **Ayarlar**'dan değişir.",
        needs="Gerekenler",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 veya 11",
        need_2="DLC gerekmez",
        need_3="Tek oyunculu: TruckersMP ile çalışmaz",
        need_4="25 tıra uyar; Renault E-Tech T'ye uymaz",
        help="Yardım ve topluluk",
        help_body="Sorular, sorunlar ve duyurular için **[Discord sunucumuza katıl]({discord})**.\n\nSorun bildirmek için ekrandaki **Ayarlar**'ı aç, **Hata raporu oluştur**'a bas ve masaüstüne kaydettiği zip'i paylaş.",
        business="İletişim",
        business_body="**İş birliği ve sponsorluk için:** **[{email}](mailto:{email})** adresine yaz ya da bize [Discord]({discord}) üzerinden ulaş.\n\n**Şikayet ve önerilerin için:** aynı adrese yaz ya da [Discord]({discord}) sunucumuzda bize ilet.",
        credits="Emeği geçenler ve lisans",
        credits_body="Topluluk sunucumuz ve yazılım ekibimiz **HydRaboN** tarafından yapıldı. CabinPlay ücretsiz ve açık kaynaklıdır; [MIT lisansı](LICENSE) ile yayınlanır. İçinde [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause) kullanılır.",
        disclaimer="CabinPlay resmi olmayan bir hayran projesidir. SCS Software, Google/YouTube, Twitch veya Apple ile bağlantılı değildir ve onlar tarafından desteklenmez.",
    ),
    "de": dict(
        intro="Ein funktionierender Medienbildschirm für deine Lkw-Kabine in **Euro Truck Simulator 2**. Schau YouTube, höre Musik und folge der GPS-Karte des Spiels auf einem Tablet an der Windschutzscheibe, während du fährst.",
        download="Download",
        release_download="Lade unten **{zip}** herunter und entpacke die Datei.",
        download_body="**[Lade die neueste Version herunter]({repo}/releases/latest)** und nimm die Datei `CabinPlay-x.y.z.zip`.",
        install="Installation",
        install_intro="Schließe das Spiel und entpacke die Datei. Wähle dann eine Variante:",
        auto="**A) Automatisch.** Doppelklicke auf **Install.cmd**. Meldet Windows, der Computer wurde geschützt, klicke auf *Weitere Informationen* und dann auf *Trotzdem ausführen* (die Dateien sind nicht signiert). `Install.cmd` startet nur `data\\install.ps1`, ein Skript in Klartext, das du lesen kannst.",
        manual="**B) Von Hand.** Es wird kein Skript ausgeführt; du kopierst drei Dinge aus dem Ordner `data`:",
        manual_1="`cabinplay.scs` nach `Dokumente\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` und `plugin\\cabinplay.ini` in den Ordner `bin\\win_x64\\plugins` des Spiels (lege `plugins` an, falls er fehlt). So findest du den Spielordner: in Steam Rechtsklick auf das Spiel > *Verwalten* > *Lokale Dateien durchsuchen*.",
        manual_3="den Ordner `app` an einen beliebigen Ort; starte dann einmal `CabinPlay.exe` darin. Danach startet es von selbst mit dem Spiel.",
        then="**Danach im Spiel:**",
        game_1="Starte das Spiel und bestätige den Hinweis zu den \"SDK-Funktionen\", der einmal erscheint.",
        game_2="Aktiviere im **Mod-Manager** **CabinPlay Screen**.",
        game_3="Baue in einer Lkw-Werkstatt **CabinPlay Screen** im Zubehörplatz an der linken Windschutzscheibe ein. Es gibt zwei Varianten: *Large* (11\", am Arm) und *Compact* (10\", an der Scheibe).",
        done="Das war's. Der Bildschirm schaltet sich mit der Zündung ein. Zum Entfernen doppelklicke auf **Uninstall.cmd** oder lösche die von Hand kopierten Dateien.",
        use="Bedienung",
        shortcut="Tastenkürzel", does="Funktion", space="Leertaste",
        k_control="Den Bildschirm im Spiel mit Maus und Tastatur bedienen; das Spiel pausiert solange (verlassen mit `Esc`)",
        k_nav="Navigationskarte", k_home="Startbildschirm", k_play="Wiedergabe / Pause", k_next="Weiter / zurück",
        k_seek="10 Sekunden vor / zurück", k_full="Video im Vollbild",
        use_note="Die Wiedergabe pausiert, wenn du das Spiel pausierst oder die Zündung ausschaltest. Die Sprache findest du unter **Einstellungen** auf dem Bildschirm.",
        needs="Voraussetzungen",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 oder 11",
        need_2="Kein DLC nötig",
        need_3="Einzelspieler: funktioniert nicht mit TruckersMP",
        need_4="Passt in 25 Lkw; nicht in den Renault E-Tech T",
        help="Hilfe und Community",
        help_body="Fragen, Probleme und Neuigkeiten: **[tritt unserem Discord bei]({discord})**.\n\nUm ein Problem zu melden, öffne **Einstellungen** auf dem Bildschirm, drücke **Fehlerbericht erstellen** und poste die Zip-Datei, die auf dem Desktop gespeichert wird.",
        business="Kontakt",
        business_body="**Zusammenarbeit und Sponsoring:** schreib an **[{email}](mailto:{email})** oder erreiche uns auf [Discord]({discord}).\n\n**Beschwerden und Vorschläge:** schreib an dieselbe Adresse oder melde dich auf [Discord]({discord}).",
        credits="Mitwirkende und Lizenz",
        credits_body="Entwickelt von **HydRaboN**, unserem Community-Server und Software-Team. CabinPlay ist kostenlos und quelloffen unter der [MIT-Lizenz](LICENSE). Enthält [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay ist ein inoffizielles Fanprojekt. Es steht in keiner Verbindung zu SCS Software, Google/YouTube, Twitch oder Apple und wird von ihnen nicht unterstützt.",
    ),
    "pl": dict(
        intro="Działający ekran multimedialny w kabinie ciężarówki w **Euro Truck Simulator 2**. Oglądaj YouTube, słuchaj muzyki i śledź mapę GPS z gry na tablecie przy przedniej szybie, podczas jazdy.",
        download="Pobieranie",
        release_download="Pobierz poniżej **{zip}** i rozpakuj plik.",
        download_body="**[Pobierz najnowszą wersję]({repo}/releases/latest)** i weź plik `CabinPlay-x.y.z.zip`.",
        install="Instalacja",
        install_intro="Zamknij grę i rozpakuj plik. Następnie wybierz jeden sposób:",
        auto="**A) Automatycznie.** Kliknij dwukrotnie **Install.cmd**. Jeśli system Windows poinformuje, że ochronił komputer, kliknij *Więcej informacji*, a potem *Uruchom mimo to* (pliki nie są podpisane). `Install.cmd` uruchamia tylko `data\\install.ps1`, zwykły skrypt tekstowy, który możesz przeczytać.",
        manual="**B) Ręcznie.** Żaden skrypt nie jest uruchamiany; kopiujesz trzy rzeczy z folderu `data`:",
        manual_1="`cabinplay.scs` do `Dokumenty\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` i `plugin\\cabinplay.ini` do folderu gry `bin\\win_x64\\plugins` (utwórz `plugins`, jeśli go nie ma). Aby znaleźć folder gry: w Steam kliknij grę prawym przyciskiem > *Zarządzaj* > *Przeglądaj pliki lokalne*.",
        manual_3="folder `app` w dowolne miejsce, a potem uruchom w nim raz `CabinPlay.exe`. Od tej pory będzie się uruchamiać razem z grą.",
        then="**Potem, w grze:**",
        game_1="Uruchom grę i zaakceptuj komunikat o \"funkcjach SDK\", który pojawia się raz.",
        game_2="W **Menedżerze modów** włącz **CabinPlay Screen**.",
        game_3="W warsztacie zamontuj **CabinPlay Screen** w gnieździe akcesoriów po lewej stronie przedniej szyby. Są dwie wersje: *Large* (11\", na ramieniu) i *Compact* (10\", przy szybie).",
        done="To wszystko. Ekran włącza się razem z zapłonem ciężarówki. Aby usunąć, kliknij dwukrotnie **Uninstall.cmd** albo usuń pliki skopiowane ręcznie.",
        use="Obsługa",
        shortcut="Skrót", does="Co robi", space="Spacja",
        k_control="Obsługa ekranu z poziomu gry myszą i klawiaturą; gra jest w tym czasie wstrzymana (wyjście: `Esc`)",
        k_nav="Mapa nawigacji", k_home="Ekran główny", k_play="Odtwarzaj / wstrzymaj", k_next="Następny / poprzedni",
        k_seek="10 sekund do przodu / do tyłu", k_full="Wideo na pełnym ekranie",
        use_note="Odtwarzanie wstrzymuje się, gdy zatrzymasz grę lub wyłączysz zapłon. Język zmienisz w **Ustawieniach** na ekranie.",
        needs="Wymagania",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 lub 11",
        need_2="Nie wymaga DLC",
        need_3="Tryb jednoosobowy: nie działa z TruckersMP",
        need_4="Pasuje do 25 ciężarówek; nie do Renault E-Tech T",
        help="Pomoc i społeczność",
        help_body="Pytania, problemy i nowości: **[dołącz do naszego Discorda]({discord})**.\n\nAby zgłosić problem, otwórz **Ustawienia** na ekranie, naciśnij **Utwórz raport o błędzie** i wyślij plik zip zapisany na pulpicie.",
        business="Kontakt",
        business_body="**Współpraca i sponsoring:** napisz na **[{email}](mailto:{email})** albo odezwij się na [Discordzie]({discord}).\n\n**Skargi i sugestie:** napisz na ten sam adres albo daj nam znać na [Discordzie]({discord}).",
        credits="Autorzy i licencja",
        credits_body="Stworzone przez **HydRaboN**, nasz serwer społeczności i zespół programistów. CabinPlay jest darmowy i otwartoźródłowy na [licencji MIT](LICENSE). Zawiera [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay to nieoficjalny projekt fanowski. Nie jest powiązany z SCS Software, Google/YouTube, Twitch ani Apple i nie jest przez nie wspierany.",
    ),
    "fr": dict(
        intro="Un écran multimédia fonctionnel pour la cabine de votre camion dans **Euro Truck Simulator 2**. Regardez YouTube, écoutez de la musique et suivez la carte GPS du jeu sur une tablette fixée au pare-brise, tout en conduisant.",
        download="Téléchargement",
        release_download="Téléchargez **{zip}** ci-dessous et décompressez-le.",
        download_body="**[Téléchargez la dernière version]({repo}/releases/latest)** et prenez le fichier `CabinPlay-x.y.z.zip`.",
        install="Installation",
        install_intro="Fermez le jeu et décompressez le fichier. Choisissez ensuite une méthode :",
        auto="**A) Automatique.** Double-cliquez sur **Install.cmd**. Si Windows indique qu'il a protégé votre ordinateur, cliquez sur *Informations complémentaires*, puis sur *Exécuter quand même* (les fichiers ne sont pas signés). `Install.cmd` lance uniquement `data\\install.ps1`, un script en texte clair que vous pouvez lire.",
        manual="**B) À la main.** Aucun script n'est exécuté ; vous copiez trois éléments du dossier `data` :",
        manual_1="`cabinplay.scs` dans `Documents\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` et `plugin\\cabinplay.ini` dans le dossier `bin\\win_x64\\plugins` du jeu (créez `plugins` s'il n'existe pas). Pour trouver le dossier du jeu : dans Steam, clic droit sur le jeu > *Gérer* > *Parcourir les fichiers locaux*.",
        manual_3="le dossier `app` où vous voulez, puis lancez une fois `CabinPlay.exe` qui s'y trouve. Ensuite, il démarre tout seul avec le jeu.",
        then="**Ensuite, dans le jeu :**",
        game_1="Lancez le jeu et acceptez l'avertissement sur les « fonctionnalités SDK », affiché une seule fois.",
        game_2="Dans le **Gestionnaire de mods**, activez **CabinPlay Screen**.",
        game_3="Dans un garage, installez **CabinPlay Screen** dans l'emplacement d'accessoire à gauche du pare-brise. Deux versions existent : *Large* (11\", sur un bras) et *Compact* (10\", contre la vitre).",
        done="C'est tout. L'écran s'allume avec le contact du camion. Pour le retirer, double-cliquez sur **Uninstall.cmd** ou supprimez les fichiers copiés à la main.",
        use="Utilisation",
        shortcut="Raccourci", does="Action", space="Espace",
        k_control="Utiliser l'écran depuis le jeu avec la souris et le clavier ; le jeu est en pause pendant ce temps (`Échap` pour quitter)",
        k_nav="Carte de navigation", k_home="Écran d'accueil", k_play="Lecture / pause", k_next="Suivant / précédent",
        k_seek="Avancer / reculer de 10 secondes", k_full="Vidéo en plein écran",
        use_note="La lecture se met en pause quand vous mettez le jeu en pause ou coupez le contact. La langue se règle dans **Réglages**, à l'écran.",
        needs="Prérequis",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 ou 11",
        need_2="Aucun DLC requis",
        need_3="Solo uniquement : ne fonctionne pas avec TruckersMP",
        need_4="Compatible avec 25 camions ; pas avec le Renault E-Tech T",
        help="Aide et communauté",
        help_body="Questions, problèmes et nouveautés : **[rejoignez notre Discord]({discord})**.\n\nPour signaler un problème, ouvrez **Réglages** à l'écran, appuyez sur **Créer un rapport d'erreur** et publiez le fichier zip enregistré sur le bureau.",
        business="Contact",
        business_body="**Collaboration et sponsoring :** écrivez à **[{email}](mailto:{email})** ou contactez-nous sur [Discord]({discord}).\n\n**Réclamations et suggestions :** écrivez à la même adresse ou dites-le-nous sur [Discord]({discord}).",
        credits="Crédits et licence",
        credits_body="Réalisé par **HydRaboN**, notre serveur communautaire et équipe logicielle. CabinPlay est gratuit et open source sous [licence MIT](LICENSE). Il inclut [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay est un projet de fans non officiel. Il n'est ni affilié à SCS Software, Google/YouTube, Twitch ou Apple, ni approuvé par eux.",
    ),
    "es": dict(
        intro="Una pantalla multimedia que funciona de verdad en la cabina de tu camión en **Euro Truck Simulator 2**. Mira YouTube, escucha música y sigue el mapa GPS del juego en una tableta fijada al parabrisas, mientras conduces.",
        download="Descarga",
        release_download="Descarga **{zip}** más abajo y descomprímelo.",
        download_body="**[Descarga la última versión]({repo}/releases/latest)** y elige el archivo `CabinPlay-x.y.z.zip`.",
        install="Instalación",
        install_intro="Cierra el juego y descomprime el archivo. Después elige una opción:",
        auto="**A) Automática.** Haz doble clic en **Install.cmd**. Si Windows dice que protegió tu PC, pulsa *Más información* y luego *Ejecutar de todas formas* (los archivos no están firmados). `Install.cmd` solo inicia `data\\install.ps1`, un script de texto plano que puedes leer.",
        manual="**B) A mano.** No se ejecuta ningún script; copias tres cosas de la carpeta `data`:",
        manual_1="`cabinplay.scs` en `Documentos\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` y `plugin\\cabinplay.ini` en la carpeta `bin\\win_x64\\plugins` del juego (crea `plugins` si no existe). Para encontrar la carpeta del juego: en Steam, clic derecho en el juego > *Administrar* > *Ver archivos locales*.",
        manual_3="la carpeta `app` donde quieras; después inicia una vez `CabinPlay.exe` dentro de ella. A partir de ahí se abre solo con el juego.",
        then="**Después, en el juego:**",
        game_1="Inicia el juego y acepta el aviso de \"funciones del SDK\" que aparece una vez.",
        game_2="En el **Gestor de mods**, activa **CabinPlay Screen**.",
        game_3="En un taller, monta **CabinPlay Screen** en la ranura de accesorios izquierda del parabrisas. Hay dos versiones: *Large* (11\", en un brazo) y *Compact* (10\", junto al cristal).",
        done="Eso es todo. La pantalla se enciende con el contacto del camión. Para quitarlo, haz doble clic en **Uninstall.cmd** o borra los archivos que copiaste a mano.",
        use="Uso",
        shortcut="Atajo", does="Qué hace", space="Espacio",
        k_control="Usar la pantalla desde el juego con ratón y teclado; el juego se pausa mientras tanto (`Esc` para salir)",
        k_nav="Mapa de navegación", k_home="Pantalla de inicio", k_play="Reproducir / pausar", k_next="Siguiente / anterior",
        k_seek="10 segundos adelante / atrás", k_full="Vídeo a pantalla completa",
        use_note="La reproducción se pausa cuando pausas el juego o quitas el contacto. El idioma está en **Ajustes**, en la pantalla.",
        needs="Requisitos",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 u 11",
        need_2="No requiere DLC",
        need_3="Un jugador: no funciona con TruckersMP",
        need_4="Compatible con 25 camiones; no con el Renault E-Tech T",
        help="Ayuda y comunidad",
        help_body="Preguntas, problemas y novedades: **[únete a nuestro Discord]({discord})**.\n\nPara informar de un problema, abre **Ajustes** en la pantalla, pulsa **Crear informe de error** y publica el zip que guarda en el escritorio.",
        business="Contacto",
        business_body="**Colaboración y patrocinio:** escribe a **[{email}](mailto:{email})** o contáctanos en [Discord]({discord}).\n\n**Quejas y sugerencias:** escribe a la misma dirección o cuéntanoslo en [Discord]({discord}).",
        credits="Créditos y licencia",
        credits_body="Hecho por **HydRaboN**, nuestro servidor de comunidad y equipo de software. CabinPlay es gratuito y de código abierto bajo la [licencia MIT](LICENSE). Incluye [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay es un proyecto de fans no oficial. No está afiliado a SCS Software, Google/YouTube, Twitch ni Apple, ni cuenta con su respaldo.",
    ),
    "ru": dict(
        intro="Работающий медиаэкран в кабине грузовика в **Euro Truck Simulator 2**. Смотрите YouTube, слушайте музыку и следите за GPS-картой игры на планшете у лобового стекла прямо во время поездки.",
        download="Скачать",
        release_download="Скачайте **{zip}** ниже и распакуйте файл.",
        download_body="**[Скачайте последнюю версию]({repo}/releases/latest)** и возьмите файл `CabinPlay-x.y.z.zip`.",
        install="Установка",
        install_intro="Закройте игру и распакуйте файл. Затем выберите способ:",
        auto="**А) Автоматически.** Дважды щёлкните **Install.cmd**. Если Windows сообщит, что защитила компьютер, нажмите *Подробнее*, затем *Выполнить в любом случае* (файлы не подписаны). `Install.cmd` только запускает `data\\install.ps1` — обычный текстовый скрипт, который можно прочитать.",
        manual="**Б) Вручную.** Скрипты не запускаются; нужно скопировать три вещи из папки `data`:",
        manual_1="`cabinplay.scs` в `Документы\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` и `plugin\\cabinplay.ini` в папку игры `bin\\win_x64\\plugins` (создайте `plugins`, если её нет). Как найти папку игры: в Steam щёлкните игру правой кнопкой > *Управление* > *Просмотреть локальные файлы*.",
        manual_3="папку `app` в любое место, затем один раз запустите в ней `CabinPlay.exe`. После этого приложение будет запускаться вместе с игрой само.",
        then="**Затем в игре:**",
        game_1="Запустите игру и подтвердите уведомление о «функциях SDK», которое показывается один раз.",
        game_2="В **Менеджере модификаций** включите **CabinPlay Screen**.",
        game_3="В мастерской установите **CabinPlay Screen** в слот аксессуаров слева на лобовом стекле. Есть две версии: *Large* (11\", на кронштейне) и *Compact* (10\", у стекла).",
        done="Готово. Экран включается вместе с зажиганием. Чтобы удалить, дважды щёлкните **Uninstall.cmd** или удалите файлы, скопированные вручную.",
        use="Управление",
        shortcut="Сочетание", does="Действие", space="Пробел",
        k_control="Управлять экраном из игры мышью и клавиатурой; игра в это время на паузе (выход: `Esc`)",
        k_nav="Карта навигации", k_home="Главный экран", k_play="Воспроизведение / пауза", k_next="Следующее / предыдущее",
        k_seek="На 10 секунд вперёд / назад", k_full="Видео на весь экран",
        use_note="Воспроизведение приостанавливается, когда вы ставите игру на паузу или выключаете зажигание. Язык меняется в **Настройках** на экране.",
        needs="Требования",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 или 11",
        need_2="DLC не требуется",
        need_3="Только одиночная игра: с TruckersMP не работает",
        need_4="Подходит для 25 грузовиков; не подходит для Renault E-Tech T",
        help="Помощь и сообщество",
        help_body="Вопросы, проблемы и новости: **[присоединяйтесь к нашему Discord]({discord})**.\n\nЧтобы сообщить о проблеме, откройте **Настройки** на экране, нажмите **Создать отчёт об ошибке** и отправьте zip-файл, сохранённый на рабочем столе.",
        business="Контакты",
        business_body="**Сотрудничество и спонсорство:** пишите на **[{email}](mailto:{email})** или свяжитесь с нами в [Discord]({discord}).\n\n**Жалобы и предложения:** пишите на тот же адрес или расскажите нам в [Discord]({discord}).",
        credits="Авторы и лицензия",
        credits_body="Сделано **HydRaboN** — нашим сервером сообщества и командой разработчиков. CabinPlay бесплатен и распространяется с открытым исходным кодом по [лицензии MIT](LICENSE). Включает [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay — неофициальный фанатский проект. Он не связан с SCS Software, Google/YouTube, Twitch или Apple и не одобрен ими.",
    ),
}

# Only the English README carries this: it is for people who build the project.
BUILDING = """## Building from source

Needs Python 3, MinGW-w64 gcc and the .NET 8 SDK on Windows.

```powershell
.\\build.ps1      # builds the mod, plugin, app and dist\\CabinPlay-<version>.zip
.\\install.ps1    # installs that build on this PC
```

`tools\\` generates the mod, `plugin\\` is the game plugin, `app\\` is the companion app with the
screen's interface in `app\\ui\\`. The version number lives in `VERSION`. This README and its
translations are generated by `tools\\build_readme.py`.

"""

TEMPLATE = """# CabinPlay

{languages}

{intro}

## {download}

{download_body}

## {install}

{install_intro}

{auto}

{manual}

1. {manual_1}
2. {manual_2}
3. {manual_3}

{then}

1. {game_1}
2. {game_2}
3. {game_3}

{done}

## {use}

| {shortcut} | {does} |
|---|---|
| `Ctrl+Alt+C` | {k_control} |
| `Ctrl+Alt+N` | {k_nav} |
| `Ctrl+Alt+H` | {k_home} |
| `Ctrl+Alt+{space}` | {k_play} |
| `Ctrl+Alt+→` `←` | {k_next} |
| `Ctrl+Alt+↑` `↓` | {k_seek} |
| `Ctrl+Alt+F` | {k_full} |

{use_note}

## {needs}

- {need_1}
- {need_2}
- {need_3}
- {need_4}

## {help}

{help_body}

## {business}

{business_body}

{building}## {credits}

{credits_body}

{disclaimer}
"""


RELEASE_SECTION = """{intro}

{release_download} {install_intro_tail}

{auto}

{manual}

1. {manual_1}
2. {manual_2}
3. {manual_3}

{then}

1. {game_1}
2. {game_2}
3. {game_3}

**{needs}:** {need_1}. {need_2}. {need_3}.

**{help}:** {help_line}

{business_body}
"""


def release_notes(version):
    """Text for the GitHub release page: English in full, every other language folded."""
    zip_name = "CabinPlay-%s.zip" % version
    parts = []
    for code, (name, filename) in LANGUAGES.items():
        values = {k: v.format(repo=REPO, discord=DISCORD, email=EMAIL, zip=zip_name) for k, v in TEXT[code].items()}
        # "Close the game and unzip the file. Then pick one:" -> keep only the last sentence,
        # the download line above already covers the first.
        values["install_intro_tail"] = values["install_intro"].split(". ", 1)[-1]
        values["help_line"] = values["help_body"].replace("\n\n", " ")
        section = RELEASE_SECTION.format(**values)
        if code == "en":
            parts.append(section)
        else:
            parts.append("<details>\n<summary><b>%s</b></summary>\n\n%s\n</details>\n" % (name, section))
    others = ", ".join(name for code, (name, _) in LANGUAGES.items() if code != "en")
    return "🌐 **English** below. Click a language further down to open it: %s.\n\n%s" % (others, "\n".join(parts))


def main():
    for code, (_, filename) in LANGUAGES.items():
        bar = " · ".join("**%s**" % name if c == code else "[%s](%s)" % (name, f)
                         for c, (name, f) in LANGUAGES.items())
        values = {k: v.format(repo=REPO, discord=DISCORD, email=EMAIL, zip="") for k, v in TEXT[code].items()}
        missing = set(TEXT["en"]) - set(TEXT[code])
        assert not missing, "%s lacks: %s" % (code, ", ".join(sorted(missing)))
        text = TEMPLATE.format(languages=bar, building=BUILDING if code == "en" else "", **values)
        with io.open(os.path.join(ROOT, filename), "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print("wrote", filename)
    version_file = os.path.join(ROOT, "VERSION")
    dist = os.path.join(ROOT, "dist")
    if os.path.isdir(dist):
        version = io.open(version_file, encoding="utf-8").read().strip()
        with io.open(os.path.join(dist, "release-notes.md"), "w", encoding="utf-8", newline="\n") as f:
            f.write(release_notes(version))
        print("wrote dist/release-notes.md for", version)


if __name__ == "__main__":
    main()
