// Opt-in failures ("What if something fails?"): what the picker lists, what each pick plays, and the what-if run
// itself. Host-agnostic: main.js passes the page's side effects in `host`, test/run.mjs and test/build.mjs stubs.
//
// Every prompt has at least one TOP-LEVEL { failPoint: name } in its steps (a no-op in a normal run; test/run.mjs
// requires one). Its clean run is the default and ends with its { end } step; the visitor picks a failure from its
// success end card. Scene data:
//   devices[id].ref       how NCB names the device mid-sentence ("the floor lamp", "Mirror M2")
//   devices[id].plural    true for a name that takes a plural verb ("the blinds"): "What if the blinds fail?", "them"
//   prompt.genericAt      failPoint where a device the request doesn't use fails (default: the first failPoint)
//   prompt.genericTitle   the alert title of that engine-generated failure (default '⚠ Device down'; a title starting
//                         with 📝 shows as a calm note, js/engine/chat.js)
//   prompt.whatIf[id]     a device of the scene stops working: { at?, intro?, ask?, steps, alsoFaults? }
//   prompt.whatIf[key]    a special (key is not a device id): { label, ask, at?, intro?, steps, alsoFaults? }
//     at          the failPoint the replay stops before (default: the prompt's first failPoint)
//     intro       NCB's opening line (default: "Replaying this request. This time, <ref> fails during the run." for a
//                 device, "fail" for a plural one; "Replaying this request. This time: <label, first letter lower-cased>."
//                 for a special)
//     ask         the visitor's bubble (default for a device: "What if <ref> fails?")
//     label       the picker button of a special
//     steps       the scenario, in the step DSL (js/engine/player.js), ending with its { end } card (the last step). No
//                 { beat } steps: runWhatIf() puts { beat: 'recover' } before them. It updates the status line (a status
//                 step, a label, ctx.status or a fail) before NCB first speaks, so "Replaying this request…" goes then.
//     alsoFaults  other device ids the scenario may leave in 'fault' (tests)
//     show        device ids whose tiles the phone strip brings into view when the scenario starts (optional; for a
//                 special that changes no tile, so the strip isn't left on the first tiles while NCB talks about others)
// A device with no entry gets the engine-generated scenario below (the tests check it really is unused).
//
// runWhatIf(): the visitor's bubble; host.freshRoom() and the status "Replaying this request…"; the prompt's steps
// again, quiet and instant, answered with the visitor's recorded choices, up to the failPoint (at once, so no frame
// shows the room right after setup, and the last what-if's fault is gone as soon as the visitor asks); NCB's intro and a
// short hold in the replayed room; the Recover beat and the scenario at normal speed. host.quiet(true) spans the replay
// and the intro (scenes read it through the `quiet` flag of build(), A2); host.clearMarkers() runs right after them.
// If the visitor answers a plan card in the scenario with "Not this", nothing more of it runs: NCB says so and the
// engine's own end card closes the what-if.

export const REPLAYING = 'Replaying this request…';
export const GENERIC_HEADLINE = 'It knew what mattered.';
// The generic scenario, right before the rest of the request runs quietly: the room jumps to its end, so NCB says so.
export const SKIP_AHEAD = 'Skipping ahead to the end of the request.';
// "Not this" on a plan card inside a scenario: what NCB says, and the card that ends the what-if there.
export const DECLINED_SAY = "OK, I won't run that plan. I'll leave everything as it is and wait for you before I change anything else.";
export const DECLINED_END = { headline: 'You said no, so it waited.', body: "NeuCharBox didn't run the new plan. It left everything as it was and waited for you instead of acting on its own." };
export const ENGINE_HEADLINES = [GENERIC_HEADLINE, DECLINED_END.headline]; // the engine's own end cards (tests: not a scene's)

