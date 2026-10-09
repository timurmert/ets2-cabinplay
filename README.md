# ETS2 CarPlay

Euro Truck Simulator 2 (1.61) için kabin içi ekran: ön cama esnek kolla takılan 12 inçlik bir
ekran aksesuarı ve o ekranda YouTube ile oyunun navigasyon haritasını gösteren CarPlay tarzı
bir arayüz.

## Parçalar

| Parça | Nerede | Ne yapar |
|---|---|---|
| `ets2_carplay.scs` | `Belgeler\Euro Truck Simulator 2\mod` | Ekran aksesuarını (model, materyal, tanımlar) oyuna ekler |
| `ets2_carplay.dll` | `<oyun>\bin\win_x64\plugins` | Ekranın dokusunu canlı görüntüyle değiştirir; kontak, duraklatma ve navigasyon bilgisini okur; oyun içi kontrolü sağlar |
| `ETS2CarPlay.exe` | `dist\app` (masaüstünde kısayol) | CarPlay arayüzünü ve YouTube'u çalıştırır, görüntüyü eklentiye yollar |

Ses bilgisayardan normal şekilde çıkar. Uygulama kapalıyken oyundaki ekran kapalı (siyah) durur.

Oyunla birlikte davranış:

- **Kontak kapalıyken** ekran kararır ve çalan şey durur; kontak açılınca kaldığı yerden devam eder.
- **Oyun duraklatıldığında** (menü dahil) çalan şey durur, oyuna dönünce devam eder.
- **Haritalar** uygulaması oyunun kendi GPS haritasını gösterir; hız, hız sınırı, varış saati ve
  kalan mesafe üstüne yazılır. Harita yalnızca oyundaki ekranda görünür, masaüstü penceresinde
  o alan siyahtır.

## Kullanım

1. Masaüstündeki **ETS2 CarPlay** kısayolunu aç. 1024×512'lik çerçevesiz bir pencere açılır;
   oyundaki ekran bu pencerenin aynısını gösterir. Pencereyi küçültme, oyunun arkasında kalabilir.
2. Oyunu başlat. İlk açılışta "SDK özellikleri kullanılıyor" uyarısı çıkar, onayla.
3. Mod Yöneticisi'nde **CarPlay Screen** modunu etkinleştir.
4. Servise gir, tırın iç aksesuarlarında ön cam (sol) yuvasından ekranı tak. Bu yuva oyunun
   taşınabilir navigasyon cihazının takıldığı yerdir. İki sürüm var:
   - **CarPlay Screen (Large):** 11 inç, esnek kolla yukarıda ve sürücüye yakın.
   - **CarPlay Screen (Compact):** 10 inç, camın dibinde, navigasyon cihazının durduğu yerde.

### Oyunun içinden kontrol

`Ctrl+Alt+C` ekranı oyunun ortasında büyük olarak açar. Bu moddayken fare ekrandaki imleci
oynatır, tıklama, tekerlek ve klavye CarPlay'e gider (YouTube'da arama yazabilirsin); oyun bu
sırada fare ve klavyeyi görmez. Direksiyon ve gamepad çalışmaya devam eder, klavyeyle sürüyorsan
tır o sırada komut almaz. `Esc` veya tekrar `Ctrl+Alt+C` ile çıkılır; oyun duraklatılınca mod
kendiliğinden kapanır.

Tırı fareyle sürüyorsan ve fare oyunda kalsın istiyorsan `ets2_carplay.ini` içinde `control_mouse=0` yap:
o zaman imleci ok tuşları oynatır, `Enter` tıklar, `Page Up` / `Page Down` kaydırır, `Shift+Enter`
gerçek Enter yazar.

Ekrandaki uygulamayı fareyle CarPlay penceresinden de yönetebilirsin. Oyundayken şu kısayollar çalışır:

| Kısayol | İşlev |
|---|---|
| `Ctrl+Alt+Boşluk` | Oynat / duraklat |
| `Ctrl+Alt+→` / `←` | Sonraki / önceki |
| `Ctrl+Alt+↑` / `↓` | 10 sn ileri / geri |
| `Ctrl+Alt+F` | Videoyu tam ekran yap |
| `Ctrl+Alt+H` | Ana ekran |
| `Ctrl+Alt+N` | Navigasyon (Haritalar) |
| `Ctrl+Alt+C` | Oyunun içinden kontrol |

Pencereyi taşımak için sol üstteki saati sürükle. Sistem tepsisindeki simgeden "her zaman üstte"
ve "çıkış" seçenekleri var.

## Kapsam

- 23 tırda çalışır. DAF XF Electric, Renault E-Tech T ve Scania S 2024e'de ön cam aksesuar
  yuvası olmadığı için bu üçünde ekran takılamaz.
- Yuva Kabin Aksesuarları DLC'siyle gelir.
- Oyunu pencereli veya kenarlıksız modda çalıştırmak, CarPlay penceresine geçişi kolaylaştırır.
- Eklenti oyunun içine yüklendiği için TruckersMP'de kullanılamaz.

## Sorun giderme

- **Ekran siyah kalıyor:** CarPlay uygulaması açık mı? `<oyun>\bin\win_x64\plugins\ets2_carplay.log`
  dosyasında `screen texture found` ve `connected to the companion app` satırları olmalı.
- **Görüntü baş aşağı:** `ets2_carplay.ini` içinde `flip_v=1` yap.
- **Harita gelmiyor:** günlükte `navigation texture found` satırı olmalı. Yoksa `ignored a 1024x512
  render target` satırına bak.
- **Kontrol modu açılmıyor:** günlükte `input hooks: ... installed` satırı olmalı. İmleç hızı ve
  ekranın boyutu `ets2_carplay.ini` içindeki `cursor_speed` ve `overlay_size` ile ayarlanır;
  kısayol tuşu `control_key` ile değişir.
- **Aksesuar listede yok:** `Belgeler\Euro Truck Simulator 2\game.log.txt` içinde `carplay` ara.
- Uygulamanın kendi günlüğü: `%LocalAppData%\ETS2CarPlay\app.log`

## Değiştirme ve yeniden derleme

```powershell
.\build.ps1      # dist\ altına mod, eklenti ve uygulamayı üretir
.\install.ps1    # dosyaları yerlerine kopyalar (oyun ve CarPlay uygulaması kapalıyken)
.\install.ps1 -Uninstall
```

- Ekranın konumu, boyutu ve açısı: `tools\build_mod.py` başındaki `VARIANTS` listesi (sürüm başına
  `screen`, `body`, `center`, `tilt`, `yaw`). `center` metre cinsindendir: sağ, yukarı, sürücüye doğru.
  `tools\preview_model.py` modeli oyunu açmadan PNG olarak çizer.
- Ana ekrana uygulama eklemek: `app\ui\apps.js`.
- Arayüz: `app\ui\` (HTML/CSS/JS).

Gerekenler: Python 3, MinGW-w64 gcc, .NET 8 SDK, WebView2 Runtime.
