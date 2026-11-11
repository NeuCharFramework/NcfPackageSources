// The Warehouse types' card UI and 3D models for the custom room (contract: devices/index.js). Their look follows the
// warehouse scene (js/scenes/warehouse.js): a belt on a steel frame, a scanner arch, a sorter gate that swings to lane B,
// a small cart that follows a taped loop, and a roller door at a loading dock. Each is a compact, self-contained model
// with its own status LED, since here nothing else (a hub desk, the other belts) is there to carry it.
import { HEX, num, clamp01, statusLed, mergeMeshes, canvasTex, ease, stepper } from './kit.js';

const TAU = Math.PI * 2;
const SPEED_K = 2.2; // the warehouse scene runs its belts 2.2× faster than labelled; so does this belt on screen

// A dark belt with a lighter cross stripe every 10 cm (u runs along the belt): offset.x scrolls it.
const beltTex = (THREE, length) => {
  const t = canvasTex(THREE, (g, w, h) => { g.fillStyle = '#23272B'; g.fillRect(0, 0, w, h); g.fillStyle = '#3C434A'; g.fillRect(0, 0, 9, h); g.fillStyle = '#1B1E21'; g.fillRect(9, 0, 3, h); });
  t.repeat.set(length / 0.1, 1);
  return t;
};

// A glowing lamp mesh with its own material.
const lamp = (THREE, r, h) => new THREE.Mesh(new THREE.CylinderGeometry(r, r, h, 16), new THREE.MeshStandardMaterial({ color: 0x222222, emissive: 0x000000, emissiveIntensity: 0, roughness: 0.4 }));

const CART_SPOT = { dock: Math.PI, aisle: -Math.PI / 2, charger: 0 }; // where each stop sits on the loop (x = r cos, z = r sin)
const wrapPi = (a) => { a = ((a + Math.PI) % TAU + TAU) % TAU - Math.PI; return a; };

