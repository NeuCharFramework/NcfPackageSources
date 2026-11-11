import { createStore } from './engine/store.js';
import { createChat } from './engine/chat.js';
import { createPanel } from './engine/panel.js';
import { interpret, asksWhatIfFails } from './engine/match.js';
import { icon } from './engine/icons.js';
import { createFlow, BEATS, quoted, WHATIF_HOW } from './engine/flow.js';
import { SCENES, CUSTOM } from './scenes/index.js';
import { SOCIAL, socialLinks } from './engine/social.js';
import { createLive, LIVE_COPY } from './engine/live.js';
import { createLiveView } from './engine/livechat.js';
import { createClockCtl } from './engine/clockctl.js';
import { createAutomations, foldMedia } from './engine/automations.js';
import { createLiveEntry } from './engine/liveentry.js';
import { liveApiBase } from './live-config.js';

// Picker thumbnails: the hub and a few of the devices each room connects (line icons, same as the dashboard tiles).
const THUMB_ICONS = { home: ['lamp', 'blinds', 'moisture', 'camera', 'thermostat'], lab: ['laser', 'mirror', 'stage', 'meter', 'beamcam'], elder: ['bed', 'door', 'motion', 'kettle', 'bulb'], warehouse: ['conveyor', 'scanner', 'gate', 'cart', 'dock'], creator: ['keylight', 'camera', 'mic', 'capture', 'sign'], custom: ['lamp', 'camera', 'thermostat', 'conveyor', 'mic'] };
const app = document.getElementById('app');
let current = null; // { scene, teardown }
let mountGen = 0;   // bumped on every route; an in-flight mount that sees a newer value abandons itself

