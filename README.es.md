# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · [Polski](README.pl.md) · [Français](README.fr.md) · **Español** · [Русский](README.ru.md)

Una pantalla multimedia que funciona de verdad en la cabina de tu camión en **Euro Truck Simulator 2**. Mira YouTube, escucha música y sigue el mapa GPS del juego en una tableta fijada al parabrisas, mientras conduces.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/releases/latest"><img src="https://img.shields.io/github/downloads/timurmert/ets2-cabinplay/total?style=for-the-badge&logo=github&logoColor=white&labelColor=24292f&color=2ea44f&label=DESCARGAS" alt="Descargas"></a>
  <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc"><img src="https://img.shields.io/static/v1?style=for-the-badge&logo=virustotal&logoColor=white&labelColor=394EFF&color=2ea44f&label=VirusTotal&message=0/67%20LIMPIO" alt="VirusTotal: 0/67 limpio"></a>
</p>

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4"><img src="docs/media/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4">Ver el vídeo completo con sonido</a></sub>
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
  <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc"><img src="docs/media/virustotal.png" width="100%" alt="VirusTotal: 0/67 limpio"></a><br>
  <sub>🛡️ Ninguno de los 67 antivirus de VirusTotal ha marcado CabinPlay. <a href="https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc">Ver el informe</a></sub>
</p>

## Descarga

**[Descarga la última versión](https://github.com/timurmert/ets2-cabinplay/releases/latest)** y elige el archivo `CabinPlay-Setup-x.y.z.exe`.

> 🛡️ **Análisis antivirus:** el instalador se ha analizado en VirusTotal con 67 antivirus y ninguno lo ha marcado. **[Ver el informe](https://www.virustotal.com/gui/file/413bbc4238e4b158a00563c00a311827dcc839d870e873479a05ac875f8770bc)**

## Instalación

Cierra el juego y después elige una opción:

**A) Instalador (recomendado).** Descarga **CabinPlay-Setup-x.y.z.exe** y ejecútalo. Encuentra el juego por sí mismo y no pide permisos de administrador. Si Windows dice que protegió tu PC, pulsa *Más información* y luego *Ejecutar de todas formas* (el instalador aún no está firmado).

**B) A mano.** Descarga en su lugar **CabinPlay-x.y.z-manual-install.zip**, descomprímelo y copia tres cosas de él:

1. `cabinplay.scs` en `Documentos\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` y `plugin\cabinplay.ini` en la carpeta `bin\win_x64\plugins` del juego (crea `plugins` si no existe). Para encontrar la carpeta del juego: en Steam, clic derecho en el juego > *Administrar* > *Ver archivos locales*.
3. la carpeta `app` donde quieras; después inicia una vez `CabinPlay.exe` dentro de ella. A partir de ahí se abre solo con el juego.

**Después, en el juego:**

1. Inicia el juego y acepta el aviso de "funciones del SDK" que aparece una vez.
2. En el **Gestor de mods**, activa **CabinPlay Screen**.
3. En un taller, monta **CabinPlay Screen** en una ranura de accesorios del parabrisas. A la izquierda hay dos versiones: *Large* (11", en un brazo) y *Compact* (10", junto al cristal); en la ranura central está *Centre*, girada hacia el conductor (con una versión propia para cabinas con volante a la derecha).

Eso es todo. La pantalla se enciende con el contacto del camión. Para quitar CabinPlay, abre *Configuración* > *Aplicaciones* en Windows o ejecuta `Uninstall.exe` en `%LocalAppData%\Programs\CabinPlay`; si lo instalaste a mano, borra los archivos que copiaste.

## Uso