export default {
  // A 1.5 m belt section on a steel frame. The belt's cross stripes scroll with the speed, and two parcels ride it while
  // it runs. The beacon on the end post is blue while the belt moves.
  conveyor: {
    icon: 'conveyor', size: 1.6,
    edit: [{ field: 'speed', kind: 'slider', step: 0.05, unit: ' m/s', label: 'speed' }],
    format: (s) => (num(s.speed) > 0.05 ? `running · ${num(s.speed).toFixed(1)} m/s` : 'stopped'),
    active: (s) => num(s.speed) > 0.05,
    control: { kind: 'bar', field: 'speed', min: 0, max: 0.5 },
    model({ THREE, P, M }) {
      const L = 1.5, W = 0.38, TOP = 0.7;
      const legs = [[-1, -1], [1, -1], [-1, 1], [1, 1]].map(([sx, sz]) => P.box(0.04, TOP - 0.06, 0.04, M.steel, sx * 0.68, (TOP - 0.06) / 2, sz * 0.17));
      const rails = [-1, 1].map((sz) => P.box(1.56, 0.1, 0.03, M.steel, 0, TOP - 0.04, sz * 0.2));
      const rollers = [-1, 1].map((sx) => { const r = P.cyl(0.04, 0.04, W, M.anodized, sx * 0.745, TOP - 0.035, 0, 14); r.rotation.x = Math.PI / 2; return r; });
      const tex = beltTex(THREE, L);
      const beltMat = new THREE.MeshStandardMaterial({ map: tex, roughness: 0.9 });
      const belt = P.box(L, 0.03, W, beltMat, 0, TOP - 0.015, 0);
      const motor = P.box(0.12, 0.11, 0.09, M.anodized, -0.66, TOP - 0.1, 0.275, 0.01);
      const post = P.cyl(0.012, 0.012, 0.16, M.steel, 0.74, TOP + 0.08 - 0.04, 0.215, 8);
      const beacon = lamp(THREE, 0.026, 0.045); beacon.position.set(0.74, TOP + 0.145, 0.215);
      const parcels = [0, 1].map((i) => P.box(0.18, 0.11, 0.14 + 0.03 * i, M.cardboard, 0, TOP + 0.055, (i ? 0.03 : -0.04), 0.006));
      const root = P.group(...legs, ...rails, ...rollers, belt, motor, post, beacon, ...parcels);
      const led = statusLed(THREE, root, { at: [-0.66, TOP - 0.04, 0.322], r: 0.007 });
      const dt = stepper(); let phase = 0, scroll = 0, shown = -1;
      return {
        root,
        update(s, t) {
          const d = dt(t), sp = clamp01(num(s.speed) / 0.5) * 0.5, v = sp * SPEED_K, moving = sp > 0.05;
          scroll = (scroll - (v * d) / 0.1) % 1; tex.offset.x = scroll; // the pattern runs toward +x
          phase = (phase + v * d) % 1.3;
          parcels.forEach((p, i) => { p.visible = sp > 0.01; p.position.x = -0.65 + ((phase + i * 0.65) % 1.3); });
          if (moving !== shown) { shown = moving; beacon.material.emissive.setHex(moving ? 0x3AB7FF : 0x000000); beacon.material.emissiveIntensity = moving ? 2.4 : 0; }
          led.set(s, t, moving ? HEX.blue : HEX.green, 2);
        },
      };
    },
  },

  // A portal over a lane: two posts and a head with a scan strip. While armed a red scan line sweeps up and down between
  // the posts and the beacon on top glows amber.
  scanner_arch: {
    icon: 'scanner', size: 1.2,
    edit: [{ field: 'armed', kind: 'switch' }, { field: 'scanned', kind: 'slider', step: 1, label: 'scanned' }],
    format: (s) => `${s.armed ? 'armed' : 'idle'} · ${Math.round(num(s.scanned))} scanned`,
    active: (s) => !!s.armed,
    control: { kind: 'switch', field: 'armed' },
    model({ THREE, P, M }) {
      const feet = [-0.5, 0.5].map((x) => P.box(0.16, 0.03, 0.3, M.anodized, x, 0.015, 0));
      const posts = [-0.5, 0.5].map((x) => P.box(0.08, 1.85, 0.1, M.steel, x, 0.955, 0, 0.008));
      const head = P.box(1.14, 0.2, 0.16, M.anodized, 0, 1.95, 0, 0.012);
      const strip = P.box(0.9, 0.025, 0.02, M.screen, 0, 1.84, 0.07);
      const lineMat = new THREE.MeshBasicMaterial({ color: 0xFF3A3A, transparent: true, opacity: 0.9, depthWrite: false });
      const line = new THREE.Mesh(new THREE.BoxGeometry(0.92, 0.012, 0.012), lineMat); line.position.y = 0.95; line.userData.noAO = true; line.raycast = () => {}; line.visible = false;
      const beacon = lamp(THREE, 0.03, 0.05); beacon.position.set(0, 2.075, 0);
      const root = P.group(...feet, ...posts, head, strip);
      root.add(line, beacon);
      const led = statusLed(THREE, root, { at: [-0.5, 1.5, 0.056], r: 0.008 });
      let shown = null;
      return {
        root,
        update(s, t) {
          const armed = !!s.armed;
          line.visible = armed; if (armed) line.position.y = 0.95 + 0.85 * Math.sin(t * 2.2);
          if (armed !== shown) { shown = armed; beacon.material.emissive.setHex(armed ? 0xFFB020 : 0x000000); beacon.material.emissiveIntensity = armed ? 2.6 : 0; }
          led.set(s, t, armed ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },

  // A short belt with a diverter arm at its end and a side belt leaving to lane B. Lane A leaves the arm along the edge;
  // lane B swings it across the belt.
  sorter_gate: {
    icon: 'gate', size: 0.8,
    format: (s) => `→ lane ${s.lane === 'B' ? 'B' : 'A'}`,
    active: (s) => s.lane === 'B',
    control: false,
    model({ THREE, P, M }) {
      const TOP = 0.4;
      const main = P.box(0.55, 0.03, 0.28, M.rubber, -0.125, TOP - 0.015, 0.1);
      const sideA = [-0.04, 0.24].map((z) => P.box(0.55, 0.07, 0.02, M.steel, -0.125, TOP - 0.03, z));
      const branch = P.box(0.26, 0.03, 0.22, M.rubber, 0, 0, 0); branch.position.set(0.22, TOP - 0.015, -0.1); branch.rotation.y = 0.55;
      const legs = [[-0.37, -0.01], [-0.37, 0.21], [0.12, 0.21], [0.2, -0.17]].map(([x, z]) => P.box(0.03, TOP - 0.03, 0.03, M.steel, x, (TOP - 0.03) / 2, z));
      const blade = P.box(0.3, 0.07, 0.015, M.yellow, 0.15, 0, 0, 0.003);
      const hinge = P.cyl(0.02, 0.02, 0.09, M.anodized, 0, 0, 0, 12);
      const arm = P.group(blade, hinge); arm.position.set(-0.02, TOP + 0.04, 0.22);
      const ctrl = P.box(0.06, 0.05, 0.04, M.black, -0.36, TOP + 0.02, 0.27, 0.004);
      const root = P.group(main, ...sideA, branch, ...legs, ctrl);
      root.add(arm);
      const led = statusLed(THREE, root, { at: [-0.36, TOP + 0.0455, 0.27], r: 0.006 });
      const dt = stepper(); let ang = 0;
      return {
        root,
        update(s, t) {
          ang = ease(ang, s.lane === 'B' ? 0.62 : 0, dt(t), 6);
          arm.rotation.y = ang; // positive turns +x toward -z: across the belt, toward the lane B belt
          led.set(s, t, s.lane === 'B' ? HEX.blue : HEX.green, 2);
        },
      };
    },
  },

  // A small driverless cart on a taped loop with three stops: the dock (orange), the aisle (grey) and the charger
  // (green). It circles the loop while `driving` is true and settles on its stop when it stops.
  cart: {
    icon: 'cart', size: 1.0,
    format: (s) => (s.driving ? 'driving' : `at the ${String(s.at || 'dock')}`),
    active: (s) => !!s.driving,
    control: false,
    model({ THREE, P, M }) {
      const R = 0.3;
      const tape = new THREE.Mesh(new THREE.RingGeometry(R - 0.012, R + 0.012, 64), new THREE.MeshBasicMaterial({ color: 0xC9A227, side: THREE.DoubleSide }));
      tape.rotation.x = -Math.PI / 2; tape.position.y = 0.003; tape.userData.noAO = true; tape.raycast = () => {};
      const padMat = (hex) => new THREE.MeshStandardMaterial({ color: hex, roughness: 0.8 });
      const chargeMat = new THREE.MeshStandardMaterial({ color: 0x1F6B45, emissive: 0x2FBF71, emissiveIntensity: 0, roughness: 0.6 });
      const pads = Object.entries(CART_SPOT).map(([k, a]) => {
        const p = P.box(0.17, 0.006, 0.17, k === 'dock' ? padMat(0xE08A2E) : k === 'aisle' ? padMat(0x8E9499) : chargeMat, R * Math.cos(a), 0.003, R * Math.sin(a), 0.002);
        p.castShadow = false; return p;
      });
      const body = P.box(0.34, 0.09, 0.22, M.yellow, 0, 0.075, 0, 0.012);
      const deck = P.box(0.3, 0.014, 0.19, M.steel, 0, 0.127, 0, 0.003);
      const bumper = P.box(0.012, 0.04, 0.2, M.black, 0.175, 0.06, 0);
      const mast = P.cyl(0.014, 0.014, 0.06, M.black, 0.14, 0.16, 0, 8);
      const wheels = [[-0.11, -0.115], [0.11, -0.115], [-0.11, 0.115], [0.11, 0.115]].map(([x, z]) => { const w = P.cyl(0.032, 0.032, 0.02, M.rubber, x, 0.032, z, 14); w.rotation.x = Math.PI / 2; return w; });
      const cart = P.group(body, deck, bumper, mast, ...wheels);
      const root = P.group(tape, ...pads);
      root.add(cart);
      const led = statusLed(THREE, cart, { at: [-0.13, 0.1375, 0], r: 0.007 });
      const dt = stepper(); let ang = CART_SPOT.dock, shown = null;
      return {
        root,
        update(s, t) {
          const d = dt(t), goal = CART_SPOT[s.at] ?? CART_SPOT.dock;
          if (s.driving) ang = wrapPi(ang + 1.25 * d);
          else { const gap = wrapPi(goal - ang); ang = Math.abs(gap) < 0.01 ? goal : wrapPi(ang + gap * Math.min(1, d * 5)); }
          cart.position.set(R * Math.cos(ang), 0, R * Math.sin(ang)); cart.rotation.y = -ang - Math.PI / 2; // heading along the loop
          const charging = !s.driving && s.at === 'charger';
          if (charging !== shown) { shown = charging; chargeMat.emissiveIntensity = charging ? 1.6 : 0; }
          led.set(s, t, s.driving ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },

  // A roller door on the wall at a loading dock, with its frame, bumpers and a signal lamp (green open, red closed).
  // The door rolls up into its housing over 3 s; parcels pile up in the doorway as they arrive.
  loading_dock: {
    icon: 'dock', size: 1.6, mountY: 1.05,
    edit: [{ field: 'open', kind: 'switch' }, { field: 'parcels', kind: 'slider', step: 1, label: 'parcels' }],
    format: (s) => { const n = Math.round(num(s.parcels)); return `${s.open ? 'open' : 'closed'} · ${n} ${n === 1 ? 'parcel' : 'parcels'}`; },
    active: (s) => !!s.open,
    control: { kind: 'switch', field: 'open' },
    model({ THREE, P, M }) {
      const H = 1.9, TOPY = 0.85; // the opening runs from the floor (y -1.05) to TOPY
      const jambs = [-0.73, 0.73].map((x) => P.box(0.1, 2.0, 0.08, M.steel, x, -0.05, 0.04));
      const header = P.box(1.56, 0.12, 0.12, M.steel, 0, 1.01, 0.06, 0.004);
      const housing = P.box(1.4, 0.1, 0.1, M.anodized, 0, 0.9, 0.05, 0.006);
      const dark = new THREE.MeshStandardMaterial({ color: 0x15181B, roughness: 1 });
      const recess = P.box(1.36, H, 0.01, dark, 0, -0.1, 0.005);
      const slatMat = new THREE.MeshStandardMaterial({ color: 0xC9CED2, roughness: 0.55, metalness: 0.4 });
      const ribs = [];
      for (let i = 0; i < 19; i++) ribs.push(P.box(1.34, 0.092, 0.03, slatMat, 0, -(i + 0.5) * 0.1, 0, 0.004));
      const door = mergeMeshes(THREE, ribs, slatMat, { consume: true });
      const doorPivot = P.group(door); doorPivot.position.set(0, TOPY, 0.04);
      const bumpers = [-0.55, 0.55].map((x) => P.box(0.16, 0.2, 0.1, M.black, x, -0.95, 0.05));
      const plate = P.box(1.3, 0.02, 0.4, M.steel, 0, -1.04, 0.2); plate.castShadow = false;
      const edge = P.box(1.3, 0.005, 0.05, M.yellow, 0, -1.028, 0.375); edge.castShadow = false;
      const lampMesh = lamp(THREE, 0.04, 0.06); lampMesh.position.set(0.62, 1.1, 0.07);
      const lampMat = lampMesh.material;
      // [x, height above the plate, z, w, h]: three on the plate, two on those, one on top
      const boxes = [[-0.4, 0, 0.27, 0.26, 0.3], [0, 0, 0.27, 0.32, 0.26], [0.42, 0, 0.27, 0.24, 0.3], [-0.2, 0.3, 0.27, 0.26, 0.22], [0.2, 0.26, 0.27, 0.26, 0.2], [0.02, 0.5, 0.27, 0.22, 0.2]]
        .map(([x, base, z, w, h]) => P.box(w, h, 0.26, M.cardboard, x, -1.03 + base + h / 2, z, 0.008));
      const root = P.group(...jambs, header, housing, recess, doorPivot, ...bumpers, plate, edge, ...boxes);
      root.add(lampMesh);
      const led = statusLed(THREE, root, { at: [-0.73, 0.78, 0.082], r: 0.008 });
      const dt = stepper(); let up = 0, shown = null, shownN = -1;
      return {
        root,
        update(s, t) {
          const open = !!s.open, d = dt(t);
          up = Math.abs((open ? 1 : 0) - up) < 0.001 ? (open ? 1 : 0) : up + Math.sign((open ? 1 : 0) - up) * Math.min(Math.abs((open ? 1 : 0) - up), d / 3); // 3 s
          doorPivot.scale.y = Math.max(0.02, 1 - 0.98 * up);
          if (open !== shown) { shown = open; lampMat.emissive.setHex(open ? 0x2FBF71 : 0xE0563A); lampMat.emissiveIntensity = 2.4; }
          const n = Math.round(num(s.parcels)), vis = n <= 0 ? 0 : Math.min(boxes.length, 1 + Math.floor(n / 40));
          if (vis !== shownN) { shownN = vis; boxes.forEach((b, i) => { b.visible = i < vis; }); }
          led.set(s, t, open ? HEX.green : HEX.amber, 2);
        },
      };
    },
  },
};
