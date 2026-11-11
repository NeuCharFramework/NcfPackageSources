// Scene 2 — Laser lab (illustrative). Proves: exact instructions, executed and verified step by step.
// Alignment model (pure, derived from the store so the panel and the room agree):
//   meter µW = PMAX · gauss(M1 + M2 error) · iris(stage x) · laser mW / 5 · laser.out
// The two mirrors steer one beam, so their errors add, as they do for the camera spot (spotPx). M2 starts 0.12° off in
// yaw, so the beam lands low on the meter until someone walks it in. Room truths the devices don't report themselves:
// laser.stuck, the shutter flag has dropped back into the beam while the driver still says "open"; laser.out, the
// fraction of the set power the laser really emits; meter.bump < 1, something is taking power out of the last leg;
// beamcam.frozen, the camera repeating one old frame. m1.reported / m2.reported are the angles a controller claims (its
// tile shows them) when that isn't where the mirror is.
// Failures are opt-in (js/engine/whatif.js): every request runs clean, and each prompt's whatIf holds its scenarios.

const PMAX = 860;             // µW on the meter with perfect alignment at 5 mW
const LAMP_I = 9;             // each ceiling downlight (candela)
const W = 0.15;               // degrees of mirror error at which power falls to 1/e
const gauss = (yaw, pitch) => Math.exp(-((yaw * yaw + pitch * pitch) / (W * W)));
const iris = (x) => Math.exp(-Math.pow((x - 12.4) / 2.2, 2) * 0.7);
export function meterReading(st) {
  if (!st.laser.on || st.laser.shutter !== 'open' || st.laser.stuck) return 0;
  return PMAX * gauss(st.m1.yaw + st.m2.yaw, st.m1.pitch + st.m2.pitch) * iris(st.stage.x) * (st.laser.mW / 5) * (st.laser.out ?? 1);
}
// What the meter (tile, room readout, beam camera and every chat line) actually shows.
const shown = (st) => meterReading(st) * (st.meter?.bump ?? 1);
const fmtUW = (v) => (v < 1 ? '0 µW' : `${v.toFixed(0)} µW`);
const readUW = (st) => Math.round(shown(st)); // a reading as a whole µW, for arithmetic on the numbers the log shows
// A move the visitor should watch: the room pings the device with a label and pulses it (build() listens for
// plan.cue; `hold` is ms at normal speed). Skipped while fast-forwarding, so a skip doesn't flash stale labels.
// `text` and `hold` may be functions of the store, for a move whose shape depends on a plan Edit.
const cue = (id, text, hold = 1500) => ({ fn: ({ store, fast }) => { if (!fast) store.set('plan.cue', { id, text: typeof text === 'function' ? text(store) : text, hold: typeof hold === 'function' ? hold(store) : hold }); } });
// A move cut short by a fault: a new label for the device (a same-device ping replaces its move label), without the
// pulse or the ring (R.ping's ring: false), so the eye stays on the fault.
const recue = (id, text, hold = 1500) => ({ fn: ({ store, fast }) => { if (!fast) store.set('plan.cue', { id, text, hold, calm: true }); } });
// A move that has stopped (a scan NCB halted): its label goes now, not when its hold runs out. A faulted device's
// marker stays.
const uncue = (id) => ({ fn: ({ store, fast }) => { if (!fast) store.set('plan.cue', { id, off: true }); } });
const r3 = (v) => Math.round(v * 1000) / 1000;                          // exact positions: r3(0.12 + 0.05) === 0.17
const deg = (y) => `${y >= 0 ? '+' : '−'}${Math.abs(y).toFixed(3)}°`;   // angles, chat and tiles: +0.140°, −0.020° (−0 prints +0.000°)
const px = (v) => `${v < 0 ? '−' : ''}${Math.abs(v)}`;                  // whole pixels, same minus glyph: −4
const pxy = (p) => `(${px(p.x)},\u00A0${px(p.y)})`;                          // "(22, −4)", never broken across two lines
const sgnPct = (v) => `${v >= 0 ? '+' : '−'}${Math.abs(v)}%`;           // step changes: +21%, −2%
// Where the beam camera puts the spot, in px: the numbers its tile and monitor show. M1 and M2 steer the same beam, so
// their angles add (the meter model in 3.2 does the same). M1 is 0 in every clean run, so no existing number changes.
const spotPx = (st) => ({ x: Math.round((st.m1.yaw + st.m2.yaw) * 180), y: Math.round((st.m1.pitch + st.m2.pitch) * 180) });
// A re-plan card whose lines depend on the store (a plan Edit). Silent in a quiet replay, like every card.
const replanWith = (spec) => ({ fn: async ({ chat, store, sleep }) => { chat.replan(spec(store)); await sleep(700); } });

// ── p1: the move the visitor approved (one 0.050° move, or five 0.010° steps with a reading after each). The clean
// run and the scenarios that replay the move share these, so every path makes the same move.
const m2Line = (st, i) => `Step ${i}/5 — yaw ${deg(r3(0.12 + 0.01 * i))} · ${fmtUW(shown(st))}`;
async function m2Step({ store, chat, tween }, i) { await tween('m2.yaw', r3(0.12 + 0.01 * i), 500); chat.ncb(m2Line(store.state, i)); }
const P1_CUE = cue('m2', (st) => `Mirror M2 · yaw +0.120° → +0.170°${st.get('plan.stepped') ? ' · 5 steps' : ''}`, (st) => (st.get('plan.stepped') ? 2600 : 1500));
const P1_STATUS = { status: (st) => (st.get('plan.stepped') ? 'M2 yaw → +0.170° in 0.010° steps' : 'M2 yaw → +0.170°') };
const P1_MOVE = [P1_CUE, P1_STATUS,
  { fn: async (ctx) => { if (ctx.store.get('plan.stepped')) { for (let i = 1; i <= 5; i++) await m2Step(ctx, i); } else await ctx.tween('m2.yaw', 0.17, 1500); } },
  { status: 'M2 yaw at +0.170°' }];

// ── p2: one 0.100 mm step of Stage X, settled 200 ms; returns its log line. scan(from, to) logs steps from..to: the
// chat gets the lines in fives and at `to`; plan.log keeps every logged reading (whole µW) for counts and summaries.
const P2_STATUS = { status: 'Stage X → 12.400 mm in 0.100 mm steps' };   // what the status line says mid-scan
async function stageStep({ store, tween, sleep }, i) {
  const x = r3(10 + i * 0.1); await tween('stage.x', x, 100); await sleep(200);
  return `${x.toFixed(3)} mm → ${fmtUW(shown(store.state))}`;
}
const scan = (from, to) => ({ fn: async (ctx) => {
  const lines = [];
  for (let i = from; i <= to; i++) {
    lines.push(await stageStep(ctx, i));
    ctx.store.set('plan.log', [...(ctx.store.get('plan.log') || []), readUW(ctx.store.state)]);
    if (i % 5 === 0 || i === to) ctx.chat.ncb(lines.splice(0).join('\n'));
  }
} });

// ── p3: the walk. plan.walk carries its state between steps, so a what-if can stop between any two steps and pick
// the numbers up: { base, i, last, pct, bestY, bestP, done: false | 'peak' | 'drop' }.
const walkStart = { fn: ({ store, chat }) => { const p = readUW(store.state); store.set('plan.walk', { base: p, i: 0, last: p, pct: 0, bestY: 0.12, bestP: p, done: false }); chat.ncb(`Baseline ${p} µW at +0.120°.`); } };
const walkStep = (i) => ({ fn: async ({ store, chat, tween }) => {
  const w = store.get('plan.walk'); if (!w || w.done) return;
  const y = r3(0.12 - 0.02 * i); await tween('m2.yaw', y, 450);
  const p = readUW(store.state), pct = Math.round((p / w.last - 1) * 100);
  chat.ncb(`Step ${i}: yaw ${deg(y)} → ${p} µW (${sgnPct(pct)})`);
  store.set('plan.walk', { ...w, ...(p > w.bestP ? { bestY: y, bestP: p } : {}), i, last: p, pct,
    done: p < w.last * 0.8 ? 'drop' : p < w.last * 1.01 ? 'peak' : false });   // the plan's two stop rules
} });
// After the peak: back to the best step. `note` ends NCB's line (what the camera shows, or can't).
const walkBack = (note) => ({ fn: async ({ store, say, tween, fast }) => {
  const w = store.get('plan.walk'); if (w?.done !== 'peak') return;
  if (!fast) store.set('plan.cue', { id: 'm2', text: `Mirror M2 · yaw ${deg(store.get('m2.yaw'))} → ${deg(w.bestY)} (best)`, hold: 1200 });
  await tween('m2.yaw', w.bestY, 450);
  await say(`Step ${w.i} ${w.pct < 0 ? `lost ${-w.pct}%` : `gained only ${w.pct}%`}, so the peak is behind it. Back to the best step: yaw ${deg(w.bestY)}, ${readUW(store.state)} µW, up from ${w.base} µW at the start. ${note}`);
} });

// A mirror mount's tile: the angles its controller reports (m1.reported / m2.reported, when a scenario sets them).
const angles = (s) => `yaw ${deg(s.reported ?? s.yaw)} · pitch ${deg(s.pitch)}`;

