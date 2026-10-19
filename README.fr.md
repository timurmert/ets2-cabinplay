# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · [Polski](README.pl.md) · **Français** · [Español](README.es.md) · [Русский](README.ru.md)

Un écran multimédia fonctionnel pour la cabine de votre camion dans **Euro Truck Simulator 2**. Regardez YouTube, écoutez de la musique et suivez la carte GPS du jeu sur une tablette fixée au pare-brise, tout en conduisant.

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/releases/latest"><img src="https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Ftimurmert%2Fets2-cabinplay%2Fbadges%2Fdownloads.json&style=for-the-badge&logo=github&logoColor=white&labelColor=24292f&color=2ea44f&label=T%C3%89L%C3%89CHARGEMENTS" alt="Téléchargements"></a>
  <a href="https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56"><img src="https://img.shields.io/static/v1?style=for-the-badge&logo=virustotal&logoColor=white&labelColor=394EFF&color=2ea44f&label=VirusTotal&message=0/68%20SAIN" alt="VirusTotal: 0/68 sain"></a>
</p>

<p align="center">
  <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4"><img src="docs/media/preview.webp" width="100%" alt="CabinPlay"></a><br>
  <sub>▶ <a href="https://github.com/timurmert/ets2-cabinplay/blob/main/docs/media/cabinplay-demo.mp4">Voir la vidéo complète avec le son</a></sub>
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
  <a href="https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56"><img src="docs/media/virustotal.png" width="100%" alt="VirusTotal: 0/68 sain"></a><br>
  <sub>🛡️ Aucun des 68 antivirus de VirusTotal n'a signalé CabinPlay. <a href="https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56">Voir le rapport</a></sub>
</p>

## Téléchargement

**[Téléchargez la dernière version](https://github.com/timurmert/ets2-cabinplay/releases/latest)** et prenez le fichier `CabinPlay-Setup-x.y.z.exe`.

> 🛡️ **Analyse antivirus :** le programme d'installation a été analysé sur VirusTotal par 68 antivirus, et aucun ne l'a signalé. **[Voir le rapport](https://www.virustotal.com/gui/file/468fd2ee5d0a35a9c45087a34f3be5ed2bea95f12796afad03c9cc5c95758d56)**

## Installation

Fermez le jeu, puis choisissez une méthode :

**A) Programme d'installation (recommandé).** Téléchargez **CabinPlay-Setup-x.y.z.exe** et lancez-le. Il trouve le jeu tout seul et ne demande pas de droits d'administrateur. Si Windows indique qu'il a protégé votre ordinateur, cliquez sur *Informations complémentaires*, puis sur *Exécuter quand même* (le programme n'est pas signé).

**B) À la main.** Téléchargez plutôt **CabinPlay-x.y.z-manual-install.zip**, décompressez-le et copiez-en trois éléments :

1. `cabinplay.scs` dans `Documents\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` et `plugin\cabinplay.ini` dans le dossier `bin\win_x64\plugins` du jeu (créez `plugins` s'il n'existe pas). Pour trouver le dossier du jeu : dans Steam, clic droit sur le jeu > *Gérer* > *Parcourir les fichiers locaux*.
3. le dossier `app` où vous voulez, puis lancez une fois `CabinPlay.exe` qui s'y trouve. Ensuite, il démarre tout seul avec le jeu.

**Ensuite, dans le jeu :**

1. Lancez le jeu et acceptez l'avertissement sur les « fonctionnalités SDK », affiché une seule fois.
2. Dans le **Gestionnaire de mods**, activez **CabinPlay Screen**.
3. Dans un garage, installez **CabinPlay Screen** dans un emplacement d'accessoire du pare-brise. À gauche, trois versions existent : *Large* (13") et *Medium* (11") sur un bras, et *Compact* (10", contre la vitre) ; au milieu, il y a *Centre*, tourné vers le conducteur (avec une version à part pour les cabines à conduite à droite).

C'est tout. L'écran s'allume avec le contact du camion. Installé avec le programme d'installation, CabinPlay se met à jour tout seul : il télécharge la nouvelle version pendant que vous jouez et l'installe quand vous fermez le jeu et l'indique sur l'écran ; dans *Réglages* > *Mises à jour*, vous pouvez lui demander de vous consulter d'abord, ou le désactiver. Pour désinstaller CabinPlay, ouvrez *Paramètres* > *Applications* dans Windows, ou lancez `Uninstall.exe` dans `%LocalAppData%\Programs\CabinPlay` ; si vous l'avez installé à la main, supprimez les fichiers copiés.

## Utilisation

