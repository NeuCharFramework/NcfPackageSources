// The "Build your own" page (#custom): the invite-code gate, then build mode (inventory, drag placement, the selection
// bar), then run mode (the device cards and the live chat). The modes and the layout live in the controller
// (controller.js); this file is its DOM and 3D side, laid out as main.js's mountScene.
//   mountCustom({ app, gen, isCurrent, setCurrent }) → Promise<void>
//     isCurrent() is false once the visitor has routed elsewhere; setCurrent({ teardown }) hands main.js the teardown.
// Markup: the header (logo, name, Build | Run, Done building →, Scenes, social links) and .stage.custom[data-mode] with
// .room (canvas, hint, #selbar, #notice, #changedNote, #emptyNote) and .side (#inventory, #panel, #autos, #chat). The mode decides what shows:
//   gate  the chat (the access card)        build  the inventory        run  the device cards, the automations and the chat
// No header "Live AI" button here (the scripted scenes have one, js/engine/liveentry.js): this page's own gate and
// "Done building →" are its way into live mode.
// Every live session (controller.done with a new device list) gets a fresh store state (initialFor), a fresh panel and
// a cleared chat: a new room is a new conversation. The window.__ncb hooks are for QA in the browser.
import { createStore } from '../engine/store.js';
import { createChat } from '../engine/chat.js';
import { createPanel } from '../engine/panel.js';
import { createLive, codeStore, LIVE_COPY } from '../engine/live.js';
import { createLiveView } from '../engine/livechat.js';
import { createLiveApi } from '../engine/liveapi.js';
import { createClockCtl } from '../engine/clockctl.js';
import { createAutomations, foldMedia } from '../engine/automations.js';
import { socialLinks } from '../engine/social.js';
import { liveApiBase } from '../live-config.js';
import { CUSTOM_COPY as C } from './copy.js';
import { createCustomController } from './controller.js';
import { passGate } from './gate.js';
import { sceneFor, initialFor, cameraFor } from './scene.js';
import { createRoom3d } from './room3d.js';
import { createInventory, tileOrder } from './inventory.js';
import { createThumbs } from './thumbs.js';
import { createPlacement } from './placement.js';
import { liveUiFor, createSessionWiring, dropGone } from './liveui.js';

const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const coarse = typeof matchMedia === 'function' && matchMedia('(pointer: coarse)').matches;
const ORBIT_HINT = coarse ? 'drag · pinch · tap a device' : 'drag to orbit · scroll to zoom · right-drag to pan · tap a device';
const NOTICE_MS = 4500;
const THUMB_IDLE_MS = 60000; // build mode closed this long: the pictures' WebGL context goes (it is built again on return)
// localStorage itself throws in some browsers with site data blocked; the layout and code stores then work in memory only.
const safeStorage = () => { try { return globalThis.localStorage ?? null; } catch { return null; } };

