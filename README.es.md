# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · [Polski](README.pl.md) · [Français](README.fr.md) · **Español** · [Русский](README.ru.md)

Una pantalla multimedia que funciona de verdad en la cabina de tu camión en **Euro Truck Simulator 2**. Mira YouTube, escucha música y sigue el mapa GPS del juego en una tableta fijada al parabrisas, mientras conduces.

## Descarga

**[Descarga la última versión](https://github.com/timurmert/ets2-cabinplay/releases/latest)** y elige el archivo `CabinPlay-x.y.z.zip`.

## Instalación

Cierra el juego y descomprime el archivo. Después elige una opción:

**A) Automática.** Haz doble clic en **Install.cmd**. Si Windows dice que protegió tu PC, pulsa *Más información* y luego *Ejecutar de todas formas* (los archivos no están firmados). `Install.cmd` solo inicia `data\install.ps1`, un script de texto plano que puedes leer.

**B) A mano.** No se ejecuta ningún script; copias tres cosas de la carpeta `data`:

1. `cabinplay.scs` en `Documentos\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` y `plugin\cabinplay.ini` en la carpeta `bin\win_x64\plugins` del juego (crea `plugins` si no existe). Para encontrar la carpeta del juego: en Steam, clic derecho en el juego > *Administrar* > *Ver archivos locales*.
3. la carpeta `app` donde quieras; después inicia una vez `CabinPlay.exe` dentro de ella. A partir de ahí se abre solo con el juego.

**Después, en el juego:**

1. Inicia el juego y acepta el aviso de "funciones del SDK" que aparece una vez.
2. En el **Gestor de mods**, activa **CabinPlay Screen**.
3. En un taller, monta **CabinPlay Screen** en la ranura de accesorios izquierda del parabrisas. Hay dos versiones: *Large* (11", en un brazo) y *Compact* (10", junto al cristal).

Eso es todo. La pantalla se enciende con el contacto del camión. Para quitarlo, haz doble clic en **Uninstall.cmd** o borra los archivos que copiaste a mano.

## Uso

| Atajo | Qué hace |
|---|---|
| `Ctrl+Alt+C` | Usar la pantalla desde el juego con ratón y teclado (`Esc` para salir) |
| `Ctrl+Alt+N` | Mapa de navegación |
| `Ctrl+Alt+H` | Pantalla de inicio |
| `Ctrl+Alt+Espacio` | Reproducir / pausar |
| `Ctrl+Alt+→` `←` | Siguiente / anterior |
| `Ctrl+Alt+↑` `↓` | 10 segundos adelante / atrás |
| `Ctrl+Alt+F` | Vídeo a pantalla completa |

La reproducción se pausa cuando pausas el juego o quitas el contacto. El idioma está en **Ajustes**, en la pantalla.

Si conduces con el ratón, pon `control_mouse=0` en `cabinplay.ini` (en la carpeta `bin\win_x64\plugins` del juego): el ratón se queda con el camión y las flechas mueven el cursor de la pantalla.

## Requisitos

- Euro Truck Simulator 2 **1.61**, Windows 10 u 11
- El DLC **Cabin Accessories** (aporta la ranura de montaje)
- Un jugador: no funciona con TruckersMP
- Compatible con 23 camiones; no con DAF XF Electric, Renault E-Tech T ni Scania S 2024e

## Ayuda y comunidad

Preguntas, problemas y novedades: **[únete a nuestro Discord](https://discord.gg/rhGEbsu3zy)**.

Para informar de un problema, abre **Ajustes** en la pantalla, pulsa **Crear informe de error** y publica el zip que guarda en el escritorio.

## Contacto

**Colaboración y patrocinio:** escribe a **[contact@hydrabon.com](mailto:contact@hydrabon.com)** o contáctanos en [Discord](https://discord.gg/rhGEbsu3zy).

**Quejas y sugerencias:** escribe a la misma dirección o cuéntanoslo en [Discord](https://discord.gg/rhGEbsu3zy).

## Créditos y licencia

Hecho por **HydRaboN**, nuestro servidor de comunidad y equipo de software. CabinPlay es gratuito y de código abierto bajo la [licencia MIT](LICENSE). Incluye [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay es un proyecto de fans no oficial. No está afiliado a SCS Software, Google/YouTube, Twitch ni Apple, ni cuenta con su respaldo.
