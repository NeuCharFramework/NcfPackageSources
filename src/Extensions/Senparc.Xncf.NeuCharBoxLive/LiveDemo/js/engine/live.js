// Live AI demo: the controller (wire contract: docs/superpowers/specs/2026-11-07-live-api-contract.md).
// Access (invite code), session and room; then requests, the event stream (event → view / panel / store), what-ifs,
// reset and limits. The invite code goes in storage and request headers only, never in a URL, a log or the page.
import { createLiveApi, LiveError } from './liveapi.js';
import { openEventStream } from './sse.js';
import { devicesOf, refOf, REPLAYING } from './whatif.js';
import { FRESH_START } from './flow.js';
import { editsFor } from './controls.js';

const left = (n) => (n == null ? '' : n === 1 ? '1 live request left today' : `${n} live requests left today`);
export const LIVE_COPY = {
  button: '✨ Try your own request (live AI)',
  accessIntro: 'Live AI is invite-only. Enter your code, or apply.',
  offline: 'Live AI is offline right now. Try the guided requests, or follow NeuCharBox for updates.',
  // why a live button could not open live mode (whyOff below)
  backendDown: (base) => `NCF backend not found at ${base}. Start your NCF site with the NeuCharBox Live module, then press Try it live again.`,
  backendOff: 'Your NCF backend is running, but live AI is off there. Open NeuCharBox Live, Settings in its admin to see what is missing.',
  sceneOff: "Your NCF backend is running, but this scene isn't live there yet. Today live AI runs the Home scene and Build your own.",
  rate: 'Too many tries from here. Wait a minute and try again.',
  sessionsBusy: 'Live AI is busy right now. Try again in a few minutes.',
  badCode: "That code didn't work. Check it, or apply below.",
  savedCodeGone: 'Your saved code no longer works. Enter a new one, or apply below.',
  noneLeft: 'This code has no live requests left today. They reset at midnight US Eastern.',
  applyEmailUse: 'We use your email only to send your invite.',
  applied: 'Thanks. We read every application and send codes by email.',
  applyRate: 'Too many applications from this connection. Try again in an hour.',
  back: 'Back to guided requests',
  intro: (n) => `Live AI is on. Type what you want ${n === 1 ? 'this device' : `these ${n} devices`} to do. A real model plans it, and nothing runs until you approve.`,
  tryHeading: 'Try:',
  placeholder: (n) => `Type your own request for ${n === 1 ? 'this device' : `these ${n} devices`}…`,
  badge: 'LIVE AI',
  planning: 'Planning…',
  reading: (ref) => `Checking ${ref}…`,
  planIntro: "Here's the plan. Nothing runs until you approve.",
  replanIntro: (reason) => `I need to change the plan: ${String(reason ?? '').replace(/[\s.!?…]+$/, '')}.`,
  checklistTitle: 'Running your plan',
  replayTitle: 'Replaying your plan',
  declined: "OK, nothing ran. Everything is as it was. Type another request when you're ready.",
  expired: 'That plan waited too long, so I dropped it. Nothing ran.',
  limitCode: "That's all the live requests for this code today. They reset at midnight US Eastern. The guided requests still work.",
  limitGlobal: 'Live AI has reached its limit for today. The guided requests still work, or come back tomorrow.',
  limitRun: "I stopped there: this request reached the live demo's limit for one run. What I did so far is above.",
  tooLong: 'That request is over 400 characters. Try a shorter one.',
  error: 'Something went wrong on the live server, not with a device. Try again.',
  badRoom: "This room couldn't be set up. Go back to building and try again.",
  lost: 'I lost the connection to the live AI. The room shows the last thing I heard.',
  reconnect: 'Reconnect',
  timedOut: 'Your live session timed out, so I started a fresh room.',
  whatIfHeading: "Pick what goes wrong. I'll replay this request live, with that device failing.",
  another: 'Try another live request',
  whatIf: 'What if something fails?',
  anotherFailure: 'Try another failure',
  reset: 'Reset room',
  otherScene: 'Try another scene',
  left,
  lastOne: "That was this code's last live request for today.",
  statusWaiting: (n) => `Live AI · ${n} ${n === 1 ? 'device' : 'devices'} · waiting for your request`,
  statusApproval: 'Waiting for your approval',
  statusDone: (n) => (n == null ? 'Done' : `Done · ${left(n)}`),
  whatIfAsk: (ref, plural) => `What if ${ref} ${plural ? 'fail' : 'fails'}?`,
  editRejected: "That change didn't go through, so the card is back to the room's value.",
  editRate: "That's a lot of changes at once. Slow down a little.",
  clockBusy: 'The clock moves again when this request is done.',
  clockPlus1: '+1 h',
  clockPlus6: '+6 h',
  clockSet: 'Set time…',
  clockGo: 'Go',
  ruleFired: 'Rule fired',
  ruleHeld: 'Rule held back',
  scheduled: 'Scheduled',
  // the scene header's direct entry (spec 2026-11-08 §4)
  headerButton: 'Try it live',
  headerButtonTitle: 'Try your own request with the live AI',
  // the Automations section (spec 2026-11-08 §1, js/engine/automations.js)
  autosTitle: 'Automations',
  autosOn: (n) => `${n} on`,
  autosAllOff: 'all off',
  autosNone: 'none yet',
  autosEmpty: 'No automations yet. Ask for one, like “When the soil is below 30%, run the pump.”',
  autosEmptyAny: 'No automations yet. Ask for a rule (“when …, do …”) or a schedule (“every day at …”).',
  autoRule: 'Rule',
  autoSchedule: 'Schedule',
  autoRan: (n) => `ran ${n}× today`,
  autoOff: 'Off',
  autoDone: 'Done', // a one-off schedule whose time has passed
  autosAllDone: 'all done',
  autoJustRan: 'JUST RAN',
  autoHeldBadge: 'HELD',
  autoRejected: "That switch didn't go through, so the automation is back as it was.",
  autoGone: "That automation isn't there any more, so nothing changed.",
};

