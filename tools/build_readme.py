"""Writes README.md and its translations (README.<code>.md) from one template.

Run it after changing a text below; the README files themselves are generated.
"""
import io
import os
from urllib.parse import quote

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPO = "https://github.com/timurmert/ets2-cabinplay"
DISCORD = "https://discord.gg/hydrabon"
EMAIL = "contact@hydrabon.com"
MEDIA = "docs/media"
# Download count: written by .github/workflows/download-badge.yml to the "badges" branch, so the
# badge does not depend on shields.io querying GitHub (that fails with "invalid" when its tokens run out).
BADGE = ("https://img.shields.io/endpoint?url=" + quote("https://raw.githubusercontent.com/timurmert/ets2-cabinplay/badges/downloads.json", safe="")
         + "&style=for-the-badge&logo=github&logoColor=white&labelColor=24292f&color=2ea44f&label=")
# VirusTotal report of the released setup and its result (also shown in
# docs/media/virustotal.png); update them after a release.
VIRUSTOTAL = "https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56"
VT_SCORE = "0/68"
VT_ENGINES = "68"
VT_BADGE = "https://img.shields.io/static/v1?style=for-the-badge&logo=virustotal&logoColor=white&labelColor=394EFF&color=2ea44f&label=VirusTotal&message="
# What changed in this release: shown at the top of each language on the release page
# only (not in the READMEs). Rewrite it for every release.
WHATS_NEW = {
    "en": """### What's new in {version}

- **Automatic updates.** CabinPlay now keeps itself up to date: it downloads a new version while you play and installs it when you close the game. The screen tells you when an update is ready and when it is done. On the screen, *Settings* > *Updates*: *Automatic*, *Ask me* (installs only after you press *Update*) or *Off*.
- **Fix:** on some PCs the screen stayed empty because of a "The parameter is incorrect" error.
- Error reports now also hold the log of the previous run, so a crash still shows after CabinPlay was started again.

**Already using CabinPlay?** Close the game, download **CabinPlay-Setup-{version}.exe** below and run it: it updates CabinPlay and keeps your settings. Updates after this one come by themselves.""",
    "tr": """### {version} sürümünde neler var

- **Otomatik güncelleme.** CabinPlay artık kendini güncel tutuyor: yeni sürümü sen oynarken indirir, oyunu kapattığında kurar. Güncellemenin hazır olduğunu ve tamamlandığını ekranda görürsün. Ekrandaki *Ayarlar* > *Güncellemeler*: *Otomatik*, *Bana sor* (sadece *Güncelle*'ye bastığında kurar) ya da *Kapalı*.
- **Düzeltme:** bazı bilgisayarlarda "Parametre yanlış" hatası yüzünden ekran boş kalıyordu.
- Hata raporları artık bir önceki çalıştırmanın günlüğünü de içeriyor; CabinPlay yeniden açılsa bile bir çökmenin izi kaybolmuyor.

**CabinPlay'i zaten kullanıyor musun?** Oyunu kapat, aşağıdan **CabinPlay-Setup-{version}.exe**'yi indir ve çalıştır: CabinPlay güncellenir, ayarların korunur. Bundan sonraki güncellemeler kendiliğinden gelir.""",
    "de": """### Neu in {version}

- **Automatische Updates.** CabinPlay hält sich jetzt selbst aktuell: Eine neue Version wird beim Spielen heruntergeladen und beim Schließen des Spiels installiert. Der Bildschirm zeigt an, wann ein Update bereit ist und wann es fertig ist. Auf dem Bildschirm unter *Einstellungen* > *Updates*: *Automatisch*, *Fragen* (installiert erst, wenn du *Aktualisieren* drückst) oder *Aus*.
- **Behoben:** Auf manchen PCs blieb der Bildschirm wegen des Fehlers „Falscher Parameter“ leer.
- Fehlerberichte enthalten jetzt auch das Protokoll des vorherigen Starts, sodass ein Absturz auch nach einem Neustart von CabinPlay sichtbar bleibt.

**Du nutzt CabinPlay schon?** Schließe das Spiel, lade unten **CabinPlay-Setup-{version}.exe** herunter und starte es: CabinPlay wird aktualisiert, deine Einstellungen bleiben. Alle weiteren Updates kommen von selbst.""",
    "pl": """### Co nowego w {version}

- **Automatyczne aktualizacje.** CabinPlay sam dba o aktualność: nową wersję pobiera w trakcie gry i instaluje po jej zamknięciu. Ekran pokazuje, kiedy aktualizacja jest gotowa i kiedy się zakończyła. Na ekranie w *Ustawienia* > *Aktualizacje*: *Automatyczne*, *Pytaj mnie* (instaluje dopiero po naciśnięciu *Aktualizuj*) albo *Wyłączone*.
- **Poprawka:** na niektórych komputerach ekran pozostawał pusty z powodu błędu „Parametr jest niepoprawny”.
- Raporty o błędach zawierają teraz także dziennik poprzedniego uruchomienia, więc ślad awarii zostaje nawet po ponownym uruchomieniu CabinPlay.

**Masz już CabinPlay?** Zamknij grę, pobierz poniżej **CabinPlay-Setup-{version}.exe** i uruchom go: CabinPlay zostanie zaktualizowany, a ustawienia zostaną zachowane. Kolejne aktualizacje przyjdą same.""",
    "fr": """### Nouveautés de la version {version}

- **Mises à jour automatiques.** CabinPlay se met désormais à jour tout seul : il télécharge la nouvelle version pendant que vous jouez et l'installe quand vous fermez le jeu. L'écran indique quand une mise à jour est prête et quand elle est terminée. Sur l'écran, *Réglages* > *Mises à jour* : *Automatiques*, *Me demander* (installe seulement après un appui sur *Mettre à jour*) ou *Désactivées*.
- **Correction :** sur certains PC, l'écran restait vide à cause de l'erreur « Paramètre incorrect ».
- Les rapports d'erreur contiennent maintenant aussi le journal de l'exécution précédente : un plantage reste visible même après avoir relancé CabinPlay.

**Vous utilisez déjà CabinPlay ?** Fermez le jeu, téléchargez **CabinPlay-Setup-{version}.exe** ci-dessous et lancez-le : CabinPlay est mis à jour et vos réglages sont conservés. Les mises à jour suivantes arriveront toutes seules.""",
    "es": """### Novedades de la {version}

- **Actualizaciones automáticas.** CabinPlay ahora se mantiene al día solo: descarga la nueva versión mientras juegas y la instala al cerrar el juego. La pantalla te avisa cuando una actualización está lista y cuando ha terminado. En la pantalla, *Ajustes* > *Actualizaciones*: *Automáticas*, *Preguntarme* (solo instala cuando pulsas *Actualizar*) o *Desactivadas*.
- **Corregido:** en algunos PC la pantalla se quedaba vacía por el error «El parámetro no es correcto».
- Los informes de error ahora incluyen también el registro de la ejecución anterior, así un fallo sigue a la vista aunque CabinPlay se haya vuelto a abrir.

**¿Ya usas CabinPlay?** Cierra el juego, descarga **CabinPlay-Setup-{version}.exe** abajo y ejecútalo: CabinPlay se actualiza y tus ajustes se conservan. Las siguientes actualizaciones llegarán solas.""",
    "ru": """### Что нового в {version}

- **Автоматические обновления.** CabinPlay теперь обновляется сам: скачивает новую версию во время игры и устанавливает её, когда вы закрываете игру. Экран сообщает, когда обновление готово и когда оно установлено. На экране, *Настройки* > *Обновления*: *Автоматически*, *Спрашивать* (установка только после нажатия *Обновить*) или *Выключены*.
- **Исправлено:** на некоторых ПК экран оставался пустым из-за ошибки «Параметр задан неверно».
- Отчёты об ошибках теперь содержат и журнал предыдущего запуска, поэтому сбой виден даже после повторного запуска CabinPlay.

**Уже пользуетесь CabinPlay?** Закройте игру, скачайте ниже **CabinPlay-Setup-{version}.exe** и запустите: CabinPlay обновится, настройки сохранятся. Следующие обновления придут сами.""",
}

