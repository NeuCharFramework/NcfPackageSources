// Device dashboard: one tile per device (icon + name + state), rendered from the store.
// Tone: offline (grey, dimmed) · idle (grey icon) · on (amber) · busy (blue) · bad (red).
// On phones it is a horizontal strip, on desktop a two-column grid; either way only the panel scrolls, never the page.
import { icon } from './icons.js';
import { controlFor, editsFor } from './controls.js';

const looksActive = (s) => !!(s.on || s.running || s.armed || s.active || s.pressed || s.moving || s.state === 'moving' || (s.open > 0.5) || (s.brightness > 0.02) || s.shutter === 'open' || s.light === 'green' || s.scene === 'live' || s.scene === 'recording' || (s.muted === false));
const now = () => performance.now();
const shape = (v) => v.replace(/[+\-−]?\d[\d.,:]*/g, '#'); // a value's kind, numbers aside: "14 scanned" ~ "15 scanned"
const qs = typeof location !== 'undefined' ? Number(new URLSearchParams(location.search).get('speed')) : NaN;
const DWELL = Math.max(450, 900 / (Number.isFinite(qs) && qs > 0 ? Math.min(qs, 6) : 1)); // the strip holds still this long before its next jump (?speed= as in the player)
// A slider's readout (the edit controls' value text and aria-valuetext): round(value × scale), one decimal when a step
// shows finer than 1, then the unit. A percent that rounds to 100 reads as its top word instead (the spec's `top`,
// default "full"): the copy guardrails keep "100%" out of every visitor-facing text.
export function editValueText(e, v) {
  const d = e.step * e.scale < 1 ? 1 : 0, n = Math.round(v * e.scale * 10 ** d) / 10 ** d;
  return e.unit === '%' && n >= 100 ? e.top || 'full' : `${n.toFixed(d)}${e.unit || ''}`;
}
const PAD = 12; // margin kept around a tile brought into view: the panel's own padding

