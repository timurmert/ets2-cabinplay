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
  const VIEWS = ['home', 'maps', 'truck'];

  function open(app) {
    $('settings').hidden = true;
    if (app.builtin) {
      showView(app.id);
    } else {
      send({ type: 'open', id: app.id, url: app.url, next: app.next, prev: app.prev, fullscreenKey: app.fullscreenKey });
    }
  }

  function showView(view) {
    $('settings').hidden = true;
    // Applied at once so the screen does not wait for the host's next state message.
    state = { ...state, active: null, view };
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
  $('dock-back').addEventListener('click', () => send({ type: 'browser', action: 'back' }));
  $('dock-forward').addEventListener('click', () => send({ type: 'browser', action: 'forward' }));
  for (const btn of document.querySelectorAll('#actions-card [data-action]')) {
    btn.addEventListener('click', () => send({ type: 'game-action', action: btn.dataset.action }));
  }
  for (const btn of document.querySelectorAll('[data-media]')) {
    btn.addEventListener('click', () => send({ type: 'media', action: btn.dataset.media }));
  }
  $('now-playing').addEventListener('click', (e) => {
    // Tapping the card (not its buttons) jumps back into the app that is playing.
    if (e.target.closest('button') || !state.media) return;
    const app = apps.find((a) => a.id === state.media.app);
    if (app) open(app);
  });

  // ---- truck page: damage lists, one row per part
  const TRUCK_PARTS = [['cabin', 'partCabin'], ['chassis', 'partChassis'], ['engine', 'partEngine'],
    ['transmission', 'partTransmission'], ['wheels', 'partWheels']];
  const TRAILER_PARTS = [['body', 'partBody'], ['chassis', 'partChassis'], ['wheels', 'partWheels'], ['cargo', 'partCargo']];
  function damageRows(list, parts) {
    const rows = {};
    for (const [id, key] of parts) {
      const li = document.createElement('li');
      li.innerHTML = '<span class="dot"></span><span class="name"></span><span class="val"></span>';
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
    for (const tile of grid.querySelectorAll('.app[data-id]')) {
      tile.querySelector('.app-name').textContent = appName(apps.find((a) => a.id === tile.dataset.id));
    }
    settingsTile.querySelector('.app-name').textContent = t('settings');
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
  // Green while as good as new, then yellow, orange and red as the part wears.
  function wearColor(p) {
    if (p == null) return '';
    return p < 8 ? '#30d158' : p < 25 ? '#ffd60a' : p < 50 ? '#ff9f0a' : '#ff453a';
  }
  function setPart(part, percent) {
    for (const el of $('rig').querySelectorAll(`[data-part="${part}"]`)) el.style.fill = wearColor(percent);
  }
  function setRow(row, percent) {
    row.querySelector('.dot').style.background = wearColor(percent);
    row.querySelector('.val').textContent = percent == null ? dash : `${percent}%`;
  }
  const duration = (minutes) => {
    const h = Math.floor(minutes / 60);
    return h ? `${h} ${t('hour')} ${minutes % 60} ${t('minute')}` : `${minutes} ${t('minute')}`;
  };
  const place = (city, company) => [city, company].filter(Boolean).join(' · ') || dash;

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
    for (const [id] of TRAILER_PARTS) {
      const percent = trailer ? trailer.wear[id] : null;
      if (id !== 'cargo') setPart(`trailer-${id}`, percent);
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
      $('job-from').textContent = place(job.from, job.fromCompany);
      $('job-to').textContent = place(job.to, job.toCompany);
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

    // the game's own functions need the game
    for (const btn of document.querySelectorAll('#actions-card [data-action]')) {
      btn.disabled = !(game.connected && game.telemetry);
    }
  }

  function render() {
    const media = state.media;
    const game = state.game || {};
    const playing = !!(media && media.playing);
    if (state.view !== 'home') usedViews.add(state.view);

    $('home').hidden = state.view !== 'home';
    $('maps').hidden = state.view !== 'maps';
    $('truck').hidden = state.view !== 'truck';
    if (state.view === 'maps') renderMaps(game);
    if (state.view === 'truck') renderTruck(game, state.vehicle);

    // dock: one shortcut per open app
    const shown = apps.filter((a) => (a.builtin ? usedViews.has(a.id) : state.open.includes(a.id)));
    const front = state.active || (state.view !== 'home' ? state.view : null);
    const dock = $('dock-apps');
    dock.classList.toggle('many', shown.length === 4);
    dock.classList.toggle('lots', shown.length >= 5);
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
  if (startApp) open({ ...startApp, url: params.get('url') || startApp.url });
})();
