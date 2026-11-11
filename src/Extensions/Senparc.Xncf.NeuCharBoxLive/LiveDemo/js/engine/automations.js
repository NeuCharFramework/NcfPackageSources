// The Automations section (live mode, below the device cards; spec docs/superpowers/specs/2026-11-08-automations-design.md
// §1): one card per rule or schedule the AI set up in this session's room, in creation order, each with an on/off switch.
// It shows what render() is given and reports a flipped switch to onToggle(id, on): js/engine/live.js keeps the visitor's
// value until the server agrees, and puts it back when the server refuses it.
//   createAutomations(root, { onToggle, emptyText, fold, overlay, doc })
//     render(items | null)     items: [{ id, kind: 'rule'|'schedule', text, enabled, firedToday, done }]; null: not in
//                              live mode (guided, or live mode over), the section hides
//     flash(id, 'ran'|'held')  the card's JUST RAN / HELD badge for 2 s (folded, the summary line's too)
//     open(on)                 unfold or fold the list
//     attention()              the chat needs the visitor (a plan waits for approval): the list folds if it lies over it
//     dispose()                the page's teardown
//   fold     true, or a MediaQueryList (foldMedia()): start folded, as one summary line ("Automations · 2 on"), and fold
//            again whenever the page enters that layout (a phone rotated, a window made smaller)
//   overlay  true, or a MediaQueryList: the layout in which the open list lies over the page (css/app.css: phones). There a
//            tap outside it and attention() fold it, and its height is kept inside the window.
// Escape folds an open list wherever it is and gives focus back to the summary line. A one-off schedule whose time has
// passed (done) has no switch: it never runs again.
// Built with createElement and textContent only (the server's text is never parsed as HTML), so the Node tests run it on a
// small stand-in document (test/automations.mjs).
import { LIVE_COPY as C } from './live.js';

const ICON = { rule: '⚡', schedule: '🕓' };
const FLASH_MS = 2000; // as a device card's JUST SET
let sections = 0;

// The card's small line: its kind, then "Done", "Off", or how often it ran today.
export const subLine = (a) => `${a.kind === 'schedule' ? C.autoSchedule : C.autoRule}${a.done ? ` · ${C.autoDone}` : a.enabled === false ? ` · ${C.autoOff}` : a.firedToday > 0 ? ` · ${C.autoRan(a.firedToday)}` : ''}`;
// The summary beside the heading: "2 on", "all off", "all done", "none yet". A done one-off is not "on".
export const summaryOf = (items) => {
  const on = items.filter((a) => a.enabled !== false && !a.done).length;
  return !items.length ? C.autosNone : on ? C.autosOn(on) : items.every((a) => a.done) ? C.autosAllDone : C.autosAllOff;
};
// The pages' media: folded on phones (where the open list lies over the chat) and on windows under 900 px tall (where it
// would push the chat up); elsewhere open, under the device cards. The phone breakpoint is css/app.css's.
export const FOLD_QUERY = '(max-width: 959px), (max-height: 899px)', OVERLAY_QUERY = '(max-width: 959px)';
export const foldMedia = (mm = globalThis.matchMedia?.bind(globalThis)) => (typeof mm === 'function' ? { fold: mm(FOLD_QUERY), overlay: mm(OVERLAY_QUERY) } : { fold: false, overlay: false });
const asMedia = (q) => (q && typeof q === 'object' ? q : { matches: !!q });
// A MediaQueryList's change listener on or off (Safari < 14 only has addListener / removeListener).
const listen = (m, fn, on) => { if (typeof m.addEventListener === 'function') m[on ? 'addEventListener' : 'removeEventListener']('change', fn); else if (typeof m.addListener === 'function') m[on ? 'addListener' : 'removeListener'](fn); };

