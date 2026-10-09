# CabinPlay

Euro Truck Simulator 2 (1.61) için kabin içi ekran: ön cama esnek kolla takılan 12 inçlik bir
ekran aksesuarı ve o ekranda YouTube ile oyunun navigasyon haritasını gösteren araç ekranı tarzı
bir arayüz.

## Parçalar

| Parça | Nerede | Ne yapar |
|---|---|---|
| `cabinplay.scs` | `Belgeler\Euro Truck Simulator 2\mod` | Ekran aksesuarını (model, materyal, tanımlar) oyuna ekler |
| `cabinplay.dll` | `<oyun>\bin\win_x64\plugins` | Ekranın dokusunu canlı görüntüyle değiştirir; kontak, duraklatma ve navigasyon bilgisini okur; oyun içi kontrolü sağlar |
| `CabinPlay.exe` | `dist\app` (masaüstünde kısayol) | CabinPlay arayüzünü ve YouTube'u çalıştırır, görüntüyü eklentiye yollar |

Ses bilgisayardan normal şekilde çıkar. Uygulama kapalıyken oyundaki ekran kapalı (siyah) durur.

Oyunla birlikte davranış:

- **Kontak kapalıyken** ekran kararır ve çalan şey durur; kontak açılınca kaldığı yerden devam eder.
- **Oyun duraklatıldığında** (menü dahil) çalan şey durur, oyuna dönünce devam eder.
- **Haritalar** uygulaması oyunun kendi GPS haritasını gösterir; hız, hız sınırı, varış saati ve
  kalan mesafe üstüne yazılır. Harita yalnızca oyundaki ekranda görünür, masaüstü penceresinde
  o alan siyahtır.

## Kullanım

1. Oyunu başlat. CabinPlay uygulaması oyunla birlikte arka planda kendiliğinden açılır ve oyun
   kapanınca kapanır (`cabinplay.ini` içinde `autostart=0` ile kapatılabilir). İlk açılışta
   "SDK özellikleri kullanılıyor" uyarısı çıkar, onayla.
2. Mod Yöneticisi'nde **CabinPlay Screen** modunu etkinleştir.
3. Servise gir, tırın iç aksesuarlarında ön cam (sol) yuvasından ekranı tak. Bu yuva oyunun
   taşınabilir navigasyon cihazının takıldığı yerdir. İki sürüm var:
   - **CabinPlay Screen (Large):** 11 inç, esnek kolla yukarıda ve sürücüye yakın.
   - **CabinPlay Screen (Compact):** 10 inç, camın dibinde, navigasyon cihazının durduğu yerde.

Uygulamanın penceresini görmek istersen sistem tepsisindeki CabinPlay simgesinden **Göster**'i seç;
masaüstü kısayoluyla oyun olmadan da açılabilir. Arayüz dili Ayarlar'dan değişir: İngilizce, Türkçe,
Almanca, Lehçe, Fransızca, İspanyolca, Rusça. Ayarlar'daki **Tanılama kaydet** düğmesi, destek için
gereken günlükleri masaüstüne tek bir zip olarak yazar.

### Oyunun içinden kontrol

`Ctrl+Alt+C` ekranı oyunun ortasında büyük olarak açar. Bu moddayken fare ekrandaki imleci
oynatır, tıklama, tekerlek ve klavye CabinPlay'e gider (YouTube'da arama yazabilirsin); oyun bu
sırada fare ve klavyeyi görmez. Direksiyon ve gamepad çalışmaya devam eder, klavyeyle sürüyorsan
tır o sırada komut almaz. `Esc` veya tekrar `Ctrl+Alt+C` ile çıkılır; oyun duraklatılınca mod
kendiliğinden kapanır.

Tırı fareyle sürüyorsan ve fare oyunda kalsın istiyorsan `cabinplay.ini` içinde `control_mouse=0` yap:
o zaman imleci ok tuşları oynatır, `Enter` tıklar, `Page Up` / `Page Down` kaydırır, `Shift+Enter`
gerçek Enter yazar.

Ekrandaki uygulamayı fareyle CabinPlay penceresinden de yönetebilirsin. Oyundayken şu kısayollar çalışır:

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
- Oyunu pencereli veya kenarlıksız modda çalıştırmak, CabinPlay penceresine geçişi kolaylaştırır.
- Eklenti oyunun içine yüklendiği için TruckersMP'de kullanılamaz.

## Sorun giderme

- **Ekran siyah kalıyor:** CabinPlay uygulaması açık mı? `<oyun>\bin\win_x64\plugins\cabinplay.log`
  dosyasında `screen texture found` ve `connected to the companion app` satırları olmalı.
- **Görüntü baş aşağı:** `cabinplay.ini` içinde `flip_v=1` yap.
- **Harita gelmiyor:** günlükte `navigation texture found` satırı olmalı. Yoksa `ignored a 1024x512
  render target` satırına bak.
- **Kontrol modu açılmıyor:** günlükte `input hooks: ... installed` satırı olmalı. İmleç hızı ve
  ekranın boyutu `cabinplay.ini` içindeki `cursor_speed` ve `overlay_size` ile ayarlanır;
  kısayol tuşu `control_key` ile değişir.
- **Aksesuar listede yok:** `Belgeler\Euro Truck Simulator 2\game.log.txt` içinde `cabinplay` ara.
- Uygulamanın kendi günlüğü: `%LocalAppData%\CabinPlay\app.log`

## Değiştirme ve yeniden derleme

```powershell
.\build.ps1      # dist\ altına mod, eklenti ve uygulamayı üretir
.\install.ps1    # dosyaları yerlerine kopyalar (oyun ve CabinPlay uygulaması kapalıyken)
.\install.ps1 -Uninstall
```

- Ekranın konumu, boyutu ve açısı: `tools\build_mod.py` başındaki `VARIANTS` listesi (sürüm başına
  `screen`, `body`, `center`, `tilt`, `yaw`). `center` metre cinsindendir: sağ, yukarı, sürücüye doğru.
  `tools\preview_model.py` modeli oyunu açmadan PNG olarak çizer.
- Ana ekrana uygulama eklemek: `app\ui\apps.js`.
- Arayüz: `app\ui\` (HTML/CSS/JS).

Gerekenler: Python 3, MinGW-w64 gcc, .NET 8 SDK, WebView2 Runtime.