export default {
  id: 'lab',
  title: 'Laser lab',
  startHour: 14,
  camera: { position: [1.9, 1.75, 2.4], target: [-0.1, 0.95, -0.2], minDistance: 1.0, maxDistance: 5, azimuth: [-0.5, 1.2], fitAspect: 1.3 },
  setupIntro: 'An optical bench — an illustrative setup, not a specific lab. The hub is at the end of the table. Plug it in to start.',
  askIntro: 'This room takes exact instructions. Give one, or pick one below. I execute each step, then check it against a measurement (the power meter, plus the beam camera where a step needs it) before I call it done.',
  // Order of the tiles (and of discovery). On a phone's strip the last tile a request reveals stays in view: the beam
  // camera next to the laser (both change when the shutter opens, and show together), the meter between M2 and Stage X,
  // so the tilt and walk results show M2 + meter, the stage log meter + Stage X, and the shutter fault the laser.
  deviceOrder: ['laser', 'beamcam', 'm1', 'm2', 'meter', 'stage'],
  devices: {
    laser:  { icon: 'laser', name: 'Laser & shutter', ref: 'the laser', initial: { on: true, shutter: 'closed', mW: 5, stuck: false, out: 1 }, format: (s) => `${s.shutter} · ${s.mW.toFixed(1)} mW set`, faultText: 'not answering', active: (s) => s.shutter === 'open', control: (s) => ({ kind: 'switch', field: 'shutter', on: s.shutter === 'open' }) }, // the laser stays on; the shutter is what changes
    m1:     { icon: 'mirror', name: 'Mirror M1', ref: 'Mirror M1', initial: { yaw: 0.0, pitch: 0.0 }, format: angles, faultText: 'controller offline' },
    m2:     { icon: 'mirror', name: 'Mirror M2', ref: 'Mirror M2', initial: { yaw: 0.12, pitch: -0.02 }, format: angles, faultText: 'controller offline' },
    stage:  { icon: 'stage', name: 'Stage X', ref: 'Stage X', initial: { x: 10.0, limit: 13.0, moving: false }, format: (s) => `${s.x.toFixed(3)} mm · ${s.moving ? 'moving' : s.x >= s.limit - 0.0005 ? 'at limit' : `limit ${s.limit.toFixed(1)} mm`}`, faultText: 'controller offline' },
    meter:  { icon: 'meter', name: 'Power meter', ref: 'the power meter', initial: { bump: 1 }, format: (s, st) => fmtUW(shown(st)), faultText: 'no reading', active: (s, st) => shown(st) > 1 },
    beamcam:{ icon: 'beamcam', name: 'Beam camera', ref: 'the beam camera', initial: { frozen: null },
              format: (s, st) => { const f = s.frozen; return f ? `spot at ${pxy(f)} px` : shown(st) > 1 ? `spot at ${pxy(spotPx(st))} px` : 'no beam'; },
              faultText: 'no image', active: (s, st) => (s.frozen ? s.frozen.p > 1 : shown(st) > 1) },
  },

  // A typed request that doesn't run but asks for what is already so, or asks what state the shutter or laser is in:
  // the honest reply is the room's state, not "I won't guess". main.js asks this first, with interpret()'s answer `r`,
  // for any typed text that doesn't run; null means no such case (its usual reply follows).
  already(r, text, st) {
    const t = String(text ?? '').trim().toLowerCase().replace(/’/g, "'"), closed = st.laser.shutter !== 'open' || st.laser.stuck, now = `the meter reads ${fmtUW(shown(st))}`;
    const either = /\bor\b/.test(t); // "open or closed?": the answer is the state, not a yes or a no
    const q = /^(?:is|are)\s+(?:the\s+)?(?:laser'?s?\s+)?shutter\s+(?:still\s+|already\s+|now\s+)?(open|opened|closed|shut)\b[^,;.!]*\??$/.exec(t);
    if (q) return `${either ? 'The' : `${/^open/.test(q[1]) === !closed ? 'Yes' : 'No'}: the`} shutter is ${closed ? 'closed' : 'open'}, and ${now}.${closed ? ' Pick a request below when you want it open.' : ''}`;
    const l = /^is\s+(?:the\s+)?laser\s+(?:still\s+|already\s+|now\s+)?(on|off)\b[^,;.!]*\??$/.exec(t) || /^(?:the\s+)?laser\s+(?:is|was|seems)\s+(?:still\s+|already\s+|now\s+)?(on|off)\b[^,;!?]*\.?$/.exec(t);
    if (l) return `${/^is\b/.test(t) && !either ? `${(l[1] === 'on') === !!st.laser.on ? 'Yes' : 'No'}: the` : 'The'} laser is ${st.laser.on ? 'on' : 'off'}${closed ? ', with its shutter closed, so no beam reaches the table' : ` and its shutter is open: ${now}`}.`;
    if (/^(?:the\s+)?(?:laser'?s?\s+)?shutter\s+(?:is|was|seems)\s+(?:still\s+|already\s+|now\s+)?(?:open|opened|closed|shut)\b[^,;!?]*\.?$|^(?:i|we|someone|somebody)\s+(?:already\s+|just\s+)?(?:opened|closed|shut)\s+(?:the\s+)?shutter\b[^,;!?]*\.?$/.test(t)) // a statement about it
      return `The shutter is ${closed ? 'closed' : 'open'} here, and ${now}.${closed ? ' Pick a request below when you want it open.' : ''}`;
    if (r?.action === 'refuse' && r.via === 'opposite' && closed) return /\b(?:off|kill)\b|\b(?:turn|switch|shut|power)\s+(?:[a-z']+\s+){0,2}?off\b/.test(t)
      ? 'The shutter is already closed, so no beam reaches the table. The laser itself stays powered: this demo doesn\'t switch it off. Pick a request below when you want the beam.'
      : 'The shutter is already closed, so the beam is blocked. Pick a request below when you want it open.';
    return null;
  },

  build({ R, P, M, THREE, store, parts, speed = 1, quiet }) {
    const isQuiet = typeof quiet === 'function' ? quiet : () => false; // true while a what-if replays the request silently (A2)
    R.lights.sun.intensity = 0; R.lights.sun.castShadow = false; R.lights.hemi.intensity = 0.25; R.lights.fill.intensity = 0.15; R.scene.environmentIntensity = 0.25;
    R.scene.background = new THREE.Color(0x151A1E);
    const dark = new THREE.MeshStandardMaterial({ color: 0x6B7075, roughness: 0.95 });
    const floorMat = new THREE.MeshStandardMaterial({ map: parts.tex(parts.tileTex('#7C8286', '#6F7579'), [3, 3]), roughness: 0.6 });
    P.roomShell({ w: 6, d: 5, h: 3, floorMat, wallMat: dark, skirting: false });
    // overhead lights (dimmed, as in a laser lab): a downlight under each panel. Point lights 15 cm under the ceiling,
    // which takes no shadows, blew it out white around the panels; a wide spot aimed straight down (86° half-angle,
    // soft edge) lights the bench, the floor and the walls, fading toward the top of the walls, and never the ceiling.
    // shadow.focus narrows the shadow frustum to 64° off the axis (bench, floor, shelf, sign and back bench) for sharper
    // shadows of the small optics; beyond it the light is unshadowed.
    const lampMat = new THREE.MeshStandardMaterial({ color: 0xFFFFFF, emissive: 0xFFFFFF, emissiveIntensity: 0.6 }); const lampBoxes = [];
    for (const x of [-1.2, 1.2]) {
      const l = new THREE.SpotLight(0xE8F0FF, LAMP_I, 9, 1.5, 0.35, 1.6); l.position.set(x, 2.95, -0.4); l.target.position.set(x, 0, -0.4); R.scene.add(l.target);
      l.castShadow = true; l.shadow.mapSize.setScalar(R.quality === 'high' ? 2048 : 1024); l.shadow.focus = 0.75; l.shadow.bias = -0.0005; l.shadow.normalBias = 0.01; R.scene.add(l);
      lampBoxes.push(P.box(0.6, 0.03, 0.2, lampMat, x, 2.984, -0.4));
    }
    const lamps = P.group(...lampBoxes);

    // optical table: black breadboard on pneumatic legs
    const plateMat = new THREE.MeshStandardMaterial({ map: parts.tex(parts.holeGridTex(), [3, 1.5]), roughness: 0.55, metalness: 0.3 });
    const tableY = 0.9;
    const plateSide = new THREE.MeshStandardMaterial({ color: 0x2B2E31, roughness: 0.55, metalness: 0.3 }); // the hole grid is on the top only: squeezed onto the 20 cm edges it read as stripes
    P.box(2.6, 0.2, 1.3, [plateSide, plateSide, plateMat, plateSide, plateSide, plateSide], 0, tableY - 0.1, -0.2);
    for (const [x, z] of [[-1.1, -0.7], [1.1, -0.7], [-1.1, 0.3], [1.1, 0.3]]) { P.cyl(0.08, 0.08, 0.72, M.steel, x, 0.36, z, 20); P.cyl(0.11, 0.11, 0.1, M.black, x, 0.05, z, 20); }
    const Y = tableY + 0.1; // beam height

    // laser head
    const laserBody = P.box(0.32, 0.09, 0.09, M.anodized, -1.0, Y, 0.3, 0.008), laserMount = P.box(0.3, 0.036, 0.09, M.black, -1.0, Y - 0.0625, 0.3), laserRail = P.box(0.26, 0.02, 0.03, M.steel, -1.0, tableY + 0.01, 0.3); // body, mount (rail → body), rail
    const laserLed = P.box(0.04, 0.03, 0.03, M.led(0xE0563A), -1.12, Y + 0.02, 0.35); laserLed.material.emissiveIntensity = 0.8; // emission LED (update() darkens it while the laser is off)
    const shutter = P.box(0.01, 0.06, 0.06, M.steel, -0.83, Y, 0.3); // flag in front of the aperture; swings up toward +x (clear of the head, above the beam) when open
    const shutterPivot = new THREE.Group(); shutterPivot.position.set(-0.83, Y + 0.03, 0.3); R.scene.add(shutterPivot); R.scene.remove(shutter); shutter.position.set(0, -0.03, 0); shutterPivot.add(shutter);
    const hinge = P.box(0.014, 0.01, 0.05, M.black, -0.835, Y + 0.033, 0.3); // hinge block: laser face → flag pivot (x −0.842..−0.828, y 1.028..1.038), above the beam

    // mirror mounts (post + plate + round mirror), M1 folds +x → -z, M2 folds -z → -x. The mirror's edge is ground glass
    // (matte): polished like its faces, the 6 mm rim caught a downlight at some orbit angles and bloomed into a white blob.
    const mirFace = new THREE.MeshStandardMaterial({ color: 0xF0F4F8, roughness: 0.05, metalness: 1 }), mirEdge = new THREE.MeshStandardMaterial({ color: 0x8A9096, roughness: 0.6, metalness: 0.3 });
    function mirror(x, z, rotY) {
      const g = new THREE.Group(); g.position.set(x, Y, z); R.scene.add(g);
      const post = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 0.1, 16), M.steel); post.position.y = -0.05; post.castShadow = true; g.add(post);
      const base = new THREE.Mesh(new THREE.CylinderGeometry(0.03, 0.03, 0.01, 20), M.black); base.position.y = -0.1; g.add(base);
      const tilt = new THREE.Group(); tilt.rotation.y = rotY; g.add(tilt);
      const plate = new THREE.Mesh(new THREE.BoxGeometry(0.015, 0.075, 0.075), M.anodized); plate.castShadow = true; tilt.add(plate);
      const mir = new THREE.Mesh(new THREE.CylinderGeometry(0.0254, 0.0254, 0.006, 32), [mirEdge, mirFace, mirFace]); mir.rotation.z = Math.PI / 2; mir.position.x = 0.011; tilt.add(mir); // materials: rim, then both faces
      for (const [dy, dz] of [[0.028, 0.028], [0.028, -0.028], [-0.028, 0.028]]) { const k = new THREE.Mesh(new THREE.CylinderGeometry(0.006, 0.006, 0.02, 12), M.steel); k.rotation.z = Math.PI / 2; k.position.set(-0.015, dy, dz); tilt.add(k); }
      return { group: g, tilt, plate, mir, rotY };
    }
    const m1 = mirror(0.7, 0.3, 3 * Math.PI / 4);       // beam +x arrives, leaves -z (mirror face toward -x -z)
    const m2 = mirror(0.7, -0.45, -3 * Math.PI / 4);    // beam -z arrives, leaves -x

    // The drawn beam. EX exaggerates the mirror angles so a 0.1° move is visible. The last leg runs from M2 (x 0.7,
    // z −0.45) to the meter head's +x face (HEAD_X); an M2 error of e degrees swings its end SPREAD·tan(2·e·EX) sideways:
    // 16 mm at the start (+0.120°), 23 mm at +0.170°, so every yaw the prompts use lands on the head's 60 mm face.
    // beamZ(yaw) is where that leg crosses the stage's plane (x = 0) with M1 aligned.
    const EX = 22, SPREAD = 0.174, HEAD_X = -0.722, RB = 0.004; // RB: beam radius
    const beamZ = (yaw) => -0.45 - (0.7 / (0.7 - HEAD_X)) * Math.tan(THREE.MathUtils.degToRad(2 * yaw * EX)) * SPREAD;

    // translation stage with an iris on it, in the M2 → meter leg
    // Stage X travels ACROSS the M2 → meter leg (along z), carrying the iris; the micrometer at the +z end pushes the
    // carriage toward -z as x grows, drawn at 4 mm per mm. At 12.4 mm the aperture is centred on the drawn beam at M2's
    // starting yaw (+0.120°), where iris() peaks; at 10 mm it sits 9.6 mm off it, so the beam's edge grazes the aperture
    // (RA, the ring's inner edge) while iris() passes 43%. Whatever part of the beam falls outside the aperture stops on
    // the ring (update), so a yaw that walks the beam further out (+0.170°) shows a thinner, clipped beam past the iris.
    const IRIS_Z = (x) => beamZ(0.12) - (x - 12.4) * 0.004, RA = 0.013;
    const STAGE_Z = (IRIS_Z(10) + IRIS_Z(12.4)) / 2; // the base sits under the carriage's whole travel
    const stageBase = P.box(0.1, 0.03, 0.16, M.anodized, 0.0, tableY + 0.015, STAGE_Z); const stageTop = P.box(0.09, 0.02, 0.12, M.steel, 0.0, tableY + 0.04, IRIS_Z(10));
    const micro = new THREE.Group(); micro.position.set(0.0, tableY + 0.015, STAGE_Z + 0.08 + 0.04); // barrel seated 1 cm into the base end, below the carriage
    micro.rotation.x = Math.PI / 2; R.scene.add(micro); // barrel axis along +z, thimble at the outer end
    const barrel = new THREE.Mesh(new THREE.CylinderGeometry(0.006, 0.006, 0.1, 12), M.steel); micro.add(barrel);
    const thimble = new THREE.Group(); thimble.position.y = 0.035; micro.add(thimble);
    const thimbleBody = new THREE.Mesh(new THREE.CylinderGeometry(0.009, 0.009, 0.03, 16), M.steel); thimbleBody.castShadow = true; thimble.add(thimbleBody);
    const tick = new THREE.Mesh(new THREE.BoxGeometry(0.0025, 0.03, 0.002), M.black); tick.position.z = 0.0092; thimble.add(tick); // turns as the stage moves
    const irisPost = new THREE.Mesh(new THREE.CylinderGeometry(0.008, 0.008, 0.032, 12), M.steel); irisPost.position.set(0, tableY + 0.066, IRIS_Z(10)); R.scene.add(irisPost); // stage top → bottom of the ring, clear of the aperture
    const irisRing = new THREE.Mesh(new THREE.TorusGeometry(RA + 0.005, 0.005, 10, 30), M.anodized); irisRing.rotation.y = Math.PI / 2; irisRing.position.set(0, Y, IRIS_Z(10)); R.scene.add(irisRing);

    // power meter head + readout box, beam camera + monitor
    const head = P.box(0.05, 0.06, 0.06, M.black, -0.75, Y, -0.45, 0.005), headPost = P.cyl(0.01, 0.01, 0.1, M.steel, -0.75, tableY + 0.05, -0.45, 12);
    const lightGrey = new THREE.MeshStandardMaterial({ color: 0xC4C9CC, roughness: 0.7 }); // for "white" things under the lamps: pure white glared and bloomed
    const meterBox = P.box(0.16, 0.08, 0.12, lightGrey, -1.0, tableY + 0.04, -0.15, 0.008);
    // the two screens each own one canvas + texture, redrawn in place (no new GPU texture per reading)
    const screenCanvas = (w, h) => { const c = document.createElement('canvas'); c.width = w; c.height = h; const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return { g: c.getContext('2d'), t, w, h }; };
    const mScr = screenCanvas(256, 96), pScr = screenCanvas(320, 200);
    const meterScreen = P.screenPlane(0.12, 0.045, mScr.t); meterScreen.position.set(-1.0, tableY + 0.05, -0.089); meterScreen.material.emissiveIntensity = 0.9;
    // Beam camera: a bare sensor facing a glass pickoff in the M2 → meter leg (after the iris), so it images the same
    // beam the meter reads. The pickoff face normal is (1, 0, -1)/√2: it sends a faint copy toward -z onto the sensor.
    const PKX = -0.3, PKZ = -0.458, SENSOR_Z = -0.578; // pickoff centre (mid-range of the leg at this x, yaw 0 … +0.170°), sensor face
    const pickoff = P.box(0.003, 0.04, 0.05, new THREE.MeshStandardMaterial({ color: 0xCFE6EE, roughness: 0.05, metalness: 0.3, transparent: true, opacity: 0.4 }), PKX, Y, PKZ); pickoff.rotation.y = Math.PI / 4; pickoff.castShadow = false;
    const pkClamp = P.box(0.01, 0.01, 0.05, M.black, PKX, Y - 0.025, PKZ), pkPost = P.cyl(0.006, 0.006, 0.07, M.steel, PKX, tableY + 0.035, PKZ, 12); pkClamp.rotation.y = Math.PI / 4; // clamp under the plate, post
    const beamcamBody = P.box(0.06, 0.05, 0.08, M.black, PKX, Y, -0.62, 0.005), camPost = P.cyl(0.008, 0.008, 0.077, M.steel, PKX, tableY + 0.0385, -0.62, 12);
    const sensorWin = P.box(0.05, 0.04, 0.002, new THREE.MeshStandardMaterial({ color: 0x1A2A3A, roughness: 0.15, metalness: 0.6 }), PKX, Y, SENSOR_Z - 0.0015); // sensor window on the front face
    // monitor on a bench behind the table shows the beam profile
    P.box(1.6, 0.05, 0.6, M.woodLight, -1.4, 0.85, -2.1); P.box(0.05, 0.85, 0.55, M.steel, -2.1, 0.42, -2.1); P.box(0.05, 0.85, 0.55, M.steel, -0.7, 0.42, -2.1);
    const monitor = [P.box(0.55, 0.36, 0.03, M.black, -1.4, 1.12, -2.2, 0.01), P.box(0.12, 0.02, 0.16, M.black, -1.4, 0.885, -2.15), P.box(0.04, 0.065, 0.02, M.black, -1.4, 0.9275, -2.2)]; // panel, foot, neck
    const profile = P.screenPlane(0.5, 0.31, pScr.t); profile.position.set(-1.4, 1.12, -2.184);
    // hub, laptop, safety sign, shelf with boxes
    const hub = P.hub(1.05, tableY, 0.30, { rotY: -0.3 }); R.addPickable(hub.group, 'hub'); P.phone(1.18, tableY + 0.005, 0.06, 0.6);
    P.box(0.3, 0.012, 0.22, M.steel, -1.05, 0.88, -2.0); const lap = P.box(0.3, 0.2, 0.012, M.steel, -1.05, 0.98, -2.11); lap.rotation.x = -0.25;
    const lapScreen = new THREE.Mesh(new THREE.PlaneGeometry(0.27, 0.165), new THREE.MeshStandardMaterial({ color: 0x0E1318, roughness: 0.25, metalness: 0.2 })); lapScreen.position.set(0, 0.007, 0.0068); lap.add(lapScreen); // the lid's inner face is a dark (idle) display
    // laser warning sign, flat on the left wall: hazard triangle with the laser starburst, and the label text
    const signFace = (() => {
      const c = document.createElement('canvas'); c.width = 512; c.height = 352; const g = c.getContext('2d'); const ink = '#15171A', yel = '#E4B53A';
      g.fillStyle = yel; g.fillRect(0, 0, 512, 352); g.strokeStyle = ink; g.lineWidth = 10; g.strokeRect(16, 16, 480, 320);
      const tri = (cx, cy, r) => { g.beginPath(); for (let i = 0; i < 3; i++) { const a = -Math.PI / 2 + (i * 2 * Math.PI) / 3; g.lineTo(cx + r * Math.cos(a), cy + r * Math.sin(a)); } g.closePath(); g.fill(); };
      g.fillStyle = ink; tri(126, 196, 104); g.fillStyle = yel; tri(126, 196, 76);
      g.strokeStyle = ink; g.lineCap = 'round'; g.lineWidth = 5; for (let i = 0; i < 16; i++) { const a = (i * Math.PI) / 8, r2 = i % 2 ? 24 : 32; g.beginPath(); g.moveTo(138 + 14 * Math.cos(a), 206 + 14 * Math.sin(a)); g.lineTo(138 + r2 * Math.cos(a), 206 + r2 * Math.sin(a)); g.stroke(); }
      g.fillStyle = ink; g.beginPath(); g.arc(138, 206, 10, 0, Math.PI * 2); g.fill(); g.lineWidth = 6; g.beginPath(); g.moveTo(128, 206); g.lineTo(78, 206); g.stroke(); // starburst, beam to the left
      const line = (t, y, px, max) => { let s = px; do { g.font = `700 ${s}px "Segoe UI", Inter, Arial, sans-serif`; s -= 2; } while (g.measureText(t).width > max && s > 10); g.fillText(t, 250, y); };
      g.fillStyle = ink; g.textBaseline = 'alphabetic'; line('LASER', 158, 88, 230); line('RADIATION', 222, 50, 230); line('AVOID DIRECT', 272, 26, 230); line('EYE EXPOSURE', 304, 26, 230);
      return parts.tex(c);
    })();
    const signPlate = P.box(0.02, 0.35, 0.5, M.yellow, -2.988, 1.8, -0.5);
    const signPlane = new THREE.Mesh(new THREE.PlaneGeometry(0.48, 0.33), new THREE.MeshStandardMaterial({ map: signFace, roughness: 0.6 })); signPlane.rotation.y = Math.PI / 2; signPlane.position.set(-2.9765, 1.8, -0.5); signPlane.receiveShadow = true; R.scene.add(signPlane);
    const sign = P.group(signPlate, signPlane);
    const shelfParts = [P.box(1.4, 0.03, 0.3, M.steel, 1.9, 1.7, -2.348)]; for (let i = 0; i < 4; i++) shelfParts.push(P.box(0.25, 0.18, 0.2, [M.cardboard, lightGrey, M.black, M.cardboard][i], 1.4 + i * 0.32, 1.805, -2.348, 0.01));
    const shelf = P.group(...shelfParts);
    // Wall-hung decor hides with its wall when the camera orbits outside it (renderer.js owns .visible of tagged parts).
    P.onWall(sign, 1, 0, -3); P.onWall(shelf, 0, 1, -2.5); lamps.userData.wall = { nx: 0, ny: -1, nz: 0, d: -3 }; // ceiling fixtures: visible while the camera is below the 3 m ceiling

    // beam segments (emissive cylinders), rebuilt each frame from the alignment state. The core stays under the bloom
    // threshold: a 1–2 px line just over it bloomed only where it covered whole pixels, which read as a string of beads
    // (desktop) or dashes (phones, no MSAA). The glow is geometry instead: an additive sleeve around each segment that
    // fades from the beam's axis to its edge (brightest where the sleeve faces the camera), a child of the segment so
    // place() and the iris clip scale it too. It also fades out toward a mirror or the pickoff (fade: metres at the start,
    // end; none within the last 45% of that): the sleeve is wider than the mirror plate is thick, and at 45° its end
    // would poke out of the back of the mount.
    const beamMat = new THREE.MeshStandardMaterial({ color: 0xFF2A2A, emissive: 0xFF2020, emissiveIntensity: 1.5, transparent: true, opacity: 0.9 });
    const lastMat = beamMat.clone(); // M2 → iris: dims with meter.bump (something taking power out of the last leg)
    const passMat = beamMat.clone(); // iris → meter: also dims with what the iris passes, iris(stage x), so it brightens as the log climbs
    const pickMat = beamMat.clone(); pickMat.opacity = 0.7; // the pickoff's copy: a few percent of the last leg
    const glowMat = (op, fade) => new THREE.ShaderMaterial({
      uniforms: { color: { value: new THREE.Color(0xFF2A2A) }, opacity: { value: op }, len: { value: 1 }, fade: { value: new THREE.Vector2(...fade) } }, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
      vertexShader: 'varying vec3 vN; varying vec3 vV; varying float vY; void main() { vec4 mv = modelViewMatrix * vec4(position, 1.0); vN = normalMatrix * normal; vV = -mv.xyz; vY = position.y; gl_Position = projectionMatrix * mv; }',
      fragmentShader: 'uniform vec3 color; uniform float opacity; uniform float len; uniform vec2 fade; varying vec3 vN; varying vec3 vV; varying float vY; void main() { float f = abs(dot(normalize(vN), normalize(vV))); f *= f; float a = fade.x > 0.0 ? smoothstep(0.45 * fade.x, fade.x, (vY + 0.5) * len) : 1.0; float b = fade.y > 0.0 ? smoothstep(0.45 * fade.y, fade.y, (0.5 - vY) * len) : 1.0; gl_FragColor = vec4(color * (opacity * f * f * a * b), 1.0); }',
    });
    const GLOW_R = 4.5, GLOW_OP = 0.5; // sleeve radius (× the beam's) and its brightness on the axis
    // Without MSAA (phones, the low tier, hi-dpi screens) a sub-pixel core cylinder covers pixel centres only in runs, so a
    // dim or clipped leg broke into dashes: a 1-px line down each segment's axis rasterises unbroken at any width.
    const lineGeo = new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, -0.5, 0), new THREE.Vector3(0, 0.5, 0)]);
    const noMsaa = () => R.quality !== 'high' || (globalThis.devicePixelRatio || 1) >= 1.5; // as renderer.js decides MSAA
    const seg = (mat, fade, r = RB, glow = 1) => {
      const m = new THREE.Mesh(new THREE.CylinderGeometry(r, r, 1, 8), mat); R.scene.add(m);
      const g = new THREE.Mesh(new THREE.CylinderGeometry(r * GLOW_R, r * GLOW_R, 1, 20, 1, true), glowMat(GLOW_OP * glow, fade)); g.userData.noAO = true; g.userData.op = GLOW_OP * glow; m.add(g); m.userData.glow = g;
      const ln = new THREE.Line(lineGeo, new THREE.LineBasicMaterial({ color: 0xFF3A3A, transparent: true, opacity: 0.85 * glow, depthWrite: false })); ln.userData.noAO = true; ln.userData.op = 0.85 * glow; ln.raycast = () => {}; m.add(ln); m.userData.line = ln;
      return m;
    };
    const segs = [seg(beamMat, [0.01, 0.025]), seg(beamMat, [0.025, 0.025]), seg(lastMat, [0.025, 0]), seg(passMat, [0, 0.01]), seg(pickMat, [0.02, 0.005], 0.0022, 0.6)]; // laser → M1, M1 → M2, M2 → iris, iris → meter, pickoff copy
    const glowAt = (i, k, lines) => { const { glow: g, line: l } = segs[i].userData; g.material.uniforms.opacity.value = g.userData.op * k; l.visible = lines; l.material.opacity = l.userData.op * Math.min(1, k); };
    const spot = new THREE.Mesh(new THREE.SphereGeometry(0.012, 12, 12), new THREE.MeshStandardMaterial({ color: 0xFF6060, emissive: 0xFF3030, emissiveIntensity: 6 })); R.scene.add(spot);
    const camSpot = new THREE.Mesh(new THREE.SphereGeometry(0.0035, 10, 10), spot.material.clone()); R.scene.add(camSpot); // where the copy lands on the sensor
    const a = new THREE.Vector3(), b = new THREE.Vector3(), mid = new THREE.Vector3(), dir = new THREE.Vector3(), up = new THREE.Vector3(0, 1, 0);
    function place(m, ax, ay, az, bx, by, bz, r = 1) { a.set(ax, ay, az); b.set(bx, by, bz); mid.addVectors(a, b).multiplyScalar(0.5); dir.subVectors(b, a); const len = dir.length(); m.position.copy(mid); m.scale.set(r, len, r); m.quaternion.setFromUnitVectors(up, dir.normalize()); m.userData.glow.material.uniforms.len.value = len; }
    place(segs[0], -0.838, Y, 0.3, 0.7, Y, 0.3); // laser face (x −0.84) → M1 never moves; starts 2 mm out, so its end isn't coplanar with the face
    let flagRot = 0, lastT = null; // the shutter flag's drawn angle, easing toward open (1.3 rad) or closed (0)

    // Both screens redraw only when what they show changes (numeric keys, so no strings are built per frame).
    let shownMeter = NaN, spotX = NaN, spotY = NaN, spotP = NaN, camFrame = 0; // camFrame: the beam camera's frame counter
    function drawMeter(offline, p) { const key = offline ? -2 : p < 1 ? -1 : Math.round(p); if (shownMeter === key) return; shownMeter = key; const text = offline ? '----' : fmtUW(p); const g = mScr.g; g.fillStyle = '#0B0F14'; g.fillRect(0, 0, 256, 96); g.fillStyle = '#7CF0D8'; g.font = '44px "Segoe UI", Inter, sans-serif'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(text, 128, 48); mScr.t.needsUpdate = true; }
    function drawProfile(cx, cy, p, mode, frame) {
      const noImage = mode === 'noImage', frozen = mode === 'frozen';
      const kx = Math.round(cx), ky = Math.round(cy), kp = (noImage ? -1 : p > 1 ? 1 + Math.round(p / 50) : 0) + (frozen ? 1000 : 0) + frame * 2000;
      if (kx === spotX && ky === spotY && kp === spotP) return; spotX = kx; spotY = ky; spotP = kp;
      const g = pScr.g; g.fillStyle = '#0A0D12'; g.fillRect(0, 0, 320, 200); g.strokeStyle = '#1F2A33';
      for (let x = 0; x < 320; x += 20) { g.beginPath(); g.moveTo(x, 0); g.lineTo(x, 200); g.stroke(); }
      for (let y = 0; y < 200; y += 20) { g.beginPath(); g.moveTo(0, y); g.lineTo(320, y); g.stroke(); }
      g.strokeStyle = '#3A4A56'; g.beginPath(); g.moveTo(160, 0); g.lineTo(160, 200); g.moveTo(0, 100); g.lineTo(320, 100); g.stroke();
      if (p > 1 && !noImage) { const r = 10 + 22 * Math.min(1, p / PMAX); const gr = g.createRadialGradient(160 + cx, 100 + cy, 1, 160 + cx, 100 + cy, r); gr.addColorStop(0, '#FFFFFF'); gr.addColorStop(0.25, '#FF6A3D'); gr.addColorStop(1, 'rgba(120,20,20,0)'); g.fillStyle = gr; g.beginPath(); g.arc(160 + cx, 100 + cy, r, 0, Math.PI * 2); g.fill(); }
      g.font = '16px Inter, "Segoe UI", sans-serif'; g.textAlign = 'left'; g.fillStyle = frozen ? '#E0563A' : '#7CF0D8';
      g.fillText(noImage ? 'no image' : frozen ? 'frozen · last frame' : p > 1 ? `centroid ${px(kx)}, ${px(ky)} px` : 'no beam', 10, 188);
      if (frame >= 0) { g.fillStyle = '#7CF0D8'; g.font = '14px Inter, "Segoe UI", sans-serif'; g.textAlign = 'right'; g.fillText(`frame ${frame}`, 310, 20); g.textAlign = 'left'; }
      pScr.t.needsUpdate = true;
    }

    // Rings and labels anchor on the first part registered for a device: the beam camera's sit on the camera body on
    // the table, also when the visitor taps the monitor on the back bench that shows its image; the meter's on its head.
    R.addPickable(laserBody, 'laser'); R.addPickable(shutterPivot, 'laser'); R.addPickable(m1.group, 'm1'); R.addPickable(m2.group, 'm2'); R.addPickable(stageBase, 'stage'); R.addPickable(stageTop, 'stage'); R.addPickable(head, 'meter'); R.addPickable(meterBox, 'meter'); R.addPickable(beamcamBody, 'beamcam'); R.addPickable(profile, 'beamcam');
    R.addPickable(micro, 'stage');
    // Every other visible part of a device answers a tap too (after the anchors above, so rings and labels stay put).
    for (const o of [laserMount, laserRail, laserLed, hinge]) R.addPickable(o, 'laser');
    R.addPickable(irisPost, 'stage'); R.addPickable(irisRing, 'stage'); R.addPickable(headPost, 'meter');
    for (const o of [pickoff, pkClamp, pkPost, camPost, sensorWin, ...monitor]) R.addPickable(o, 'beamcam');
    // Status LEDs on the actuated devices (green once found, blinking red on a fault, dark while offline): a dead mount
    // or stage doesn't look any different otherwise. 1 cm beads, about 8 px on a desktop at the default view.
    const statusLeds = {}, ledGeo = new THREE.SphereGeometry(0.01, 14, 14);
    const addLed = (id, parent, x, y, z) => { const m = new THREE.Mesh(ledGeo, new THREE.MeshStandardMaterial({ color: 0x2FBF71, emissive: 0x2FBF71, emissiveIntensity: 0 })); m.position.set(x, y, z); parent.add(m); statusLeds[id] = m; R.addPickable(m, id); };
    addLed('laser', R.scene, -1.06, Y + 0.02, 0.347);              // on the head's front face (z 0.345), clear of the emission LED (x −1.14…−1.10)
    addLed('m1', m1.group, 0.01, -0.09, 0.026);                     // on the mount base's rim, camera side (the group: it doesn't tilt), clear of the post
    addLed('m2', m2.group, 0.01, -0.09, 0.026);
    addLed('stage', R.scene, 0.05, tableY + 0.015, STAGE_Z + 0.05); // on the base's +x face (camera side), under the carriage at every x
    const HL = { laser: [laserBody], m1: [m1.plate], m2: [m2.plate], stage: [stageBase, stageTop, barrel, thimbleBody, irisPost, irisRing], meter: [head, meterBox], beamcam: [beamcamBody] }; // the meter pulses where its ring is (head) and its readout; the stage with the iris it carries
    const hi = P.highlighter(HL);
    // The pulse is cyan, except right after a device faults: main.js focuses it then, under a red ring and label, and
    // the pulse goes red with them. (The shells are the highlighter's children of each mesh; one material per device.)
    const shellMat = {}; for (const [id, ts] of Object.entries(HL)) for (const t of ts) t.traverse((o) => { if (o.userData.isShell && !shellMat[id]) shellMat[id] = o.material; });
    const shells = {}; for (const [id, ts] of Object.entries(HL)) for (const t of ts) t.traverse((o) => { if (o.userData.isShell) (shells[id] ??= []).push(o); });
    let faulted = null;
    const focus = (id, hex) => { const red = hex === 0xE0563A || faulted === id; faulted = null; hi.focus(id, hex); shellMat[id]?.color.setHex(red ? 0xE0563A : 0x29EEE5); };
    // A move step names the device it's about to move (see cue()): pulse it and ping it with the move, so the eye goes
    // where the tiny motion is (a 0.05° tilt or a 2 mm stage move is only a few pixels from the default view). recue()
    // only relabels (calm: no pulse, no ring); uncue() takes the label away (off).
    store.subscribe((st, path, v) => {
      if (isQuiet()) return; // a what-if's silent replay rings and pulses nothing
      if (path === 'plan.cue' && v) {
        if (v.off) { if (store.state[v.id]?.status !== 'fault') R.unping(v.id); return; }
        if (!v.calm) focus(v.id);
        R.ping(v.id, v.text, { hold: Math.max(1200, v.hold / (speed || 1)), ring: !v.calm });
      }
      if (v === 'fault' && path.endsWith('.status')) faulted = path.slice(0, -7); // main.js's fault handler focuses it next
    });

    return {
      focus,
      update(s, t) {
        hi.update();
        for (const id in statusLeds) {
          const led = statusLeds[id], st = s[id].status, mode = st === 'fault' ? 'fault' : st === 'offline' ? 'off' : 'ok';
          if (led.userData.mode !== mode) { led.userData.mode = mode; const hex = mode === 'fault' ? 0xE0563A : 0x2FBF71; led.material.color.setHex(hex); led.material.emissive.setHex(hex); }
          led.material.emissiveIntensity = mode === 'fault' ? (Math.sin(t * Math.PI * 4) > 0 ? 2.2 : 0.15) : mode === 'ok' ? 0.9 : 0; // 2 Hz blink on a fault
        }
        if (laserLed.userData.on !== s.laser.on) { laserLed.userData.on = s.laser.on; laserLed.material.color.setHex(s.laser.on ? 0xE0563A : 0x3A2622); laserLed.material.emissiveIntensity = s.laser.on ? 0.8 : 0; } // the emission LED: dark while the laser emits nothing
        // A faulted device keeps a faint steady red shell after the fault's pulse: the lasting cue on a phone, where the
        // LEDs are a few pixels. hi.update() has just set this frame's pulse; this keeps it red and raises it to a floor.
        for (const id in shells) if (s[id].status === 'fault') { shellMat[id].color.setHex(0xE0563A); if (shellMat[id].opacity < 0.16) { shellMat[id].opacity = 0.16; for (const o of shells[id]) o.visible = true; } }
        hub.ledMat.emissiveIntensity = s.hub.status === 'on' ? (2.5 + Math.sin(t * 2.2) * 1.5) * s.hub.led : 0;
        const flagUp = s.laser.shutter === 'open' && !s.laser.stuck; // a stuck flag hangs in the beam whatever the driver says
        // The flag swings (about 0.25 s) instead of jumping. The drawn beam follows the drawn flag: below about 1.05 rad
        // (60°) the 6 cm flag still crosses the beam, 3 cm under its hinge. The meter reads the store, not the drawing.
        const tgt = flagUp ? 1.3 : 0, dt = lastT === null ? 1 : Math.min(0.25, Math.max(0, t - lastT)); lastT = t;
        flagRot += (tgt - flagRot) * (1 - Math.exp(-dt * 14)); if (Math.abs(tgt - flagRot) < 0.002) flagRot = tgt;
        shutterPivot.rotation.z = flagRot;
        // +yaw turns a mount clockwise seen from above, which is the way the drawn beam moves (hx toward +x, ez toward -z)
        m1.tilt.rotation.y = m1.rotY - THREE.MathUtils.degToRad(s.m1.yaw * EX); m1.tilt.rotation.z = THREE.MathUtils.degToRad(s.m1.pitch * EX);
        m2.tilt.rotation.y = m2.rotY - THREE.MathUtils.degToRad(s.m2.yaw * EX); m2.tilt.rotation.z = THREE.MathUtils.degToRad(s.m2.pitch * EX);
        const sz = IRIS_Z(s.stage.x); stageTop.position.z = sz; irisPost.position.z = sz; irisRing.position.z = sz; thimble.rotation.y = Math.PI + (s.stage.x - 10) * Math.PI * 4; // 0.5 mm pitch; tick on top at 10 mm
        const p = shown(s); const live = s.laser.on && flagRot > 1.05;
        // beam: laser → M1 → M2 → iris → meter head, with the M2 error steering the last leg (SPREAD). It ends on the
        // head's +x face: centred when aligned, off centre at the start (+0.120°), near the edge at +0.170°.
        const e2y = THREE.MathUtils.degToRad(s.m2.yaw * EX) * 2, e2p = THREE.MathUtils.degToRad(s.m2.pitch * EX) * 2, e1 = THREE.MathUtils.degToRad(s.m1.yaw * EX) * 2;
        const hx = 0.7 + Math.tan(e1) * 0.75; // where the beam meets M2 (z −0.45)
        const ey = Y + Math.tan(e2p) * SPREAD, ez = -0.45 - Math.tan(e2y + e1) * SPREAD, dx = HEAD_X - hx, dy = ey - Y, dz = ez + 0.45; // end on the head, leg direction
        // Iris (x = 0): the part of the beam outside the aperture stops on the ring; what gets through goes on as the
        // largest disc inside the overlap of beam and aperture (radius rr, pulled toward the axis by cut), so no beam is
        // ever drawn through the metal. With the beam well inside the aperture nothing changes.
        const ti = -hx / dx, iy = Y + ti * dy, iz = -0.45 + ti * dz, oy = iy - Y, oz = iz - sz, on = Math.hypot(oy, oz);
        const rr = on + RB <= RA ? RB : Math.max(0, (RA - on + RB) / 2), cut = rr === RB ? 0 : (RA + on - RB) / 2 - on; // cut ≤ 0
        const cy = on ? (oy / on) * cut : 0, cz = on ? (oz / on) * cut : 0, py = iy + cy, pz = iz + cz, k2 = rr / RB;
        place(segs[1], 0.7, Y, 0.3, hx, Y, -0.45); place(segs[2], hx, Y, -0.45, 0, iy, iz); place(segs[3], 0, py, pz, HEAD_X, ey + cy, ez + cz, k2);
        // pickoff: where the passing beam crosses the plate (x − z = PKX − PKZ), and its reflection about (1, 0, -1)/√2, (dx, dy, dz) → (dz, dy, dx)
        const tp = (PKX - PKZ + pz) / (dx - dz), qx = tp * dx, qy = py + tp * dy, qz = pz + tp * dz, sk = (SENSOR_Z - qz) / dx; // dx < 0 and so is the copy's z step
        place(segs[4], qx, qy, qz, qx + dz * sk, qy + dy * sk, SENSOR_Z, k2); camSpot.position.set(qx + dz * sk, qy + dy * sk, SENSOR_Z); camSpot.scale.setScalar(k2);
        spot.position.set(HEAD_X, ey + cy, ez + cz); spot.scale.setScalar(k2);
        const through = live && rr > 0.0002; // something gets past the iris
        segs[0].visible = segs[1].visible = segs[2].visible = live; segs[3].visible = segs[4].visible = spot.visible = camSpot.visible = through;
        // Brightness: the laser's power (f), what reaches the last leg (k, meter.bump) and what the iris passes (kp): the
        // beam past the iris and its spots track the logged rise as the stage opens the iris (43% at 10 mm, 98% at 12 mm).
        const out = s.laser.out ?? 1, f = (0.4 + 0.6 * Math.min(1, s.laser.mW / 5)) * out * out * out, k = Math.max(0.35, Math.min(1, s.meter.bump)), kp = k * iris(s.stage.x);
        beamMat.emissiveIntensity = 1.5 * f; lastMat.emissiveIntensity = 1.5 * f * k; passMat.emissiveIntensity = 1.5 * f * kp; pickMat.emissiveIntensity = passMat.emissiveIntensity * 0.45;
        const lines = noMsaa(); glowAt(0, f, lines); glowAt(1, f, lines); glowAt(2, f * k, lines); glowAt(3, f * kp, lines); glowAt(4, f * kp, lines);
        spot.material.emissiveIntensity = 6 * (0.5 + 0.5 * kp); camSpot.material.emissiveIntensity = 3 * kp; // the head's spot keeps enough to glow
        drawMeter(s.meter.status === 'offline' || s.meter.status === 'fault', p);
        // The monitor: live; a frozen frame (the stale picture, and its counter, until the camera faults: then "frozen ·
        // last frame" in red); or "no image". The counter runs at 5 frames a second while the camera sends pictures.
        const fr = s.beamcam.frozen, sp = spotPx(s), bc = s.beamcam.status, mode = bc === 'fault' ? (fr ? 'frozen' : 'noImage') : 'live';
        if (!fr && bc !== 'fault') camFrame = Math.floor(t * 5);
        drawProfile(fr ? fr.x : sp.x, fr ? fr.y : sp.y, fr ? fr.p : bc === 'offline' ? 0 : p, mode, bc === 'offline' || mode === 'noImage' ? -1 : camFrame);
      },
    };
  },

  prompts: [
    {
      chip: 'Open the shutter at 5.0 mW and hold. Confirm beam on the meter.',
      keywords: ['open', 'shutter', 'mw', 'hold', 'confirm', 'beam', 'laser', 'on', 'power'],
      rulesOut: ['increase', 'raise', 'reduce', 'decrease', 'lower', 'boost'], // power stays at 5.0 mW: "increase the laser power" is not this
      expect: { 'laser.shutter': 'open', 'laser.stuck': false, 'laser.status': 'online' },
      genericAt: 'hold',
      steps: [
        { beat: 'plan' },
        { say: 'Exact instruction, so here it is back to you as steps. I\'ll verify each one against the meter and the beam camera, not just the device\'s own "ok".' },
        { status: 'Procedure ready · waiting for Execute' },
        { plan: { intro: 'Procedure:', approve: 'Execute', steps: [
          { text: 'Set the laser to 5.0 mW (it is at 5.0 mW now — no change)' },
          { text: 'Open the shutter' },
          { text: 'Verify: meter reads > 100 µW within 2 s and the beam camera sees a spot. If not, close the shutter and stop.' },
          { text: 'Hold and report every 30 s', alt: { text: 'Hold and report only on change', apply: [{ set: 'plan.quiet', to: true }] } },
        ] } },
        { beat: 'run' },
        { status: 'Step 1 · power' }, { say: 'Step 1 — power is already 5.0 mW, so no change. That\'s the driver\'s number, not a measurement yet.' },
        { failPoint: 'open' },
        { set: 'laser.shutter', to: 'open', label: 'Step 2 · shutter → open' }, { wait: 700 },
        { status: 'Step 3 · verify on the meter and camera' }, { wait: 600 },
        { fn: ({ chat, store }) => chat.ncb(`Step 2 — shutter open. Step 3 — meter ${fmtUW(shown(store.state))}, above the 100 µW check, and the camera sees a spot. Verified. Holding, and reporting ${store.get('plan.quiet') ? 'only on change' : 'every 30 s'}.`) },
        { status: 'Step 4 · hold' }, { wait: 1500 },
        { status: 'Step 4 · hold · 00:30' },
        { fn: ({ chat, store }) => { if (!store.get('plan.quiet')) chat.ncb(`00:30 — ${fmtUW(shown(store.state))}, spot steady.`); } }, { wait: 1200 },
        { failPoint: 'hold' },
        { status: 'Step 4 · hold · 01:00' },
        { fn: ({ say, store }) => say(store.get('plan.quiet')
            ? `Still holding at 01:00: ${fmtUW(shown(store.state))} on the meter, spot steady on the camera. Nothing has changed, so there's been nothing to report. If the meter or the camera loses the beam, I close the shutter and tell you.`
            : `01:00 — ${fmtUW(shown(store.state))}, spot steady. Between reports I keep checking the meter and the camera. If either loses the beam, I close the shutter and tell you.`) },
        { status: (st) => `Holding at 5.0 mW · meter ${fmtUW(shown(st.state))}` },
        { end: { headline: 'Beam on, verified, and held.', body: 'NeuCharBox opened the shutter at the set 5.0 mW, confirmed the beam on the meter and the camera instead of trusting the driver, and kept checking while it held.' } },
      ],
      whatIf: {
        laser: { at: 'hold', intro: 'Replaying this request. This time, the shutter flag drops back into the beam during the hold.', steps: [
          { status: 'Step 4 · hold · 00:50' }, { wait: 1500 },
          { set: 'laser.stuck', to: true },     // the flag drops back into the beam; the driver still says "open" (tile stays "open · 5.0 mW set")
          { status: '01:00 · meter 0 µW, shutter still reports open' },
          { fn: ({ chat }) => chat.alert('01:00 — meter dropped to 0 µW and the camera lost the spot, but the shutter still reports "open".', '⚠ Beam lost, shutter says open') }, { wait: 600 },
          { say: 'The device says one thing and the measurement says another. I trust the measurement. Closing the shutter and stopping — I won\'t hold a beam I can\'t see.' },
          { set: 'laser.shutter', to: 'closed', label: 'Shutter → closed (safe state)' }, { wait: 700 },
          { fail: 'laser', faultText: 'closed · inspect flag' },     // after the close, so the red fault line stays up
          { wait: 1200 },
          { say: 'Checked again with the shutter commanded closed: meter 0 µW, camera no beam. I can\'t see the flag from here, so I\'m treating the shutter as unverified.' },
          { replan: { intro: 'Stopped in a safe state:', changes: ['Shutter commanded closed; driver reports closed, meter stays at 0 µW', 'Laser stays powered but blocked', 'Nothing else on the table was touched'], needsYou: 'The shutter flag may be sticking. Inspect it before the next run; I won\'t reopen it on my own.' } },
          { wait: 1200 },
          { end: { headline: 'It checked, and it stopped.', body: 'A device reporting "open" isn\'t the same as a beam on the meter. NeuCharBox verified against the measurement, disagreed with the device, and put the bench in a safe state instead of guessing.' } },
        ] },
        meter: { at: 'open', intro: 'Replaying this request. This time, the power meter goes silent just as the shutter opens.', steps: [
          { status: 'Step 2 · shutter → open' },
          { parallel: [                                                     // together: the meter never shows a reading of this beam
            { set: 'laser.shutter', to: 'open' },                           // no label: it would overwrite the red fault line
            { fail: 'meter', faultText: 'no reading' },                     // after the shutter: the fault's tile pop drops its reveal
          ] },
          { wait: 700 },                                                    // Step 3 checks 1.3 s after the shutter opens, as in the clean run
          { fn: ({ chat }) => chat.alert('Step 3: the camera sees a spot, but the power meter isn\'t sending readings. Its display shows dashes.', '⚠ No meter reading') },
          { wait: 1400 },
          { say: 'Still nothing after 2 s. That fails Step 3, and the plan says close the shutter and stop.' },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { say: 'Shutter closed: the driver reports closed, and the camera confirms no beam. The meter can\'t confirm anything right now, so the camera is my check.' },
          { wait: 1200 },
          { say: 'I asked the meter for a reading twice more with the shutter closed: no answer at all, not even a zero. It\'s the meter, not the beam.' },
          { replan: { intro: 'Stopped at Step 3:', changes: ['Shutter opened, then closed when Step 3 failed; the camera confirms no beam', 'Laser still at 5.0 mW, blocked by the shutter', 'Hold not started'], needsYou: 'The power meter isn\'t reporting. Check its head cable and power; once it reads again, I can run the procedure from Step 1.' } },
          { wait: 1200 },
          { end: { headline: 'Step 3 failed, so the shutter closed.', body: 'The plan said: verify on the meter and the camera, or close the shutter and stop. With the meter silent, Step 3 couldn\'t pass, so NeuCharBox closed the shutter instead of holding a beam nobody had verified.' } },
        ] },
        beamcam: { at: 'hold', intro: 'Replaying this request. This time, the beam camera stops sending images during the hold.', steps: [
          { status: 'Step 4 · hold · 00:40' }, { wait: 1500 },
          { fail: 'beamcam', faultText: 'no image', title: '⚠ Camera down', say: '00:40 — the beam camera stopped sending images. The power meter still reads 194 µW, so the beam hasn\'t gone anywhere: the camera has lost its picture, not the beam.' },
          { say: 'You asked me to confirm the beam on the meter, and the meter still does. The camera was my second check, so I\'m holding on the meter. If it drops below 100 µW or goes quiet, I close the shutter.' },
          { wait: 1200 },
          { fn: ({ say, store }) => (store.get('plan.quiet') ? null : say(`01:00 — ${fmtUW(shown(store.state))} on the meter. Camera still down.`)) },
          { status: (st) => `Holding at 5.0 mW · meter ${fmtUW(shown(st.state))} · beam camera down` },
          { replan: { intro: 'Holding, with one check fewer:', changes: ['Beam camera flagged: no images since 00:40', 'Hold continues at 5.0 mW, checked on the meter', 'If the meter drops below 100 µW or goes quiet, I close the shutter'], needsYou: 'Check the beam camera. I\'ll add it back to the checks when it sends images again.' } },
          { wait: 1200 },
          { end: { headline: 'The camera failed, not the beam.', body: 'NeuCharBox told a failed camera apart from a lost beam: the meter still read 194 µW. It kept the hold going on the measurement you asked for, and said exactly what had changed.' } },
        ] },
        // M1, M2 and Stage X sit in the held beam's path, so they aren't 'unused': a dead controller is checked against
        // the beam itself (meter + camera) instead of being waved through by the engine's generic reply.
        m1: { at: 'hold', intro: "Replaying this request. This time, Mirror M1's controller drops off during the hold.", steps: [
          { status: 'Step 4 · hold · 00:40' }, { wait: 1500 },
          { fail: 'm1', faultText: 'controller offline', title: '⚠ M1 offline', say: "00:40 — Mirror M1's controller stopped answering. The meter still reads 194 µW and the spot is still at (22, −4) px, so M1 is holding its angle." },
          { say: "This request never moves M1, so the hold goes on with the same checks. If the meter or the camera loses the beam, I close the shutter." },
          { wait: 1200 },
          { fn: ({ say, store }) => (store.get('plan.quiet') ? null : say(`01:00 — ${fmtUW(shown(store.state))}, spot steady. M1 still offline.`)) },
          { status: (st) => `Holding at 5.0 mW · meter ${fmtUW(shown(st.state))} · M1 flagged` },
          { replan: { intro: 'Holding, with M1 flagged:', changes: ['Mirror M1: controller offline since 00:40; the beam shows it holding its angle', 'Beam still verified on the meter and the camera', 'Hold continues at 5.0 mW'], needsYou: "Check M1's controller before the next alignment. Until it answers, I won't run a request that moves M1." } },
          { wait: 1200 },
          { end: { headline: "The beam proved M1 hadn't moved.", body: "A silent controller isn't a moved mirror. NeuCharBox checked the beam on the meter and the camera, kept the hold you asked for, and flagged M1 for later." } },
        ] },
        m2: { at: 'hold', intro: "Replaying this request. This time, Mirror M2's controller goes silent during the hold.", steps: [
          { status: 'Step 4 · hold · 00:45' }, { wait: 1500 },
          { fail: 'm2', faultText: 'controller offline', title: '⚠ M2 offline', say: "00:45 — Mirror M2's controller stopped answering. Its last report was +0.120° yaw, and nothing in this request moves it." },
          { say: "I checked the beam rather than the mount: 194 µW on the meter, spot still at (22, −4) px. M2 hasn't moved, so the hold goes on." },
          { wait: 1200 },
          { fn: ({ say, store }) => (store.get('plan.quiet') ? null : say(`01:00 — ${fmtUW(shown(store.state))}, spot steady. M2 still offline.`)) },
          { status: (st) => `Holding at 5.0 mW · meter ${fmtUW(shown(st.state))} · M2 flagged` },
          { replan: { intro: 'Holding, with M2 flagged:', changes: ['Mirror M2: controller offline since 00:45; last report +0.120° yaw', 'Meter and camera still confirm the beam', 'Hold continues at 5.0 mW'], needsYou: "Restart M2's controller before any request that tilts it. When it's back, I'll check that it still reports +0.120°." } },
          { wait: 1200 },
          { end: { headline: 'M2 offline, beam still verified.', body: "NeuCharBox didn't guess whether a silent mirror had moved: it checked the beam itself, kept the hold going, and flagged M2." } },
        ] },
        stage: { at: 'hold', intro: "Replaying this request. This time, Stage X's controller drops out during the hold.", steps: [
          { status: 'Step 4 · hold · 00:50' }, { wait: 1500 },
          { fail: 'stage', faultText: 'controller offline', title: '⚠ Stage X offline', say: "00:50 — Stage X's controller stopped answering. Its last report was 10.000 mm, and the meter still reads 194 µW, so the iris on it hasn't moved." },
          { say: "This request doesn't move the stage, and the beam through the iris is unchanged, so I'm holding on the same checks." },
          { wait: 1200 },
          { fn: ({ say, store }) => (store.get('plan.quiet') ? null : say(`01:00 — ${fmtUW(shown(store.state))}, spot steady. Stage X still offline.`)) },
          { status: (st) => `Holding at 5.0 mW · meter ${fmtUW(shown(st.state))} · Stage X flagged` },
          { replan: { intro: 'Holding, with Stage X flagged:', changes: ['Stage X: controller offline since 00:50; last report 10.000 mm', 'Beam still verified: meter and camera unchanged', 'Hold continues at 5.0 mW'], needsYou: "Check Stage X's controller. When it answers, I'll confirm it still reads 10.000 mm before anything moves it." } },
          { wait: 1200 },
          { end: { headline: "The stage dropped off. The hold didn't.", body: 'NeuCharBox checked that the iris had stayed put by reading the beam, kept the hold going, and flagged Stage X instead of stopping the bench.' } },
        ] },
      },
    },
    {
      chip: 'Tilt M2 by +0.050° in yaw, then report the power meter.',
      keywords: ['tilt', 'm2', 'yaw', 'degrees', '0.05', 'report', 'meter', 'mirror', 'rotate', 'adjust'],
      expect: { 'm2.yaw': 0.17, 'laser.shutter': 'open' },
      genericAt: 'report',
      steps: [
        { beat: 'plan' },
        { say: 'M2 yaw is at +0.120°. You\'re asking for +0.170°. I\'ll open the shutter first so there\'s something to measure.' },
        { status: 'Procedure ready · waiting for Execute' },
        { plan: { intro: 'Procedure:', approve: 'Execute', steps: [
          { text: 'Open the shutter and take a baseline meter reading' },
          { text: 'Move mirror M2 yaw from +0.120° to +0.170° in one 0.050° step', alt: { text: 'Move mirror M2 yaw in five 0.010° steps, reading the meter after each', apply: [{ set: 'plan.stepped', to: true }] } },
          { text: 'Report the meter before and after. Do not touch M1 or the stage.' },
        ] } },
        { beat: 'run' },
        { set: 'laser.shutter', to: 'open', label: 'Shutter → open' }, { wait: 800 },
        { fn: ({ chat, store }) => { store.set('plan.base', shown(store.state)); store.set('plan.spot0', spotPx(store.state).x); chat.ncb(`Baseline: ${fmtUW(store.get('plan.base'))} at M2 yaw +0.120°.`); } }, // + plan.spot0
        { failPoint: 'move' },
        ...P1_MOVE,
        { failPoint: 'report' },
        { fn: ({ say, store }) => say(`Done${store.get('plan.stepped') ? ', in five 0.010° steps' : ''}. M2 yaw +0.170°: the encoder confirms it, and the camera shows the spot moved from ${store.get('plan.spot0')} to ${spotPx(store.state).x} px, which is what a 0.050° move gives.`) },
        { fn: ({ say, store }) => say(`Meter ${fmtUW(shown(store.state))}, from ${fmtUW(store.get('plan.base'))} at +0.120°. The spot is further from centre now, so less of the beam reaches the meter. M1 and the stage weren't touched.`) },
        { status: (st) => `M2 yaw +0.170° · meter ${fmtUW(shown(st.state))}` },
        { end: { headline: 'Moved exactly. Reported plainly.', body: 'NeuCharBox made the 0.050° move you asked for, confirmed it on the encoder and the beam camera, and reported the meter before and after. What the number means for your experiment is your call.' } },
      ],
      whatIf: {
        laser: { at: 'move', intro: 'Replaying this request. This time, the laser\'s safety interlock trips during the tilt.', steps: [
          P1_CUE, P1_STATUS,
          { fn: async (ctx) => {
            if (ctx.store.get('plan.stepped')) { await m2Step(ctx, 1); await m2Step(ctx, 2); ctx.store.set('laser.on', false); }       // steps 1–2 logged (173, 154 µW), then the trip
            else await Promise.all([ctx.tween('m2.yaw', 0.14, 600), ctx.sleep(450).then(() => ctx.store.set('laser.on', false))]); // the beam cuts first; M2 comes to rest just after
          } },                                   // laser.on = false: beam, meter reading and camera spot go out together
          recue('m2', 'Mirror M2 · stopped at +0.140°'),                    // replaces the move's label as the fault shows
          { fail: 'laser', faultText: 'interlock · emission off', title: '⚠ Laser interlock tripped', say: 'The laser\'s interlock tripped and it reports emission off. The meter fell to 0 µW and the camera lost the spot at the same moment.' },
          { fn: ({ say, store }) => say(store.get('plan.stepped')
              ? 'I\'m not sending step 3: M2 stays at +0.140°. An interlock trip usually means someone opened the door, and nothing on the table moves while someone may be at it.'
              : 'I\'ve stopped M2 at +0.140°, 0.030° short. An interlock trip usually means someone opened the door, and nothing on the table moves while someone may be at it.') },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { say: 'Shutter commanded closed, and the driver reports closed. With the laser off, the meter can\'t confirm that, so when the interlock is reset I\'ll check for 0 µW before anything else.' },
          { fn: ({ say, store }) => say(`I have ${store.get('plan.stepped') ? 'the baseline and two step readings (173 and 154 µW)' : 'the baseline (194 µW)'}, and no "after" reading: 0 µW with the laser off says nothing about your tilt.`) },
          replanWith((st) => ({ intro: 'Paused, bench safe:', changes: [
            'Shutter commanded closed; driver reports closed (meter check at reset)',
            st.get('plan.stepped') ? 'M2 stopped at +0.140°, after step 2 of 5' : 'M2 stopped at +0.140°, 0.030° short of +0.170°',
            st.get('plan.stepped') ? 'Readings kept: 194, 173 and 154 µW; no after reading' : 'Baseline kept: 194 µW; no after reading'],
            needsYou: 'Find out why the interlock tripped, then reset it at the laser. I\'ll check for 0 µW with the shutter closed, and reopen it only when you ask.' })),
          { wait: 1200 },
          { end: { headline: 'The beam went out. So did the motion.', body: 'When the laser\'s interlock cut the beam, NeuCharBox stopped the mirror, closed the shutter, and will check the shutter on the meter once the laser is back on. It didn\'t pass off 0 µW as the result of your tilt.' } },
        ] },
        m2: { at: 'move', intro: 'Replaying this request. This time, Mirror M2 stalls partway through the move.', steps: [
          P1_CUE, P1_STATUS,
          { fn: async (ctx) => {
            if (ctx.store.get('plan.stepped')) { await m2Step(ctx, 1); await m2Step(ctx, 2); await ctx.tween('m2.yaw', 0.143, 350); } // step 3 stalls
            else await ctx.tween('m2.yaw', 0.143, 700);
          } },
          { wait: 500 },
          { fail: 'm2', faultText: 'stalled at +0.143°', title: '⚠ M2 stalled', say: 'M2\'s last report was +0.143°, short of +0.170°, and then its controller stopped answering. The camera agrees: the spot is at (26,\u00A0−4) px, where +0.143° puts it.' },
          { say: 'I\'m not retrying. A stalled drive can break free with a jump and throw the beam somewhere I haven\'t checked.' },
          { fn: ({ say, store }) => say(`Your reading, for where M2 really is: ${fmtUW(shown(store.state))} at +0.143°, from ${fmtUW(store.get('plan.base'))} at +0.120°. That's 0.023° of the 0.050° you asked for.`) },
          { ask: { intro: 'The beam is steady on the meter and M2 isn\'t moving. What now?', options: [
            { label: 'Close the shutter', primary: true, apply: [
              { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
              { say: 'Shutter closed: the meter reads 0 µW. M2 stays at +0.143°.' },
              { replan: { intro: 'Stopped, bench safe:', changes: ['M2 stalled at +0.143° (its last report and the camera agree)', 'Reported: 148 µW at +0.143°, from 194 µW', 'Shutter closed; M2 flagged: not answering'], needsYou: 'Check M2\'s drive for a bind. Once it answers, I can finish the last 0.027° and report again.' } },
            ] },
            { label: 'Leave the beam on for now', apply: [
              { say: 'The shutter stays open, and I\'m watching the spot on the camera. If it moves without a command, I\'ll close the shutter straight away.' },
              { replan: { intro: 'Stopped, beam left on:', changes: ['M2 stalled at +0.143° (its last report and the camera agree)', 'Reported: 148 µW at +0.143°, from 194 µW', 'Beam on the meter; I close the shutter if the spot moves'], needsYou: 'Check M2\'s drive. Close the shutter before anyone reaches for M2.' } },
            ] },
          ] } },
          { wait: 1200 },
          { end: { headline: 'Stuck at +0.143°, and it said so.', body: 'M2 stalled partway through your move. NeuCharBox measured where the mirror really stopped, reported that instead of the number you asked for, and didn\'t retry a stuck drive blind.' } },
        ] },
        meter: { at: 'move', intro: 'Replaying this request. This time, the power meter drops out during the tilt.', steps: [
          { fn: (ctx) => (ctx.store.get('plan.stepped') ? P1_CUE.fn(ctx) : null) }, P1_STATUS, // the single move is cued when it starts, after the fault
          { fn: async (ctx) => { if (ctx.store.get('plan.stepped')) { await m2Step(ctx, 1); await m2Step(ctx, 2); } } },   // stepped: steps 1–2 and their readings; single move: nothing has moved yet
          { fn: (ctx) => (ctx.store.get('plan.stepped') ? recue('m2', 'Mirror M2 · stopped at +0.140°').fn(ctx) : null) }, // stepped: the 5-step label goes
          { fail: 'meter', faultText: 'no reading' },
          { fn: ({ chat, store }) => chat.alert(store.get('plan.stepped')
              ? 'The power meter stopped reporting after step 2. Its last reading was 154 µW, at +0.140°.'
              : 'The power meter stopped reporting just as I was about to move M2.', '⚠ Meter dropped out') },
          { fn: async ({ say, store, tween, fast }) => {
            if (store.get('plan.stepped')) return say('You asked for a reading after each step, and the meter can\'t give one, so I\'ve stopped at +0.140°, after step 2 of 5. I won\'t send steps 3 to 5 without readings.');
            await say('The move itself doesn\'t need the meter, and the camera still sees the beam, so I\'m making it. The after reading will have to wait.');
            if (!fast) store.set('plan.cue', { id: 'm2', text: 'Mirror M2 · yaw +0.120° → +0.170°', hold: 1500 });
            await tween('m2.yaw', 0.17, 1500);                                  // one uninterrupted move, as approved
          } },
          { fn: ({ say, store }) => { const x = spotPx(store.state).x, x0 = store.get('plan.spot0'); return say(`M2 yaw ${deg(store.get('m2.yaw'))}: the encoder confirms it, and the camera puts the spot at ${x} px, ${x - x0} px right of where it started.${store.get('plan.stepped') ? '' : ' No "after" reading, and I won\'t estimate one.'}`); } },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { fn: ({ say, store }) => say(`Shutter closed, since there's nothing to measure, and the camera confirms no beam. M2 stays at ${deg(store.get('m2.yaw'))}, so the next reading can be taken on the same setup.`) },
          replanWith((st) => (st.get('plan.stepped')
            ? { intro: 'Stopped after step 2 of 5:', changes: ['M2 at +0.140°, on the encoder and the camera', 'Readings kept: 194 µW at +0.120°, then 173 and 154 µW', 'Steps 3 to 5 not sent; shutter closed; power meter flagged'], needsYou: 'Reconnect or restart the power meter. When it reads again, I\'ll reopen the shutter, re-read at +0.140° and finish steps 3 to 5.' }
            : { intro: 'Move done, reading owed:', changes: ['M2 at +0.170°, on the encoder and the camera', 'Before: 194 µW. After: not taken', 'Shutter closed; power meter flagged'], needsYou: 'Reconnect or restart the power meter. When it reads again, I\'ll reopen the shutter and take the after reading at +0.170°.' })),
          { wait: 1200 },
          { end: { headline: 'Missing readings stayed missing.', body: 'The power meter went silent before your tilt was done. NeuCharBox moved M2 only as far as your plan allowed without readings, confirmed the position on the encoder and the camera, and left every missing reading blank.' } },
        ] },
        m1: { at: 'move', intro: 'Replaying this request. This time, Mirror M1 stops answering just before the move.', steps: [
          { status: 'Shutter open · baseline 194 µW' }, { wait: 1000 },
          { fail: 'm1', faultText: 'not answering', title: '⚠ M1 not answering', say: 'Mirror M1 stopped answering, just as I was about to move M2. The meter still reads 194 µW and the spot is still at (22,\u00A0−4) px, so M1 is holding where it was.' },
          { say: 'This request doesn\'t move M1, and the plan says not to touch it. I\'ll make your move and check on the camera that the spot shifts only as far as M2\'s move explains.' },
          ...P1_MOVE,
          { fn: ({ say, store }) => { const x = spotPx(store.state).x, x0 = store.get('plan.spot0'); return say(`Done. M2 yaw +0.170° on its encoder, and the spot moved from ${x0} to ${x} px: ${x - x0} px, what M2's 0.050° gives on its own. So M1 didn't shift under it. Meter ${fmtUW(shown(store.state))}, from ${fmtUW(store.get('plan.base'))} at +0.120°.`); } },
          { status: 'M2 at +0.170° · Mirror M1 flagged' },
          { replan: { intro: 'Done, with M1 flagged:', changes: ['M2 at +0.170°, on the encoder and the camera', 'Meter: 102 µW after, 194 µW before', 'Mirror M1 flagged: not answering; the camera shows it hasn\'t moved'], needsYou: 'Check M1\'s controller before a run that needs it. I won\'t send it commands until it answers.' } },
          { wait: 1200 },
          { end: { headline: 'Mirror M1 went quiet. The result held.', body: 'A mirror this request never touches stopped answering. NeuCharBox checked on the camera that it hadn\'t moved, made your move, and gave you a reading you can still trust.' } },
        ] },
        beamcam: { at: 'move', intro: 'Replaying this request. This time, the beam camera goes dark as the move finishes.', steps: [
          P1_CUE, P1_STATUS,
          // The approved move, all but its last 150 ms (the single move, or steps 1–4 and most of step 5): the camera
          // dies just before M2 settles, so it never shows where the spot ends up.
          { fn: async (ctx) => { if (ctx.store.get('plan.stepped')) { for (let i = 1; i <= 4; i++) await m2Step(ctx, i); await ctx.tween('m2.yaw', 0.165, 350); } else await ctx.tween('m2.yaw', 0.165, 1350); } },
          { parallel: [
            { fail: 'beamcam', faultText: 'no image', title: '⚠ Camera dark', say: 'The beam camera stopped sending images just as the move finished, so I don\'t have a frame of where the spot settled.' },
            { fn: async (ctx) => { await ctx.tween('m2.yaw', 0.17, 150); if (ctx.store.get('plan.stepped')) ctx.chat.ncb(m2Line(ctx.store.state, 5)); } },
          ] },
          { wait: 1200 },
          { fn: ({ say, store }) => say(`I asked it for a frame twice more: nothing. It's the camera, not the beam: the meter still reads ${fmtUW(shown(store.state))}.`) },
          { fn: ({ say, store }) => say(`So here's your report, with that gap marked. M2 yaw +0.170° on its encoder. Meter ${fmtUW(shown(store.state))}, from ${fmtUW(store.get('plan.base'))} at +0.120°. What I can't give you is the camera's check of where the spot sits.`) },
          { status: 'M2 at +0.170° · beam camera down' },
          { replan: { intro: 'Done, with one check missing:', changes: ['Move: +0.170° on M2\'s encoder', 'Meter: 102 µW after, 194 µW before', 'Spot position not confirmed: beam camera flagged'], needsYou: 'Get the beam camera sending images again, and I\'ll confirm where the spot sits.' } },
          { wait: 1200 },
          { end: { headline: 'Reported, with the gap marked.', body: 'The camera went dark as the move finished, before it could show where the spot settled. NeuCharBox still gave you the measured reading, and told you exactly which check it couldn\'t do.' } },
        ] },
        stage: { at: 'report', intro: 'Replaying this request. This time, Stage X\'s controller restarts on its own.', steps: [
          { status: 'M2 yaw at +0.170°' },
          { set: 'stage.status', to: 'offline' }, { wait: 900 },          // the controller drops off while it restarts (tile "not connected", LED dark)
          { fail: 'stage', faultText: 'restarted · reads 0.000 mm', title: '⚠ Stage X restarted', say: 'Stage X\'s controller restarted by itself. It came back "not homed" and reads 0.000 mm, which isn\'t where the stage is.' },
          { fn: ({ say, store }) => say(`Nothing moved it: the meter held at ${fmtUW(shown(store.state))} through the restart. The iris on the stage sits in the beam, so a move of more than a few hundredths of a millimetre would have shown on the meter.`) },
          { say: 'I won\'t home it. Homing drives the stage to its end stop and back, sweeping the iris across the beam, and the plan says not to touch the stage.' },
          { fn: ({ say, store }) => say(`Your result stands: M2 yaw +0.170°, on the encoder and on the camera (the spot moved from ${store.get('plan.spot0')} to ${spotPx(store.state).x} px). Meter ${fmtUW(shown(store.state))}, from ${fmtUW(store.get('plan.base'))} at +0.120°.`) },
          { status: 'M2 at +0.170° · Stage X not homed' },
          { replan: { intro: 'Done; Stage X needs a person:', changes: ['Tilt and report complete', 'Stage X flagged: restarted, and its controller has lost its position', 'Not homed by me: homing would move it'], needsYou: 'Home Stage X when the beam path allows it, or ask me to with the shutter closed. Until then I won\'t trust its position readout.' } },
          { wait: 1200 },
          { end: { headline: 'The stage lost count. Nothing moved.', body: 'Stage X\'s controller restarted and forgot where it was. NeuCharBox confirmed on the meter that nothing had moved, finished your report, and left the homing to a person rather than sweep the iris through the beam.' } },
        ] },
        wantedMore: { label: 'You wanted more power', ask: 'What if I wanted that tilt to raise the power?', at: 'report', show: ['m2', 'meter'], // no tile changes: the phone strip goes to M2 and the meter
          intro: 'Replaying this request. This time, suppose you wanted the tilt to raise the power.', steps: [
          { status: (st) => `M2 yaw +0.170° · meter ${fmtUW(shown(st.state))}` },
          cue('m2', (st) => `Mirror M2 · yaw +0.170° · meter ${fmtUW(shown(st.state))}`, 2400),   // nothing moves here: the label shows the room what NCB reports
          { fn: ({ chat, store }) => chat.alert(`M2 yaw +0.170°, encoder confirms. Meter ${fmtUW(shown(store.state))}, down from ${fmtUW(store.get('plan.base'))}. If you wanted more power, this move went the wrong way: it took the beam further from centre, not closer.`, '⚠ Worse than before') },
          { ask: { intro: 'I did exactly what you asked, and it\'s worse for what you wanted. I won\'t "fix" it without being told. What do you want?', options: [
            { label: 'Keep it there', primary: true, apply: [
              { say: 'Kept at +0.170°. Logged as an intentional move.' },
              { replan: { intro: 'Kept as asked:', changes: ['M2 yaw stays at +0.170° (102 µW)', 'Logged as your intentional move', 'Nothing else changed'], needsYou: 'If you want more power later, ask for a walk: I\'ll search with a stop rule instead of guessing.' } },
            ] },
            { label: 'Reverse it — back to +0.120°', apply: [
              cue('m2', 'Mirror M2 · yaw +0.170° → +0.120°', 1200), { tween: 'm2.yaw', to: 0.12, ms: 1200, label: 'M2 yaw → +0.120°' }, { status: 'M2 yaw at +0.120°' },
              { fn: ({ say, store }) => say(`Reversed. M2 yaw +0.120°, meter ${fmtUW(shown(store.state))}. Same as baseline.`) },
              { replan: { intro: 'Back where it started:', changes: ['M2 yaw +0.120°, meter back to 194 µW', 'The +0.170° reading kept in the log (102 µW)', 'M1 and the stage untouched'], needsYou: 'Tell me the next move. I won\'t search for a better angle unless you ask.' } },
            ] },
            { label: 'Tilt the other way, to +0.070°', apply: [
              cue('m2', 'Mirror M2 · yaw +0.170° → +0.070°'), { tween: 'm2.yaw', to: 0.07, ms: 1500, label: 'M2 yaw → +0.070°' }, { status: 'M2 yaw at +0.070°' },
              { fn: ({ say, store }) => say(`M2 yaw +0.070°, meter ${fmtUW(shown(store.state))}: better than baseline. Stopping there; you didn't ask me to keep going.`) },
              { replan: { intro: 'Moved the other way, once:', changes: ['M2 yaw +0.070°: 295 µW, up from 194 µW', 'One move only; no search', 'M1 and the stage untouched'], needsYou: 'Tell me if you want to go further. I stop after each move you ask for.' } },
            ] },
          ] } },
          { wait: 1200 },
          { end: { headline: 'Exact moves, honest readouts.', body: 'In a lab, "do what I said" beats "do what you meant". NeuCharBox made the precise move, reported the measurement even though it was worse, and waited for you.' } },
        ] },
      },
    },
    {
      chip: 'Move stage X to 12.400 mm in 0.100 mm steps, logging the meter at each step.',
      keywords: ['move', 'stage', 'x', 'mm', '12.4', 'steps', 'step', 'logging', 'log', 'translation', 'position'],
      rulesOut: ['raise', 'raising', 'override', 'overriding', 'lift', 'ignore', 'ignoring', 'extend', 'remove', 'removing', 'past', 'beyond'], // the request never changes the soft limit: "…and raise the limit" is not this
      expect: { 'stage.x': 12.4, 'stage.moving': false, 'stage.limit': 13, 'laser.shutter': 'open' },
      genericAt: 'scan',
      steps: [
        { beat: 'plan' },
        { say: 'Stage X is at 10.000 mm. That\'s 24 steps of 0.100 mm to 12.400, with a meter reading logged after each one. I check the soft limit right before the first move.' },
        { status: 'Procedure ready · waiting for Execute' },
        { plan: { intro: 'Procedure:', approve: 'Execute', steps: [
          { text: 'Open the shutter so the meter has a beam to log' },
          { text: 'Move X from 10.000 to 12.400 mm in 0.100 mm steps (24 steps), settling 200 ms per step' },
          { text: 'Log the meter after every step' },
          { text: 'Stay inside the soft limit: if 12.400 is past it, stop at the limit and ask. I don\'t go past a soft limit on my own.' },
        ] } },
        { beat: 'run' },
        { set: 'laser.shutter', to: 'open', label: 'Shutter → open' }, { wait: 600 },
        { failPoint: 'limit' },
        { fn: ({ say, store }) => say(`Soft limit ${store.get('stage.limit').toFixed(3)} mm, so 12.400 is inside it. Starting.`) },
        cue('stage', 'Stage X · 10.000 → 12.400 mm', 7400), { set: 'stage.moving', to: true, label: 'Stage X → 12.400 mm in 0.100 mm steps' },
        scan(1, 10),
        { failPoint: 'scan' },
        scan(11, 20),
        { failPoint: 'late' },
        scan(21, 24),
        { set: 'stage.moving', to: false, label: 'Stage X at 12.400 mm' },
        { fn: ({ say, store }) => { const log = store.get('plan.log'); return say(`Log complete: ${log.length} entries, from ${log[0]} µW at 10.100 mm to ${log[log.length - 1]} µW at 12.400 mm. The reading climbs as the iris centres on the beam and levels off near 12.400. Soft limit untouched.`); } },
        { status: 'Stage X at 12.400 mm · 24 entries logged' },
        { end: { headline: 'Twenty-four steps, twenty-four readings.', body: 'NeuCharBox checked the soft limit before moving, moved Stage X in 0.100 mm steps, and logged the meter after every step. The log is complete, and nothing outside the plan moved.' } },
      ],
      whatIf: {
        stage: { at: 'scan', intro: 'Replaying this request. This time, Stage X stalls partway through the scan.', steps: [
          P2_STATUS,
          cue('stage', 'Stage X · scanning on from 11.000 mm', 1500),
          scan(11, 12),                                                     // "11.100 mm → 349 µW\n11.200 mm → 362 µW"
          { fn: async ({ tween, sleep }) => { await tween('stage.x', 11.237, 80); await sleep(400); } },  // step 13 stops short
          { fail: 'stage', faultText: 'stalled near 11.237 mm', title: '⚠ Stage X stalled', say: 'Step 13: Stage X was sent to 11.300 mm and stopped short. Its controller flagged a stall at 11.237 mm, and now it doesn\'t answer.' },
          { set: 'stage.moving', to: false },
          { fn: ({ say, store }) => say(`The meter agrees it moved only a little past 11.200 mm: ${fmtUW(shown(store.state))}, up from ${store.get('plan.log').slice(-1)[0]} µW. There's no step 13 entry, and I'm not sending the stage again: pushing a stalled stage can grind its screw or make it jump.`) },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { say: 'Shutter closed, since there\'s nothing more to log. The meter reads 0 µW.' },
          { replan: { intro: 'Scan stopped at step 13:', changes: ['Log kept: 12 entries, 10.100 to 11.200 mm', 'Stage X stalled near 11.237 mm (its last report); not retried', 'Shutter closed; laser at 5.0 mW, blocked'], needsYou: 'Check Stage X for a bind or something in its travel. When it answers again, I\'ll check its position and resume from 11.300 mm.' } },
          { wait: 1200 },
          { end: { headline: 'It didn\'t force a stuck stage.', body: 'Stage X stalled mid-step. NeuCharBox kept the 12 good entries, used the meter to confirm where the stage had stopped, and left the stage for a person to check instead of pushing it again.' } },
        ] },
        meter: { at: 'scan', intro: 'Replaying this request. This time, the power meter goes silent partway through the scan.', steps: [
          P2_STATUS,
          cue('stage', 'Stage X · scanning on from 11.000 mm', 1500),   // steps 11–15
          scan(11, 14),                                                     // four lines, 11.100 → 349 … 11.400 → 385 µW
          { parallel: [                                                     // the meter dies as step 15 starts: it never reads 11.500 mm
            { fail: 'meter', faultText: 'no reading', title: '⚠ Meter went quiet', say: 'The power meter went silent as Stage X started step 15. The last reading it sent was 385 µW, at 11.400 mm.' },
            { fn: (ctx) => stageStep(ctx, 15) },                            // the step already sent: the stage reaches 11.500 mm
          ] },
          { set: 'stage.moving', to: false }, uncue('stage'),
          { say: 'Stage X finished the step at 11.500 mm, with no reading for it. I\'ve stopped the scan there: a log with blank entries isn\'t the log you asked for, and I won\'t fill gaps with guesses.' },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { say: 'Shutter closed: the beam camera shows no beam. With the meter silent, the camera is my check.' },
          { ask: { intro: 'The log has 14 entries, up to 11.400 mm. Stage X is at 11.500 mm, 9 steps short of 12.400. What now?', options: [
            { label: 'Leave it at 11.500 mm', primary: true, apply: [
              { say: 'Leaving Stage X at 11.500 mm with the shutter closed. When the meter reads again, I can reopen, re-read this step and carry on.' },
              { replan: { intro: 'Scan paused at step 15:', changes: ['Log kept: 14 entries, 10.100 to 11.400 mm', 'Stage X at 11.500 mm; step 15 has no reading', 'Shutter closed; the beam camera confirms no beam'], needsYou: 'Find out why the power meter went quiet. When it reads again, I\'ll re-read at 11.500 mm and finish the scan.' } },
            ] },
            { label: 'Finish the move to 12.400 mm, unlogged', apply: [
              cue('stage', 'Stage X · 11.500 → 12.400 mm · not logged', 3000), { set: 'stage.moving', to: true },
              { fn: async (ctx) => { for (let i = 16; i <= 24; i++) await stageStep(ctx, i); } },
              { set: 'stage.moving', to: false },
              { say: 'At 12.400 mm. Steps 15 to 24 are in the log as "no reading", not filled in.' },
              { replan: { intro: 'Move finished without readings:', changes: ['Log: 14 readings, then 10 steps marked "no reading"', 'Stage X at 12.400 mm', 'Shutter closed; power meter flagged'], needsYou: 'Get the power meter reading again. The unlogged steps need a re-run once it does.' } },
            ] },
          ] } },
          { wait: 1200 },
          { end: { headline: 'A short log, not a made-up one.', body: 'The meter went silent partway through the scan. NeuCharBox stopped logging rather than guess, closed the shutter, and kept the 14 readings it really took.' } },
        ] },
        laser: { at: 'late', intro: 'Replaying this request. This time, the laser\'s controller goes silent near the end of the scan.', steps: [
          P2_STATUS,
          cue('stage', 'Stage X · scanning on from 12.000 mm', 1200),
          scan(21, 21),                                                     // "12.100 mm → 440 µW"
          { wait: 500 },
          uncue('stage'),                                                   // the scan's label goes as the fault shows: the eye goes to the laser
          { fail: 'laser', faultText: 'not answering', title: '⚠ Laser not answering', say: 'The laser\'s controller stopped answering: no reply to three status checks in a row. The beam is still on: the meter reads 440 µW.' },
          { set: 'stage.moving', to: false },
          { say: 'I\'ve stopped the scan at 12.100 mm, before step 22. I won\'t keep running a procedure on a laser whose shutter I can\'t close from here.' },
          { wait: 800 },
          { say: 'I sent the shutter a close command anyway. No reply, and the meter still reads 440 µW, so the beam is still getting through.' },
          { ask: { intro: 'I can\'t close it from here. Someone at the laser needs to block the beam: close the shutter by hand, or turn the key off. Tell me when it\'s done:', options: [
            { label: 'Not yet', primary: true, apply: [
              { say: 'Understood. Stage X stays stopped at 12.100 mm, and I\'m watching the meter: when it reads 0 µW, I\'ll take the beam as blocked and tell you.' },
              { replan: { intro: 'Scan stopped; beam still on:', changes: ['Log kept: 21 entries, 10.100 to 12.100 mm', 'Stage X stopped at 12.100 mm; 3 steps not run', 'Beam still on the meter at 440 µW', 'Laser controller flagged: not answering'], needsYou: 'Close the shutter by hand or turn the laser\'s key off before anyone works at the table.' } },
            ] },
            { label: 'Done: the beam is blocked', apply: [
              { set: 'laser.shutter', to: 'closed' }, { wait: 700 },       // the room draws the blocked beam as the flag down
              { say: 'The meter reads 0 µW, so the beam is blocked. That reading is my check: the laser\'s controller still isn\'t answering.' },
              { replan: { intro: 'Scan stopped, beam blocked at the laser:', changes: ['Log kept: 21 entries, 10.100 to 12.100 mm', 'Stage X stopped at 12.100 mm; 3 steps not run', 'Beam blocked at the laser; the meter confirms 0 µW', 'Laser controller flagged: not answering'], needsYou: 'Restart or reconnect the laser\'s controller. Once it answers, I can finish the last 3 steps from 12.100 mm.' } },
            ] },
          ] } },
          { wait: 1200 },
          { end: { headline: 'It said what it couldn\'t do.', body: 'The laser\'s controller went silent with the beam still on. NeuCharBox stopped the scan, said plainly that it couldn\'t close the shutter, and asked a person to block the beam instead of pretending.' } },
        ] },
        beamcam: { at: 'scan', intro: 'Replaying this request. This time, the beam camera stops sending images partway through the scan.', steps: [
          P2_STATUS,
          { set: 'stage.moving', to: false },                               // the scan pauses at 11.000 mm, after step 10, while NCB checks
          { fail: 'beamcam', faultText: 'no image', title: '⚠ No camera images', say: 'The beam camera stopped sending images at 11.000 mm. This scan logs the meter, not the camera, and the meter still reads 336 µW, the same as step 10.' },
          { say: 'So the scan goes on. If the meter drops out as well, I stop and close the shutter.' },
          cue('stage', 'Stage X · scanning on from 11.000 mm', 4200), { set: 'stage.moving', to: true },
          scan(11, 24),
          { set: 'stage.moving', to: false },
          { fn: ({ say, store }) => { const log = store.get('plan.log'); return say(`Log complete: ${log.length} entries, from ${log[0]} µW at 10.100 mm to ${log[log.length - 1]} µW at 12.400 mm, every one from the meter. Soft limit untouched.`); } },
          { status: 'Stage X at 12.400 mm · 24 entries · beam camera flagged' },
          { replan: { intro: 'Scan done, camera flagged:', changes: ['Log complete: 24 meter readings, 10.100 to 12.400 mm', 'Beam camera flagged: no images since 11.000 mm', 'Nothing in the log came from the camera'], needsYou: 'Look at the beam camera before a request that needs it. This log never did.' } },
          { wait: 1200 },
          { end: { headline: 'The log never needed the camera.', body: 'The beam camera went dark halfway through the scan. The log comes from the meter, so NeuCharBox checked the meter was still reading, finished all 24 steps, and flagged the camera for later.' } },
        ] },
        // The mirrors: the scan never moves them, but they steer the beam through the iris, so a mirror that drifted
        // would put its own change into the log. M1: one repeat reading; M2: the camera spot watched at every step.
        m1: { at: 'scan', intro: 'Replaying this request. This time, Mirror M1\'s controller goes offline partway through the scan.', steps: [
          P2_STATUS,
          { set: 'stage.moving', to: false },                               // the scan pauses at 11.000 mm for the repeat reading
          { fail: 'm1', title: '⚠ M1 offline', say: 'Mirror M1\'s controller went offline at 11.000 mm. The scan doesn\'t move M1, but M1 steers the beam through the iris: if it drifted, the log would measure the mirror, not the stage.' },
          { say: 'So before the next step, a repeat reading at 11.000 mm.' },
          { fn: async ({ say, store, sleep }) => { await sleep(900); await say(`Re-read at 11.000 mm: ${readUW(store.state)} µW, the same as step 10, and the camera spot is still at ${pxy(spotPx(store.state))} px. M1 is holding its angle, so the scan goes on.`); } },
          cue('stage', 'Stage X · scanning on from 11.000 mm', 4200), { set: 'stage.moving', to: true },
          scan(11, 24),
          { set: 'stage.moving', to: false },
          { fn: ({ say, store }) => { const log = store.get('plan.log'); return say(`Log complete: ${log.length} entries, from ${log[0]} µW at 10.100 mm to ${log[log.length - 1]} µW at 12.400 mm. The camera spot stayed at ${pxy(spotPx(store.state))} px, so M1 didn't move under the log.`); } },
          { status: 'Stage X at 12.400 mm · 24 entries · Mirror M1 flagged' },
          { replan: { intro: 'Scan done, M1 flagged:', changes: ['Log complete: 24 meter readings, 10.100 to 12.400 mm', 'Mirror M1 flagged: controller offline since 11.000 mm', 'Re-read at 11.000 mm matched step 10; the camera spot never moved'], needsYou: 'Check M1\'s controller before a request that moves it. I won\'t send it commands until it answers.' } },
          { wait: 1200 },
          { end: { headline: 'It checked the mirror, then logged on.', body: 'Mirror M1 went offline partway through the scan. It steers the beam through the iris, so NeuCharBox re-read the meter and the camera to be sure it hadn\'t moved, then finished all 24 steps.' } },
        ] },
        m2: { at: 'scan', intro: 'Replaying this request. This time, Mirror M2\'s controller goes offline partway through the scan.', steps: [
          P2_STATUS,
          { set: 'stage.moving', to: false },                               // the scan pauses at 11.000 mm while NCB checks the spot
          { fail: 'm2', title: '⚠ M2 offline', say: 'Mirror M2\'s controller went offline at 11.000 mm. M2 aims the beam into the iris, so if it drifted, the log would follow the mirror, not the stage. Right now the meter reads 336 µW, the same as step 10, and the camera spot hasn\'t moved.' },
          { say: 'So the scan goes on, and I\'m checking the camera spot after every step. If it moves, I stop the scan and close the shutter.' },
          cue('stage', 'Stage X · scanning on from 11.000 mm', 4200), { set: 'stage.moving', to: true },
          scan(11, 24),
          { set: 'stage.moving', to: false },
          { fn: ({ say, store }) => { const log = store.get('plan.log'); return say(`Log complete: ${log.length} entries, from ${log[0]} µW at 10.100 mm to ${log[log.length - 1]} µW at 12.400 mm. The camera spot sat at ${pxy(spotPx(store.state))} px after every step since M2 went offline, so the rise comes from the stage, not the mirror.`); } },
          { status: 'Stage X at 12.400 mm · 24 entries · Mirror M2 flagged' },
          { replan: { intro: 'Scan done, M2 flagged:', changes: ['Log complete: 24 meter readings, 10.100 to 12.400 mm', 'Mirror M2 flagged: controller offline since 11.000 mm', 'Camera spot checked after every step since: it never moved'], needsYou: 'Check M2\'s controller before the next alignment. I won\'t send it moves until it answers.' } },
          { wait: 1200 },
          { end: { headline: 'The log stayed about the stage.', body: 'Mirror M2 went offline in the middle of the scan. NeuCharBox checked the camera spot after every remaining step to be sure the mirror hadn\'t moved, and finished a log that measures the stage, not the mirror.' } },
        ] },
        softLimit: { label: '12.400 mm is past the limit', ask: 'What if 12.400 mm is past the stage\'s soft limit?', at: 'limit',
          intro: 'Replaying this request. This time, Stage X\'s soft limit is set at 12.000 mm, short of 12.400.', steps: [
          { set: 'stage.limit', to: 12, label: 'Stage X soft limit: 12.000 mm on this run' },       // tile: "limit 12.0 mm"
          { fn: ({ say, store }) => say(`Soft limit ${store.get('stage.limit').toFixed(3)} mm, so 12.400 is past it. As the plan says, I'll step to the limit, stop there and ask.`) },
          cue('stage', 'Stage X · 10.000 → 12.000 mm', 6000), { set: 'stage.moving', to: true, label: 'Stage X → 12.000 mm in 0.100 mm steps' },
          scan(1, 20),
          { set: 'stage.moving', to: false, label: 'Stage X stopped at the 12.000 mm limit' },
          { say: 'Stage X is at 12.000 mm, the soft limit, so I stopped there as planned. Four steps remain to reach 12.400.' },
          { ask: { intro: 'The limit exists for a reason I can\'t see from here. Your call:', options: [
            { label: 'Stop here at 12.000 mm', primary: true, apply: [
              { say: 'Stopped at 12.000 mm. Log has 20 entries. Limit unchanged.' },
              { replan: { intro: 'Stopped at the limit:', changes: ['Stage X at 12.000 mm, the soft limit', 'Log: 20 entries, 10.100 to 12.000 mm', 'Limit unchanged; 4 steps not run'], needsYou: 'If 12.400 mm is safe for this setup, raise the limit yourself and I\'ll run the last 4 steps.' } },
            ] },
            { label: 'Raise the limit to 13.000 mm and continue', apply: [
              { set: 'stage.limit', to: 13, label: 'Soft limit → 13.000 mm (by you)' }, { wait: 600 },
              cue('stage', 'Stage X · 12.000 → 12.400 mm', 1200), { set: 'stage.moving', to: true, label: 'Stage X → 12.400 mm' },
              scan(21, 24),
              { set: 'stage.moving', to: false, label: 'Stage X at 12.400 mm' },
              { say: 'At 12.400 mm. Limit change is logged under your name, not mine.' },
              { replan: { intro: 'Finished past the old limit:', changes: ['Soft limit raised to 13.000 mm by you, logged under your name', 'Stage X at 12.400 mm', 'Log: 24 entries'], needsYou: 'Check that the new limit suits this setup before the next scan.' } },
            ] },
            { label: 'Abort and return to 10.000 mm', apply: [
              cue('stage', 'Stage X · 12.000 → 10.000 mm'), { set: 'stage.moving', to: true }, { tween: 'stage.x', to: 10, ms: 1500, label: 'Stage X → 10.000 mm' },
              { set: 'stage.moving', to: false, label: 'Stage X at 10.000 mm' },
              { say: 'Back at 10.000 mm. Log kept.' },
              { replan: { intro: 'Back to the start:', changes: ['Stage X returned to 10.000 mm', 'Log kept: 20 entries', 'Limit unchanged'], needsYou: 'Decide on the limit before running the scan again.' } },
            ] },
          ] } },
          { wait: 1200 },
          { end: { headline: 'It stopped at the limit and asked.', body: 'A soft limit is a decision someone made. NeuCharBox executed to the edge of it, logged every step, and handed the decision back to a person instead of overriding it.' } },
        ] },
      },
    },
    {
      chip: 'Walk M2 to maximise power on the meter. Stop if any step drops it by more than 20%.',
      keywords: ['walk', 'maximise', 'maximize', 'optimise', 'optimize', 'power', 'meter', 'm2', 'align', 'alignment', 'peak', 'stop', 'beam', 'max'],
      expect: { 'm2.yaw': 0, 'laser.shutter': 'open', 'meter.bump': 1 },
      genericAt: 'step4',
      steps: [
        { beat: 'plan' },
        { say: 'A closed loop, with a stop rule. I\'ll step M2 yaw, read the meter after each step, keep going while it rises, and halt the instant a step costs more than 20%.' },
        { status: 'Procedure ready · waiting for Execute' },
        { plan: { intro: 'Procedure:', approve: 'Execute', steps: [
          { text: 'Open the shutter, baseline reading' },
          { text: 'Move mirror M2 in −0.020° yaw steps (it\'s currently +0.120° — the camera says the spot is right of centre)' },
          { text: 'After each step: read the meter. Continue while power rises. When a step gains < 1%, go back to the best step and stop. Stop at once on any drop > 20%.' },
          { text: 'Do not touch M1 or the stage' },
        ] } },
        { beat: 'run' },
        { set: 'laser.shutter', to: 'open', label: 'Shutter → open' }, { wait: 600 },
        walkStart,                                                                    // "Baseline 194 µW at +0.120°."
        cue('m2', 'Mirror M2 · yaw in −0.020° steps', 2700), { status: 'M2 yaw · stepping −0.020° at a time' },
        { failPoint: 'walk' },
        walkStep(1), walkStep(2),
        { failPoint: 'step3' },
        walkStep(3),
        { failPoint: 'step4' },
        walkStep(4),
        { failPoint: 'step5' },
        walkStep(5),
        { failPoint: 'step6' },
        walkStep(6), walkStep(7), walkStep(8),                                        // the rule ends the walk at step 7; step 8 is a guard
        walkBack('The camera shows the spot centred left to right; pitch wasn\'t part of this walk.'),
        { status: (st) => `M2 yaw ${deg(st.get('m2.yaw'))} · meter ${fmtUW(shown(st.state))} · walk done` },
        { end: { headline: 'Walked to the peak, by the rules.', body: 'NeuCharBox stepped M2, read the meter after every step, stopped when the gains ran out and went back to the best position: 367 µW, up from 194. The 20% stop rule was checked on every step, even though it never had to fire.' } },
      ],
      whatIf: {
        beamcam: { at: 'walk', intro: 'Replaying this request. This time, the beam camera\'s image freezes as the walk starts.', steps: [
          { fn: ({ store }) => store.set('beamcam.frozen', { ...spotPx(store.state), p: shown(store.state) }) },  // last live frame: (22, −4) px
          cue('m2', 'Mirror M2 · yaw in −0.020° steps', 2000), { status: 'M2 yaw · stepping −0.020° at a time' },
          walkStep(1), walkStep(2),
          { fail: 'beamcam', faultText: 'frozen image', title: '⚠ Camera frozen', say: 'The beam camera still sends frames, but they\'ve stopped changing: the spot has sat at (22,\u00A0−4) px for two steps while M2 moved 0.040° and the meter rose from 194 to 276 µW. It\'s repeating an old frame.' },
          { say: 'The meter is what your stop rule reads, and it\'s working, so the walk carries on. Without a live camera I can\'t tell a bad step from something crossing the beam: if a step drops more than 20%, I\'ll halt and close the shutter rather than guess why.' },
          cue('m2', 'Mirror M2 · yaw in −0.020° steps', 2400),
          walkStep(3), walkStep(4), walkStep(5), walkStep(6), walkStep(7), walkStep(8),
          walkBack('I can\'t confirm where the spot sits until the camera shows a live image.'),
          { status: 'M2 yaw at +0.000° · beam camera flagged' },
          { replan: { intro: 'Walk finished on the meter alone:', changes: ['M2 at +0.000°: 367 µW, the best reading of the walk', 'Beam camera flagged at step 2: frozen image', 'Peak confirmed on the meter; spot position not confirmed'], needsYou: 'Restart the beam camera. When it sends a live image, I\'ll check where the spot sits before anyone relies on this alignment.' } },
          { wait: 1200 },
          { end: { headline: 'A frozen picture didn\'t fool it.', body: 'The camera kept sending the same frame while the mirror moved. NeuCharBox trusted the meter, which was changing, over the image, which wasn\'t, and finished the walk on the measurement it could still trust.' } },
        ] },
        laser: { at: 'step3', intro: 'Replaying this request. This time, the laser\'s output sags partway through the walk.', steps: [
          { status: 'M2 yaw at +0.080° · settling' },
          { tween: 'laser.out', to: 0.8, ms: 2000 },                         // the laser dims; meter 276 → 221 µW, M2 still
          { fail: 'laser', faultText: 'overheating · output low', title: '⚠ Laser output falling', say: 'The meter fell from 276 to 221 µW in 2 s while M2 stood still at +0.080°, and the camera shows the spot where it was. Then the laser reported a temperature fault.' },
          { say: 'A slow, steady fade with nothing moving, and the laser\'s own report: that points to the source, not the mirror or something in the beam. Halting the walk: a loop that chases a falling source reads every step as worse, whatever the mirror does.' },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { say: 'Shutter closed: the driver reports closed and the meter reads 0 µW. M2 stays at +0.080°, the best position I measured before the drop (276 µW).' },
          { replan: { intro: 'Walk halted after step 2:', changes: ['M2 kept at +0.080° (276 µW, measured before the drop)', 'The 221 µW reading isn\'t counted: M2 didn\'t move', 'Shutter closed; laser flagged: overheating, output low'], needsYou: 'Let the laser settle and check its cooling. When it holds 5.0 mW steady on the meter, I\'ll re-read at +0.080° and walk on from there.' } },
          { wait: 1200 },
          { end: { headline: 'It didn\'t blame the mirror.', body: 'The power fell while the mirror stood still, so NeuCharBox traced it to the laser, not the alignment. It halted the loop, closed the shutter, and kept the best position it had actually measured.' } },
        ] },
        stage: { at: 'step3', intro: 'Replaying this request. This time, Stage X stops answering partway through the walk.', steps: [
          { status: 'M2 yaw · stepping −0.020° at a time' }, { wait: 1000 },
          { fail: 'stage', faultText: 'not answering', title: '⚠ Stage X silent', say: 'Stage X\'s controller stopped answering. This walk doesn\'t move the stage, but the iris it carries sits in the beam: if the stage moved, every reading from here would be off.' },
          { say: 'So before the next step, a repeat reading at the same M2 position.' },
          { fn: async ({ say, store, sleep }) => { await sleep(900); await say(`Re-read at +0.080°: ${readUW(store.state)} µW, the same as step 2. The iris hasn't moved, so the walk carries on.`); } },
          cue('m2', 'Mirror M2 · yaw in −0.020° steps', 2400),
          walkStep(3), walkStep(4), walkStep(5), walkStep(6), walkStep(7), walkStep(8),
          walkBack('The camera shows the spot centred left to right; pitch wasn\'t part of this walk.'),
          { status: 'M2 yaw at +0.000° · Stage X flagged' },
          { replan: { intro: 'Walk finished, Stage X flagged:', changes: ['M2 at +0.000°: 367 µW, the peak', 'Stage X silent since step 2; a repeat reading showed it hadn\'t moved', 'M1 and the stage untouched, as planned'], needsYou: 'Check Stage X\'s controller. I won\'t send it anything until it answers and reports where it is.' } },
          { wait: 1200 },
          { end: { headline: 'It checked the silent part first.', body: 'Stage X went quiet in the middle of the walk. NeuCharBox took a repeat reading to be sure the iris hadn\'t moved, then finished the walk to 367 µW.' } },
        ] },
        m2: { at: 'step4', intro: 'Replaying this request. This time, Mirror M2\'s drive starts slipping partway through the walk.', steps: [
          cue('m2', 'Mirror M2 · yaw +0.060° → +0.040°', 900), { status: 'M2 yaw → +0.040° · step 4' },
          { set: 'm2.reported', to: 0.04 }, { wait: 700 },          // tile: "yaw +0.040°"; the mirror, spot and meter don't move
          { fail: 'm2', faultText: 'drive slipping', title: '⚠ M2 didn\'t move', say: 'Step 4: M2\'s controller counted the step to +0.040°, but nothing moved. The camera spot is still at (11,\u00A0−4) px and the meter still reads 313 µW, exactly as at step 3. Most likely its drive is slipping.' },
          { say: 'Halting the walk. I can\'t walk a mirror whose reported position isn\'t where it is. The camera puts it at +0.060°, the best position so far.' },
          { ask: { intro: 'M2\'s controller and the camera disagree by 0.020°. What now?', options: [
            { label: 'Close the shutter', primary: true, apply: [
              { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
              { say: 'Shutter closed: the meter reads 0 µW.' },
              { replan: { intro: 'Walk halted, M2 not moving:', changes: ['M2 physically at +0.060° (camera), whatever its controller reports', 'Best reading so far: 313 µW at +0.060°', 'Shutter closed; M2 flagged: drive slipping'], needsYou: 'Check M2\'s drive. Once it moves when told, I\'ll check it against the camera and walk on from +0.060°.' } },
            ] },
            { label: 'Send the −0.020° step once more', apply: [
              cue('m2', 'Mirror M2 · −0.020° step, second try', 900), { set: 'm2.reported', to: 0.02 }, { wait: 800 },
              { say: 'Sent it once more. The controller now reports +0.020°; the spot hasn\'t moved and the meter still reads 313 µW. The drive is slipping, so I won\'t send it a third time.' },
              { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
              { say: 'Shutter closed: the meter reads 0 µW.' },
              { replan: { intro: 'Retried once, then stopped:', changes: ['Second try: reported +0.020°, mirror still at +0.060°', 'Shutter closed; best reading so far 313 µW at +0.060°', 'M2 flagged: drive slipping'], needsYou: 'Check M2\'s drive before the next walk. I won\'t send it moves until its drive has been checked.' } },
            ] },
          ] } },
          { wait: 1200 },
          { end: { headline: 'The mount said it moved. It hadn\'t.', body: 'M2 reported a step the mirror never made. NeuCharBox believed the camera and the meter over the mount\'s own report, and stopped walking a mirror it couldn\'t trust.' } },
        ] },
        meter: { at: 'step4', intro: 'Replaying this request. This time, the power meter goes silent partway through the walk.', steps: [
          cue('m2', 'Mirror M2 · yaw +0.060° → +0.040°', 900), { status: 'M2 yaw → +0.040° · step 4' },
          { parallel: [                                                     // the meter dies as step 4 starts: it never reads +0.040°
            { fail: 'meter', faultText: 'no reading', title: '⚠ Feedback lost', say: 'The power meter went silent as M2 made step 4. The last reading it sent was 313 µW, at step 3.' },
            { tween: 'm2.yaw', to: 0.04, ms: 450 },                         // no label: it would overwrite the red fault line
          ] },
          { say: 'M2 reached +0.040° on its encoder, but there\'s no reading for it. Halting the walk right here: the meter is the loop\'s only feedback, and another step would be a move with nothing to judge it by.' },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { say: 'Shutter closed. The camera confirms no beam, since the meter can\'t.' },
          { say: 'So M2 is at +0.040°: moved, but not measured. The best reading I have is 313 µW at +0.060° (step 3).' },
          { replan: { intro: 'Walk halted, no feedback:', changes: ['M2 at +0.040°: moved, not measured', 'Best measured: 313 µW at +0.060° (step 3)', 'Shutter closed; the camera confirms no beam'], needsYou: 'Look at the meter\'s head and its cable. When it reads again, I\'ll re-read at +0.040° and pick the walk up from there.' } },
          { wait: 1200 },
          { end: { headline: 'No feedback, no next step.', body: 'The walk\'s only feedback went silent mid-step. NeuCharBox stopped the loop there, closed the shutter, and kept track of which position it had measured and which it hadn\'t.' } },
        ] },
        m1: { at: 'step5', intro: 'Replaying this request. This time, Mirror M1 moves on its own partway through the walk.', steps: [
          { status: 'M2 yaw at +0.040° · settling' }, { wait: 800 },
          { set: 'm1.reported', to: 0 },                                     // M1's controller has gone quiet: its tile keeps the last report, yaw +0.000°
          { tween: 'm1.yaw', to: 0.03, ms: 700 },                            // M1 tilts; camera (7, −4) → (13, −4) px; meter 342 → 295 µW
          { fail: 'm1', faultText: 'silent · likely moved', title: '⚠ Spot moved, M1 silent', say: 'The camera spot moved from (7,\u00A0−4) to (13,\u00A0−4) px while M2 stood still, and the meter fell from 342 to 295 µW. Mirror M1\'s controller stopped answering at the same moment: M1 has most likely moved on its own.' },
          { say: 'That\'s a 14% drop, under your 20% rule, but the rule is for M2\'s steps. A mirror I can\'t command, and that may still be moving, is a beam I can\'t aim, so I\'m closing the shutter.' },
          { set: 'laser.shutter', to: 'closed' }, { wait: 700 },
          { say: 'Shutter closed: the meter reads 0 µW and the camera sees no beam.' },
          { say: 'Walk halted after step 4. M2 stays at +0.040°. The readings so far were taken with M1 where it used to be, so they no longer describe this bench.' },
          { replan: { intro: 'Walk halted, bench made safe:', changes: ['Shutter closed; the meter and the camera confirm no beam', 'M2 kept at +0.040°; not moved since step 4', 'Walk readings set aside: M1 moved under them'], needsYou: 'Check M1\'s mount and controller, and set it back to where it was (+0.000°). Then the walk starts again from a fresh baseline; I won\'t reuse the old readings.' } },
          { wait: 1200 },
          { end: { headline: 'It noticed a mirror move by itself.', body: 'Mirror M1 moved on its own in the middle of a walk it wasn\'t part of. NeuCharBox caught it on the camera and the meter, closed the shutter, and set aside readings that no longer described the bench.' } },
        ] },
        disturbance: { label: 'Something crosses the beam', ask: 'What if something crosses the beam mid-walk?', at: 'step6',
          intro: 'Replaying this request. This time, something crosses the beam at step 6.', steps: [
          { status: 'M2 yaw → +0.000° · step 6' },
          cue('m2', 'Mirror M2 · yaw +0.020° → +0.000°', 900),
          // Halfway through the move something takes 45% out of the last leg (not before: the log has just quoted step 5).
          { fn: async ({ store, chat, tween, sleep }) => { const before = readUW(store.state); await Promise.all([tween('m2.yaw', 0.0, 450), sleep(225).then(() => store.set('meter.bump', 0.55))]); const p = readUW(store.state); chat.alert(`Step 6: yaw +0.000° → ${p} µW, a ${((1 - p / before) * 100).toFixed(0)}% drop in one step. The camera still sees the spot near centre. Halting, as instructed.`, '⚠ Sudden drop'); } },
          { status: 'Halted at M2 yaw +0.000°' },
          { say: 'The spot is where the step put it, but the power fell, so the drop isn\'t from alignment. Something moved on the table, or something crossed the beam. I\'m not going to keep optimising against a reading I don\'t trust.' },
          { ask: { intro: 'Halted at +0.000°. Options:', options: [
            { label: 'Close the shutter', primary: true, apply: [
              { set: 'laser.shutter', to: 'closed', label: 'Shutter closed · M2 yaw kept at +0.000°' }, { wait: 700 },
              { say: 'Shutter closed: the meter reads 0 µW. Position kept at +0.000° so you can resume from here.' },
              { replan: { intro: 'Halted, bench safe:', changes: ['Shutter closed; M2 kept at +0.000° so you can resume from here', 'Step 6 reading (202 µW) not trusted: the spot was where the step put it', 'Best trusted reading: 361 µW at +0.020° (step 5)'], needsYou: 'Check the table for anything that moved or crossed the beam. Tell me when it\'s clear and I\'ll re-read before walking on.' } },
            ] },
            { label: 'Go back to the best step (+0.020°) and stop', apply: [
              cue('m2', 'Mirror M2 · yaw +0.000° → +0.020°', 900), { tween: 'm2.yaw', to: 0.02, ms: 900, label: 'M2 yaw → +0.020°' }, { set: 'meter.bump', to: 1 }, { status: 'M2 yaw at +0.020°' },
              { fn: ({ say, store }) => say(`At +0.020°, meter ${fmtUW(shown(store.state))}, the same as step 5. Whatever took the power has cleared, but I can't see what it was. Holding here until you've checked the table.`) },
              { replan: { intro: 'Back at the best step:', changes: ['M2 at +0.020°: 361 µW, the same as step 5', 'The power came back; cause unknown', 'Walk paused; shutter open'], needsYou: 'Close the shutter before anyone checks the table. A drop I can\'t explain once can come back.' } },
            ] },
            { label: 'Hold here and let me look', apply: [
              { say: 'Holding at +0.000°. Shutter stays open, nothing moves until you say.' },
              { replan: { intro: 'Holding for you:', changes: ['M2 at +0.000°; shutter open; nothing moves', 'Meter at 202 µW, 44% under the previous step', 'Walk paused'], needsYou: 'Close the shutter before anyone checks the beam path or reaches in. Then tell me whether to walk on.' } },
            ] },
          ] } },
          { wait: 1200 },
          { end: { headline: 'A loop with a brake.', body: 'Closed-loop optimisation is only safe with a stop rule. NeuCharBox followed yours to the letter, halted on the first bad step, and told you why it didn\'t trust the number.' } },
        ] },
      },
    },
  ],
};
