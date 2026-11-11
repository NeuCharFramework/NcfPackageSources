// The NeuCharBox conversation column. All wording comes from the scene.
// Every interactive card owns its resolver. A beat skip (resolvePending) settles each open card with the
// visitor's current choices, marks it done and disables its buttons, so a stale card can't answer a later one.
import { icon } from './icons.js';
import { socialLinks } from './social.js';

export function createChat(root, { onPromptText } = {}) {
  root.innerHTML = `
    <div class="chat-log" id="chatLog" role="log" aria-live="polite" aria-label="Conversation with NeuCharBox" tabindex="-1"></div>
    <form class="chat-input off" id="chatForm" autocomplete="off">
      <input id="chatText" type="text" placeholder="Or type what you want…" aria-label="Your request" disabled>
      <button type="submit" aria-label="Send" disabled>➤</button>
    </form>`;
  const log = root.querySelector('#chatLog');
  const form = root.querySelector('#chatForm');
  const input = root.querySelector('#chatText');
  const send = form.querySelector('button');
  let pending = [];   // [{ node, settle(value), skipValue() }]
  let picker = null;  // the last picker of failures
  let typingEl = null;
  const finePointer = typeof matchMedia === 'function' && matchMedia('(pointer: fine)').matches;

  form.addEventListener('submit', (e) => { e.preventDefault(); if (input.disabled) return; const t = input.value.trim(); if (!t) return; input.value = ''; onPromptText && onPromptText(t); });
  // Input and send button are on or off together; switching off drops any half-typed text, so it can't be sent
  // (or reappear) later. If one of them had keyboard focus, focus stays in the conversation (not back to the page top).
  // The form carries .off while they are: on a phone the bar then gives its height to the conversation (css/app.css).
  // Coming back, it takes that height from the log: a log sent to its end stays at its end, even while a smooth scroll
  // is still on its way there (a live plan that waits for Approve with the bar on keeps its buttons in view).
  const setInput = (on) => {
    if (!on && form.contains(document.activeElement)) log.focus({ preventScroll: true });
    const keepEnd = on && form.classList.contains('off') && (pinned || toEnd);
    input.disabled = !on; send.disabled = !on; form.classList.toggle('off', !on); if (!on) input.value = '';
    if (keepEnd) log.scrollTop = log.scrollHeight;
  };

  const el = (html) => { const d = document.createElement('div'); d.innerHTML = html.trim(); return d.firstElementChild; };
  // Follow new messages only while the visitor is at (or near) the bottom: reading a tall plan and pressing Edit must
  // not snap the log away from the line they changed. Adding a message always scrolls to it.
  let pinned = true;
  // toEnd: this code last sent the log to its end (push, scrollDown), and the visitor hasn't scrolled up since. Unlike
  // `pinned` it holds while a smooth scroll is still on its way down. A log that only got shorter (it clamps, still at
  // its end) doesn't count as scrolling up.
  let toEnd = true, lastTop = 0;
  // upByVisitor: the visitor scrolled the log up (a wheel, touch, key or scrollbar press came just before the scroll),
  // as opposed to this code placing a tall card at its top. Reset when code sends the log to its end or adds a card,
  // and when the visitor comes back to the end.
  let upByVisitor = false, intent = false, intentTimer = 0;
  const wantScroll = () => { intent = true; clearTimeout(intentTimer); intentTimer = setTimeout(() => { intent = false; }, 600); };
  ['wheel', 'touchstart', 'touchmove', 'mousedown', 'keydown'].forEach((t) => log.addEventListener(t, wantScroll, { passive: true }));
  log.addEventListener('scroll', () => {
    const fromEnd = log.scrollHeight - log.scrollTop - log.clientHeight;
    pinned = fromEnd < 48;
    if (log.scrollTop < lastTop && fromEnd > 2) { toEnd = false; if (intent) upByVisitor = true; }
    if (fromEnd < 8) upByVisitor = false;
    lastTop = log.scrollTop;
  }, { passive: true });
  const scrollDown = (force = false) => requestAnimationFrame(() => { if (force || pinned) { log.scrollTop = log.scrollHeight; pinned = true; toEnd = true; upByVisitor = false; } });
  // A card taller than the log is shown from its top, so a plan is read from item 1 rather than from its button; a
  // list of chips that doesn't fit below the message that introduces it is shown from that message. A re-plan card
  // follows like a message: what it ends with, "Needs you", stays in view on a phone, and its first lines are a scroll up.
  const topIn = (n) => n.getBoundingClientRect().top - log.getBoundingClientRect().top + log.scrollTop; // position inside the log's content
  const push = (node, { from = node.classList.contains('card') && !node.classList.contains('replan') ? node : null } = {}) => {
    if (typingEl) { typingEl.remove(); typingEl = null; }
    log.appendChild(node);
    requestAnimationFrame(() => {
      if (from && from.isConnected && topIn(node) + node.offsetHeight - topIn(from) > log.clientHeight - 16) { log.scrollTop = topIn(from) - 8; pinned = false; toEnd = false; upByVisitor = false; }
      else { log.scrollTop = log.scrollHeight; pinned = true; toEnd = true; upByVisitor = false; }
    });
    return node;
  };
  new MutationObserver(() => scrollDown()).observe(log, { childList: true, subtree: true, characterData: true });
  // A number keeps its unit on its line ("12.400 mm", "194 µW", "7 am"): a no-break space between them. Words stay as
  // they are ("5 steps", "12 entries").
  const UNIT = /([0-9)]) (?=(?:µW|mW|kW|W|mm|cm|µm|nm|km|m|px|ms|s|min|h|°C|°F|°|%|l|ml|mrad|rad|dB|Hz|fps|am|pm)(?![\p{L}\d]))/gu;
  const nb = (s) => String(s).replace(UNIT, '$1\u00a0');
  const esc = (s) => nb(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
  // Closing a card disables its buttons; if one of them had keyboard focus, keep focus in the conversation.
  const closeCard = (node) => { const had = node.contains(document.activeElement); node.classList.add('done'); node.querySelectorAll('button').forEach((b) => (b.disabled = true)); if (had) log.focus({ preventScroll: true }); };

  // Register an interactive card; returns a promise settled by the card itself or by a skip.
  function interactive(node, skipValue) {
    return new Promise((res) => {
      const rec = { node, skipValue, settle: (v) => { pending = pending.filter((p) => p !== rec); closeCard(node); res(v); } };
      pending.push(rec);
      node._settle = rec.settle;
    });
  }

  const PLACEHOLDER = 'Or type what you want…';
  return {
    enableInput(on) { setInput(on); },
    // Read-only: the visitor scrolled the log up on purpose (and hasn't come back to its end or added a card since).
    userScrolledUp() { return upByVisitor; },
    // Live mode hooks (js/engine/livechat.js): the log's own helpers, so live cards scroll and escape like the rest.
    mount(node, opts) { return push(node, opts); },
    esc,
    // The LIVE AI badge (CSS, .chat-input.live::before), the live placeholder and the length cap. It leaves the
    // input's on/off alone (enableInput does that).
    live(on, { placeholder, max = 400 } = {}) {
      form.classList.toggle('live', !!on);
      if (on) { input.maxLength = max; input.placeholder = placeholder || PLACEHOLDER; }
      else { input.removeAttribute('maxlength'); input.placeholder = PLACEHOLDER; }
    },
    typing(on) { if (on) { if (!typingEl) typingEl = push(el('<div class="msg ncb typing" aria-hidden="true"><span></span><span></span><span></span></div>')); } else if (typingEl) { typingEl.remove(); typingEl = null; } },
    ncb(text) { push(el(`<div class="msg ncb">${esc(text)}</div>`)); },
    user(text) { push(el(`<div class="msg me">${esc(text)}</div>`)); },
    // An alert is red (role="alert"). A title starting with 📝 is a calm note instead: neutral border and title,
    // role="status" (e.g. a device flagged at night that nothing depends on, prompt.genericTitle in whatif.js).
    alert(text, title = '⚠ Something changed') {
      const note = /^📝/u.test(String(title));
      push(el(`<div class="msg ncb alert${note ? ' note' : ''}" role="${note ? 'status' : 'alert'}"><b>${esc(title)}</b>${esc(text)}</div>`));
    },
    resolvePending() { for (const p of [...pending]) p.settle(p.skipValue()); },
    // Live mode took over mid-request (the player's halt): each open card closes as stopped. Nothing of it ran, so a plan
    // reads "Stopped · nothing ran", never "Skipped ahead · ran as shown", and an ask shows no answer.
    stopPending() { for (const p of [...pending]) { p.node.classList.add('stopped'); p.settle(p.skipValue()); } },

    button(label) {
      const node = push(el(`<div class="card action"><button class="btn primary">${esc(label)}</button></div>`));
      const done = interactive(node, () => undefined);
      node.querySelector('button').onclick = () => node._settle();
      return done;
    },
    qr() {
      const node = push(el(`<div class="card qr"><div class="qr-code" aria-hidden="true"></div><div><b>Scan to pair</b><p>Point your phone at the code on the hub.</p><button class="btn primary">Scan QR</button></div></div>`));
      const q = node.querySelector('.qr-code'); for (let i = 0; i < 49; i++) { const c = document.createElement('i'); if (Math.random() < 0.45 || i < 3 || i % 7 === 0) c.className = 'on'; q.appendChild(c); }
      const done = interactive(node, () => undefined);
      node.querySelector('button').onclick = () => node._settle();
      return done;
    },
    chips(list, onPick, { heading } = {}) {
      if (heading) this.ncb(heading);
      const intro = log.lastElementChild?.classList.contains('ncb') ? log.lastElementChild : null; // what NCB just said about these chips
      const node = push(el(`<div class="chips"></div>`), { from: intro });
      for (const p of list) { const b = el(`<button class="chip">${esc(p.chip)}</button>`); b.onclick = () => { if (!node.isConnected) return; if (node.contains(document.activeElement)) log.focus({ preventScroll: true }); node.remove(); onPick(p); }; node.appendChild(b); } // one pick per list
      setInput(true); if (finePointer) input.focus({ preventScroll: true });
      return node;
    },
    // skipped: the visitor fast-forwarded past this card, so it is shown and settled at once, labelled as skipped.
    // "Not this" settles { approved: false }: the player stops the request there and nothing in the plan runs.
    plan(spec, { skipped = false } = {}) {
      const alts = {}; let clicked = false, declined = false;
      const node = push(el(`<div class="card plan"><p class="intro">${esc(spec.intro || 'Here\'s the plan. Nothing runs until you approve it.')}</p><ol></ol><div class="row"><button class="btn approve">${esc(spec.approve || 'Approve')}</button><button class="btn decline" type="button">Not this</button></div></div>`));
      const ol = node.querySelector('ol');
      spec.steps.forEach((s, i) => {
        const li = el(`<li><span class="txt">${esc(s.text)}</span>${s.alt ? `<button class="link">Edit</button>` : ''}</li>`);
        if (s.alt) li.querySelector('.link').onclick = (e) => { alts[i] = !alts[i]; li.querySelector('.txt').textContent = nb(alts[i] ? s.alt.text : s.text); e.target.textContent = alts[i] ? 'Undo' : 'Edit'; };
        ol.appendChild(li);
      });
      const approveBtn = node.querySelector('.approve'), declineBtn = node.querySelector('.decline');
      const done = interactive(node, () => ({ approved: true, alts: { ...alts } }));
      approveBtn.onclick = () => { clicked = true; node._settle({ approved: true, alts: { ...alts } }); };
      declineBtn.onclick = () => { declined = true; node._settle({ approved: false, alts: {} }); };
      if (skipped) node._settle({ approved: true, alts: {} });
      return done.then((v) => {
        if (node.classList.contains('stopped')) { declineBtn.remove(); approveBtn.textContent = 'Stopped · nothing ran'; return v; }
        if (declined) { node.classList.add('declined'); declineBtn.textContent = 'Not run ✕'; declineBtn.classList.add('chosen'); return v; }
        declineBtn.remove();
        approveBtn.textContent = clicked ? 'Approved ✓' : 'Skipped ahead · ran as shown'; if (!clicked) node.classList.add('skipped');
        return v;
      });
    },
    // Options are neutral unless the scene marks the safe default { primary: true }: only that one is highlighted,
    // and a beat skip picks it (otherwise the first option).
    ask(spec, { skipped = false } = {}) {
      const node = push(el(`<div class="card plan halt"><p class="intro">${esc(spec.intro)}</p><div class="row col"></div></div>`));
      const row = node.querySelector('.row');
      const def = Math.max(0, spec.options.findIndex((o) => o.primary));
      const done = interactive(node, () => def);
      spec.options.forEach((o, i) => { const b = el(`<button class="btn ${o.primary ? 'approve' : ''}">${esc(o.label)}</button>`); b.onclick = () => { b.classList.add('chosen'); b.textContent += ' ✓'; node._settle(i); }; row.appendChild(b); });
      if (skipped) node._settle(def);
      return done.then((i) => { if (node.classList.contains('stopped')) return i; const b = row.children[i]; if (b) { b.classList.add('chosen'); if (!b.textContent.includes('✓')) b.textContent += ' ✓ (skipped ahead)'; } return i; }); // no ✓ yet = not clicked
    },
    replan(spec) {
      push(el(`<div class="card replan"><p class="intro">${esc(spec.intro)}</p><ul>${(spec.changes || []).map((c) => `<li>${esc(c)}</li>`).join('')}</ul>${spec.needsYou ? `<p class="needs"><b>Needs you:</b> ${esc(spec.needsYou)}</p>` : ''}</div>`));
    },
    // End card. `onMore` (optional) adds "Another request here", which the host uses to re-offer prompts. `onWhatIf`
    // (optional) adds the what-if button first, labelled `whatIfLabel` ("What if something fails?" on a success card,
    // "Try another failure" on a what-if's): the host opens the picker of failures (failures() below). Pressed from the
    // keyboard (Enter or Space), focus moves to that picker's first choice; a mouse or touch press leaves focus alone.
    end(spec, { onMore, onWhatIf, whatIfLabel, onLive, liveLabel } = {}) {
      setInput(false);
      const node = push(el(`<div class="card end"><h3>${esc(spec.headline)}</h3><p>${esc(spec.body)}</p><div class="row">${onLive ? `<button class="btn primary live-go" type="button">${esc(liveLabel)}</button>` : ''}${onWhatIf ? `<button class="btn whatif" type="button">${icon('whatif')}<span>${esc(whatIfLabel || 'What if something fails?')}</span></button>` : ''}${onMore ? '<button class="btn more" type="button">Another request here</button>' : ''}<a class="btn" href="#">Try another scene</a></div><div class="follow"><span>Follow<span class="long"> NeuCharBox</span></span>${socialLinks({ labels: true })}</div></div>`));
      if (onLive) { const b = node.querySelector('.live-go'); b.onclick = () => { b.disabled = true; onLive(); }; }
      if (onMore) { const b = node.querySelector('.more'); b.onclick = () => { b.disabled = true; onMore(); }; }
      if (onWhatIf) {
        const b = node.querySelector('.whatif');
        b.onclick = (e) => {
          // A keyboard press: the button has focus and the click has no pointer behind it (detail 0) or shows the ring.
          const had = document.activeElement === b, keyboard = had && (e.detail === 0 || b.matches(':focus-visible'));
          b.disabled = true; if (had) log.focus({ preventScroll: true });
          onWhatIf();
          const first = keyboard && picker && !picker.classList.contains('done') ? picker.querySelector('.pick') : null;
          if (first) first.focus({ preventScroll: true });
        };
      }
      return node;
    },
    // The picker of failures: { heading, devices: [{ key, icon, name, hint? }], specials: [{ key, label }],
    // devicesLabel?, specialsLabel? }. Tiles like the dashboard's (icon + name, a small grey hint), in a grid; the
    // specials follow under "Or". One pick per card: the picked tile stays marked, the rest go inert, onPick(key).
    failures({ heading, devices = [], specials = [], devicesLabel = 'A device stops working', specialsLabel = 'Or' }, onPick) {
      const tile = (o, cls) => `<button class="pick ${cls}" type="button" data-key="${esc(o.key)}"><span class="ico">${icon(o.icon || 'whatif')}</span><span class="txt"><b>${esc(o.name || o.label)}</b>${o.hint ? `<span class="hint">${esc(o.hint)}</span>` : ''}</span></button>`;
      const group = (label, list, cls, aria = label) => (list.length ? `<p class="sec">${esc(label)}</p><div class="tiles ${cls}" role="group" aria-label="${esc(aria)}">${list.map((o) => tile(o, cls)).join('')}</div>` : '');
      const node = push(el(`<div class="card failures"><p class="intro">${esc(heading || '')}</p>${group(devicesLabel, devices, 'device')}${group(specialsLabel, specials, 'special', 'Something else goes wrong')}</div>`));
      node.querySelectorAll('.pick').forEach((b) => { b.onclick = () => { if (node.classList.contains('done')) return; b.classList.add('chosen'); b.setAttribute('aria-pressed', 'true'); closeCard(node); onPick && onPick(b.dataset.key); }; });
      picker = node;
      return node;
    },
    // Close any open picker (the visitor moved on), and the what-if / "Another request here" buttons of past end cards;
    // take away any list of chips (a request started).
    closeChips() { log.querySelectorAll('.chips').forEach((n) => { if (n.contains(document.activeElement)) log.focus({ preventScroll: true }); n.remove(); }); },
    closeFailures() { log.querySelectorAll('.card.failures:not(.done)').forEach((n) => closeCard(n)); },
    closeEnds() { log.querySelectorAll('.card.end button').forEach((b) => { if (b === document.activeElement) log.focus({ preventScroll: true }); b.disabled = true; }); },
  };
}
