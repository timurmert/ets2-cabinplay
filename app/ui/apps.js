// Apps shown on the home screen. Add an entry to put another site on the screen.
//   name / nameKey   label under the tile; nameKey is looked up in i18n.js instead
//   next / prev      CSS selectors of the site's own skip buttons (used by the media keys)
//   fullscreenKey    key the site's player uses to toggle fullscreen (Ctrl+Alt+F sends it)
//                    ('video' instead makes whatever video is playing fullscreen)
//   builtin          drawn by the shell itself instead of loading a site
//   browser          the web browser: any address, under the shell's address bar
window.CABINPLAY_APPS = [
  {
    id: 'maps',
    nameKey: 'maps',
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
    id: 'mirrors',
    nameKey: 'mirrors',
    builtin: true,
    icon: `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-mi" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#5ac8fa"/><stop offset="1" stop-color="#0a6fd6"/></linearGradient>
        <linearGradient id="g-mg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#e9f4ff"/><stop offset="1" stop-color="#9fc4ec"/></linearGradient></defs>
      <rect width="100" height="100" fill="url(#g-mi)"/>
      <path d="M58 30h8v9h-8z" fill="#1d2a3c"/>
      <rect x="26" y="20" width="34" height="58" rx="9" fill="#1d2a3c"/>
      <rect x="30" y="24" width="26" height="50" rx="6" fill="url(#g-mg)"/>
      <path d="M33 66l20-34M33 54l14-24" stroke="#fff" stroke-width="3.5" stroke-linecap="round" opacity=".8"/></svg>`,
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
  {
    id: 'browser',
    nameKey: 'browser',
    browser: true,
    fullscreenKey: 'video',
    icon: `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-br" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#3fd0ff"/><stop offset="1" stop-color="#0a6fe8"/></linearGradient></defs>
      <rect width="100" height="100" fill="#fff"/>
      <circle cx="50" cy="50" r="35" fill="url(#g-br)"/>
      <g fill="none" stroke="#fff" stroke-width="3.5" opacity=".9"><ellipse cx="50" cy="50" rx="15" ry="35"/>
      <path d="M15 50h70M20.5 32h59M20.5 68h59"/></g></svg>`,
  },
  {
    id: 'truck',
    nameKey: 'truck',
    builtin: true,
    icon: `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-tk" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#ffb340"/><stop offset="1" stop-color="#ff7a00"/></linearGradient></defs>
      <rect width="100" height="100" fill="url(#g-tk)"/>
      <rect x="9" y="29" width="52" height="31" rx="3.5" fill="#fff"/>
      <path d="M64 61V41q0-5 5-5h11q4 0 6 4l5 10v11z" fill="#fff"/>
      <path d="M74 40h6q2 0 3 2l3.5 7H74z" fill="#ff8a00"/>
      <rect x="9" y="61" width="82" height="5" rx="2" fill="#fff"/>
      <g fill="#fff" stroke="#ff7a00" stroke-width="3"><circle cx="22" cy="70" r="7"/><circle cx="39" cy="70" r="7"/>
      <circle cx="78" cy="70" r="7"/></g></svg>`,
  },
];

// Shortcuts on the browser's start page, next to the pages the user keeps with the star.
//   lang   only shown in that screen language
window.CABINPLAY_SITES = [
  { name: 'TRT 1 Canlı', url: 'https://www.tabii.com/tr/watch/live/trt1?trackId=150002', lang: 'tr' },
  { name: 'tabii', url: 'https://www.tabii.com/tr', lang: 'tr' },
  { name: 'TRT Spor', url: 'https://www.trtspor.com.tr/canli-yayin-izle/trt-spor', lang: 'tr' },
  { name: 'beIN CONNECT', url: 'https://www.beinconnect.com.tr/', lang: 'tr' },
  { name: 'TOD', url: 'https://www.todtv.com.tr/', lang: 'tr' },
  { name: 'S Sport Plus', url: 'https://www.ssportplus.com/', lang: 'tr' },
  { name: 'Exxen', url: 'https://www.exxen.com/', lang: 'tr' },
  { name: 'Google', url: 'https://www.google.com/' },
];