export function createAutomations(root, { onToggle = () => {}, emptyText = C.autosEmptyAny, fold = false, overlay = fold, doc = globalThis.document } = {}) {
  const k = ++sections, foldM = asMedia(fold), overM = asMedia(overlay);
  let nSub = 0, open = !foldM.matches, headTimer = 0;
  const mk = (tag, cls, text) => { const n = doc.createElement(tag); if (cls) n.className = cls; if (text != null) n.textContent = text; return n; };
  root.replaceChildren();
  root.classList.add('autos'); root.setAttribute('role', 'region'); root.setAttribute('aria-label', C.autosTitle); root.hidden = true;
  const head = mk('button', 'autos-head'); head.type = 'button';
  const count = mk('span', 'autos-count'), badge = mk('span', 'autos-badge'), chev = mk('span', 'autos-chev', '▾');
  chev.setAttribute('aria-hidden', 'true');
  head.append(mk('span', 'autos-title', C.autosTitle), count, badge, chev);
  const body = mk('div', 'autos-body'); body.id = `autos-${k}`; head.setAttribute('aria-controls', body.id);
  const list = mk('ul', 'alist'), empty = mk('p', 'aempty', emptyText);
  body.append(list, empty); root.append(head, body);

  // Lying over the page, the open list stays inside the window: the room below the summary line (a portrait phone) and
  // above it (a phone held sideways, where it opens upward over the room). css/app.css reads the two lengths.
  function fit() {
    if (!overM.matches || typeof root.getBoundingClientRect !== 'function' || !body.style) return;
    const r = root.getBoundingClientRect(), top = root.closest?.('.stage')?.getBoundingClientRect().top ?? 0, h = globalThis.innerHeight || 0;
    if (h) body.style.setProperty('--autos-below', `${Math.max(120, Math.round(h - r.bottom - 8))}px`);
    body.style.setProperty('--autos-above', `${Math.max(120, Math.round(r.top - top - 6))}px`);
  }
  function setOpen(on) {
    const was = open; open = !!on;
    if (!open && was && body.contains(doc.activeElement)) head.focus(); // a folded list's switch can't keep focus
    body.hidden = !open; head.setAttribute('aria-expanded', String(open)); root.classList.toggle('open', open);
    if (open) fit();
  }
  head.onclick = () => setOpen(!open);
  root.onkeydown = (e) => { if (e.key === 'Escape' && open) { setOpen(false); head.focus(); } };
  const onFold = () => { if (foldM.matches && open) setOpen(false); }; // entering a folding layout folds it
  // Over the page, a tap anywhere else folds it (the summary line toggles it itself).
  const onTap = (e) => { if (open && overM.matches && !root.hidden && !root.contains(e.target)) setOpen(false); };
  listen(foldM, onFold, true); doc.addEventListener?.('pointerdown', onTap, { passive: true });
  setOpen(open);

  const cards = new Map(); // id → { li, ico, text, sub, sw, badge, timer }
  function cardFor(a) {
    let c = cards.get(a.id);
    if (!c) {
      const li = mk('li', 'auto'); li.dataset.id = a.id;
      const ico = mk('span', 'aico'); ico.setAttribute('aria-hidden', 'true');
      const txt = mk('span', 'atxt'), text = mk('span', 'atext'), sub = mk('span', 'asub'); sub.id = `autos-${k}-${++nSub}`;
      // A real switch: a button with role="switch" and aria-checked (Enter and Space press it), named by the automation's text.
      const sw = mk('button', 'sw auto-sw'); sw.type = 'button'; sw.setAttribute('role', 'switch'); sw.setAttribute('aria-describedby', sub.id);
      const id = a.id; sw.onclick = () => onToggle(id, sw.getAttribute('aria-checked') !== 'true');
      const b = mk('span', 'badge');
      txt.append(text, sub); li.append(ico, txt, sw, b);
      c = { li, ico, text, sub, sw, badge: b, timer: 0 }; cards.set(id, c);
    }
    const on = a.enabled !== false, done = !!a.done;
    c.li.dataset.kind = a.kind; c.li.classList.toggle('off', !on && !done); c.li.classList.toggle('done', done);
    c.ico.textContent = ICON[a.kind] || ICON.rule;
    c.text.textContent = a.text; c.sub.textContent = subLine(a);
    if (done && c.sw.contains(doc.activeElement)) head.focus(); // its switch goes: focus doesn't drop to the page
    c.sw.hidden = done;
    // Never unnamed: an automation without text is named by its sub-line.
    c.sw.setAttribute('aria-label', a.text || subLine(a)); c.sw.setAttribute('aria-checked', String(on)); c.sw.classList.toggle('on', on);
    return c;
  }
  const unflashHead = () => { clearTimeout(headTimer); headTimer = 0; badge.textContent = ''; head.classList.remove('flash-ran', 'flash-held'); };

  function render(items) {
    if (items == null) { // live mode is over: the section goes, and opens as it started next time
      for (const c of cards.values()) clearTimeout(c.timer);
      cards.clear(); list.replaceChildren(); unflashHead(); setOpen(!foldM.matches); root.hidden = true;
      return;
    }
    const keep = new Set(items.map((a) => a.id));
    for (const [id, c] of cards) {
      if (keep.has(id)) continue;
      if (c.li.contains(doc.activeElement)) head.focus(); // a re-plan dropped the card under the visitor's focus
      clearTimeout(c.timer); c.li.remove(); cards.delete(id);
    }
    // In place: a card that stays keeps its node (and the focus on its switch); only an order change moves one.
    items.forEach((a, i) => { const c = cardFor(a); if (list.children[i] !== c.li) list.insertBefore(c.li, list.children[i] ?? null); });
    list.hidden = !items.length; empty.hidden = items.length > 0;
    count.textContent = ` · ${summaryOf(items)}`;
    root.hidden = false;
    if (open) fit();
  }

  function flash(id, kind) {
    const c = cards.get(id); if (!c) return;
    const word = kind === 'held' ? C.autoHeldBadge : C.autoJustRan;
    clearTimeout(c.timer);
    c.li.classList.toggle('flash-ran', kind !== 'held'); c.li.classList.toggle('flash-held', kind === 'held'); c.badge.textContent = word;
    c.timer = setTimeout(() => { c.timer = 0; c.li.classList.remove('flash-ran', 'flash-held'); c.badge.textContent = ''; }, FLASH_MS);
    unflashHead(); // the summary line carries it too (css/app.css shows it only while the list is folded)
    badge.textContent = word; head.classList.add(kind === 'held' ? 'flash-held' : 'flash-ran');
    headTimer = setTimeout(unflashHead, FLASH_MS);
  }

  // The page's teardown: the document and media listeners go, and no flash timer is left.
  function dispose() { listen(foldM, onFold, false); doc.removeEventListener?.('pointerdown', onTap, { passive: true }); for (const c of cards.values()) clearTimeout(c.timer); unflashHead(); }

  return { render, flash, open: setOpen, attention: () => { if (open && overM.matches) setOpen(false); }, dispose, get isOpen() { return open; } };
}