export async function mountCustom({ app, isCurrent, setCurrent }) {
  if (!isCurrent()) return; // this module arrived after the visitor routed elsewhere: their page stays as it is
  app.classList.remove('picker-mode', 'failed', 'live-edit');
  app.innerHTML = `
    <header class="top custom-top"><a class="logo" href="#" aria-label="All scenes"></a><a class="brand" href="#" style="text-decoration:none">NeuCharBox</a><div class="scene-name">${esc(C.cardTitle)}</div>
      <div class="modebar"><div class="seg" role="group" aria-label="Mode"><button type="button" data-mode="build" aria-pressed="false" disabled>${esc(C.build)}</button><button type="button" data-mode="run" aria-pressed="false" disabled>${esc(C.run)}</button></div><button class="btn small primary done" type="button" disabled>${esc(C.done)}</button></div>
      <div class="spacer"></div><a class="btn small secondary" href="#"><span class="back" aria-hidden="true">‹ </span>Scenes</a><nav class="social" aria-label="Follow NeuCharBox"><span class="social-label">Follow</span>${socialLinks()}</nav></header>
    <div class="stage custom" data-mode="gate">
      <div class="room"><canvas id="room"></canvas><div class="hint" id="hint">${esc(ORBIT_HINT)}</div><button class="reset" id="resetView" type="button" title="Reset view">⟲ view</button><div class="selbar" id="selbar" role="toolbar" aria-label="Selected device" hidden></div><div class="notice" id="notice" role="status" hidden></div><p class="empty-note" id="emptyNote" hidden></p><p class="changed-note" id="changedNote" role="status" hidden>${esc(C.changed)}</p></div>
      <div class="status" id="status" hidden><span class="stext" id="statusText" role="status"></span><span class="clockctl" id="clockctl" hidden></span></div>
      <div class="side">
        <div class="inventory" id="inventory" hidden></div>
        <div class="panel" id="panel" hidden></div>
        <div class="autos" id="autos" hidden></div>
        <div class="chat" id="chat"></div>
      </div>
    </div>`;
  const $ = (id) => document.getElementById(id);
  const stage = app.querySelector('.stage.custom'), statusEl = $('status'), statusTextEl = $('statusText'), hintEl = $('hint'), noticeEl = $('notice'), changedEl = $('changedNote'), chatEl = $('chat'), invEl = $('inventory');
  let panelEl = $('panel');
  const buildBtn = app.querySelector('.seg [data-mode="build"]'), runBtn = app.querySelector('.seg [data-mode="run"]'), doneBtn = app.querySelector('.top .done');

  const [{ createRenderer }, partsMod] = await Promise.all([import('../engine/renderer.js'), import('../engine/parts.js')]);
  if (!isCurrent()) return; // the visitor navigated away while the 3D code was loading

  const storage = safeStorage(), codes = codeStore(), base = liveApiBase();
  const qp = new URLSearchParams(location.search).get('q'); // ?q=high|low locks the render tier (QA screenshots)
  const R = createRenderer($('room'), { camera: cameraFor(innerWidth, innerHeight), ...(qp === 'high' || qp === 'low' ? { quality: qp, lockQuality: true } : {}) });
  let controller = null, placement = null, inventory = null, scene = null, hooks = null, wiring = null, thumbs = null, thumbTimer = 0, gate = null, autos = null;
  const thumbsDone = new Set();
  const baselineRef = { current: null };
  let noticeTimer = 0, doneBusy = false, shownCanBuild = null, torn = false;
  const teardown = () => {
    if (torn) return; torn = true;
    gate?.cancel(); // a gate still waiting on /health settles now, so a late answer mounts nothing
    clearTimeout(noticeTimer); clearTimeout(thumbTimer);
    thumbs?.dispose();
    controller?.teardown(); placement?.dispose(); inventory?.dispose();
    wiring?.dispose(); autos?.dispose();
    R.dispose();
    app.classList.remove('live-edit');
    if (window.__ncb === hooks) window.__ncb = null;
  };
  setCurrent({ scene: null, teardown });
  try {
    const M = partsMod.materials(), P = partsMod.parts(R.scene, M);
    const room3d = createRoom3d({ R, P, M, THREE: R.THREE });
    const chat = createChat(chatEl, { onPromptText: (text) => controller?.live?.submit(text) });
    const chatLog = chatEl.querySelector('.chat-log');

    const say = (text) => {
      if (!text) return;
      noticeEl.textContent = text; noticeEl.hidden = false;
      clearTimeout(noticeTimer); noticeTimer = setTimeout(() => { noticeEl.hidden = true; }, NOTICE_MS);
    };
    const toScenes = () => { if (isCurrent()) location.hash = ''; };
    const ping = (id) => {
      const n = id === 'hub' ? 'NeuCharBox' : scene?.devices[id]?.name; if (!n) return;
      room3d.focus(id); R.ping(id, n, { hold: 1200 });
    };

    // The header's Build | Run and Done building →. Done (and Run, which is Done with the room unchanged) stays off while
    // a done() is in flight, so a double click can't open two sessions.
    function updateBar() {
      if (!controller) return;
      const m = controller.mode, has = controller.layout.devices.length > 0, busy = m === 'run' && !controller.canBuild;
      buildBtn.setAttribute('aria-pressed', String(m === 'build')); runBtn.setAttribute('aria-pressed', String(m === 'run'));
      changedEl.hidden = !(m === 'build' && !!controller.live); // the persistent note: while a live session exists, a device change restarts the conversation at Done building
      buildBtn.disabled = m === 'gate' || busy; buildBtn.title = busy ? C.buildBusy : '';
      runBtn.disabled = m !== 'build' || doneBusy || !has || !controller.live;
      doneBtn.hidden = m === 'run'; doneBtn.disabled = m !== 'build' || doneBusy || !has;
      shownCanBuild = controller.canBuild;
    }
    async function finish() {
      if (doneBusy || !controller || controller.mode !== 'build' || !controller.layout.devices.length) return;
      doneBusy = true; updateBar();
      try { await controller.done(); } catch (e) { console.error(e); } finally { doneBusy = false; if (isCurrent()) updateBar(); }
    }
    doneBtn.onclick = finish;
    runBtn.onclick = finish;
    buildBtn.onclick = () => { if (controller?.mode === 'run') controller.toBuild(); };

    // Each tile's icon gives way to a picture of the device's model (thumbs.js), type by type as they are drawn; where
    // WebGL is missing the icons stay. Entering build mode again asks only for the types still without one.
    function fillThumbs() {
      clearTimeout(thumbTimer); thumbTimer = 0;
      if (!inventory) return;
      thumbs ||= createThumbs({ THREE: R.THREE, parts: partsMod });
      for (const t of tileOrder()) {
        if (thumbsDone.has(t.type)) continue;
        thumbs.get(t.type).then((url) => { if (url && !torn) { thumbsDone.add(t.type); inventory.setThumb(t.type, url); } });
      }
    }

    function showMode(m) {
      stage.dataset.mode = m;
      invEl.hidden = m !== 'build'; panelEl.hidden = m !== 'run'; chatEl.hidden = m === 'build'; statusEl.hidden = m !== 'run';
      hintEl.textContent = m === 'build' ? C.hint : ORBIT_HINT;
      if (m !== 'build') placement?.clear();
      // build: the phone sheet starts folded, so the room shows; leaving it arms the timer that frees the thumbnails' WebGL context
      if (m === 'build') { inventory?.open(false); inventory?.refresh(); fillThumbs(); } else if (thumbs && !thumbTimer) thumbTimer = setTimeout(() => { thumbTimer = 0; thumbs.dispose(); }, THUMB_IDLE_MS);
      updateBar();
    }

    let liveUi = null;
    // A live session for this room (controller.done with a new device list).
    function makeLive(sc, devices) {
      liveUi.editing(false); // the old room's edit mode, clock control and live-edit go now, not when the new session answers
      liveUi.automations(null); // and its automations: the new session sends its own (none yet)
      scene = sc;
      wiring.begin(sc); // the old panel's subscriptions go before the store changes (liveui.js)
      chatLog.replaceChildren(); chat.enableInput(false); chat.live(false);
      const view = createLiveView({ chat, scene: sc });
      // live.js's own access card (a saved code that died at Done building) says "Back to scenes" here, and goes there
      // (the route's teardown closes the live).
      const wrapped = { ...view, access: (spec, h) => view.access({ ...spec, backLabel: C.back, offlineText: C.offline }, { ...h, onBack: toScenes }) };
      return createLive({ scene: sc, store, panel: wiring.panel, base, codes, view: wrapped, ui: liveUi, sessionExtra: () => ({ devices }) });
    }

    controller = createCustomController({
      storage, makeLive,
      ui: {
        mode: (m) => showMode(m),
        layout: (l) => { dropGone(store, l); room3d.sync(l); placement?.sync(l); inventory?.refresh(); updateBar(); },
        notice: (kind) => { if (kind !== 'changed') say(C[kind]); }, // 'changed' shows as the persistent #changedNote
      },
    });
    const store = createStore(initialFor(sceneFor(controller.layout)));
    wiring = createSessionWiring({
      store, baselineRef, clearRoom: () => { R.clearMarkers(); room3d.reset(); },
      replaceEl: () => { const fresh = panelEl.cloneNode(false); panelEl.replaceWith(fresh); panelEl = fresh; },
      makePanel: (sc, scoped) => createPanel(panelEl, sc, scoped, { onSelect: ping }),
    });
    // The clock control beside the status line: shown while a live session's cards are editable, usable between requests.
    const clockCtl = createClockCtl($('clockctl'), { onAdvance: (h) => controller?.live?.advance(h) });
    // The Automations section under the device cards (run mode only, css/custom.css); its switches go to the current live.
    autos = createAutomations($('autos'), { onToggle: (id, on) => controller?.live?.setAutomation(id, on), onRemove: (id) => controller?.live?.removeAutomation(id), onClear: () => controller?.live?.clearAutomations(), ...foldMedia(), emptyText: LIVE_COPY.autosEmptyAny });
    liveUi = liveUiFor({
      statusEl, statusTextEl, getPanel: () => wiring.panel, room3d, R, baselineRef, chat, alive: () => isCurrent() && !torn,
      onBackToBuilding: () => controller.toBuild(),
      clockCtl, editRoot: app, getLive: () => controller?.live, autos,
    });
    room3d.sync(controller.layout);
    R.onFrame((t) => {
      store.tick(performance.now()); room3d.update(store.state, t);
      if (controller.mode === 'run' && controller.canBuild !== shownCanBuild) updateBar(); // a request started or ended
    });
    R.onPick((id) => { if (controller.mode === 'run' && scene) { wiring.panel?.select(scene.devices[id] ? id : null); ping(id); } });
    $('resetView').onclick = () => R.resetView();
    R.start();

    placement = createPlacement({ R, room3d, controller, canvas: $('room'), selbar: $('selbar') });
    inventory = createInventory(invEl, {
      onAdd: (type) => { const id = controller.add(type); if (id) placement.select(id); },
      onClear: () => { placement.clear(); controller.clear(); },
      inRoom: () => new Set(controller.layout.devices.map((d) => d.type)),
      count: () => controller.layout.devices.length,
      note: $('emptyNote'),
    });
    // Tapping or dragging in the room folds the phone sheet back down, so the room shows.
    $('room').addEventListener('pointerdown', () => inventory.open(false), { passive: true });

    hooks = { scene: 'custom', controller, store, R, room3d, placement, inventory, autos, get live() { return controller.live; } };
    window.__ncb = hooks;
    showMode(controller.mode);

    // The gate: the access card in the chat until a code works ("Back to scenes" leaves the page). With no API base
    // (main.js doesn't route here then) it shows the offline card without asking anything.
    const api = createLiveApi({ base, getCode: () => codes.get() });
    gate = passGate({ api, codes, view: createLiveView({ chat, scene: sceneFor(controller.layout) }), onBack: toScenes, offline: !base, base });
    const passed = await gate;
    if (!passed || !isCurrent() || torn) return;
    chatLog.replaceChildren(); // the spent access card goes; run mode starts its own conversation
    controller.gated();
  } catch (e) {
    if (isCurrent()) setCurrent(null);
    teardown();
    throw e;
  }
}
