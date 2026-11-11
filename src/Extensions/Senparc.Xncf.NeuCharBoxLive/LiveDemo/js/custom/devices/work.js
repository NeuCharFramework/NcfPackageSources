// The Work types' card UI and 3D models for the custom room (contract: devices/index.js): a 3D printer that builds a
// part while it prints, a robot arm that poses at home, pick and place, and a label printer that ejects a strip on every
// print. Each has its own status LED; standby is green, never the fault red.
import { HEX, num, clamp01, statusLed, canvasTex, ease, stepper } from './kit.js';

const whole = (v) => Math.round(num(v));
const cycle = (t, period, phase = 0) => (((t / period + phase) % 1) + 1) % 1;

export default {
  // A box-frame printer: a bed that slides front to back, an X carriage that climbs the two posts as the part grows, a
  // spool on top. The part is an orange vase that grows with the progress; the head and bed move only while printing,
  // and a row of ten lights on the base fills with the progress.
  printer_3d: {
    icon: 'printer3d', size: 0.5,
    edit: [{ field: 'printing', kind: 'switch' }, { field: 'progress', kind: 'slider', step: 1, unit: '%', label: 'progress', top: 'done' }],
    format: (s) => {
      const p = whole(s.progress);
      if (p >= 100) return 'print done';
      if (s.printing) return `printing · ${Math.max(0, p)}%`;
      return p > 0 ? `stopped at ${p}%` : 'idle';
    },
    active: (s) => !!s.printing,
    control: { kind: 'switch', field: 'printing' },
    model({ THREE, P, M }) {
      const BED_Y = 0.098, MAXH = 0.2;
      const base = P.box(0.4, 0.08, 0.38, M.black, 0, 0.04, 0, 0.012);
      const posts = [-0.17, 0.17].map((x) => P.box(0.03, 0.36, 0.04, M.black, x, 0.26, -0.15, 0.006));
      const topBar = P.box(0.37, 0.03, 0.05, M.black, 0, 0.455, -0.15, 0.006);
      const litMat = new THREE.MeshStandardMaterial({ color: 0x1B3B2C, emissive: 0x39E58B, emissiveIntensity: 1.8 });
      const darkMat = new THREE.MeshStandardMaterial({ color: 0x1E2326, roughness: 0.6 });
      const cells = [];
      for (let i = 0; i < 10; i++) { const cl = P.box(0.02, 0.012, 0.004, darkMat, (i - 4.5) * 0.026, 0.035, 0.1905); cl.castShadow = false; cells.push(cl); }
      // the bed and the part on it slide together
      const bedPlate = P.box(0.26, 0.012, 0.26, M.anodized, 0, 0, 0, 0.004);
      const filament = new THREE.MeshStandardMaterial({ color: 0xFF7A1C, roughness: 0.45 });
      const part = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.06, MAXH, 24), filament);
      part.castShadow = part.receiveShadow = true; part.position.y = 0.006; part.scale.y = 0.001;
      const bed = P.group(bedPlate, part); bed.position.set(0, BED_Y, 0);
      // the X beam, with the head hanging forward over the bed
      const beam = P.box(0.34, 0.022, 0.03, M.steel, 0, 0, 0, 0.004);
      const carriage = P.box(0.05, 0.05, 0.04, M.black, 0, 0, 0.03, 0.006);
      const arm = P.box(0.03, 0.026, 0.12, M.black, 0, -0.012, 0.09, 0.005);
      const nozzle = P.cyl(0.002, 0.009, 0.018, M.metal, 0, -0.036, 0.15, 12);
      const head = P.group(carriage, arm, nozzle);
      const gantry = P.group(beam, head); gantry.position.set(0, 0.2, -0.15);
      const spool = P.cyl(0.06, 0.06, 0.05, filament, 0, 0.53, -0.15, 24); spool.rotation.z = Math.PI / 2;
      const spoolHub = P.cyl(0.018, 0.018, 0.06, M.steel, 0, 0.53, -0.15, 12); spoolHub.rotation.z = Math.PI / 2;
      const notch = P.box(0.012, 0.004, 0.03, M.black, 0.04, 0.026, 0); spool.add(notch); notch.castShadow = false; // a mark, so the turning shows
      const root = P.group(base, ...posts, topBar, ...cells, bed, gantry, spool, spoolHub);
      const led = statusLed(THREE, root, { at: [0.17, 0.062, 0.1905], r: 0.006, size: 0.035 });
      const dt = stepper(); let act = 0, shown = -1;
      return {
        root,
        update(s, t) {
          const d = dt(t), p = clamp01(num(s.progress) / 100);
          const busy = !!s.printing && p < 1 && s.status !== 'offline' && s.status !== 'fault';
          act = ease(act, busy ? 1 : 0, d, 4);
          part.scale.y = Math.max(0.001, p); part.position.y = 0.006 + MAXH * p / 2;
          // the nozzle tip rides 4 mm over the part's top; the head parks at the top once it is done
          gantry.position.y = BED_Y + 0.006 + MAXH * p + 0.05 + (p >= 1 ? 0.04 : 0);
          head.position.x = 0.05 * Math.sin(t * 5.1) * act;
          bed.position.z = 0.04 * Math.sin(t * 2.3) * act;
          if (busy) { spool.rotation.x += d * 1.5; }
          const lit = Math.round(p * 10);
          if (lit !== shown) { shown = lit; cells.forEach((cl, i) => { cl.material = i < lit ? litMat : darkMat; }); }
          led.set(s, t, busy ? HEX.blue : HEX.green, busy ? 4 : 1.4);
        },
      };
    },
  },

  // A six-axis-style arm on a round base, with a pick pad in front (a small cube on it) and a place tray at its side.
  // It poses at home (folded up), pick (over the pad) and place (over the tray), turning the base and bending two joints to
  // get there. The cube rides in the gripper while it holds, and stays on the tray or the pad where it was let go.
  robot_arm: {
    icon: 'arm', size: 0.8,
    edit: [{ field: 'holding', kind: 'switch' }],
    format: (s) => `${s.pose === 'pick' ? 'at the pick spot' : s.pose === 'place' ? 'at the place spot' : 'at home'} · ${s.holding ? 'holding a part' : 'gripper empty'}`,
    active: (s) => !!s.holding,
    control: { kind: 'switch', field: 'holding' },
    model({ THREE, P, M }) {
      const HS = 0.14, L1 = 0.22, L2 = 0.2, LG = 0.08, REACH = 0.28;
      // joint angles from the vertical, measured toward the arm's front: link 1 (t1), link 2 (t2). Reach poses are solved so
      // the wrist sits LG over the target with the gripper pointing straight down.
      const reach = (r, tipY) => {
        const dx = r, dy = tipY + LG - HS, dist = Math.hypot(dx, dy);
        const a = Math.acos(Math.min(1, Math.max(-1, (L1 * L1 + dist * dist - L2 * L2) / (2 * L1 * dist))));
        const t1 = Math.atan2(dx, dy) - a;
        return { t1, t2: Math.atan2(dx - L1 * Math.sin(t1), dy - L1 * Math.cos(t1)) };
      };
      const POSES = { home: { yaw: 0, t1: 0.35, t2: 1.9 }, pick: { yaw: 0, ...reach(REACH, 0.02) }, place: { yaw: Math.PI / 2, ...reach(REACH, 0.036) } };
      const plate = P.cyl(0.13, 0.14, 0.02, M.black, 0, 0.01, 0, 32);
      const turntable = P.cyl(0.09, 0.1, 0.1, M.white, 0, 0.07, 0, 28);
      const pad = P.box(0.12, 0.008, 0.12, M.steel, 0, 0.004, REACH, 0.003);
      const tray = P.box(0.12, 0.02, 0.12, M.anodized, REACH, 0.01, 0, 0.004);
      const trayLip = P.box(0.1, 0.004, 0.1, M.black, REACH, 0.022, 0, 0.002);
      const link1 = P.box(0.05, L1, 0.05, M.white, 0, L1 / 2, 0, 0.01);
      const shoulderCap = P.cyl(0.032, 0.032, 0.06, M.black, 0, 0, 0, 18); shoulderCap.rotation.z = Math.PI / 2;
      const elbowCap = P.cyl(0.028, 0.028, 0.06, M.black, 0, 0, 0, 18); elbowCap.rotation.z = Math.PI / 2;
      const wristCap = P.cyl(0.02, 0.02, 0.05, M.black, 0, 0, 0, 16); wristCap.rotation.z = Math.PI / 2;
      const link2 = P.box(0.042, L2, 0.042, M.white, 0, L2 / 2, 0, 0.008);
      const palm = P.box(0.06, 0.02, 0.03, M.anodized, 0, 0.012, 0, 0.004);
      const fingerL = P.box(0.01, 0.05, 0.02, M.steel, 0, 0.05, 0, 0.002), fingerR = P.box(0.01, 0.05, 0.02, M.steel, 0, 0.05, 0, 0.002);
      const gripper = P.group(palm, fingerL, fingerR);
      const wrist = P.group(wristCap, gripper);
      const forearm = P.group(elbowCap, link2, wrist); wrist.position.y = L2;
      const upper = P.group(shoulderCap, link1, forearm); forearm.position.y = L1;
      const swing = P.group(upper); upper.position.y = 0; swing.position.set(0, HS, 0);
      const cubeMat = new THREE.MeshStandardMaterial({ color: 0xFF7A1C, roughness: 0.5 });
      const cube = P.box(0.05, 0.05, 0.05, cubeMat, 0, 0, 0, 0.004);
      cube.position.set(0, 0.033, REACH); // on the pad until the first update
      const root = P.group(plate, turntable, pad, tray, trayLip, swing, cube);
      const led = statusLed(THREE, root, { at: [0, 0.03, 0.14], r: 0.007, size: 0.035 });
      const dt = stepper();
      let yaw = null, t1 = 0, t2 = 0, grip = 0, holding = null, rest = 'pad';
      const place = (where) => { // let the cube rest on the pad or the tray
        if (cube.parent !== root) root.add(cube);
        cube.rotation.set(0, 0, 0);
        if (where === 'tray') cube.position.set(REACH, 0.047, 0); else cube.position.set(0, 0.033, REACH);
      };
      return {
        root,
        update(s, t) {
          const d = dt(t), want = POSES[s.pose] || POSES.home;
          if (yaw === null) { yaw = want.yaw; t1 = want.t1; t2 = want.t2; } // start at the state's pose, not folded from home
          yaw = ease(yaw, want.yaw, d, 2.6); t1 = ease(t1, want.t1, d, 2.6); t2 = ease(t2, want.t2, d, 2.6);
          const hold = !!s.holding;
          grip = ease(grip, hold ? 1 : 0, d, 10);
          swing.rotation.y = yaw; upper.rotation.x = t1; forearm.rotation.x = t2 - t1; wrist.rotation.x = Math.PI - t2;
          const gap = 0.034 - 0.022 * grip; fingerL.position.x = -gap; fingerR.position.x = gap;
          if (hold !== holding) {
            if (holding === true && !hold) rest = yaw > Math.PI / 4 ? 'tray' : 'pad'; // let go where the arm is
            holding = hold;
            if (hold) { gripper.add(cube); cube.position.set(0, 0.067, 0); cube.rotation.set(0, 0, 0); } else place(rest);
          }
          led.set(s, t, HEX.green, 2);
        },
      };
    },
  },

  // A desktop label printer on a small stand, with a label roll behind it. Each print (the count going up) slides a
  // barcode strip out of its front slot, holds it a moment and lets it drop; the light blinks amber while it prints.
  label_printer: {
    icon: 'labelprinter', size: 0.4,
    edit: [{ field: 'printed', kind: 'slider', step: 1, label: 'printed' }],
    format: (s) => { const n = Math.max(0, whole(s.printed)); return n === 0 ? 'no labels yet' : `${n} label${n === 1 ? '' : 's'} printed`; },
    control: false,
    model({ THREE, P, M }) {
      const stand = P.box(0.3, 0.36, 0.28, M.woodLight, 0, 0.18, 0, 0.01);
      const body = P.box(0.22, 0.09, 0.17, M.white, 0, 0.405, 0.01, 0.012);
      const lid = P.box(0.2, 0.012, 0.15, M.black, 0, 0.456, 0.01, 0.004);
      const slot = P.box(0.12, 0.012, 0.004, M.anodized, 0, 0.39, 0.096);
      const roll = P.cyl(0.06, 0.06, 0.1, new THREE.MeshStandardMaterial({ color: 0xE9EEF0, roughness: 0.7 }), 0, 0.5, -0.07, 24); roll.rotation.z = Math.PI / 2;
      const core = P.cyl(0.02, 0.02, 0.11, M.steel, 0, 0.5, -0.07, 12); core.rotation.z = Math.PI / 2;
      const stripTex = canvasTex(THREE, (g, w, h) => {
        g.fillStyle = '#F5F7F6'; g.fillRect(0, 0, w, h); g.fillStyle = '#1B1D20';
        for (let i = 0; i < 12; i++) g.fillRect(10 + i * 4 + (i % 3), 40, 2 + (i % 2), 18);
        g.fillRect(8, 12, 30, 5); g.fillRect(8, 22, 18, 4);
      }, 64, 64);
      stripTex.wrapS = stripTex.wrapT = THREE.ClampToEdgeWrapping;
      const stripMat = new THREE.MeshStandardMaterial({ map: stripTex, roughness: 0.8, side: THREE.DoubleSide });
      const strip = new THREE.Mesh(new THREE.PlaneGeometry(0.1, 0.1), stripMat);
      strip.rotation.x = -Math.PI / 2; strip.castShadow = true; strip.userData.noAO = true; strip.raycast = () => {}; strip.visible = false;
      const root = P.group(stand, body, lid, slot, roll, core, strip);
      const led = statusLed(THREE, root, { at: [0.09, 0.425, 0.0955], r: 0.005, size: 0.03 });
      const EJECT = 1.6; // seconds the strip is out
      let last = null, since = null;
      return {
        root,
        update(s, t) {
          const n = whole(s.printed);
          if (last !== null && n > last) since = t; // the count went up: print a label now
          last = n;
          const a = since == null ? EJECT : t - since, out = a < EJECT && s.status !== 'offline';
          strip.visible = out;
          if (out) {
            const slide = Math.min(1, a / 0.5), drop = Math.max(0, (a - 1.1) / 0.5);
            const len = 0.08 * slide;
            strip.scale.set(1, Math.max(0.01, len / 0.1), 1);
            strip.position.set(0, 0.392 - drop * 0.1, 0.098 + len / 2 + drop * 0.015);
            strip.rotation.x = -Math.PI / 2 + drop * 0.5;
            stripMat.opacity = 1;
          }
          led.set(s, t, out ? HEX.amber : HEX.green, out ? 4 : 1.5);
        },
      };
    },
  },
};
