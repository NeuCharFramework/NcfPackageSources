// Scene 1 — Home, away for a week. Proves: say what you want, never name a device.

// True for every way into the away-for-a-week request: a typed one may name devices, and a beat skip runs the plan unapproved.
const KS_END = { headline: 'Lived in all week, watered when needed.', body: 'You said what you wanted. NeuCharBox found the six devices, showed you the plan before anything ran, and ran your week, counting a watering only when the sensor saw the water arrive.' };
// Typed requests (js/engine/match.js, rulesOut): things this room doesn't have. A request that adds one ("…and lock the
// door", "…and feed the cat") gets the chips back instead of a plan that quietly leaves it out.
const NOT_HERE = ['lock', 'unlock', 'alarm', 'siren', 'tv', 'television', 'radio', 'music', 'garden', 'lawn', 'sprinkler', 'garage', 'feed', 'cat', 'cats', 'dog', 'dogs', 'pet', 'pets', 'fish'];

// An NCB line whose wording depends on the visitor's plan choices, paced like a { say } step.
async function say({ chat, sleep, fast }, text) {
  if (!fast) { chat.typing(true); await sleep(Math.min(1800, 350 + text.length * 14)); chat.typing(false); }
  chat.ncb(text); await sleep(400);
}

// What-if helpers (opt-in failures, js/engine/whatif.js). Every home scenario holds 1.5 s before its end card (A7).
const HOLD = { wait: 1500 };
const setPoint = (store) => (store.get('plan.warm') ? 20 : 18); // p0: the away set point the visitor approved
const SOIL_START = 34; // soil moisture right after setup (%)
// A re-plan card, NCB line or alert whose wording depends on the visitor's Edits. Silent in a quiet replay, like every card.
const replanWith = (spec) => ({ fn: async ({ chat, store, sleep }) => { chat.replan(spec(store)); await sleep(700); } });
const sayWith = (text) => ({ fn: (c) => c.say(text(c.store)) });
const alertWith = (text, title) => ({ fn: async ({ chat, store, sleep }) => { chat.alert(text(store), title); await sleep(600); } });

