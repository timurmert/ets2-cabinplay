# CabinPlay

[English](README.md) · **Türkçe** · [Deutsch](README.de.md) · [Polski](README.pl.md) · [Français](README.fr.md) · [Español](README.es.md) · [Русский](README.ru.md)

**Euro Truck Simulator 2** için tır kabininde gerçekten çalışan bir medya ekranı. Sürerken ön cama takılı tablette YouTube izle, müzik dinle ve oyunun GPS haritasını takip et.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/releases/latest"><img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Ftimurmert%2Fets2-cabinplay%2Fbadges%2Fdownloads.json&style=for-the-badge&logo=github&logoColor=white&labelColor=24292f&color=2ea44f&label=%C4%B0ND%C4%B0RME" alt="İndirme"></a>
  <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc"><img src="https://img.shields.io/static/v1?style=for-the-badge&logo=virustotal&logoColor=white&labelColor=394EFF&color=2ea44f&label=VirusTotal&message=0/67%20TEM%C4%B0Z" alt="VirusTotal: 0/67 temiz"></a>
</p>

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4"><img src="docs/media/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4">Videonun tamamını sesli izle</a></sub>
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
  <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc"><img src="docs/media/virustotal.png" width="100%" alt="VirusTotal: 0/67 temiz"></a><br>
  <sub>🛡️ VirusTotal'daki 67 antivirüs motorunun hiçbiri CabinPlay'i zararlı bulmadı. <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc">Raporu gör</a></sub>
</p>

## İndir

**[Son sürümü indir](https://github.com/timurmert/ets2-cabinplay/releases/latest)**; `CabinPlay-Setup-x.y.z.exe` adlı dosyayı al.

> 🛡️ **Virüs taraması:** Kurulum dosyası VirusTotal'da 67 antivirüs motoruyla tarandı, hiçbiri zararlı bulmadı. **[Raporu gör](https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc)**

## Kur

Oyunu kapat, sonra birini seç:

**A) Kurulum (önerilen).** **CabinPlay-Setup-x.y.z.exe** dosyasını indir ve çalıştır. Oyunu kendisi bulur, yönetici izni istemez. Windows "kişisel bilgisayarınızı korudu" derse *Ek bilgi*, sonra *Yine de çalıştır* de (kurulum imzalı değil).

**B) Elle.** Bunun yerine **CabinPlay-x.y.z-manual-install.zip** dosyasını indir, aç ve içinden üç şeyi kopyala:

1. `cabinplay.scs` dosyasını `Belgeler\Euro Truck Simulator 2\mod` klasörüne
2. `plugin\cabinplay.dll` ve `plugin\cabinplay.ini` dosyalarını oyunun `bin\win_x64\plugins` klasörüne (`plugins` yoksa oluştur). Oyun klasörünü bulmak için: Steam'de oyuna sağ tıkla > *Yönet* > *Yerel dosyalara göz at*.
3. `app` klasörünü istediğin bir yere; sonra içindeki `CabinPlay.exe` dosyasını bir kez çalıştır. Sonrasında oyunla birlikte kendiliğinden açılır.

**Sonra, oyunda:**

1. Oyunu başlat ve bir kez çıkan "SDK özellikleri" uyarısını onayla.
2. **Mod Yöneticisi**'nde **CabinPlay Screen**'i etkinleştir.
3. Serviste **CabinPlay Screen**'i ön camdaki bir aksesuar yuvasına tak. Sol yuvada iki sürüm var: *Large* (11", kolda) ve *Compact* (10", camın dibinde); orta yuvada ise sürücüye dönük *Centre* var (sağdan direksiyonlu kabinler için ayrı bir sürümüyle).

Bu kadar. Ekran tırın kontağıyla birlikte açılır. CabinPlay'i kaldırmak için Windows *Ayarlar* > *Uygulamalar*'ı aç ya da `%LocalAppData%\Programs\CabinPlay` klasöründeki `Uninstall.exe`'yi çalıştır; elle kurduysan kopyaladığın dosyaları sil.

## Kullan

| Kısayol | Ne yapar |
|---|---|
| `Ctrl+Alt+C` | Ekranı oyunun içinden fare ve klavyeyle kullan; bu sırada oyun duraklar (çıkış: `Esc`) |
| `Ctrl+Alt+N` | Navigasyon haritası |
| `Ctrl+Alt+H` | Ana ekran |
| `Ctrl+Alt+Boşluk` | Oynat / duraklat |
| `Ctrl+Alt+→` `←` | Sonraki / önceki |
| `Ctrl+Alt+↑` `↓` | 10 saniye ileri / geri |
| `Ctrl+Alt+F` | Videoyu tam ekran yap |
| `Ctrl+Alt+PgUp` `PgDn` | Önceki / sonraki açık uygulama |
| `Ctrl+Alt+S` | İki uygulamayı yan yana aç / kapat |
| `Ctrl+Alt+X` | İki tarafı değiştir |

