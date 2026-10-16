# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · [Polski](README.pl.md) · [Français](README.fr.md) · [Español](README.es.md) · **Русский**

Работающий медиаэкран в кабине грузовика в **Euro Truck Simulator 2**. Смотрите YouTube, слушайте музыку и следите за GPS-картой игры на планшете у лобового стекла прямо во время поездки.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/releases/latest"><img src="https://img.shields.io/github/downloads/timurmert/ets2-cabinplay/total?style=for-the-badge&logo=github&logoColor=white&labelColor=24292f&color=2ea44f&label=%D0%97%D0%90%D0%93%D0%A0%D0%A3%D0%97%D0%9A%D0%98" alt="Загрузки"></a>
  <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc"><img src="https://img.shields.io/static/v1?style=for-the-badge&logo=virustotal&logoColor=white&labelColor=394EFF&color=2ea44f&label=VirusTotal&message=0/67%20%D0%A7%D0%98%D0%A1%D0%A2%D0%9E" alt="VirusTotal: 0/67 чисто"></a>
</p>

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4"><img src="docs/media/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4">Смотреть полное видео со звуком</a></sub>
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
  <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc"><img src="docs/media/virustotal.png" width="100%" alt="VirusTotal: 0/67 чисто"></a><br>
  <sub>🛡️ Ни один из 67 антивирусов на VirusTotal не отметил CabinPlay. <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc">Открыть отчёт</a></sub>
</p>

## Скачать

**[Скачайте последнюю версию](https://github.com/timurmert/ets2-cabinplay/releases/latest)** и возьмите файл `CabinPlay-Setup-x.y.z.exe`.

> 🛡️ **Проверка на вирусы:** установщик проверен на VirusTotal 67 антивирусами, ни один ничего не нашёл. **[Открыть отчёт](https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc)**

## Установка

Закройте игру, затем выберите способ:

**А) Установщик (рекомендуется).** Скачайте **CabinPlay-Setup-x.y.z.exe** и запустите его. Он сам найдёт игру и не требует прав администратора. Если Windows сообщит, что защитила компьютер, нажмите *Подробнее*, затем *Выполнить в любом случае* (установщик не подписан).

**Б) Вручную.** Вместо этого скачайте **CabinPlay-x.y.z-manual-install.zip**, распакуйте его и скопируйте из него три вещи:

1. `cabinplay.scs` в `Документы\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` и `plugin\cabinplay.ini` в папку игры `bin\win_x64\plugins` (создайте `plugins`, если её нет). Как найти папку игры: в Steam щёлкните игру правой кнопкой > *Управление* > *Просмотреть локальные файлы*.
3. папку `app` в любое место, затем один раз запустите в ней `CabinPlay.exe`. После этого приложение будет запускаться вместе с игрой само.

**Затем в игре:**

1. Запустите игру и подтвердите уведомление о «функциях SDK», которое показывается один раз.
2. В **Менеджере модификаций** включите **CabinPlay Screen**.
3. В мастерской установите **CabinPlay Screen** в слот аксессуаров на лобовом стекле. Слева есть две версии: *Large* (11", на кронштейне) и *Compact* (10", у стекла); в среднем слоте есть *Centre*, повёрнутый к водителю (с отдельной версией для кабин с правым рулём).

Готово. Экран включается вместе с зажиганием. Чтобы удалить CabinPlay, откройте в Windows *Параметры* > *Приложения* или запустите `Uninstall.exe` в `%LocalAppData%\Programs\CabinPlay`; если вы устанавливали вручную, удалите скопированные файлы.

## Управление