export const isFailPoint = (s) => !!s && s.failPoint != null;
export const failPointsOf = (prompt) => (prompt?.steps || []).filter(isFailPoint).map((s) => s.failPoint);
export const hasWhatIf = (prompt) => failPointsOf(prompt).length > 0;
export const devicesOf = (scene) => scene.deviceOrder || Object.keys(scene.devices).filter((d) => d !== 'hub' && d !== 'env');
export const capFirst = (s) => (s ? s.charAt(0).toUpperCase() + s.slice(1) : s);
export const lcFirst = (s) => (s ? s.charAt(0).toLowerCase() + s.slice(1) : s);

// How NCB names a device mid-sentence: its `ref`, or a fallback from its name ("Water pump" → "the water pump";
// names with a code in them, "Mirror M2", "Stage X", stay as they are).
export function refOf(scene, id) {
  const d = scene.devices[id]; if (!d) return id;
  if (d.ref) return d.ref;
  if (/\s[A-Z0-9]{1,3}\b|\d/.test(d.name)) return d.name;
  return `the ${/^[A-Z][a-z]/.test(d.name) ? d.name.charAt(0).toLowerCase() + d.name.slice(1) : d.name}`; // "USB webcam" keeps its capitals
}

// The picker: every connected device in scene order (`unused`: no authored scenario, so the generic one), then the
// prompt's specials in the order they are written.
export function pickerOf(scene, prompt) {
  const wi = prompt.whatIf || {};
  const devices = devicesOf(scene).map((id) => ({ key: id, name: scene.devices[id].name, icon: scene.devices[id].icon, unused: !wi[id] }));
  const specials = Object.keys(wi).filter((k) => !scene.devices[k]).map((k) => ({ key: k, label: wi[k].label || k }));
  return { devices, specials };
}

// The engine-generated scenario for a device this request doesn't use: it fails at `genericAt`, NCB flags it and
// says it skips ahead, the rest of the request runs quietly as planned (runWhatIf), the status line gives the fault's
// red line up for a calm "flagged" one, and the end card says why nothing had to change.
export function genericScenario(scene, id, prompt = null) {
  const d = scene.devices[id], ref = refOf(scene, id), it = d.plural ? 'them' : 'it', itIs = d.plural ? "they're" : "it's";
  return {
    head: [
      { fail: id, say: `${d.name} stopped responding · ${d.faultText || 'no response'}.`, title: prompt?.genericTitle || '⚠ Device down' },
      { say: `This request doesn't use ${ref}, so nothing in the plan changes. I've flagged ${it}, and I'll tell you when ${itIs} back.` },
      { wait: 900 },
      { say: SKIP_AHEAD },
    ],
    tail: [
      { status: `${d.name} flagged · the rest ran as planned` },
      { say: 'The rest of the request ran as planned.' },
      { wait: 1200 },
      { end: { headline: GENERIC_HEADLINE, body: `${capFirst(ref)} stopped responding, but this request didn't need ${it}. NeuCharBox kept the plan running and flagged the device instead of stopping everything.` } },
    ],
  };
}

// What a pick plays. null when the key is neither a device of the scene nor a what-if of the prompt, or the prompt
// has no failPoint.
export function scenarioOf(scene, prompt, key) {
  const pts = failPointsOf(prompt); if (!pts.length) return null;
  const wi = prompt.whatIf?.[key], dev = scene.devices[key] && devicesOf(scene).includes(key);
  const atOf = (a) => (a != null && pts.includes(a) ? a : pts[0]); // a misspelt `at` falls back to the first failPoint (the tests flag it)
  if (dev) {
    const ref = refOf(scene, key), fails = scene.devices[key].plural ? 'fail' : 'fails';
    const base = { key, kind: 'device', ask: wi?.ask || `What if ${ref} ${fails}?`, intro: wi?.intro || `Replaying this request. This time, ${ref} ${fails} during the run.` };
    if (wi) return { ...base, authored: true, at: atOf(wi.at), steps: wi.steps || [], alsoFaults: wi.alsoFaults || [], show: wi.show || [] };
    return { ...base, authored: false, at: atOf(prompt.genericAt), ...genericScenario(scene, key, prompt), alsoFaults: [], show: [] };
  }
  if (!wi) return null;
  const label = wi.label || key;
  return { key, kind: 'special', authored: true, label, ask: wi.ask || `What if ${lcFirst(label)}?`, intro: wi.intro || `Replaying this request. This time: ${lcFirst(label)}.`, at: atOf(wi.at), steps: wi.steps || [], alsoFaults: wi.alsoFaults || [], show: wi.show || [] };
}

