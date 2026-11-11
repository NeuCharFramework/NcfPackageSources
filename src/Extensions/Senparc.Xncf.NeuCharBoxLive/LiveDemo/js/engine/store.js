// Single source of truth for a scene. Only the step player writes; room, panel
// and chat read. `tween` animates a numeric path over time (instant in headless).

const easeInOut = (k) => (k < 0.5 ? 2 * k * k : 1 - Math.pow(-2 * k + 2, 2) / 2);
const clone = (v) => JSON.parse(JSON.stringify(v));

export function createStore(initial, { headless = false } = {}) {
  const state = clone(initial);
  const subs = new Set();
  const tweens = new Map();

  const get = (path) => path.split('.').reduce((o, k) => (o == null ? undefined : o[k]), state);
  const set = (path, value) => {
    const ks = path.split('.');
    let o = state;
    for (let i = 0; i < ks.length - 1; i++) o = o[ks[i]] ?? (o[ks[i]] = {});
    o[ks[ks.length - 1]] = value;
    for (const fn of subs) fn(state, path, value);
  };
  const subscribe = (fn) => { subs.add(fn); return () => subs.delete(fn); };

  function tween(path, to, ms, ease = easeInOut) {
    if (headless || ms <= 0) { // a jump replaces a running tween on the path, as a longer tween does (else it overwrites the jump)
      const t = tweens.get(path); if (t) { tweens.delete(path); t.res(); }
      set(path, to); return Promise.resolve();
    }
    const from = Number(get(path)) || 0;
    if (tweens.has(path)) tweens.get(path).res();
    return new Promise((res) => tweens.set(path, { from, to, ms, start: performance.now(), ease, res }));
  }
  function tick(now) {
    for (const [path, t] of tweens) {
      const k = Math.min(1, (now - t.start) / t.ms);
      if (k >= 1) { tweens.delete(path); try { set(path, t.to); } finally { t.res(); } }
      else set(path, t.from + (t.to - t.from) * t.ease(k));
    }
  }
  function finishTweens() { const all = [...tweens]; tweens.clear(); for (const [p, t] of all) { try { set(p, t.to); } finally { t.res(); } } }

  // Deep copy of the whole state, and a way to put it back (used to give every prompt the post-setup room).
  const snapshot = () => clone(state);
  function restore(snap) {
    finishTweens();
    for (const k of Object.keys(state)) if (!(k in snap)) delete state[k];
    for (const [k, v] of Object.entries(snap)) set(k, clone(v));
  }

  return { state, get, set, subscribe, tween, tick, finishTweens, snapshot, restore };
}
