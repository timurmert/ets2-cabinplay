# CabinPlay

**Euro Truck Simulator 2** için tır kabininde gerçekten çalışan bir medya ekranı. Sürerken ön cama
takılı tablette YouTube izle, müzik dinle ve oyunun GPS haritasını takip et.

[English](README.md)

## İndir

**[Son sürümü indir](https://github.com/timurmert/ets2-cabinplay/releases/latest)**;
`CabinPlay-x.y.z.zip` adlı dosyayı al.

## Kur

1. Oyunu kapat.
2. Zip'i aç ve **Install.cmd** dosyasına çift tıkla.
   Windows "kişisel bilgisayarınızı korudu" derse *Ek bilgi*, sonra *Yine de çalıştır* de
   (dosyalar imzalı değil).
3. Oyunu başlat ve bir kez çıkan "SDK özellikleri" uyarısını onayla.
4. **Mod Yöneticisi**'nde **CabinPlay Screen**'i etkinleştir.
5. Serviste, sol ön cam aksesuar yuvasına **CabinPlay Screen**'i tak. İki sürüm var:
   *Large* (11", kolda) ve *Compact* (10", camın dibinde).

Bu kadar. Ekran tırın kontağıyla birlikte açılır.

Kaldırmak için **Uninstall.cmd** dosyasına çift tıkla.

## Kullan

| Kısayol | Ne yapar |
|---|---|
| `Ctrl+Alt+C` | Ekranı oyunun içinden fare ve klavyeyle kullan (çıkış: `Esc`) |
| `Ctrl+Alt+N` | Navigasyon haritası |
| `Ctrl+Alt+H` | Ana ekran |
| `Ctrl+Alt+Boşluk` | Oynat / duraklat |
| `Ctrl+Alt+→` `←` | Sonraki / önceki |
| `Ctrl+Alt+↑` `↓` | 10 saniye ileri / geri |
| `Ctrl+Alt+F` | Videoyu tam ekran yap |

Oyunu duraklattığında veya kontağı kapattığında çalan şey durur. Dil (İngilizce, Türkçe, Almanca,
Lehçe, Fransızca, İspanyolca, Rusça) ekrandaki **Ayarlar**'dan değişir.

Tırı fareyle sürüyorsan oyunun `bin\win_x64\plugins` klasöründeki `cabinplay.ini` içinde
`control_mouse=0` yap: fare tırda kalır, ekrandaki imleci ok tuşları oynatır.

## Gerekenler

- Euro Truck Simulator 2 **1.61**, Windows 10 veya 11
- **Kabin Aksesuarları** DLC'si (takma yuvasını o sağlıyor)
- Tek oyunculu: TruckersMP ile çalışmaz
- 23 tıra uyar; DAF XF Electric, Renault E-Tech T ve Scania S 2024e'ye uymaz

## Yardım

Ekrandaki **Ayarlar**'ı aç ve **Hata raporu oluştur**'a bas. Masaüstüne bir zip kaydeder; yardım
isterken o dosyayı paylaş.

---

CabinPlay resmi olmayan bir hayran projesidir. SCS Software, Google/YouTube, Twitch veya Apple ile
bağlantılı değildir ve onlar tarafından desteklenmez.
