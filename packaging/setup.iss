; CabinPlay setup, built by build.ps1 with Inno Setup 6:
;   ISCC /DAppVersion=1.2.3 /DDist=<dist folder> /DPictures=<folder of the wizard pictures> setup.iss
;
; Installs what install.ps1 installs, without a script for people to distrust:
;   mod      -> Documents\Euro Truck Simulator 2\mod\cabinplay.scs
;   plugin   -> <game>\bin\win_x64\plugins\cabinplay.dll (+ .ini)
;   app      -> %LocalAppData%\Programs\CabinPlay (+ Uninstall.exe)
; Everything is in the user's own folders and the game's Steam folder, so it needs no
; administrator rights. Windows lists it under Settings > Apps, with an Uninstall button.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef Dist
  #define Dist "..\dist"
#endif
#ifndef Compression
  ; Every packing installs the same files. Antivirus engines that guess from the bytes
  ; judge them differently: for 1.2.0, lzma2/ultra64 got 2 such flags on VirusTotal and
  ; lzma/ultra64 none. To try another: ISCC /DCompression=lzma2/max ...
  #define Compression "lzma/ultra64"
#endif
#ifndef Pictures
  #define Pictures "..\build\setup"
#endif

[Setup]
; Never change the AppId: Windows finds an earlier install through it to update or remove
; it, and packaging/uninstall/uninstall.c looks it up.
AppId={{EA7CAAE8-A55D-479D-86EB-8C4F281D283D}
AppName=CabinPlay
AppVersion={#AppVersion}
AppVerName=CabinPlay {#AppVersion}
AppPublisher=HydRaboN
AppPublisherURL=https://github.com/timurmert/ets2-cabinplay
AppSupportURL=https://discord.gg/hydrabon
AppUpdatesURL=https://github.com/timurmert/ets2-cabinplay/releases/latest
AppCopyright=Copyright (C) 2026 HydRaboN, GPL-3.0-or-later
VersionInfoVersion={#AppVersion}
VersionInfoDescription=CabinPlay Setup
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DefaultDirName={autopf}\CabinPlay
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
; We close our own app ourselves; the game must be closed by the player (see PrepareToInstall).
CloseApplications=no
RestartApplications=no
SetupIconFile=..\app\CabinPlay.ico
UninstallDisplayIcon={app}\CabinPlay.exe
UninstallDisplayName=CabinPlay
WizardStyle=modern
WizardImageFile={#Pictures}\wizard-large-100.bmp,{#Pictures}\wizard-large-200.bmp
WizardSmallImageFile={#Pictures}\wizard-small-100.bmp,{#Pictures}\wizard-small-200.bmp
WizardImageAlphaFormat=defined
WizardImageStretch=no
; The colour around the picture when the page is larger; the same as the picture's own.
WizardImageBackColor=$301610
OutputDir={#Dist}
OutputBaseFilename=CabinPlay-Setup-{#AppVersion}
Compression={#Compression}
SolidCompression=yes
ShowLanguageDialog=auto

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "pl"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
; The tasks page is the last one before installing; its button says Install, not Next.
en.SelectTasksLabel2=Select the additional tasks you would like Setup to perform while installing [name], then click Install.
tr.SelectTasksLabel2=[name] kurulumu sırasında yapılmasını istediğiniz ek işleri seçin ve Kur üzerine tıklayın.
de.SelectTasksLabel2=Wählen Sie die zusätzlichen Aufgaben aus, die das Setup während der Installation von [name] ausführen soll, und klicken Sie danach auf "Installieren".
pl.SelectTasksLabel2=Zaznacz dodatkowe zadania, które instalator ma wykonać podczas instalacji aplikacji [name], a następnie kliknij przycisk Instaluj.
fr.SelectTasksLabel2=Sélectionnez les tâches supplémentaires que l'assistant d'installation doit effectuer pendant l'installation de [name], puis cliquez sur Installer.
es.SelectTasksLabel2=Seleccione las tareas adicionales que desea que se realicen durante la instalación de [name] y haga clic en Instalar.
ru.SelectTasksLabel2=Выберите дополнительные задачи, которые должны выполниться при установке [name], после этого нажмите «Установить».
en.FinishedLabelNoIcons=CabinPlay is installed.%n%nNow, in the game:%n1. Start the game and accept the "SDK features" notice it shows once.%n2. In the Mod Manager, switch on "CabinPlay Screen".%n3. In a truck workshop, fit "CabinPlay Screen" in a windscreen accessory slot, or in any other place the workshop lists it in.%n%nThe screen switches on with the truck's ignition.
en.FinishedLabel=CabinPlay is installed.%n%nNow, in the game:%n1. Start the game and accept the "SDK features" notice it shows once.%n2. In the Mod Manager, switch on "CabinPlay Screen".%n3. In a truck workshop, fit "CabinPlay Screen" in a windscreen accessory slot, or in any other place the workshop lists it in.%n%nThe screen switches on with the truck's ignition.
tr.FinishedLabelNoIcons=CabinPlay kuruldu.%n%nŞimdi oyunda:%n1. Oyunu başlat ve bir kez çıkan "SDK özellikleri" uyarısını onayla.%n2. Mod Yöneticisi'nde "CabinPlay Screen"i etkinleştir.%n3. Serviste "CabinPlay Screen"i ön camdaki bir aksesuar yuvasına ya da servisin gösterdiği başka bir yere tak.%n%nEkran tırın kontağıyla birlikte açılır.
tr.FinishedLabel=CabinPlay kuruldu.%n%nŞimdi oyunda:%n1. Oyunu başlat ve bir kez çıkan "SDK özellikleri" uyarısını onayla.%n2. Mod Yöneticisi'nde "CabinPlay Screen"i etkinleştir.%n3. Serviste "CabinPlay Screen"i ön camdaki bir aksesuar yuvasına ya da servisin gösterdiği başka bir yere tak.%n%nEkran tırın kontağıyla birlikte açılır.
de.FinishedLabelNoIcons=CabinPlay ist installiert.%n%nJetzt im Spiel:%n1. Starte das Spiel und bestätige den Hinweis zu den "SDK-Funktionen", der einmal erscheint.%n2. Aktiviere im Mod-Manager "CabinPlay Screen".%n3. Baue in einer Lkw-Werkstatt "CabinPlay Screen" in einen Zubehörplatz an der Windschutzscheibe ein oder an einen anderen Platz, den die Werkstatt anbietet.%n%nDer Bildschirm schaltet sich mit der Zündung ein.
de.FinishedLabel=CabinPlay ist installiert.%n%nJetzt im Spiel:%n1. Starte das Spiel und bestätige den Hinweis zu den "SDK-Funktionen", der einmal erscheint.%n2. Aktiviere im Mod-Manager "CabinPlay Screen".%n3. Baue in einer Lkw-Werkstatt "CabinPlay Screen" in einen Zubehörplatz an der Windschutzscheibe ein oder an einen anderen Platz, den die Werkstatt anbietet.%n%nDer Bildschirm schaltet sich mit der Zündung ein.
pl.FinishedLabelNoIcons=CabinPlay został zainstalowany.%n%nTeraz w grze:%n1. Uruchom grę i zaakceptuj komunikat o "funkcjach SDK", który pojawia się raz.%n2. W Menedżerze modów włącz "CabinPlay Screen".%n3. W warsztacie zamontuj "CabinPlay Screen" w gnieździe akcesoriów na przedniej szybie albo w innym miejscu, które pokazuje warsztat.%n%nEkran włącza się razem z zapłonem ciężarówki.
pl.FinishedLabel=CabinPlay został zainstalowany.%n%nTeraz w grze:%n1. Uruchom grę i zaakceptuj komunikat o "funkcjach SDK", który pojawia się raz.%n2. W Menedżerze modów włącz "CabinPlay Screen".%n3. W warsztacie zamontuj "CabinPlay Screen" w gnieździe akcesoriów na przedniej szybie albo w innym miejscu, które pokazuje warsztat.%n%nEkran włącza się razem z zapłonem ciężarówki.
fr.FinishedLabelNoIcons=CabinPlay est installé.%n%nEnsuite, dans le jeu :%n1. Lancez le jeu et acceptez l'avertissement sur les « fonctionnalités SDK », affiché une seule fois.%n2. Dans le Gestionnaire de mods, activez « CabinPlay Screen ».%n3. Dans un garage, installez « CabinPlay Screen » dans un emplacement d'accessoire du pare-brise, ou à un autre endroit proposé par le garage.%n%nL'écran s'allume avec le contact du camion.
fr.FinishedLabel=CabinPlay est installé.%n%nEnsuite, dans le jeu :%n1. Lancez le jeu et acceptez l'avertissement sur les « fonctionnalités SDK », affiché une seule fois.%n2. Dans le Gestionnaire de mods, activez « CabinPlay Screen ».%n3. Dans un garage, installez « CabinPlay Screen » dans un emplacement d'accessoire du pare-brise, ou à un autre endroit proposé par le garage.%n%nL'écran s'allume avec le contact du camion.
es.FinishedLabelNoIcons=CabinPlay está instalado.%n%nAhora, en el juego:%n1. Inicia el juego y acepta el aviso de "funciones del SDK" que aparece una vez.%n2. En el Gestor de mods, activa "CabinPlay Screen".%n3. En un taller, monta "CabinPlay Screen" en una ranura de accesorios del parabrisas o en otro sitio que ofrezca el taller.%n%nLa pantalla se enciende con el contacto del camión.
es.FinishedLabel=CabinPlay está instalado.%n%nAhora, en el juego:%n1. Inicia el juego y acepta el aviso de "funciones del SDK" que aparece una vez.%n2. En el Gestor de mods, activa "CabinPlay Screen".%n3. En un taller, monta "CabinPlay Screen" en una ranura de accesorios del parabrisas o en otro sitio que ofrezca el taller.%n%nLa pantalla se enciende con el contacto del camión.
ru.FinishedLabelNoIcons=CabinPlay установлен.%n%nТеперь в игре:%n1. Запустите игру и подтвердите уведомление о «функциях SDK», которое показывается один раз.%n2. В Менеджере модификаций включите «CabinPlay Screen».%n3. В мастерской установите «CabinPlay Screen» в слот аксессуаров на лобовом стекле или в другое место, которое предлагает мастерская.%n%nЭкран включается вместе с зажиганием.
ru.FinishedLabel=CabinPlay установлен.%n%nТеперь в игре:%n1. Запустите игру и подтвердите уведомление о «функциях SDK», которое показывается один раз.%n2. В Менеджере модификаций включите «CabinPlay Screen».%n3. В мастерской установите «CabinPlay Screen» в слот аксессуаров на лобовом стекле или в другое место, которое предлагает мастерская.%n%nЭкран включается вместе с зажиганием.

[CustomMessages]
en.GameCaption=Euro Truck Simulator 2
en.GameDescription=Where is the game installed?
en.GameFound=Setup found the game in this folder:
en.GameNotFound=Setup could not find the game by itself. In Steam, right-click the game > Manage > Browse local files, and choose the folder that opens.
en.NotGameFolder=This folder does not contain Euro Truck Simulator 2 (bin\win_x64\eurotrucks2.exe is missing).
en.GameRunning=Euro Truck Simulator 2 is running. Close the game, then click Retry.
en.WebViewInstalling=Installing the Microsoft WebView2 Runtime...
en.WebViewFailed=The Microsoft WebView2 Runtime could not be installed. CabinPlay needs it to show its pages: install it from https://go.microsoft.com/fwlink/p/?LinkId=2124703 before starting the game.
en.DeleteUserData=Also delete your CabinPlay settings, backgrounds and website sign-ins (YouTube, Twitch...)?
en.UpdateCaption=CabinPlay update
en.UpdatingTitle=Updating CabinPlay
en.UpdatingText=From version %1 to %2. This window closes by itself.
en.UpdatedTitle=CabinPlay is updated ✓
en.UpdatedText=Version %1 is installed.
tr.GameCaption=Euro Truck Simulator 2
tr.GameDescription=Oyun nerede kurulu?
tr.GameFound=Kurulum oyunu bu klasörde buldu:
tr.GameNotFound=Kurulum oyunu kendisi bulamadı. Steam'de oyuna sağ tıkla > Yönet > Yerel dosyalara göz at, açılan klasörü burada seç.
tr.NotGameFolder=Bu klasörde Euro Truck Simulator 2 yok (bin\win_x64\eurotrucks2.exe bulunamadı).
tr.GameRunning=Euro Truck Simulator 2 açık. Oyunu kapat, sonra Yeniden Dene'ye tıkla.
tr.WebViewInstalling=Microsoft WebView2 Runtime kuruluyor...
tr.WebViewFailed=Microsoft WebView2 Runtime kurulamadı. CabinPlay sayfalarını göstermek için buna ihtiyaç duyar: oyunu başlatmadan önce https://go.microsoft.com/fwlink/p/?LinkId=2124703 adresinden kur.
tr.DeleteUserData=CabinPlay ayarların, arka planların ve sitelerdeki oturumların (YouTube, Twitch...) da silinsin mi?
tr.UpdateCaption=CabinPlay güncellemesi
tr.UpdatingTitle=CabinPlay güncelleniyor
tr.UpdatingText=%1 sürümünden %2 sürümüne. Bu pencere kendiliğinden kapanacak.
tr.UpdatedTitle=CabinPlay güncellendi ✓
tr.UpdatedText=%1 sürümü kuruldu.
de.GameCaption=Euro Truck Simulator 2
de.GameDescription=Wo ist das Spiel installiert?
de.GameFound=Das Setup hat das Spiel in diesem Ordner gefunden:
de.GameNotFound=Das Setup hat das Spiel nicht selbst gefunden. In Steam: Rechtsklick auf das Spiel > Verwalten > Lokale Dateien durchsuchen, und wähle den Ordner, der sich öffnet.
de.NotGameFolder=Dieser Ordner enthält kein Euro Truck Simulator 2 (bin\win_x64\eurotrucks2.exe fehlt).
de.GameRunning=Euro Truck Simulator 2 läuft. Schließe das Spiel und klicke dann auf Wiederholen.
de.WebViewInstalling=Die Microsoft WebView2 Runtime wird installiert...
de.WebViewFailed=Die Microsoft WebView2 Runtime konnte nicht installiert werden. CabinPlay braucht sie, um seine Seiten anzuzeigen: Installiere sie vor dem Spielstart von https://go.microsoft.com/fwlink/p/?LinkId=2124703.
de.DeleteUserData=Auch deine CabinPlay-Einstellungen, Hintergründe und Anmeldungen auf Websites (YouTube, Twitch...) löschen?
de.UpdateCaption=CabinPlay-Update
de.UpdatingTitle=CabinPlay wird aktualisiert
de.UpdatingText=Von Version %1 auf %2. Dieses Fenster schließt sich von selbst.
de.UpdatedTitle=CabinPlay ist aktualisiert ✓
de.UpdatedText=Version %1 ist installiert.
pl.GameCaption=Euro Truck Simulator 2
pl.GameDescription=Gdzie jest zainstalowana gra?
pl.GameFound=Instalator znalazł grę w tym folderze:
pl.GameNotFound=Instalator nie znalazł gry samodzielnie. W Steam kliknij grę prawym przyciskiem > Zarządzaj > Przeglądaj pliki lokalne i wybierz folder, który się otworzy.
pl.NotGameFolder=Ten folder nie zawiera Euro Truck Simulator 2 (brak bin\win_x64\eurotrucks2.exe).
pl.GameRunning=Euro Truck Simulator 2 jest uruchomiony. Zamknij grę, a potem kliknij Ponów próbę.
pl.WebViewInstalling=Instalowanie Microsoft WebView2 Runtime...
pl.WebViewFailed=Nie udało się zainstalować Microsoft WebView2 Runtime. CabinPlay potrzebuje go do wyświetlania stron: zainstaluj go z https://go.microsoft.com/fwlink/p/?LinkId=2124703 przed uruchomieniem gry.
pl.DeleteUserData=Usunąć także ustawienia CabinPlay, tła i zalogowania na stronach (YouTube, Twitch...)?
pl.UpdateCaption=Aktualizacja CabinPlay
pl.UpdatingTitle=Aktualizowanie CabinPlay
pl.UpdatingText=Z wersji %1 do %2. To okno zamknie się samo.
pl.UpdatedTitle=CabinPlay zaktualizowany ✓
pl.UpdatedText=Zainstalowano wersję %1.
fr.GameCaption=Euro Truck Simulator 2
fr.GameDescription=Où le jeu est-il installé ?
fr.GameFound=Le programme d'installation a trouvé le jeu dans ce dossier :
fr.GameNotFound=Le programme d'installation n'a pas trouvé le jeu tout seul. Dans Steam, clic droit sur le jeu > Gérer > Parcourir les fichiers locaux, puis choisissez le dossier qui s'ouvre.
fr.NotGameFolder=Ce dossier ne contient pas Euro Truck Simulator 2 (bin\win_x64\eurotrucks2.exe est introuvable).
fr.GameRunning=Euro Truck Simulator 2 est en cours d'exécution. Fermez le jeu, puis cliquez sur Réessayer.
fr.WebViewInstalling=Installation de Microsoft WebView2 Runtime...
fr.WebViewFailed=Microsoft WebView2 Runtime n'a pas pu être installé. CabinPlay en a besoin pour afficher ses pages : installez-le depuis https://go.microsoft.com/fwlink/p/?LinkId=2124703 avant de lancer le jeu.
fr.DeleteUserData=Supprimer aussi vos réglages CabinPlay, vos arrière-plans et vos connexions aux sites (YouTube, Twitch...) ?
fr.UpdateCaption=Mise à jour de CabinPlay
fr.UpdatingTitle=Mise à jour de CabinPlay
fr.UpdatingText=De la version %1 à la version %2. Cette fenêtre se ferme toute seule.
fr.UpdatedTitle=CabinPlay est à jour ✓
fr.UpdatedText=La version %1 est installée.
es.GameCaption=Euro Truck Simulator 2
es.GameDescription=¿Dónde está instalado el juego?
es.GameFound=El instalador encontró el juego en esta carpeta:
es.GameNotFound=El instalador no encontró el juego por sí mismo. En Steam, clic derecho en el juego > Administrar > Ver archivos locales, y elige la carpeta que se abre.
es.NotGameFolder=Esta carpeta no contiene Euro Truck Simulator 2 (falta bin\win_x64\eurotrucks2.exe).
es.GameRunning=Euro Truck Simulator 2 está abierto. Cierra el juego y pulsa Reintentar.
es.WebViewInstalling=Instalando Microsoft WebView2 Runtime...
es.WebViewFailed=No se pudo instalar Microsoft WebView2 Runtime. CabinPlay lo necesita para mostrar sus páginas: instálalo desde https://go.microsoft.com/fwlink/p/?LinkId=2124703 antes de iniciar el juego.
es.DeleteUserData=¿Borrar también tus ajustes de CabinPlay, tus fondos y tus sesiones en sitios web (YouTube, Twitch...)?
es.UpdateCaption=Actualización de CabinPlay
es.UpdatingTitle=Actualizando CabinPlay
es.UpdatingText=De la versión %1 a la %2. Esta ventana se cierra sola.
es.UpdatedTitle=CabinPlay está actualizado ✓
es.UpdatedText=La versión %1 está instalada.
ru.GameCaption=Euro Truck Simulator 2
ru.GameDescription=Где установлена игра?
ru.GameFound=Программа установки нашла игру в этой папке:
ru.GameNotFound=Программа установки не нашла игру сама. В Steam щёлкните игру правой кнопкой > Управление > Просмотреть локальные файлы и выберите открывшуюся папку.
ru.NotGameFolder=В этой папке нет Euro Truck Simulator 2 (не найден bin\win_x64\eurotrucks2.exe).
ru.GameRunning=Euro Truck Simulator 2 запущена. Закройте игру и нажмите «Повторить».
ru.WebViewInstalling=Установка Microsoft WebView2 Runtime...
ru.WebViewFailed=Не удалось установить Microsoft WebView2 Runtime. Он нужен CabinPlay для показа страниц: установите его с https://go.microsoft.com/fwlink/p/?LinkId=2124703 перед запуском игры.
ru.DeleteUserData=Удалить также настройки CabinPlay, фоны и входы на сайтах (YouTube, Twitch...)?
ru.UpdateCaption=Обновление CabinPlay
ru.UpdatingTitle=Обновление CabinPlay
ru.UpdatingText=С версии %1 на %2. Это окно закроется само.
ru.UpdatedTitle=CabinPlay обновлён ✓
ru.UpdatedText=Версия %1 установлена.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[InstallDelete]
; A desktop shortcut left by the old installer when the box is now unticked.
Type: files; Name: "{userdesktop}\CabinPlay.lnk"

[Files]
Source: "{#Dist}\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Dist}\Uninstall.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Dist}\cabinplay.scs"; DestDir: "{userdocs}\Euro Truck Simulator 2\mod"; Flags: ignoreversion
Source: "{#Dist}\plugin\cabinplay.dll"; DestDir: "{code:PluginDir}"; Flags: ignoreversion
; Merged into the player's own cabinplay.ini by MergeIni, so their settings survive updates.
Source: "{#Dist}\plugin\cabinplay.ini"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
; The plugin contains MinHook, whose licence asks for its notice to travel with binaries.
Source: "..\plugin\minhook\LICENSE.txt"; DestDir: "{app}"; DestName: "THIRD-PARTY-LICENSES.txt"; Flags: ignoreversion

[Dirs]
; Site sign-ins, backgrounds and logs; kept on uninstall unless the player says otherwise.
Name: "{localappdata}\CabinPlay"; Flags: uninsneveruninstall

[Icons]
Name: "{userprograms}\CabinPlay"; Filename: "{app}\CabinPlay.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\CabinPlay"; Filename: "{app}\CabinPlay.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
; Written by the app (where it is, where the plugin is); removed with it.
Root: HKCU; Subkey: "Software\CabinPlay"; Flags: uninsdeletekey

[Run]
; After an update the player started from the app's own Update button (with the game closed),
; the app comes back by itself. Updates run when the game closes leave it to the game.
Filename: "{app}\CabinPlay.exe"; Flags: nowait; Check: RestartApp

[UninstallDelete]
; Also what an older version or the old install script left in the app's own folder.
Type: filesandordirs; Name: "{app}"
Type: files; Name: "{code:PluginDir}\cabinplay.ini"
Type: files; Name: "{code:PluginDir}\cabinplay.log"
; Leave the plugins folder alone if other plugins live there.
Type: dirifempty; Name: "{code:PluginDir}"

[Code]
const
  WebViewClient = 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  WebViewBootstrapper = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';

var
  GamePage: TInputDirWizardPage;
  DownloadPage: TDownloadWizardPage;

function IsGameDir(Dir: String): Boolean;
begin
  Result := (Dir <> '') and FileExists(AddBackslash(Dir) + 'bin\win_x64\eurotrucks2.exe');
end;

{ Steam keeps its library folders in libraryfolders.vdf, lines like:  "path"  "E:\\SteamLibrary" }
function FindGameDir: String;
var
  Steam, Line, Path, Candidate: String;
  Lines: TArrayOfString;
  I, Q: Integer;
begin
  Result := '';
  if not RegQueryStringValue(HKCU, 'Software\Valve\Steam', 'SteamPath', Steam) then
    Exit;
  StringChangeEx(Steam, '/', '\', True);
  Candidate := AddBackslash(Steam) + 'steamapps\common\Euro Truck Simulator 2';
  if IsGameDir(Candidate) then
  begin
    Result := Candidate;
    Exit;
  end;
  if not LoadStringsFromFile(AddBackslash(Steam) + 'steamapps\libraryfolders.vdf', Lines) then
    Exit;
  for I := 0 to GetArrayLength(Lines) - 1 do
  begin
    Line := Trim(Lines[I]);
    if Pos('"path"', Line) <> 1 then
      Continue;
    Path := Trim(Copy(Line, 7, Length(Line)));
    if (Length(Path) < 2) or (Path[1] <> '"') then
      Continue;
    Path := Copy(Path, 2, Length(Path));
    Q := Pos('"', Path);
    if Q > 0 then
      Path := Copy(Path, 1, Q - 1);
    StringChangeEx(Path, '\\', '\', True);
    Candidate := AddBackslash(Path) + 'steamapps\common\Euro Truck Simulator 2';
    if IsGameDir(Candidate) then
    begin
      Result := Candidate;
      Exit;
    end;
  end;
end;

function GameDir: String;
begin
  Result := '';
  if GamePage <> nil then
    Result := RemoveBackslashUnlessRoot(GamePage.Values[0]);
end;

function PluginDir(Param: String): String;
begin
  Result := AddBackslash(GameDir) + 'bin\win_x64\plugins';
end;

function IsRunning(ExeName: String): Boolean;
var
  Locator, Service, Found: Variant;
begin
  Result := False;
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\CIMV2');
    Found := Service.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name = ''' + ExeName + '''');
    Result := Found.Count > 0;
  except
    Result := False;
  end;
end;

{ Asks until the game is closed; False if the player gives up. }
function WaitForGameClosed: Boolean;
begin
  Result := True;
  while IsRunning('eurotrucks2.exe') do
    if SuppressibleMsgBox(CustomMessage('GameRunning'), mbError, MB_RETRYCANCEL, IDCANCEL) = IDCANCEL then
    begin
      Result := False;
      Exit;
    end;
end;

{ Our app starts with the game and stays in the tray; it holds its own files open. }
procedure StopApp;
var
  Code, Tries: Integer;
begin
  if not IsRunning('CabinPlay.exe') then
    Exit;
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM CabinPlay.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
  for Tries := 1 to 10 do
  begin
    if not IsRunning('CabinPlay.exe') then
      Exit;
    Sleep(300);
  end;
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM CabinPlay.exe', '', SW_HIDE, ewWaitUntilTerminated, Code);
  Sleep(500);
end;

function HasWebView: Boolean;
var
  Version: String;
begin
  Result := (RegQueryStringValue(HKLM32, WebViewClient, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0'))
    or (RegQueryStringValue(HKLM64, WebViewClient, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0'))
    or (RegQueryStringValue(HKCU, WebViewClient, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0'));
end;

{ Part of Windows 11 and most Windows 10 PCs; fetched from Microsoft when it is missing. }
procedure InstallWebView;
var
  Code: Integer;
begin
  DownloadPage.Clear;
  DownloadPage.Add(WebViewBootstrapper, 'MicrosoftEdgeWebview2Setup.exe', '');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      DownloadPage.SetText(CustomMessage('WebViewInstalling'), '');
      DownloadPage.ProgressBar.Style := npbstMarquee;
      Exec(ExpandConstant('{tmp}\MicrosoftEdgeWebview2Setup.exe'), '/silent /install', '', SW_HIDE,
        ewWaitUntilTerminated, Code);
    except
      Log('WebView2: ' + GetExceptionMessage);
    end;
  finally
    DownloadPage.ProgressBar.Style := npbstNormal;
    DownloadPage.Hide;
  end;
  if not HasWebView then
    SuppressibleMsgBox(CustomMessage('WebViewFailed'), mbInformation, MB_OK, IDOK);
end;

{ The app runs the setup with /UPDATE=<its version> to update itself; the window then says so. }
function UpdatingFrom: String;
begin
  Result := ExpandConstant('{param:update|}');
end;

function RestartApp: Boolean;
begin
  Result := ExpandConstant('{param:restartapp|0}') = '1';
end;

{ Shows that the update is done for a moment before the window closes by itself. }
procedure ShowUpdated;
var
  I: Integer;
begin
  WizardForm.PageNameLabel.Caption := CustomMessage('UpdatedTitle');
  WizardForm.PageDescriptionLabel.Caption := FmtMessage(CustomMessage('UpdatedText'), ['{#AppVersion}']);
  WizardForm.StatusLabel.Caption := '';
  WizardForm.FilenameLabel.Caption := '';
  WizardForm.ProgressGauge.Position := WizardForm.ProgressGauge.Max;
  for I := 1 to 25 do
  begin
    WizardForm.Refresh;
    Sleep(100);
  end;
end;

procedure InitializeWizard;
var
  Found: String;
begin
  GamePage := CreateInputDirPage(wpWelcome, CustomMessage('GameCaption'), CustomMessage('GameDescription'),
    '', False, '');
  GamePage.Add('');
  Found := GetPreviousData('GameDir', '');
  if not IsGameDir(Found) then
    Found := FindGameDir;
  GamePage.Values[0] := Found;
  if IsGameDir(Found) then
    GamePage.SubCaptionLabel.Caption := CustomMessage('GameFound')
  else
    GamePage.SubCaptionLabel.Caption := CustomMessage('GameNotFound');
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), CustomMessage('WebViewInstalling'), nil);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  { The tasks page is the last one before installing, so its button installs. }
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall);
  { An update from the app: not an install, and nothing technical to read. }
  if ((CurPageID = wpPreparing) or (CurPageID = wpInstalling)) and (UpdatingFrom <> '') then
  begin
    WizardForm.Caption := CustomMessage('UpdateCaption');
    WizardForm.PageNameLabel.Caption := CustomMessage('UpdatingTitle');
    WizardForm.PageDescriptionLabel.Caption := FmtMessage(CustomMessage('UpdatingText'), [UpdatingFrom, '{#AppVersion}']);
    WizardForm.FilenameLabel.Visible := False;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = GamePage.ID) and not IsGameDir(GameDir) then
  begin
    SuppressibleMsgBox(CustomMessage('NotGameFolder'), mbError, MB_OK, IDOK);
    Result := False;
  end;
  { The tasks page is the last one before installing (there is no Ready page). }
  if (CurPageID = wpSelectTasks) and not HasWebView then
    InstallWebView;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not WaitForGameClosed then
  begin
    Result := CustomMessage('GameRunning');
    Exit;
  end;
  StopApp;
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'GameDir', GameDir);
end;

function EndsWithNewLine(FileName: String): Boolean;
var
  Text: AnsiString;
begin
  Result := not LoadStringFromFile(FileName, Text) or (Length(Text) = 0) or (Text[Length(Text)] = #10);
end;

{ Keeps the player's values; adds settings that are new in this version, with their
  comments; tells the plugin where the app is (it starts the app with the game). }
procedure MergeIni;
var
  Target, Shipped, Line, Key, Pending: String;
  Lines: TArrayOfString;
  I, Eq: Integer;
begin
  Target := PluginDir('') + '\cabinplay.ini';
  Shipped := ExpandConstant('{tmp}\cabinplay.ini');
  if not FileExists(Target) then
    FileCopy(Shipped, Target, False)
  else if LoadStringsFromFile(Shipped, Lines) then
  begin
    if not EndsWithNewLine(Target) then
      SaveStringToFile(Target, #13#10, True);
    Pending := '';
    for I := 0 to GetArrayLength(Lines) - 1 do
    begin
      Line := Lines[I];
      if (Length(Trim(Line)) > 0) and (Trim(Line)[1] = ';') then
      begin
        Pending := Pending + Line + #13#10;
        Continue;
      end;
      Eq := Pos('=', Line);
      if (Eq > 0) and (Trim(Line)[1] <> '[') then
      begin
        Key := Trim(Copy(Line, 1, Eq - 1));
        if not IniKeyExists('cabinplay', Key, Target) then
          SaveStringToFile(Target, Pending + Line + #13#10, True);
      end;
      Pending := '';
    end;
  end;
  SetIniString('cabinplay', 'app_path', ExpandConstant('{app}\CabinPlay.exe'), Target);
end;

function JsonString(Value: String): String;
begin
  StringChangeEx(Value, '\', '\\', True);
  StringChangeEx(Value, '"', '\"', True);
  Result := '"' + Value + '"';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MergeIni;
    { Lets the app find the plugin's log for error reports. }
    SaveStringsToUTF8File(ExpandConstant('{localappdata}\CabinPlay\install.json'), [
      '{ "gameDir": ' + JsonString(GameDir) + ' }'], False);
    if UpdatingFrom <> '' then
      ShowUpdated;
  end;
end;

function InitializeUninstall: Boolean;
begin
  Result := WaitForGameClosed;
  if Result then
    StopApp;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and DirExists(ExpandConstant('{localappdata}\CabinPlay')) then
    if SuppressibleMsgBox(CustomMessage('DeleteUserData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES then
      DelTree(ExpandConstant('{localappdata}\CabinPlay'), True, True, True);
end;