export default {
  id: 'home',
  title: 'Home, away for a week',
  startHour: 16.5,
  // Frames every device (door camera and thermostat on the left wall, lamp in the right corner) at aspect 1.5; the
  // renderer widens the vertical FOV on narrower canvases (fitAspect). The auto-sway (home azimuth 0.75) only turns
  // right of home: below ~0.7 the door camera's ping label ("Motion at the door") runs off the left edge.
  camera: { position: [3.52, 2.22, 3.0], target: [-0.3, 1.2, -1.1], fov: 42, fitAspect: 1.5, azimuth: [0.72, 0.9], minDistance: 1.6, maxDistance: 6.5 },
  setupIntro: 'This is NeuCharBox, on the side table. Plug it in to start.',
  askIntro: 'You\'re leaving for a week on Friday. What do you want the house to do while you\'re gone? Pick one, or type your own.',
  deviceOrder: ['lamp', 'blinds', 'soil', 'pump', 'camera', 'thermostat'],
  // ref: how NCB names a device mid-sentence (what-if copy). The engine's generic copy is singular ("What if <ref>
  // fails?", "…didn't need it"), so the blinds are "the blinds controller" there: the part that goes offline, and it
  // keeps every line plural-free. Their own scenarios, and every other line, say "the blinds".
  // faultText: what a fault says when the scenario gives no note of its own; the engine's generic what-if reads
  // "<name> stopped responding · <faultText>.", so each adds something true after it. The soil sensor's is its last
  // reading: only a request that never touches the soil plays its generic what-if, so that is still SOIL_START.
  // The extra state (blinds jammed/flat, soil probeOut, pump blocked, thermostat cold) is only the room's look in a
  // what-if (update()); no format() reads it.
  devices: {
    // Tile follows the brightness, like the room does: a lamp faded to 0 reads "off" before its on flag drops.
    lamp:       { icon: 'lamp', name: 'Floor lamp', ref: 'the floor lamp', initial: { on: false, brightness: 0 }, edit: [{ field: 'brightness', kind: 'slider', min: 0, max: 1, step: 0.01, scale: 100, unit: '%' }], format: (s) => (Math.round(s.brightness * 100) > 0 ? `on · ${Math.round(s.brightness * 100)}%` : 'off'), active: (s) => s.brightness > 0.02, faultText: 'off the network' },
    // Blinds read "open" and "closed" at the ends of their travel, not "0% closed" and "100% closed".
    blinds:     { icon: 'blinds', name: 'Blinds', ref: 'the blinds controller', initial: { closed: 0, flat: false, jammed: false }, edit: [{ field: 'closed', kind: 'slider', min: 0, max: 1, step: 0.01, scale: 100, unit: '%', top: 'closed' }], format: (s) => (s.closed < 0.005 ? 'open' : s.closed > 0.995 ? 'closed' : `${Math.round(s.closed * 100)}% closed`), faultText: 'controller offline' },
    soil:       { icon: 'moisture', name: 'Soil sensor', ref: 'the soil sensor', initial: { moisture: SOIL_START, probeOut: false }, edit: [{ field: 'moisture', kind: 'slider', min: 0, max: 100, step: 1, unit: '%', top: 'saturated' }], format: (s) => `${s.moisture.toFixed(0)}% moisture`, faultText: `last reading ${SOIL_START}%` },
    pump:       { icon: 'pump', name: 'Water pump', ref: 'the water pump', initial: { running: false, blocked: false }, edit: [{ field: 'running', kind: 'switch' }], format: (s) => (s.running ? 'running' : 'idle'), faultText: 'controller offline' },
    camera:     { icon: 'camera', name: 'Door camera', ref: 'the door camera', initial: { armed: false, motion: false, off: false }, edit: [{ field: 'armed', kind: 'switch', label: 'armed' }, { field: 'motion', kind: 'switch', label: 'motion' }], format: (s) => (s.off ? 'off' : s.armed ? (s.motion ? 'motion at door' : 'armed · no motion') : 'standby'), faultText: 'missed its check-ins' },
    thermostat: { icon: 'thermostat', name: 'Thermostat', ref: 'the thermostat', initial: { target: 21, current: 21.4, cold: false }, edit: [{ field: 'current', kind: 'slider', min: 5, max: 35, step: 0.5, unit: '°C', label: 'now' }, { field: 'target', kind: 'slider', min: 10, max: 28, step: 0.5, unit: '°C', label: 'set' }], format: (s) => `${s.target.toFixed(1)}°C set · ${s.current.toFixed(1)}°C now`, faultText: 'off the network', control: { kind: 'bar', field: 'target', min: 10, max: 28 } }, // the bar shows the set point on the thermostat's 10–28 °C range
  },

  build({ R, P, M, THREE, store, speed = 1, quiet }) {
    // True while a what-if replays the request quietly (A2): the store hooks below ring and pulse nothing meanwhile.
    const isQuiet = typeof quiet === 'function' ? quiet : () => false;
    R.scene.background = new THREE.Color(0xDCEBF3);
    // ceilShadows: the floor lamp's shade keeps its light off the ceiling outside the shade's top opening (see the lamp).
    P.roomShell({ w: 5, d: 5, h: 2.8, window: { x: 0.7, y: 1.55, ww: 1.7, wh: 1.36 }, ceilShadows: true });
    // Skirting and crown on the right and front walls too (the shell trims the back and left walls only, and from outside
    // those two a bare joint showed on the far walls), as in elder.js: they hide with their walls, sit a few mm lower than
    // the shell's so no two trims share a face in a corner, and the front ones stop at the side walls' trims.
    for (const [th, ty] of [[0.098, 0.049], [0.057, 2.8 - 0.0295]]) { // skirting, crown
      P.onWall(P.flatNormals(P.box(0.02, th, 5, M.trim, 2.49, ty, 0), -1, 0, 0), -1, 0, -2.5);     // right wall, x = 2.5
      P.onWall(P.flatNormals(P.box(4.96, th, 0.02, M.trim, 0, ty, 2.49), 0, 0, -1), 0, -1, -2.5);  // front wall, z = 2.5
    }
    // Everything a builder adds to the room: P.blinds returns its slats but not the headrail, and both hang on the back
    // wall (tagged below).
    const addedBy = (build) => { const before = new Set(R.scene.children); const ret = build(); return [ret, R.scene.children.filter((o) => !before.has(o))]; };
    // 18 slats at a 7 cm pitch: tilted to 1.3 rad a 7.5 cm slat covers more than the pitch, so "100% closed" closes.
    // 1.74 m wide, past the 1.65 m between the window trims: the blind hangs 4 cm in front of them, so anything
    // narrower leaves bare glass at the sides from the room camera. The lowest slat (y 0.95) clears the sill (top 0.885).
    const [slats, blindParts] = addedBy(() => P.blinds(0.7, 2.14, -2.42, 1.74, 18, 0.07));
    // One shared slat geometry: the thin front/back edge faces take the broad face's normal, so a closed blind has no
    // dark sub-pixel line per slat (without MSAA, on phones, those broke into a fan of dashes), and a light→darker tint
    // from each slat's back edge to its front edge still reads as separate slats.
    {
      const geo = slats[0].geometry.clone(), nrm = geo.attributes.normal, pos = geo.attributes.position, col = [];
      for (let i = 0; i < nrm.count; i++) { if (Math.abs(nrm.getZ(i)) > 0.5) nrm.setXYZ(i, 0, 1, 0); const c = 0.84 + 0.16 * (0.5 - pos.getZ(i) / 0.075); col.push(c, c, c); }
      geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
      const mat = M.slat.clone(); mat.vertexColors = true;
      slats.forEach((s) => { s.geometry.dispose(); s.geometry = geo; s.material = mat; });
    }
    // Motor status LED on the headrail's front face, at the left end: at the right end the lamp's bloom drowns it.
    const blindsLed = P.ledDot(-0.08, 2.2, -2.376, 0x2FBF71, 0.01); P.onWall(P.group(blindsLed), 0, 1, -2.5); // see the cut-away note below
    // Screen-sized glow for a small status LED in fault (the LED itself is a 1–2 px dot from the room camera).
    // Depth-tested, but kept 12 cm towards the camera each frame so the device it sits on never cuts it in half.
    // Normal blending, not additive: these sit on white walls, where an additive red just washes out to white.
    // wall: [nx, nz, d] for a glow on a wall-hung device; it sits in a holder tagged with that wall (see the cut-away note).
    const glowMap = (() => { const c = document.createElement('canvas'); c.width = c.height = 64; const g = c.getContext('2d'); const rg = g.createRadialGradient(32, 32, 0, 32, 32, 32); rg.addColorStop(0, 'rgba(255,255,255,1)'); rg.addColorStop(0.22, 'rgba(255,255,255,.85)'); rg.addColorStop(0.5, 'rgba(255,255,255,.3)'); rg.addColorStop(1, 'rgba(255,255,255,0)'); g.fillStyle = rg; g.fillRect(0, 0, 64, 64); const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return t; })();
    // size: the sprite's screen size. The returned function's .at is its anchor (the probe's glow follows the probe).
    const glow = (x, y, z, { hex = 0xE8402A, wall = null, size = 0.04 } = {}) => {
      const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowMap, color: hex, transparent: true, depthWrite: false, sizeAttenuation: false, opacity: 0 }));
      sp.scale.setScalar(size); sp.visible = false; sp.userData.noAO = true; sp.raycast = () => {}; R.scene.add(sp); if (wall) P.onWall(P.group(sp), ...wall);
      const at = new THREE.Vector3(x, y, z), v = new THREE.Vector3();
      const fn = (k) => { sp.visible = k > 0.01; if (!sp.visible) return; sp.material.opacity = k; v.subVectors(R.camera.position, at); const d = v.length() || 1; sp.position.copy(at).addScaledVector(v, Math.min(0.12, d * 0.5) / d); };
      fn.at = at; return fn;
    };
    const blindsGlow = glow(-0.08, 2.2, -2.37, { wall: [0, 1, -2.5] });
    P.sofa(0.5, -1.86); // backrest rear face at z -2.32, just clear of the window sill (front face z -2.33)
    P.rug(0.5, -0.55); P.table(0.5, -0.6, 1.0, 0.5); P.box(0.18, 0.025, 0.25, M.art, 0.3, 0.4525, -0.6);
    const plant = P.plant(-1.9, -1.75);
    // The soil darkens as it takes up water: 20% moisture reads dry and pale, 45% dark and wet.
    const soilMat = M.soil.clone(); plant.soil.material = soilMat; const DRY = new THREE.Color(0x7B5B40), WET = new THREE.Color(0x2A1C12); let soilShown = null;
    const stake = P.box(0.018, 0.34, 0.018, M.metal, -1.74, 0.5, -1.66); const sensorHead = P.box(0.06, 0.05, 0.03, M.hub, -1.74, 0.69, -1.66);
    const soilLed = P.ledDot(-1.74, 0.697, -1.64, 0x2FBF71, 0.009); // status LED on the sensor head's front face
    const soilGlow = glow(-1.74, 0.697, -1.635); // its faults are what-if stories: keep them visible after the ping
    // A probe knocked out of the pot (soil.probeOut, p0's soil what-if) tips out over the rim and leans on the pot's
    // outside wall: inside the pot there is no room to lay a 39 cm probe flat among the stems. Stake, head and LED hang
    // under one pivot at the stake's foot; out, the foot is on the floor in front of the pot and the top end rests on the
    // pot wall 60° round from +x (towards the room, clear of the hose and the pump), the LED face (local +z) outwards.
    const probe = new THREE.Group(); probe.position.set(-1.74, 0.33, -1.66); R.scene.add(probe);
    for (const m of [stake, sensorHead, soilLed]) probe.attach(m);
    const PROBE_HOME = probe.position.clone(), PROBE_FOOT = new THREE.Vector3(-1.699, 0.006, -1.402), PROBE_TOP = new THREE.Vector3(-1.776, 0.362, -1.535);
    const PROBE_Q = (() => {
      const y = PROBE_TOP.clone().sub(PROBE_FOOT).normalize(), r = new THREE.Vector3(Math.cos(Math.PI / 3), 0, Math.sin(Math.PI / 3));
      const z = r.addScaledVector(y, -r.dot(y)).normalize(), x = new THREE.Vector3().crossVectors(y, z);
      return new THREE.Quaternion().setFromRotationMatrix(new THREE.Matrix4().makeBasis(x, y, z));
    })();
    let probeShown = null; const SOIL_GLOW_HOME = soilGlow.at.clone();
    const pump = P.box(0.26, 0.2, 0.2, M.black, -1.35, 0.1, -1.5, 0.02);
    // Water reservoir on top of the pump, so it's clear where the water comes from.
    const tank = P.box(0.24, 0.2, 0.18, new THREE.MeshPhysicalMaterial({ color: 0xE8F4F8, roughness: 0.08, transparent: true, opacity: 0.28, depthWrite: false }), -1.35, 0.3, -1.5, 0.012);
    tank.castShadow = false; tank.userData.noAO = true; tank.renderOrder = 1; // drawn after the water inside it
    const water = P.box(0.22, 0.13, 0.16, new THREE.MeshStandardMaterial({ color: 0x4FA8CF, roughness: 0.2, transparent: true, opacity: 0.75 }), -1.35, 0.27, -1.5);
    const tankLid = P.box(0.25, 0.012, 0.19, M.black, -1.35, 0.406, -1.5, 0.004);
    // Hose from the pump over the pot rim to a drip end 8 cm above the soil, its mouth turned down; clears the tank, the rim
    // and the sensor stake. The drip end is 12 cm from the plant's centre, just clear of the stems on the room's side: in
    // among the stems the drops were hidden from the front and the right.
    const DRIP = [-1.855, 0.5, -1.636];
    P.tube([[-1.46, 0.12, -1.5], [-1.56, 0.3, -1.52], [-1.68, 0.55, -1.56], [-1.79, 0.55, -1.6], [-1.845, 0.52, -1.628], DRIP]);
    const pumpLed = P.ledDot(-1.35, 0.15, -1.39, 0x2FBF71);
    const pumpGlow = glow(-1.35, 0.15, -1.385); // pumpLed is a 1–2 px dot at floor level
    // Drops falling from the hose mouth to the soil (6 cm) while the pump runs, 2 cm apart so they read as separate drops.
    const dropMat = new THREE.MeshStandardMaterial({ color: 0xBFE6F5, emissive: 0x3AB7FF, emissiveIntensity: 0.35, roughness: 0.1, transparent: true, opacity: 0.9 });
    const drops = [0, 1, 2].map(() => { const d = new THREE.Mesh(new THREE.SphereGeometry(0.0055, 8, 8), dropMat); d.visible = false; d.userData.noAO = true; R.scene.add(d); return d; });
    // shaded: the shade blocks its bulb on every tier (P.floorLamp). Without it the bulb lit the walls, ceiling and room
    // straight through the shade: a bright blob round the shade instead of light from its top and bottom openings.
    const lamp = P.floorLamp(1.95, -1.6, { shaded: true });
    // A soft rim to the light: at the default PCF radius (1) the shade's shadow edge was knife-sharp, a hard arc across
    // the floor and a stair-stepped ellipse on the ceiling. 8 softens both without visible grain at 1x (three's point-light
    // PCF takes 5 samples: from about 12 the edge turns to stipple). The phone tier's spot stand-ins have their own penumbra.
    lamp.bulb.shadow.radius = 8;
    // The lamp's controller: a smart dimmer plug in a wall socket on the back wall, between the sofa's right arm and the
    // lamp, with a status LED and a cord along the floor to the lamp base (the what-if copy calls it "its plug"). Right
    // of the lamp (x 2.2) it sat about 7 CSS px from the canvas edge on a 375 px phone. Its glow is larger than the
    // others: the lamp lights this wall.
    const socket = P.box(0.08, 0.08, 0.008, M.white, 1.75, 0.3, -2.496, 0.004);
    const plugBody = P.box(0.055, 0.075, 0.04, M.white, 1.75, 0.3, -2.472, 0.008);
    const lampLed = P.ledDot(1.75, 0.325, -2.451, 0x2FBF71, 0.006); // on the plug's front face
    P.tube([[1.75, 0.27, -2.46], [1.75, 0.03, -2.42], [1.8, 0.02, -2.1], [1.9, 0.022, -1.8]], 0.004);
    for (const o of [socket, plugBody]) P.onWall(o, 0, 1, -2.5);
    P.onWall(P.group(lampLed), 0, 1, -2.5);
    const lampGlow = glow(1.75, 0.325, -2.446, { wall: [0, 1, -2.5], size: 0.06 });
    P.table(-1.75, -0.3, 0.6, 0.42, 0.55, M.wood, M.black);
    const hub = P.hub(-1.75, 0.57, -0.3); R.addPickable(hub.group, 'hub');
    const phone = P.phone(-1.55, 0.575, -0.22, -0.4);
    // Left wall: front door with the door camera in it, thermostat beside it. Wall-mounted parts sit on the wall plane.
    P.doorLeft(-2.5, 0.45); // tags its own parts with the left wall
    // Door camera: a door viewer at eye height. Its lens sits in the door and looks out at the mat; the room side is
    // the body with an eyepiece and the status LED.
    const camBody = P.box(0.022, 0.13, 0.075, M.black, -2.459, 1.5, 0.45, 0.008);
    const camEye = P.cyl(0.013, 0.013, 0.006, M.anodized, -2.447, 1.475, 0.45, 20); camEye.rotation.z = Math.PI / 2;
    const camLed = P.ledDot(-2.447, 1.545, 0.45, 0x2FBF71, 0.007);
    const cam = { group: P.group(camBody, camEye, camLed), led: camLed };
    const camGlow = glow(-2.44, 1.545, 0.45, { wall: [1, 0, -2.5] }); // the LED itself is a 7 mm dot
    P.picture(-2.48, 1.6, -1.0); // tagged with the left wall (x -2.5) by P.picture
    const thermo = P.box(0.03, 0.11, 0.11, M.white, -2.483, 1.45, -0.3, 0.01);
    const thermoCanvas = document.createElement('canvas'); thermoCanvas.width = 256; thermoCanvas.height = 144;
    const thermoTex = new THREE.CanvasTexture(thermoCanvas); thermoTex.colorSpace = THREE.SRGBColorSpace;
    const thermoScreen = P.screenPlane(0.07, 0.04, thermoTex); thermoScreen.rotation.y = Math.PI / 2; thermoScreen.position.set(-2.466, 1.46, -0.3); thermoScreen.material.emissiveIntensity = 0.7;
    const thermoLed = P.ledDot(-2.466, 1.492, -0.34, 0xE0563A, 0.006); thermoLed.visible = false; P.onWall(P.group(thermoLed), 1, 0, -2.5); // alert LED, lit only in a fault
    const thermoGlow = glow(-2.46, 1.492, -0.34, { wall: [1, 0, -2.5] });
    // The room too cold for its set point (thermostat.cold, p3's heating what-if): a slow amber glow at the alert LED's
    // spot, never the red fault look: the thermostat answers, only the room is off target. The screen's amber "now" line
    // alone is a few pixels from the room camera.
    const thermoWarn = glow(-2.46, 1.492, -0.34, { hex: 0xFFB020, wall: [1, 0, -2.5] });
    let thermoShown = -1; // numeric key of what the screen shows (-1 offline, -2 fault); update() builds no strings unless it changes
    // Set point large, measured room temperature small underneath (one canvas and texture, redrawn on change).
    // A fault fills the screen red, so it reads as an alarm even as a few pixels from across the room. warn: the room
    // is too cold for the set point (thermostat.cold), so the small "now" line turns amber.
    function thermoText(big, small, alarm = false, warn = false) {
      const g = thermoCanvas.getContext('2d'); g.fillStyle = alarm ? '#5A120D' : '#0B0F14'; g.fillRect(0, 0, 256, 144);
      g.textAlign = 'center'; g.textBaseline = 'middle';
      g.fillStyle = alarm ? '#FF7A62' : '#9FE3F0'; g.font = '600 70px Inter, "Segoe UI", sans-serif'; g.fillText(big, 128, small ? 56 : 72);
      if (small) { g.fillStyle = alarm ? '#FFC2B6' : warn ? '#FFB020' : '#6FB3C2'; g.font = '30px Inter, "Segoe UI", sans-serif'; g.fillText(small, 128, 116); }
      thermoTex.needsUpdate = true;
    }
    thermoText('--', '');
    // Dollhouse cut-away: seen from outside, a wall is see-through (single-sided), so what hangs on it hides with it
    // instead of floating in the air. The renderer owns .visible of anything tagged with P.onWall (renderer.js header).
    // Parts whose visibility also follows their device's state (the camera, blinds and thermostat LEDs, the fault glows)
    // hang in a tagged parent instead: the renderer shows the parent, update() shows the part.
    for (const o of [cam.group, thermo, thermoScreen]) P.onWall(o, 1, 0, -2.5);
    for (const o of blindParts) P.onWall(o, 0, 1, -2.5);
    R.addPickable(lamp.group, 'lamp'); R.addPickable(lamp.shade, 'lamp'); R.addPickable(plugBody, 'lamp');
    slats.forEach((s) => R.addPickable(s, 'blinds')); R.addPickable(blindsLed, 'blinds');
    R.addPickable(sensorHead, 'soil'); R.addPickable(plant.pot, 'soil'); R.addPickable(soilLed, 'soil');
    R.addPickable(tank, 'pump', { anchor: true }); R.addPickable(pump, 'pump'); R.addPickable(water, 'pump'); R.addPickable(tankLid, 'pump'); // rings sit on top of the unit
    R.addPickable(cam.group, 'camera'); R.addPickable(thermo, 'thermostat'); R.addPickable(thermoScreen, 'thermostat');

    const hi = P.highlighter({ lamp: [lamp.shade, plugBody], blinds: slats, soil: [sensorHead, plant.pot], pump: [pump, tank], camera: [camBody], thermostat: [thermo] });
    // Motion at the door is otherwise only an LED colour: mark it in the room. The label never outlives the motion: the
    // ping waits a tick and needs the motion still on (a beat skip runs a visit and clears it in one go), and when the
    // motion ends while the label can still be on screen (hold + its 600 ms fade: a beat skip, or ?speed= review mode)
    // it goes too, and so does the camera's highlight pulse (camHiCut, until the next focus on it). R.clearMarkers()
    // also drops a label the visitor tapped up in that moment; R.unping(id) drops just this one, used when there is one.
    // Holds are ms at normal speed and shrink with ?speed= (as in the lab). A what-if's quiet replay rings nothing: it
    // passes through the visits of p0 (Wednesday) and p3 (Saturday) on the way to later failure points (A2). Both amber
    // cues (motion, a room below target) pulse their device amber, like their rings: a bare focus(id) pulses cyan.
    const MOTION_HOLD = Math.max(1200, 2500 / speed); let motionAt = -Infinity, camHiCut = false;
    const focus = (id, hex) => { if (id === 'camera') camHiCut = false; hi.focus(id, hex); };
    store.subscribe((state, path, value) => {
      if (isQuiet()) return;
      if (path === 'camera.motion' && value) setTimeout(() => { if (isQuiet() || !store.get('camera.motion')) return; focus('camera', 0xFFB020); motionAt = performance.now(); R.ping('camera', 'Motion at the door', { color: '#FFB020', hex: 0xFFB020, hold: MOTION_HOLD }); }, 0);
      if (path === 'camera.motion' && !value && performance.now() - motionAt < MOTION_HOLD + 600) { motionAt = -Infinity; camHiCut = true; if (R.unping) R.unping('camera'); else R.clearMarkers(); }
      // The room too cold for its set point (p3's heating what-if): an amber warning at the thermostat, not a fault.
      if (path === 'thermostat.cold' && value) { focus('thermostat', 0xFFB020); R.ping('thermostat', `Room ${(state.thermostat.target - state.thermostat.current).toFixed(1)}° below target`, { color: '#FFB020', hex: 0xFFB020, hold: Math.max(1200, 2200 / speed) }); }
    });

    const led = (m, hex, intensity) => { m.material.color.set(hex); m.material.emissive.copy(m.material.color); m.material.emissiveIntensity = intensity; };
    return {
      focus,
      update(s, t) {
        hi.update(); if (camHiCut) camBody.traverse((o) => { if (o.userData.isShell) o.visible = false; });
        R.daylight(s.env.hour);
        const blink = Math.floor(t * 3) % 2 === 0;
        // A stalled motor (blinds.jammed: something caught in the cord or track) leaves the slats uneven and the stack
        // hanging askew from the headrail. A flat battery (blinds.flat) stops them even, with the headrail LED dark (no
        // charge left to light it) and the red glow as the room's fault marker. A controller that is just offline
        // (the generic what-if) keeps them even too: the room claims no stall.
        const blindsBad = s.blinds.status === 'fault', stalled = blindsBad && !!s.blinds.jammed, skew = 0.03 + 0.12 * (1 - s.blinds.closed);
        slats.forEach((sl, i) => { sl.rotation.x = s.blinds.closed * 1.3 + (stalled ? Math.sin(i * 1.9) * skew : 0); sl.rotation.z = stalled ? (0.03 * i) / (slats.length - 1) : 0; });
        blindsLed.visible = s.blinds.status !== 'offline' && !(blindsBad && s.blinds.flat); led(blindsLed, blindsBad ? 0xE0563A : 0x2FBF71, blindsBad ? (blink ? 6 : 1) : 1.5);
        blindsGlow(blindsBad ? (blink ? 1 : 0.25) : 0);
        const b = s.lamp.brightness; lamp.bulb.intensity = b * 9; lamp.bulbMesh.material.emissiveIntensity = b * 3; lamp.shadeMat.emissiveIntensity = b * 0.35;
        // The lamp's light follows its brightness in a fault too (dark over a red plug, or lit and stuck on).
        const lampBad = s.lamp.status === 'fault'; lampLed.visible = s.lamp.status !== 'offline';
        led(lampLed, lampBad ? 0xE0563A : 0x2FBF71, lampBad ? (blink ? 6 : 1) : 1.5); lampGlow(lampBad ? (blink ? 1 : 0.25) : 0);
        // No cube-shadow passes while the lamp is dark; render once up front so the depth map exists for the sampler.
        const lampShadow = lamp.bulb.shadow; lampShadow.autoUpdate = b > 0.02; if (!lampShadow.map) lampShadow.needsUpdate = true;
        const bad = s.pump.status === 'fault';
        led(pumpLed, bad ? 0xE0563A : s.pump.running ? 0x3AB7FF : 0x2FBF71, bad ? (blink ? 6 : 1) : s.pump.running ? 4 : 2);
        pumpLed.visible = s.pump.status !== 'offline'; pumpGlow(bad ? (blink ? 1 : 0.25) : 0);
        // A blocked hose (pump.blocked): the motor runs, the LED is blue, and no drop reaches the hose mouth.
        drops.forEach((d, i) => { d.visible = s.pump.running && !bad && !s.pump.blocked; if (d.visible) d.position.set(DRIP[0], DRIP[1] - 0.012 - ((t * 2.4 + i / 3) % 1) * 0.06, DRIP[2]); });
        if (s.soil.moisture !== soilShown) { soilShown = s.soil.moisture; const k = THREE.MathUtils.clamp((soilShown - 20) / 25, 0, 1); soilMat.color.lerpColors(DRY, WET, k); soilMat.roughness = 1 - 0.3 * k; }
        const soilBad = s.soil.status === 'fault';
        led(soilLed, soilBad ? 0xE0563A : 0x2FBF71, soilBad ? (blink ? 6 : 1) : 2);
        const out = !!s.soil.probeOut;
        if (out !== probeShown) {
          probeShown = out;
          if (out) { probe.position.copy(PROBE_FOOT); probe.quaternion.copy(PROBE_Q); } else { probe.position.copy(PROBE_HOME); probe.quaternion.identity(); }
          probe.updateMatrixWorld(true); if (out) soilLed.getWorldPosition(soilGlow.at); else soilGlow.at.copy(SOIL_GLOW_HOME); // the glow follows the LED
        }
        soilLed.visible = s.soil.status !== 'offline'; soilGlow(soilBad ? (blink ? 1 : 0.25) : 0);
        const camBad = s.camera.status === 'fault';
        led(cam.led, camBad ? 0xE0563A : s.camera.armed ? (s.camera.motion ? 0xFFB020 : 0xE0563A) : 0x2FBF71, camBad ? (blink ? 6 : 1) : s.camera.motion ? (blink ? 6 : 3) : s.camera.armed ? 3 : 2);
        cam.led.visible = s.camera.status !== 'offline' && (camBad || !s.camera.off); // switched off: LED dark
        camGlow(camBad ? (blink ? 1 : 0.25) : 0);
        const hubOn = s.hub.status === 'on'; hub.ledMat.emissiveIntensity = hubOn ? (2.5 + Math.sin(t * 2.2) * 1.5) * s.hub.led : 0;
        const th = s.thermostat, thBad = th.status === 'fault', thKey = th.status === 'offline' ? -1 : thBad ? -2 : Math.round(th.target) * 10000 + Math.round(th.current * 10) + (th.cold ? 0.5 : 0);
        if (thKey !== thermoShown) {
          thermoShown = thKey;
          if (thKey === -1) thermoText('--', '');
          else if (thKey === -2) thermoText('!!', 'no network', true);
          else thermoText(`${Math.round(th.target)}°C`, `now ${th.current.toFixed(1)}°`, false, !!th.cold);
        }
        thermoLed.visible = thBad; if (thBad) led(thermoLed, 0xE0563A, blink ? 6 : 1);
        thermoGlow(thBad ? (blink ? 1 : 0.25) : 0);
        thermoWarn(!thBad && th.status !== 'offline' && th.cold ? 0.55 + 0.3 * Math.sin(t * 2.4) : 0);
      },
    };
  },

  prompts: [
    {
      chip: 'I\'m away for a week. Make the place look lived in, and don\'t let my plants die.',
      keywords: ['away', 'sunday', 'week', 'lived', 'plants', 'die', 'holiday', 'trip', 'travel', 'gone', 'vacation'],
      rulesOut: NOT_HERE,
      // The clean week under every Edit (thermostat.target ends at 21 or, held warm, 20). Every device is named in the
      // Plan-beat line, so each has its own what-if and the engine's generic one never runs here.
      expect: { 'pump.status': 'online', 'pump.running': false, 'soil.status': 'online', 'lamp.on': false, 'blinds.closed': 0, 'camera.armed': true, 'camera.motion': false, 'thermostat.status': 'online' },
      steps: [
        { beat: 'plan' },
        { say: 'Got it. Let me see what I have to work with.' },
        { status: 'Checking connected devices' },
        { say: 'Six devices: the floor lamp, the blinds, the soil sensor, the pump, the door camera and the thermostat. Here\'s what I\'d do — nothing runs until you approve.' },
        { status: 'Waiting for your approval' },
        { plan: { intro: 'Plan for the week you\'re away:', steps: [
          { text: 'Evenings: lamp on a varied schedule between 18:30 and 23:00, like someone is home', alt: { text: 'Evenings: lamp on at a fixed 19:00–22:00', apply: [{ set: 'plan.lampFixed', to: true }] } },
          { text: 'Blinds close as the sun sets, open again at 08:00' },
          { text: 'Water the plant for 40 seconds whenever soil moisture drops below 30%' },
          { text: 'Door camera armed; I\'ll message you if anyone comes to the door' },
          { text: 'Thermostat held at 18°C to save energy, back to 21°C the afternoon you\'re due home', alt: { text: 'Thermostat held at 20°C the whole time', apply: [{ set: 'plan.warm', to: true }] } },
        ] } },
        { beat: 'run' },
        { say: 'Running from now. I\'ll show you the week at speed.' }, // no "Approved": the plan may have been skipped past
        { failPoint: 'start' },
        { status: 'Friday 17:40' },
        { parallel: [{ tween: 'env.hour', to: 17.67, ms: 1500 }, { fn: ({ store, tween }) => tween('thermostat.target', setPoint(store), 1200) }] },
        { set: 'camera.armed', to: true, label: 'Door camera armed' },
        { parallel: [{ tween: 'env.hour', to: 18.58, ms: 3000 }, { fn: ({ store, tween }) => tween('thermostat.current', store.get('plan.warm') ? 20.3 : 18.4, 3000) }] },
        { status: 'Friday 18:35' },
        { fn: (c) => say(c, c.store.get('plan.lampFixed') ? '18:35 — sun\'s going. Blinds closing; the lamp waits for 19:00, as you set it.' : '18:35 — sun\'s going. Lamp on at 70%, blinds closing.') },
        // Fixed schedule: blinds close first, the lamp comes on at 19:00.
        { fn: async (c) => { if (!c.store.get('plan.lampFixed')) return; await Promise.all([c.tween('blinds.closed', 1, 3000), c.tween('env.hour', 19, 3000)]); c.status('Friday 19:00'); await say(c, '19:00 — lamp on at 70%.'); } },
        { failPoint: 'evening' },
        { set: 'lamp.on', to: true },
        { parallel: [{ tween: 'blinds.closed', to: 1, ms: 3000, label: 'Blinds → closing' }, { tween: 'env.hour', to: 19.6, ms: 3000 }, { tween: 'lamp.brightness', to: 0.7, ms: 1200, label: 'Lamp → 70%' }] },
        { wait: 700 },
        { fn: (c) => say(c, c.store.get('plan.lampFixed') ? 'Overnight — lamp off at 22:00 as you set it. Blinds stay closed.' : 'Overnight — lamp off at 22:50 tonight, 22:25 tomorrow. Never the same minute twice. Blinds stay closed.') },
        { parallel: [{ tween: 'env.hour', to: 22.9, ms: 2500 }, { tween: 'lamp.brightness', to: 0, ms: 1600, label: 'Lamp → off' }] },
        { set: 'lamp.on', to: false },
        { failPoint: 'night' },
        { status: 'Saturday 08:00' },
        { parallel: [{ tween: 'env.hour', to: 32, ms: 2000 }, { tween: 'soil.moisture', to: 31, ms: 2000 }] },
        { failPoint: 'morning' },
        { say: 'Saturday 08:00 — soil is drifting down: 31%. Opening the blinds.' },
        { tween: 'blinds.closed', to: 0, ms: 2000, label: 'Blinds → open' },
        { status: 'Saturday 14:20' },
        { parallel: [{ tween: 'env.hour', to: 38.33, ms: 1500 }, { tween: 'soil.moisture', to: 29, ms: 1500 }] },
        { failPoint: 'watering' },
        { say: '14:20 — soil at 29%. Starting a 40-second watering.' },
        { set: 'pump.status', to: 'busy' }, { set: 'pump.running', to: true, label: 'Pump → running' },
        { wait: 1800 },
        { failPoint: 'pumping' },
        { tween: 'soil.moisture', to: 41, ms: 2500 },
        { set: 'pump.running', to: false }, { set: 'pump.status', to: 'online', label: 'Pump → idle' },
        { say: 'Soil at 41%. The sensor saw the water arrive, so that\'s a watering I can vouch for, not just a pump saying it ran.' },
        { failPoint: 'midweek' },
        { status: 'Wednesday 11:05' },
        { set: 'env.hour', to: 130.9 }, // Wednesday 10:54: daylight, like Saturday 14:20, so no night flash on the jump
        { parallel: [{ tween: 'env.hour', to: 131.08, ms: 1500 }, { tween: 'soil.moisture', to: 36, ms: 1500 }] }, // the soil has dried since Saturday
        { set: 'camera.motion', to: true, label: 'Motion at the door' },
        { say: 'Wednesday 11:05 — motion at the door for about 30 seconds, then nothing. I\'ve messaged you.' },
        { wait: 1200 },
        { set: 'camera.motion', to: false },
        { status: 'Next Friday 16:00' },
        { set: 'env.hour', to: 182.4 }, // same time of day a week on: no day/night flicker on the jump
        { parallel: [{ tween: 'env.hour', to: 184, ms: 2000 }, { tween: 'soil.moisture', to: 33, ms: 2000 }] },
        { fn: (c) => say(c, `Next Friday 16:00 — you're due back. ${c.store.get('plan.warm') ? 'Thermostat held 20°C all week, as you set it' : 'Thermostat going from 18°C back to 21°C'}; blinds open; the lamp's schedule ends tonight.`) },
        { say: 'Soil at 33%. It was watered each time it dipped under 30%, and each watering showed up on the sensor.' },
        // "Held at 20°C the whole time" (Edit) leaves the set point alone; the default plan warms back up to 21°C.
        { fn: (c) => { if (c.store.get('plan.warm')) return; c.status('Thermostat → 21°C'); return Promise.all([c.tween('thermostat.target', 21, 1200), c.tween('thermostat.current', 20.6, 2500)]); } },
        { status: 'Away plan finished · Friday 16:00' },
        { end: KS_END },
      ],
      // What-ifs (opt-in, from the success card). Home never forces a motor, never retries a dead pump, never waters on a
      // number it doesn't believe, and never lets silence pass for "all fine"; the owner is away, so each one ends by
      // saying what changed, what NCB couldn't see, and the one thing a person has to do.
      whatIf: {
        // Never takes the away set point: it may hold its last setting, or have lost power. NCB can't tell which.
        thermostat: { at: 'start', intro: 'Replaying this request. This time, the thermostat stops answering just as the away plan starts.', steps: [
          { status: (st) => `Friday 17:40 · Thermostat → ${setPoint(st)}°C` },
          { tween: 'env.hour', to: 17.67, ms: 1500 },
          { fail: 'thermostat', faultText: 'no reply since 17:40', title: '⚠ Thermostat not answering', say: '17:40 — the thermostat didn\'t take the new set point: three tries, no reply. The last thing it told me was 21°C set, 21.4°C in the room.' },
          { wait: 1500 },
          { say: 'Many thermostats keep their last setting offline, so it may still hold 21°C. If it lost power, the heating won\'t run at all. I can\'t read the room to tell which.' },
          { set: 'camera.armed', to: true },
          sayWith((st) => `What changes: ${st.get('plan.warm') ? 'the house may sit at 21°C, not your 20°C' : 'you probably won\'t get the saving from 18°C'}. The lamp, blinds, watering and door camera don't need the thermostat, so they run as planned.`),
          replanWith((st) => ({ intro: 'Away plan, without the heating change:', changes: [`Thermostat: no reply since 17:40, last report 21°C set. ${st.get('plan.warm') ? 'Your 20°C' : 'The 18°C away setting'} isn't applied`, 'Lamp, blinds, watering and door camera run exactly as planned', 'I\'ll try it every 15 minutes and apply your setting when it answers. If it\'s still silent at 22:00, I\'ll message you again', 'I won\'t report a room temperature until it answers'], needsYou: 'If you\'re still at home, restart the thermostat before you leave. If you\'ve gone, ask someone to check that its screen is on.' })),
          // The evening runs without the thermostat.
          { fn: async (c) => { const fixed = c.store.get('plan.lampFixed'); c.status(fixed ? 'Friday 19:00 · Lamp → 70%' : 'Friday 18:35 · Lamp → 70%, blinds → closing'); await Promise.all([c.tween('env.hour', fixed ? 19 : 18.58, 2500), c.tween('blinds.closed', 1, 2500)]); c.store.set('lamp.on', true); await c.tween('lamp.brightness', 0.7, 1200); } },
          { status: 'Away plan running · thermostat flagged' },
          HOLD,
          { end: { headline: 'The away setting never landed.', body: 'The thermostat never took the away setting. NeuCharBox said what that could mean, said it couldn\'t check which, and ran the rest of the plan without it.' } },
        ] },
        // The lamp's plug drops off the network at dusk. Nothing in this room measures light, so NCB treats it as off.
        lamp: { at: 'evening', intro: 'Replaying this request. This time, the floor lamp doesn\'t come on in the evening.', steps: [
          { status: (st) => (st.get('plan.lampFixed') ? 'Friday 19:00 · Lamp → 70%' : 'Friday 18:35 · Lamp → 70%, blinds → closing') },
          { fn: ({ store, tween }) => (store.get('plan.lampFixed') ? tween('env.hour', 19.1, 1200) : Promise.all([tween('blinds.closed', 1, 3000), tween('env.hour', 18.75, 3000)])) },
          { fail: 'lamp', faultText: 'no reply to "on"', title: '⚠ Lamp not answering', say: 'The floor lamp didn\'t answer "on", three times over. I can\'t tell whether it came on, so I\'m treating it as off.' },
          { wait: 1500 },
          { say: 'There\'s no other light in this room I can switch, so I can\'t cover for it.' },
          { say: 'The blinds are closed and the door camera is armed. From the street, though, the house will likely look dark in the evenings, and you should know that now.' },
          { tween: 'env.hour', to: 19.6, ms: 1500 },
          replanWith((st) => ({ intro: 'Away plan, without the lamp:', changes: [`Floor lamp: no reply since ${st.get('plan.lampFixed') ? '19:00' : '18:35'}; I'm treating it as off`, 'Blinds still close at dusk and open at 08:00', 'Watering, door camera and thermostat unchanged', 'I\'ll try the lamp every 10 minutes and put it back on your schedule the moment it answers'], needsYou: 'If someone can get in, unplug the lamp\'s smart plug for ten seconds and plug it back in. Otherwise it waits for you.' })),
          { status: 'Away plan running · floor lamp flagged' },
          HOLD,
          { end: { headline: 'The lamp stayed dark. You knew at dusk.', body: 'The floor lamp stopped answering at dusk. NeuCharBox couldn\'t fake a light it didn\'t have, so it told you the house would likely look dark and kept everything else running.' } },
        ] },
        // Something catches in the cord or track and the motor stalls while opening. Same policy as p2's evening stall.
        blinds: { at: 'morning', ask: 'What if the blinds fail?', intro: 'Replaying this request. This time, the blinds jam as they open on Saturday morning.', steps: [
          { status: 'Saturday 08:00 · Blinds → open' },
          { tween: 'blinds.closed', to: 0.7, ms: 1200 },
          { set: 'blinds.jammed', to: true }, // caught in the cord or track: the stall look
          { fail: 'blinds', faultText: 'stalled at 70% closed', title: '⚠ Blinds stalled', say: 'Saturday 08:00 — the blinds stopped at 70% closed while opening, and the motor reports a stall. Something is probably caught in the cord or the track.' },
          { wait: 1500 },
          { say: 'I\'ve stopped the motor: pulling harder on stalled blinds can strip their gears. I\'ll try once, gently, tomorrow at 08:00. If they stall again, I leave them for you.' },
          { say: 'Less light means slower drying, and watering follows the soil, so it adjusts by itself.' },
          { say: 'At night, 70% closed still keeps the room mostly private. The lamp\'s evenings go on as planned.' },
          { replan: { intro: 'Re-planned around the blinds:', changes: ['Blinds left at 70% closed; one gentle retry tomorrow at 08:00, then I leave them alone', 'Watering still follows the soil sensor; with less sun it will be needed less often', 'Lamp, door camera and thermostat unchanged'], needsYou: 'Check the blinds\' cord and track for something caught when you\'re home. After tomorrow\'s try, I won\'t run the motor again without asking you.' } },
          // The evening goes on behind the stuck blind.
          { fn: async (c) => { const fixed = c.store.get('plan.lampFixed'); c.status(`Saturday ${fixed ? '19:00' : '18:40'} · Lamp → 70%`); await c.tween('env.hour', fixed ? 43 : 42.67, 2500); c.store.set('lamp.on', true); await c.tween('lamp.brightness', 0.7, 1200); } },
          { status: 'Away plan running · blinds flagged' },
          HOLD,
          { end: { headline: 'Jammed blinds, stopped before they broke.', body: 'The blinds jammed while opening. NeuCharBox stopped the motor instead of forcing it, allowed one gentle retry, and worked out what less light meant for the plant.' } },
        ] },
        // The probe works loose and tips out of the pot: it reads the air, a few percent. p1's soil story goes silent;
        // this one lies. soil.moisture is the soil itself, never the sensor's claim, so it is never set to 4.
        soil: { at: 'watering', intro: 'Replaying this request. This time, the soil sensor starts reporting a number that can\'t be right.', steps: [
          { status: 'Saturday 14:20 · soil 29%' },
          { wait: 1400 },
          { set: 'soil.probeOut', to: true }, // the probe tips out and leans on the pot: loose, not broken
          { fail: 'soil', faultText: 'reads 4%: probe out?', title: '⚠ Soil reading not believable', say: '14:20 — as the watering came due, the soil sensor fell from 29% to 4%. Soil doesn\'t dry that fast. The probe has most likely come out and is reading the air.' },
          { wait: 1500 },
          { say: 'Taken at face value, 4% would have me watering all afternoon, until the water ran onto the floor. I won\'t act on a reading I don\'t believe.' },
          { say: 'Its last believable reading, 29%, is due a watering under your plan, so it gets one 40-second watering now. I can\'t check that it lands.' },
          { set: 'pump.status', to: 'busy' }, { set: 'pump.running', to: true, label: 'Pump → running · 40 s' },
          { tween: 'soil.moisture', to: 41, ms: 2500 }, // the room shows the soil taking the water; the tile keeps the fault
          { set: 'pump.running', to: false }, { set: 'pump.status', to: 'online', label: 'Pump → idle · watering not confirmed' },
          { replan: { intro: 'Watering without the sensor:', changes: ['Soil sensor set aside: nothing waters on its numbers, however low they read', 'Watering on a timer instead: 30 seconds every second morning at 08:00, roughly what the pot was using', 'Blinds, lamp, door camera and thermostat unchanged'], needsYou: 'Push the sensor\'s probe back into the soil. When its readings make sense again, I\'ll go back to watering by the soil.' } },
          { status: 'Away plan running · soil sensor flagged' },
          HOLD,
          { end: { headline: 'It didn\'t water on a bad number.', body: 'The soil sensor\'s probe came out of the pot and read 4%. NeuCharBox refused to run the pump on a number that couldn\'t be right, and kept the plant on a gentle timer until someone fixes it.' } },
        ] },
        // The pump stops answering mid-watering. A controller that goes silent while commanded on may still be running,
        // so NCB checks the soil four minutes later before it says the pump stopped (water shows within about 10 s).
        pump: { at: 'pumping', intro: 'Replaying this request. This time, the water pump stops answering in the middle of a watering.', steps: [
          { status: 'Saturday 14:20 · watering, 40 s' },
          { wait: 1200 },
          { fail: 'pump', faultText: 'went silent mid-watering', title: '⚠ Pump not answering', say: 'The pump stopped answering six seconds into a 40-second watering.' },
          { set: 'pump.running', to: false }, // the room's truth: it did stop (the drops already hide on fault)
          { wait: 1500 },
          { status: 'Saturday 14:24 · soil 29%' },
          { tween: 'env.hour', to: 38.4, ms: 1200 },
          { say: '14:24 — the soil still reads 29%. If the pump were still running, it would be rising by now, so it has stopped.' },
          { say: 'I\'m not going to keep poking a device that isn\'t answering. Here\'s what I\'ve changed while it\'s out:' },
          { replan: { intro: 'Re-planned around the pump:', changes: ['Watering paused — no retries on a device that isn\'t responding', 'Blinds also half-close in the afternoons, so the plant gets less direct sun and dries slower', 'Lamp, camera and thermostat continue exactly as planned', 'I\'ll ping the pump once an hour and tell you the moment it\'s back'], needsYou: 'Check the pump\'s hose and power when you\'re home. If the soil drops below 20% before then, I\'ll message you so someone can water it by hand.' } },
          { tween: 'blinds.closed', to: 0.5, ms: 2000, label: 'Blinds → 50%' },
          { status: 'Next Friday 16:00' },
          { set: 'env.hour', to: 182.4 }, // same time of day a week on: no day/night flicker on the jump
          { parallel: [{ tween: 'env.hour', to: 184, ms: 2000 }, { tween: 'soil.moisture', to: 14, ms: 2000 }] },
          // The blinds were half-closed for the afternoons since the pump went: they open for your return as NCB says so.
          { parallel: [{ fn: (c) => say(c, `Next Friday 16:00 — you're due back. ${c.store.get('plan.warm') ? 'Thermostat held 20°C all week, as you set it' : 'Thermostat going from 18°C back to 21°C'}; blinds opening for your return; the lamp's schedule ends tonight.`) }, { tween: 'blinds.closed', to: 0, ms: 2000 }] },
          { say: 'The pump never came back. The soil fell under 20% on Tuesday, and I messaged you then. It\'s at 14% now: water the plant tonight, then check the pump.' },
          // "Held at 20°C the whole time" (Edit) leaves the set point alone; the default plan warms back up to 21°C.
          { fn: (c) => { if (c.store.get('plan.warm')) return; c.status('Thermostat → 21°C'); return Promise.all([c.tween('thermostat.target', 21, 1200), c.tween('thermostat.current', 20.6, 2500)]); } },
          { status: 'Away plan finished · water pump flagged' },
          HOLD,
          { end: { headline: 'The pump went quiet. The soil kept watch.', body: 'The pump stopped answering mid-watering. NeuCharBox didn\'t keep poking it, slowed the drying with the blinds, watched the soil all week, and messaged you when it ran low.' } },
        ] },
        // The door camera stops checking in on Tuesday. NCB tells you that day, and won't turn "no alerts" into "nobody came".
        camera: { at: 'midweek', intro: 'Replaying this request. This time, the door camera stops checking in partway through the week.', steps: [
          { status: 'Tuesday 13:10' },
          { set: 'env.hour', to: 108.9 }, // Tuesday 12:54: daylight, like Saturday 14:21, so no night flash on the jump
          { parallel: [{ tween: 'env.hour', to: 109.17, ms: 1500 }, { tween: 'soil.moisture', to: 38, ms: 1500 }] },
          { fail: 'camera', faultText: 'silent since Tue 13:10', title: '⚠ Door camera not checking in', say: 'Tuesday 13:10 — the door camera has missed three check-ins in a row, so right now nothing is watching the door.' },
          { wait: 1500 },
          { say: 'I\'ve messaged you now: it\'s your front door, and you may want someone to look at it this week.' },
          { say: 'Nothing else in the plan uses the camera, so the lamp, blinds, watering and heating carry on.' },
          { replan: { intro: 'Away plan, without the door camera:', changes: ['Door camera: no check-in since Tuesday 13:10', 'I\'ll look for it every 10 minutes and tell you when it\'s back', 'Lamp, blinds, watering and thermostat unchanged', 'Until it\'s back, I can\'t tell you about anyone at the door'], needsYou: 'The camera may only need a restart. If someone has a key, they can do it; otherwise check it when you\'re home.' } },
          { status: 'Wednesday 11:05' },
          { set: 'env.hour', to: 130.9 },
          { parallel: [{ tween: 'env.hour', to: 131.08, ms: 1500 }, { tween: 'soil.moisture', to: 36, ms: 1500 }] },
          { say: 'Wednesday 11:05 — still no camera. If anyone came to the door since Tuesday, I wouldn\'t know: no alerts means no information, not no visitors.' },
          { status: 'Away plan running · door camera flagged' },
          HOLD,
          { end: { headline: 'No camera, so no "all clear".', body: 'When the door camera stopped checking in, NeuCharBox told you that day, not when you got home. It kept the rest of the week running and never called a door it couldn\'t see quiet.' } },
        ] },
        // Special: a 42-minute power cut overnight, NeuCharBox included. Devices come back in their own defaults, not the
        // plan's: the lamp's plug switches on, the camera comes back unarmed. NCB checks the water first, then every
        // device, and says what it can't know about the gap. Nothing ends in fault ('offline' mid-run doesn't ping, A5).
        powerCut: { label: 'The power goes out overnight', ask: 'What if the power goes out overnight?', at: 'night', intro: 'Replaying this request. This time, the power goes out on Friday night, and NeuCharBox goes down with it.', steps: [
          { status: 'Saturday 02:10' },
          { tween: 'env.hour', to: 26.17, ms: 1500 },
          { status: 'Saturday 02:10 · power cut · NeuCharBox is off too' },
          { parallel: [
            { set: 'hub.status', to: 'off' }, { tween: 'hub.led', to: 0, ms: 300 },
            { set: 'lamp.status', to: 'offline' }, { set: 'blinds.status', to: 'offline' }, { set: 'soil.status', to: 'offline' },
            { set: 'pump.status', to: 'offline' }, { set: 'camera.status', to: 'offline' }, { set: 'thermostat.status', to: 'offline' },
          ] },
          // 42 minutes in the dark, heating off with everything else. No NCB line: nothing is running to say one.
          { fn: ({ store, tween }) => Promise.all([tween('env.hour', 26.87, 3000), tween('thermostat.current', store.get('plan.warm') ? 19.8 : 17.9, 3000)]) },
          { set: 'hub.status', to: 'on' }, { tween: 'hub.led', to: 1, ms: 900 }, // the host pings "NeuCharBox · powered"
          { status: 'Saturday 02:52 · power back · checking every device' },
          alertWith(() => '02:52 — I\'ve just restarted. My last log line is 02:10, so I was off for about 40 minutes, most likely a power cut.', '⚠ NeuCharBox restarted'),
          { say: 'I can\'t tell you what happened in those 40 minutes. Checking every device now, instead of assuming the plan picked up where it left off.' },
          { set: 'pump.status', to: 'online' }, { set: 'soil.status', to: 'online' }, { wait: 400 }, // the water first
          { say: 'The pump came back idle, and the soil reads 34%, as before the cut: no water running.' },
          { set: 'blinds.status', to: 'online' }, { wait: 300 },
          { set: 'lamp.status', to: 'online' }, { set: 'lamp.on', to: true }, { set: 'lamp.brightness', to: 0.7 }, { wait: 400 }, // back on by itself
          { set: 'camera.armed', to: false }, { set: 'camera.status', to: 'online' }, { wait: 400 }, // back on standby
          { set: 'thermostat.status', to: 'online' }, { wait: 700 },
          { say: 'The floor lamp came back on by itself, at 70%. Its plug switches on whenever power returns, as many do out of the box.' },
          { say: 'It\'s 02:52 and your schedule says off, so I\'m switching it off.' },
          { tween: 'lamp.brightness', to: 0, ms: 1200, label: 'Lamp → off' }, { set: 'lamp.on', to: false },
          { say: 'The door camera came back on standby, not armed. Re-arming it.' },
          { set: 'camera.armed', to: true, label: 'Door camera armed' },
          sayWith((st) => `The thermostat kept its ${setPoint(st)}°C setting and is calling for heat again: the room fell to ${st.get('plan.warm') ? '19.8' : '17.9'}°C while the power was out. Blinds came back closed.`),
          replanWith((st) => ({ intro: 'After the power cut:', changes: ['Power out from about 02:10 to 02:52; I was off too, so those 40 minutes are a gap in the record', 'Floor lamp came back on by itself (its plug\'s power-on setting): switched off, as the schedule says', 'Door camera came back unarmed: re-armed', `Pump came back idle; thermostat kept ${setPoint(st)}°C; blinds and soil sensor came back as they were`], needsYou: 'Nothing urgent. When you\'re home, set the lamp\'s plug to stay off after a power cut. I\'ve sent you this summary, in case a neighbour saw the lamp come on at 02:52.' })),
          { status: 'Away plan running · power back since 02:52' },
          HOLD,
          { end: { headline: 'Back from the dark, nothing assumed.', body: 'After a 40-minute power cut, NeuCharBox checked every device instead of trusting the plan, switched off a lamp that came back on by itself, and re-armed the door camera. It also said plainly what it couldn\'t know about those 40 minutes.' } },
        ] },
      },
    },
    {
      chip: 'Just keep the plants alive while I\'m gone.',
      // Trip words at half weight (holidays, vacations, weekly, sundays match holiday, vacation, week, sunday as close
      // forms): "water my plants while I'm on holiday" outscores the whole-house prompt, "I'm on holiday" alone doesn't.
      keywords: ['plants', 'alive', 'water', 'watering', 'plant', 'soil', 'dry', 'away', 'holidays', 'vacations', 'travel', 'weekly', 'sundays'],
      // Not the lamp, camera or heating: "water the plants and turn on the lamp" gets the chips back. ('lamps' also
      // catches "lamp", 'lights' "light", but not "water them lightly".) No blinds at night either: they are open outside
      // 13:00–17:00, so "water the plants and close the blinds at night" (overnight, at dusk, when it gets dark…) gets
      // the chips back too, and so does "water the plants every evening" (it waters by the soil, not the clock). Those
      // are phrases (js/engine/match.js): "I'm away for a few nights", "over the holidays", "a dark corner" and "the
      // evenings are warm" say nothing against this plan.
      rulesOut: [...NOT_HERE, 'lamps', 'lights', 'lighting', 'cameras', 'cam', 'thermostat', 'heating', 'heater',
        ...['night', 'nights', 'overnight', 'evening', 'evenings', 'dusk', 'sunset', 'sundown', 'dark', 'bedtime'].flatMap((w) => [`blinds ${w}`, `${w} blinds`]),
        'every night', 'every evening', 'each night', 'each evening'],
      // The heating stays on, at its own setting: "…and keep the heating off" asks back (js/engine/match.js conflict()),
      // while "…and keep the lights off" runs, as the lamp starts off and this plan never switches it on.
      touches: ['Heating stays on at its current setting'],
      expect: { 'soil.status': 'online', 'pump.status': 'online', 'pump.running': false, 'blinds.closed': 0.5 },
      genericAt: 'shade', // a device this request doesn't use fails on Saturday, between the first watering and the first shade
      steps: [
        { beat: 'plan' },
        { status: 'Checking connected devices' },
        { say: 'Only the plant, then. I\'ll use the soil sensor, the pump and the blinds — the blinds because afternoon sun is what dries the pot out.' },
        { status: 'Waiting for your approval' },
        { plan: { intro: 'Plant-only plan:', steps: [
          { text: 'Water for 40 seconds whenever soil moisture drops below 30%, never more than twice a day', alt: { text: 'Water for 40 seconds every morning at 08:00, regardless of the sensor', apply: [{ set: 'plan.timed', to: true }] } },
          { text: 'Blinds half-closed between 13:00 and 17:00 to keep direct sun off the pot, open the rest of the day' }, // "keep the blinds closed" conflicts
          { text: 'Message you if the sensor reads below 20% or the pump fails' },
        ] } },
        { beat: 'run' },
        { say: 'Running. Fast-forwarding to Saturday.' },
        { status: (store) => (store.get('plan.timed') ? 'Saturday 08:00' : 'Saturday 11:20') },
        // Moisture rule: water when the soil reads under 30% (11:20). Timed schedule: water at 08:00 whatever it reads.
        { fn: ({ store, tween }) => { const timed = store.get('plan.timed'); return Promise.all([tween('env.hour', timed ? 32 : 35.33, 2500), tween('soil.moisture', timed ? 31 : 29, 2500)]); } },
        { failPoint: 'watering' },
        { fn: (c) => say(c, c.store.get('plan.timed') ? '08:00 — scheduled watering, 40 seconds. Soil reads 31%; you asked me not to wait for the sensor.' : '11:20 — soil at 29%, under the 30% line. Watering for 40 seconds.') },
        { set: 'pump.status', to: 'busy' }, { set: 'pump.running', to: true, label: 'Pump → running' },
        { tween: 'soil.moisture', to: 41, ms: 2500 },
        { set: 'pump.running', to: false }, { set: 'pump.status', to: 'online', label: 'Pump → idle' },
        { say: 'Done — 41%. The sensor confirmed the water arrived, so I know the pump actually pumped.' },
        { failPoint: 'shade' },
        { status: 'Saturday 13:00 · Blinds → 50%' }, // one line: a tween label would replace the time at once
        { parallel: [{ tween: 'env.hour', to: 37, ms: 2000 }, { tween: 'blinds.closed', to: 0.5, ms: 2000 }, { tween: 'soil.moisture', to: 39, ms: 2000 }] },
        { status: 'Sunday 07:10' },
        { parallel: [{ tween: 'env.hour', to: 55.17, ms: 2500 }, { tween: 'blinds.closed', to: 0, ms: 1500 }, { tween: 'soil.moisture', to: 33, ms: 2500 }] },
        { failPoint: 'sunday' },
        { fn: (c) => say(c, c.store.get('plan.timed') ? 'Sunday 07:10 — soil at 33%. Your 08:00 watering is next.' : 'Sunday 07:10 — soil at 33%, still above the 30% line, so no watering yet. The soil decides, not the clock.') },
        // Timed schedule (Edit): Sunday's 08:00 watering, whatever the sensor reads; the status follows the pump back to idle.
        { fn: async (c) => { if (!c.store.get('plan.timed')) return; c.status('Sunday 08:00 · Pump → running'); await c.tween('env.hour', 56, 1200); c.store.set('pump.status', 'busy'); c.store.set('pump.running', true); await c.tween('soil.moisture', 44, 2500); c.store.set('pump.running', false); c.store.set('pump.status', 'online'); c.status('Sunday 08:00 · Pump → idle'); await say(c, '08:00 — 40 seconds, as scheduled. 44% now: the sensor saw it land.'); } },
        { status: 'Next Friday 16:00' },
        { set: 'env.hour', to: 182.4 }, // Friday 14:24: daylight, like Sunday morning, so no night flash on the jump
        { parallel: [{ tween: 'env.hour', to: 184, ms: 2000 }, { tween: 'blinds.closed', to: 0.5, ms: 1500 }, { fn: ({ store, tween }) => tween('soil.moisture', store.get('plan.timed') ? 43 : 36, 2000) }] },
        { fn: (c) => say(c, c.store.get('plan.timed') ? 'Next Friday 16:00 — you\'re due back. The plant was watered at 08:00 every morning, as you set it, and each watering showed up on the sensor.' : 'Next Friday 16:00 — you\'re due back. The plant was watered whenever the soil dipped under 30%, at most twice a day, and each watering showed up on the sensor.') },
        { fn: (c) => say(c, `Soil at ${c.store.get('plan.timed') ? 43 : 36}%; blinds half-closed until 17:00, as planned.`) },
        { status: 'Plant plan finished · Friday 16:00' },
        { end: { headline: 'Watered by your rule, checked by the soil.', body: 'NeuCharBox watered the way you approved, kept the afternoon sun off the pot, and counted a watering only when the sensor saw the water arrive.' } },
      ],
      // What-ifs: the lamp, the door camera and the thermostat play the engine's generic one (this request uses none).
      whatIf: {
        // A kinked or clogged hose: the pump runs and says so, but no water reaches the pot. NCB believes the sensor over
        // the pump and stops it (p0's pump story goes silent; this one says it's working and isn't).
        pump: { at: 'watering', intro: 'Replaying this request. This time, the pump runs, but no water reaches the plant.', steps: [
          { status: (st) => `${st.get('plan.timed') ? 'Saturday 08:00' : 'Saturday 11:20'} · Pump → running · 40 s` },
          { set: 'pump.blocked', to: true }, // the motor turns, no drops at the hose mouth
          { set: 'pump.status', to: 'busy' }, { set: 'pump.running', to: true },
          { wait: 2500 },
          { fail: 'pump', faultText: 'no water reaching the soil', title: '⚠ Pump running, soil not changing', say: 'The pump says it\'s running, but 20 seconds in, the soil reading hasn\'t moved. A working watering shows on the sensor within about 10 seconds.' },
          { set: 'pump.running', to: false }, // stop is a command the pump still takes, and confirms
          { wait: 1500 },
          { say: 'I\'ve stopped the pump, and it confirms it\'s off. A blocked hose or a leak would both look like this, and neither is a reason to try again.' },
          sayWith((st) => (st.get('plan.timed') ? 'I\'ve messaged you, as the plan says. The soil stays at 31% for now, and tomorrow\'s 08:00 watering can\'t run either.' : 'I\'ve messaged you, as the plan says. The soil stays at 29% for now, and I can\'t water it.')),
          { ask: { intro: 'Without water, the pot will dry. I could keep the blinds half-closed all day, not only 13:00 to 17:00, to slow it down. The plant gets less light that way. Want that?', options: [
            { label: 'Keep the blinds as planned', primary: true, apply: [
              replanWith((st) => ({ intro: 'Watering stopped, blinds as planned:', changes: ['Pump stopped after 20 seconds: no water reached the soil. No retries', `Soil at ${st.get('plan.timed') ? 31 : 29}%; I'll message you if it reads under 20%`, 'Blinds half-closed 13:00–17:00, as planned'], needsYou: 'Check the hose from the pump to the pot for a kink or a clog, and look for water on the floor around the pump.' })),
              { status: 'Plant plan running · water pump flagged' },
            ] },
            { label: 'Half-close them all day', apply: [
              { tween: 'blinds.closed', to: 0.5, ms: 2000, label: 'Blinds → 50%' },
              replanWith((st) => ({ intro: 'Watering stopped, blinds half-closed all day:', changes: ['Pump stopped after 20 seconds: no water reached the soil. No retries', `Soil at ${st.get('plan.timed') ? 31 : 29}%; I'll message you if it reads under 20%`, 'Blinds half-closed all day to slow the drying; back to your 13:00–17:00 plan once watering works'], needsYou: 'Check the hose from the pump to the pot for a kink or a clog, and look for water on the floor around the pump.' })),
              { status: 'Plant plan running · water pump flagged' },
            ] },
          ] } },
          HOLD,
          { end: { headline: 'It stopped a pump that wasn\'t watering.', body: 'The pump said it was running, but the soil sensor never saw the water. NeuCharBox believed the sensor, stopped the pump, and told you where to look.' } },
        ] },
        // The battery runs flat mid-move, after one low-battery warning as the motor started: the slats stop even (nothing
        // is jammed) and the headrail LED goes dark. No retries: each one drains what's left.
        blinds: { at: 'shade', ask: 'What if the blinds fail?', intro: 'Replaying this request. This time, the blinds\' battery runs out as they close for the afternoon.', steps: [
          { status: 'Saturday 13:00 · Blinds → 50%' },
          { parallel: [{ tween: 'env.hour', to: 37, ms: 1500 }, { tween: 'blinds.closed', to: 0.2, ms: 1500 }, { tween: 'soil.moisture', to: 40, ms: 1500 }] },
          { set: 'blinds.flat', to: true }, // stopped where the battery gave out: slats even, not jammed
          { fail: 'blinds', faultText: 'low battery · last at 20%', title: '⚠ Blinds not answering: low battery', say: '13:00 — the blinds went quiet partway through closing. A low-battery warning came in as the motor started; their last report was 20% closed, and nothing since.' },
          { wait: 1500 },
          { say: 'I won\'t try them again: each attempt drains what\'s left, and a flat battery won\'t recover by itself. They\'ll stay where they stopped until someone charges them.' },
          sayWith((st) => (st.get('plan.timed') ? 'So the pot will likely get more afternoon sun than planned, and dry faster. Your 08:00 watering doesn\'t look at the soil, so on sunny days it may not keep up.' : 'So the pot will likely get more afternoon sun than planned, and dry faster. Your rule follows the soil, so it will water sooner, still never more than twice a day.')),
          { say: 'If the sensor reads under 20%, I\'ll message you, as the plan says.' },
          replanWith((st) => ({ intro: 'Re-planned around the blinds:', changes: ['Blinds: last reported 20% closed, after a low-battery warning. No retries', st.get('plan.timed') ? 'Watering stays at 08:00 every morning, as you set it' : 'Watering still follows the soil: under 30%, at most twice a day', 'The soil sensor is now my only view of the extra sun: under 20% and I message you'], needsYou: 'Charge or swap the blinds\' battery when you\'re home.' })),
          { status: 'Plant plan running · blinds flagged' },
          HOLD,
          { end: { headline: 'No retries on a dying battery.', body: 'The blinds\' battery died mid-move. NeuCharBox didn\'t drain it trying again, and told you what the extra sun meant for the watering you\'d approved.' } },
        ] },
        // The soil sensor goes quiet on Sunday morning (a battery or a crash, no visible cause): a fixed schedule, and no
        // more claims that the water landed. The room shows the soil taking the 08:00 water, which the sensor can't report.
        soil: { at: 'sunday', intro: 'Replaying this request. This time, the soil sensor goes quiet on Sunday morning.', steps: [
          { status: 'Sunday 07:10 · soil 33%' },
          { wait: 1200 },
          { fail: 'soil', faultText: 'no reading since 07:10', title: '⚠ Soil sensor quiet', say: 'The soil sensor stopped reporting at 07:10. Last reading: 33%.' },
          { wait: 1500 },
          { say: 'Without the sensor I can\'t see the soil, so I won\'t pretend to. Here\'s what that means:' },
          replanWith((store) => (store.get('plan.timed')
            ? { intro: 'Your 08:00 schedule carries on:', changes: ['The 40-second watering at 08:00 stays — it never depended on the sensor', 'What I lose is the check: I can\'t confirm the water lands, or warn you below 20%', 'Blinds stay half-closed in the afternoons', 'Sensor is polled every 10 minutes; I\'ll tell you the moment it\'s back'], needsYou: 'The sensor may need a new battery or a restart. Check it when you\'re home.' }
            : { intro: 'Switched to a conservative schedule:', changes: ['One small 30-second watering each day at 08:00, enough for most pots and not enough to drown one', 'Blinds stay half-closed in the afternoons', 'Sensor is polled every 10 minutes; the moment it\'s back I return to moisture-based watering'], needsYou: 'The sensor may need a new battery or a restart. Check it when you\'re home.' })),
          { status: 'Sunday 08:00' },
          { tween: 'env.hour', to: 56, ms: 1200 },
          { fn: (c) => say(c, c.store.get('plan.timed') ? '08:00 — your scheduled 40 seconds.' : '08:00 — the conservative schedule: 30 seconds, the small daily dose, not a response to a reading.') },
          { set: 'pump.status', to: 'busy' }, { set: 'pump.running', to: true, label: 'Pump → running' },
          { fn: ({ store, tween }) => tween('soil.moisture', store.get('plan.timed') ? 44 : 40, store.get('plan.timed') ? 2000 : 1500) },
          { set: 'pump.running', to: false }, { set: 'pump.status', to: 'online', label: 'Pump → idle' },
          { say: 'Done. With the sensor out I can\'t see whether it reached the soil, so I\'m not saying it did.' },
          { status: 'Plant plan running · soil sensor flagged' },
          HOLD,
          { end: { headline: 'It didn\'t guess.', body: 'When the sensor went quiet, NeuCharBox said so, watered on a fixed daily schedule, and stopped claiming the water had landed. Then it told you what to check.' } },
        ] },
      },
    },
    {
      chip: 'Make it look like someone\'s home in the evenings.',
      keywords: ['look', 'someone', 'home', 'evenings', 'evening', 'lived', 'occupied', 'burglar', 'security', 'lights'],
      rulesOut: [...NOT_HERE, 'mornings', 'afternoons', 'daytime', 'thermostat', 'heating', 'heater'], // the plan runs 18:20–23:20
      touches: ['Heating stays on at its current setting'], // "…and keep the heating off" asks back, as for the plant-only plan
      expect: { 'lamp.on': false, 'lamp.brightness': 0, 'blinds.closed': 1, 'blinds.status': 'online', 'camera.status': 'online' },
      genericAt: 'evening', // a device this request doesn't use fails mid-evening, with the lamp and blinds doing their job
      steps: [
        { beat: 'plan' },
        { status: 'Checking connected devices' },
        { say: 'Presence, then. Lamp, blinds and the door camera. The trick is variation — a light that switches at the same minute every day is easy to spot from the street.' },
        { status: 'Waiting for your approval' },
        { plan: { intro: 'Evening presence plan:', steps: [
          { text: 'Lamp on between 18:20 and 18:50, off between 22:40 and 23:20 — different minute each day' },
          { text: 'Blinds close 10 minutes after the lamp comes on, open at 08:00' },
          { text: 'Door camera armed; I message you on motion, and I don\'t record when there isn\'t any', alt: { text: 'Door camera off entirely', apply: [{ set: 'plan.noCam', to: true }] } },
        ] } },
        { beat: 'run' },
        { status: 'Friday 18:32' },
        { fn: ({ store }) => store.set(store.get('plan.noCam') ? 'camera.off' : 'camera.armed', true) }, // Edit: switched off, not left on standby
        { tween: 'env.hour', to: 18.53, ms: 1500 },
        { say: '18:32 — lamp on.' },
        { set: 'lamp.on', to: true },
        { tween: 'lamp.brightness', to: 0.75, ms: 1200, label: 'Lamp → 75%' },
        { tween: 'env.hour', to: 18.7, ms: 1000 },
        { say: '18:42 — closing the blinds.' },
        { failPoint: 'blinds' },
        { parallel: [{ tween: 'blinds.closed', to: 1, ms: 3000, label: 'Blinds → closing' }, { tween: 'env.hour', to: 18.8, ms: 3000 }] },
        { say: '18:48 — blinds closed. From the street: a lit room behind closed blinds, like any evening in.' },
        { status: 'Friday 21:30' },
        { tween: 'env.hour', to: 21.5, ms: 2000 },
        { failPoint: 'evening' },
        { fn: (c) => say(c, c.store.get('plan.noCam') ? '21:30 — lamp on, blinds closed. The door camera is off, as you chose.' : '21:30 — nothing at the door so far. The camera is armed and has seen no motion, so it has recorded nothing.') },
        { status: 'Friday 23:05' },
        { tween: 'env.hour', to: 23.08, ms: 2000 },
        { failPoint: 'lampOff' },
        { say: '23:05 — lamp off. Tomorrow it\'ll be 22:48. And no, I won\'t tell anyone the pattern.' },
        { tween: 'lamp.brightness', to: 0, ms: 1400, label: 'Lamp → off' },
        { set: 'lamp.on', to: false },
        { status: 'Friday 23:05 · blinds closed until 08:00' },
        { end: { headline: 'From the street, someone\'s home.', body: 'The lamp came on at 18:32 and went off at 23:05, at minutes picked fresh each day, with the blinds closed behind it. From the street it looked like an evening in.' } },
      ],
      // What-ifs: the soil sensor, the water pump and the thermostat play the engine's generic one (not in this request).
      whatIf: {
        // The blinds stall at 40% while closing. NCB stops driving the motor and keeps the lamp at 75%: behind a
        // half-open blind at night, a brighter lamp would only show passers-by more of an empty room.
        blinds: { at: 'blinds', ask: 'What if the blinds fail?', intro: 'Replaying this request. This time, the blinds stall as they close for the evening.', steps: [
          { status: 'Friday 18:42 · Blinds → closing' },
          { parallel: [{ tween: 'blinds.closed', to: 0.4, ms: 1600 }, { tween: 'env.hour', to: 18.75, ms: 1600 }] },
          { set: 'blinds.jammed', to: true }, // caught in the track: the stall look
          { fail: 'blinds', faultText: 'stalled at 40% closed', title: '⚠ Blinds stalled', say: 'The blinds stopped at 40% closed, and the motor reports a stall.' },
          { wait: 1500 },
          { say: 'I\'ve stopped driving the motor — forcing stalled blinds is how you break them. Re-planned:' },
          replanWith((store) => ({ intro: 'Presence without the blinds:', changes: ['Lamp schedule continues at 75%: brighter would only show more of the room through the gap', store.get('plan.noCam') ? 'Door camera stays off, as you chose' : 'Door camera stays armed', 'Blinds left at 40% closed; one gentle retry at 08:00, then I leave them alone'], needsYou: 'Something is probably caught in the blinds\' track. Check it when you\'re back.' })),
          { wait: 800 },
          { status: 'Friday 23:05' },
          { tween: 'env.hour', to: 23.08, ms: 2000 },
          { say: '23:05 — lamp off. Tomorrow it\'ll be 22:48. And no, I won\'t tell anyone the pattern.' },
          { tween: 'lamp.brightness', to: 0, ms: 1400, label: 'Lamp → off' },
          { set: 'lamp.on', to: false },
          { status: 'Evening plan done · blinds flagged' },
          HOLD,
          { end: { headline: 'It stopped instead of forcing it.', body: 'A stalled motor is a device telling you something. NeuCharBox listened, kept the rest of the plan working, and told you what to look at.' } },
        ] },
        // The camera falls off the network at 21:30. Armed (default), it is the only thing that tells you about someone at
        // the door; switched off (Edit), it still checks in every few minutes (LED dark), which is how NCB notices it went.
        camera: { at: 'evening', intro: 'Replaying this request. This time, the door camera stops checking in during the evening.', steps: [
          { status: 'Friday 21:30' },
          { wait: 1200 },
          { fail: 'camera', faultText: 'no check-in since 21:30' }, // the alert follows from an fn: its words depend on the Edit
          alertWith((st) => (st.get('plan.noCam') ? '21:30 — the door camera stopped its check-ins. Its camera was off, as you chose, but the unit itself had kept checking in every few minutes until now.' : '21:30 — the door camera stopped answering its check-ins.'), '⚠ Door camera not answering'),
          { wait: 1500 },
          sayWith((st) => (st.get('plan.noCam') ? 'Nothing in tonight\'s plan changes. I\'m telling you because it\'s on your front door, and you may want it working when you\'re back.' : 'So right now nothing is watching the door, and you\'d hear nothing about a visitor. The lamp and blinds don\'t depend on it, so the evening goes on as planned.')),
          replanWith((st) => ({ intro: 'Evening plan, without the door camera:', changes: st.get('plan.noCam')
            ? ['Door camera: off, as you chose, and now not answering at all', 'When it\'s back I\'ll leave it off; that was your choice', 'Lamp off at a different minute each night; blinds open at 08:00']
            : ['Door camera: not answering since 21:30', 'I won\'t say "all quiet at the door" while it\'s down', 'Lamp off at a different minute each night; blinds open at 08:00', 'I\'ll check for it every 10 minutes'],
          needsYou: st.get('plan.noCam') ? 'Nothing urgent. Check the camera\'s power when you\'re home.' : 'It may only need restarting: switch it off and on at its power when you\'re back.' })),
          { status: 'Friday 23:05' },
          { tween: 'env.hour', to: 23.08, ms: 2000 },
          { say: '23:05 — lamp off, as planned. The blinds stay closed until 08:00.' },
          { tween: 'lamp.brightness', to: 0, ms: 1400, label: 'Lamp → off' },
          { set: 'lamp.on', to: false },
          { status: 'Evening plan done · door camera flagged' },
          HOLD,
          { end: { headline: 'The camera went quiet. You heard at once.', body: 'When the door camera stopped checking in, NeuCharBox told you straight away and kept the lamp and blinds running. It never reported a quiet door it couldn\'t see.' } },
        ] },
        // The lamp's plug drops off the network while on, so it stays lit. Nothing in the room measures light, so NCB goes
        // by the last report (on, 75%) and assumes it's still lit (p0's lamp story is the opposite: dark when it should be lit).
        lamp: { at: 'lampOff', intro: 'Replaying this request. This time, the floor lamp won\'t switch off at night.', steps: [
          { status: 'Friday 23:05 · Lamp → off' },
          { wait: 1200 },
          { fail: 'lamp', faultText: 'no reply · last seen on', title: '⚠ Lamp won\'t switch off', say: '23:05 — the floor lamp didn\'t answer "off", and its plug has gone quiet. Its last report was on at 75%, so I have to assume it\'s still lit.' },
          { wait: 1500 },
          { say: 'Only a hand at the wall socket can switch it off now. A lamp left on isn\'t usually a danger, but it breaks the pattern: people go to bed.' },
          { say: 'The blinds stay closed, so if it\'s lit, from outside it\'s a glow, not a view in. I\'ll keep trying the lamp every 10 minutes.' },
          replanWith((st) => ({ intro: 'Presence, with the lamp stuck on:', changes: ['Floor lamp: last reported on at 75%; no reply since 23:05', 'Blinds stay closed until 08:00, as planned', st.get('plan.noCam') ? 'Door camera stays off, as you chose' : 'Door camera stays armed', 'I\'ll try the lamp every 10 minutes and put it back on your schedule when it answers'], needsYou: 'If someone has a key, switching the lamp off at the wall socket is all it needs. Otherwise it stays on until it answers or you\'re home.' })),
          { status: 'Saturday 02:30' },
          { tween: 'env.hour', to: 26.5, ms: 2000 },
          { say: '02:30 — still no reply from the lamp. As far as I know, it\'s still on.' },
          { status: 'Evening plan running · floor lamp flagged' },
          HOLD,
          { end: { headline: 'It never said the lamp was off.', body: 'The lamp stopped answering at 23:05 and stayed lit. NeuCharBox went by its last report, kept trying, kept the blinds closed, and told you a hand at the socket was all it needed.' } },
        ] },
      },
    },
    {
      chip: 'Hold the house at 19°C and tell me if anyone comes to the door.',
      keywords: ['hold', 'house', '19', 'degrees', 'temperature', 'heating', 'thermostat', 'door', 'anyone', 'comes', 'visitor', 'delivery', 'parcel', 'freeze', 'pipes', 'someone', 'doorbell', 'warm', 'camera', 'arm', 'courier'],
      // Only the thermostat and the door camera. 'close' too: the matcher reads "…and close the blinds" like "leave the
      // lamp off", as something the text wants left out, which a plan that never touches them would agree with.
      rulesOut: [...NOT_HERE, 'lamps', 'lights', 'lighting', 'blinds', 'curtains', 'shades', 'close'],
      expect: { 'thermostat.status': 'online', 'thermostat.target': 19, 'thermostat.current': 19.1, 'camera.armed': true, 'camera.motion': false, 'camera.status': 'online' },
      // A device this request doesn't use fails at Saturday 14:02, just before the visit. The generic what-if then runs the
      // rest of the request quietly, straight to its end (Sunday 09:00): daylight to daylight, where from 03:15 the room
      // flashed from night to day.
      genericAt: 'visit',
      steps: [
        { beat: 'plan' },
        { status: 'Checking connected devices' },
        { say: 'Two jobs: heating and the front door. I\'ll use the thermostat and the door camera — nothing else needs to be involved.' },
        { status: 'Waiting for your approval' },
        { plan: { intro: 'Plan:', steps: [
          { text: 'Thermostat set to 19°C and held there; I message you if the room drifts more than 2° from it' },
          { text: 'Door camera armed. On motion I send you a still photo and a one-line note — no continuous recording', alt: { text: 'Door camera armed. On motion I send you a one-line note only, no image — no continuous recording', apply: [{ set: 'plan.noImage', to: true }] } },
        ] } },
        { beat: 'run' },
        { status: 'Friday 17:00 · Thermostat → 19°C' }, // one line: a tween label would replace the time at once
        { parallel: [{ tween: 'env.hour', to: 17, ms: 1200 }, { tween: 'thermostat.target', to: 19, ms: 1200 }, { tween: 'thermostat.current', to: 19.2, ms: 3000 }] },
        { set: 'camera.armed', to: true, label: 'Door camera armed' },
        { wait: 900 }, // long enough to read, before the status jumps to Saturday
        { status: 'Saturday 14:02' },
        { tween: 'env.hour', to: 38.03, ms: 2500 },
        { failPoint: 'visit' },
        { set: 'camera.motion', to: true, label: 'Motion at the door' },
        { fn: (c) => say(c, c.store.get('plan.noImage') ? 'Saturday 14:02 — motion at the door for 40 seconds. A one-line note went to your phone. No image, as you chose, so I can\'t tell you who it was.' : 'Saturday 14:02 — motion at the door for 40 seconds. A still photo and a one-line note went to your phone, as planned. No video was recorded.') },
        { wait: 1200 },
        { set: 'camera.motion', to: false },
        { status: 'Sunday 03:15' },
        { parallel: [{ tween: 'env.hour', to: 51.25, ms: 2500 }, { tween: 'thermostat.current', to: 18.9, ms: 2500 }] },
        { failPoint: 'overnight' },
        { say: 'Sunday 03:15 — the coldest hour so far: 18.9°C, well inside the 2° you allowed. The heating is keeping up.' },
        { status: 'Sunday 09:00' },
        { parallel: [{ tween: 'env.hour', to: 57, ms: 2000 }, { tween: 'thermostat.current', to: 19.1, ms: 2000 }] },
        { fn: (c) => say(c, c.store.get('plan.noImage') ? '09:00 — 19.1°C. One visit so far, Saturday at 14:02, and you have the note.' : '09:00 — 19.1°C. One visit so far, Saturday at 14:02, and you have the photo and the note.') },
        { status: 'Holding 19°C · door camera armed' },
        { end: { headline: 'Warm house, watched door, no surprises.', body: 'NeuCharBox held 19°C and checked the room\'s own temperature, not just the set point. When someone came to the door, you heard about it the way you chose.' } },
      ],
      // What-ifs: the lamp, the blinds, the soil sensor and the water pump play the engine's generic one (not in this
      // request; the heating special's ask may name the blinds, A6).
      whatIf: {
        // Silent from 03:15. The story keeps the promise its own re-plan makes: still silent at 07:00, NCB messages again.
        thermostat: { at: 'overnight', intro: 'Replaying this request. This time, the thermostat stops responding in the middle of the night.', steps: [
          { status: 'Sunday 03:15 · 18.9°C' },
          { wait: 1200 },
          { fail: 'thermostat', faultText: 'no reply since 03:15', title: '⚠ Thermostat not answering', say: 'Sunday 03:15 — the thermostat stopped responding. Last reading 18.9°C.' },
          { wait: 1500 },
          { say: 'I can\'t see or set the temperature right now, and I\'m not going to assume it\'s fine. Here\'s what I\'m doing:' },
          { replan: { intro: 'Heating is unverified:', changes: ['Camera and door alerts continue — they don\'t depend on the thermostat', 'I\'ll keep trying the thermostat every 15 minutes', 'If it\'s still silent by 07:00 I\'ll message you again so you can decide whether to call someone'], needsYou: 'The thermostat may have dropped off the network. Power-cycle it when you\'re home, or ask a neighbour if it\'s cold.' } },
          { status: 'Sunday 07:00' },
          { tween: 'env.hour', to: 55, ms: 2500 },
          { say: '07:00 — still nothing from the thermostat, so I\'ve messaged you again, as I said I would.' },
          { say: 'Its last reading was 18.9°C, almost four hours ago. I don\'t know the temperature now, and I won\'t guess it.' },
          { status: 'Heating unverified · thermostat flagged' },
          HOLD,
          { end: { headline: 'It told you what it couldn\'t verify.', body: 'A quiet device isn\'t a working device. NeuCharBox separates "I did it" from "I checked it" — and says which one it is.' } },
        ] },
        // Motion, then silence: the camera drops off mid-upload. NCB passes on only what it knows.
        camera: { at: 'visit', intro: 'Replaying this request. This time, the door camera drops off in the middle of a visit.', steps: [
          { status: 'Saturday 14:02' },
          { wait: 900 },
          { set: 'camera.motion', to: true, label: 'Motion at the door' },
          { wait: 1500 },
          { set: 'camera.motion', to: false }, // a camera that stopped reporting reports no motion either; before the fail, so the room's motion unping can't drop the fault label
          { fail: 'camera', faultText: 'silent since 14:02', title: '⚠ Door camera went silent', say: 'Saturday 14:02 — the door camera reported motion at the door, then went silent a few seconds later.' },
          { wait: 1500 },
          sayWith((st) => (st.get('plan.noImage') ? 'Your one-line note went out at 14:02, as planned. That\'s everything I know: after it, the camera went silent, so I can\'t say whether anyone is still there.' : 'The photo never came through, so you\'ve had the one-line note and a second line saying there\'s no picture. I won\'t guess who it was, or whether they\'re still there.')),
          replanWith((st) => ({ intro: 'Door alerts are down:', changes: ['Door camera: silent since 14:02, seconds after reporting motion', st.get('plan.noImage') ? 'Your note went out; nothing came after it' : 'Note sent; the photo never arrived', 'Thermostat unaffected: holding 19°C, 19.2°C in the room', 'Until it\'s back, a quiet door tells me nothing'], needsYou: 'If you\'re expecting a delivery, ask someone to check the doorstep. The camera may need a restart when you\'re home.' })),
          { status: 'Holding 19°C · door camera flagged' },
          HOLD,
          { end: { headline: 'Half a visit reported, nothing invented.', body: 'The door camera caught motion, then went silent. NeuCharBox passed on what it knew, said what it didn\'t, and kept the heating on track.' } },
        ] },
        // Special: every device answers, but the room keeps cooling (a boiler that stopped or lost pressure, or a failed
        // link from the thermostat to it: none of it connected). NCB goes by the temperature, messages at the 2° line
        // the plan set, and asks before touching the blinds, which this request left out.
        heatingLags: { label: 'The heating can\'t keep up', ask: 'What if the heating can\'t keep up?', at: 'overnight', intro: 'Replaying this request. This time, the thermostat keeps answering, but the room keeps getting colder.', steps: [
          { status: 'Sunday 03:15 · 18.9°C · thermostat calling for heat' },
          { wait: 1200 },
          // Before dawn, so the offer below (blinds closed to keep the heat in) is made in a dark room, not in the sunrise.
          { status: 'Sunday 04:00' },
          { parallel: [{ tween: 'env.hour', to: 52, ms: 2500 }, { tween: 'thermostat.current', to: 17.8, ms: 2500 }] },
          { say: '04:00 — 17.8°C and still falling. The thermostat is answering and asking for heat at 19°C, but the room has lost 1.1° in 45 minutes.' },
          { status: 'Sunday 04:30 · 16.9°C' },
          { parallel: [{ tween: 'env.hour', to: 52.5, ms: 1500 }, { tween: 'thermostat.current', to: 16.9, ms: 1500 }] },
          { set: 'thermostat.cold', to: true }, // the room marks the 2° line: an amber ping and glow at the thermostat
          alertWith(() => '04:30 — 16.9°C: more than 2° under the 19°C you asked me to hold. I\'ve messaged you, as the plan says.', '⚠ Room 2.1° below target'),
          { wait: 900 },
          { say: 'The thermostat is asking for heat, but the heat isn\'t arriving. The boiler may have stopped or lost pressure, or the thermostat\'s link to it may have failed.' },
          { say: 'None of that is connected to me, so I can\'t see which. Turning the set point up wouldn\'t help.' },
          { ask: { intro: 'One thing I could do: the blinds are open, and the room keeps more of its heat with them closed. Close them until 08:00? They\'re not part of this request, so it\'s your call.', options: [
            { label: 'Leave the blinds alone', primary: true, apply: [
              { replan: { intro: 'Heating not keeping up:', changes: ['Room 16.9°C and falling, set point 19°C; the thermostat answers and asks for heat', 'Messaged you at the 2° line, as planned; I\'ll message again if it drops below 15°C', 'Blinds left as they are; the door camera stays armed'], needsYou: 'Someone should check the boiler and the thermostat\'s link to it: the boiler may have stopped or lost pressure. A house left cold for days can put the pipes at risk.' } },
              { status: 'Set to 19°C · room 16.9°C · heating flagged' },
            ] },
            { label: 'Close them until 08:00', apply: [
              { tween: 'blinds.closed', to: 1, ms: 2000, label: 'Blinds → closed until 08:00' },
              { replan: { intro: 'Heating not keeping up:', changes: ['Room 16.9°C and falling, set point 19°C; the thermostat answers and asks for heat', 'Messaged you at the 2° line, as planned; I\'ll message again if it drops below 15°C', 'Blinds closed until 08:00 to keep in what heat there is, as you asked; the door camera stays armed'], needsYou: 'Someone should check the boiler and the thermostat\'s link to it: the boiler may have stopped or lost pressure. A house left cold for days can put the pipes at risk.' } },
              { status: 'Set to 19°C · room 16.9°C · heating flagged' },
            ] },
          ] } },
          HOLD,
          { end: { headline: 'Every device answered. The room still cooled.', body: 'The thermostat kept calling for heat, but the room kept cooling. NeuCharBox went by the temperature, not the call for heat, messaged you at the 2° line you set, and asked before touching anything else.' } },
        ] },
      },
    },
  ],
};