export const normalizeCode = (text) => String(text ?? '').toUpperCase().replace(/\s+/g, '');

// The saved invite code. Every storage access is inside try/catch (Safari with site data blocked throws even on the
// property read), and a throwing store behaves as empty.
export function codeStore(storage) {
  const KEY = 'ncb.liveCode';
  const use = (f) => { try { return f(storage ?? globalThis.localStorage); } catch { return null; } };
  return {
    get: () => { const v = use((s) => s.getItem(KEY)); return typeof v === 'string' && v ? v : null; },
    set: (code) => { use((s) => s.setItem(KEY, String(code))); },
    clear: () => { use((s) => s.removeItem(KEY)); },
  };
}

const finite = (n) => typeof n === 'number' && Number.isFinite(n);
const isObj = (v) => v && typeof v === 'object' && !Array.isArray(v);
const str = (v) => (typeof v === 'string' ? v : '');
const FIELD = /^[A-Za-z_$][\w$]*$/, NOT_FIELDS = new Set(['__proto__', 'constructor', 'prototype']);
const STEP_STATUS = ['running', 'done', 'failed', 'skipped'];
// A result's or timeline row's ok: a missing one is never read as success when the outcome says it failed.
const okOf = (d) => d.ok === true || (d.ok == null && d.outcome !== 'failed');
// Steps as the contract sends them ({ id, text, devices }); anything else is dropped.
const stepsOf = (list) => (Array.isArray(list) ? list : []).filter((s) => isObj(s) && typeof s.id === 'string')
  .map((s) => ({ id: s.id, text: str(s.text), devices: Array.isArray(s.devices) ? s.devices.filter((d) => typeof d === 'string') : [] }));
// Automations as the contract sends them ({ id, kind, text, devices, enabled, firedToday, done }), in the server's order; an
// item without an id, or a repeated id, is dropped. done: a one-off schedule whose time has passed.
export const automationsOf = (list) => {
  const seen = new Set();
  return (Array.isArray(list) ? list : []).filter((a) => isObj(a) && typeof a.id === 'string' && a.id && !seen.has(a.id) && seen.add(a.id))
    .map((a) => ({ id: a.id, kind: a.kind === 'schedule' ? 'schedule' : 'rule', text: str(a.text), devices: Array.isArray(a.devices) ? a.devices.filter((d) => typeof d === 'string') : [],
      enabled: a.enabled !== false, firedToday: Number.isInteger(a.firedToday) && a.firedToday > 0 ? a.firedToday : 0, done: a.done === true }));
};

// What the offline card says after a /health that did not allow live mode for this scene: no NCF backend at the API
// base (nothing answered, or not the live API), a backend with live off, or one that does not list the scene.
export function whyOff(h, sceneId, base) {
  if (!h?.ok) return LIVE_COPY.backendDown(base);
  if (!h.liveEnabled) return LIVE_COPY.backendOff;
  return h.scenes?.includes(sceneId) ? LIVE_COPY.offline : LIVE_COPY.sceneOff;
}