| Atajo | Qué hace |
|---|---|
| `Ctrl+Alt+C` | Usar la pantalla desde el juego con ratón y teclado; el juego se pausa mientras tanto (`Esc` para salir) |
| `Ctrl+Alt+N` | Mapa de navegación |
| `Ctrl+Alt+H` | Pantalla de inicio |
| `Ctrl+Alt+Espacio` | Reproducir / pausar |
| `Ctrl+Alt+→` `←` | Siguiente / anterior |
| `Ctrl+Alt+↑` `↓` | 10 segundos adelante / atrás |
| `Ctrl+Alt+F` | Vídeo a pantalla completa |
| `Ctrl+Alt+PgUp` `PgDn` | App abierta anterior / siguiente |
| `Ctrl+Alt+S` | Dos apps en paralelo, sí / no |
| `Ctrl+Alt+X` | Intercambiar los dos lados |

La reproducción se pausa cuando pausas el juego o quitas el contacto. El idioma está en **Ajustes**, en la pantalla. Si cerraste CabinPlay desde sus ajustes, `Ctrl+Alt+C` lo vuelve a iniciar.

La app **Camión** muestra los daños del camión y del remolque, la carga, el combustible y los indicadores. Los atajos Ctrl+Alt también funcionan con el juego en primer plano, y sus teclas nunca llegan al juego: un atajo no hace sonar la bocina ni acciona nada del camión.

La app **Retrovisores** muestra en la pantalla los retrovisores del camión: el izquierdo, su gran angular y, si lo tiene, el frontal. Toca uno para ampliarlo.

La app **Navegador** abre cualquier sitio web: escribe una dirección o una búsqueda en la barra superior y pulsa la estrella para guardar una página en la página de inicio. Las sesiones de los sitios se mantienen iniciadas, y `Ctrl+Alt+F` pone el vídeo de una página a pantalla completa. Para escribir hace falta el modo de control (`Ctrl+Alt+C`).

La vista **en paralelo** muestra dos apps a la vez, por ejemplo el mapa a la izquierda y la música a la derecha: pulsa el botón de división del dock y elige la app de cada mitad. En **Ajustes** puedes elegir un fondo, también entre tus propias imágenes, cambiar las teclas de los atajos y asignar cualquiera de ellos a un botón de tu volante o mando.

**Más sitios:** además del parabrisas, el taller tiene la pantalla para el *Dashboard* (salpicadero), el *Air vent* (rejilla de ventilación), *Overhead* (techo), el *Passenger seat* (asiento del acompañante), la *Console* (consola), el *Cup holder* (portavasos, camiones nuevos) y el *Passenger side* (lado del acompañante del parabrisas). Para ver desde la litera está la **CabinPlay TV** de 27": en la pared trasera (ranura del panel de luces), plana o abierta hacia la almohada, o en el parabrisas, plana o girada hacia la litera. En el Scania S, sigue girando la cabeza a la derecha después de mirar atrás: la cámara pasa a la litera y se detiene en la almohada, frente a la tele.

## Requisitos

- Euro Truck Simulator 2 **1.61**, Windows 10 u 11
- No requiere DLC
- Un jugador: no funciona con TruckersMP
- Compatible con 25 camiones; no con el Renault E-Tech T

## Ayuda y comunidad

Preguntas, problemas y novedades: **[únete a nuestro Discord](https://discord.gg/rhGEbsu3zy)**.

Para informar de un problema, abre **Ajustes** en la pantalla, pulsa **Crear informe de error** y publica el zip que guarda en el escritorio.

**¿El vídeo va a tirones o se congela?** El juego está usando toda tu tarjeta gráfica. En los ajustes gráficos del juego, baja el **Escalado** al 200 % o menos (el 400 % suele ser la causa).

## Contacto

**Colaboración y patrocinio:** escribe a **[contact@hydrabon.com](mailto:contact@hydrabon.com)** o contáctanos en [Discord](https://discord.gg/rhGEbsu3zy).

**Quejas y sugerencias:** escribe a la misma dirección o cuéntanoslo en [Discord](https://discord.gg/rhGEbsu3zy).

## Créditos y licencia

Hecho por **HydRaboN**, nuestro servidor de comunidad y equipo de software. CabinPlay es gratuito y de código abierto bajo la [licencia MIT](LICENSE). Incluye [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay es un proyecto de fans no oficial. No está afiliado a SCS Software, Google/YouTube, Twitch ni Apple, ni cuenta con su respaldo.
