# CabinPlay

[English](README.md) · [Türkçe](README.tr.md) · [Deutsch](README.de.md) · [Polski](README.pl.md) · **Français** · [Español](README.es.md) · [Русский](README.ru.md)

Un écran multimédia fonctionnel pour la cabine de votre camion dans **Euro Truck Simulator 2**. Regardez YouTube, écoutez de la musique et suivez la carte GPS du jeu sur une tablette fixée au pare-brise, tout en conduisant.

## Téléchargement

**[Téléchargez la dernière version](https://github.com/timurmert/ets2-cabinplay/releases/latest)** et prenez le fichier `CabinPlay-x.y.z.zip`.

## Installation

Fermez le jeu et décompressez le fichier. Choisissez ensuite une méthode :

**A) Automatique.** Double-cliquez sur **Install.cmd**. Si Windows indique qu'il a protégé votre ordinateur, cliquez sur *Informations complémentaires*, puis sur *Exécuter quand même* (les fichiers ne sont pas signés). `Install.cmd` lance uniquement `data\install.ps1`, un script en texte clair que vous pouvez lire.

**B) À la main.** Aucun script n'est exécuté ; vous copiez trois éléments du dossier `data` :

1. `cabinplay.scs` dans `Documents\Euro Truck Simulator 2\mod`
2. `plugin\cabinplay.dll` et `plugin\cabinplay.ini` dans le dossier `bin\win_x64\plugins` du jeu (créez `plugins` s'il n'existe pas). Pour trouver le dossier du jeu : dans Steam, clic droit sur le jeu > *Gérer* > *Parcourir les fichiers locaux*.
3. le dossier `app` où vous voulez, puis lancez une fois `CabinPlay.exe` qui s'y trouve. Ensuite, il démarre tout seul avec le jeu.

**Ensuite, dans le jeu :**

1. Lancez le jeu et acceptez l'avertissement sur les « fonctionnalités SDK », affiché une seule fois.
2. Dans le **Gestionnaire de mods**, activez **CabinPlay Screen**.
3. Dans un garage, installez **CabinPlay Screen** dans l'emplacement d'accessoire à gauche du pare-brise. Deux versions existent : *Large* (11", sur un bras) et *Compact* (10", contre la vitre).

C'est tout. L'écran s'allume avec le contact du camion. Pour le retirer, double-cliquez sur **Uninstall.cmd** ou supprimez les fichiers copiés à la main.

## Utilisation

| Raccourci | Action |
|---|---|
| `Ctrl+Alt+C` | Utiliser l'écran depuis le jeu avec la souris et le clavier (`Échap` pour quitter) |
| `Ctrl+Alt+N` | Carte de navigation |
| `Ctrl+Alt+H` | Écran d'accueil |
| `Ctrl+Alt+Espace` | Lecture / pause |
| `Ctrl+Alt+→` `←` | Suivant / précédent |
| `Ctrl+Alt+↑` `↓` | Avancer / reculer de 10 secondes |
| `Ctrl+Alt+F` | Vidéo en plein écran |

La lecture se met en pause quand vous mettez le jeu en pause ou coupez le contact. La langue se règle dans **Réglages**, à l'écran.

Si vous dirigez à la souris, mettez `control_mouse=0` dans `cabinplay.ini` (dans le dossier `bin\win_x64\plugins` du jeu) : la souris reste alors au camion et les flèches déplacent le curseur de l'écran.

## Prérequis

- Euro Truck Simulator 2 **1.61**, Windows 10 ou 11
- Le DLC **Cabin Accessories** (il fournit l'emplacement de fixation)
- Solo uniquement : ne fonctionne pas avec TruckersMP
- Compatible avec 23 camions ; pas avec les DAF XF Electric, Renault E-Tech T et Scania S 2024e

## Aide et communauté

Questions, problèmes et nouveautés : **[rejoignez notre Discord](https://discord.gg/rhGEbsu3zy)**.

Pour signaler un problème, ouvrez **Réglages** à l'écran, appuyez sur **Créer un rapport d'erreur** et publiez le fichier zip enregistré sur le bureau.

## Collaboration et sponsoring

Écrivez à **[contact@hydrabon.com](mailto:contact@hydrabon.com)** ou contactez-nous sur [Discord](https://discord.gg/rhGEbsu3zy).

## Crédits et licence

Réalisé par **HydRaboN**, notre serveur communautaire et équipe logicielle. CabinPlay est gratuit et open source sous [licence MIT](LICENSE). Il inclut [MinHook](https://github.com/TsudaKageyu/minhook) (BSD 2-Clause).

CabinPlay est un projet de fans non officiel. Il n'est ni affilié à SCS Software, Google/YouTube, Twitch ou Apple, ni approuvé par eux.