Oyunu duraklattığında veya kontağı kapattığında çalan şey durur. Dil, ekrandaki **Ayarlar**'dan değişir. CabinPlay'i ayarlardan kapattıysan `Ctrl+Alt+C` onu yeniden başlatır.

**Tır** uygulaması tırın ve dorsenin hasarını, yükü, yakıtı ve göstergeleri gösterir. Ctrl+Alt kısayolları oyun öndeyken de çalışır ve tuşları oyuna gitmez; bu yüzden kısayollar korna çalmaz, tırda hiçbir şeyi açıp kapatmaz.

**Aynalar** uygulaması tırın kendi aynalarını ekrana getirir: sol ayna, sol geniş açı aynası ve tırda varsa ön ayna. Büyütmek için birine dokun.

**Tarayıcı** uygulaması istediğin siteyi açar: üstteki çubuğa adres ya da arama yaz, bir sayfayı başlangıç sayfasında tutmak için yıldıza bas. Giriş yaptığın sitelerde oturumun açık kalır; `Ctrl+Alt+F` sayfadaki videoyu tam ekran yapar. Yazı yazmak için kontrol modu gerekir (`Ctrl+Alt+C`).

**Yan yana** görünüm iki uygulamayı aynı anda gösterir; örneğin solda harita, sağda müzik: kenar çubuğundaki bölme düğmesine bas ve her yarı için uygulamayı seç. **Ayarlar**'dan arka planı (kendi resimlerinden de) seçebilir, kısayol tuşlarını değiştirebilir ve her birini direksiyonundaki ya da oyun kolundaki bir düğmeye atayabilirsin.

**Başka yerler:** Serviste ekranı ön camın dışında *Dashboard* (torpido), *Air vent* (havalandırma), *Overhead* (tavan), *Passenger seat* (yolcu koltuğu), *Console* (konsol), *Cup holder* (bardaklık, yeni tırlarda) ve *Passenger side* (camın yolcu tarafı) olarak da takabilirsin. Yatakta izlemek için 27" **CabinPlay TV** var: arka duvara (Işık Panosu yuvası) düz ya da yastığa doğru açılmış, veya ön cama düz ya da yatağa dönük. Scania S'te kafanı sağa çevirmeye arkaya baktıktan sonra da devam et: kamera yatağa geçer ve yastıkta, TV'ye bakarak durur.

## Gerekenler

- Euro Truck Simulator 2 **1.61**, Windows 10 veya 11
- DLC gerekmez
- Tek oyunculu: TruckersMP ile çalışmaz
- 25 tıra uyar; Renault E-Tech T'ye uymaz

## Yardım ve topluluk

Sorular, sorunlar ve duyurular için **[Discord sunucumuza katıl](https://discord.gg/hydrabon)**.

Sorun bildirmek için ekrandaki **Ayarlar**'ı aç, **Hata raporu oluştur**'a bas ve masaüstüne kaydettiği zip'i paylaş.

**Video takılıyor ya da donuyor mu?** Oyun ekran kartının tamamını kullanıyordur. Oyunun grafik ayarlarında **Ölçekleme**'yi %200'e ya da altına indir (genelde neden %400'dür).

## İletişim

**İş birliği ve sponsorluk için:** **[contact@hydrabon.com](mailto:contact@hydrabon.com)** adresine yaz ya da bize [Discord](https://discord.gg/hydrabon) üzerinden ulaş.

**Şikayet ve önerilerin için:** aynı adrese yaz ya da [Discord](https://discord.gg/hydrabon) sunucumuzda bize ilet.

## Emeği geçenler ve lisans

Topluluk sunucumuz ve yazılım ekibimiz **HydRaboN** tarafından yapıldı. CabinPlay ücretsiz ve açık kaynaklıdır; [MIT lisansı](LICENSE) ile yayınlanır. İçinde [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause) kullanılır.

CabinPlay resmi olmayan bir hayran projesidir. SCS Software, Google/YouTube, Twitch veya Apple ile bağlantılı değildir ve onlar tarafından desteklenmez.