| Сочетание | Действие |
|---|---|
| `Ctrl+Alt+C` | Управлять экраном из игры мышью и клавиатурой; игра в это время на паузе (выход: `Esc`) |
| `Ctrl+Alt+N` | Карта навигации |
| `Ctrl+Alt+H` | Главный экран |
| `Ctrl+Alt+Пробел` | Воспроизведение / пауза |
| `Ctrl+Alt+→` `←` | Следующее / предыдущее |
| `Ctrl+Alt+↑` `↓` | На 10 секунд вперёд / назад |
| `Ctrl+Alt+F` | Видео на весь экран |
| `Ctrl+Alt+PgUp` `PgDn` | Предыдущее / следующее открытое приложение |
| `Ctrl+Alt+S` | Два приложения рядом: вкл. / выкл. |
| `Ctrl+Alt+X` | Поменять стороны местами |

Воспроизведение приостанавливается, когда вы ставите игру на паузу или выключаете зажигание. Язык меняется в **Настройках** на экране. Если вы закрыли CabinPlay в настройках, `Ctrl+Alt+C` запустит его снова.

Приложение **Грузовик** показывает повреждения грузовика и прицепа, груз, топливо и показания приборов. Сочетания Ctrl+Alt работают и тогда, когда игра на переднем плане, а их клавиши в игру не попадают, поэтому сочетание не включит гудок и ничего не переключит в грузовике.

Приложение **Зеркала** выводит на экран зеркала грузовика: левое, левое широкоугольное и, если есть, переднее. Коснитесь зеркала, чтобы увеличить его.

Приложение **Браузер** открывает любой сайт: введите адрес или запрос в строке сверху и нажмите звезду, чтобы оставить страницу на стартовой. Вход на сайтах сохраняется, а `Ctrl+Alt+F` разворачивает видео на странице на весь экран. Для ввода текста нужен режим управления (`Ctrl+Alt+C`).

Режим **«рядом»** показывает два приложения одновременно, например карту слева и музыку справа: нажмите кнопку разделения в доке и выберите приложение для каждой половины. В **Настройках** можно выбрать фон, в том числе из своих изображений, изменить клавиши сочетаний и назначить любое из них на кнопку руля или геймпада.

**Другие места:** кроме лобового стекла, в мастерской экран можно поставить на *Dashboard* (приборная панель), *Air vent* (дефлектор), *Overhead* (потолок), *Passenger seat* (пассажирское сиденье), *Console* (консоль), *Cup holder* (подстаканник, новые грузовики) и *Passenger side* (пассажирская сторона стекла). Чтобы смотреть с койки, есть 27-дюймовый **CabinPlay TV**: на задней стенке (слот светового панно), плоско или выдвинутый к подушке, либо на лобовом стекле, плоско или повёрнутый к койке. В Scania S продолжайте поворачивать голову вправо дальше взгляда назад: камера переместится на койку и остановится у подушки, лицом к телевизору.

## Требования

- Euro Truck Simulator 2 **1.61**, Windows 10 или 11
- DLC не требуется
- Только одиночная игра: с TruckersMP не работает
- Подходит для 25 грузовиков; не подходит для Renault E-Tech T

## Помощь и сообщество

Вопросы, проблемы и новости: **[присоединяйтесь к нашему Discord](https://discord.gg/hydrabon)**.

Чтобы сообщить о проблеме, откройте **Настройки** на экране, нажмите **Создать отчёт об ошибке** и отправьте zip-файл, сохранённый на рабочем столе.

**Видео тормозит или зависает?** Игра полностью загружает видеокарту. В настройках графики игры уменьшите **Масштабирование** до 200% или ниже (обычно причина — 400%).

## Контакты

**Сотрудничество и спонсорство:** пишите на **[contact@hydrabon.com](mailto:contact@hydrabon.com)** или свяжитесь с нами в [Discord](https://discord.gg/hydrabon).

**Жалобы и предложения:** пишите на тот же адрес или расскажите нам в [Discord](https://discord.gg/hydrabon).

## Авторы и лицензия

Сделано **HydRaboN** — нашим сервером сообщества и командой разработчиков. CabinPlay бесплатен и распространяется с открытым исходным кодом по [лицензии MIT](LICENSE). Включает [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay — неофициальный фанатский проект. Он не связан с SCS Software, Google/YouTube, Twitch или Apple и не одобрен ими.
