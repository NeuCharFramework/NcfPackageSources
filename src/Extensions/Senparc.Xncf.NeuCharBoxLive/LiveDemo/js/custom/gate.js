// The invite-code gate for the custom scene, before any session exists. It follows createLive.open()'s order (health,
// the saved code, then the access card) but makes no session: it resolves true once a code works, false on Back or
// cancel() (the page's teardown). offline: true (no API base) shows the offline card without fetching anything.
// base (the API base) is named on the offline card when no NCF backend answers there.
import { LIVE_COPY, normalizeCode, whyOff } from '../engine/live.js';
import { LiveError } from '../engine/liveapi.js';
import { CUSTOM_COPY } from './copy.js';

const finite = (n) => typeof n === 'number' && Number.isFinite(n);
const status = (e) => (e instanceof LiveError ? e.status : 0);
const kindOf = (e) => (e instanceof LiveError ? e.kind : null);

// api.check reduced to { left } or { err: 'bad'|'rate'|'offline' }.
async function checkCode(api, code) {
  try {
    const r = await api.check(code);
    return finite(r?.remainingToday) ? { left: r.remainingToday } : { err: 'offline' };
  } catch (e) { return { err: status(e) === 401 ? 'bad' : status(e) === 429 && kindOf(e) === 'rate' ? 'rate' : 'offline' }; }
}

export function passGate({ api, codes, view, onBack, offline = false, base = '' }) {
  let cancel = () => {};
  const gate = new Promise((resolve) => {
    let finished = false, handle = null;
    const finish = (value) => {
      if (finished) return;
      finished = true;
      const h = handle; handle = null; h?.close?.();
      resolve(value);
    };
    cancel = () => finish(false);
    const back = () => { if (finished) return; try { onBack?.(); } finally { finish(false); } };
    const show = (spec) => {
      if (finished) return;
      const old = handle; handle = null; old?.close?.();
      handle = view.access({ ...spec, backLabel: CUSTOM_COPY.back, offlineText: spec.offlineText ?? CUSTOM_COPY.offline }, { onCode, onApply, onBack: back }) ?? null;
    };
    async function onCode(raw) {
      if (finished) return null;
      const code = normalizeCode(raw), r = await checkCode(api, code);
      if (finished) return null;
      if (r.err) return r.err === 'bad' ? LIVE_COPY.badCode : r.err === 'rate' ? LIVE_COPY.rate : CUSTOM_COPY.offline;
      if (r.left <= 0) return LIVE_COPY.noneLeft;
      codes.set(code);
      finish(true);
      return null;
    }
    async function onApply(form) {
      if (finished) return null;
      try { await api.apply({ ...(form && typeof form === 'object' ? form : {}), scene: 'custom' }); } catch (e) {
        return status(e) === 429 ? LIVE_COPY.applyRate : CUSTOM_COPY.offline;
      }
      return LIVE_COPY.applied;
    }
    (async () => {
      if (offline) return show({ mode: 'offline' });
      const h = await api.health();
      if (finished) return;
      if (!(h.ok && h.liveEnabled && h.scenes.includes('custom'))) return show({ mode: 'offline', offlineText: whyOff(h, 'custom', base) });
      const stored = codes.get();
      if (!stored) return show({ mode: 'code' });
      const r = await checkCode(api, stored);
      if (finished) return;
      if (r.err === 'bad') { codes.clear(); return show({ mode: 'code', note: LIVE_COPY.savedCodeGone }); }
      if (r.err) return show({ mode: 'offline' });
      if (r.left <= 0) return show({ mode: 'code', note: LIVE_COPY.noneLeft });
      finish(true);
    })();
  });
  gate.cancel = () => cancel();
  return gate;
}