VIDEO = REPO + "/blob/main/" + MEDIA + "/cabinplay-demo.mp4"

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
        downloads="Downloads",
        watch="Watch the full video with sound",
        scan_line="🛡️ **Virus scan:** the setup was checked by {vt_engines} antivirus engines on VirusTotal and none of them flagged it. **[See the report]({virustotal})**",
        vt_clean="{vt_score} clean",
        vt_caption="None of the {vt_engines} antivirus engines on VirusTotal flagged CabinPlay.",
        vt_report="See the report",
        download_body="**[Download the latest version]({repo}/releases/latest)** and get the file named `CabinPlay-Setup-x.y.z.exe`.",
        install="Install",
        install_intro="Close the game, then pick one:",
        auto="**A) Setup (recommended).** Download **{setup}** and run it. It finds the game by itself and needs no administrator rights. If Windows says it protected your PC, click *More info*, then *Run anyway* (the setup is not code-signed).",
        manual="**B) By hand.** Download **{zip}** instead, unzip it and copy three things from it:",
        manual_1="`cabinplay.scs` into `Documents\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` and `plugin\\cabinplay.ini` into the game's `bin\\win_x64\\plugins` folder (create `plugins` if it is not there). To find the game folder: in Steam, right-click the game > *Manage* > *Browse local files*.",
        manual_3="the `app` folder to anywhere you like, then start `CabinPlay.exe` in it once. From then on it starts with the game by itself.",
        then="**Then, in the game:**",
        game_1="Start the game and accept the \"SDK features\" notice it shows once.",
        game_2="In the **Mod Manager**, switch on **CabinPlay Screen**.",
        game_3="In a truck workshop, fit **CabinPlay Screen** in a windscreen accessory slot. On the left there are two versions, *Large* (11\", on an arm) and *Compact* (10\", at the glass); in the middle slot there is *Centre*, turned towards the driver (with a version of its own for right-hand-drive cabins).",
        done="That's it. The screen switches on with the truck's ignition. Installed with the setup, CabinPlay keeps itself up to date: it downloads a new version while you play and installs it when you close the game and shows it on the screen; under *Settings* > *Updates* you can have it ask first, or turn it off. To remove CabinPlay, open Windows *Settings* > *Apps*, or run `Uninstall.exe` in `%LocalAppData%\\Programs\\CabinPlay`; if you installed it by hand, delete the files you copied.",
        use="Use",
        shortcut="Shortcut", does="What it does", space="Space",
        k_control="Use the screen from inside the game with mouse and keyboard; the game pauses meanwhile (`Esc` to leave)",
        k_nav="Navigation map", k_home="Home screen", k_play="Play / pause", k_next="Next / previous",
        k_seek="10 seconds forward / back", k_full="Video fullscreen",
        k_apps="Previous / next open app",
        k_split="Two apps side by side, on / off", k_swap="Swap the two sides",
        use_note="Playback pauses when you pause the game or switch the ignition off. The language is under **Settings** on the screen. If you closed CabinPlay from its settings, `Ctrl+Alt+C` starts it again.",
        truck_note="The **Truck** app shows the damage to the truck and trailer, the cargo, the fuel and the gauges. The Ctrl+Alt shortcuts also work while the game is in front, and their keys never reach the game, so a shortcut does not sound the horn or switch anything on the truck.",
        mirror_note="The **Mirrors** app puts the truck's own mirrors on the screen: the left mirror, its wide-angle mirror and, if the truck has one, the front mirror. Tap one to enlarge it.",
        browser_note="The **Browser** app opens any website: type an address or a search in the bar at the top, and press the star to keep a page on its start page. Sites you sign in to stay signed in, and `Ctrl+Alt+F` makes the video on a page fullscreen. Typing needs control mode (`Ctrl+Alt+C`).",
        custom_note="**Side by side** puts two apps next to each other, for example the map on the left and music on the right: press the split button in the dock and pick the app for each half. Under **Settings** you can choose a background, also from your own pictures, change the keys of the shortcuts, and put any of them on a button of your steering wheel or gamepad.",
        places_note="**More places:** besides the windscreen, the workshop has the screen for the *Dashboard*, the *Air vent*, *Overhead*, the *Passenger seat*, the *Console*, the *Cup holder* (newer trucks) and the *Passenger side* of the windscreen. To watch from the bunk there is **CabinPlay TV**, a 27\" set for the back wall (the light panel slot), flat or swung out towards the pillow, or for the windscreen, flat or turned to the bunk. In the Scania S, keep turning your head to the right past looking back: the camera moves onto the bunk and stops at the pillow, facing the TV.",
        needs="What you need",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 or 11",
        need_2="No DLC needed",
        need_3="Single player: it does not work with TruckersMP",
        need_4="Fits 25 trucks; not the Renault E-Tech T",
        help="Help and community",
        help_body="Questions, problems and news: **[join our Discord]({discord})**.\n\nTo report a problem, open **Settings** on the screen, press **Create error report** and post the zip it saves to your desktop.",
        stutter="**Video stutters or freezes?** The game is using all of your graphics card. In the game's graphics settings, lower **Scaling** to 200% or less (400% is the usual cause).",
        business="Contact",
        business_body="**Collaboration and sponsorship:** write to **[{email}](mailto:{email})** or reach us on [Discord]({discord}).\n\n**Complaints and suggestions:** write to the same address or tell us on [Discord]({discord}).",
        credits="Credits and licence",
        credits_body="Made by **HydRaboN**, our community server and software team. CabinPlay is free and open source under the [GNU General Public License v3.0 or later](LICENSE): you may share and change it, but anything made from it must stay open source under the same licence and credit CabinPlay. Under section 7 of the licence, the name *CabinPlay* and its logo are not licensed, and a modified version must carry a different name. It includes [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay is an unofficial fan project. It is not affiliated with or endorsed by SCS Software, Google/YouTube, Twitch or Apple.",
    ),
    "tr": dict(
        intro="**Euro Truck Simulator 2** için tır kabininde gerçekten çalışan bir medya ekranı. Sürerken ön cama takılı tablette YouTube izle, müzik dinle ve oyunun GPS haritasını takip et.",
        download="İndir",
        downloads="İndirme",
        watch="Videonun tamamını sesli izle",
        scan_line="🛡️ **Virüs taraması:** Kurulum dosyası VirusTotal'da {vt_engines} antivirüs motoruyla tarandı, hiçbiri zararlı bulmadı. **[Raporu gör]({virustotal})**",
        vt_clean="{vt_score} temiz",
        vt_caption="VirusTotal'daki {vt_engines} antivirüs motorunun hiçbiri CabinPlay'i zararlı bulmadı.",
        vt_report="Raporu gör",
        download_body="**[Son sürümü indir]({repo}/releases/latest)**; `CabinPlay-Setup-x.y.z.exe` adlı dosyayı al.",
        install="Kur",
        install_intro="Oyunu kapat, sonra birini seç:",
        auto="**A) Kurulum (önerilen).** **{setup}** dosyasını indir ve çalıştır. Oyunu kendisi bulur, yönetici izni istemez. Windows \"kişisel bilgisayarınızı korudu\" derse *Ek bilgi*, sonra *Yine de çalıştır* de (kurulum imzalı değil).",
        manual="**B) Elle.** Bunun yerine **{zip}** dosyasını indir, aç ve içinden üç şeyi kopyala:",
        manual_1="`cabinplay.scs` dosyasını `Belgeler\\Euro Truck Simulator 2\\mod` klasörüne",
        manual_2="`plugin\\cabinplay.dll` ve `plugin\\cabinplay.ini` dosyalarını oyunun `bin\\win_x64\\plugins` klasörüne (`plugins` yoksa oluştur). Oyun klasörünü bulmak için: Steam'de oyuna sağ tıkla > *Yönet* > *Yerel dosyalara göz at*.",
        manual_3="`app` klasörünü istediğin bir yere; sonra içindeki `CabinPlay.exe` dosyasını bir kez çalıştır. Sonrasında oyunla birlikte kendiliğinden açılır.",
        then="**Sonra, oyunda:**",
        game_1="Oyunu başlat ve bir kez çıkan \"SDK özellikleri\" uyarısını onayla.",
        game_2="**Mod Yöneticisi**'nde **CabinPlay Screen**'i etkinleştir.",
        game_3="Serviste **CabinPlay Screen**'i ön camdaki bir aksesuar yuvasına tak. Sol yuvada iki sürüm var: *Large* (11\", kolda) ve *Compact* (10\", camın dibinde); orta yuvada ise sürücüye dönük *Centre* var (sağdan direksiyonlu kabinler için ayrı bir sürümüyle).",
        done="Bu kadar. Ekran tırın kontağıyla birlikte açılır. Kurulum dosyasıyla kurduysan CabinPlay kendini güncel tutar: yeni sürümü sen oynarken indirir, oyunu kapattığında kurar ve bunu ekranda gösterir; ekrandaki *Ayarlar* > *Güncellemeler*'den önce sana sormasını seçebilir ya da kapatabilirsin. CabinPlay'i kaldırmak için Windows *Ayarlar* > *Uygulamalar*'ı aç ya da `%LocalAppData%\\Programs\\CabinPlay` klasöründeki `Uninstall.exe`'yi çalıştır; elle kurduysan kopyaladığın dosyaları sil.",
        use="Kullan",
        shortcut="Kısayol", does="Ne yapar", space="Boşluk",
        k_control="Ekranı oyunun içinden fare ve klavyeyle kullan; bu sırada oyun duraklar (çıkış: `Esc`)",
        k_nav="Navigasyon haritası", k_home="Ana ekran", k_play="Oynat / duraklat", k_next="Sonraki / önceki",
        k_seek="10 saniye ileri / geri", k_full="Videoyu tam ekran yap",
        k_apps="Önceki / sonraki açık uygulama",
        k_split="İki uygulamayı yan yana aç / kapat", k_swap="İki tarafı değiştir",
        use_note="Oyunu duraklattığında veya kontağı kapattığında çalan şey durur. Dil, ekrandaki **Ayarlar**'dan değişir. CabinPlay'i ayarlardan kapattıysan `Ctrl+Alt+C` onu yeniden başlatır.",
        truck_note="**Tır** uygulaması tırın ve dorsenin hasarını, yükü, yakıtı ve göstergeleri gösterir. Ctrl+Alt kısayolları oyun öndeyken de çalışır ve tuşları oyuna gitmez; bu yüzden kısayollar korna çalmaz, tırda hiçbir şeyi açıp kapatmaz.",
        mirror_note="**Aynalar** uygulaması tırın kendi aynalarını ekrana getirir: sol ayna, sol geniş açı aynası ve tırda varsa ön ayna. Büyütmek için birine dokun.",
        browser_note="**Tarayıcı** uygulaması istediğin siteyi açar: üstteki çubuğa adres ya da arama yaz, bir sayfayı başlangıç sayfasında tutmak için yıldıza bas. Giriş yaptığın sitelerde oturumun açık kalır; `Ctrl+Alt+F` sayfadaki videoyu tam ekran yapar. Yazı yazmak için kontrol modu gerekir (`Ctrl+Alt+C`).",
        custom_note="**Yan yana** görünüm iki uygulamayı aynı anda gösterir; örneğin solda harita, sağda müzik: kenar çubuğundaki bölme düğmesine bas ve her yarı için uygulamayı seç. **Ayarlar**'dan arka planı (kendi resimlerinden de) seçebilir, kısayol tuşlarını değiştirebilir ve her birini direksiyonundaki ya da oyun kolundaki bir düğmeye atayabilirsin.",
        places_note="**Başka yerler:** Serviste ekranı ön camın dışında *Dashboard* (torpido), *Air vent* (havalandırma), *Overhead* (tavan), *Passenger seat* (yolcu koltuğu), *Console* (konsol), *Cup holder* (bardaklık, yeni tırlarda) ve *Passenger side* (camın yolcu tarafı) olarak da takabilirsin. Yatakta izlemek için 27\" **CabinPlay TV** var: arka duvara (Işık Panosu yuvası) düz ya da yastığa doğru açılmış, veya ön cama düz ya da yatağa dönük. Scania S'te kafanı sağa çevirmeye arkaya baktıktan sonra da devam et: kamera yatağa geçer ve yastıkta, TV'ye bakarak durur.",
        needs="Gerekenler",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 veya 11",
        need_2="DLC gerekmez",
        need_3="Tek oyunculu: TruckersMP ile çalışmaz",
        need_4="25 tıra uyar; Renault E-Tech T'ye uymaz",
        help="Yardım ve topluluk",
        help_body="Sorular, sorunlar ve duyurular için **[Discord sunucumuza katıl]({discord})**.\n\nSorun bildirmek için ekrandaki **Ayarlar**'ı aç, **Hata raporu oluştur**'a bas ve masaüstüne kaydettiği zip'i paylaş.",
        stutter="**Video takılıyor ya da donuyor mu?** Oyun ekran kartının tamamını kullanıyordur. Oyunun grafik ayarlarında **Ölçekleme**'yi %200'e ya da altına indir (genelde neden %400'dür).",
        business="İletişim",
        business_body="**İş birliği ve sponsorluk için:** **[{email}](mailto:{email})** adresine yaz ya da bize [Discord]({discord}) üzerinden ulaş.\n\n**Şikayet ve önerilerin için:** aynı adrese yaz ya da [Discord]({discord}) sunucumuzda bize ilet.",
        credits="Emeği geçenler ve lisans",
        credits_body="Topluluk sunucumuz ve yazılım ekibimiz **HydRaboN** tarafından yapıldı. CabinPlay ücretsiz ve açık kaynaklıdır; [GNU GPL v3.0 veya sonrası](LICENSE) lisansıyla yayınlanır: paylaşabilir ve değiştirebilirsin, ama ondan yapılan her şey aynı lisansla açık kaynak kalmalı ve CabinPlay'i kaynak göstermelidir. Lisansın 7. bölümü uyarınca *CabinPlay* adı ve logosu lisansa dahil değildir; değiştirilmiş bir sürüm farklı bir adla yayınlanmalıdır. İçinde [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause) kullanılır.",
        disclaimer="CabinPlay resmi olmayan bir hayran projesidir. SCS Software, Google/YouTube, Twitch veya Apple ile bağlantılı değildir ve onlar tarafından desteklenmez.",
    ),
    "de": dict(
        intro="Ein funktionierender Medienbildschirm für deine Lkw-Kabine in **Euro Truck Simulator 2**. Schau YouTube, höre Musik und folge der GPS-Karte des Spiels auf einem Tablet an der Windschutzscheibe, während du fährst.",
        download="Download",
        downloads="Downloads",
        watch="Das ganze Video mit Ton ansehen",
        scan_line="🛡️ **Virenscan:** Das Setup wurde auf VirusTotal von {vt_engines} Antivirenprogrammen geprüft, keines hat es beanstandet. **[Zum Bericht]({virustotal})**",
        vt_clean="{vt_score} sauber",
        vt_caption="Keine der {vt_engines} Antiviren-Engines auf VirusTotal hat CabinPlay beanstandet.",
        vt_report="Zum Bericht",
        download_body="**[Lade die neueste Version herunter]({repo}/releases/latest)** und nimm die Datei `CabinPlay-Setup-x.y.z.exe`.",
        install="Installation",
        install_intro="Schließe das Spiel und wähle dann eine Variante:",
        auto="**A) Setup (empfohlen).** Lade **{setup}** herunter und starte es. Es findet das Spiel von selbst und braucht keine Administratorrechte. Meldet Windows, der Computer wurde geschützt, klicke auf *Weitere Informationen* und dann auf *Trotzdem ausführen* (das Setup ist nicht signiert).",
        manual="**B) Von Hand.** Lade stattdessen **{zip}** herunter, entpacke es und kopiere drei Dinge daraus:",
        manual_1="`cabinplay.scs` nach `Dokumente\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` und `plugin\\cabinplay.ini` in den Ordner `bin\\win_x64\\plugins` des Spiels (lege `plugins` an, falls er fehlt). So findest du den Spielordner: in Steam Rechtsklick auf das Spiel > *Verwalten* > *Lokale Dateien durchsuchen*.",
        manual_3="den Ordner `app` an einen beliebigen Ort; starte dann einmal `CabinPlay.exe` darin. Danach startet es von selbst mit dem Spiel.",
        then="**Danach im Spiel:**",
        game_1="Starte das Spiel und bestätige den Hinweis zu den \"SDK-Funktionen\", der einmal erscheint.",
        game_2="Aktiviere im **Mod-Manager** **CabinPlay Screen**.",
        game_3="Baue in einer Lkw-Werkstatt **CabinPlay Screen** in einen Zubehörplatz an der Windschutzscheibe ein. Links gibt es zwei Varianten, *Large* (11\", am Arm) und *Compact* (10\", an der Scheibe); im mittleren Platz gibt es *Centre*, zum Fahrer gedreht (mit einer eigenen Variante für Rechtslenker).",
        done="Das war's. Der Bildschirm schaltet sich mit der Zündung ein. Mit dem Setup installiert, hält sich CabinPlay selbst aktuell: Eine neue Version wird beim Spielen heruntergeladen und beim Schließen des Spiels installiert und zeigt das auf dem Bildschirm an; unter *Einstellungen* > *Updates* kannst du es vorher fragen lassen oder abschalten. Zum Entfernen öffne in Windows *Einstellungen* > *Apps* oder starte `Uninstall.exe` in `%LocalAppData%\\Programs\\CabinPlay`; hast du von Hand installiert, lösche die kopierten Dateien.",
        use="Bedienung",
        shortcut="Tastenkürzel", does="Funktion", space="Leertaste",
        k_control="Den Bildschirm im Spiel mit Maus und Tastatur bedienen; das Spiel pausiert solange (verlassen mit `Esc`)",
        k_nav="Navigationskarte", k_home="Startbildschirm", k_play="Wiedergabe / Pause", k_next="Weiter / zurück",
        k_seek="10 Sekunden vor / zurück", k_full="Video im Vollbild",
        k_apps="Vorherige / nächste offene App",
        k_split="Zwei Apps nebeneinander, ein / aus", k_swap="Die beiden Seiten tauschen",
        use_note="Die Wiedergabe pausiert, wenn du das Spiel pausierst oder die Zündung ausschaltest. Die Sprache findest du unter **Einstellungen** auf dem Bildschirm. Hast du CabinPlay in den Einstellungen beendet, startet `Ctrl+Alt+C` es wieder.",
        truck_note="Die App **Lkw** zeigt die Schäden an Lkw und Auflieger, die Ladung, den Kraftstoff und die Anzeigen. Die Ctrl+Alt-Tastenkürzel funktionieren auch, wenn das Spiel im Vordergrund ist, und ihre Tasten erreichen das Spiel nicht: Ein Tastenkürzel hupt nicht und schaltet nichts am Lkw.",
        mirror_note="Die App **Spiegel** zeigt die Spiegel des Lkw auf dem Bildschirm: den linken Spiegel, seinen Weitwinkelspiegel und, falls vorhanden, den Frontspiegel. Tippe auf einen, um ihn zu vergrößern.",
        browser_note="Die App **Browser** öffnet jede Website: Gib oben in der Leiste eine Adresse oder eine Suche ein und tippe auf den Stern, um eine Seite auf der Startseite zu behalten. Auf Websites bleibst du angemeldet, und `Ctrl+Alt+F` zeigt das Video einer Seite im Vollbild. Zum Tippen brauchst du den Steuermodus (`Ctrl+Alt+C`).",
        custom_note="**Nebeneinander** zeigt zwei Apps gleichzeitig, zum Beispiel links die Karte und rechts Musik: Tippe im Dock auf die Teilen-Taste und wähle für jede Hälfte eine App. Unter **Einstellungen** kannst du einen Hintergrund wählen, auch aus eigenen Bildern, die Tasten der Tastenkürzel ändern und jedes davon auf einen Knopf deines Lenkrads oder Gamepads legen.",
        places_note="**Weitere Plätze:** Außer an der Windschutzscheibe gibt es den Bildschirm in der Werkstatt auch für das *Dashboard* (Armaturenbrett), den *Air vent* (Lüftungsgitter), *Overhead* (Dach), den *Passenger seat* (Beifahrersitz), die *Console* (Mittelkonsole), den *Cup holder* (Getränkehalter, neuere Lkw) und die *Passenger side* (Beifahrerseite der Scheibe). Zum Zuschauen von der Liege gibt es den 27\"-**CabinPlay TV**: an der Rückwand (Platz des Lichtpaneels), flach oder zum Kopfkissen ausgeschwenkt, oder an der Windschutzscheibe, flach oder zur Liege gedreht. Im Scania S dreh den Kopf über den Blick nach hinten hinaus weiter nach rechts: Die Kamera wandert auf die Liege und bleibt am Kopfkissen stehen, mit Blick auf den Fernseher.",
        needs="Voraussetzungen",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 oder 11",
        need_2="Kein DLC nötig",
        need_3="Einzelspieler: funktioniert nicht mit TruckersMP",
        need_4="Passt in 25 Lkw; nicht in den Renault E-Tech T",
        help="Hilfe und Community",
        help_body="Fragen, Probleme und Neuigkeiten: **[tritt unserem Discord bei]({discord})**.\n\nUm ein Problem zu melden, öffne **Einstellungen** auf dem Bildschirm, drücke **Fehlerbericht erstellen** und poste die Zip-Datei, die auf dem Desktop gespeichert wird.",
        stutter="**Das Video ruckelt oder friert ein?** Das Spiel lastet deine Grafikkarte voll aus. Stelle in den Grafikeinstellungen des Spiels die **Skalierung** auf 200 % oder weniger (meist ist 400 % die Ursache).",
        business="Kontakt",
        business_body="**Zusammenarbeit und Sponsoring:** schreib an **[{email}](mailto:{email})** oder erreiche uns auf [Discord]({discord}).\n\n**Beschwerden und Vorschläge:** schreib an dieselbe Adresse oder melde dich auf [Discord]({discord}).",
        credits="Mitwirkende und Lizenz",
        credits_body="Entwickelt von **HydRaboN**, unserem Community-Server und Software-Team. CabinPlay ist kostenlos und quelloffen unter der [GNU GPL v3.0 oder später](LICENSE): Du darfst es weitergeben und verändern, aber alles, was daraus entsteht, muss unter derselben Lizenz quelloffen bleiben und CabinPlay nennen. Nach Abschnitt 7 der Lizenz sind der Name *CabinPlay* und sein Logo nicht mitlizenziert; eine veränderte Version muss einen anderen Namen tragen. Enthält [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay ist ein inoffizielles Fanprojekt. Es steht in keiner Verbindung zu SCS Software, Google/YouTube, Twitch oder Apple und wird von ihnen nicht unterstützt.",
    ),
    "pl": dict(
        intro="Działający ekran multimedialny w kabinie ciężarówki w **Euro Truck Simulator 2**. Oglądaj YouTube, słuchaj muzyki i śledź mapę GPS z gry na tablecie przy przedniej szybie, podczas jazdy.",
        download="Pobieranie",
        downloads="Pobrania",
        watch="Obejrzyj cały film z dźwiękiem",
        scan_line="🛡️ **Skan antywirusowy:** instalator został sprawdzony na VirusTotal przez {vt_engines} silników antywirusowych i żaden go nie oznaczył. **[Zobacz raport]({virustotal})**",
        vt_clean="{vt_score} czysty",
        vt_caption="Żaden z {vt_engines} silników antywirusowych na VirusTotal nie oznaczył CabinPlay.",
        vt_report="Zobacz raport",
        download_body="**[Pobierz najnowszą wersję]({repo}/releases/latest)** i weź plik `CabinPlay-Setup-x.y.z.exe`.",
        install="Instalacja",
        install_intro="Zamknij grę, a potem wybierz jeden sposób:",
        auto="**A) Instalator (zalecane).** Pobierz **{setup}** i uruchom go. Sam znajdzie grę i nie wymaga uprawnień administratora. Jeśli system Windows poinformuje, że ochronił komputer, kliknij *Więcej informacji*, a potem *Uruchom mimo to* (instalator nie jest podpisany).",
        manual="**B) Ręcznie.** Zamiast tego pobierz **{zip}**, rozpakuj go i skopiuj z niego trzy rzeczy:",
        manual_1="`cabinplay.scs` do `Dokumenty\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` i `plugin\\cabinplay.ini` do folderu gry `bin\\win_x64\\plugins` (utwórz `plugins`, jeśli go nie ma). Aby znaleźć folder gry: w Steam kliknij grę prawym przyciskiem > *Zarządzaj* > *Przeglądaj pliki lokalne*.",
        manual_3="folder `app` w dowolne miejsce, a potem uruchom w nim raz `CabinPlay.exe`. Od tej pory będzie się uruchamiać razem z grą.",
        then="**Potem, w grze:**",
        game_1="Uruchom grę i zaakceptuj komunikat o \"funkcjach SDK\", który pojawia się raz.",
        game_2="W **Menedżerze modów** włącz **CabinPlay Screen**.",
        game_3="W warsztacie zamontuj **CabinPlay Screen** w gnieździe akcesoriów na przedniej szybie. Po lewej są dwie wersje: *Large* (11\", na ramieniu) i *Compact* (10\", przy szybie); w środkowym gnieździe jest *Centre*, zwrócony w stronę kierowcy (z osobną wersją dla kabin z kierownicą po prawej).",
        done="To wszystko. Ekran włącza się razem z zapłonem ciężarówki. Zainstalowany instalatorem CabinPlay sam się aktualizuje: nową wersję pobiera w trakcie gry i instaluje po jej zamknięciu i pokazuje to na ekranie; w *Ustawienia* > *Aktualizacje* możesz kazać mu najpierw pytać albo to wyłączyć. Aby usunąć CabinPlay, otwórz w Windows *Ustawienia* > *Aplikacje* albo uruchom `Uninstall.exe` w `%LocalAppData%\\Programs\\CabinPlay`; jeśli instalacja była ręczna, usuń skopiowane pliki.",
        use="Obsługa",
        shortcut="Skrót", does="Co robi", space="Spacja",
        k_control="Obsługa ekranu z poziomu gry myszą i klawiaturą; gra jest w tym czasie wstrzymana (wyjście: `Esc`)",
        k_nav="Mapa nawigacji", k_home="Ekran główny", k_play="Odtwarzaj / wstrzymaj", k_next="Następny / poprzedni",
        k_seek="10 sekund do przodu / do tyłu", k_full="Wideo na pełnym ekranie",
        k_apps="Poprzednia / następna otwarta aplikacja",
        k_split="Dwie aplikacje obok siebie, wł. / wył.", k_swap="Zamień strony miejscami",
        use_note="Odtwarzanie wstrzymuje się, gdy zatrzymasz grę lub wyłączysz zapłon. Język zmienisz w **Ustawieniach** na ekranie. Jeśli zamkniesz CabinPlay w ustawieniach, `Ctrl+Alt+C` uruchomi go ponownie.",
        truck_note="Aplikacja **Ciężarówka** pokazuje uszkodzenia ciężarówki i naczepy, ładunek, paliwo i wskaźniki. Skróty Ctrl+Alt działają także wtedy, gdy gra jest na pierwszym planie, a ich klawisze nie trafiają do gry, więc skrót nie włączy klaksonu ani niczego w ciężarówce.",
        mirror_note="Aplikacja **Lusterka** pokazuje na ekranie lusterka ciężarówki: lewe, lewe szerokokątne oraz, jeśli jest, przednie. Dotknij jednego, aby je powiększyć.",
        browser_note="Aplikacja **Przeglądarka** otwiera dowolną stronę: wpisz adres lub wyszukiwanie w pasku u góry i naciśnij gwiazdkę, aby zachować stronę na stronie startowej. W serwisach pozostajesz zalogowany, a `Ctrl+Alt+F` włącza pełny ekran dla wideo na stronie. Do pisania potrzebny jest tryb sterowania (`Ctrl+Alt+C`).",
        custom_note="Widok **obok siebie** pokazuje dwie aplikacje naraz, na przykład mapę po lewej i muzykę po prawej: naciśnij przycisk podziału w doku i wybierz aplikację dla każdej połowy. W **Ustawieniach** możesz wybrać tło, także z własnych obrazów, zmienić klawisze skrótów i przypisać każdy z nich do przycisku na kierownicy lub padzie.",
        places_note="**Więcej miejsc:** poza przednią szybą warsztat ma ekran także na *Dashboard* (deska rozdzielcza), *Air vent* (kratka nawiewu), *Overhead* (sufit), *Passenger seat* (fotel pasażera), *Console* (konsola), *Cup holder* (uchwyt na kubek, nowsze ciężarówki) i *Passenger side* (strona pasażera na szybie). Do oglądania z leżanki jest 27-calowy **CabinPlay TV**: na tylnej ścianie (gniazdo panelu świetlnego), płasko lub wysunięty w stronę poduszki, albo na przedniej szybie, płasko lub zwrócony do leżanki. W Scanii S obracaj głowę w prawo dalej, za widok do tyłu: kamera przesunie się na leżankę i zatrzyma przy poduszce, zwrócona do telewizora.",
        needs="Wymagania",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 lub 11",
        need_2="Nie wymaga DLC",
        need_3="Tryb jednoosobowy: nie działa z TruckersMP",
        need_4="Pasuje do 25 ciężarówek; nie do Renault E-Tech T",
        help="Pomoc i społeczność",
        help_body="Pytania, problemy i nowości: **[dołącz do naszego Discorda]({discord})**.\n\nAby zgłosić problem, otwórz **Ustawienia** na ekranie, naciśnij **Utwórz raport o błędzie** i wyślij plik zip zapisany na pulpicie.",
        stutter="**Wideo się zacina lub zamiera?** Gra zajmuje całą kartę graficzną. W ustawieniach grafiki gry zmniejsz **Skalowanie** do 200% lub mniej (najczęstszą przyczyną jest 400%).",
        business="Kontakt",
        business_body="**Współpraca i sponsoring:** napisz na **[{email}](mailto:{email})** albo odezwij się na [Discordzie]({discord}).\n\n**Skargi i sugestie:** napisz na ten sam adres albo daj nam znać na [Discordzie]({discord}).",
        credits="Autorzy i licencja",
        credits_body="Stworzone przez **HydRaboN**, nasz serwer społeczności i zespół programistów. CabinPlay jest darmowy i otwartoźródłowy na [licencji GNU GPL v3.0 lub nowszej](LICENSE): możesz go udostępniać i zmieniać, ale wszystko, co z niego powstanie, musi pozostać otwartoźródłowe na tej samej licencji i wskazywać CabinPlay jako źródło. Zgodnie z sekcją 7 licencji nazwa *CabinPlay* i jej logo nie są objęte licencją; zmieniona wersja musi nosić inną nazwę. Zawiera [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay to nieoficjalny projekt fanowski. Nie jest powiązany z SCS Software, Google/YouTube, Twitch ani Apple i nie jest przez nie wspierany.",
    ),
    "fr": dict(
        intro="Un écran multimédia fonctionnel pour la cabine de votre camion dans **Euro Truck Simulator 2**. Regardez YouTube, écoutez de la musique et suivez la carte GPS du jeu sur une tablette fixée au pare-brise, tout en conduisant.",
        download="Téléchargement",
        downloads="Téléchargements",
        watch="Voir la vidéo complète avec le son",
        scan_line="🛡️ **Analyse antivirus :** le programme d'installation a été analysé sur VirusTotal par {vt_engines} antivirus, et aucun ne l'a signalé. **[Voir le rapport]({virustotal})**",
        vt_clean="{vt_score} sain",
        vt_caption="Aucun des {vt_engines} antivirus de VirusTotal n'a signalé CabinPlay.",
        vt_report="Voir le rapport",
        download_body="**[Téléchargez la dernière version]({repo}/releases/latest)** et prenez le fichier `CabinPlay-Setup-x.y.z.exe`.",
        install="Installation",
        install_intro="Fermez le jeu, puis choisissez une méthode :",
        auto="**A) Programme d'installation (recommandé).** Téléchargez **{setup}** et lancez-le. Il trouve le jeu tout seul et ne demande pas de droits d'administrateur. Si Windows indique qu'il a protégé votre ordinateur, cliquez sur *Informations complémentaires*, puis sur *Exécuter quand même* (le programme n'est pas signé).",
        manual="**B) À la main.** Téléchargez plutôt **{zip}**, décompressez-le et copiez-en trois éléments :",
        manual_1="`cabinplay.scs` dans `Documents\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` et `plugin\\cabinplay.ini` dans le dossier `bin\\win_x64\\plugins` du jeu (créez `plugins` s'il n'existe pas). Pour trouver le dossier du jeu : dans Steam, clic droit sur le jeu > *Gérer* > *Parcourir les fichiers locaux*.",
        manual_3="le dossier `app` où vous voulez, puis lancez une fois `CabinPlay.exe` qui s'y trouve. Ensuite, il démarre tout seul avec le jeu.",
        then="**Ensuite, dans le jeu :**",
        game_1="Lancez le jeu et acceptez l'avertissement sur les « fonctionnalités SDK », affiché une seule fois.",
        game_2="Dans le **Gestionnaire de mods**, activez **CabinPlay Screen**.",
        game_3="Dans un garage, installez **CabinPlay Screen** dans un emplacement d'accessoire du pare-brise. À gauche, deux versions existent : *Large* (11\", sur un bras) et *Compact* (10\", contre la vitre) ; au milieu, il y a *Centre*, tourné vers le conducteur (avec une version à part pour les cabines à conduite à droite).",
        done="C'est tout. L'écran s'allume avec le contact du camion. Installé avec le programme d'installation, CabinPlay se met à jour tout seul : il télécharge la nouvelle version pendant que vous jouez et l'installe quand vous fermez le jeu et l'indique sur l'écran ; dans *Réglages* > *Mises à jour*, vous pouvez lui demander de vous consulter d'abord, ou le désactiver. Pour désinstaller CabinPlay, ouvrez *Paramètres* > *Applications* dans Windows, ou lancez `Uninstall.exe` dans `%LocalAppData%\\Programs\\CabinPlay` ; si vous l'avez installé à la main, supprimez les fichiers copiés.",
        use="Utilisation",
        shortcut="Raccourci", does="Action", space="Espace",
        k_control="Utiliser l'écran depuis le jeu avec la souris et le clavier ; le jeu est en pause pendant ce temps (`Échap` pour quitter)",
        k_nav="Carte de navigation", k_home="Écran d'accueil", k_play="Lecture / pause", k_next="Suivant / précédent",
        k_seek="Avancer / reculer de 10 secondes", k_full="Vidéo en plein écran",
        k_apps="App ouverte précédente / suivante",
        k_split="Deux apps côte à côte, activer / désactiver", k_swap="Échanger les deux côtés",
        use_note="La lecture se met en pause quand vous mettez le jeu en pause ou coupez le contact. La langue se règle dans **Réglages**, à l'écran. Si vous avez fermé CabinPlay depuis ses réglages, `Ctrl+Alt+C` le relance.",
        truck_note="L'app **Camion** affiche les dégâts du camion et de la remorque, la cargaison, le carburant et les indicateurs. Les raccourcis Ctrl+Alt fonctionnent aussi quand le jeu est au premier plan, et leurs touches n'atteignent jamais le jeu : un raccourci ne déclenche ni le klaxon ni rien d'autre sur le camion.",
        mirror_note="L'app **Rétroviseurs** affiche les rétroviseurs du camion à l'écran : le rétro gauche, son grand angle et, s'il y en a un, le rétro avant. Touchez-en un pour l'agrandir.",
        browser_note="L'app **Navigateur** ouvre n'importe quel site : saisissez une adresse ou une recherche dans la barre du haut, et touchez l'étoile pour garder une page sur la page de démarrage. Vous restez connecté aux sites, et `Ctrl+Alt+F` passe la vidéo d'une page en plein écran. La saisie nécessite le mode contrôle (`Ctrl+Alt+C`).",
        custom_note="La vue **côte à côte** affiche deux apps en même temps, par exemple la carte à gauche et la musique à droite : touchez le bouton de partage dans le dock et choisissez l'app de chaque moitié. Dans **Réglages**, vous pouvez choisir un arrière-plan, y compris parmi vos propres images, changer les touches des raccourcis et placer chacun d'eux sur un bouton de votre volant ou de votre manette.",
        places_note="**Autres emplacements :** en plus du pare-brise, le garage propose l'écran pour le *Dashboard* (tableau de bord), l'*Air vent* (aérateur), *Overhead* (plafond), le *Passenger seat* (siège passager), la *Console* (console centrale), le *Cup holder* (porte-gobelet, camions récents) et le *Passenger side* (côté passager du pare-brise). Pour regarder depuis la couchette, il y a la **CabinPlay TV** de 27\" : sur la paroi arrière (emplacement du panneau lumineux), à plat ou sortie vers l'oreiller, ou sur le pare-brise, à plat ou tournée vers la couchette. Dans le Scania S, continuez à tourner la tête vers la droite au-delà de la vue arrière : la caméra passe sur la couchette et s'arrête à l'oreiller, face à la télé.",
        needs="Prérequis",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 ou 11",
        need_2="Aucun DLC requis",
        need_3="Solo uniquement : ne fonctionne pas avec TruckersMP",
        need_4="Compatible avec 25 camions ; pas avec le Renault E-Tech T",
        help="Aide et communauté",
        help_body="Questions, problèmes et nouveautés : **[rejoignez notre Discord]({discord})**.\n\nPour signaler un problème, ouvrez **Réglages** à l'écran, appuyez sur **Créer un rapport d'erreur** et publiez le fichier zip enregistré sur le bureau.",
        stutter="**La vidéo saccade ou se fige ?** Le jeu utilise toute votre carte graphique. Dans les paramètres graphiques du jeu, baissez la **Mise à l'échelle** à 200 % ou moins (400 % en est la cause habituelle).",
        business="Contact",
        business_body="**Collaboration et sponsoring :** écrivez à **[{email}](mailto:{email})** ou contactez-nous sur [Discord]({discord}).\n\n**Réclamations et suggestions :** écrivez à la même adresse ou dites-le-nous sur [Discord]({discord}).",
        credits="Crédits et licence",
        credits_body="Réalisé par **HydRaboN**, notre serveur communautaire et équipe logicielle. CabinPlay est gratuit et open source sous [licence GNU GPL v3.0 ou ultérieure](LICENSE) : vous pouvez le partager et le modifier, mais tout ce qui en est tiré doit rester open source sous la même licence et citer CabinPlay. Selon la section 7 de la licence, le nom *CabinPlay* et son logo ne sont pas concédés ; une version modifiée doit porter un autre nom. Il inclut [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay est un projet de fans non officiel. Il n'est ni affilié à SCS Software, Google/YouTube, Twitch ou Apple, ni approuvé par eux.",
    ),
    "es": dict(
        intro="Una pantalla multimedia que funciona de verdad en la cabina de tu camión en **Euro Truck Simulator 2**. Mira YouTube, escucha música y sigue el mapa GPS del juego en una tableta fijada al parabrisas, mientras conduces.",
        download="Descarga",
        downloads="Descargas",
        watch="Ver el vídeo completo con sonido",
        scan_line="🛡️ **Análisis antivirus:** el instalador se ha analizado en VirusTotal con {vt_engines} antivirus y ninguno lo ha marcado. **[Ver el informe]({virustotal})**",
        vt_clean="{vt_score} limpio",
        vt_caption="Ninguno de los {vt_engines} antivirus de VirusTotal ha marcado CabinPlay.",
        vt_report="Ver el informe",
        download_body="**[Descarga la última versión]({repo}/releases/latest)** y elige el archivo `CabinPlay-Setup-x.y.z.exe`.",
        install="Instalación",
        install_intro="Cierra el juego y después elige una opción:",
        auto="**A) Instalador (recomendado).** Descarga **{setup}** y ejecútalo. Encuentra el juego por sí mismo y no pide permisos de administrador. Si Windows dice que protegió tu PC, pulsa *Más información* y luego *Ejecutar de todas formas* (el instalador no está firmado).",
        manual="**B) A mano.** Descarga en su lugar **{zip}**, descomprímelo y copia tres cosas de él:",
        manual_1="`cabinplay.scs` en `Documentos\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` y `plugin\\cabinplay.ini` en la carpeta `bin\\win_x64\\plugins` del juego (crea `plugins` si no existe). Para encontrar la carpeta del juego: en Steam, clic derecho en el juego > *Administrar* > *Ver archivos locales*.",
        manual_3="la carpeta `app` donde quieras; después inicia una vez `CabinPlay.exe` dentro de ella. A partir de ahí se abre solo con el juego.",
        then="**Después, en el juego:**",
        game_1="Inicia el juego y acepta el aviso de \"funciones del SDK\" que aparece una vez.",
        game_2="En el **Gestor de mods**, activa **CabinPlay Screen**.",
        game_3="En un taller, monta **CabinPlay Screen** en una ranura de accesorios del parabrisas. A la izquierda hay dos versiones: *Large* (11\", en un brazo) y *Compact* (10\", junto al cristal); en la ranura central está *Centre*, girada hacia el conductor (con una versión propia para cabinas con volante a la derecha).",
        done="Eso es todo. La pantalla se enciende con el contacto del camión. Si lo instalaste con el instalador, CabinPlay se actualiza solo: descarga la nueva versión mientras juegas y la instala al cerrar el juego y lo muestra en la pantalla; en *Ajustes* > *Actualizaciones* puedes hacer que pregunte antes o desactivarlo. Para quitar CabinPlay, abre *Configuración* > *Aplicaciones* en Windows o ejecuta `Uninstall.exe` en `%LocalAppData%\\Programs\\CabinPlay`; si lo instalaste a mano, borra los archivos que copiaste.",
        use="Uso",
        shortcut="Atajo", does="Qué hace", space="Espacio",
        k_control="Usar la pantalla desde el juego con ratón y teclado; el juego se pausa mientras tanto (`Esc` para salir)",
        k_nav="Mapa de navegación", k_home="Pantalla de inicio", k_play="Reproducir / pausar", k_next="Siguiente / anterior",
        k_seek="10 segundos adelante / atrás", k_full="Vídeo a pantalla completa",
        k_apps="App abierta anterior / siguiente",
        k_split="Dos apps en paralelo, sí / no", k_swap="Intercambiar los dos lados",
        use_note="La reproducción se pausa cuando pausas el juego o quitas el contacto. El idioma está en **Ajustes**, en la pantalla. Si cerraste CabinPlay desde sus ajustes, `Ctrl+Alt+C` lo vuelve a iniciar.",
        truck_note="La app **Camión** muestra los daños del camión y del remolque, la carga, el combustible y los indicadores. Los atajos Ctrl+Alt también funcionan con el juego en primer plano, y sus teclas nunca llegan al juego: un atajo no hace sonar la bocina ni acciona nada del camión.",
        mirror_note="La app **Retrovisores** muestra en la pantalla los retrovisores del camión: el izquierdo, su gran angular y, si lo tiene, el frontal. Toca uno para ampliarlo.",
        browser_note="La app **Navegador** abre cualquier sitio web: escribe una dirección o una búsqueda en la barra superior y pulsa la estrella para guardar una página en la página de inicio. Las sesiones de los sitios se mantienen iniciadas, y `Ctrl+Alt+F` pone el vídeo de una página a pantalla completa. Para escribir hace falta el modo de control (`Ctrl+Alt+C`).",
        custom_note="La vista **en paralelo** muestra dos apps a la vez, por ejemplo el mapa a la izquierda y la música a la derecha: pulsa el botón de división del dock y elige la app de cada mitad. En **Ajustes** puedes elegir un fondo, también entre tus propias imágenes, cambiar las teclas de los atajos y asignar cualquiera de ellos a un botón de tu volante o mando.",
        places_note="**Más sitios:** además del parabrisas, el taller tiene la pantalla para el *Dashboard* (salpicadero), el *Air vent* (rejilla de ventilación), *Overhead* (techo), el *Passenger seat* (asiento del acompañante), la *Console* (consola), el *Cup holder* (portavasos, camiones nuevos) y el *Passenger side* (lado del acompañante del parabrisas). Para ver desde la litera está la **CabinPlay TV** de 27\": en la pared trasera (ranura del panel de luces), plana o abierta hacia la almohada, o en el parabrisas, plana o girada hacia la litera. En el Scania S, sigue girando la cabeza a la derecha después de mirar atrás: la cámara pasa a la litera y se detiene en la almohada, frente a la tele.",
        needs="Requisitos",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 u 11",
        need_2="No requiere DLC",
        need_3="Un jugador: no funciona con TruckersMP",
        need_4="Compatible con 25 camiones; no con el Renault E-Tech T",
        help="Ayuda y comunidad",
        help_body="Preguntas, problemas y novedades: **[únete a nuestro Discord]({discord})**.\n\nPara informar de un problema, abre **Ajustes** en la pantalla, pulsa **Crear informe de error** y publica el zip que guarda en el escritorio.",
        stutter="**¿El vídeo va a tirones o se congela?** El juego está usando toda tu tarjeta gráfica. En los ajustes gráficos del juego, baja el **Escalado** al 200 % o menos (el 400 % suele ser la causa).",
        business="Contacto",
        business_body="**Colaboración y patrocinio:** escribe a **[{email}](mailto:{email})** o contáctanos en [Discord]({discord}).\n\n**Quejas y sugerencias:** escribe a la misma dirección o cuéntanoslo en [Discord]({discord}).",
        credits="Créditos y licencia",
        credits_body="Hecho por **HydRaboN**, nuestro servidor de comunidad y equipo de software. CabinPlay es gratuito y de código abierto bajo la [licencia GNU GPL v3.0 o posterior](LICENSE): puedes compartirlo y modificarlo, pero todo lo que se haga a partir de él debe seguir siendo de código abierto con la misma licencia y citar a CabinPlay. Según la sección 7 de la licencia, el nombre *CabinPlay* y su logotipo no están incluidos, y una versión modificada debe llevar otro nombre. Incluye [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay es un proyecto de fans no oficial. No está afiliado a SCS Software, Google/YouTube, Twitch ni Apple, ni cuenta con su respaldo.",
    ),
    "ru": dict(
        intro="Работающий медиаэкран в кабине грузовика в **Euro Truck Simulator 2**. Смотрите YouTube, слушайте музыку и следите за GPS-картой игры на планшете у лобового стекла прямо во время поездки.",
        download="Скачать",
        downloads="Загрузки",
        watch="Смотреть полное видео со звуком",
        scan_line="🛡️ **Проверка на вирусы:** установщик проверен на VirusTotal {vt_engines} антивирусами, ни один ничего не нашёл. **[Открыть отчёт]({virustotal})**",
        vt_clean="{vt_score} чисто",
        vt_caption="Ни один из {vt_engines} антивирусов на VirusTotal не отметил CabinPlay.",
        vt_report="Открыть отчёт",
        download_body="**[Скачайте последнюю версию]({repo}/releases/latest)** и возьмите файл `CabinPlay-Setup-x.y.z.exe`.",
        install="Установка",
        install_intro="Закройте игру, затем выберите способ:",
        auto="**А) Установщик (рекомендуется).** Скачайте **{setup}** и запустите его. Он сам найдёт игру и не требует прав администратора. Если Windows сообщит, что защитила компьютер, нажмите *Подробнее*, затем *Выполнить в любом случае* (установщик не подписан).",
        manual="**Б) Вручную.** Вместо этого скачайте **{zip}**, распакуйте его и скопируйте из него три вещи:",
        manual_1="`cabinplay.scs` в `Документы\\Euro Truck Simulator 2\\mod`",
        manual_2="`plugin\\cabinplay.dll` и `plugin\\cabinplay.ini` в папку игры `bin\\win_x64\\plugins` (создайте `plugins`, если её нет). Как найти папку игры: в Steam щёлкните игру правой кнопкой > *Управление* > *Просмотреть локальные файлы*.",
        manual_3="папку `app` в любое место, затем один раз запустите в ней `CabinPlay.exe`. После этого приложение будет запускаться вместе с игрой само.",
        then="**Затем в игре:**",
        game_1="Запустите игру и подтвердите уведомление о «функциях SDK», которое показывается один раз.",
        game_2="В **Менеджере модификаций** включите **CabinPlay Screen**.",
        game_3="В мастерской установите **CabinPlay Screen** в слот аксессуаров на лобовом стекле. Слева есть две версии: *Large* (11\", на кронштейне) и *Compact* (10\", у стекла); в среднем слоте есть *Centre*, повёрнутый к водителю (с отдельной версией для кабин с правым рулём).",
        done="Готово. Экран включается вместе с зажиганием. Если CabinPlay установлен программой установки, он обновляется сам: скачивает новую версию во время игры и устанавливает её, когда вы закрываете игру и сообщает об этом на экране; в *Настройки* > *Обновления* можно включить вопрос перед установкой или отключить это. Чтобы удалить CabinPlay, откройте в Windows *Параметры* > *Приложения* или запустите `Uninstall.exe` в `%LocalAppData%\\Programs\\CabinPlay`; если вы устанавливали вручную, удалите скопированные файлы.",
        use="Управление",
        shortcut="Сочетание", does="Действие", space="Пробел",
        k_control="Управлять экраном из игры мышью и клавиатурой; игра в это время на паузе (выход: `Esc`)",
        k_nav="Карта навигации", k_home="Главный экран", k_play="Воспроизведение / пауза", k_next="Следующее / предыдущее",
        k_seek="На 10 секунд вперёд / назад", k_full="Видео на весь экран",
        k_apps="Предыдущее / следующее открытое приложение",
        k_split="Два приложения рядом: вкл. / выкл.", k_swap="Поменять стороны местами",
        use_note="Воспроизведение приостанавливается, когда вы ставите игру на паузу или выключаете зажигание. Язык меняется в **Настройках** на экране. Если вы закрыли CabinPlay в настройках, `Ctrl+Alt+C` запустит его снова.",
        truck_note="Приложение **Грузовик** показывает повреждения грузовика и прицепа, груз, топливо и показания приборов. Сочетания Ctrl+Alt работают и тогда, когда игра на переднем плане, а их клавиши в игру не попадают, поэтому сочетание не включит гудок и ничего не переключит в грузовике.",
        mirror_note="Приложение **Зеркала** выводит на экран зеркала грузовика: левое, левое широкоугольное и, если есть, переднее. Коснитесь зеркала, чтобы увеличить его.",
        browser_note="Приложение **Браузер** открывает любой сайт: введите адрес или запрос в строке сверху и нажмите звезду, чтобы оставить страницу на стартовой. Вход на сайтах сохраняется, а `Ctrl+Alt+F` разворачивает видео на странице на весь экран. Для ввода текста нужен режим управления (`Ctrl+Alt+C`).",
        custom_note="Режим **«рядом»** показывает два приложения одновременно, например карту слева и музыку справа: нажмите кнопку разделения в доке и выберите приложение для каждой половины. В **Настройках** можно выбрать фон, в том числе из своих изображений, изменить клавиши сочетаний и назначить любое из них на кнопку руля или геймпада.",
        places_note="**Другие места:** кроме лобового стекла, в мастерской экран можно поставить на *Dashboard* (приборная панель), *Air vent* (дефлектор), *Overhead* (потолок), *Passenger seat* (пассажирское сиденье), *Console* (консоль), *Cup holder* (подстаканник, новые грузовики) и *Passenger side* (пассажирская сторона стекла). Чтобы смотреть с койки, есть 27-дюймовый **CabinPlay TV**: на задней стенке (слот светового панно), плоско или выдвинутый к подушке, либо на лобовом стекле, плоско или повёрнутый к койке. В Scania S продолжайте поворачивать голову вправо дальше взгляда назад: камера переместится на койку и остановится у подушки, лицом к телевизору.",
        needs="Требования",
        need_1="Euro Truck Simulator 2 **1.61**, Windows 10 или 11",
        need_2="DLC не требуется",
        need_3="Только одиночная игра: с TruckersMP не работает",
        need_4="Подходит для 25 грузовиков; не подходит для Renault E-Tech T",
        help="Помощь и сообщество",
        help_body="Вопросы, проблемы и новости: **[присоединяйтесь к нашему Discord]({discord})**.\n\nЧтобы сообщить о проблеме, откройте **Настройки** на экране, нажмите **Создать отчёт об ошибке** и отправьте zip-файл, сохранённый на рабочем столе.",
        stutter="**Видео тормозит или зависает?** Игра полностью загружает видеокарту. В настройках графики игры уменьшите **Масштабирование** до 200% или ниже (обычно причина — 400%).",
        business="Контакты",
        business_body="**Сотрудничество и спонсорство:** пишите на **[{email}](mailto:{email})** или свяжитесь с нами в [Discord]({discord}).\n\n**Жалобы и предложения:** пишите на тот же адрес или расскажите нам в [Discord]({discord}).",
        credits="Авторы и лицензия",
        credits_body="Сделано **HydRaboN** — нашим сервером сообщества и командой разработчиков. CabinPlay бесплатен и распространяется с открытым исходным кодом по [лицензии GNU GPL v3.0 или более поздней](LICENSE): его можно распространять и изменять, но всё, что сделано на его основе, должно оставаться открытым под той же лицензией и указывать CabinPlay. Согласно разделу 7 лицензии, название *CabinPlay* и его логотип не лицензируются, а изменённая версия должна называться иначе. Включает [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).",
        disclaimer="CabinPlay — неофициальный фанатский проект. Он не связан с SCS Software, Google/YouTube, Twitch или Apple и не одобрен ими.",
    ),
}