export function createPanel(root, scene, store, { onSelect } = {}) {
  const ids = scene.deviceOrder || Object.keys(scene.devices).filter((d) => d !== 'hub' && d !== 'env');
  root.innerHTML = '';
  const cards = {};
  for (const id of ids) {
    const d = scene.devices[id];
    const card = document.createElement('div'); // a div, not a button: edit mode puts real controls inside it
    card.className = 'ent offline'; card.dataset.id = id; card.setAttribute('role', 'button'); card.tabIndex = 0;
    card.innerHTML = `<span class="ico">${icon(d.icon)}</span><span class="txt"><b>${d.name}</b><span class="val"></span><span class="ctl" hidden></span></span><span class="badge"></span>`;
    card.onclick = () => { select(id); onSelect && onSelect(id); };
    card.onkeydown = (e) => { if (e.target === card && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); card.click(); } }; // as a click did on the button
    root.appendChild(card); cards[id] = card;
  }

  // The visitor's hand wins: while they touch, and for 3 s after they last drag, wheel or key through the dashboard,
  // nothing scrolls it for them (a tile they tap is still brought fully into view).
  let handAt = -Infinity, handDown = false, lastMove = -Infinity, focus = []; // focus: the tiles last brought into view
  root.addEventListener('scroll', () => { lastMove = now(); }, { passive: true }); // a smooth scroll moves for a while: the dwell counts from its end
  // Where a smooth scroll still in flight is taking the strip: until it lands, "in view" is judged there, not where the
  // strip is right now (a fault that pops just as the strip starts back to its first tile must still be brought into
  // view). The visitor's hand cancels it; a scroll that never lands (interrupted) stops counting after 1.5 s.
  let aim = null;
  const pos = () => { if (aim && now() - aim.at < 1500 && (Math.abs(root.scrollLeft - aim.x) > 1 || Math.abs(root.scrollTop - aim.y) > 1)) return aim; aim = null; return { x: root.scrollLeft, y: root.scrollTop }; };
  const glide = (x, y, smooth = true) => { root.scrollTo({ left: x, top: y, behavior: smooth ? 'smooth' : 'auto' }); aim = smooth ? { x, y, at: now() } : null; lastMove = now(); };
  const hand = () => { handAt = now(); focus = []; aim = null; };
  for (const ev of ['pointerdown', 'touchstart']) root.addEventListener(ev, () => { handDown = true; hand(); }, { passive: true });
  for (const ev of ['pointerup', 'pointercancel', 'touchend', 'touchcancel']) root.addEventListener(ev, () => { handDown = false; hand(); }, { passive: true });
  for (const ev of ['touchmove', 'wheel', 'keydown']) root.addEventListener(ev, hand, { passive: true });
  const handBusy = () => handDown || now() - handAt < 3000;

  // A tile's box in the panel's scroll coordinates, from its layout size around its on-screen centre: a tile mid-pop
  // is scaled (.85 to 1.06), and aligning to that box once left the last tile cut at the screen edge.
  function box(c) {
    const r = c.getBoundingClientRect(), p = root.getBoundingClientRect();
    const cx = (r.left + r.right) / 2 - p.left - root.clientLeft + root.scrollLeft, cy = (r.top + r.bottom) / 2 - p.top - root.clientTop + root.scrollTop;
    return { l: cx - c.offsetWidth / 2, r: cx + c.offsetWidth / 2, t: cy - c.offsetHeight / 2, b: cy + c.offsetHeight / 2 };
  }
  const shown = (id) => { const c = cards[id]; if (!c || c.offsetParent === null) return true; const b = box(c), { x, y } = pos(); return b.l >= x - 1 && b.r <= x + root.clientWidth + 1 && b.t >= y - 1 && b.b <= y + root.clientHeight + 1; };
  // Scroll the panel so these tiles show, nearest edge, as many as fit together in order. Returns the ids it covered
  // ([] while the visitor's hand is on the panel).
  function reveal(list, { force = false, smooth = true } = {}) {
    if (!force && handBusy()) return [];
    const items = list.filter((id) => cards[id] && cards[id].offsetParent !== null).map((id) => ({ id, b: box(cards[id]) }));
    if (!items.length) return [];
    const w = root.clientWidth, h = root.clientHeight; let u = items[0].b; const got = [items[0].id];
    for (const { id, b } of items.slice(1)) {
      const n = { l: Math.min(u.l, b.l), r: Math.max(u.r, b.r), t: Math.min(u.t, b.t), b: Math.max(u.b, b.b) };
      if (n.r - n.l > w - 2 * PAD || n.b - n.t > h - 2 * PAD) break;
      u = n; got.push(id);
    }
    const { x, y } = pos();
    // Across (the phone strip), the strip starts at a tile, just clear of the one before it (the gap between tiles, so
    // not even its border shows): scrolling left, at the first of these tiles; scrolling right, at the first tile that
    // still keeps them all in view, rather than with them at the right edge and a cut tile, maybe only its empty
    // right-hand part, on the left. A small nudge (a tile in view whose value grew) stays a nudge. (At the end of the
    // strip the last tiles decide where it stops.)
    const gap = (parseFloat(getComputedStyle(root).columnGap) || 8) - 1; // a pixel less: the border before never peeks in
    let nx = x;
    if (u.l - PAD < x) nx = u.l - gap <= PAD ? 0 : u.l - gap;
    else if (u.r + PAD > x + w && u.r + PAD - w - x <= 40) nx = u.r + PAD - w;
    else if (u.r + PAD > x + w) { nx = u.r + PAD - w; const edge = ids.map((id) => cards[id]).filter((c) => c.offsetParent !== null).map((c) => box(c).l - gap).filter((e) => e >= nx - 1 && e <= u.l - gap + 1).sort((a, c) => a - c)[0]; if (edge != null) nx = edge; }
    const ny = u.t - PAD < y ? u.t - PAD : u.b + PAD > y + h ? u.b + PAD - h : y;
    const tx = Math.max(0, Math.min(root.scrollWidth - w, Math.round(nx))), ty = Math.max(0, Math.min(root.scrollHeight - h, Math.round(ny)));
    if (Math.abs(tx - x) > 1 || Math.abs(ty - y) > 1) glide(tx, ty, smooth);
    focus = got;
    return got;
  }
  // Back to the first tile (when the chips are offered, and when a what-if starts over); drops any reveal still waiting
  // and the faulted tile it kept in view.
  function rewind() { pinned = null; queue = []; clearTimeout(timer); timer = 0; clearTimeout(popTimer); popTimer = 0; focus = []; const { x, y } = pos(); if (!handBusy() && (x > 0 || y > 0)) glide(0, 0); }

  // Following a request (the host turns this on at its start and again at each beat, and off at its end): a tile is
  // brought into view the first time it changes in the request, and after that when its state changes kind (a new
  // tone, or a new kind of value: "15 scanned" to "15 scanned · 1 unreadable", not 14 to 15 scanned) to one it hasn't
  // shown in this beat. So a tween or a counter moves the strip once, not every frame, and a device toggling between
  // two states moves it at most twice a beat. The strip holds still for DWELL before each jump (a change that happens
  // in view holds it too), then shows the oldest waiting tiles (of tiles that changed together, the nearest first), as
  // many as fit together; what is still waiting when the request ends is shown after it. The tile in view is kept
  // fully in view while its value grows. While a device is in fault (`pinned`, from its pop), its tile stays in view:
  // later changes show beside it if they fit together, else not at all, so the strip doesn't scroll away from what
  // failed while NCB talks about it.
  let changed = null, beatKeys = null, queue = [], timer = 0, batch = 0, pinned = null;
  const pinNow = () => (pinned && store.state[pinned]?.status === 'fault' ? pinned : null);
  function track(on) {
    if (!on) { changed = beatKeys = null; return; }
    if (!changed) { changed = new Set(); queue = []; clearTimeout(timer); timer = 0; }
    beatKeys = new Map(ids.map((id) => [id, new Set()]));
  }
  function note(id, key, prev) {
    const first = !changed.has(id); changed.add(id);
    const ks = beatKeys.get(id); const fresh = key !== prev && !ks.has(key); if (key !== prev) ks.add(key);
    if (!first && !fresh) { if (focus.includes(id) && !queue.length && !shown(id)) reveal(pinNow() && pinNow() !== id ? [pinNow(), id] : [id]); return; }
    const q = queue.find((e) => e.id === id); if (q) { if (fresh) q.keys.push(key); q.first ||= first; } else queue.push({ id, keys: fresh ? [key] : [], first, batch });
    if (!timer) timer = setTimeout(flush, 0);
  }
  function flush() {
    timer = 0; batch++; if (!queue.length) return;
    if (handBusy()) { if (changed) for (const e of queue) { for (const k of e.keys) beatKeys.get(e.id).delete(k); if (e.first) changed.delete(e.id); } queue = []; return; } // a later change brings them up
    const quiet = now() - lastMove; if (quiet < DWELL) { timer = setTimeout(flush, DWELL - quiet + 20); return; }
    const vis = queue.filter((e) => shown(e.id)).map((e) => e.id);
    if (vis.length) { queue = queue.filter((e) => !vis.includes(e.id)); focus = vis; lastMove = now(); }
    else {
      const { x, y } = pos(), w = root.clientWidth, h = root.clientHeight;
      const away = (e) => { const b = box(cards[e.id]); return Math.max(0, x - b.l, b.r - x - w) + Math.max(0, y - b.t, b.b - y - h); };
      const head = queue.filter((e) => e.batch === queue[0].batch).map((e) => [away(e), e]).sort((a, b) => a[0] - b[0]).map(([, e]) => e.id);
      const pin = pinNow() && !head.includes(pinned) ? pinned : null;
      const got = reveal(pin ? [pin, ...head] : head);
      queue = pin && got.length < 2 ? queue.filter((e) => !head.includes(e.id)) : queue.filter((e) => !got.includes(e.id));
    }
    if (queue.length) timer = setTimeout(flush, DWELL);
  }

  // The card's control row: a slim bar or a switch, shown in every status; hidden when the device has none.
  function setControl(c, ctl) {
    const el = c.querySelector('.ctl'); el.hidden = !ctl;
    if (!ctl) { el.textContent = ''; el._kind = null; return; }
    if (el._kind !== ctl.kind) { el._kind = ctl.kind; el.innerHTML = ctl.kind === 'bar' ? '<span class="bar"><i></i></span>' : '<span class="sw"></span>'; }
    if (ctl.kind === 'bar') el.querySelector('i').style.width = `${Math.round(ctl.value * 100)}%`;
    else el.querySelector('.sw').classList.toggle('on', !!ctl.on);
  }
  // ---- edit mode (live): the controls of editsFor() on the cards, in the store's units ----
  // A slider shows its value as round(value × scale) + unit; it reports on `change` (a release, a key press), in store
  // units. A switch reports !on on click. The controls never select the card: they stop click, pointer and key events.
  let onEditCb = null, editing = false;
  const OFFTONES = ['offline', 'bad', 'busy'];
  const KEY_MS = 400;
  const showVal = editValueText;
  const fieldOf = (e) => e.label || e.path.split('.').slice(1).join('.');
  function buildEdits(id) {
    const c = cards[id], d = scene.devices[id], list = editsFor(id, d, store.state[id]);
    if (!list.length) return;
    const box = document.createElement('span'); box.className = list.length === 1 && list[0].kind === 'slider' ? 'edits one' : 'edits';
    c._ctls = [];
    for (const e of list) {
      const row = document.createElement('span'); row.className = `erow ${e.kind}`;
      const name = `${d.name} ${fieldOf(e)}`;
      let el;
      if (e.kind === 'slider') {
        el = document.createElement('input'); el.type = 'range'; el.className = 'edit-range';
        el.min = String(Math.round(e.min * e.scale * 1e6) / 1e6); el.max = String(Math.round(e.max * e.scale * 1e6) / 1e6); el.step = String(Math.round(e.step * e.scale * 1e6) / 1e6);
        const val = document.createElement('span'); val.className = 'eval';
        if (e.label) { const lab = document.createElement('span'); lab.className = 'elab'; lab.textContent = e.label; row.appendChild(lab); }
        row.append(el, val);
        const show = (shown = Number(el.value)) => { const n = Number(el.value); val.textContent = showVal(e, shown / e.scale); el.setAttribute('aria-valuetext', val.textContent); el.style.setProperty('--fill', `${((n - el.min) / (el.max - el.min)) * 100}%`); };
        el._show = show;
        el.oninput = () => { el._at = now(); show(); };
        // A pointer release reports at once. Chrome also fires `change` on every arrow press, so a key-driven change waits
        // until the keys stop (KEY_MS) or the slider loses focus: one edit for one adjustment, not one per step.
        const send = () => { clearTimeout(el._kt); el._kt = 0; el._keyed = false; const v = Math.round((Number(el.value) / e.scale) * 1e6) / 1e6; if (onEditCb) onEditCb(e.path, v); };
        el.onkeydown = (k) => { if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End'].includes(k.key)) el._keyed = true; };
        el.onchange = () => { el._at = now(); show(); if (el._keyed) { clearTimeout(el._kt); el._kt = setTimeout(send, KEY_MS); } else send(); };
        const rest = () => { el._drag = false; removeEventListener('pointerup', rest, true); removeEventListener('pointercancel', rest, true); setTimeout(() => { if (editing) render(store.state); }, 0); }; // after the release's own `change`
        // Capture phase: the controls' box stops pointerup from bubbling, so a bubbling listener would never hear a release.
        el.onpointerdown = () => { el._drag = true; addEventListener('pointerup', rest, true); addEventListener('pointercancel', rest, true); };
        el.onblur = () => { rest(); if (el._kt) send(); };
      } else {
        el = document.createElement('button'); el.type = 'button'; el.className = 'sw edit-sw'; el.setAttribute('role', 'switch'); el.setAttribute('aria-checked', 'false');
        const lab = document.createElement('span'); lab.className = 'elab'; lab.textContent = e.label || fieldOf(e);
        row.append(el, lab);
        el.onclick = () => { if (onEditCb) onEditCb(e.path, el.getAttribute('aria-checked') !== 'true'); };
      }
      el.dataset.path = e.path; el.setAttribute('aria-label', name);
      c._ctls.push({ el, e }); box.appendChild(row);
    }
    // A control never selects its card nor pings it in 3D. The panel's hand-tracking (which would see these events
    // otherwise) is kept by hand: the strip doesn't scroll away from a slider being dragged.
    for (const ev of ['click', 'keyup', 'pointerup']) box.addEventListener(ev, (x) => x.stopPropagation());
    box.addEventListener('keydown', (x) => { x.stopPropagation(); hand(); });
    const lift = () => { handDown = false; hand(); removeEventListener('pointerup', lift, true); removeEventListener('pointercancel', lift, true); };
    box.addEventListener('pointerdown', (x) => { x.stopPropagation(); handDown = true; hand(); addEventListener('pointerup', lift, true); addEventListener('pointercancel', lift, true); });
    c.querySelector('.txt').appendChild(box); c._edits = box; c.classList.add('has-edits');
    c.setAttribute('role', 'group'); c.setAttribute('aria-label', d.name); // button children are presentational: with controls inside, the card is a group (still tabbable; Enter/Space/click select)
  }
  function syncEdits(id, s, tone) {
    const c = cards[id]; if (!c._edits) return;
    const list = editsFor(id, scene.devices[id], s), off = OFFTONES.includes(tone);
    const key = `${off}|${list.map((e) => e.value).join(',')}`;
    const stale = c._ctls.some(({ el }) => el._drag || (el.ownerDocument.activeElement === el && now() - (el._at || -Infinity) < 1200)); // being used: leave it be
    if (c._ek === key && !stale) return;
    list.forEach((e, i) => {
      const { el } = c._ctls[i], v = Number(e.value), ok = Number.isFinite(v);
      el.disabled = off || !ok;
      if (e.kind === 'switch') { el.setAttribute('aria-checked', String(!!e.value)); el.classList.toggle('on', !!e.value); return; }
      if (el._drag) return;
      const left = el.ownerDocument.activeElement === el ? 1200 - (now() - (el._at || -Infinity)) : 0;
      if (left > 0) { clearTimeout(el._t); el._t = setTimeout(() => { if (editing && c._ctls) { c._ek = null; render(store.state); } }, left + 20); return; } // in use: look again when the window ends
      if (ok) { el.value = String(Math.round(v * e.scale * 1e6) / 1e6); el._show(v * e.scale); } // the text shows the store's value, which the input may have snapped to its step
    });
    c._ek = stale ? null : key;
  }
  function editable(on, { onEdit } = {}) {
    editing = !!on; onEditCb = on ? onEdit || null : null;
    root.classList.toggle('editable', editing);
    for (const id of ids) {
      const c = cards[id];
      if (c._edits) { for (const { el } of c._ctls) { clearTimeout(el._t); clearTimeout(el._kt); } c._edits.remove(); c._edits = c._ctls = c._ek = null; c.classList.remove('has-edits'); c.setAttribute('role', 'button'); c.removeAttribute('aria-label'); }
      if (editing) buildEdits(id);
    }
    render(store.state);
  }
  const markTimers = {};
  // mark(id, 'just' | 'busy' | null): the JUST SET (clears itself after 2 s) and WORKING badges.
  function mark(id, kind) {
    const c = cards[id]; if (!c) return;
    clearTimeout(markTimers[id]); delete markTimers[id];
    c.classList.toggle('marked-just', kind === 'just'); c.classList.toggle('marked-busy', kind === 'busy');
    c.querySelector('.badge').textContent = kind === 'just' ? 'JUST SET' : kind === 'busy' ? 'WORKING' : '';
    if (kind === 'just') markTimers[id] = setTimeout(() => mark(id, null), 2000);
  }

  function render(state) {
    for (const id of ids) {
      const s = state[id], d = scene.devices[id], c = cards[id];
      if (!s) continue; // a custom room's device removed while this panel still lists it: its card keeps its last value
      const tone = s.status === 'offline' ? 'offline' : s.status === 'fault' ? 'bad' : s.status === 'busy' ? 'busy' : (d.active ? d.active(s, state) : looksActive(s)) ? 'on' : 'idle';
      // A fault shows the scenario's note for it (fail.faultText → <id>.faultNote, rewritten later if the scene says so;
      // every store write re-renders), else the device's default fault text.
      const v = s.status === 'offline' ? 'not connected' : s.status === 'fault' ? (s.faultNote || d.faultText || 'unavailable') : d.format(s, state);
      const ctl = controlFor(d, s, state), ck = ctl ? `${ctl.kind}|${ctl.kind === 'switch' ? ctl.on : Math.round(ctl.value * 100) / 100}` : '';
      if (c._ck !== ck) { c._ck = ck; setControl(c, ctl); } // a moving tween updates the bar; the change-tracking key below ignores it
      if (c._edits) syncEdits(id, s, tone);
      if (c._tone === tone && c._v === v) continue;
      if (c._tone !== tone) { c._tone = tone; for (const t of ['offline', 'bad', 'busy', 'on', 'idle']) c.classList.toggle(t, t === tone); }
      if (c._v !== v) { c._v = v; c.querySelector('.val').textContent = v; c.title = `${d.name}: ${v}`; } // title: the full value if the tile cuts it
      const prev = c._key; c._key = `${tone}|${shape(v)}`;
      if (changed) note(id, c._key, prev);
    }
  }
  function select(id) {
    for (const c of Object.values(cards)) c.classList.toggle('selected', c.dataset.id === id);
    if (id && cards[id]) { reveal([id], { force: true }); clearTimeout(select.t); select.t = setTimeout(() => cards[id]?.classList.remove('selected'), 2500); }
  }
  // Discovery and faults: the tile pops and comes into view at once (no dwell), or, while the visitor's hand is on
  // the panel, once it has been off it for 3 s (the latest pop only). Reveals still waiting from before are dropped, so
  // they don't scroll the strip away from a faulted tile while NCB asks what to do about it.
  let popTimer = 0;
  function pop(id) {
    const c = cards[id]; if (!c) return;
    if (store.state[id]?.status === 'fault') pinned = id;
    c.classList.remove('pop'); void c.offsetWidth; c.classList.add('pop');
    queue = []; clearTimeout(timer); timer = 0; clearTimeout(popTimer);
    const show = () => { popTimer = 0; if (handBusy()) popTimer = setTimeout(show, Math.max(100, handAt + 3050 - now())); else reveal([id]); };
    show();
  }
  // The end of a what-if: these tiles (the devices it left in fault) come into view now, and reveals still waiting are
  // dropped, so the strip ends on what failed. No pop: nothing new happened to them.
  function show(list) { queue = []; clearTimeout(timer); timer = 0; clearTimeout(popTimer); popTimer = 0; reveal(list); }
  store.subscribe(render); render(store.state);
  return { select, render, pop, reveal, rewind, track, show, mark, editable };
}
