// The Home and living types' card UI and 3D models for the custom room (contract: devices/index.js): a smart plug, a
// ceiling light, a colour bulb, a TV, a smart speaker, a robot vacuum, a ceiling fan, an air conditioner, a space heater,
// a humidifier and an air purifier. Each stands, hangs or mounts on its own with a status LED. What is on shows on the
// model: the TV's picture, the speaker's LED ring, the vacuum on its round, the fan's blades, air lines from the air
// conditioner and purifier, mist from the humidifier, the heater's glowing grille. The three lights carry real point
// lights (kit.pointLight); room3d.js deals the few light slots to the lit ones.
import { HEX, LIGHT_HEX, num, clamp01, pct, oneDecimal, statusLed, mergeMeshes, pointLight, ease, stepper, glowMesh as glow, screenTex, glowMap } from './kit.js';

const whole = (v) => Math.round(num(v));
const dimmer = (s) => { const r = pct(s.brightness); return r <= 0 ? 'off' : r >= 100 ? 'full' : `on · ${r}%`; };
// A 0–100 reading as text: "saturated" at the top, never "100%".
const percent = (v, full = 'saturated') => { const r = whole(v); return r >= 100 ? full : `${Math.max(0, r)}%`; };

// A flat, see-through, unlit shape (air lines, heat haze): own material so each can fade alone. Hidden until its model
// shows it, but parked at `y` (where the effect starts) so it never sits under the floor.
function ghost(THREE, w, h, hex, y = 0) {
  const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshBasicMaterial({ color: hex, transparent: true, opacity: 0, depthWrite: false }));
  m.position.y = y; m.userData.noAO = true; m.raycast = () => {}; m.visible = false;
  return m;
}
// 0 → 1 over `period` seconds, offset by `phase` (0–1): for looping effects.
const cycle = (t, period, phase = 0) => (((t / period + phase) % 1) + 1) % 1;