export function createLive({ scene, store, view, panel, ui, base, fetch: doFetch = globalThis.fetch, codes = codeStore(), now = () => performance.now(), sessionExtra = () => ({}) }) {
  let code = null;            // the code for this visit (kept here too, so a blocked store still works)
  const api = createLiveApi({ base, fetch: doFetch, getCode: () => code ?? codes.get() });
  let closed = false, active = false, available = false, phase = 'off', advancing = false, clockLabel = null, lastRateAt = -Infinity;
  let sessionId = null, stream = null, accessHandle = null, suggestions = [], lastClockAt = 0, isWhatIf = false;
  // The request: its id (null while the POST that starts it is in flight), the latest finished one (what a what-if
  // replays), the events held for that POST, the cards and checklist on screen, the card waiting for an answer and the
  // answer in flight, and a typed change waiting for its decline's `done`.
  let requesting = false;     // a request or what-if is under way (the clock says why it waits only then)
  let held = null;            // a request, what-if or reset picked during an advance: it goes when the advance ends
  let rid = null, lastRid = null, holding = null, card = null, replanCard = null, replanSteps = [], checklist = null;
  let waiting = null, answer = null, pendingText = null, reading = null, planningOn = false, ranStep = false, exitOnDone = false;
  const labels = new Map();   // callId → the call's label, for the status line when its result comes
  let planned = false;        // a checklist exists (an approved plan, or a what-if's): only then is there something to replay
  let ended = null;           // the end card on screen: closed by the next end card, a lost code or a fresh room
  let remainingToday = null;
  const known = new Map();    // path → the room's last value for it (what a refused edit goes back to)
  const busy = new Set();     // the ids live.js marked 'busy': done clears only these, so a JUST SET runs its full 2 s
  const devices = devicesOf(scene), n = devices.length;
  // The Automations section (spec 2026-11-08): the room's automations as the server last sent them, and the switches the
  // visitor flipped: each shows the visitor's value until the server's list agrees (a toggle made mid-request is applied
  // at the runner's next servicing point, after its 202), or goes back when the server refuses it.
  let autos = [];
  const autoWant = new Map(); // id → { on, n, text }: the flip shown, its number, and the text of the automation flipped
  const autoFlying = new Map(); // id → its switch POSTs not answered yet
  let autoN = 0, announced = null;

  // The clock control: usable only between requests (idle), while no advance runs, and while a session exists.
  const clockUi = () => {
    if (dead()) return;
    const waitingOnRequest = requesting && ['planning', 'awaiting', 'running', 'busy'].includes(phase) && !!sessionId;
    ui.clock?.({ label: clockLabel, enabled: phase === 'idle' && !advancing && !!sessionId, reason: waitingOnRequest ? LIVE_COPY.clockBusy : null });
  };
  const setPhase = (p) => { phase = p; clockUi(); };
  const dead = () => closed || !ui.alive();
  const gone = () => dead() || !active;
  const beat = (current) => ui.beats({ current, recoverHidden: !isWhatIf, skippable: [] });
  const status = (e) => (e instanceof LiveError ? e.status : 0);
  const kindOf = (e) => (e instanceof LiveError ? e.kind : null);
  const isDevice = (id) => typeof id === 'string' && devices.includes(id);
  const mark = (id, kind) => { if (kind === 'busy') busy.add(id); else busy.delete(id); panel.mark(id, kind); };
  // A store path the server may write: <slice>.<field…> on a slice the store has, with no prototype keys.
  const writable = (path) => {
    if (typeof path !== 'string') return false;
    const ks = path.split('.');
    return ks.length > 1 && Object.prototype.hasOwnProperty.call(store.state, ks[0]) && ks.every((k) => FIELD.test(k) && !NOT_FIELDS.has(k));
  };
  // The scene header's Live AI button follows this (js/engine/liveentry.js): live mode available here, and open or not.
  const announce = () => { if (dead()) return; const k = `${available}|${active}`; if (k === announced) return; announced = k; ui.availability?.({ available, active }); };

  // ---- automations ----
  const shownAutos = () => autos.map((a) => { const w = autoWant.get(a.id); return w && w.on !== a.enabled ? { ...a, enabled: w.on } : a; });
  const showAutos = () => { if (!dead()) ui.automations?.(shownAutos()); };
  // A flipped switch is settled once the server's list agrees and none of its POSTs is still out (a list that agrees
  // while one is out may predate both flips), or once its automation is gone or replaced (a re-plan can reuse an id).
  const settled = (id, w) => { const a = autos.find((x) => x.id === id); return !a || a.text !== w.text || (a.enabled === w.on && !autoFlying.get(id)); };
  // A list from the server (last one wins). fresh (a new session, a reset, a what-if's room): flipped switches go too.
  function setAutos(list, { fresh = false } = {}) {
    autos = automationsOf(list);
    if (fresh) autoWant.clear();
    else for (const [id, w] of [...autoWant]) if (settled(id, w)) autoWant.delete(id);
    showAutos();
  }
  const noAutos = () => { autos = []; autoWant.clear(); autoFlying.clear(); if (!dead()) ui.automations?.(null); }; // live mode is over: the section goes
  // A timeline row an automation made (a rule fired, a schedule ran, a rule held back): its card flashes.
  function flashAuto(d) {
    if (dead() || typeof d.automation !== 'string' || !autos.some((a) => a.id === d.automation)) return;
    if (d.origin === 'held') ui.automationFlash?.(d.automation, 'held');
    else if (d.origin === 'rule' || d.origin === 'schedule') ui.automationFlash?.(d.automation, 'ran');
  }

  async function probe() {
    try {
      const h = await api.health();
      available = !!(h.ok && h.liveEnabled && h.scenes.includes(scene.id));
    } catch { available = false; }
    announce();
    return available;
  }

  // ---- room ----
  function setClock(c, { instant = false, status = true } = {}) {
    if (!isObj(c) || !finite(c.hour)) return;
    const cur = Number(store.get('env.hour')) || 0, t = now();
    const ms = instant || c.hour < cur ? 0 : Math.min(800, Math.max(0, t - lastClockAt));
    lastClockAt = t; store.tween('env.hour', c.hour, ms);
    if (typeof c.label === 'string') { clockLabel = c.label; clockUi(); }
    if (!instant && status && typeof c.label === 'string') ui.status(c.label); // a room, a reset, a hand edit or an advance sets the clock without taking the status line
  }

  // The server's whole room, merged per field onto the post-setup snapshot (room-only fields keep their values).
  function applyRoom(state, clock) {
    const snap = ui.baseline();
    if (snap) store.restore(snap);
    ui.resetRoom(); known.clear();
    if (isObj(state)) {
      for (const [id, slice] of Object.entries(state)) {
        if (isObj(slice)) for (const [field, value] of Object.entries(slice)) { const p = `${id}.${field}`; if (writable(p)) { store.set(p, value); known.set(p, value); } }
      }
    }
    setClock(clock, { instant: true });
    reading = null;
    for (const id of devices) mark(id, null);
  }

  // ---- the event stream ----
  function onEvent(ev) {
    const d = ev?.data;
    if (gone() || !isObj(d)) return;
    if (ev.event === 'room') { // any request's (a what-if's comes first), with that room's automations
      if (isObj(d.state)) { applyRoom(d.state, d.clock); if (Array.isArray(d.automations)) setAutos(d.automations, { fresh: true }); }
      return;
    }
    if (ev.event === 'automations') { if (Array.isArray(d.items)) setAutos(d.items); return; } // any rid or src: applied at once, never held
    if (d.src === 'edit') return applyEdit(ev); // before the rid check: a late frame may carry a finished request's rid; never held
    if (rid !== null && d.rid === rid) return handle(ev);
    if (holding && typeof d.rid === 'string') holding.push(ev); // maybe the request whose POST hasn't answered yet
  }

  // One event of the current request → the view, the panel and the store.
  function handle({ event: k, data: d }) {
    if (gone()) return;
    // An Approve / Not this in flight: the server acted on it if the run goes on, not if the card expired or was declined.
    if (k === 'expired' || k === 'declined') { if (answer) answer.over = true; answer = null; } else answer?.apply();
    if (reading) { const id = reading; reading = null; mark(id, null); } // a running read's `call` marks it WORKING again
    switch (k) {
      case 'planning': setPhase('planning'); if (!planningOn) { planningOn = true; view.planning(true); } beat('plan'); break;
      case 'reading':
        if (!isDevice(d.device)) break;
        mark(d.device, 'busy'); reading = d.device; panel.reveal([d.device]); // a read changes no value: reveal shows its card
        if (phase === 'planning') view.reading(refOf(scene, d.device));
        break;
      case 'plan': {
        const steps = stepsOf(d.steps);
        planningOn = false; view.planning(false);
        if (isWhatIf) { checklist = view.checklist(steps, { replay: true }); planned = true; setPhase('running'); } // a replay asks nothing
        else card = planCard({ kind: 'plan', steps });
        break;
      }
      case 'awaiting_approval': {
        const c = d.kind === 'replan' ? replanCard : card;
        if (!c) break;
        waiting = c; setPhase('awaiting'); ui.status(LIVE_COPY.statusApproval); view.input(true); c.enable(); // a typed change re-plans
        ui.attention?.(); // an Automations list lying over the chat (phones) folds: the card needs the visitor
        break;
      }
      case 'cannot_do':
        planningOn = false; view.planning(false); view.ncb(str(d.reason));
        view.chips(Array.isArray(d.suggestions) ? d.suggestions.filter((t) => typeof t === 'string') : [], submit);
        break;
      case 'declined': card?.declined(); waiting = null; if (!pendingText) view.ncb(LIVE_COPY.declined); break; // plans only
      case 'expired': { const c = waiting; waiting = null; if (c) { c.close(); view.ncb(LIVE_COPY.expired); } break; }
      case 'step':
        if (typeof d.id !== 'string' || !STEP_STATUS.includes(d.status)) break;
        checklist?.step(d.id, d.status);
        if (d.status === 'running' && !ranStep) { ranStep = true; beat('run'); }
        break;
      case 'call':
        checklist?.call({ callId: d.callId, step: d.step ?? null, label: str(d.label) }); // a null step: a row of its own (view)
        if (isDevice(d.device)) { mark(d.device, 'busy'); panel.reveal([d.device]); }
        if (typeof d.label === 'string') { ui.status(d.label); if (typeof d.callId === 'string') labels.set(d.callId, d.label); }
        break;
      case 'state': applyState(d); break;
      case 'result':
        checklist?.result({ callId: d.callId, outcome: d.outcome, text: str(d.text), ok: okOf(d) });
        { // the line keeps the call's label with its outcome (red when it failed)
          const lab = labels.get(d.callId); labels.delete(d.callId);
          if (lab != null) ui.status(`${lab} · ${str(d.text)}`, !okOf(d));
        }
        if (isDevice(d.device)) mark(d.device, okOf(d) && (d.outcome === 'confirmed' || d.outcome === 'made_safe') ? 'just' : null);
        break;
      case 'fault': // the server's text first: the card and flow.js's status line read it once the status turns 'fault'
        if (!isDevice(d.device)) break;
        store.set(`${d.device}.faultNote`, str(d.text)); mark(d.device, null);
        if (isWhatIf) beat('recover');
        break;
      case 'clock': setClock(d); break;
      case 'timeline': checklist?.timeline({ at: str(d.at), text: str(d.text), ok: okOf(d), kind: d.kind, origin: d.origin }); flashAuto(d); break;
      case 'replan': // its ids are final (contract): approving merges them into the checklist
        replanSteps = stepsOf(d.steps);
        replanCard = planCard({ kind: 'replan', steps: replanSteps, reason: str(d.reason) });
        break;
      case 'say': if (typeof d.delta === 'string') view.sayDelta(d.delta); break;
      case 'summary': if (typeof d.text === 'string') view.summary(d.text); break;
      case 'limit':
        if (d.kind === 'budget') { available = false; exitOnDone = true; announce(); view.ncb(LIVE_COPY.limitGlobal); } else view.ncb(LIVE_COPY.limitRun);
        break;
      case 'error': view.ncb(LIVE_COPY.error); break;
      case 'done': done(d); break;
      default: break;
    }
  }

  function applyState(d) {
    if (!writable(d.path)) return;
    known.set(d.path, d.value);
    // a number always goes through tween (0 ms: a jump that also drops a running tween, which a plain set would lose to)
    if (finite(d.value)) store.tween(d.path, d.value, finite(d.ms) && d.ms > 0 ? d.ms : 0); else store.set(d.path, d.value);
  }

  // An event a hand edit (or a rule or schedule it set off) caused: it has its own routing, outside any request's.
  function applyEdit({ event: k, data: d }) {
    if (gone()) return;
    if (k === 'state') return applyState(d);
    if (k === 'clock') return setClock(d, { status: false });
    if (k !== 'timeline') return;
    const row = { at: str(d.at), text: str(d.text), ok: okOf(d), kind: d.kind, origin: d.origin };
    if (rid !== null && d.rid === rid && checklist) checklist.timeline(row); else view.ruleLine(row);
    if (isDevice(d.device) && d.ok === true && (d.origin === 'rule' || d.origin === 'schedule')) mark(d.device, 'just');
    flashAuto(d);
  }

  function done(d) {
    for (const id of [...busy]) mark(id, null); // never every device: that would cut a JUST SET short
    ui.track(false); checklist?.done();
    if (planningOn) { planningOn = false; view.planning(false); }
    if (finite(d.remainingToday)) remainingToday = d.remainingToday;
    waiting?.close(); // a card still waiting (the request ended under it) takes no more answers
    card = replanCard = checklist = waiting = null; requesting = false;
    if (exitOnDone) { exitOnDone = false; return exit(); } // the kill switch tripped mid-run
    const out = d.outcome;
    if (out === 'ok' || out === 'failed' || out === 'limit') {
      const replayable = planned && out !== 'limit'; // a what-if replays the steps that ran: none ran, nothing to replay
      if (replayable && !isWhatIf) lastRid = rid;     // a what-if never becomes the latest finished request (contract)
      closeEnd(); // the last end card, which stays usable until this one shows (a decline or cannot_do shows none)
      ended = view.endCard({ remainingToday, whatIfLabel: isWhatIf ? LIVE_COPY.anotherFailure : LIVE_COPY.whatIf, whatIf: replayable }, endActions()) ?? null;
      beat('end'); ui.status(LIVE_COPY.statusDone(remainingToday));
    } else { beat('ask'); ui.status(LIVE_COPY.statusWaiting(n)); }
    if (remainingToday === 0 && !isWhatIf) view.ncb(LIVE_COPY.lastOne); // a what-if spends no request
    setPhase('idle'); view.input(true);
    if (pendingText) { const t = pendingText; pendingText = null; submit(t); }
  }

  // The end card's buttons (the view disables them all when one is pressed). Each acts only between requests.
  const endActions = () => ({
    onAnother: () => { if (gone() || phase !== 'idle') return; isWhatIf = false; beat('ask'); ui.status(LIVE_COPY.statusWaiting(n)); if (!advancing) view.input(true); }, // an advance gives the input back itself
    onWhatIf: () => {
      if (gone() || phase !== 'idle' || !lastRid) return;
      view.failures(devices.map((id) => ({ key: id, icon: scene.devices[id].icon, name: scene.devices[id].name })), whatIf);
    },
    onReset: () => resetRoom(),
  });

  // ---- plan and re-plan cards ----
  function planCard(spec) {
    let h = null;
    h = view.planCard(spec, { onApprove: () => respond(h, 'approve'), onDecline: () => respond(h, 'decline') });
    return h;
  }
  // Approve / Not this on the card waiting, or a typed change (a decline that keeps `typed` for when the request ends).
  // One POST at a time per card. Its effects run once: on the 202, or on the first event that shows the server acted on
  // it, whichever comes first (the server runs on before its answer reaches the page).
  async function respond(c, action, typed = null) {
    if (gone() || !c || c !== waiting || phase !== 'awaiting') return;
    const replan = c === replanCard, steps = replanSteps, a = { over: false };
    a.apply = () => {
      if (a.over) return;
      a.over = true; if (answer === a) answer = null; waiting = null;
      if (action === 'approve') {
        if (replan) { c.close(); checklist?.mergeSteps(steps); } else { checklist = c.toChecklist(); planned = true; }
        ui.status(LIVE_COPY.checklistTitle); // no longer "Waiting for your approval"; the first call takes it from here
      } else if (replan) c.declined(); // no event comes for it (contract): the demo marks the card, the run carries on
      else return;                     // a plan's decline: its `declined` and `done` follow
      setPhase('running'); view.input(false);
    };
    answer = a; if (typed != null) pendingText = typed;
    setPhase('busy'); view.input(false); c.busy();
    try { await (action === 'approve' ? api.approve(sessionId, rid) : api.decline(sessionId, rid)); } catch (e) {
      if (a.over || gone()) return;
      a.over = true; answer = null;
      if (status(e) === 401) return unauthorized();
      if (kindOf(e) === 'not_waiting') { c.close(); if (waiting === c) waiting = null; view.ncb(LIVE_COPY.expired); return; } // its `done` follows
      pendingText = null; view.ncb(LIVE_COPY.error); setPhase('awaiting'); view.input(true); c.enable();
      return;
    }
    if (!gone()) a.apply();
  }

  // ---- requests ----
  // A new request or what-if: nothing of the last one stays live.
  function begin() { labels.clear(); card = replanCard = checklist = waiting = null; replanSteps = []; reading = null; ranStep = false; exitOnDone = false; planningOn = false; planned = false; }
  // The POST that starts a request (requests, …/whatif). Until it answers, events with an unknown rid are held (the server
  // starts before it answers); then those of its requestId run in order and the rest are dropped, or all of them when it
  // fails. Resolves false when the visit moved on meanwhile.
  async function start(post) {
    rid = null; const held = holding = [];
    let r;
    try {
      r = await post();
      if (!isObj(r) || typeof r.requestId !== 'string') throw new LiveError(200, 'bad_response');
    } catch (e) { if (holding === held) holding = null; throw e; }
    if (holding !== held) return false;
    holding = null;
    if (gone()) return false;
    rid = r.requestId;
    for (const ev of held) if (ev.data.rid === rid && !gone()) handle(ev);
    return true;
  }
  const backToIdle = () => { requesting = false; setPhase('idle'); ui.track(false); view.input(true); ui.status(LIVE_COPY.statusWaiting(n)); };
  const closeEnd = () => { ended?.close?.(); ended = null; };
  const limited = (line, off = false) => { if (off) available = false; view.ncb(line); exit(); };

  async function submit(text) {
    const t = String(text ?? '').trim();
    if (gone() || !t) return;
    if (t.length > 400) { view.ncb(LIVE_COPY.tooLong); return; } // UTF-16 units, as the server counts
    if (phase === 'awaiting') return respond(waiting, 'decline', t); // a typed change: decline quietly, then plan this
    if (phase !== 'idle') return;                                   // one request at a time
    if (advancing) { held = () => submit(t); return; }              // the server is busy with the advance: send it when that ends
    begin(); requesting = true; setPhase('busy'); isWhatIf = false;
    view.closeChips(); // an old "Try:" or cannot_do chip can't start a second request
    view.input(false); view.user(t); ui.status(LIVE_COPY.planning); ui.track(true);
    planningOn = true; view.planning(true); // "Planning…" at once (spec 3.3), not when the server's event arrives
    for (let retried = false; ; retried = true) {
      try { await start(() => api.request(sessionId, t)); return; } catch (e) {
        if (gone()) return;
        const k = kindOf(e);
        if (status(e) === 401) return unauthorized();
        if (k === 'code_limit') return limited(LIVE_COPY.limitCode);
        if (k === 'global_limit' || k === 'budget') return limited(LIVE_COPY.limitGlobal, k === 'budget');
        if (k === 'live_off') return limited(LIVE_COPY.offline, true); // switched off, or no NeuChar key: not a limit
        if (k === 'no_session' && !retried) { if ((await reopen()) && !gone()) { planningOn = true; view.planning(true); continue; } return; }
        if (planningOn) { planningOn = false; view.planning(false); }
        view.ncb(k === 'too_long' ? LIVE_COPY.tooLong : LIVE_COPY.error);
        return backToIdle();
      }
    }
  }

  // "What if something fails?" on an end card: the latest finished request, replayed with `id` failing.
  async function whatIf(id) {
    if (gone() || phase !== 'idle' || !lastRid || !isDevice(id)) return;
    if (advancing) { held = () => whatIf(id); return; }
    view.user(LIVE_COPY.whatIfAsk(refOf(scene, id), !!scene.devices[id].plural));
    begin(); requesting = true; isWhatIf = true; setPhase('busy'); ranStep = true; // its Run beat is now
    view.input(false); ui.rewind(); ui.status(REPLAYING); ui.track(true); beat('run');
    try { await start(() => api.whatIf(sessionId, lastRid, id)); } catch (e) {
      if (gone()) return;
      const k = kindOf(e);
      if (status(e) === 401) return unauthorized();
      if (k === 'budget') return limited(LIVE_COPY.limitGlobal, true);
      if (k === 'live_off') return limited(LIVE_COPY.offline, true);
      isWhatIf = false;
      if (k === 'no_session') return regenerate();
      view.ncb(LIVE_COPY.error); beat('ask'); backToIdle();
    }
  }

  // "Reset room" on an end card. reset emits no event (contract), so its answer is the only source of the room.
  async function resetRoom() {
    if (gone() || phase !== 'idle') return;
    if (advancing) { held = () => resetRoom(); return; }
    setPhase('busy'); view.input(false);
    let r;
    try {
      r = await api.reset(sessionId);
      if (!isObj(r) || !isObj(r.state)) throw new LiveError(200, 'bad_response');
    } catch (e) {
      if (gone()) return;
      if (status(e) === 401) return unauthorized();
      if (kindOf(e) === 'no_session') return regenerate();
      view.ncb(LIVE_COPY.error); setPhase('idle'); view.input(true);
      return;
    }
    if (gone()) return;
    applyRoom(r.state, r.clock);
    setAutos(Array.isArray(r.automations) ? r.automations : [], { fresh: true }); // a reset clears them all (spec §2)
    lastRid = null; isWhatIf = false; // nothing to replay until the next request
    view.ncb(FRESH_START); beat('ask'); ui.status(LIVE_COPY.statusWaiting(n)); setPhase('idle'); view.input(true);
  }

  // ---- hand edits and the clock ----
  const editable = (path) => devices.some((id) => editsFor(id, scene.devices[id], store.state[id]).some((e) => e.path === path));
  // The rate message at most once per 10 s, on the clock the rest of this file uses.
  const sayRate = () => { const t = now(); if (t - lastRateAt >= 10000) { lastRateAt = t; view.ncb(LIVE_COPY.editRate); } };

  async function edit(path, value) {
    if (gone() || !sessionId || !writable(path) || !editable(path)) return;
    const sid = sessionId, prev = store.get(path);
    store.tween(path, value, 0); // at once, and a running tween on the path is dropped
    // Back to the room's value, unless a later edit of the card has moved it on meanwhile.
    const rollback = () => { if (store.get(path) === value) { const back = known.has(path) ? known.get(path) : prev; if (finite(back)) store.tween(path, back, 0); else store.set(path, back); } };
    try { await api.set(sid, path, value); } catch (e) {
      if (gone() || sid !== sessionId) return;
      if (status(e) === 401) return unauthorized();
      if (kindOf(e) === 'no_session') return regenerate();
      rollback();
      if (kindOf(e) === 'rate') sayRate(); else view.ncb(LIVE_COPY.editRejected);
      return;
    }
  }

  // An automation's switch: shown at once, then POST …/automations/{id}. Free, allowed at any time, even mid-request or
  // during an advance (the server applies it where it owns the room); it shares the edit rate limit. A refusal puts the
  // switch back to the server's value, unless a later flip of it has moved it on meanwhile.
  async function setAutomation(id, on) {
    const a = autos.find((x) => x.id === id);
    if (gone() || !sessionId || typeof on !== 'boolean' || !a || a.done) return; // a done one-off never runs again: no switch
    const sid = sessionId, k = ++autoN;
    autoWant.set(id, { on, n: k, text: a.text }); autoFlying.set(id, (autoFlying.get(id) || 0) + 1); showAutos();
    let err = null;
    try { await api.automation(sid, id, on); } catch (e) { err = e; }
    if (sid === sessionId) { const left = (autoFlying.get(id) || 1) - 1; if (left > 0) autoFlying.set(id, left); else autoFlying.delete(id); }
    if (gone() || sid !== sessionId) return;
    const settle = () => { const w = autoWant.get(id); if (w && settled(id, w)) { autoWant.delete(id); showAutos(); } };
    if (!err) return settle(); // the server's list may have agreed while this POST was out
    if (status(err) === 401) return unauthorized();
    if (kindOf(err) === 'no_session') return regenerate();
    if (autoWant.get(id)?.n === k) { autoWant.delete(id); showAutos(); } else settle(); // a later flip moved it on: that one stands
    if (kindOf(err) === 'rate') sayRate(); else view.ncb(kindOf(err) === 'no_automation' ? LIVE_COPY.autoGone : LIVE_COPY.autoRejected);
  }

  async function advance(hours) {
    if (gone() || !sessionId || phase !== 'idle' || advancing) return;
    const sid = sessionId;
    advancing = true; clockUi(); view.input(false); // the server answers 409 busy to a request, what-if or reset meanwhile: hold those
    try {
      const r = await api.advance(sid, hours);
      if (!gone() && sid === sessionId) setClock(r?.clock, { status: false });
    } catch (e) {
      if (gone() || sid !== sessionId) return;
      if (status(e) === 401) unauthorized();
      else if (kindOf(e) === 'no_session') regenerate();
      else if (kindOf(e) === 'rate') sayRate();
    } finally {
      advancing = false; clockUi();
      if (!gone() && sid === sessionId && phase === 'idle') { view.input(true); const h = held; held = null; h?.(); }
    }
  }

  // ---- the stream's status ----
  function onStatus(s) {
    if (gone()) return;
    if (s === 'gone') regenerate();
    else if (s === 'unauthorized') unauthorized();
    else if (s === 'lost') view.lost(reconnect);
  }
  function openStream(after) {
    stream?.close();
    stream = openEventStream({ api, sessionId, after: finite(after) ? after : 0, fetch: doFetch, onEvent, onStatus });
  }
  function reconnect() {
    if (gone() || !sessionId || phase === 'access') return; // a Reconnect left from before a lost code does nothing
    openStream(stream?.lastId);
  }

  // ---- sessions ----
  // A run abandoned without its `done` (a lost code, exit): take down what it left on screen. Not on close(), which is
  // silent, and nothing once the page is gone.
  function release() {
    if (dead()) return;
    for (const id of [...busy]) mark(id, null);
    if (planningOn) { planningOn = false; view.planning(false); }
    waiting?.close(); waiting = null;
    checklist?.done(); closeEnd(); // no step left running, no end card left to press
    ui.track(false);
  }
  // Forget the request on screen (a new session, a lost code, exit, close): late events and answers find nothing.
  function forget() {
    if (answer) answer.over = true;
    rid = null; lastRid = null; holding = null; answer = null; pendingText = null; held = null; isWhatIf = false; requesting = false;
    busy.clear(); begin(); autos = []; autoWant.clear(); autoFlying.clear();
  }
  // Makes a session and its stream. false when it failed (already handled) or the visit moved on.
  async function newSession({ notice } = {}) {
    stream?.close(); stream = null;
    let s;
    try {
      s = await api.createSession(scene.id, sessionExtra());
      if (!s || typeof s.sessionId !== 'string' || !isObj(s.state)) throw new LiveError(200, 'bad_response');
    } catch (e) { if (!gone()) failed(e); return false; }
    if (gone()) return false;
    if (planningOn) { planningOn = false; view.planning(false); }
    waiting?.close(); // a card left from the expired session takes no answers (its marks go with applyRoom below)
    checklist?.done(); closeEnd(); // and its run is over: no step left running, no end card left to press
    forget();
    sessionId = s.sessionId;
    suggestions = Array.isArray(s.suggestions) ? s.suggestions.filter((t) => typeof t === 'string') : [];
    applyRoom(s.state, s.clock);
    ui.editing?.(true);
    setAutos(s.automations, { fresh: true }); // the section shows from here (empty at the start)
    openStream(0);
    if (notice) view.ncb(notice);
    return true;
  }
  const idle = () => { ui.status(LIVE_COPY.statusWaiting(n)); beat('ask'); setPhase('idle'); };
  // The session expired under the stream (or under a POST): a fresh room, and the visitor can go on.
  // One at a time: a stream 'gone', a failed edit and a failed request can all find the session expired together.
  let opening = null, regenerating = null;
  const reopen = () => (opening ??= newSession({ notice: LIVE_COPY.timedOut }).finally(() => { opening = null; }));
  function regenerate() {
    if (regenerating) return regenerating;
    requesting = false; setPhase('busy');
    return (regenerating = (async () => {
      if (await reopen()) { ui.track(false); view.input(true); idle(); }
    })().finally(() => { regenerating = null; }));
  }
  // A POST sessions that failed (or answered nonsense).
  function failed(e) {
    const k = kindOf(e);
    if (status(e) === 401) return unauthorized();
    if (k === 'global_limit') view.ncb(LIVE_COPY.sessionsBusy); // the server's session cap, not the daily one
    else if (k === 'budget') view.ncb(LIVE_COPY.limitGlobal);
    else if (k === 'code_limit') view.ncb(LIVE_COPY.limitCode);
    else if (k === 'live_off') view.ncb(LIVE_COPY.offline);
    else if (k === 'bad_room') view.ncb(LIVE_COPY.badRoom); // the custom scene's room was refused
    else view.ncb(LIVE_COPY.error);
    if (k === 'budget' || k === 'live_off') available = false; // /health says off from here on too
    exit();
  }
  // The code stopped working: forget it (and the session it opened) and ask again.
  function unauthorized() {
    release();
    codes.clear(); code = null;
    stream?.close(); stream = null; sessionId = null; forget();
    ui.editing?.(false); noAutos(); clockUi();
    view.input(false);
    showAccess({ mode: 'code', note: LIVE_COPY.savedCodeGone });
  }

  // ---- access ----
  function showAccess(spec) {
    setPhase('access');
    accessHandle?.close(); accessHandle = null;
    accessHandle = view.access(spec, { onCode, onApply, onBack: () => { if (!gone()) exit(); } }) ?? null;
  }
  // api.check, reduced to { left } or { err: 'bad'|'offline' }.
  async function checkCode(c) {
    try {
      const r = await api.check(c);
      return finite(r?.remainingToday) ? { left: r.remainingToday } : { err: 'offline' };
    } catch (e) { return { err: status(e) === 401 ? 'bad' : status(e) === 429 && kindOf(e) === 'rate' ? 'rate' : 'offline' }; }
  }
  async function onCode(raw) {
    if (gone()) return null;
    const c = normalizeCode(raw), r = await checkCode(c);
    if (gone() || phase !== 'access') return null; // a second code in flight: the first one already got in
    if (r.err) return r.err === 'bad' ? LIVE_COPY.badCode : r.err === 'rate' ? LIVE_COPY.rate : LIVE_COPY.offline;
    if (r.left <= 0) return LIVE_COPY.noneLeft;
    code = c; codes.set(c);
    const handle = accessHandle; accessHandle = null; handle?.close();
    await enter();
    return null;
  }
  async function onApply(form) {
    if (gone()) return null;
    try { await api.apply({ ...(isObj(form) ? form : {}), scene: scene.id }); } catch (e) {
      return status(e) === 429 ? LIVE_COPY.applyRate : LIVE_COPY.offline;
    }
    return LIVE_COPY.applied;
  }

  async function enter() {
    setPhase('busy');
    if (!(await newSession()) || gone()) return;
    view.live(true); view.input(true);
    view.ncb(LIVE_COPY.intro(n));
    if (suggestions.length) view.chips(suggestions, (t) => { if (!gone()) return submit(t); }, LIVE_COPY.tryHeading);
    idle();
  }

  async function open() {
    if (active || closed) return;
    active = true; setPhase('access'); announce();
    const h = await api.health();
    if (gone()) return;
    available = !!(h.ok && h.liveEnabled && h.scenes.includes(scene.id)); announce();
    if (!available) return showAccess({ mode: 'offline', offlineText: whyOff(h, scene.id, base) });
    const stored = codes.get();
    if (!stored) return showAccess({ mode: 'code' });
    const r = await checkCode(stored);
    if (gone()) return;
    if (r.err === 'bad') { codes.clear(); return showAccess({ mode: 'code', note: LIVE_COPY.savedCodeGone }); }
    if (r.err) return showAccess({ mode: 'offline' });
    if (r.left <= 0) return showAccess({ mode: 'code', note: LIVE_COPY.noneLeft });
    code = stored;
    await enter();
  }

  function exit(heading) {
    if (!active) return;
    release();
    view.closeChips();
    stream?.close(); stream = null;
    accessHandle?.close(); accessHandle = null;
    active = false; setPhase('off'); sessionId = null; forget();
    if (dead()) return;
    ui.track(false); ui.editing?.(false); noAutos(); view.input(false); view.live(false);
    ui.offerGuided(heading);
    announce();
  }
  // Teardown (the visitor left the scene): abort and fall silent, with no view, panel, ui or store call after it.
  function close() { closed = true; active = false; phase = 'off'; stream?.close(); stream = null; forget(); }

  return {
    probe, open, submit, exit, close, edit, advance, setAutomation,
    get automations() { return shownAutos(); }, // what the section shows (QA hooks)
    get available() { return available; },
    get active() { return active; },
    get phase() { return phase; },
    get advancing() { return advancing; },
    get sessionId() { return sessionId; },
  };
}
