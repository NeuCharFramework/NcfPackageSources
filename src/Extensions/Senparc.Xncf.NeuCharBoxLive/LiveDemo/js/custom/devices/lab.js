// The Laser lab types' card UI and 3D models for the custom room (contract: devices/index.js). Their look follows the lab
// scene (js/scenes/lab.js): an anodized laser head with a flip-up shutter, a mirror in a mount, a linear stage with a
// ruler and a power meter. Each sits on its own small optical breadboard (here nothing shares the table, so there is no
// beam from one to the next): the laser shows its own beam into a dump, the mirror shows its tilt, and so on.
import { HEX, num, clamp01, statusLed, mergeMeshes, breadboard, BOARD_TOP, ease, stepper } from './kit.js';

const Y0 = BOARD_TOP; // the breadboard's top
const deg = (v) => { const x = num(v); return Math.abs(x) < 0.05 ? '+0.0°' : `${x >= 0 ? '+' : '−'}${Math.abs(x).toFixed(1)}°`; }; // +0.1°, −2.5°: one decimal, as its slider reads

export default {
  // A laser head on a breadboard, firing along the board into a beam dump. A flip-up shutter sits in front of the
  // aperture: upright while closed (no beam), folded flat once open, and the red beam brightens with the power setting.
  laser: {
    icon: 'laser', size: 0.6,
    edit: [{ field: 'open', kind: 'switch', label: 'shutter' }, { field: 'mW', kind: 'slider', step: 0.5, unit: ' mW', label: 'power' }],
    format: (s) => `shutter ${s.open ? 'open' : 'closed'} · ${num(s.mW).toFixed(1)} mW set`,
    active: (s) => !!s.open,
    control: { kind: 'switch', field: 'open' },
    model({ THREE, P, M }) {
      const board = breadboard(THREE, P, M, 0.6, 0.4);
      const head = P.box(0.24, 0.09, 0.09, M.anodized, -0.15, Y0 + 0.045, 0, 0.008);
      const nose = P.cyl(0.02, 0.024, 0.03, M.steel, -0.02, Y0 + 0.045, 0, 16); nose.rotation.z = Math.PI / 2;
      const label = P.box(0.07, 0.004, 0.05, M.white, -0.2, Y0 + 0.092, 0);
      const dump = P.box(0.045, 0.07, 0.07, M.black, 0.26, Y0 + 0.035 + 0.01, 0, 0.004);
      const post = P.cyl(0.008, 0.008, 0.012, M.steel, 0.045, Y0 + 0.006, 0, 10);
      const paddle = P.box(0.008, 0.07, 0.05, M.black, 0, 0.037, 0, 0.002); // hinged at its foot
      const flap = P.group(paddle); flap.position.set(0.045, Y0 + 0.012, 0);
      const beamMat = new THREE.MeshBasicMaterial({ color: 0xFF2D2D, transparent: true, opacity: 0, depthWrite: false, blending: THREE.AdditiveBlending });
      const beam = new THREE.Mesh(new THREE.CylinderGeometry(0.0035, 0.0035, 0.21, 8), beamMat);
      beam.rotation.z = Math.PI / 2; beam.position.set(0.145, Y0 + 0.045, 0); beam.userData.noAO = true; beam.raycast = () => {}; beam.visible = false;
      const spotMat = new THREE.MeshBasicMaterial({ color: 0xFF5A4A, transparent: true, opacity: 0, depthWrite: false, blending: THREE.AdditiveBlending });
      const spot = new THREE.Mesh(new THREE.CircleGeometry(0.014, 12), spotMat);
      spot.rotation.y = -Math.PI / 2; spot.position.set(0.2375, Y0 + 0.045, 0); spot.userData.noAO = true; spot.raycast = () => {}; spot.visible = false;
      const root = P.group(...board, head, nose, label, dump, post, flap, beam, spot);
      const led = statusLed(THREE, root, { at: [-0.23, Y0 + 0.0915, 0.025], r: 0.006 });
      const dt = stepper(); let fold = 0;
      return {
        root,
        update(s, t) {
          const open = !!s.open, k = clamp01(num(s.mW) / 20);
          fold = ease(fold, open ? 1 : 0, dt(t), 9);
          flap.rotation.x = fold * (Math.PI / 2 - 0.02); // upright → folded flat toward +z
          beam.visible = spot.visible = open && fold > 0.9;
          beamMat.opacity = 0.4 + 0.5 * k; spotMat.opacity = 0.4 + 0.55 * k;
          led.set(s, t, open ? HEX.red : HEX.green, open ? 4 : 1.5);
        },
      };
    },
  },

  // A mirror on a post in a gimbal mount. The face tips with yaw and pitch (drawn three times larger than the real
  // fraction of a degree, so a change shows from across the room).
  mirror: {
    icon: 'mirror', size: 0.4,
    edit: [{ field: 'yaw', kind: 'slider', step: 0.1, unit: '°', label: 'yaw' }, { field: 'pitch', kind: 'slider', step: 0.1, unit: '°', label: 'pitch' }],
    format: (s) => `yaw ${deg(s.yaw)} · pitch ${deg(s.pitch)}`,
    active: (s) => Math.abs(num(s.yaw)) > 0.005 || Math.abs(num(s.pitch)) > 0.005,
    control: false,
    model({ THREE, P, M }) {
      const board = breadboard(THREE, P, M, 0.4, 0.3);
      const clamp = P.box(0.07, 0.016, 0.07, M.anodized, -0.04, Y0 + 0.008, 0, 0.003);
      const post = P.cyl(0.012, 0.012, 0.11, M.steel, -0.04, Y0 + 0.07, 0, 12);
      const ring = P.cyl(0.05, 0.05, 0.016, M.anodized, 0, 0, 0, 24); ring.rotation.x = Math.PI / 2;
      const glassMat = new THREE.MeshStandardMaterial({ color: 0xBFD8EA, roughness: 0.04, metalness: 1 });
      const glass = new THREE.Mesh(new THREE.CylinderGeometry(0.041, 0.041, 0.006, 28), glassMat); glass.rotation.x = Math.PI / 2; glass.position.z = 0.011;
      const knobs = [-0.032, 0.032].map((x) => P.cyl(0.006, 0.006, 0.03, M.steel, x, 0, -0.022, 8));
      knobs.forEach((k) => { k.rotation.x = Math.PI / 2; });
      const head = P.group(ring, glass, ...knobs); head.position.set(-0.04, Y0 + 0.155, 0);
      const box = P.box(0.07, 0.022, 0.05, M.black, 0.12, Y0 + 0.011, 0.07, 0.004);
      const lead = P.tube([[-0.04, Y0 + 0.012, 0.03], [0.0, Y0 + 0.006, 0.07], [0.09, Y0 + 0.01, 0.07]], 0.004, M.black);
      const root = P.group(...board, clamp, post, head, box, lead);
      const led = statusLed(THREE, root, { at: [0.12, Y0 + 0.0225, 0.07], r: 0.006 });
      const dt = stepper(); let yaw = 0, pitch = 0;
      const K = (Math.PI / 180) * 3; // 3× the real angle
      return {
        root,
        update(s, t) {
          const d = dt(t);
          yaw = ease(yaw, Math.max(-5, Math.min(5, num(s.yaw))), d, 6); pitch = ease(pitch, Math.max(-5, Math.min(5, num(s.pitch))), d, 6);
          head.rotation.y = yaw * K; head.rotation.x = -pitch * K;
          led.set(s, t);
        },
      };
    },
  },

  // A motorised stage: a carriage runs along a rail beside a ruler. The motor sits at the left end.
  linear_stage: {
    icon: 'stage', size: 0.6,
    edit: [{ field: 'x', kind: 'slider', step: 0.5, unit: ' mm' }],
    format: (s) => `${num(s.x).toFixed(1)} mm`,
    active: (s) => num(s.x) > 0.05,
    control: { kind: 'bar', field: 'x', min: 0, max: 25 },
    model({ THREE, P, M }) {
      const board = breadboard(THREE, P, M, 0.6, 0.3);
      const rail = P.box(0.46, 0.03, 0.1, M.steel, 0, Y0 + 0.015, 0, 0.004);
      const motor = P.box(0.05, 0.06, 0.08, M.black, -0.265, Y0 + 0.03, 0, 0.004);
      const screwEnd = P.box(0.015, 0.04, 0.09, M.anodized, 0.24, Y0 + 0.02, 0, 0.003);
      const ticks = [];
      for (let i = 0; i <= 10; i++) ticks.push(P.box(0.002, 0.0012, i % 5 === 0 ? 0.014 : 0.009, M.black, -0.16 + i * 0.032, Y0 + 0.0062, 0.071));
      const strip = P.box(0.34, 0.004, 0.018, M.white, 0, Y0 + 0.002 + 0.0015, 0.071);
      const ruler = mergeMeshes(THREE, ticks, M.black, { consume: true });
      const carriage = P.box(0.14, 0.035, 0.09, M.anodized, 0, 0.0475, 0, 0.004);
      const optic = P.cyl(0.014, 0.014, 0.06, M.steel, 0, 0.095, 0, 12);
      const mark = P.box(0.004, 0.004, 0.03, M.yellow, 0, 0.067, 0.02);
      const slide = P.group(carriage, optic, mark); slide.position.y = Y0; // local y 0 is the breadboard top
      const root = P.group(...board, rail, motor, screwEnd, strip, ruler, slide);
      const led = statusLed(THREE, root, { at: [-0.265, Y0 + 0.0615, 0], r: 0.006 });
      const dt = stepper(); let pos = 0;
      return {
        root,
        update(s, t) {
          const target = (clamp01(num(s.x) / 25) - 0.5) * 0.32;
          pos = Math.abs(target - pos) < 0.0004 ? target : pos + (target - pos) * Math.min(1, dt(t) * 7);
          slide.position.x = pos;
          led.set(s, t);
        },
      };
    },
  },

  // A power meter: a black absorber head on a post and a small readout box whose bar of eight segments follows the
  // reading (0–20 mW). The head's face glows red with the power.
  power_meter: {
    icon: 'meter', size: 0.4,
    edit: [{ field: 'mW', kind: 'slider', step: 0.1, unit: ' mW' }],
    format: (s) => `${num(s.mW).toFixed(2)} mW`,
    active: (s) => num(s.mW) > 0.05,
    control: false,
    model({ THREE, P, M }) {
      const board = breadboard(THREE, P, M, 0.4, 0.3);
      const post = P.cyl(0.011, 0.011, 0.1, M.steel, -0.12, Y0 + 0.05, 0, 12);
      const headBody = P.cyl(0.05, 0.05, 0.022, M.black, -0.12, Y0 + 0.115, 0, 24); headBody.rotation.z = Math.PI / 2;
      const faceMat = new THREE.MeshStandardMaterial({ color: 0x2A1A1A, emissive: 0xFF3030, emissiveIntensity: 0, roughness: 0.6 });
      const face = new THREE.Mesh(new THREE.CircleGeometry(0.036, 20), faceMat); face.rotation.y = Math.PI / 2; face.position.set(-0.108, Y0 + 0.115, 0);
      const unit = P.box(0.14, 0.07, 0.09, M.anodized, 0.08, Y0 + 0.035, 0.02, 0.006);
      const screen = P.box(0.12, 0.034, 0.003, M.screen, 0.08, Y0 + 0.045, 0.0665);
      const litMat = new THREE.MeshStandardMaterial({ color: 0x1B3B2C, emissive: 0x39E58B, emissiveIntensity: 1.8 });
      const darkMat = new THREE.MeshStandardMaterial({ color: 0x142019, roughness: 0.6 });
      const segs = [];
      for (let i = 0; i < 8; i++) { const sg = P.box(0.011, 0.016, 0.003, darkMat, 0.08 - 0.0455 + i * 0.013, Y0 + 0.045, 0.0685); sg.castShadow = false; segs.push(sg); }
      const cable = P.tube([[-0.12, Y0 + 0.01, 0.0], [-0.06, Y0 + 0.006, -0.08], [0.03, Y0 + 0.008, -0.07], [0.05, Y0 + 0.02, -0.02]], 0.0045, M.black);
      const root = P.group(...board, post, headBody, face, unit, screen, ...segs, cable);
      const led = statusLed(THREE, root, { at: [0.135, Y0 + 0.0715, 0.02], r: 0.006 });
      let shown = -1;
      return {
        root,
        update(s, t) {
          const mw = num(s.mW), lit = mw > 0.05 ? Math.max(1, Math.ceil(clamp01(mw / 20) * 8)) : 0;
          if (lit !== shown) { shown = lit; segs.forEach((sg, i) => { sg.material = i < lit ? litMat : darkMat; }); faceMat.emissiveIntensity = clamp01(mw / 20) * 2.6; }
          led.set(s, t, lit ? HEX.green : HEX.amber, 2);
        },
      };
    },
  },
};
