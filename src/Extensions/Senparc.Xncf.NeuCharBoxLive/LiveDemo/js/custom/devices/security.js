// The Security and access types' card UI and 3D models for the custom room (contract: devices/index.js): a smart lock
// with its bolt, a video doorbell, a door/window contact sensor, a smoke/CO alarm, a siren and a garage door that
// slides up over 8 s. Each has its own status LED; standby is green, a dim amber or off, never the fault red.
import { HEX, mergeMeshes, statusLed, ease, stepper, glowMesh as kitGlow } from './kit.js';

// A glowing mesh with its own material (never a shared one).
const glowMesh = (THREE, geo, hex, emissive) => kitGlow(THREE, geo, hex, emissive, 0.4);
const quiet = (m) => { m.userData.noAO = true; m.raycast = () => {}; m.castShadow = false; return m; };

export default {
  // A deadbolt on a door edge: a dark body with a thumb-turn, and a metal bolt that slides out into the strike plate
  // on the frame while locked. The thumb-turn quarter-turns with it.
  smart_lock: {
    icon: 'lock', size: 0.2, mountY: 1.1,
    edit: [{ field: 'locked', kind: 'switch' }],
    format: (s) => (s.locked ? 'locked' : 'unlocked'),
    active: (s) => !!s.locked,
    control: { kind: 'switch', field: 'locked' },
    model({ THREE, P, M }) {
      const body = P.box(0.1, 0.17, 0.03, M.black, -0.045, 0, 0.015, 0.012);
      const ring = P.cyl(0.036, 0.036, 0.01, M.steel, -0.045, -0.015, 0.035, 24); ring.rotation.x = Math.PI / 2;
      const turn = P.box(0.012, 0.05, 0.016, M.metal, 0, 0, 0, 0.004); // thumb-turn, pivots about the ring centre
      const turnPivot = P.group(turn); turnPivot.position.set(-0.045, -0.015, 0.045);
      const slot = P.box(0.03, 0.036, 0.022, M.anodized, 0.04, 0.03, 0.011, 0.004);
      const bolt = P.box(0.05, 0.026, 0.016, M.metal, 0, 0.03, 0.013, 0.004);
      const strike = P.box(0.03, 0.12, 0.02, M.steel, 0.085, 0, 0.01, 0.004);
      const hole = P.box(0.012, 0.034, 0.004, M.anodized, 0.085, 0.03, 0.021);
      const root = P.group(body, ring, turnPivot, slot, bolt, strike, hole);
      const led = statusLed(THREE, root, { at: [-0.045, 0.062, 0.031], r: 0.006 });
      const dt = stepper(); let k = 1;
      return {
        root,
        update(s, t) {
          const locked = !!s.locked;
          k = ease(k, locked ? 1 : 0, dt(t), 7);
          bolt.position.x = k * 0.07; // retracted inside the body, extended into the strike
          turnPivot.rotation.z = (1 - k) * Math.PI / 2;
          led.set(s, t, locked ? HEX.green : HEX.amber, 2);
        },
      };
    },
  },

  // A slim doorbell with a camera lens above its button. The button ring glows blue and pulses while it is rung; the
  // lens ring and the LED go amber while it sees movement.
  video_doorbell: {
    icon: 'doorbell', size: 0.12, mountY: 1.3,
    edit: [{ field: 'ringing', kind: 'switch', label: 'ringing' }, { field: 'motion', kind: 'switch', label: 'motion' }],
    format: (s) => (s.ringing ? 'ringing' : s.motion ? 'movement at the door' : 'quiet'),
    active: (s) => !!s.ringing || !!s.motion,
    control: false,
    model({ THREE, P, M }) {
      const plate = P.box(0.07, 0.17, 0.026, M.black, 0, 0, 0.013, 0.012);
      const face = P.box(0.058, 0.158, 0.006, M.white, 0, 0, 0.027, 0.01);
      const lens = P.cyl(0.015, 0.015, 0.008, M.anodized, 0, 0.04, 0.032, 20); lens.rotation.x = Math.PI / 2;
      const lensRing = glowMesh(THREE, new THREE.TorusGeometry(0.018, 0.003, 8, 24), 0x2C3035, 0xFFB020);
      lensRing.position.set(0, 0.04, 0.032);
      const btn = glowMesh(THREE, new THREE.TorusGeometry(0.014, 0.004, 8, 24), 0xBFC4C8, 0x3AB7FF);
      btn.position.set(0, -0.04, 0.031);
      const btnDot = P.cyl(0.009, 0.009, 0.006, M.steel, 0, -0.04, 0.03, 16); btnDot.rotation.x = Math.PI / 2;
      const wave = new THREE.Mesh(new THREE.RingGeometry(0.02, 0.026, 28), new THREE.MeshBasicMaterial({ color: 0x3AB7FF, transparent: true, opacity: 0, depthWrite: false, side: THREE.DoubleSide }));
      wave.position.set(0, -0.04, 0.05); quiet(wave); wave.visible = false;
      const root = P.group(plate, face, lens, lensRing, btn, btnDot, wave);
      const led = statusLed(THREE, root, { at: [0, 0.073, 0.031], r: 0.004, size: 0.03 });
      return {
        root,
        update(s, t) {
          const ring = !!s.ringing, mo = !!s.motion;
          btn.material.emissiveIntensity = ring ? 2 + 1.5 * Math.sin(t * 9) : 0.35;
          lensRing.material.emissiveIntensity = mo ? 2.4 : 0;
          wave.visible = ring;
          if (ring) { const k = (t * 1.6) % 1; wave.scale.setScalar(1 + k * 1.6); wave.material.opacity = (1 - k) * 0.7; }
          led.set(s, t, ring ? HEX.blue : mo ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },

  // A two-piece contact sensor on a frame: the sensor body and its magnet sit together while closed and part by 6 cm
  // when open.
  door_window_sensor: {
    icon: 'windowsensor', size: 0.2, mountY: 1.0,
    edit: [{ field: 'open', kind: 'switch' }],
    format: (s) => (s.open ? 'open' : 'closed'),
    active: (s) => !!s.open,
    control: { kind: 'switch', field: 'open' },
    model({ THREE, P, M }) {
      const sensor = P.box(0.09, 0.034, 0.02, M.white, -0.0475, 0, 0.01, 0.006);
      const magnet = P.box(0.04, 0.034, 0.02, M.white, 0.0225, 0, 0.01, 0.006);
      const mark = P.box(0.03, 0.004, 0.003, M.steel, 0, 0, 0.0115); mark.castShadow = false; // the magnet's mark
      magnet.add(mark);
      const root = P.group(sensor, magnet);
      const led = statusLed(THREE, root, { at: [-0.075, 0, 0.0205], r: 0.0045, size: 0.03 });
      const dt = stepper(); let k = 0;
      return {
        root,
        update(s, t) {
          const open = !!s.open;
          k = ease(k, open ? 1 : 0, dt(t), 6);
          magnet.position.x = 0.0225 + k * 0.06;
          led.set(s, t, open ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },

  // A round ceiling alarm with a vent ring. The ring and a halo flash orange while the alarm is on; the LED is a dim
  // green heartbeat otherwise.
  smoke_co_alarm: {
    icon: 'smoke', size: 0.28,
    edit: [{ field: 'alarm', kind: 'switch' }],
    format: (s) => (s.alarm ? 'alarm sounding' : 'all clear'),
    active: (s) => !!s.alarm,
    control: false,
    model({ THREE, P, M }) {
      const base = P.cyl(0.1, 0.1, 0.012, M.white, 0, -0.006, 0, 36);
      const body = P.cyl(0.085, 0.1, 0.03, M.white, 0, -0.027, 0, 36);
      const vents = [];
      for (let i = 0; i < 12; i++) {
        const a = i / 12 * Math.PI * 2;
        const v = P.box(0.012, 0.004, 0.03, M.anodized, Math.cos(a) * 0.058, -0.0425, Math.sin(a) * 0.058);
        v.rotation.y = -a + Math.PI / 2; v.castShadow = false; vents.push(v);
      }
      const cap = P.cyl(0.034, 0.034, 0.008, M.hub, 0, -0.046, 0, 24);
      const ring = glowMesh(THREE, new THREE.TorusGeometry(0.075, 0.004, 8, 40), 0xDCE0DF, 0xFF4A1C);
      ring.rotation.x = Math.PI / 2; ring.position.y = -0.043;
      const halo = new THREE.Mesh(new THREE.RingGeometry(0.105, 0.135, 36), new THREE.MeshBasicMaterial({ color: 0xFF4A1C, transparent: true, opacity: 0, depthWrite: false, side: THREE.DoubleSide }));
      halo.rotation.x = Math.PI / 2; halo.position.y = -0.047; quiet(halo); halo.visible = false;
      const root = P.group(base, body, ...vents, cap, ring, halo);
      const led = statusLed(THREE, root, { at: [0, -0.0515, 0], r: 0.006 });
      return {
        root,
        update(s, t) {
          const al = !!s.alarm, blink = Math.floor(t * 3) % 2 === 0;
          ring.material.emissiveIntensity = al ? (blink ? 4 : 0.6) : 0;
          halo.visible = al; if (al) halo.material.opacity = blink ? 0.7 : 0.15;
          led.set(s, t, al ? 0xFF4A1C : HEX.green, al ? (blink ? 5 : 1) : 0.8 + 0.5 * Math.sin(t * 1.5));
        },
      };
    },
  },

  // A wall siren: a white box with a speaker horn and a red beacon on top. The beacon flashes and sound arcs spread
  // while it sounds.
  siren: {
    icon: 'siren', size: 0.3, mountY: 2.2,
    edit: [{ field: 'sounding', kind: 'switch' }],
    format: (s) => (s.sounding ? 'sounding' : 'silent'),
    active: (s) => !!s.sounding,
    control: { kind: 'switch', field: 'sounding' },
    model({ THREE, P, M }) {
      const box = P.box(0.22, 0.2, 0.07, M.white, 0, -0.03, 0.035, 0.012);
      const horn = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.028, 0.07, 22, 1, true), M.anodized.clone());
      horn.material.side = THREE.DoubleSide;
      horn.rotation.x = Math.PI / 2; horn.position.set(0, -0.045, 0.1); horn.castShadow = true;
      const throat = P.cyl(0.03, 0.03, 0.012, M.black, 0, -0.045, 0.07, 16); throat.rotation.x = Math.PI / 2;
      const base = P.cyl(0.045, 0.05, 0.02, M.black, 0, 0.08, 0.06, 20);
      const beacon = glowMesh(THREE, new THREE.SphereGeometry(0.045, 18, 12, 0, Math.PI * 2, 0, Math.PI / 2), 0xE6E3DC, 0xFF2A1A);
      beacon.position.set(0, 0.09, 0.06); beacon.material.roughness = 0.25;
      const arcs = [0, 1, 2].map(() => {
        const a = new THREE.Mesh(new THREE.RingGeometry(0.05, 0.057, 28), new THREE.MeshBasicMaterial({ color: 0xFF5A3A, transparent: true, opacity: 0, depthWrite: false, side: THREE.DoubleSide }));
        a.position.set(0, -0.045, 0.14); quiet(a); a.visible = false; return a;
      });
      const root = P.group(box, horn, throat, base, beacon, ...arcs);
      const led = statusLed(THREE, root, { at: [-0.09, -0.1, 0.0705], r: 0.005 });
      return {
        root,
        update(s, t) {
          const on = !!s.sounding, blink = Math.floor(t * 4) % 2 === 0;
          beacon.material.emissiveIntensity = on ? (blink ? 5 : 0.8) : 0;
          beacon.material.color.setHex(!on ? 0xE6E3DC : blink ? 0xFF5A4A : 0xB8332A); // pale and clear until it sounds: red at rest would read as a fault
          arcs.forEach((a, i) => {
            a.visible = on;
            if (!on) return;
            const k = (t * 1.2 + i / 3) % 1; a.scale.setScalar(0.8 + k * 1.1); a.position.z = 0.14 + k * 0.01; a.material.opacity = (1 - k) * 0.7;
          });
          led.set(s, t, on ? HEX.amber : HEX.green, on ? 3 : 1);
        },
      };
    },
  },

  // A sectional garage door, 1.9 × 2 m in the wall, with its opener on the header. The panels rise and stack at the
  // top over 8 s (the same time the server takes), showing the dark bay behind; a lamp on the opener blinks while moving.
  garage_door: {
    icon: 'garage', size: 1.9, mountY: 1.0,
    edit: [{ field: 'open', kind: 'switch' }],
    format: (s) => (s.open ? 'open' : 'closed'),
    active: (s) => !!s.open,
    control: { kind: 'switch', field: 'open' },
    model({ THREE, P, M }) {
      const H = 1.96, TOPY = 0.98, W = 1.76; // the bay runs from the floor (y -1) to TOPY
      const jambs = [-0.9, 0.9].map((x) => P.box(0.1, 2.0, 0.08, M.trim, x, 0, 0.04));
      const header = P.box(1.9, 0.1, 0.1, M.trim, 0, 0.95, 0.05, 0.004);
      const dark = new THREE.MeshStandardMaterial({ color: 0x15181B, roughness: 1 });
      const bay = P.box(W, H, 0.01, dark, 0, -0.02, 0.005);
      const panelMat = new THREE.MeshStandardMaterial({ color: 0xDADDDC, roughness: 0.6, metalness: 0.15 });
      const panels = [];
      for (let i = 0; i < 6; i++) {
        panels.push(P.box(W, 0.316, 0.04, panelMat, 0, -(i + 0.5) * 0.3267, 0, 0.006));
        panels.push(P.box(W - 0.3, 0.19, 0.012, panelMat, 0, -(i + 0.5) * 0.3267, 0.024, 0.006));
      }
      const door = mergeMeshes(THREE, panels, panelMat, { consume: true });
      const doorPivot = P.group(door); doorPivot.position.set(0, TOPY, 0.04);
      const opener = P.box(0.5, 0.12, 0.18, M.black, 0, 0.83, 0.17, 0.01);
      const lampGlass = glowMesh(THREE, new THREE.SphereGeometry(0.03, 12, 8), 0xE7E0CF, 0xFFB020);
      lampGlass.position.set(0.18, 0.83, 0.26);
      const step = P.box(W, 0.02, 0.2, M.steel, 0, -0.99, 0.1); step.castShadow = false;
      const root = P.group(...jambs, header, bay, doorPivot, opener, lampGlass, step);
      const led = statusLed(THREE, root, { at: [-0.18, 0.83, 0.262], r: 0.01 });
      const dt = stepper(); let up = null; // null until the first state: a door that starts open shows open, not rising from closed
      return {
        root,
        update(s, t) {
          const target = s.open ? 1 : 0, d = dt(t); if (up === null) up = target;
          up = Math.abs(target - up) < 0.0005 ? target : up + Math.sign(target - up) * Math.min(Math.abs(target - up), d / 8); // 8 s end to end
          doorPivot.scale.y = Math.max(0.1, 1 - 0.9 * up);
          const moving = up > 0 && up < 1;
          lampGlass.material.emissiveIntensity = moving ? (Math.floor(t * 3) % 2 === 0 ? 4 : 0.3) : 0;
          led.set(s, t, moving ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },
};
