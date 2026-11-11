// Run mode's glue between the page and the engine (no renderer or DOM imports at the top, so Node can test it):
//   liveUiFor(...) → the `ui` object createLive drives (status line, panel, room, the card when live mode ends);
//   createSessionWiring(...) → what every new live session resets: the panel, the store and the baseline.
//   dropGone(store, layout) → a removed device's state goes with it (the controller then starts a new session at Done).
import { CUSTOM_COPY as C } from './copy.js';
import { initialFor } from './scene.js';

const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

// getPanel() is the current session's panel (it is made again for every session). alive() is false once the page is
// gone. chat.mount(node) puts the card in the conversation; doc is the document that makes it.
// Hand edits, as main.js wires Home: editing(on) turns the panel's edit mode on or off (its edits go to getLive(), the
// current live), shows the clock control (js/engine/clockctl.js) and marks editRoot `live-edit`; clock(c) renders the
// clock control. statusTextEl is the status row's text (the row also holds the clock control); while the clock control
// shows, a status line that is only the clock's label is dropped, as on Home. autos is the Automations section
// (js/engine/automations.js), one for the page: automations(list | null) renders it, automationFlash(id, kind) flashes a card,
// attention() folds its list when it lies over the chat and a plan waits for approval.
export function liveUiFor({ statusEl, statusTextEl = statusEl, getPanel, room3d, R, baselineRef, onBackToBuilding, chat, alive, doc = globalThis.document,
  clockCtl = null, editRoot = null, getLive = () => null, autos = null }) {
  let clockOn = false, clockText = null;
  return {
    alive: () => !!alive(),
    status: (text, bad) => { if (clockOn && clockText && text === clockText) return; statusTextEl.textContent = text; statusEl.classList.toggle('bad', !!bad); },
    editing: (on) => {
      clockOn = !!on; editRoot?.classList.toggle('live-edit', !!on);
      getPanel()?.editable(on, { onEdit: (p, v) => getLive()?.edit(p, v) });
      clockCtl?.active(on);
    },
    clock: (c) => { clockText = c?.label || null; clockCtl?.render(c); },
    automations: (list) => autos?.render(list),
    automationFlash: (id, kind) => autos?.flash(id, kind),
    attention: () => autos?.attention(),
    beats: () => {}, // no beat bar in this scene
    track: (on) => getPanel()?.track(on),
    rewind: () => getPanel()?.rewind(),
    resetRoom: () => { room3d.reset(); R.clearMarkers(); },
    baseline: () => baselineRef.current,
    // Live mode ended under the visitor (a limit, live switched off, an error making the session): back to building,
    // or another scene.
    offerGuided: () => {
      const node = doc.createElement('div'); node.className = 'card end custom-exit';
      node.innerHTML = `<div class="row"><button class="btn primary" type="button">${esc(C.toBuilding)}</button><a class="btn" href="#">${esc(C.otherScenes)}</a></div>`;
      const b = node.querySelector('button');
      b.onclick = () => { if (onBackToBuilding()) b.disabled = true; };
      chat.mount(node);
    },
  };
}

// A new live session is a new room: the old panel goes first, then the store is put back to the new devices' initials
// and a fresh panel is made. The old panel's subscriptions must be dropped BEFORE store.restore: its render reads every
// device it was made for, and restore drops the slices of devices that left (a TypeError in it would leave done() with
// no live). Its DOM listeners go with its element, which replaceEl() swaps for a fresh one.
//   begin(scene) → the new panel; dispose() drops the subscriptions (the page's teardown).
// A removed device's state goes with it, so one added later under the same id starts from the catalog, not the old run.
export function dropGone(store, layout) {
  const ids = new Set(['hub', 'env', 'plan', ...layout.devices.map((d) => d.id)]);
  for (const k of Object.keys(store.state)) if (!ids.has(k)) delete store.state[k];
}

export function createSessionWiring({ store, baselineRef, replaceEl, makePanel, clearRoom }) {
  let subs = [], panel = null;
  const drop = () => { for (const off of subs) off(); subs = []; };
  return {
    get panel() { return panel; },
    begin(scene) {
      drop(); panel = null;
      replaceEl(); clearRoom();
      store.restore(initialFor(scene)); baselineRef.current = store.snapshot();
      const scoped = { ...store, subscribe: (fn) => { const off = store.subscribe(fn); subs.push(off); return off; } };
      panel = makePanel(scene, scoped);
      return panel;
    },
    dispose() { drop(); panel = null; },
  };
}
