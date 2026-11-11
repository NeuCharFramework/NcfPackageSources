// The Elder care types' card UI and 3D models for the custom room (contract: devices/index.js). Their look follows the
// elder scene (js/scenes/elder.js): a bed with a pressure mat, a door with a contact sensor, a hallway motion dome, a
// kettle on its smart plug, and the two lights. Each is rebuilt as a compact, self-contained model with its own status LED.
import { HEX, num, clamp01, pct, statusLed, pointLight, glowMesh } from './kit.js';

const on3 = (s) => !!s.on;
const dimmer = (s) => { const r = pct(s.brightness); return r <= 0 ? 'off' : r >= 100 ? 'full' : `on · ${r}%`; };


export default {
  // A single bed (1 × 2 m, head at the back) with the pressure mat across the mattress. The mat lights up and a person's
  // shape lies under the blanket while occupied; the mat sits a little wider than the sleeper, so both read at once.
  bed_sensor: {
    icon: 'bed', size: 2,
    edit: [{ field: 'occupied', kind: 'switch', label: 'in bed' }],
    format: (s) => (s.occupied ? 'in bed' : 'out of bed'),
    active: (s) => !!s.occupied,
    control: false,
    model({ THREE, P, M }) {
      const frame = P.box(1.0, 0.22, 2.0, M.wood, 0, 0.11, 0, 0.02);
      const mattress = P.box(0.94, 0.2, 1.9, M.bedding, 0, 0.32, 0, 0.05);
      const pillow = P.box(0.6, 0.1, 0.34, M.white, 0, 0.47, -0.74, 0.04);
      const head = P.box(1.0, 0.55, 0.06, M.wood, 0, 0.5, -0.97, 0.02);
      const blanket = P.box(0.92, 0.06, 0.62, M.cushion, 0, 0.45, 0.62, 0.03);
      const matMat = new THREE.MeshStandardMaterial({ color: 0x2F3A3A, emissive: 0x2FBF71, emissiveIntensity: 0, roughness: 0.6 });
      const mat = P.box(0.82, 0.014, 0.6, matMat, 0, 0.427, -0.12, 0.004); mat.castShadow = false;
      const sleeper = P.box(0.44, 0.17, 0.92, M.cushion, 0, 0.5, -0.1, 0.075);
      const box = P.box(0.07, 0.04, 0.05, M.white, 0.42, 0.44, 0.3, 0.006);
      const root = P.group(frame, mattress, pillow, head, blanket, mat, sleeper, box);
      const led = statusLed(THREE, root, { at: [0.42, 0.464, 0.325], r: 0.006 });
      return {
        root,
        update(s, t) {
          const occ = !!s.occupied;
          sleeper.visible = occ;
          matMat.emissiveIntensity = occ ? 0.5 + 0.15 * Math.sin(t * 2) : 0;
          matMat.color.setHex(occ ? 0x3FA89A : 0x2F3A3A);
          led.set(s, t, occ ? HEX.green : HEX.amber, 2);
        },
      };
    },
  },

  // A door in the wall with its frame; the leaf swings into the room when open. A contact sensor sits on the frame head.
  bedroom_door: {
    icon: 'door', size: 1, mountY: 1.02,
    edit: [{ field: 'open', kind: 'switch' }],
    format: (s) => (s.open ? 'open' : 'closed'),
    active: (s) => !!s.open,
    control: { kind: 'switch', field: 'open' },
    model({ THREE, P, M }) {
      const jambL = P.box(0.06, 2.04, 0.06, M.trim, -0.43, 0, 0.03), jambR = P.box(0.06, 2.04, 0.06, M.trim, 0.43, 0, 0.03), headC = P.box(0.92, 0.06, 0.06, M.trim, 0, 0.99, 0.03);
      const leaf = P.box(0.76, 1.96, 0.04, M.door, 0.38, -0.01, 0.0, 0.004); // hinge at x = -0.38 (the pivot's origin)
      const knob = new THREE.Mesh(new THREE.SphereGeometry(0.025, 12, 10), M.metal); knob.position.set(0.3, 0, 0.04); knob.castShadow = true;
      const magnet = P.box(0.012, 0.07, 0.02, M.white, 0.37, 0.93, 0.0);
      leaf.add(knob, magnet);
      const pivot = P.group(leaf); pivot.position.set(-0.38, 0, 0.03);
      const sensor = P.box(0.08, 0.03, 0.03, M.white, 0.28, 1.035, 0.015, 0.004);
      const root = P.group(jambL, jambR, headC, pivot, sensor);
      const led = statusLed(THREE, root, { at: [0.28, 1.036, 0.032], r: 0.006 });
      let angle = 0, last = null;
      return {
        root,
        update(s, t) {
          const target = s.open ? 1.2 : 0, dt = last == null ? 1 : Math.min(0.2, Math.max(0, t - last)); last = t;
          angle = Math.abs(target - angle) < 0.002 ? target : angle + (target - angle) * Math.min(1, dt * 6);
          pivot.rotation.y = -angle;
          led.set(s, t, s.open ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },

  // A small white plate with a dome; the dome glows amber with sensing rings spreading while it sees movement.
  hallway_motion: {
    icon: 'motion', size: 0.24, mountY: 2.3,
    edit: [{ field: 'motion', kind: 'switch' }],
    format: (s) => (s.motion ? 'movement now' : 'no movement'),
    active: (s) => !!s.motion,
    control: false,
    model({ THREE, P, M }) {
      const plate = P.box(0.1, 0.1, 0.016, M.white, 0, 0, 0.008, 0.01);
      const dome = glowMesh(THREE, new THREE.SphereGeometry(0.04, 18, 10, 0, Math.PI * 2, 0, Math.PI / 2), 0xEFEFEA, 0xFFB020);
      dome.rotation.x = Math.PI / 2; dome.position.z = 0.016; dome.material.roughness = 0.25;
      const rings = [0, 1].map(() => {
        const r = new THREE.Mesh(new THREE.RingGeometry(0.048, 0.056, 28), new THREE.MeshBasicMaterial({ color: 0xFFB020, transparent: true, opacity: 0, depthWrite: false, side: THREE.DoubleSide }));
        r.position.z = 0.05; r.userData.noAO = true; r.raycast = () => {}; r.visible = false; return r;
      });
      const root = P.group(plate, dome, ...rings);
      const led = statusLed(THREE, root, { at: [0, -0.07, 0.016], r: 0.005 });
      return {
        root,
        update(s, t) {
          const m = !!s.motion;
          dome.material.emissiveIntensity = m ? 2.2 : 0;
          rings.forEach((r, i) => {
            r.visible = m;
            if (!m) return;
            const k = (t * 0.9 + i * 0.5) % 1; r.scale.setScalar(1 + k * 0.9); r.material.opacity = (1 - k) * 0.7;
          });
          led.set(s, t, m ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },

  // A kettle standing on its smart plug. Steam rises while it draws power; the LED is amber while on.
  kettle_plug: {
    icon: 'kettle', size: 0.5,
    edit: [{ field: 'on', kind: 'switch' }],
    format: (s) => (s.on ? `on · ${Math.round(num(s.watts)).toLocaleString('en-GB')} W` : 'off'),
    active: on3,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const pad = P.box(0.26, 0.035, 0.18, M.white, 0, 0.0175, 0.02, 0.012);
      const body = P.cyl(0.08, 0.1, 0.2, M.metal, 0, 0.135, 0.02, 24), lid = P.cyl(0.05, 0.07, 0.025, M.black, 0, 0.2475, 0.02, 20);
      const handle = P.box(0.02, 0.17, 0.035, M.black, 0.115, 0.14, 0.02, 0.006), spout = P.box(0.06, 0.04, 0.05, M.metal, -0.1, 0.2, 0.02, 0.01); spout.rotation.z = 0.5;
      const lead = P.tube([[0.1, 0.02, 0.11], [0.17, 0.012, 0.15], [0.2, 0.012, 0.22]], 0.006, M.white);
      const puffMat = new THREE.MeshBasicMaterial({ color: 0xFFFFFF, transparent: true, opacity: 0, depthWrite: false });
      const puffs = [0, 1, 2].map(() => { const p = new THREE.Mesh(new THREE.SphereGeometry(0.035, 10, 8), puffMat.clone()); p.userData.noAO = true; p.raycast = () => {}; p.visible = false; p.position.y = 0.3; return p; });
      const root = P.group(pad, body, lid, handle, spout, lead, ...puffs);
      const led = statusLed(THREE, root, { at: [0.09, 0.0365, 0.1], r: 0.007 });
      return {
        root,
        update(s, t) {
          const run = !!s.on && num(s.watts) > 0;
          puffs.forEach((p, i) => {
            p.visible = run;
            if (!run) return;
            const k = (t * 0.5 + i / 3) % 1; p.position.set(-0.02 + 0.03 * Math.sin(k * 6 + i), 0.29 + k * 0.32, 0.02); p.scale.setScalar(0.7 + k * 1.4); p.material.opacity = (1 - k) * 0.35;
          });
          led.set(s, t, s.on ? HEX.amber : HEX.green, s.on ? 4 : 2);
        },
      };
    },
  },

  // A plug-in night light low on the wall: a small plate with a warm dome that glows with the dimmer.
  night_light: {
    icon: 'bulb', size: 0.12, mountY: 0.35,
    edit: [{ field: 'brightness', kind: 'slider', step: 0.01, scale: 100, unit: '%' }],
    format: dimmer,
    active: (s) => pct(s.brightness) > 0,
    control: { kind: 'bar', field: 'brightness', min: 0, max: 1 },
    model({ THREE, P, M }) {
      const plate = P.box(0.08, 0.12, 0.02, M.white, 0, 0, 0.01, 0.01);
      const dome = glowMesh(THREE, new THREE.SphereGeometry(0.036, 16, 10, 0, Math.PI * 2, 0, Math.PI / 2), 0xF3EBDD, 0xFFD9A0);
      dome.rotation.x = Math.PI / 2; dome.position.z = 0.02; dome.material.roughness = 0.3;
      const root = P.group(plate, dome);
      const led = statusLed(THREE, root, { at: [0, -0.048, 0.0205], r: 0.0045 });
      const light = pointLight(THREE, root, [0, 0, 0.08], { hex: 0xFFD9A0, distance: 3.5 }); // a night light: soft and short-reaching
      let shown = -1;
      return {
        root,
        update(s, t) {
          const b = clamp01(s.brightness);
          if (b !== shown) { shown = b; dome.material.emissiveIntensity = b * 3; light.intensity = b * 0.9; }
          led.set(s, t);
        },
      };
    },
  },

  // A flush ceiling light: a round base with a shallow diffuser that glows with the dimmer.
  hall_light: {
    icon: 'bulb', size: 0.4,
    edit: [{ field: 'brightness', kind: 'slider', step: 0.01, scale: 100, unit: '%' }],
    format: dimmer,
    active: (s) => pct(s.brightness) > 0,
    control: { kind: 'bar', field: 'brightness', min: 0, max: 1 },
    model({ THREE, P, M }) {
      const base = P.cyl(0.18, 0.18, 0.03, M.white, 0, -0.015, 0, 32);
      const diff = glowMesh(THREE, new THREE.SphereGeometry(0.16, 24, 8, 0, Math.PI * 2, Math.PI / 2, Math.PI / 2), 0xF6EEDD, 0xFFE2B8);
      diff.scale.y = 0.4; diff.position.y = -0.03; diff.material.roughness = 0.35;
      const root = P.group(base, diff);
      const led = statusLed(THREE, root, { at: [0.17, -0.031, 0], r: 0.006 });
      const light = pointLight(THREE, root, [0, -0.2, 0], { hex: 0xFFE2B8 });
      let shown = -1;
      return {
        root,
        update(s, t) {
          const b = clamp01(s.brightness);
          if (b !== shown) { shown = b; diff.material.emissiveIntensity = b * 3; light.intensity = b * 2.2; }
          led.set(s, t);
        },
      };
    },
  },
};
