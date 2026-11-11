// Live AI demo: the chat cards (the real LiveView that js/engine/live.js is written against; see the plan's "Shared
// interfaces"). The plan card turns into a checklist in place: steps with their calls as pills, calls without a step as
// rows of their own, and a timeline for what happens later. Everything a visitor or the model wrote is inserted as text.
import { LIVE_COPY } from './live.js';
import { socialLinks } from './social.js';
import { icon } from './icons.js';
import { devicesOf } from './whatif.js';

const GLYPH = { running: '●', done: '✓', failed: '✕', skipped: '–', pending: '○' };
const STATUS_WORD = { running: 'Running', done: 'Done', failed: 'Failed', skipped: 'Skipped', pending: 'Not started' };
const INTERESTS = [['home', 'Home'], ['lab', 'Lab'], ['warehouse', 'Warehouse'], ['business', 'Business'], ['other', 'Something else']];

const parse = (html) => { const t = document.createElement('template'); t.innerHTML = html.trim(); return t.content.firstElementChild; };
const mk = (tag, cls, text) => { const n = document.createElement(tag); if (cls) n.className = cls; if (text != null) n.textContent = text; return n; };

export function createLiveView({ chat, scene }) {
  const { esc } = chat;
  const devices = devicesOf(scene), n = devices.length;
  let planningEl = null, sayEl = null, picker = null;

  const mount = (node, opts) => chat.mount(node, opts);

  // ---- steps: the plan card's list and the checklist's, one structure ----
  function stepList(ol) {
    const rows = new Map();
    const setDevices = (li, s) => { li.dataset.devices = (s.devices || []).join(' '); };
    const api = {
      rows,
      add(s, glyph) {
        const li = mk('li', 'lstep'); li.dataset.id = s.id; li.dataset.status = 'pending';
        const g = mk('span', 'glyph', glyph); g.setAttribute('aria-hidden', 'true');
        const txt = mk('span', 'txt'); txt.innerHTML = esc(s.text);
        li.append(g, txt, mk('span', 'sr', ''), mk('div', 'calls'));
        setDevices(li, s); ol.appendChild(li); rows.set(s.id, li); return li;
      },
      // A known id gets the new text and devices in place (its status and pills stay); a new id is appended as not started.
      merge(steps) {
        for (const s of steps) {
          const li = rows.get(s.id);
          if (li) { li.querySelector('.txt').innerHTML = esc(s.text); setDevices(li, s); } else api.add(s, GLYPH.pending);
        }
      },
      status(id, status) {
        const li = rows.get(id); if (!li || !(status in GLYPH)) return;
        li.dataset.status = status; li.querySelector('.glyph').textContent = GLYPH[status];
        li.querySelector('.sr').textContent = ` (${STATUS_WORD[status]})`;
      },
      toChecklist() {
        for (const li of rows.values()) { li.querySelector('.glyph').textContent = GLYPH.pending; li.querySelector('.calls').textContent = ''; }
      },
    };
    return api;
  }

  // ---- the checklist: steps (+ pills), stepless rows, timeline ----
  function makeChecklist(node, list) {
    let loose = null, tl = null;
    const pills = new Map(); // callId → { el, label }
    return {
      mergeSteps: (steps) => list.merge(steps),
      step: (id, status) => list.status(id, status),
      call({ callId, step, label }) {
        if (pills.has(callId)) return;
        const text = `${label} · …`;
        const row = step != null ? list.rows.get(step) : null;
        let p;
        if (row) { p = mk('span', 'call'); row.querySelector('.calls').appendChild(p); }
        else { // a read or safe-state call made without a step: its own row, after the steps
          if (!loose) { loose = mk('ul', 'lloose'); node.insertBefore(loose, tl); }
          p = mk('li', 'call loose'); loose.appendChild(p);
        }
        p.dataset.call = callId; p.dataset.outcome = ''; p.innerHTML = esc(text);
        pills.set(callId, { el: p, label });
      },
      result({ callId, outcome, text, ok }) {
        const p = pills.get(callId); if (!p) return;
        p.el.dataset.outcome = outcome ?? ''; p.el.classList.toggle('bad', ok === false);
        p.el.innerHTML = esc(text ? `${p.label} · ${text}` : p.label);
      },
      timeline({ at, text, ok, kind, origin }) {
        if (!tl) { tl = mk('div', 'ltimeline'); node.appendChild(tl); }
        const stick = tl.scrollHeight - tl.scrollTop - tl.clientHeight < 8; // follow new rows only from the bottom
        const row = mk('div', 'trow'); row.dataset.kind = kind ?? ''; row.dataset.ok = String(ok !== false);
        const when = at ? `${at} · ` : '';
        const body = origin === 'held' ? `${LIVE_COPY.ruleHeld} · ${text}` : kind === 'notify' ? `📩 ${text}` : kind === 'fault' ? text : `${text}${ok === false ? ' ✕' : ' ✓'}`;
        row.innerHTML = esc(when + body);
        tl.appendChild(row);
        if (stick) tl.scrollTop = tl.scrollHeight;
      },
      done() { node.classList.add('done'); },
    };
  }

  const title = (replay) => `<span class="ttl">${esc(replay ? LIVE_COPY.replayTitle : LIVE_COPY.checklistTitle)}</span> <span class="live-badge">${esc(LIVE_COPY.badge)}</span>`;

  // The plan (or re-plan) card, waiting for Approve / Not this. toChecklist() turns the same card into the checklist.
  function planCard(spec, { onApprove, onDecline }) {
    const replan = spec.kind === 'replan';
    const node = parse(`<div class="card live-plan${replan ? ' replan' : ''}"><p class="intro"><span class="itxt">${esc(replan ? LIVE_COPY.replanIntro(spec.reason) : LIVE_COPY.planIntro)}</span> <span class="live-badge">${esc(LIVE_COPY.badge)}</span></p><ol class="lsteps"></ol><div class="row"><button class="btn approve" type="button" disabled>Approve</button><button class="btn decline" type="button" disabled>Not this</button></div></div>`);
    const list = stepList(node.querySelector('ol'));
    (spec.steps || []).forEach((s, i) => list.add(s, `${i + 1}.`));
    const approve = node.querySelector('.approve'), decline = node.querySelector('.decline');
    let shut = false;
    const keepFocus = () => { if (node.contains(document.activeElement)) node.closest('.chat-log')?.focus({ preventScroll: true }); };
    const both = (off) => { if (off) keepFocus(); approve.disabled = off; decline.disabled = off; };
    approve.onclick = () => { if (!approve.disabled) onApprove(); };
    decline.onclick = () => { if (!decline.disabled) onDecline(); };
    mount(node);
    return {
      // Approve / Not this go live: scroll the log just far enough that they are in view (the input bar coming back
      // shrinks the log; a plan taller than the log would otherwise leave them a few pixels below its edge).
      enable() {
        if (shut) return;
        both(false);
        const follow = !chat.userScrolledUp?.(); // a visitor who scrolled up (reading an error) is left where they are
        const reveal = () => {
          const lg = node.closest('.chat-log'); if (!lg || shut || !follow) return;
          const over = approve.getBoundingClientRect().bottom - lg.getBoundingClientRect().bottom + 8;
          if (over > 0) lg.scrollTop += over;
        };
        reveal(); requestAnimationFrame(reveal);
      },
      busy() { both(true); },
      declined() { shut = true; both(true); node.classList.add('declined'); decline.classList.add('chosen'); },
      close() { shut = true; both(true); node.classList.add('done'); },
      toChecklist({ replay = false } = {}) {
        shut = true; keepFocus(); node.classList.add('running');
        node.querySelector('.intro').innerHTML = title(replay);
        node.querySelector('.row').remove();
        list.toChecklist();
        return makeChecklist(node, list);
      },
    };
  }

  function checklist(steps, { replay = false } = {}) {
    sayEl = null;
    const node = parse(`<div class="card live-plan running"><p class="intro">${title(replay)}</p><ol class="lsteps"></ol></div>`);
    const list = stepList(node.querySelector('ol'));
    for (const s of steps || []) list.add(s, GLYPH.pending);
    mount(node);
    return makeChecklist(node, list);
  }

  // ---- the end card ----
  function endCard({ remainingToday, whatIfLabel, whatIf }, { onAnother, onWhatIf, onReset }) {
    sayEl = null;
    const node = parse(`<div class="card end live">${remainingToday != null ? `<p class="left">${esc(LIVE_COPY.left(remainingToday))}</p>` : ''}<div class="row"><button class="btn primary another" type="button">${esc(LIVE_COPY.another)}</button>${whatIf ? `<button class="btn whatif" type="button">${icon('whatif')}<span>${esc(whatIfLabel)}</span></button>` : ''}<button class="btn reset" type="button">${esc(LIVE_COPY.reset)}</button><a class="btn" href="#">${esc(LIVE_COPY.otherScene)}</a></div><div class="follow"><span>Follow<span class="long"> NeuCharBox</span></span>${socialLinks({ labels: true })}</div></div>`);
    const buttons = [...node.querySelectorAll('button')];
    const log = () => node.closest('.chat-log');
    const press = (b, act, e) => {
      const had = document.activeElement === b, keyboard = had && e && (e.detail === 0 || b.matches(':focus-visible'));
      buttons.forEach((x) => (x.disabled = true));
      if (had) log()?.focus({ preventScroll: true });
      act();
      return keyboard;
    };
    node.querySelector('.another').onclick = (e) => {
      press(e.currentTarget, onAnother, e);
      const input = log()?.parentElement?.querySelector('#chatText');
      if (input && !input.disabled) input.focus({ preventScroll: true });
    };
    node.querySelector('.reset').onclick = (e) => { press(e.currentTarget, onReset, e); };
    const w = node.querySelector('.whatif');
    if (w) w.onclick = (e) => {
      const keyboard = press(w, onWhatIf, e);
      const first = keyboard && picker && !picker.classList.contains('done') ? picker.querySelector('.pick') : null;
      if (first) first.focus({ preventScroll: true });
    };
    mount(node);
    // The next end card, a lost code or a fresh room ends what this card's buttons act on: none is left to press, and
    // nothing in the what-if picker it opened.
    return {
      close() {
        if (node.contains(document.activeElement)) log()?.focus({ preventScroll: true });
        buttons.forEach((x) => (x.disabled = true));
        chat.closeFailures();
      },
    };
  }

  // ---- access: invite code, application, or offline ----
  function access({ mode, note, backLabel, offlineText }, { onCode, onApply, onBack }) {
    const offline = mode === 'offline';
    const labelled = (text, control) => `<label class="fld"><span>${esc(text)}</span>${control}</label>`;
    const body = offline
      ? `<p class="intro">${esc(offlineText ?? LIVE_COPY.offline)}</p><div class="follow">${socialLinks({ labels: true })}</div>`
      : `<p class="intro">${esc(LIVE_COPY.accessIntro)}</p>${note ? `<p class="anote">${esc(note)}</p>` : ''}
         <form class="code" novalidate><input name="code" type="text" autocomplete="off" autocapitalize="characters" spellcheck="false" maxlength="40" aria-label="Invite code" placeholder="Invite code"><button class="btn primary go" type="submit">Enter</button></form>
         <button class="lnk apply-open" type="button" aria-expanded="false">Apply for access</button>
         <form class="apply" hidden>
           ${labelled('Email', '<input name="email" type="email" required maxlength="200" autocomplete="email" aria-describedby="apply-use">')}
           <p class="use" id="apply-use">${esc(LIVE_COPY.applyEmailUse)}</p>
           ${labelled('Name (optional)', '<input name="name" type="text" maxlength="100" autocomplete="name">')}
           ${labelled("I'm interested in", `<select name="interest">${INTERESTS.map(([v, t]) => `<option value="${v}">${esc(t)}</option>`).join('')}</select>`)}
           ${labelled('What would you try?', '<textarea name="wouldTry" rows="2" required maxlength="400"></textarea>')}
           <div class="hp" aria-hidden="true"><label>Website<input name="website" type="text" tabindex="-1" autocomplete="off"></label></div>
           <button class="btn primary send" type="submit">Send application</button>
         </form>`;
    let codeInput = null;
    const node = parse(`<div class="card access${offline ? ' offline' : ''}">${body}${offline ? '' : '<p class="err" role="alert" hidden></p>'}<button class="lnk back" type="button">${esc(backLabel ?? LIVE_COPY.back)}</button></div>`);
    node.querySelector('.back').onclick = () => onBack();
    if (!offline) {
      const err = node.querySelector('.err'), codeForm = node.querySelector('form.code'), applyForm = node.querySelector('form.apply');
      codeInput = codeForm.elements.code;
      const say = (t) => { err.textContent = t || ''; err.hidden = !t; };
      // One answer in flight per form; a rejected handler reads as offline.
      const run = async (form, fn) => {
        if (form.dataset.busy || node.classList.contains('done')) return;
        form.dataset.busy = '1'; form.querySelectorAll('button[type=submit]').forEach((b) => (b.disabled = true)); say('');
        let r = null;
        try { r = await fn(); } catch { r = LIVE_COPY.offline; }
        delete form.dataset.busy;
        if (node.classList.contains('done')) return;
        form.querySelectorAll('button[type=submit]').forEach((b) => (b.disabled = false));
        return r;
      };
      codeForm.onsubmit = async (e) => {
        e.preventDefault();
        const r = await run(codeForm, () => onCode(codeForm.elements.code.value));
        if (r) say(r); else codeInput.value = ''; // the code never stays on the page
      };
      node.querySelector('.apply-open').onclick = (e) => {
        applyForm.hidden = !applyForm.hidden; e.currentTarget.setAttribute('aria-expanded', String(!applyForm.hidden));
        if (!applyForm.hidden) applyForm.elements.email.focus({ preventScroll: true });
      };
      const pre = ['home', 'lab', 'warehouse'].includes(scene.id) ? scene.id : 'other';
      applyForm.elements.interest.value = pre;
      applyForm.onsubmit = async (e) => {
        e.preventDefault();
        if (!applyForm.reportValidity()) return;
        const f = applyForm.elements;
        const r = await run(applyForm, () => onApply({ email: f.email.value.trim(), name: f.name.value.trim(), interest: f.interest.value, wouldTry: f.wouldTry.value.trim(), website: f.website.value }));
        if (r === LIVE_COPY.applied) { const p = mk('p', 'applied', r); p.setAttribute('role', 'status'); applyForm.replaceWith(p); node.querySelector('.apply-open').hidden = true; say(''); }
        else if (r) say(r);
      };
    }
    mount(node);
    return {
      close() {
        node.classList.add('done');
        if (codeInput) codeInput.value = '';
        if (node.contains(document.activeElement)) node.closest('.chat-log')?.focus({ preventScroll: true });
        node.querySelectorAll('button, input, select, textarea').forEach((x) => (x.disabled = true));
      },
    };
  }

  return {
    user: (text) => { sayEl = null; chat.user(text); },
    ncb: (text) => chat.ncb(text),
    // A rule or schedule that fired (or was held back) outside a request: one status line in the chat.
    ruleLine({ at, text, kind, origin }) {
      const head = origin === 'held' ? LIVE_COPY.ruleHeld : kind === 'notify' ? '📩' : origin === 'schedule' ? LIVE_COPY.scheduled : LIVE_COPY.ruleFired;
      const node = parse(`<div class="msg ncb rule">${esc(`${head} · ${at} · ${text}`)}</div>`);
      sayEl = null; mount(node);
    },
    chips(texts, onPick, heading) {
      if (!texts.length) return;
      chat.chips(texts.map((chip) => ({ chip })), (p) => onPick(p.chip), { heading });
    },
    closeChips: () => chat.closeChips(),
    planning(on) {
      if (on) {
        sayEl = null;
        if (planningEl?.isConnected) return;
        planningEl = parse(`<div class="msg ncb planning"><span class="dots" aria-hidden="true"><span></span><span></span><span></span></span><span class="ptxt">${esc(LIVE_COPY.planning)}</span> <span class="live-badge">${esc(LIVE_COPY.badge)}</span></div>`);
        mount(planningEl);
      } else if (planningEl) { planningEl.remove(); planningEl = null; }
    },
    reading(ref) { const t = planningEl?.querySelector('.ptxt'); if (t) t.innerHTML = esc(LIVE_COPY.reading(ref)); },
    planCard,
    checklist,
    // The model's words for this request: deltas build one bubble, the summary replaces its text.
    sayDelta(delta) {
      if (!sayEl?.isConnected) { sayEl = parse('<div class="msg ncb live-say"></div>'); mount(sayEl); }
      sayEl.appendChild(document.createTextNode(delta));
    },
    summary(text) {
      if (!sayEl?.isConnected) { sayEl = parse('<div class="msg ncb live-say"></div>'); mount(sayEl); }
      sayEl.textContent = text;
    },
    endCard,
    failures(list, onPick) {
      picker = chat.failures({ heading: LIVE_COPY.whatIfHeading, devices: list }, onPick);
      return picker;
    },
    access,
    lost(onReconnect) {
      chat.ncb(LIVE_COPY.lost);
      chat.button(LIVE_COPY.reconnect).then(() => onReconnect());
    },
    input: (on) => chat.enableInput(on),
    live: (on) => chat.live(on, { placeholder: LIVE_COPY.placeholder(n) }),
  };
}