export default {
  // A smart plug on a low wall socket: a white body with a ring that glows blue while on.
  smart_plug: {
    icon: 'plug', size: 0.2, mountY: 0.4,
    edit: [{ field: 'on', kind: 'switch' }],
    format: (s) => { const w = whole(s.watts); return s.on ? (w > 0 ? `on · ${w} W` : 'on') : 'off'; },
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const plate = P.box(0.13, 0.13, 0.012, M.white, 0, 0, 0.006, 0.006);
      const body = P.box(0.075, 0.1, 0.045, M.white, 0, 0, 0.0345, 0.012);
      const ringMat = new THREE.MeshStandardMaterial({ color: 0x1B1D20, emissive: 0x3AB7FF, emissiveIntensity: 0, roughness: 0.4 });
      const ring = new THREE.Mesh(new THREE.TorusGeometry(0.022, 0.004, 8, 24), ringMat); ring.position.set(0, 0.012, 0.0575);
      const button = P.cyl(0.013, 0.013, 0.004, M.black, 0, 0.012, 0.0565, 16); button.rotation.x = Math.PI / 2;
      const root = P.group(plate, body, ring, button);
      const led = statusLed(THREE, root, { at: [0, -0.034, 0.0575], r: 0.0045 });
      return {
        root,
        update(s, t) {
          ringMat.emissiveIntensity = s.on ? 2.6 : 0;
          led.set(s, t, s.on ? HEX.blue : HEX.green, s.on ? 4 : 1.5);
        },
      };
    },
  },

  // A pendant ceiling light: a drum shade on a short stem, its underside a diffuser that glows with the dimmer.
  ceiling_light: {
    icon: 'bulb', size: 0.6,
    edit: [{ field: 'brightness', kind: 'slider', step: 0.01, scale: 100, unit: '%' }],
    format: dimmer,
    active: (s) => pct(s.brightness) > 0,
    control: { kind: 'bar', field: 'brightness', min: 0, max: 1 },
    model({ THREE, P, M }) {
      const canopy = P.cyl(0.07, 0.08, 0.03, M.white, 0, -0.015, 0, 20), stem = P.cyl(0.006, 0.006, 0.22, M.black, 0, -0.14, 0, 8);
      const shadeMat = new THREE.MeshStandardMaterial({ color: 0xF2EBDD, emissive: 0xFFE2B8, emissiveIntensity: 0, roughness: 0.6, side: THREE.DoubleSide });
      const shade = new THREE.Mesh(new THREE.CylinderGeometry(0.17, 0.27, 0.15, 32, 1, true), shadeMat); shade.position.y = -0.31;
      const disc = glow(THREE, new THREE.CircleGeometry(0.265, 32), 0xF6EEDD, 0xFFE2B8); disc.rotation.x = Math.PI / 2; disc.position.y = -0.385; disc.material.side = THREE.DoubleSide;
      const rim = P.cyl(0.275, 0.275, 0.012, M.white, 0, -0.383, 0, 32);
      const root = P.group(canopy, stem, shade, disc, rim);
      const led = statusLed(THREE, root, { at: [0.2, -0.392, 0], r: 0.006 });
      const light = pointLight(THREE, root, [0, -0.5, 0], { hex: 0xFFE2B8 });
      let shown = -1;
      return {
        root,
        update(s, t) {
          const b = clamp01(s.brightness);
          if (b !== shown) { shown = b; disc.material.emissiveIntensity = b * 3; shadeMat.emissiveIntensity = b * 0.5; light.intensity = b * 2.2; }
          led.set(s, t);
        },
      };
    },
  },

  // A smart bulb on a small floor stand. It shows its colour, glows with the dimmer and lights the room in that colour.
  colour_bulb: {
    icon: 'bulb', size: 0.4,
    edit: [{ field: 'brightness', kind: 'slider', step: 0.01, scale: 100, unit: '%' }],
    format: (s) => { const r = pct(s.brightness); return r <= 0 ? 'off' : `${s.color} · ${r >= 100 ? 'full' : `${r}%`}`; },
    active: (s) => pct(s.brightness) > 0,
    control: { kind: 'bar', field: 'brightness', min: 0, max: 1 },
    model({ THREE, P, M }) {
      const base = P.cyl(0.12, 0.14, 0.03, M.black, 0, 0.015, 0, 24), post = P.cyl(0.012, 0.012, 0.32, M.black, 0, 0.19, 0, 10);
      const socket = P.cyl(0.032, 0.026, 0.05, M.steel, 0, 0.375, 0, 14);
      const bulb = glow(THREE, new THREE.SphereGeometry(0.085, 20, 14), 0xF3F0EA, 0xFFB46B); bulb.position.y = 0.48; bulb.material.roughness = 0.25;
      const root = P.group(base, post, socket, bulb);
      const led = statusLed(THREE, root, { at: [0, 0.031, 0.1], r: 0.006 });
      const light = pointLight(THREE, root, [0, 0.5, 0], { hex: LIGHT_HEX.warm });
      let shown = '';
      return {
        root,
        update(s, t) {
          const b = clamp01(s.brightness), key = `${b.toFixed(3)}|${s.color}`;
          if (key !== shown) {
            shown = key; const hex = LIGHT_HEX[s.color] ?? LIGHT_HEX.warm;
            bulb.material.emissive.setHex(hex); bulb.material.emissiveIntensity = b * 3.2; light.color.setHex(hex); light.intensity = b * 1.8;
            bulb.material.color.setHex(b > 0 ? 0xF3F0EA : 0xE4E2DD);
          }
          led.set(s, t);
        },
      };
    },
  },

  // A flat-screen TV on the wall. The screen shows a picture and a volume bar while on, and is dark glass while off; a
  // fault dims the picture under a red "no reply" pill, offline is a dead screen.
  tv: {
    icon: 'tv', size: 1.2, mountY: 1.3,
    edit: [{ field: 'on', kind: 'switch' }, { field: 'volume', kind: 'slider', step: 1, label: 'volume' }],
    format: (s) => (s.on ? `on · volume ${whole(s.volume)}` : 'off'),
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const W = 1.1, H = 0.64;
      const frame = P.box(W, H, 0.04, M.black, 0, 0, 0.02, 0.012);
      const { tex, g } = screenTex(THREE, 320, 180);
      const screen = P.screenPlane(W - 0.05, H - 0.055, tex); screen.position.set(0, 0, 0.0405); screen.material.roughness = 0.2;
      const root = P.group(frame, screen);
      const led = statusLed(THREE, root, { at: [W / 2 - 0.05, -H / 2 + 0.0135, 0.0405], r: 0.004 });
      const draw = (on, vol, status) => {
        const dead = status === 'offline';
        g.fillStyle = '#06080A'; g.fillRect(0, 0, 320, 180);
        if (!on || dead) { // dark glass with a faint reflection
          const gr = g.createLinearGradient(0, 0, 320, 180); gr.addColorStop(0, 'rgba(255,255,255,.07)'); gr.addColorStop(0.5, 'rgba(255,255,255,0)'); g.fillStyle = gr; g.fillRect(0, 0, 320, 180);
        } else {
          const sky = g.createLinearGradient(0, 0, 0, 180); sky.addColorStop(0, '#2E6FA8'); sky.addColorStop(0.65, '#F0B27A'); sky.addColorStop(1, '#F6D9A8'); g.fillStyle = sky; g.fillRect(0, 0, 320, 180);
          g.fillStyle = '#FFE9B0'; g.beginPath(); g.arc(236, 92, 20, 0, Math.PI * 2); g.fill();
          g.fillStyle = '#2F5E57'; g.beginPath(); g.moveTo(0, 180); g.lineTo(0, 120); g.quadraticCurveTo(80, 84, 150, 122); g.quadraticCurveTo(220, 96, 320, 130); g.lineTo(320, 180); g.fill();
          g.fillStyle = '#1F3F3A'; g.beginPath(); g.moveTo(0, 180); g.lineTo(0, 150); g.quadraticCurveTo(110, 124, 210, 154); g.quadraticCurveTo(270, 140, 320, 152); g.lineTo(320, 180); g.fill();
          g.fillStyle = 'rgba(0,0,0,.45)'; g.beginPath(); g.roundRect(12, 144, 148, 24, 12); g.fill(); // volume bar
          g.fillStyle = 'rgba(255,255,255,.3)'; g.beginPath(); g.roundRect(48, 153, 100, 6, 3); g.fill();
          g.fillStyle = '#fff'; g.beginPath(); g.roundRect(48, 153, Math.max(6, 100 * clamp01(vol / 100)), 6, 3); g.fill();
          g.fillStyle = '#fff'; g.beginPath(); g.moveTo(22, 153); g.lineTo(28, 153); g.lineTo(34, 148); g.lineTo(34, 164); g.lineTo(28, 159); g.lineTo(22, 159); g.fill();
          if (status === 'fault') { g.fillStyle = 'rgba(0,0,0,.5)'; g.fillRect(0, 0, 320, 180); g.fillStyle = '#E0563A'; g.beginPath(); g.roundRect(100, 8, 120, 26, 13); g.fill(); g.fillStyle = '#fff'; g.font = '600 16px Inter, "Segoe UI", sans-serif'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('no reply', 160, 21); }
        }
        tex.needsUpdate = true;
      };
      let shown = '';
      return {
        root,
        update(s, t) {
          const lit = !!s.on && s.status !== 'offline', key = `${lit}|${s.status}|${whole(s.volume)}`;
          if (key !== shown) { shown = key; draw(!!s.on, num(s.volume), s.status); screen.material.emissiveIntensity = lit ? 0.9 : 0.05; }
          led.set(s, t, s.on ? HEX.green : HEX.amber, s.on ? 2 : 0.7); // standby is a dim amber, never the fault red
        },
      };
    },
  },

  // A cylinder speaker in dark fabric with a ring of light round its top: off while stopped, pulsing blue while playing,
  // brighter and faster with the volume.
  smart_speaker: {
    icon: 'speaker', size: 0.3,
    edit: [{ field: 'playing', kind: 'switch' }, { field: 'volume', kind: 'slider', step: 1, label: 'volume' }],
    format: (s) => (s.playing ? `playing · volume ${whole(s.volume)}` : 'stopped'),
    active: (s) => !!s.playing,
    control: { kind: 'switch', field: 'playing' },
    model({ THREE, P, M }) {
      const fabric = new THREE.MeshStandardMaterial({ color: 0x3A3F45, roughness: 0.95 });
      const body = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.105, 0.46, 28), fabric); body.position.y = 0.25; body.castShadow = body.receiveShadow = true;
      const foot = P.cyl(0.105, 0.105, 0.02, M.black, 0, 0.01, 0, 28), cap = P.cyl(0.1, 0.1, 0.02, M.black, 0, 0.49, 0, 28);
      const ringMat = new THREE.MeshStandardMaterial({ color: 0x1B1D20, emissive: 0x3AB7FF, emissiveIntensity: 0, roughness: 0.4 });
      const ring = new THREE.Mesh(new THREE.TorusGeometry(0.075, 0.005, 8, 36), ringMat); ring.rotation.x = Math.PI / 2; ring.position.y = 0.5;
      const root = P.group(foot, body, cap, ring);
      const led = statusLed(THREE, root, { at: [0, 0.05, 0.105], r: 0.005 });
      return {
        root,
        update(s, t) {
          const v = clamp01(num(s.volume) / 100);
          ringMat.emissiveIntensity = s.playing ? (1.6 + 1.8 * v) * (0.75 + 0.25 * Math.sin(t * (4 + 6 * v))) : 0;
          led.set(s, t, s.playing ? HEX.blue : HEX.green, 2);
        },
      };
    },
  },

  // A round robot vacuum with its charging dock. While cleaning it drives a small circle and its brush spins; stopped, it
  // glides back to the dock.
  robot_vacuum: {
    icon: 'vacuum', size: 1.1,
    edit: [{ field: 'cleaning', kind: 'switch' }, { field: 'battery', kind: 'slider', step: 1, unit: '%', label: 'battery' }],
    format: (s) => { const b = whole(s.battery); return `${s.cleaning ? 'cleaning' : 'docked'} · battery ${b >= 100 ? 'full' : b <= 0 ? 'empty' : `${b}%`}`; },
    active: (s) => !!s.cleaning,
    control: { kind: 'switch', field: 'cleaning' },
    model({ THREE, P, M }) {
      const R = 0.28, DOCK = [-R, 0]; // the round's radius; the resting spot is the round's far-left point
      const dock = P.box(0.09, 0.11, 0.2, M.black, -0.505, 0.055, 0, 0.012), dockLed = P.box(0.004, 0.01, 0.04, M.white, -0.459, 0.085, 0);
      const bodyMat = new THREE.MeshStandardMaterial({ color: 0xE9EBEC, roughness: 0.35, metalness: 0.1 });
      const body = new THREE.Mesh(new THREE.CylinderGeometry(0.17, 0.17, 0.075, 32), bodyMat); body.position.y = 0.0475; body.castShadow = body.receiveShadow = true;
      const top = P.cyl(0.135, 0.135, 0.006, M.black, 0, 0.0865, 0, 28), lidar = P.cyl(0.038, 0.038, 0.026, M.black, 0, 0.1, -0.03, 16);
      const bumper = new THREE.Mesh(new THREE.TorusGeometry(0.17, 0.007, 6, 32, Math.PI), M.black); bumper.rotation.x = Math.PI / 2; bumper.position.y = 0.05; // the front (+z) half ring
      const brush = P.box(0.07, 0.004, 0.012, M.white, 0, 0.004, 0); const brushHub = P.group(brush); brushHub.position.set(0.1, 0.01, 0.12);
      const bot = P.group(body, top, lidar, bumper, brushHub);
      const root = P.group(dock, dockLed, bot);
      const led = statusLed(THREE, bot, { at: [0, 0.092, 0.09], r: 0.007 });
      const step = stepper();
      let k = 0, ang = Math.PI; // how far into its round (0 docked, 1 driving it), and where on it
      return {
        root,
        update(s, t) {
          const dt = step(t), want = s.cleaning && s.status !== 'offline' && s.status !== 'fault' ? 1 : 0;
          k = ease(k, want, dt, 2.5);
          if (k > 0) ang += dt * 1.3 * Math.max(k, 0.3); else ang = Math.PI;
          const cx = R * Math.cos(ang), cz = R * Math.sin(ang);
          bot.position.set(DOCK[0] + (cx - DOCK[0]) * k, 0, DOCK[1] + (cz - DOCK[1]) * k);
          bot.rotation.y = Math.atan2(-Math.sin(ang), Math.cos(ang));
          if (k > 0.05) brushHub.rotation.y += dt * 14;
          led.set(s, t, s.cleaning ? HEX.blue : HEX.green, 2);
        },
      };
    },
  },

  // A ceiling fan: four wooden blades round a motor, turning at a rate set by its speed (0–3) and easing up and down.
  ceiling_fan: {
    icon: 'fan', size: 1.1,
    edit: [{ field: 'speed', kind: 'slider', step: 1, label: 'speed' }],
    format: (s) => { const n = whole(s.speed); return n <= 0 ? 'off' : `speed ${n} of 3`; },
    active: (s) => whole(s.speed) > 0,
    control: { kind: 'bar', field: 'speed', min: 0, max: 3 },
    model({ THREE, P, M }) {
      const canopy = P.cyl(0.06, 0.07, 0.035, M.white, 0, -0.0175, 0, 20), rod = P.cyl(0.011, 0.011, 0.24, M.black, 0, -0.15, 0, 8);
      const motor = P.cyl(0.1, 0.11, 0.09, M.black, 0, -0.31, 0, 24), cap = P.cyl(0.07, 0.07, 0.03, M.white, 0, -0.38, 0, 20);
      const blades = [];
      for (let i = 0; i < 4; i++) {
        const blade = P.box(0.42, 0.008, 0.11, M.wood, 0.31, 0, 0, 0.003); blade.rotation.x = 0.16; // pitched, so it reads as a blade
        const arm = P.box(0.07, 0.006, 0.03, M.black, 0.135, 0.004, 0);
        const pivot = P.group(blade, arm); pivot.rotation.y = (i * Math.PI) / 2; blades.push(pivot);
      }
      const rotor = P.group(...blades); rotor.position.y = -0.35;
      const root = P.group(canopy, rod, motor, cap, rotor);
      const led = statusLed(THREE, root, { at: [0.113, -0.345, 0], r: 0.007 }); // on the motor's rim, outside it
      const step = stepper(); let w = 0; // rad/s now
      return {
        root,
        update(s, t) {
          const dt = step(t), want = s.status === 'offline' ? 0 : Math.max(0, Math.min(3, num(s.speed))) * 3.4;
          w = ease(w, want, dt, 1.6); rotor.rotation.y += w * dt;
          led.set(s, t, num(s.speed) > 0 ? HEX.blue : HEX.green, 2);
        },
      };
    },
  },

  // A wall-mounted split air conditioner: a white unit with a display showing the set point and a flap that opens, with
  // pale air lines falling from it, while on.
  air_conditioner: {
    icon: 'ac', size: 1.0, mountY: 2.15,
    edit: [{ field: 'on', kind: 'switch' }, { field: 'room', kind: 'slider', step: 0.5, unit: '°C', label: 'now' }],
    format: (s) => (s.on ? `on · ${oneDecimal(s.target)}°C set · ${oneDecimal(s.room)}°C now` : `off · ${oneDecimal(s.room)}°C now`),
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const body = P.box(0.9, 0.27, 0.2, M.white, 0, 0, 0.1, 0.05);
      const slot = P.box(0.8, 0.012, 0.004, M.black, 0, 0.07, 0.2, 0.002);
      const flap = P.box(0.8, 0.012, 0.075, M.white, 0, 0, 0.0375, 0.003), hinge = P.group(flap); hinge.position.set(0, -0.125, 0.19);
      const { tex, g } = screenTex(THREE, 96, 40);
      const face = P.screenPlane(0.085, 0.035, tex); face.position.set(0.31, 0.03, 0.2005); face.material.emissiveIntensity = 0;
      const root = P.group(body, slot, hinge, face);
      const led = statusLed(THREE, root, { at: [0.375, -0.03, 0.2005], r: 0.0055 });
      const lines = [];
      for (let i = 0; i < 6; i++) { const m = ghost(THREE, 0.014, 0.2, 0xBFE6FF, -0.2); root.add(m); lines.push({ m, x: -0.33 + i * 0.132, ph: i / 6 }); }
      const draw = (txt, on) => { g.fillStyle = '#0B0F14'; g.fillRect(0, 0, 96, 40); g.fillStyle = on ? '#9FE3F0' : '#39424A'; g.font = '600 26px Inter, "Segoe UI", sans-serif'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(txt, 48, 21); tex.needsUpdate = true; };
      const step = stepper(); let shown = '', open = 0;
      return {
        root,
        update(s, t) {
          const dt = step(t), on = !!s.on && s.status !== 'offline', key = `${on}|${s.status}|${Math.round(num(s.target) * 10)}`;
          if (key !== shown) { shown = key; draw(on ? `${oneDecimal(s.target)}°` : '--', on); face.material.emissiveIntensity = on ? 0.9 : 0; }
          open = ease(open, on ? 1 : 0, dt, 6); hinge.rotation.x = 0.62 * open;
          for (const l of lines) {
            l.m.visible = on;
            if (!on) continue;
            const u = cycle(t, 1.6, l.ph);
            l.m.position.set(l.x, -0.19 - 0.3 * u, 0.2 + 0.1 + 0.16 * u); l.m.material.opacity = 0.34 * Math.sin(Math.PI * u);
          }
          led.set(s, t, on ? HEX.blue : HEX.green, 2);
        },
      };
    },
  },

  // A panel heater on short feet: slats over a grille that glows orange while on, with a little heat haze rising.
  space_heater: {
    icon: 'heater', size: 0.5,
    edit: [{ field: 'on', kind: 'switch' }, { field: 'room', kind: 'slider', step: 0.5, unit: '°C', label: 'now' }],
    format: (s) => `${s.on ? 'on' : 'off'} · ${oneDecimal(s.room)}°C now`,
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const body = P.box(0.36, 0.5, 0.12, M.white, 0, 0.32, 0, 0.025);
      const feet = [-0.13, 0.13].map((x) => P.box(0.05, 0.07, 0.2, M.black, x, 0.035, 0, 0.01));
      const grilleMat = new THREE.MeshStandardMaterial({ color: 0x3A2420, emissive: 0xFF6A2A, emissiveIntensity: 0, roughness: 0.6 });
      const grille = new THREE.Mesh(new THREE.PlaneGeometry(0.28, 0.34), grilleMat); grille.position.set(0, 0.34, 0.0615);
      const slats = mergeMeshes(THREE, [0, 1, 2, 3, 4, 5, 6].map((i) => P.box(0.29, 0.012, 0.012, M.black, 0, 0.19 + i * 0.05, 0.068)), M.black, { consume: true });
      const dial = P.cyl(0.022, 0.022, 0.012, M.black, 0.12, 0.56, 0.03, 14);
      const root = P.group(body, ...feet, grille, slats, dial);
      const led = statusLed(THREE, root, { at: [-0.12, 0.557, 0.0615], r: 0.0055 });
      const waves = [];
      for (let i = 0; i < 3; i++) { const m = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowMap(THREE), color: 0xFFB070, transparent: true, depthWrite: false, opacity: 0 })); m.position.y = 0.65; m.visible = false; m.userData.noAO = true; m.raycast = () => {}; root.add(m); waves.push({ m, x: (i - 1) * 0.1, ph: i / 3 }); } // soft round glows, not flat planes
      return {
        root,
        update(s, t) {
          const on = !!s.on && s.status !== 'offline';
          grilleMat.emissiveIntensity = on ? 1.7 + 0.25 * Math.sin(t * 3) : 0;
          for (const w of waves) {
            w.m.visible = on; if (!on) continue;
            const u = cycle(t, 2.2, w.ph);
            w.m.position.set(w.x + 0.02 * Math.sin(t * 2 + w.ph * 6), 0.62 + 0.35 * u, 0.07); w.m.scale.setScalar(0.12 + 0.1 * u); w.m.material.opacity = 0.22 * Math.sin(Math.PI * u);
          }
          led.set(s, t, on ? HEX.amber : HEX.green, on ? 4 : 1.5);
        },
      };
    },
  },

  // A tank humidifier: a clear tank of water on a base, a ring of light round the base and mist puffing from the nozzle
  // on top while on.
  humidifier: {
    icon: 'humidifier', size: 0.4,
    edit: [{ field: 'on', kind: 'switch' }, { field: 'humidity', kind: 'slider', step: 1, unit: '%', label: 'humidity', top: 'saturated' }],
    format: (s) => `${s.on ? 'on' : 'off'} · humidity ${percent(s.humidity)}`,
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const base = P.cyl(0.15, 0.16, 0.1, M.white, 0, 0.05, 0, 28);
      const tank = P.cyl(0.13, 0.13, 0.3, new THREE.MeshStandardMaterial({ color: 0xE8F4F8, roughness: 0.08, transparent: true, opacity: 0.3, depthWrite: false }), 0, 0.25, 0, 28);
      tank.castShadow = false; tank.userData.noAO = true; tank.renderOrder = 1;
      const water = P.cyl(0.12, 0.12, 0.2, new THREE.MeshStandardMaterial({ color: 0x4FA8CF, roughness: 0.2, transparent: true, opacity: 0.7 }), 0, 0.2, 0, 28);
      const cap = P.cyl(0.09, 0.13, 0.06, M.white, 0, 0.43, 0, 28), nozzle = P.cyl(0.03, 0.03, 0.05, M.white, 0, 0.485, 0, 14);
      const ringMat = new THREE.MeshStandardMaterial({ color: 0x1B1D20, emissive: 0x7FD8FF, emissiveIntensity: 0, roughness: 0.4 });
      const ring = new THREE.Mesh(new THREE.TorusGeometry(0.157, 0.004, 8, 40), ringMat); ring.rotation.x = Math.PI / 2; ring.position.y = 0.085;
      const root = P.group(base, tank, water, cap, nozzle, ring);
      const led = statusLed(THREE, root, { at: [0, 0.05, 0.16], r: 0.006 });
      const puffs = [];
      for (let i = 0; i < 6; i++) {
        const m = new THREE.Mesh(new THREE.SphereGeometry(0.04, 8, 6), new THREE.MeshBasicMaterial({ color: 0xEAF6FB, transparent: true, opacity: 0, depthWrite: false }));
        m.position.y = 0.55; m.userData.noAO = true; m.raycast = () => {}; m.visible = false; root.add(m); puffs.push({ m, ph: i / 6, a: i * 2.1 });
      }
      return {
        root,
        update(s, t) {
          const on = !!s.on && s.status !== 'offline';
          ringMat.emissiveIntensity = on ? 2.2 : 0;
          for (const p of puffs) {
            p.m.visible = on; if (!on) continue;
            const u = cycle(t, 2.4, p.ph);
            p.m.position.set(Math.cos(p.a) * 0.05 * u, 0.53 + 0.4 * u, Math.sin(p.a) * 0.05 * u); p.m.scale.setScalar(0.6 + 1.5 * u); p.m.material.opacity = 0.3 * Math.sin(Math.PI * u);
          }
          led.set(s, t, on ? HEX.blue : HEX.green, on ? 4 : 1.5);
        },
      };
    },
  },

  // A tower air purifier with a light bar down its front (green when the air is clean, amber then red as the fine dust
  // rises) and faint air lines rising from the top while on.
  air_purifier: {
    icon: 'purifier', size: 0.4,
    edit: [{ field: 'on', kind: 'switch' }, { field: 'pm25', kind: 'slider', step: 1, unit: ' µg/m³', label: 'fine dust' }],
    format: (s) => `${s.on ? 'on' : 'off'} · fine dust ${whole(s.pm25)} µg/m³`,
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const body = P.box(0.26, 0.62, 0.26, M.white, 0, 0.31, 0, 0.04);
      const top = P.box(0.2, 0.012, 0.2, M.black, 0, 0.626, 0, 0.004);
      const mesh = P.box(0.2, 0.34, 0.004, M.black, 0, 0.27, 0.1315, 0.002);
      const barMat = new THREE.MeshStandardMaterial({ color: 0x1B1D20, emissive: 0x2FBF71, emissiveIntensity: 0, roughness: 0.4 });
      const bar = new THREE.Mesh(new THREE.BoxGeometry(0.014, 0.32, 0.004), barMat); bar.position.set(0, 0.27, 0.1345);
      const root = P.group(body, top, mesh, bar);
      const led = statusLed(THREE, root, { at: [0.095, 0.575, 0.1315], r: 0.0055 });
      const lines = [];
      for (let i = 0; i < 5; i++) { const m = ghost(THREE, 0.012, 0.16, 0xBFE6FF, 0.75); root.add(m); lines.push({ m, a: i * 1.26, ph: i / 5 }); }
      const GOOD = new THREE.Color(0x2FBF71), FAIR = new THREE.Color(0xFFB020), POOR = new THREE.Color(0xE0563A), tmp = new THREE.Color();
      let shown = '';
      return {
        root,
        update(s, t) {
          const on = !!s.on && s.status !== 'offline', dust = num(s.pm25), key = `${on}|${Math.round(dust)}`;
          if (key !== shown) {
            shown = key; tmp.copy(dust <= 12 ? GOOD : dust <= 35 ? FAIR : POOR); barMat.emissive.copy(tmp); barMat.emissiveIntensity = on ? 2.2 : 0;
          }
          for (const l of lines) {
            l.m.visible = on; if (!on) continue;
            const u = cycle(t, 1.8, l.ph);
            l.m.position.set(Math.cos(l.a) * 0.07, 0.7 + 0.3 * u, Math.sin(l.a) * 0.07); l.m.material.opacity = 0.34 * Math.sin(Math.PI * u);
          }
          led.set(s, t, on ? HEX.blue : HEX.green, on ? 4 : 1.5);
        },
      };
    },
  },
};
