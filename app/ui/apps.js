// Apps shown on the home screen. Add an entry to put another site on the screen.
//   next / prev      CSS selectors of the site's own skip buttons (used by the media keys)
//   fullscreenKey    key the site's player uses to toggle fullscreen (Ctrl+Alt+F sends it)
//   builtin          drawn by the shell itself instead of loading a site
window.CARPLAY_APPS = [
  {
    id: 'maps',
    name: 'Haritalar',
    builtin: true,
    icon: `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-mp" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#d9f2d0"/><stop offset="1" stop-color="#a8dfa2"/></linearGradient></defs>
      <rect width="100" height="100" fill="url(#g-mp)"/>
      <path d="M-5 70 L40 42 L62 58 L105 26" fill="none" stroke="#fff" stroke-width="15"/>
      <path d="M-5 70 L40 42 L62 58 L105 26" fill="none" stroke="#f7c948" stroke-width="9"/>
      <path d="M22 -5 L40 42 L30 105" fill="none" stroke="#fff" stroke-width="11"/>
      <rect x="58" y="0" width="42" height="18" fill="#8ecbf5"/>
      <circle cx="62" cy="58" r="13" fill="#fff"/><path d="M62 49 L69.5 65 L62 61.5 L54.5 65 Z" fill="#0a84ff"/></svg>`,
  },
  {
    id: 'youtube',
    name: 'YouTube',
    url: 'https://www.youtube.com/',
    next: '.ytp-next-button',
    prev: '.ytp-prev-button',
    fullscreenKey: 'f',
    icon: `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-yt" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#ff4d4d"/><stop offset="1" stop-color="#e60023"/></linearGradient></defs>
      <rect width="100" height="100" fill="#fff"/>
      <rect x="14" y="27" width="72" height="46" rx="14" fill="url(#g-yt)"/>
      <path d="M43 38.5v23L63 50z" fill="#fff"/></svg>`,
  },
  {
    id: 'ytmusic',
    name: 'YouTube Music',
    url: 'https://music.youtube.com/',
    next: '.next-button',
    prev: '.previous-button',
    icon: `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-ym" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#ff3d3d"/><stop offset="1" stop-color="#c4001d"/></linearGradient></defs>
      <rect width="100" height="100" fill="url(#g-ym)"/>
      <circle cx="50" cy="50" r="27" fill="none" stroke="#fff" stroke-width="4.5"/>
      <path d="M43 37.5v25L64 50z" fill="#fff"/></svg>`,
  },
  {
    id: 'twitch',
    name: 'Twitch',
    url: 'https://www.twitch.tv/',
    fullscreenKey: 'f',
    icon: `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-tw" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#a970ff"/><stop offset="1" stop-color="#772ce8"/></linearGradient></defs>
      <rect width="100" height="100" fill="url(#g-tw)"/>
      <path d="M30 24h44v30L60 68H49l-9 9v-9H28V32z" fill="#fff"/>
      <path d="M36 30v32h10v8l8-8h10l8-8V30z" fill="#772ce8"/>
      <rect x="48" y="38" width="5" height="13" fill="#fff"/><rect x="60" y="38" width="5" height="13" fill="#fff"/></svg>`,
  },
];
