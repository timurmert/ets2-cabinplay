// CabinPlay shell: dock, home screen, the map view and the truck page. Opened apps are
// separate browser views the host layers over everything right of the dock; this page
// only tells the host what to show.
(() => {
  const apps = window.CABINPLAY_APPS || [];
  const host = window.chrome && window.chrome.webview;
  const send = (msg) => host && host.postMessage(msg);
  const $ = (id) => document.getElementById(id);
  const params = new URLSearchParams(location.search);

  // ---- language: the saved choice, else the system language, else English
  const I18N = window.CABINPLAY_I18N || {};
  function pickLanguage() {
    let saved = null;
    try { saved = localStorage.getItem('cabinplay.language'); } catch (e) { /* storage unavailable */ }
    const wanted = params.get('lang') || saved || (navigator.language || 'en').slice(0, 2).toLowerCase();
    return I18N[wanted] ? wanted : 'en';
  }
  let language = pickLanguage();
  const t = (key) => (I18N[language] && I18N[language][key]) || I18N.en[key] || key;
  const appName = (app) => (app.nameKey ? t(app.nameKey) : app.name);

  // Eight-tooth gear outline around (50, 50).
  function gearPath(teeth, outer, inner) {
    const pts = [];
    const step = (Math.PI * 2) / teeth;
    for (let i = 0; i < teeth; i++) {
      const a = i * step;
      for (const [r, off] of [[inner, -0.30], [outer, -0.17], [outer, 0.17], [inner, 0.30]]) {
        pts.push(`${(50 + r * Math.cos(a + off * step * 1.55)).toFixed(2)} ${(50 + r * Math.sin(a + off * step * 1.55)).toFixed(2)}`);
      }
    }
    return `M${pts.join('L')}Z`;
  }
  const SETTINGS_ICON = `<svg viewBox="0 0 100 100"><defs><linearGradient id="g-set" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#b9bbc2"/><stop offset="1" stop-color="#6a6c74"/></linearGradient></defs>
    <rect width="100" height="100" fill="url(#g-set)"/>
    <path d="${gearPath(8, 31, 24)}" fill="#26272c" stroke="#26272c" stroke-width="3" stroke-linejoin="round"/>
    <circle cx="50" cy="50" r="15.5" fill="#8d8f97"/><circle cx="50" cy="50" r="8.5" fill="#26272c"/></svg>`;

  const PLAY = 'M7 4.5v15L20 12z';
  const PAUSE = 'M6 4.5h4.4v15H6zM13.6 4.5H18v15h-4.4z';

  // active: id of the web app in front, or null. view: what this page shows behind it.
  let state = { active: null, view: 'home', open: [], media: null, game: {}, browser: {}, vehicle: null };
  const usedViews = new Set(); // built-in views keep their dock shortcut once opened
  const VIEWS = ['home', 'maps', 'mirrors', 'truck', 'browser'];

  // url: a page to go to, even when the app is already open on another one.
  function open(app, url) {
    $('settings').hidden = true;
    address.blur();
    if (app.builtin || (app.browser && !url && !state.open.includes(app.id))) {
      showView(app.id); // the browser's start page until it has a page
    } else {
      send({ type: 'open', id: app.id, url: url || app.url || 'about:blank', navigate: !!url, bar: !!app.browser,
             next: app.next, prev: app.prev, fullscreenKey: app.fullscreenKey });
    }
  }

  function showView(view) {
    $('settings').hidden = true;
    // Applied at once so the screen does not wait for the host's next state message.
    state = { ...state, active: null, view };
    if (view === 'mirrors') layoutMirrors(); // before the view, so the plugin has the rectangles
    render();
    send({ type: 'view', view });
  }

  // ---- home grid
  const grid = $('grid');
  for (const app of apps) {
    const el = document.createElement('button');
    el.className = 'app';
    el.dataset.id = app.id;
    el.innerHTML = `<div class="app-icon">${app.icon}</div><div class="app-name"></div>`;
    el.addEventListener('click', () => open(app));
    grid.appendChild(el);
  }
  const settingsTile = document.createElement('button');
  settingsTile.className = 'app';
  settingsTile.innerHTML = `<div class="app-icon">${SETTINGS_ICON}</div><div class="app-name"></div>`;
  settingsTile.addEventListener('click', () => { $('settings').hidden = false; });
  grid.appendChild(settingsTile);

  $('settings-close').addEventListener('click', () => { $('settings').hidden = true; });
  $('settings-quit').addEventListener('click', () => send({ type: 'quit' }));
  $('settings-diagnostics').addEventListener('click', () => send({ type: 'diagnostics' }));
  const languageSelect = $('settings-language');
  for (const [code, texts] of Object.entries(I18N)) {
    languageSelect.add(new Option(texts.name, code));
  }
  languageSelect.addEventListener('change', () => {
    language = languageSelect.value;
    try { localStorage.setItem('cabinplay.language', language); } catch (e) { /* storage unavailable */ }
    applyLanguage();
  });
  $('dock-home').addEventListener('click', () => showView('home'));
  $('dock-fullscreen').addEventListener('click', () => send({ type: 'media', action: 'fullscreen' }));
  $('mirror-all').addEventListener('click', () => pickMirror(null));
  $('dock-back').addEventListener('click', () => send({ type: 'browser', action: 'back' }));
  $('dock-forward').addEventListener('click', () => send({ type: 'browser', action: 'forward' }));
  for (const btn of document.querySelectorAll('[data-media]')) {
    btn.addEventListener('click', () => send({ type: 'media', action: btn.dataset.media }));
  }
  $('now-playing').addEventListener('click', (e) => {
    // Tapping the card (not its buttons) jumps back into the app that is playing.
    if (e.target.closest('button') || !state.media) return;
    const app = apps.find((a) => a.id === state.media.app);
    if (app) open(app);
  });

  // ---- browser: address bar, start page, kept pages
  const address = $('bar-address');
  const browserApp = apps.find((a) => a.browser);
  const SITES = window.CABINPLAY_SITES || [];
  const MARK_COLOURS = ['#0a84ff', '#30b0c7', '#34c759', '#ff9f0a', '#ff375f', '#bf5af2', '#5e5ce6'];
  let favourites = [];
  try { favourites = JSON.parse(localStorage.getItem('cabinplay.favourites')) || []; } catch (e) { /* none kept */ }
  const isWeb = (url) => /^https?:\/\//i.test(url || '');
  const hostOf = (url) => { try { return new URL(url).hostname.replace(/^www\./, ''); } catch (e) { return url; } };

  // What was typed: an address if it looks like one, otherwise something to search for.
  function toUrl(text) {
    text = text.trim();
    if (!text) return null;
    if (isWeb(text)) return text;
    if (/^[^\s/:]+\.[^\s/.:]{2,}(:\d+)?(\/\S*)?$/.test(text)) return `https://${text}`;
    return `https://www.google.com/search?q=${encodeURIComponent(text)}`;
  }

  function saveFavourites() {
    try { localStorage.setItem('cabinplay.favourites', JSON.stringify(favourites)); } catch (e) { /* storage unavailable */ }
    layoutSites();
    render();
  }

  function layoutSites() {
    const list = $('browser-sites');
    list.replaceChildren();
    const shown = [...favourites.map((f) => ({ ...f, kept: true })), ...SITES.filter((s) => !s.lang || s.lang === language)];
    for (const site of shown) {
      const el = document.createElement('button');
      el.className = 'site';
      el.innerHTML = '<span class="site-mark"></span><span class="site-text"><b></b><small></small></span>';
      const host = hostOf(site.url);
      const mark = el.querySelector('.site-mark');
      mark.textContent = (site.name || host).charAt(0).toLocaleUpperCase();
      mark.style.background = MARK_COLOURS[[...host].reduce((n, c) => n + c.charCodeAt(0), 0) % MARK_COLOURS.length];
      el.querySelector('b').textContent = site.name || host;
      el.querySelector('small').textContent = host;
      el.addEventListener('click', () => open(browserApp, site.url));
      if (site.kept) {
        const remove = document.createElement('span');
        remove.className = 'site-remove';
        remove.textContent = '×';
        remove.title = t('favRemove');
        remove.addEventListener('click', (e) => {
          e.stopPropagation();
          favourites = favourites.filter((f) => f.url !== site.url);
          saveFavourites();
        });
        el.appendChild(remove);
      }
      list.appendChild(el);
    }
  }

  // While the address is being typed the host sends the game's keyboard here instead of
  // to the page under the bar.
  address.addEventListener('focus', () => { address.select(); send({ type: 'shell-keys', on: true }); });
  address.addEventListener('blur', () => send({ type: 'shell-keys', on: false }));
  address.addEventListener('keydown', (e) => {
    if (e.key !== 'Enter') return;
    const url = toUrl(address.value);
    if (url) open(browserApp, url);
  });
  $('bar-start').addEventListener('click', () => showView('browser'));
  $('bar-reload').addEventListener('click', () => send({ type: 'browser', action: 'reload' }));
  $('bar-star').addEventListener('click', () => {
    const { url, title } = state.browser || {};
    if (!isWeb(url)) return;
    favourites = favourites.some((f) => f.url === url)
      ? favourites.filter((f) => f.url !== url)
      : [{ url, name: (title || '').trim().slice(0, 60) || hostOf(url) }, ...favourites];
    saveFavourites();
  });

  function renderBrowser() {
    const page = state.active === browserApp.id ? state.browser || {} : {};
    const url = isWeb(page.url) ? page.url : '';
    $('bar-reload').disabled = !url;
    const star = $('bar-star');
    const kept = favourites.some((f) => f.url === url);
    star.disabled = !url;
    star.classList.toggle('on', kept);
    star.title = t(kept ? 'favRemove' : 'favAdd');
    // not while it is being typed in
    if (document.activeElement !== address && address.dataset.shown !== url) {
      address.value = address.dataset.shown = url;
    }
  }

  // ---- truck page: damage lists, one row per part
  const TRUCK_PARTS = [['cabin', 'partCabin'], ['chassis', 'partChassis'], ['engine', 'partEngine'],
    ['transmission', 'partTransmission'], ['wheels', 'partWheels']];
  const TRAILER_PARTS = [['body', 'partBody'], ['chassis', 'partChassis'], ['wheels', 'partWheels'], ['cargo', 'partCargo']];
  function damageRows(list, parts) {
    const rows = {};
    for (const [id, key] of parts) {
      const li = document.createElement('li');
      li.innerHTML = '<span class="name"></span><span class="bar"><i></i></span><span class="val"></span>';
      li.querySelector('.name').dataset.i18n = key;
      list.appendChild(li);
      rows[id] = li;
    }
    const note = document.createElement('li');
    note.className = 'note';
    note.hidden = true;
    list.appendChild(note);
    return { rows, note };
  }
  const truckRows = damageRows($('damage-truck'), TRUCK_PARTS);
  const trailerRows = damageRows($('damage-trailer'), TRAILER_PARTS);

  // ---- clock
  let fmtTime, fmtShort, fmtDay, fmtLong, fmtKm, fmtInt;
  function tick() {
    const now = new Date();
    const locale = t('locale');
    $('clock-time').textContent = fmtTime.format(now);
    $('clock-date').textContent = fmtShort.format(now);
    const day = fmtDay.format(now);
    $('day-name').textContent = day.charAt(0).toLocaleUpperCase(locale) + day.slice(1);
    $('day-date').textContent = fmtLong.format(now);
  }

  // Everything that depends on the language, redone whenever it changes.
  function applyLanguage() {
    const locale = t('locale');
    document.documentElement.lang = language;
    languageSelect.value = language;
    fmtTime = new Intl.DateTimeFormat(locale, { hour: '2-digit', minute: '2-digit', hour12: false });
    fmtShort = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' });
    fmtDay = new Intl.DateTimeFormat(locale, { weekday: 'long' });
    fmtLong = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long' });
    fmtKm = new Intl.NumberFormat(locale, { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    fmtInt = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });
    for (const el of document.querySelectorAll('[data-i18n]')) el.textContent = t(el.dataset.i18n);
    for (const el of document.querySelectorAll('[data-i18n-title]')) el.title = t(el.dataset.i18nTitle);
    address.placeholder = t('addressHint');
    layoutSites(); // the shortcuts depend on the language
    for (const tile of grid.querySelectorAll('.app[data-id]')) {
      tile.querySelector('.app-name').textContent = appName(apps.find((a) => a.id === tile.dataset.id));
    }
    settingsTile.querySelector('.app-name').textContent = t('settings');
    if (state.view === 'mirrors') layoutMirrors(); // the tiles' labels
    send({ type: 'language', language });
    tick();
    render();
  }

  const clock = (s) => {
    s = Math.max(0, Math.floor(s || 0));
    const h = Math.floor(s / 3600), m = Math.floor((s % 3600) / 60), sec = String(s % 60).padStart(2, '0');
    return h ? `${h}:${String(m).padStart(2, '0')}:${sec}` : `${m}:${sec}`;
  };
  const two = (n) => String(n).padStart(2, '0');

  // ---- map view: figures come from the game's telemetry, the map itself from the game
  function renderMaps(game) {
    const driving = game.connected && game.telemetry;
    $('map-speed-value').textContent = driving ? game.speed : 0;
    const limit = driving ? game.limit : 0;
    $('map-limit').hidden = !(limit > 0);
    $('map-limit').textContent = limit;
    $('map-speed').classList.toggle('over', limit > 0 && game.speed > limit + 3);

    const routed = driving && game.navDistance > 0;
    $('map-route').classList.toggle('idle', !routed);
    if (routed) {
      // The route's remaining time is in game time, like the in-game clock.
      const minutes = Math.round(game.navTime / 60);
      const eta = (game.gameTime + minutes) % 1440;
      $('map-eta').textContent = `${two(Math.floor(eta / 60))}:${two(eta % 60)}`;
      const h = Math.floor(minutes / 60);
      $('map-remaining').textContent = h ? `${h} ${t('hour')} ${minutes % 60} ${t('minute')}` : `${minutes} ${t('minute')}`;
      const km = game.navDistance / 1000;
      $('map-distance').textContent = `${km >= 10 ? Math.round(km) : fmtKm.format(km)} km`;
    } else {
      $('map-eta').textContent = '--:--';
      $('map-remaining').textContent = t('noRoute');
      $('map-distance').textContent = '';
    }

    let status = '';
    if (!game.connected) status = t('mapOffline');
    else if (!game.mounted) status = t('mapNotMounted');
    else if (!game.navReady) status = t('mapWaiting');
    $('map-status').textContent = status;
  }

  // ---- truck page: figures from the game's telemetry
  const dash = '–';
  // Conditions, as on a car listing's damage chart: intact, light, moderate, heavy.
  // Keep the colours in sync with .legend in style.css.
  const CONDITION = [[5, '#d5d8df'], [20, '#ffd60a'], [40, '#ff9f0a'], [Infinity, '#ff453a']];
  function wearColor(p) {
    if (p == null) return '';
    return CONDITION.find(([limit]) => p < limit)[1];
  }
  function setPart(part, percent) {
    for (const el of $('rig').querySelectorAll(`[data-part="${part}"]`)) {
      if (percent == null) el.style.removeProperty('--c');
      else el.style.setProperty('--c', wearColor(percent));
    }
  }
  function setRow(row, percent) {
    const bar = row.querySelector('.bar i');
    bar.style.background = wearColor(percent);
    // a sliver even at 0%, so the colour still reads
    bar.style.width = percent == null ? '0' : `${Math.max(4, Math.min(100, percent))}%`;
    row.querySelector('.val').textContent = percent == null ? dash : `${percent}%`;
  }
  const duration = (minutes) => {
    const h = Math.floor(minutes / 60);
    return h ? `${h} ${t('hour')} ${minutes % 60} ${t('minute')}` : `${minutes} ${t('minute')}`;
  };
  // The game names cities in their own language and script; places.js has them in ours.
  const PLACES = window.CABINPLAY_PLACES || { cities: {}, countries: {} };
  const inLanguage = (names, fallback) => (names && (names[language] || names.en)) || fallback;
  function cityLine(id, name) {
    const city = id && PLACES.cities[id.toLowerCase()];
    if (!city) return name || dash;
    const country = PLACES.countries[city[0]];
    const cityName = inLanguage(city[1], name);
    return country ? `${cityName}, ${inLanguage(country, '')}` : cityName;
  }

  // ---- mirror views: where the game's mirrors are in its UI texture (x, y, width, height,
  // top row first); keep in sync with MIRRORS in tools/build_mod.py. In the grid they sit
  // where they are in the texture's lower half, so the plugin shows that half 1:1.
  const DOCK = 88, AREA_W = 1024 - DOCK, AREA_H = 512, LOWER = 512;
  const CAMERAS = [
    { id: 'close', key: 'camLeft', src: [8, 520, 296, 496] },
    { id: 'front', key: 'camFront', src: [316, 520, 612, 306] },
    { id: 'close_s', key: 'camLeftWide', src: [316, 836, 220, 180] },
  ];
  let mirror = null; // the one enlarged, or null for all of them

  function pickMirror(id) {
    mirror = CAMERAS.find((c) => c.id === id) || null;
    layoutMirrors();
  }

  function layoutMirrors() {
    const tiles = $('mirror-tiles');
    tiles.replaceChildren();
    const place = (el, [x, y, w, h]) => Object.assign(el.style, { left: `${x}px`, top: `${y}px`, width: `${w}px`, height: `${h}px` });
    let source, target;
    if (!mirror) {
      for (const cam of CAMERAS) {
        const tile = document.createElement('div');
        tile.className = 'mirror-tile';
        tile.innerHTML = '<span></span>';
        tile.querySelector('span').textContent = t(cam.key);
        place(tile, [cam.src[0], cam.src[1] - LOWER, cam.src[2], cam.src[3]]);
        tile.addEventListener('click', () => pickMirror(cam.id));
        tiles.appendChild(tile);
      }
      source = [0, LOWER, AREA_W, AREA_H];
      target = [DOCK, 0, AREA_W, AREA_H];
    } else {
      // as large as fits below the bar, same shape as in the game
      const [, , w, h] = mirror.src;
      const box = [16, 58, AREA_W - 32, AREA_H - 74];
      const scale = Math.min(box[2] / w, box[3] / h);
      const fit = [Math.round(box[0] + (box[2] - w * scale) / 2), Math.round(box[1] + (box[3] - h * scale) / 2),
                   Math.round(w * scale), Math.round(h * scale)];
      const tile = document.createElement('div');
      tile.className = 'mirror-tile';
      place(tile, fit);
      tile.addEventListener('click', () => pickMirror(null));
      tiles.appendChild(tile);
      $('mirror-name').textContent = t(mirror.key);
      source = mirror.src;
      target = [DOCK + fit[0], fit[1], fit[2], fit[3]];
    }
    $('mirrors').classList.toggle('grid', !mirror);
    $('mirrors').classList.toggle('single', !!mirror);
    $('mirror-info').hidden = !!mirror;
    $('mirror-bar').hidden = !mirror;
    $('mirror-all').hidden = !mirror;
    send({ type: 'nav-rects', source, target });
  }

  function renderMirrors(game) {
    let status = '';
    if (!game.connected) status = t('mapOffline');
    else if (!game.mounted) status = t('mapNotMounted');
    else if (!game.navReady) status = t('mapWaiting');
    $('mirror-status').textContent = status;
  }

  // Ctrl+Alt+PgUp / PgDn: the previous or next app, in the dock's order.
  function cycle(step) {
    const shown = dockApps();
    if (!shown.length) return;
    const index = shown.findIndex((a) => a.id === frontId());
    // From the home screen, Next starts at the first app and Previous at the last.
    const next = index < 0 ? (step > 0 ? 0 : shown.length - 1) : (index + step + shown.length) % shown.length;
    open(shown[next]);
  }

  function renderTruck(game, v) {
    const live = !!(game.connected && v);
    $('truck').classList.toggle('offline', !live);
    $('rig-title').textContent = live ? [v.brand, v.name].filter(Boolean).join(' ') || t('truck') : t('truck');
    $('rig-sub').textContent = live
      ? [v.plate, v.odometer ? `${fmtInt.format(v.odometer)} km` : ''].filter(Boolean).join(' · ')
      : t('truckOffline');
    const chips = $('rig-chips');
    chips.replaceChildren();
    if (live) {
      const chip = (text, cls) => {
        const el = document.createElement('span');
        el.className = `chip ${cls}`;
        el.textContent = text;
        chips.appendChild(el);
      };
      chip(t(v.engine ? 'engineOn' : 'engineOff'), v.engine ? 'on' : '');
      if (v.parkingBrake) chip(t('parkingBrake'), 'warn');
    }

    // the truck and what each part has taken
    const wear = live ? v.wear : {};
    for (const [id] of TRUCK_PARTS) {
      setPart(`truck-${id}`, live ? wear[id] : null);
      setRow(truckRows.rows[id], live ? wear[id] : null);
    }
    const trailer = live ? v.trailer : null;
    const coupled = !!(trailer && trailer.coupled);
    $('rig-trailer').classList.toggle('absent', !trailer);
    $('rig-trailer').classList.toggle('detached', !!trailer && !coupled);
    // the cargo box inside the trailer only while something is loaded
    const loaded = !!(trailer && v.job && v.job.loaded);
    $('rig').querySelector('.cargo').toggleAttribute('hidden', !loaded);
    for (const [id] of TRAILER_PARTS) {
      const percent = trailer ? trailer.wear[id] : null;
      setPart(`trailer-${id}`, percent);
      const row = trailerRows.rows[id];
      // cargo damage only means something while cargo is on board
      row.hidden = !trailer || (id === 'cargo' && !(v.job && v.job.loaded));
      setRow(row, percent);
    }
    const note = trailerRows.note;
    note.hidden = !!trailer && coupled;
    note.textContent = t(!live ? 'truckWaiting' : !trailer ? 'noTrailer' : 'trailerDetached');

    // job
    const job = live ? v.job : null;
    $('job-card').classList.toggle('idle', !job);
    if (job) {
      $('job-cargo').textContent = job.cargo || dash;
      const sub = [];
      if (job.mass) sub.push(`${fmtKm.format(job.mass / 1000)} t`);
      if (job.special) sub.push(t('special'));
      if (!job.loaded) sub.push(t('notLoaded'));
      $('job-sub').textContent = sub.join(' · ');
      $('job-from').textContent = cityLine(job.fromId, job.from);
      $('job-from-company').textContent = job.fromCompany || '';
      $('job-to').textContent = cityLine(job.toId, job.to);
      $('job-to-company').textContent = job.toCompany || '';
      // delivery time and the clock are both game time, in minutes
      const left = job.delivery && game.gameTime ? job.delivery - game.gameTime : null;
      $('job-left').textContent = left == null ? dash : left < 0 ? t('late') : duration(left);
      $('job-left').classList.toggle('late', left != null && left < 0);
      $('job-distance').textContent = job.distance ? `${fmtInt.format(job.distance)} km` : dash;
      $('job-pay').textContent = job.income
        ? new Intl.NumberFormat(t('locale'), { style: 'currency', currency: v.currency || 'EUR', maximumFractionDigits: 0 }).format(job.income)
        : dash;
    } else {
      $('job-cargo').textContent = live ? t('noJob') : dash;
      $('job-sub').textContent = '';
    }

    // fuel
    const share = live && v.fuelCapacity ? Math.max(0, Math.min(1, v.fuel / v.fuelCapacity)) : 0;
    $('fuel-amount').textContent = !live ? dash
      : v.fuelCapacity ? `${fmtInt.format(v.fuel)} / ${fmtInt.format(v.fuelCapacity)} L` : `${fmtInt.format(v.fuel)} L`;
    const level = $('fuel-level');
    level.style.width = `${share * 100}%`;
    level.classList.toggle('low', share < 0.25 && share >= 0.1);
    level.classList.toggle('empty', share < 0.1);
    $('fuel-range').textContent = live && v.range ? `${fmtInt.format(v.range)} km` : dash;
    $('fuel-average').textContent = live && v.average ? `${fmtKm.format(v.average)} L/100` : dash;
    $('fuel-adblue').textContent = live && v.adblueCapacity ? `${fmtInt.format(v.adblue)} L` : dash;

    // gauges
    $('gauge-oil').textContent = live && v.oilTemperature ? `${fmtInt.format(v.oilTemperature)} °C` : dash;
    $('gauge-water').textContent = live && v.waterTemperature ? `${fmtInt.format(v.waterTemperature)} °C` : dash;
    $('gauge-battery').textContent = live && v.battery ? `${fmtKm.format(v.battery)} V` : dash;
  }

  const frontId = () => state.active || (state.view !== 'home' ? state.view : null);
  const dockApps = () => apps.filter((a) => (a.builtin ? usedViews.has(a.id)
    : state.open.includes(a.id) || (a.browser && usedViews.has(a.id))));

  function render() {
    const media = state.media;
    const game = state.game || {};
    const playing = !!(media && media.playing);
    if (state.view !== 'home') usedViews.add(state.view);

    $('home').hidden = state.view !== 'home';
    $('maps').hidden = state.view !== 'maps';
    $('mirrors').hidden = state.view !== 'mirrors';
    $('truck').hidden = state.view !== 'truck';
    $('browser').hidden = state.view !== 'browser';
    if (state.view === 'browser') renderBrowser();
    if (state.view === 'maps') renderMaps(game);
    if (state.view === 'mirrors') {
      if (!$('mirror-tiles').children.length) layoutMirrors(); // the host put us here
      renderMirrors(game);
    }
    if (state.view === 'truck') renderTruck(game, state.vehicle);

    // dock: one shortcut per open app
    const shown = dockApps();
    const front = frontId();
    const dock = $('dock-apps');
    dock.classList.toggle('many', shown.length === 4);
    dock.classList.toggle('lots', shown.length === 5);
    dock.classList.toggle('crowded', shown.length >= 6);
    dock.replaceChildren();
    for (const app of shown) {
      const el = document.createElement('button');
      el.className = 'dock-app' + (app.id === front ? ' active' : '') + (playing && media.app === app.id ? ' playing' : '');
      el.title = appName(app);
      el.innerHTML = app.icon;
      el.addEventListener('click', () => open(app));
      dock.appendChild(el);
    }
    const activeApp = apps.find((a) => a.id === state.active);
    $('dock-fullscreen').hidden = !(activeApp && activeApp.fullscreenKey);
    // back and forward, like in any browser, for the site in front
    const browser = state.browser || {};
    $('dock-nav').hidden = !activeApp;
    $('dock-back').disabled = !browser.back;
    $('dock-forward').disabled = !browser.forward;
    for (const tile of grid.querySelectorAll('.app[data-id]')) {
      tile.classList.toggle('open', state.open.includes(tile.dataset.id));
    }

    // link to the game
    const held = game.telemetry && ((game.paused && !game.control) || !game.electric);
    $('link').className = !game.connected ? '' : held ? 'held' : 'on';
    $('link-text').textContent = t(!game.connected ? 'linkNone'
      : !game.telemetry ? 'linkGame'
      : !game.electric ? 'linkIgnition'
      : game.control ? 'linkControl'
      : game.paused ? 'linkPaused' : 'linkGame');

    // now playing
    const card = $('now-playing');
    const has = !!(media && media.hasMedia);
    card.classList.toggle('idle', !has);
    const app = media && apps.find((a) => a.id === media.app);
    if (has) {
      $('np-title').textContent = media.title.replace(/ - YouTube( Music)?$/, '').replace(/^\(\d+\)\s*/, '') || appName(app);
      $('np-sub').textContent = media.artist ? `${media.artist} · ${appName(app)}` : appName(app);
      $('np-bar').style.width = media.duration ? `${Math.min(100, (media.position / media.duration) * 100)}%` : '0';
      $('np-pos').textContent = clock(media.position);
      $('np-dur').textContent = media.duration ? clock(media.duration) : t('live');
    } else {
      $('np-title').textContent = app ? appName(app) : t('nothingPlaying');
      $('np-sub').textContent = app ? t('pickSomething') : t('openAnApp');
      $('np-bar').style.width = '0';
      $('np-pos').textContent = $('np-dur').textContent = '0:00';
    }
    const art = $('np-art');
    art.classList.toggle('has-art', !!(has && media.art));
    art.style.backgroundImage = has && media.art ? `url("${media.art.replace(/"/g, '%22')}")` : '';
    $('np-play-icon').setAttribute('d', playing ? PAUSE : PLAY);
  }

  if (host) {
    host.addEventListener('message', (e) => {
      if (e.data && e.data.type === 'cycle') cycle(e.data.step);
      if (e.data && e.data.type === 'blur') address.blur();
      if (e.data && e.data.type === 'state') {
        state = {
          active: e.data.active || null,
          view: VIEWS.includes(e.data.view) ? e.data.view : 'home',
          open: e.data.open || [],
          media: e.data.media || null,
          game: e.data.game || {},
          browser: e.data.browser || {},
          vehicle: e.data.vehicle || null,
        };
        render();
      }
    });
  }
  applyLanguage();
  setInterval(tick, 1000);

  // "?open=<id>[&url=<address>]" starts straight in an app, optionally on a given page
  // (the host passes its --open and --url arguments through).
  const startApp = apps.find((a) => a.id === params.get('open'));
  if (startApp) open(startApp, params.get('url') || undefined);
})();
