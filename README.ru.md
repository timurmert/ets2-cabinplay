# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · [Polski](README.pl.md) · [Français](README.fr.md) · [Español](README.es.md) · **Русский**

Работающий медиаэкран в кабине грузовика в **Euro Truck Simulator 2**. Смотрите YouTube, слушайте музыку и следите за GPS-картой игры на планшете у лобового стекла прямо во время поездки.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/raw/main/docs/media/cabinplay-demo.mp4"><img src="docs/media/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="https://github.com/timurmert/ets2-cabinplay/raw/main/docs/media/cabinplay-demo.mp4">Смотреть полное видео со звуком</a></sub>
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

## Скачать

**[Скачайте последнюю версию](https://github.com/timurmert/ets2-cabinplay/releases/latest)** и возьмите файл `CabinPlay-x.y.z.zip`.

## Установка

Закройте игру и распакуйте файл. Затем выберите способ:

**А) Автоматически.** Дважды щёлкните **Install.cmd**. Если Windows сообщит, что защитила компьютер, нажмите *Подробнее*, затем *Выполнить в любом случае* (файлы не подписаны). `Install.cmd` только запускает `data\install.ps1` — обычный текстовый скрипт, который можно прочитать.

**Б) Вручную.** Скрипты не запускаются; нужно скопировать три вещи из папки `data`:

1. `cabinplay.scs` в `Документы\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` и `plugin\cabinplay.ini` в папку игры `bin\win_x64\plugins` (создайте `plugins`, если её нет). Как найти папку игры: в Steam щёлкните игру правой кнопкой > *Управление* > *Просмотреть локальные файлы*.
3. папку `app` в любое место, затем один раз запустите в ней `CabinPlay.exe`. После этого приложение будет запускаться вместе с игрой само.

**Затем в игре:**

1. Запустите игру и подтвердите уведомление о «функциях SDK», которое показывается один раз.
2. В **Менеджере модификаций** включите **CabinPlay Screen**.
3. В мастерской установите **CabinPlay Screen** в слот аксессуаров слева на лобовом стекле. Есть две версии: *Large* (11", на кронштейне) и *Compact* (10", у стекла).

Готово. Экран включается вместе с зажиганием. Чтобы удалить, дважды щёлкните **Uninstall.cmd** или удалите файлы, скопированные вручную.

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

Воспроизведение приостанавливается, когда вы ставите игру на паузу или выключаете зажигание. Язык меняется в **Настройках** на экране.

## Требования

- Euro Truck Simulator 2 **1.61**, Windows 10 или 11
- DLC не требуется
- Только одиночная игра: с TruckersMP не работает
- Подходит для 25 грузовиков; не подходит для Renault E-Tech T

## Помощь и сообщество

Вопросы, проблемы и новости: **[присоединяйтесь к нашему Discord](https://discord.gg/rhGEbsu3zy)**.

Чтобы сообщить о проблеме, откройте **Настройки** на экране, нажмите **Создать отчёт об ошибке** и отправьте zip-файл, сохранённый на рабочем столе.

**Видео тормозит или зависает?** Игра полностью загружает видеокарту. В настройках графики игры уменьшите **Масштабирование** до 200% или ниже (обычно причина — 400%).

## Контакты

**Сотрудничество и спонсорство:** пишите на **[contact@hydrabon.com](mailto:contact@hydrabon.com)** или свяжитесь с нами в [Discord](https://discord.gg/rhGEbsu3zy).

**Жалобы и предложения:** пишите на тот же адрес или расскажите нам в [Discord](https://discord.gg/rhGEbsu3zy).

## Авторы и лицензия

Сделано **HydRaboN** — нашим сервером сообщества и командой разработчиков. CabinPlay бесплатен и распространяется с открытым исходным кодом по [лицензии MIT](LICENSE). Включает [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay — неофициальный фанатский проект. Он не связан с SCS Software, Google/YouTube, Twitch или Apple и не одобрен ими.