# Only the English README carries this: it is for people who build the project.
BUILDING = """## Building from source

Needs Python 3, MinGW-w64 gcc, the .NET 8 SDK and [Inno Setup 6](https://jrsoftware.org/isinfo.php)
(`winget install JRSoftware.InnoSetup`) on Windows.

```powershell
.\\build.ps1      # builds the mod, plugin, app, and in dist\\ the setup, Uninstall.exe and the zip
.\\install.ps1    # installs that build on this PC without the setup
```

The setup is made from `packaging\\setup.iss`; `packaging\\uninstall\\` is the small `Uninstall.exe`
that starts the uninstaller the setup registers with Windows. `tools\\make_icon.py` draws the icon.

`tools\\` generates the mod, `plugin\\` is the game plugin, `app\\` is the companion app with the
screen's interface in `app\\ui\\`. The version number lives in `VERSION`. This README and its
translations are generated by `tools\\build_readme.py`.
The city and country names on the truck page come from the game's own files:
`tools\\build_places.py` regenerates `app\\ui\\places.js` after a map expansion.

"""

TEMPLATE = """# CabinPlay

{languages}

{intro}

{showcase}
## {download}

{download_body}

> {scan_line}

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
| `Ctrl+Alt+PgUp` `PgDn` | {k_apps} |
| `Ctrl+Alt+S` | {k_split} |
| `Ctrl+Alt+X` | {k_swap} |

{use_note}

{truck_note}

{mirror_note}

{browser_note}

{custom_note}

{places_note}

## {needs}

- {need_1}
- {need_2}
- {need_3}
- {need_4}

## {help}

{help_body}

{stutter}

## {business}

{business_body}

{building}## {credits}

{credits_body}

{disclaimer}
"""


