// Where the truck is in the real world, and the weather there, for the home screen's card.
//
// The game's map is the real one through a Lambert conformal conic projection (standard
// parallels 37 and 65, origin 50 N 15 E, sphere of 6371 km), drawn 1:19.08 on the mainland and
// 1:14.14 in the UK. The scales and offsets were fitted to 108 cities whose game coordinates
// are known: they land 10 km from the real place on average on the mainland, 6 km in the UK
// (see tools/build_cities.py). Weather is Open-Meteo's forecast for that place at the hour
// the game's clock shows: free, no account, no key.
window.CabinPlayWeather = (() => {
  const R = 6371000;
  const rad = (d) => (d * Math.PI) / 180;
  const deg = (r) => (r * 180) / Math.PI;
  const t = (f) => Math.tan(Math.PI / 4 + f / 2);
  const f1 = rad(37), f2 = rad(65), f0 = rad(50), l0 = rad(15);
  const n = Math.log(Math.cos(f1) / Math.cos(f2)) / Math.log(t(f2) / t(f1));
  const F = (Math.cos(f1) * t(f1) ** n) / n;
  const rho0 = (R * F) / t(f0) ** n;
  // game units per metre, and where the projection's origin lies in game coordinates
  const MAINLAND = { s: 0.05241322, x: 16379.6, z: 4341.5 };
  const UK = { s: 0.07074801, x: 31933.2, z: 7482.4 };
  // The UK is drawn west and north of Calais (-30363, -4986); Dover is at (-33322, -7884).
  const inUk = (x, z) => x < -31000 && z < -5500;

  /** The real latitude and longitude of a point in the game's world (x east, z south). */
  function toWorld(x, z) {
    const p = inUk(x, z) ? UK : MAINLAND;
    const E = (x - p.x) / p.s, N = -(z - p.z) / p.s;
    const rho = Math.sign(n) * Math.hypot(E, rho0 - N);
    const theta = Math.atan2(E, rho0 - N);
    return [deg(2 * Math.atan(((R * F) / rho) ** (1 / n)) - Math.PI / 2), deg(l0 + theta / n)];
  }

  function km(a, b) {
    const p1 = rad(a[0]), p2 = rad(b[0]);
    const h = Math.sin((p2 - p1) / 2) ** 2 + Math.cos(p1) * Math.cos(p2) * Math.sin(rad(b[1] - a[1]) / 2) ** 2;
    return 2 * 6371 * Math.asin(Math.sqrt(h));
  }

  /** The game's city nearest to a point in the game's world: { id, at: [lat, lon], km }. */
  function nearestCity(x, z) {
    const here = toWorld(x, z);
    let best = null;
    for (const [id, at] of Object.entries(window.CABINPLAY_CITIES || {})) {
      const d = km(here, at);
      if (!best || d < best.km) best = { id, at, km: d };
    }
    return best && { ...best, here };
  }

  // WMO weather codes in nine kinds, each with its picture by day and by night.
  const KINDS = [
    [[0], 'clear', '☀️', '🌙'], [[1], 'mostlyClear', '🌤️', '🌙'], [[2], 'partly', '⛅', '☁️'],
    [[3], 'cloudy', '☁️', '☁️'], [[45, 48], 'fog', '🌫️', '🌫️'], [[51, 53, 55, 56, 57], 'drizzle', '🌦️', '🌧️'],
    [[61, 63, 65, 66, 67, 80, 81, 82], 'rain', '🌧️', '🌧️'], [[71, 73, 75, 77, 85, 86], 'snow', '🌨️', '🌨️'],
    [[95, 96, 99], 'thunder', '⛈️', '⛈️'],
  ];
  const kindOf = (code) => KINDS.find(([codes]) => codes.includes(code)) || KINDS[3];

  const cache = new Map(); // "lat,lon" -> { at: ms, data } or { at, pending }
  const MAX_AGE = 30 * 60 * 1000;
  const RETRY = 5 * 60 * 1000;

  /**
   * The weather at a place at a given hour of its day (0-23), or null until it is known. The
   * forecast is fetched once per place and kept for half an hour; onReady is called when one
   * arrives.
   */
  function at(place, hour, onReady) {
    const key = place.map((v) => v.toFixed(2)).join(',');
    const now = Date.now();
    const kept = cache.get(key);
    if (!kept || (!kept.pending && now - kept.at > (kept.data ? MAX_AGE : RETRY))) {
      cache.set(key, { at: now, pending: true, data: kept && kept.data });
      const q = new URLSearchParams({ latitude: place[0].toFixed(3), longitude: place[1].toFixed(3),
        hourly: 'temperature_2m,weather_code,is_day', timezone: 'auto', forecast_days: '1' });
      fetch(`https://api.open-meteo.com/v1/forecast?${q}`)
        .then((r) => (r.ok ? r.json() : Promise.reject(new Error(`HTTP ${r.status}`))))
        .then((data) => { cache.set(key, { at: Date.now(), data }); onReady(); })
        .catch(() => cache.set(key, { at: Date.now(), data: kept && kept.data }));
    }
    const data = cache.get(key).data;
    const h = data && data.hourly;
    if (!h || !h.time || !h.time.length) return null;
    const i = Math.max(0, Math.min(h.time.length - 1, hour));
    const [, kind, dayIcon, nightIcon] = kindOf(h.weather_code[i]);
    return { temp: Math.round(h.temperature_2m[i]), kind, icon: h.is_day[i] ? dayIcon : nightIcon };
  }

  return { toWorld, nearestCity, at };
})();
