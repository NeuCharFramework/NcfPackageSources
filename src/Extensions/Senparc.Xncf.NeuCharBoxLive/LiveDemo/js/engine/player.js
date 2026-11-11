// Runs a scene's authored steps against the store and the chat.
// Step shapes:
//   { beat: 'setup'|'ask'|'plan'|'run'|'recover'|'end' }
//   { say: text }                       NCB message
//   { user: text }                      visitor message
//   { status: text | (store) => text }  small progress line under the room
//   { plan: { intro, steps: [{ text, alt: { text, apply: [steps] } }], approve } }  waits for approval; "Not this"
//                                       ends the request there (play() resolves { declined: true })
//   { ask: { intro, options: [{ label, apply: [steps], primary? }] } }   halts and asks; `primary` marks the safe
//                                       default: the only highlighted option, and what a beat skip picks
//   { wait: ms }
//   { set: path, to: value, label? }
//   { tween: path, to: number, ms, label? }
//   { parallel: [steps] }
//   { fail: deviceId, say?: text, title?: text, faultText?: text }  marks the device faulted; `say` shows as an alert.
//                                       `faultText` first writes <id>.faultNote, then <id>.status = 'fault' (the host's
//                                       status line, red ping and dashboard tile show faultNote || the device's
//                                       faultText). A fail without faultText leaves an earlier faultNote as it is.
//   { failPoint: name }                 no-op marker: where a what-if replay stops (js/engine/whatif.js). Top level of
//                                       a prompt's steps only (test/run.mjs enforces it).
//   { replan: { intro, changes: [text], needsYou: text } }
//   { end: { headline, body } }
//   { fn: (ctx) => any | Promise }
//
// fn steps receive ctx = { store, chat, fast, tween, sleep, status, say }. `fast` is a live getter (true while
// skipping, headless or quiet). Use ctx.tween(path, to, ms) and ctx.sleep(ms) inside fn steps: they honour ?speed=, go
// instant when skipping, and are released immediately by a beat skip — raw store.tween/setTimeout do not.
// ctx.status(text) sets the status line; ctx.say(text) is a { say } step (typing delay, cancel-safe). ctx.chat is the
// chat (a silent stand-in in a quiet play); plan/ask cards opened from an fn are recorded and replayed like steps.
//
// play(steps, { quiet, replay, until, carry }) runs steps in order (one play at a time) and resolves
// { declined, choices, stopped, unused }:
//   choices  every plan answer ({ type: 'plan', approved, alts }) and ask answer ({ type: 'ask', index }) in the order
//            they came up: asks inside ask options and plan alt applies, plans/asks in parallel branches, and cards an
//            fn step opens through ctx.chat. Each entry remembers (non-enumerably) the card it answered and its words.
//   replay   a choices list to answer from instead of showing cards or using defaults. Each plan or ask takes the first
//            unused entry that answered the same card: the same spec object, else a card with the same words (an fn
//            step that builds its card anew each run), else, for a card an fn step opens, the next unused entry an fn
//            step's card recorded. A hand-written entry (a bare index, or { approved, alts }) answers the next plan/ask
//            of its kind. None left: the default below.
//   unused   the replay entries this play didn't consume, in order (a later play can continue the replay with them).
//   quiet    fast and silent: ctx.chat is a silent stand-in (no message, typing dots, alert, card, chips or end card of
//            any kind; button/qr resolve at once; plan/ask come from `replay`, else approved as shown / the primary
//            option), no status line (status steps, strings or functions, labels and ctx.status write nothing), and
//            no onBeat/onPlan calls. Store writes happen as usual.
//   until    (step) => boolean, tested on every step before it runs, nested ones too (parallel branches, plan alt
//            applies, ask options): the first step it is true for stops the play there, without running it. Nothing
//            after it runs (a parallel branch already running finishes its current step), and the play resolves
//            { stopped: true }. What-if replays stop at a top-level { failPoint } (test/run.mjs keeps them there).
//   carry    this play continues the request of the previous one: a beat skip in progress carries on into it. By
//            default each play() starts at normal speed.
// halt() stops every play, the one running and any started before resume(), where it is: no further step runs, nothing
// more is said or shown (an fn step still finishing gets the silent chat, its plans declined), and the card waiting for
// an answer closes as stopped (chat.stopPending). flow.stop() uses it when live mode takes over mid-request.
// Headless (tests): cards never show; autoPlan(spec) gives a plan's alts (or false: "Not this"), autoAsk(spec) an index.