# The download counter, the VirusTotal badge, the video preview, the screenshots and the
# VirusTotal result; only the badge labels and
# one caption change with the language.
SHOWCASE = """<p align="center">
  <a href="{releases}"><img src="{badge}" alt="{downloads}"></a>
  <a href="{virustotal}"><img src="{vt_badge}" alt="VirusTotal: {vt_clean}"></a>
</p>

<p align="center">
  <a href="{video}"><img src="{media}/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="{video}">{watch}</a></sub>
</p>

<p align="center">
  <img src="{media}/driving-day.jpg" width="49%" alt="">
  <img src="{media}/home-screen.jpg" width="49%" alt="">
</p>
<p align="center">
  <img src="{media}/cabin-night.jpg" width="32.5%" alt="">
  <img src="{media}/navigation.jpg" width="32.5%" alt="">
  <img src="{media}/driving-night.jpg" width="32.5%" alt="">
</p>
<p align="center">
  <img src="{media}/cabin-places.jpg" width="32.5%" alt="">
  <img src="{media}/cabin-places-side.jpg" width="32.5%" alt="">
  <img src="{media}/bunk-tv.jpg" width="32.5%" alt="">
</p>
<p align="center">
  <img src="{media}/truck-app.jpg" width="32.5%" alt="">
  <img src="{media}/mirrors.jpg" width="32.5%" alt="">
  <img src="{media}/driving-football.jpg" width="32.5%" alt="">
</p>

<p align="center">
  <a href="{virustotal}"><img src="{media}/virustotal.png" width="100%" alt="VirusTotal: {vt_clean}"></a><br>
  <sub>🛡️ {vt_caption} <a href="{virustotal}">{vt_report}</a></sub>
</p>
"""