| Raccourci | Action |
|---|---|
| `Ctrl+Alt+C` | Utiliser l'écran depuis le jeu avec la souris et le clavier ; le jeu est en pause pendant ce temps (`Échap` pour quitter) |
| `Ctrl+Alt+N` | Carte de navigation |
| `Ctrl+Alt+H` | Écran d'accueil |
| `Ctrl+Alt+Espace` | Lecture / pause |
| `Ctrl+Alt+→` `←` | Suivant / précédent |
| `Ctrl+Alt+↑` `↓` | Avancer / reculer de 10 secondes |
| `Ctrl+Alt+F` | Vidéo en plein écran |
| `Ctrl+Alt+PgUp` `PgDn` | App ouverte précédente / suivante |
| `Ctrl+Alt+S` | Deux apps côte à côte, activer / désactiver |
| `Ctrl+Alt+X` | Échanger les deux côtés |

La lecture se met en pause quand vous mettez le jeu en pause ou coupez le contact. La langue se règle dans **Réglages**, à l'écran. Si vous avez fermé CabinPlay depuis ses réglages, `Ctrl+Alt+C` le relance.

L'app **Camion** affiche les dégâts du camion et de la remorque, la cargaison, le carburant et les indicateurs. Les raccourcis Ctrl+Alt fonctionnent aussi quand le jeu est au premier plan, et leurs touches n'atteignent jamais le jeu : un raccourci ne déclenche ni le klaxon ni rien d'autre sur le camion.

L'app **Rétroviseurs** affiche les rétroviseurs du camion à l'écran : le rétro gauche, son grand angle et, s'il y en a un, le rétro avant. Touchez-en un pour l'agrandir.

L'app **Navigateur** ouvre n'importe quel site : saisissez une adresse ou une recherche dans la barre du haut, et touchez l'étoile pour garder une page sur la page de démarrage. Vous restez connecté aux sites, et `Ctrl+Alt+F` passe la vidéo d'une page en plein écran. La saisie nécessite le mode contrôle (`Ctrl+Alt+C`).

La vue **côte à côte** affiche deux apps en même temps, par exemple la carte à gauche et la musique ou le navigateur à droite : touchez le bouton de partage dans le dock et choisissez l'app de chaque moitié. Dans **Réglages**, vous pouvez choisir un arrière-plan, y compris parmi vos propres images, changer les touches des raccourcis et placer chacun d'eux sur un bouton de votre volant ou de votre manette.

**Autres emplacements :** en plus du pare-brise, le garage propose l'écran pour le *Dashboard* (tableau de bord), l'*Air vent* (aérateur), *Overhead* (plafond), le *Passenger seat* (siège passager), la *Console* (console centrale), le *Cup holder* (porte-gobelet, camions récents) et le *Passenger side* (côté passager du pare-brise). Pour regarder depuis la couchette, il y a la **CabinPlay TV** de 27" : sur la paroi arrière (emplacement du panneau lumineux), à plat ou sortie vers l'oreiller, ou sur le pare-brise, à plat ou tournée vers la couchette. Dans le Scania S, continuez à tourner la tête vers la droite au-delà de la vue arrière : la caméra passe sur la couchette et s'arrête à l'oreiller, face à la télé.

## Prérequis

- Euro Truck Simulator 2 **1.61**, Windows 10 ou 11
- Aucun DLC requis
- Solo uniquement : ne fonctionne pas avec TruckersMP
- Compatible avec 25 camions ; pas avec le Renault E-Tech T

## Aide et communauté

Questions, problèmes et nouveautés : **[rejoignez notre Discord](https://discord.gg/hydrabon)**.

Pour signaler un problème, ouvrez **Réglages** à l'écran, appuyez sur **Créer un rapport d'erreur** et publiez le fichier zip enregistré sur le bureau.

**La vidéo saccade ou se fige ?** Le jeu utilise toute votre carte graphique. Dans les paramètres graphiques du jeu, baissez la **Mise à l'échelle** à 200 % ou moins (400 % en est la cause habituelle).

## Contact

**Collaboration et sponsoring :** écrivez à **[contact@hydrabon.com](mailto:contact@hydrabon.com)** ou contactez-nous sur [Discord](https://discord.gg/hydrabon).

**Réclamations et suggestions :** écrivez à la même adresse ou dites-le-nous sur [Discord](https://discord.gg/hydrabon).

## Crédits et licence

Réalisé par **HydRaboN**, notre serveur communautaire et équipe logicielle. CabinPlay est gratuit et open source sous [licence GNU GPL v3.0 ou ultérieure](LICENSE) : vous pouvez le partager et le modifier, mais tout ce qui en est tiré doit rester open source sous la même licence et citer CabinPlay. Selon la section 7 de la licence, le nom *CabinPlay* et son logo ne sont pas concédés ; une version modifiée doit porter un autre nom. Il inclut [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay est un projet de fans non officiel. Il n'est ni affilié à SCS Software, Google/YouTube, Twitch ou Apple, ni approuvé par eux.