// One what-if run with `player` (one play at a time). `choices`: the visitor's last clean run of this prompt
// (play().choices). host = { alive(), quiet(on), freshRoom(), status(text), clearMarkers(), show(ids) }; every hook is
// optional.
// Resolves { scenario, replay } (replay: the quiet play's result, stopped: true when it reached the failPoint; declined:
// true when the visitor said "Not this" to a plan card of the scenario), or { aborted: true } when host.alive() turns
// false (the visitor left the scene).
export async function runWhatIf({ player, scene, prompt, key, choices = [], host = {} }) {
  const sc = scenarioOf(scene, prompt, key);
  if (!sc) throw new Error(`No what-if "${key}" for "${String(prompt?.chip).slice(0, 40)}"`);
  const h = { alive: () => true, quiet() {}, freshRoom() {}, status() {}, clearMarkers() {}, show() {}, ...host };
  const stops = (s) => isFailPoint(s) && s.failPoint === sc.at;
  const res = { scenario: sc, replay: null };
  const gone = () => { if (h.alive()) return false; h.quiet(false); res.aborted = true; return true; };

  // 1. The visitor asks.
  await player.play([{ beat: 'run' }, { user: sc.ask }]);
  if (gone()) return res;
  // 2. The room right after setup, then the request again, quiet and instant, with the visitor's own answers, up to
  //    the failure point. The replay settles in microtasks, straight after freshRoom(), so no frame shows the room
  //    right after setup (a daylight flash before a night-time failure), and the previous what-if's fault (its red
  //    tile, status line and room) goes the moment the visitor asks, not seconds later.
  h.quiet(true); h.freshRoom(); h.status(REPLAYING);
  res.replay = await player.play(prompt.steps, { quiet: true, replay: choices, until: stops, carry: true });
  if (gone()) return res;
  // 3. NCB says it replays the request and what fails this time, then a hold, so "Replaying this request…" and the
  //    replayed room both read.
  await player.play([{ say: sc.intro }, { wait: 1100 }], { carry: true });
  if (gone()) return res;
  h.clearMarkers(); h.quiet(false);
  if (sc.show.length) h.show(sc.show);
  // 4. The failure, at normal speed. "Not this" on a plan card of the scenario stops it there (the player runs nothing
  //    after a declined plan): NCB says so, and the engine's end card closes the what-if, so the visitor can go on.
  if (sc.authored) {
    const r = await player.play([{ beat: 'recover' }, ...sc.steps], { carry: true });
    if (r.declined && !gone()) { res.declined = true; await player.play([{ say: DECLINED_SAY }, { wait: 900 }, { end: DECLINED_END }], { carry: true }); }
    return res;
  }
  await player.play([{ beat: 'recover' }, ...sc.head], { carry: true });
  if (gone()) return res;
  const at = prompt.steps.findIndex(stops);
  if (at >= 0) { // the rest of the clean run, quiet, with the answers the replay didn't use, up to its end card
    h.quiet(true);
    res.rest = await player.play(prompt.steps.slice(at + 1), { quiet: true, replay: res.replay.unused, until: (s) => !!s.end, carry: true });
    h.quiet(false);
    if (gone()) return res;
  }
  await player.play(sc.tail, { carry: true });
  return res;
}