RELEASE_SECTION = """{intro}

> {scan_line}

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

**{needs}:** {need_1}. {need_2}. {need_3}.

**{help}:** {help_line}

{stutter}

{business_body}
"""


def badge_text(code, text):
    """The badges are drawn in capitals; shields.io would turn Turkish i into I, not İ."""
    if code == "tr":
        text = text.replace("i", "İ").replace("ı", "I")
    return quote(text.upper())


def release_notes(version):
    """Text for the GitHub release page: English in full, every other language folded."""
    parts = []
    for code, (name, filename) in LANGUAGES.items():
        values = {k: v.format(repo=REPO, discord=DISCORD, email=EMAIL, virustotal=VIRUSTOTAL, vt_score=VT_SCORE, vt_engines=VT_ENGINES,
                              setup="CabinPlay-Setup-%s.exe" % version, zip="CabinPlay-%s-manual-install.zip" % version)
                  for k, v in TEXT[code].items()}
        values["help_line"] = values["help_body"].replace("\n\n", " ")
        section = RELEASE_SECTION.format(**values)
        if code in WHATS_NEW:
            section = WHATS_NEW[code].format(version=version) + "\n\n" + section
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
        values = {k: v.format(repo=REPO, discord=DISCORD, email=EMAIL, virustotal=VIRUSTOTAL, vt_score=VT_SCORE, vt_engines=VT_ENGINES,
                              setup="CabinPlay-Setup-x.y.z.exe", zip="CabinPlay-x.y.z-manual-install.zip")
                  for k, v in TEXT[code].items()}
        missing = set(TEXT["en"]) - set(TEXT[code])
        assert not missing, "%s lacks: %s" % (code, ", ".join(sorted(missing)))
        showcase = SHOWCASE.format(video=VIDEO, media=MEDIA, watch=values["watch"],
                                   releases=REPO + "/releases/latest", downloads=values["downloads"],
                                   badge=BADGE + badge_text(code, values["downloads"]),
                                   virustotal=VIRUSTOTAL, vt_clean=values["vt_clean"],
                                   vt_caption=values["vt_caption"], vt_report=values["vt_report"],
                                   vt_badge=VT_BADGE + badge_text(code, values["vt_clean"]))
        text = TEMPLATE.format(languages=bar, building=BUILDING if code == "en" else "",
                               showcase=showcase, **values)
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