function esc(s) { return String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c])); }
const coarse = typeof matchMedia === 'function' && matchMedia('(pointer: coarse)').matches;
const follow = () => `<nav class="social" aria-label="Follow NeuCharBox"><span class="social-label">Follow</span>${socialLinks()}</nav>`;
// A thumbnail gradient is dark when its first colour is (lab, creator): the index number and icons then go light.
const darkThumb = (g) => { const m = /#([0-9a-f]{6})/i.exec(g || ''); if (!m) return false; const n = parseInt(m[1], 16); return 0.299 * (n >> 16) + 0.587 * ((n >> 8) & 255) + 0.114 * (n & 255) < 110; };

// "Build your own" needs the live AI: without an API base (js/live-config.js emptied) its card isn't shown and #custom
// opens the picker.
const customOn = () => !!liveApiBase();

function renderPicker() {
  app.classList.remove('failed', 'live-edit'); app.classList.add('picker-mode');
  app.innerHTML = `
    <header class="top"><div class="logo"></div><div class="brand">NeuCharBox</div><div class="scene-name">Live demo</div><div class="spacer"></div>${follow()}</header>
    <main class="picker">
      <h1>Try NeuCharBox before it ships.</h1>
      <p class="lede">Pick a place. Plug in the hub, connect what's there, say what you want — or give exact instructions — approve the plan, and watch it run. Then make something fail, and see what it does.</p>
      <div class="scene-grid">${(customOn() ? [...SCENES, CUSTOM] : SCENES).map((s, i) => `
        <a class="scene-card" href="#${s.id}" style="--thumb:${s.thumb}"><div class="thumb${darkThumb(s.thumb) ? ' dark' : ''}" data-n="0${i + 1}"><span class="icons" aria-hidden="true">${['hub', ...(THUMB_ICONS[s.id] || [])].map(icon).join('')}</span></div><div class="body"><h3>${esc(s.title)}</h3><p>${esc(s.promise)}</p>${s.tag ? `<span class="tag">${esc(s.tag)}</span>` : ''}</div></a>`).join('')}
      </div>
      <p class="foot">Everything here is a simulation running in your browser. No account, no data leaves the page. Scenes marked illustrative are typical setups, not a specific customer's.</p>
    </main>`;
}

async function mountScene(meta, gen) {
  app.classList.remove('picker-mode', 'failed', 'live-edit');
  app.innerHTML = `
    <header class="top"><a class="logo" href="#" aria-label="All scenes"></a><a class="brand" href="#" style="text-decoration:none">NeuCharBox</a><div class="scene-name">${esc(meta.title)}</div><div class="spacer"></div><button class="btn small primary live-top" id="liveTop" type="button" title="${esc(LIVE_COPY.headerButtonTitle)}" hidden><span class="spark" aria-hidden="true">✨ </span>${esc(LIVE_COPY.headerButton)}</button><a class="btn small secondary" href="#"><span class="back" aria-hidden="true">‹ </span>Scenes</a>${follow()}</header>
    <nav class="beats" id="beats" aria-label="Demo steps">${BEATS.map(([id, label]) => `<button data-beat="${id}" disabled${id === 'recover' ? ' hidden' : ''}>${label}</button>`).join('')}</nav>
    <div class="stage">
      <div class="room"><canvas id="room"></canvas><div class="hint">${coarse ? 'drag · pinch · tap a device' : 'drag to orbit · scroll to zoom · right-drag to pan · tap a device'}</div><button class="reset" id="resetView" type="button" title="Reset view">⟲ view</button></div>
      <div class="status" id="status"><span class="stext" id="statusText" role="status">Loading the ${esc(meta.title.toLowerCase())}…</span><span class="clockctl" id="clockctl" hidden></span></div>
      <div class="side">
        <div class="panel" id="panel"></div>
        <div class="autos" id="autos" hidden></div>
        <div class="chat" id="chat"></div>
      </div>
    </div>`;

  const [{ default: scene }, { createRenderer, THREE }, partsMod] = await Promise.all([meta.load(), import('./engine/renderer.js'), import('./engine/parts.js')]);
  if (gen !== mountGen) return; // the visitor navigated away while this scene was loading

  const initial = { env: { hour: scene.startHour ?? 17 }, hub: { status: 'off', led: 0 }, plan: {} };
  for (const [id, d] of Object.entries(scene.devices)) initial[id] = { status: 'offline', ...d.initial };
  const store = createStore(initial);
  const qp = new URLSearchParams(location.search).get('q'); // ?q=high|low locks the render tier (QA screenshots); no watchdog
  const R = createRenderer(document.getElementById('room'), { camera: scene.camera, ...(qp === 'high' || qp === 'low' ? { quality: qp, lockQuality: true } : {}) });
  let flow = null, live = null, autos = null;
  try {
  const M = partsMod.materials(); const P = partsMod.parts(R.scene, M);
  const sv = Number(new URLSearchParams(location.search).get('speed')); const speed = Number.isFinite(sv) && sv > 0 ? Math.min(sv, 6) : 1; // ?speed= review mode (≤ 6), also for room-side motion
  // A what-if replays the request quietly up to its failure point: no chat, status, pings or room marks while it does.
  // Scenes read the flag through build({ quiet }) (their store hooks return early while it is true, A2).
  const room = scene.build({ R, P, M, THREE, store, parts: partsMod, speed, quiet: () => !!flow?.quiet() });
  R.onFrame((t) => { store.tick(performance.now()); room.update(store.state, t); });
  R.start();

  const statusEl = document.getElementById('status'), statusText = document.getElementById('statusText');
  document.getElementById('resetView').onclick = () => R.resetView();
  // Tap names: devices, the hub, and anything else the scene lets you tap (scene.extraNames, e.g. { webcam: 'USB webcam' }).
  const nameOf = (id) => (scene.devices[id] ? scene.devices[id].name : id === 'hub' ? 'NeuCharBox' : scene.extraNames?.[id] || null);
  const pingDevice = (id) => { const n = nameOf(id); if (!n) return; room.focus?.(id); R.ping(id, n, { hold: 1200 }); };
  const panel = createPanel(document.getElementById('panel'), scene, store, { onSelect: pingDevice });
  R.onPick((id) => { panel.select(scene.devices[id] ? id : null); pingDevice(id); }); // the hub or a spare part (no tile) clears the last tile's highlight
  const beatBtns = [...document.getElementById('beats').querySelectorAll('button')];
  // Why a typed request didn't run, in NCB's words. `r` comes from interpret().
  const CLARIFY = {
    refuse: () => "That reads like something you don't want done. I won't guess at the opposite, and this demo can only run the requests below. Pick one, or say it another way.",
    opposite: (r) => `That's the opposite of ${quoted(r.prompt)}. This demo can only run the requests below, and I won't guess. Pick one, or say it another way.`,
    halt: () => "That asks me to stop or switch something off. None of the requests in this demo does just that, and I won't guess. Pick one below, or say it another way.",
    tie: (r) => `That could be ${quoted(r.prompt)} or ${quoted(r.also)}. I won't guess. Pick one below.`,
    values: (r) => `This demo can only run that one as written: ${quoted(r.prompt)}. I won't swap in other numbers on my own. Pick it below if that's what you want.`,
    partial: (r) => `The closest request in this demo is ${quoted(r.prompt)}. It doesn't do just what you asked, and I won't guess. Pick it below if that's what you want, or say it another way.`,
    // Quotes inside the plan line become ‘single’ ones. A line from the prompt's `touches` (what it does that its plan card
    // doesn't list, js/engine/match.js) is not on the card the visitor would see, so it isn't called part of the plan.
    conflict: (r) => `The closest request in this demo is ${quoted(r.prompt)}, but ${(r.prompt.touches || []).includes(r.line) ? 'running it means' : 'its plan includes'} "${String(r.line).replace(/[.!?]+$/, '').replace(/"([^"]*)"/g, '‘$1’')}", which goes against what you asked. I won't run it on a guess. Pick it below if that's fine, or say it another way.`,
    question: (r) => `That's a question, not a request, so I won't act on it. The closest request in this demo is ${quoted(r.prompt)}. Pick it below if that's what you want.`,
    check: (r) => `That asks me to check something, and none of the requests in this demo just checks it. I won't turn it into an action. The closest is ${quoted(r.prompt)}. Pick it below if that's what you want.`,
    statement: (r) => `That tells me how things are; it doesn't ask for anything, so I won't act on it. The closest request in this demo is ${quoted(r.prompt)}. Pick it below if that's what you want.`,
    vague: () => "I'm not sure which request that is, and I won't guess. This demo runs a few scripted requests in this room: pick one below, or say it another way.",
  };
  const chat = createChat(document.getElementById('chat'), { onPromptText: (text) => {
    if (live?.active) { live.submit(text); return; }
    if (flow.phase !== 'chips') return;
    const r = interpret(text, scene.prompts);
    // "What if the hall light stops working?": a failure is picked from a request's end card, so say how, with the
    // closest request first, rather than run that request clean.
    if (asksWhatIfFails(text, scene.devices)) {
      const near = r.action === 'run' || (r.action === 'clarify' && r.via && r.via !== 'halt') ? [r.prompt] : [];
      chat.closeChips(); chat.user(text); chat.ncb(WHATIF_HOW);
      chat.chips([...near, ...flow.remaining().filter((p) => !near.includes(p))], (p) => flow.runPrompt(p, p.chip, false));
      return;
    }
    if (r.action === 'run') { flow.runPrompt(r.prompt, text, false); return; }
    chat.closeChips();
    chat.user(text);
    // The scene's own answer comes first: text asking for what is already so, or about a device's state (scene.already).
    let said = null; try { said = scene.already?.(r, text, store.state) || null; } catch (e) { console.error(e); }
    const kind = r.action === 'refuse' ? (r.via === 'opposite' ? 'opposite' : 'refuse') : r.via;
    chat.ncb(said || (CLARIFY[kind] || CLARIFY.vague)(r));
    // The requests NCB just named come first, even if they already ran once.
    const named = (r.action === 'clarify' && r.via && r.via !== 'halt') || kind === 'opposite' ? [r.prompt, r.also].filter(Boolean) : [];
    chat.chips([...named, ...flow.remaining().filter((p) => !named.includes(p))], (p) => flow.runPrompt(p, p.chip, false));
  } });

  // The status dot turns red while the line reports a fault; the next status write clears it.
  // The clock control already shows the time: a status line that is only the clock's label is dropped while it shows.
  let clockOn = false, clockText = null;
  const setStatus = (text, bad) => { if (clockOn && clockText && text === clockText) return; statusText.textContent = text; statusEl.classList.toggle('bad', !!bad); };
  const setBeats = ({ current, recoverHidden, skippable }) => {
    const idx = BEATS.findIndex((b) => b[0] === current);
    beatBtns.forEach((b, i) => {
      if (b.dataset.beat === 'recover') b.hidden = recoverHidden;
      b.classList.toggle('done', i < idx); b.classList.toggle('now', i === idx);
      if (i === idx) b.setAttribute('aria-current', 'step'); else b.removeAttribute('aria-current');
      b.disabled = !skippable.includes(b.dataset.beat);
    });
  };
  // The request flow (js/engine/flow.js): phases, the beat bar's state, end cards, what-ifs. The page side of it here.
  flow = createFlow({ scene, store, chat, speed, ui: {
    alive: () => gen === mountGen,
    status: setStatus,
    beats: setBeats,
    // The guided end cards have the live button wherever live mode is offered (an API base). It opens live mode
    // as the header's Live AI button does (js/engine/liveentry.js: flow.stop() first, so a guided picker of failures left
    // open goes too: a pick from it would run a scripted what-if while live is on).
    endExtras: () => (live ? { onLive: () => entry?.open(), liveLabel: LIVE_COPY.button } : {}),
    // Discovery and fault effects: the pulse on a faulted device is red, like its ring, and its fault label goes as soon
    // as it is back online (a visitor confirming a stopped cart).
    ping: (id, text, opts) => R.ping(id, text, opts),
    unping: (id, kind) => R.unping?.(id, kind),
    focus: (id, hex) => room.focus?.(id, hex),
    pop: (id) => panel.pop(id),
    track: (on) => panel.track(on),
    rewind: () => panel.rewind(),
    show: (ids) => panel.show(ids),
    resetRoom: () => { room.reset?.(); R.clearMarkers(); }, // markers (rings, fault labels) are not state: drop them too
    clearMarkers: () => R.clearMarkers(),
  } });
  const base = liveApiBase();
  // The clock control beside the status line: shown in live mode, usable between requests (live.advance).
  const clockCtl = createClockCtl(document.getElementById('clockctl'), { onAdvance: (h) => live?.advance(h) });
  // The Automations section under the device cards (live mode only; js/engine/automations.js) and the header's Live AI
  // button (shown in every scene while live mode is not open). Neither exists without an API base.
  autos = base ? createAutomations(document.getElementById('autos'), { onToggle: (id, on) => live?.setAutomation(id, on), ...foldMedia(),
    emptyText: scene.devices.soil && scene.devices.pump ? LIVE_COPY.autosEmpty : LIVE_COPY.autosEmptyAny }) : null;
  const liveTop = document.getElementById('liveTop');
  let entry = null;
  live = base ? createLive({ scene, store, panel, base, view: createLiveView({ chat, scene }), ui: {
    alive: () => gen === mountGen, status: setStatus, beats: setBeats, track: (on) => panel.track(on), rewind: () => panel.rewind(),
    resetRoom: () => { room.reset?.(); R.clearMarkers(); }, baseline: () => flow.baseline,
    editing: (on) => { clockOn = !!on; app.classList.toggle('live-edit', !!on); panel.editable(on, { onEdit: (p, v) => live.edit(p, v) }); clockCtl.active(on); },
    clock: (c) => { clockText = c?.label || null; clockCtl.render(c); },
    automations: (list) => autos?.render(list),
    automationFlash: (id, kind) => autos?.flash(id, kind),
    attention: () => autos?.attention(),
    availability: () => entry?.sync(),
    offerGuided: (h) => { chat.closeEnds(); chat.closeFailures(); if (flow.baseline) { store.restore(flow.baseline); room.reset?.(); R.clearMarkers(); } flow.offerChips(h); },
  } }) : null;
  // The button sits before "‹ Scenes", so hiding it under a double-click moves nothing under the pointer. Pressed from the
  // keyboard, focus goes to the conversation, where live mode's card comes, not to the page top.
  const chatLog = document.getElementById('chatLog');
  entry = createLiveEntry({ live, flow, alive: () => gen === mountGen, show: (on) => {
    if (!on && document.activeElement === liveTop) chatLog?.focus({ preventScroll: true });
    liveTop.hidden = !on;
  } });
  liveTop.onclick = () => entry.open();
  entry.sync();
  beatBtns.forEach((b) => (b.onclick = () => flow.skipTo(b.dataset.beat)));

  current = { scene, teardown() { live?.close(); flow?.cancel(); autos?.dispose(); R.dispose(); if (window.__ncb?.scene === scene) window.__ncb = null; } };
  // QA hooks (visual harness): the host's phase, the quiet-replay flag and the recorded choices.
  window.__ncb = { live, store, panel, autos, entry, player: flow.player, scene, R, flow, host: { get phase() { return flow.phase; }, get quiet() { return flow.quiet(); }, get running() { return flow.running; }, lastChoices: flow.lastChoices } };
  await flow.start();
  } catch (e) {
    if (gen === mountGen) current = null;
    live?.close(); flow?.cancel(); autos?.dispose(); R.dispose();
    const top = document.getElementById('liveTop'); if (top) top.hidden = true; // live is closed: the button would do nothing
    throw e;
  }
}

// The scene could not start. Most often the browser can't run it (no WebGL 2, or too old for the 3D code); say so in
// plain words where the chat would be, with a way on. The raw error stays in the console.
function showLoadFailure(e) {
  // The page's own module never arrived (the custom page renders its markup itself): a bare page to say so in.
  if (!document.getElementById('chat')) {
    app.classList.remove('picker-mode');
    app.innerHTML = `<header class="top"><a class="logo" href="#" aria-label="All scenes"></a><a class="brand" href="#" style="text-decoration:none">NeuCharBox</a><div class="spacer"></div><a class="btn small secondary" href="#"><span class="back" aria-hidden="true">‹ </span>Scenes</a>${follow()}</header><div class="stage"><div class="status" id="status" role="status"></div><div class="side"><div class="chat" id="chat"></div></div></div>`;
  }
  const chatEl = document.getElementById('chat'), st = document.getElementById('status');
  let gl = false; try { gl = !!document.createElement('canvas').getContext('webgl2'); } catch {}
  const msg = String(e?.message || '');
  // A browser that has WebGL 2 and import maps can run the demo: a module that failed to download is the network.
  const modern = gl && typeof HTMLScriptElement !== 'undefined' && !!HTMLScriptElement.supports?.('importmap');
  const kind = modern && /failed to fetch|error loading dynamically imported|importing a module script failed|networkerror/i.test(msg) ? 'network'
    : !gl || /webgl|module specifier|bare specifier|does not resolve|dynamically imported|importing a module|import map/i.test(msg) || e instanceof SyntaxError ? 'browser' : 'bug';
  const T = {
    network: ['The scene didn\'t finish loading', 'The 3D room didn\'t finish loading.', 'Part of the demo didn\'t download, probably a network hiccup. Check your connection and try again.'],
    browser: ['3D view unavailable in this browser', 'Your browser can\'t show the 3D room.', 'This demo needs 3D graphics (WebGL 2) and a recent browser: Safari 16.4 or later, or a current Chrome, Edge or Firefox. If you\'re on one of those, 3D may be switched off or blocked on this device.'],
    bug: ['This scene could not load', 'This scene hit a bug and couldn\'t start.', 'That\'s a problem in this demo, not your device. Try again, or pick another scene.'],
  }[kind];
  const stt = document.getElementById('statusText'); // keeps the status row's other children (the clock control)
  if (stt) stt.textContent = T[0]; else if (st) st.textContent = T[0];
  st?.classList.add('bad');
  app.classList.add('failed'); // no empty room, dashboard or step bar: just the explanation
  if (!chatEl) return;
  chatEl.innerHTML = `<div class="chat-log"><div class="card end fail" role="alert"><h3>${T[1]}</h3>
    <p>${T[2]}</p>
    <div class="row"><button class="btn retry" type="button">Try again</button>${kind === 'browser' ? '' : '<a class="btn" href="#">Other scenes</a>'}<a class="btn primary" href="${SOCIAL[0].url}" target="_blank" rel="noopener">Watch NeuCharBox on YouTube</a></div></div></div>`;
  chatEl.querySelector('.retry').onclick = () => location.reload();
}

function route() {
  const gen = ++mountGen;
  current?.teardown?.(); current = null;
  const id = location.hash.replace('#', '');
  const meta = SCENES.find((s) => s.id === id);
  const failed = (e) => { console.error(e); if (gen === mountGen) showLoadFailure(e); };
  if (meta) mountScene(meta, gen).catch(failed);
  else if (id === CUSTOM.id && customOn()) CUSTOM.load().then((m) => m.mountCustom({ app, gen, isCurrent: () => gen === mountGen, setCurrent: (c) => { current = c; } })).catch(failed);
  else {
    if (id === CUSTOM.id) history.replaceState(null, '', location.pathname + location.search); // a shared #custom link lands on the picker
    renderPicker();
  }
  window.scrollTo(0, 0);
}
addEventListener('hashchange', route);
route();