// A card's words: what makes an fn step's card, built anew each run, the same card as last time.
const wordsOf = (kind, spec) => [kind, spec?.intro ?? '', ...(kind === 'ask' ? (spec?.options || []).map((o) => o.label) : (spec?.steps || []).map((s) => s.text))].join('␞');

export function createPlayer({ store, chat, headless = false, onBeat = () => {}, onStatus = () => {}, onPlan = () => {}, autoPlan = () => ({}), autoAsk = () => 0, endOptions = () => ({}), speed = null }) {
  let fast = headless;       // skipping (or cancelled, or headless)
  let quiet = false;         // the current play is quiet (see the header)
  let stopAt = null;         // beat name to stop fast-forwarding at
  let currentBeat = 'setup';
  let cancelled = false;
  let halted = false;        // halt(): live mode took over; nothing more runs, nothing more is said, until resume()
  let declined = false;      // the visitor answered a plan with "Not this": the rest of the request doesn't run
  let until = null, stopped = false; // the current play's `until`, and whether it has matched (nothing more runs)
  let rec = { choices: [], replay: [] }; // the current play's answers, and the replay entries it may use

  const q = typeof location !== 'undefined' ? Number(new URLSearchParams(location.search).get('speed')) : NaN;
  const SPEED = speed || (Number.isFinite(q) && q > 0 ? Math.min(q, 6) : 1);
  const isFast = () => fast || quiet;
  const timers = new Map();  // timeout id -> resolve, so a skip or cancel can release in-flight sleeps
  const sleep = (ms) => (isFast() || ms <= 0 ? Promise.resolve() : new Promise((r) => { const id = setTimeout(() => { timers.delete(id); r(); }, ms / SPEED); timers.set(id, r); }));
  const releaseSleeps = () => { for (const [id, r] of timers) { clearTimeout(id); r(); } timers.clear(); };
  const tween = (path, to, ms = 800) => (isFast() || ms <= 0 ? (store.set(path, to), Promise.resolve()) : store.tween(path, to, ms / SPEED));
  const typing = (text) => Math.min(1800, 350 + text.length * 14);

  // ---- plan and ask answers: replayed, headless defaults, quiet defaults, or the visitor's card ----
  const kindOf = (e) => (typeof e === 'number' ? 'ask' : e && (e.type || ('index' in e ? 'ask' : 'plan')));
  const valueOf = (e, kind) => (kind === 'ask' ? (typeof e === 'number' ? e : e.index) : { approved: e.approved !== false, alts: { ...(e.alts || {}) } });
  const primaryOf = (spec) => Math.max(0, (spec.options || []).findIndex((o) => o.primary));
  const entry = (kind, spec, v, via) => {
    const e = kind === 'plan' ? { type: 'plan', approved: !(v && v.approved === false), alts: { ...((v && v.alts) || {}) } } : { type: 'ask', index: Number(v) || 0 };
    Object.defineProperties(e, { step: { value: spec, enumerable: false }, words: { value: wordsOf(kind, spec), enumerable: false }, via: { value: via, enumerable: false } });
    return e;
  };
  // via: 'step' (a { plan } / { ask } step) or 'fn' (a card an fn step opened through ctx.chat).
  async function answer(kind, spec, opts = {}, via = 'step') {
    const left = rec.replay.filter((r) => !r.used && r.e != null && kindOf(r.e) === kind), words = wordsOf(kind, spec);
    const hit = left.find((r) => r.e.step === spec) // the same card
      || left.find((r) => r.e.step != null && r.e.words === words) // the same words: an fn step built its card again
      || left.find((r) => r.e.step == null || (via === 'fn' && r.e.via === 'fn')); // hand-written; or the next card an fn opened
    if (hit) { hit.used = true; rec.choices.push(hit.e); return valueOf(hit.e, kind); }
    let v;
    if (headless) { const a = kind === 'plan' ? autoPlan(spec) : null; v = kind === 'plan' ? (a === false ? { approved: false, alts: {} } : { approved: true, alts: a || {} }) : autoAsk(spec) || 0; }
    else if (halted) v = kind === 'plan' ? { approved: false, alts: {} } : primaryOf(spec); // an fn step still finishing after halt(): no card, nothing approved
    else if (quiet) v = kind === 'plan' ? { approved: true, alts: {} } : primaryOf(spec);
    else { if (kind === 'plan') onPlan(spec); v = await chat[kind](spec, { skipped: fast, ...opts }); } // the host can say "waiting for your approval" if the scene didn't
    const e = entry(kind, spec, v, via); rec.choices.push(e);
    return kind === 'plan' ? v || { approved: true, alts: {} } : e.index;
  }

  // What steps and fn code talk to: the chat itself (plan/ask recorded), or in a quiet play a silent stand-in.
  const loud = new Proxy(chat, { get(t, k) { if (k === 'plan' || k === 'ask') return (spec, o) => answer(k, spec, o, 'fn'); const v = t[k]; return typeof v === 'function' ? v.bind(t) : v; } });
  const silent = new Proxy({}, { get(t, k) {
    if (k === 'then') return undefined; // never a thenable
    if (k === 'plan' || k === 'ask') return (spec) => answer(k, spec, {}, 'fn');
    if (k === 'button' || k === 'qr') return () => Promise.resolve();
    return () => undefined;
  } });
  const out = () => (quiet || halted ? silent : loud);

  const say = async (text) => { if (!isFast()) { chat.typing(true); await sleep(typing(text)); chat.typing(false); } if (cancelled || halted) return; out().ncb(text); await sleep(400); };
  const status = (text) => { if (!quiet && !halted) onStatus(text); };
  const ctx = { store, get chat() { return out(); }, tween, sleep, say, status, get fast() { return isFast(); } };

  async function run(step) {
    if (cancelled || halted || declined || stopped) return;
    if (until && until(step)) { stopped = true; return; } // the play stops here (see the header)
    if (step.failPoint != null) return; // a marker for what-if replays; nothing happens here
    if (step.beat) {
      currentBeat = step.beat;
      if (stopAt && stopAt === step.beat) { fast = headless; stopAt = null; }
      if (!quiet) onBeat(step.beat);
      return;
    }
    if (step.fail) {
      if (step.faultText != null) store.set(`${step.fail}.faultNote`, step.faultText); // before the fault: the host reads it
      store.set(`${step.fail}.status`, 'fault');
      if (step.say) out().alert(step.say, step.title);
      await sleep(600); return;
    }
    if (step.say != null) return say(step.say);
    if (step.user != null) { out().user(step.user); await sleep(500); return; }
    if (step.status != null) { if (!quiet) onStatus(typeof step.status === 'function' ? step.status(store) : step.status); return; }
    if (step.wait != null) return sleep(step.wait);
    if (step.set != null) { store.set(step.set, step.to); if (step.label) status(step.label); return; }
    if (step.tween != null) { if (step.label) status(step.label); return tween(step.tween, step.to, step.ms ?? 800); }
    if (step.parallel) { const rs = await Promise.allSettled(step.parallel.map(run)); const bad = rs.find((r) => r.status === 'rejected'); if (bad) throw bad.reason; return; }
    if (step.replan) { out().replan(step.replan); await sleep(700); return; }
    if (step.plan) {
      const choices = await answer('plan', step.plan);
      if (choices && choices.approved === false) { declined = true; return; }
      for (const [i, on] of Object.entries(choices.alts || {})) {
        const alt = step.plan.steps[Number(i)]?.alt;
        if (on && alt?.apply) for (const s of alt.apply) await run(s);
      }
      return;
    }
    if (step.ask) {
      const idx = await answer('ask', step.ask);
      const opt = step.ask.options[idx] || step.ask.options[0];
      for (const s of opt.apply || []) await run(s);
      return;
    }
    if (step.end) {
      currentBeat = 'end';
      if (stopAt === 'end') { fast = headless; stopAt = null; } // a skip to Done ends here
      if (!quiet) { chat.end(step.end, endOptions(step.end)); onBeat('end'); }
      return;
    }
    if (step.fn) return step.fn(ctx);
    throw new Error('Unknown step: ' + JSON.stringify(step));
  }

  async function play(steps, { quiet: silentRun = false, replay = null, until: stopWhen = null, carry = false } = {}) {
    // Each play() starts at normal speed: a skip (or an error during one) never carries into the next request,
    // unless this play continues the same request (carry).
    if (!carry) { fast = headless; stopAt = null; }
    declined = false;
    const mine = { choices: [], replay: (replay || []).map((e) => ({ e, used: false })) };
    const outer = { rec, quiet, until, stopped };
    rec = mine; quiet = !!silentRun; until = stopWhen; stopped = false;
    let hit = false;
    try {
      for (const s of steps) {
        if (cancelled || halted || declined || stopped) break;
        await run(s);
      }
    } finally { hit = stopped; ({ rec, quiet, until, stopped } = outer); }
    return { declined, choices: mine.choices, stopped: hit, unused: mine.replay.filter((r) => !r.used).map((r) => r.e) };
  }

  // Fast-forward until the named beat is reached (or the end card, for 'end'). Interactive cards on screen resolve
  // with the visitor's current choices; in-flight sleeps and tweens finish immediately.
  function skipTo(beat) { stopAt = beat; fast = true; store.finishTweens(); releaseSleeps(); chat.resolvePending(); }
  // cancel (teardown): also settles in-flight store tweens, which only finish on frames that no longer come.
  function cancel() { cancelled = true; fast = true; store.finishTweens(); releaseSleeps(); chat.resolvePending(); }
  // halt (live mode takes the conversation mid-request, flow.stop()): like cancel, but it lasts only until resume(). The
  // card waiting for an answer closes as stopped (nothing of it ran), not as skipped ahead.
  function halt() { halted = true; fast = true; store.finishTweens(); releaseSleeps(); if (chat.stopPending) chat.stopPending(); else chat.resolvePending(); }
  function resume() { halted = false; fast = headless; stopAt = null; }

  return { play, run, skipTo, cancel, halt, resume, get halted() { return halted; }, get beat() { return currentBeat; }, get fast() { return isFast(); }, get quiet() { return quiet; } };
}

// The setup beat is generic: power the hub, scan, discover devices in order.
export function setupSteps(scene) {
  const ids = scene.deviceOrder || Object.keys(scene.devices).filter((d) => d !== 'hub' && d !== 'env');
  const steps = [
    { beat: 'setup' },
    { say: scene.setupIntro || 'This is NeuCharBox. Plug it in to start.' },
    { fn: ({ chat, fast }) => (fast ? null : chat.button('Plug in')) },
    { set: 'hub.status', to: 'on', label: 'Hub powered' },
    { tween: 'hub.led', to: 1, ms: 900 },
    { say: 'Now scan the QR code on the hub with your phone and I\'ll find what\'s on this network.' },
    { fn: ({ chat, fast }) => (fast ? null : chat.qr()) },
    { say: 'Looking for devices…' },
  ];
  for (const id of ids) {
    const d = scene.devices[id];
    steps.push({ set: `${id}.status`, to: 'online', label: `Found ${d.name}` }, { wait: 1100 });
  }
  steps.push({ say: `${ids.length} devices connected. No coding, no setup — that's it.` }, { status: `${ids.length} devices connected · waiting for your request` }, { beat: 'ask' });
  return steps;
}
