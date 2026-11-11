// The Sensors, Energy and Outdoor types' card UI and 3D models for the custom room (contract: devices/index.js): a
// temperature and humidity sensor with a screen, a water-leak sensor beside a puddle, a ceiling presence sensor, a wall
// energy meter, an EV charger with a glowing cable and a sprinkler valve that sprays while open. Each has its own status
// LED; standby is green or a dim blue, never the fault red.
import { HEX, num, clamp01, oneDecimal, statusLed, canvasTex, ease, stepper, glowMesh as glow, screenTex } from './kit.js';

const whole = (v) => Math.round(num(v));
// A 0–100 reading as text: "saturated" at the top, never "100%".
const percent = (v, full = 'saturated') => { const r = whole(v); return r >= 100 ? full : `${Math.max(0, r)}%`; };
const cycle = (t, period, phase = 0) => (((t / period + phase) % 1) + 1) % 1;
const quiet = (m) => { m.userData.noAO = true; m.raycast = () => {}; m.castShadow = false; return m; };

const watts = (v) => { const w = Math.max(0, num(v)); return w >= 1000 ? `${(w / 1000).toFixed(1)} kW` : `${Math.round(w)} W`; };

export default {
  // A small wall sensor with a screen: the temperature large, the humidity under it. The number is blue when cold and warm
  // when hot; a fault turns the screen red and offline shows dashes.
  temp_humidity_sensor: {
    icon: 'thermohygro', size: 0.14, mountY: 1.5,
    edit: [{ field: 'temperature', kind: 'slider', step: 0.5, unit: '°C', label: 'temp' }, { field: 'humidity', kind: 'slider', step: 1, unit: '%', label: 'humidity', top: 'saturated' }],
    format: (s) => `${oneDecimal(s.temperature)}°C · humidity ${percent(s.humidity)}`,
    control: false,
    model({ THREE, P, M }) {
      const body = P.box(0.1, 0.1, 0.022, M.white, 0, 0, 0.011, 0.012);
      const { c, tex, g } = screenTex(THREE, 256, 256);
      const screen = P.screenPlane(0.072, 0.072, tex); screen.position.set(0, 0.004, 0.0225); screen.material.emissiveIntensity = 0.7;
      const root = P.group(body, screen);
      const led = statusLed(THREE, root, { at: [0, -0.04, 0.0235], r: 0.0035, size: 0.03 });
      const draw = (big, small, tone, alarm = false) => {
        g.fillStyle = alarm ? '#5A120D' : '#0B0F14'; g.fillRect(0, 0, c.width, c.height);
        g.textAlign = 'center'; g.textBaseline = 'middle';
        g.fillStyle = alarm ? '#FF7A62' : tone; g.font = '600 78px Inter, "Segoe UI", sans-serif'; g.fillText(big, 128, 92);
        g.fillStyle = alarm ? '#FFC2B6' : '#6FB3C2'; g.font = '600 54px Inter, "Segoe UI", sans-serif'; g.fillText(small, 128, 184);
        tex.needsUpdate = true;
      };
      let shown = null;
      return {
        root,
        update(s, t) {
          const tn = Math.round(num(s.temperature) * 10), hm = whole(s.humidity);
          const key = s.status === 'offline' ? 'off' : s.status === 'fault' ? 'fault' : `${tn}/${hm}`;
          if (key !== shown) {
            shown = key;
            if (key === 'off') draw('--', '--', '#9FE3F0');
            else if (key === 'fault') draw('!!', 'no reply', '#fff', true);
            else draw(`${oneDecimal(s.temperature)}°`, percent(s.humidity), tn < 180 ? '#8FD0FF' : tn > 270 ? '#FFC48A' : '#9FE3F0');
          }
          led.set(s, t, HEX.green, 2);
        },
      };
    },
  },

  // A flat puck on the floor with a cable to a two-pin probe pad. A leak spreads a puddle round the probe and the puck's
  // ring and LED turn amber; the puddle shrinks away again when it is dry.
  water_leak_sensor: {
    icon: 'leak', size: 0.3,
    edit: [{ field: 'leak', kind: 'switch' }],
    format: (s) => (s.leak ? 'leak detected' : 'dry'),
    active: (s) => !!s.leak,
    control: { kind: 'switch', field: 'leak' },
    model({ THREE, P, M }) {
      const puck = P.cyl(0.05, 0.055, 0.028, M.white, 0, 0.014, -0.07, 28);
      const cap = P.cyl(0.032, 0.032, 0.003, M.black, 0, 0.0295, -0.07, 24);
      const ring = glow(THREE, new THREE.TorusGeometry(0.047, 0.003, 8, 32), 0xDCE0DF, 0xFFB020);
      ring.rotation.x = Math.PI / 2; ring.position.set(0, 0.022, -0.07);
      const cable = P.tube([[0, 0.008, -0.02], [0.04, 0.005, 0.0], [-0.03, 0.005, 0.03], [0, 0.005, 0.07]], 0.0035);
      const pad = P.box(0.08, 0.008, 0.04, M.black, 0, 0.004, 0.08, 0.003);
      const pins = [-0.015, 0.015].map((x) => P.box(0.006, 0.006, 0.026, M.metal, x, 0.011, 0.08));
      const puddleMat = new THREE.MeshStandardMaterial({ color: 0x4FA8CF, roughness: 0.05, metalness: 0.1, transparent: true, opacity: 0, depthWrite: false });
      const puddle = quiet(new THREE.Mesh(new THREE.CircleGeometry(0.1, 28), puddleMat));
      puddle.rotation.x = -Math.PI / 2; puddle.position.set(0, 0.003, 0.04); puddle.visible = false;
      const ripple = quiet(new THREE.Mesh(new THREE.RingGeometry(0.9, 1, 32), new THREE.MeshBasicMaterial({ color: 0xBFE6F5, transparent: true, opacity: 0, depthWrite: false, side: THREE.DoubleSide })));
      ripple.rotation.x = -Math.PI / 2; ripple.position.set(0, 0.0045, 0.04); ripple.scale.setScalar(0.03); ripple.visible = false;
      const root = P.group(puck, cap, ring, cable, pad, ...pins, puddle, ripple);
      const led = statusLed(THREE, root, { at: [0, 0.0315, -0.07], r: 0.006, size: 0.035 });
      const dt = stepper(); let k = 0;
      return {
        root,
        update(s, t) {
          const wet = !!s.leak && s.status !== 'offline';
          k = ease(k, wet ? 1 : 0, dt(t), 1.6);
          puddle.visible = k > 0.01;
          puddle.scale.setScalar(0.15 + 0.85 * k + 0.02 * Math.sin(t * 2));
          puddleMat.opacity = 0.62 * k;
          const u = cycle(t, 2.2); ripple.visible = wet && k > 0.5;
          if (ripple.visible) { ripple.scale.setScalar(0.03 + 0.07 * u); ripple.material.opacity = 0.6 * (1 - u); }
          ring.material.emissiveIntensity = wet ? 2.5 + 1.5 * Math.sin(t * 6) : 0;
          led.set(s, t, wet ? HEX.amber : HEX.green, wet ? 4 : 1.6);
        },
      };
    },
  },

  // A ceiling dome (a radar presence sensor). While it senses someone, rings spread down from it and its ring glows blue;
  // the LED is a quiet green otherwise.
  presence_sensor: {
    icon: 'presence', size: 0.2,
    edit: [{ field: 'present', kind: 'switch' }],
    format: (s) => (s.present ? 'someone is here' : 'room empty'),
    active: (s) => !!s.present,
    control: { kind: 'switch', field: 'present' },
    model({ THREE, P, M }) {
      // dark smoked dome on a dark mount, so it never reads as the white smoke alarm
      const base = P.cyl(0.055, 0.055, 0.012, M.black, 0, -0.006, 0, 32);
      const dome = new THREE.Mesh(new THREE.SphereGeometry(0.045, 24, 10, 0, Math.PI * 2, Math.PI / 2, Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x1E252B, roughness: 0.18, metalness: 0.2 }));
      dome.position.y = -0.012; dome.castShadow = true;
      const ring = glow(THREE, new THREE.TorusGeometry(0.052, 0.0035, 8, 36), 0xDCE0DF, 0x3AB7FF);
      ring.rotation.x = Math.PI / 2; ring.position.y = -0.0125;
      const waves = [0, 1, 2].map(() => {
        const w = quiet(new THREE.Mesh(new THREE.RingGeometry(0.78, 1, 32), new THREE.MeshBasicMaterial({ color: 0x3AB7FF, transparent: true, opacity: 0, depthWrite: false, side: THREE.DoubleSide })));
        w.rotation.x = Math.PI / 2; w.position.y = -0.06; w.scale.setScalar(0.03); w.visible = false; return w;
      });
      const root = P.group(base, dome, ring, ...waves);
      const led = statusLed(THREE, root, { at: [0.034, -0.04, 0.0], r: 0.004, size: 0.03 });
      return {
        root,
        update(s, t) {
          const on = !!s.present && s.status !== 'offline';
          ring.material.emissiveIntensity = on ? 2.4 + 0.8 * Math.sin(t * 5) : 0;
          waves.forEach((w, i) => {
            w.visible = on; if (!on) return;
            const u = cycle(t, 1.8, i / 3);
            w.position.y = -0.06 - 0.3 * u; w.scale.setScalar(0.03 + 0.065 * u); w.material.opacity = 0.6 * Math.sin(Math.PI * u);
          });
          led.set(s, t, on ? HEX.blue : HEX.green, on ? 3 : 0.9 + 0.5 * Math.sin(t * 1.5));
        },
      };
    },
  },

  // A wall energy meter: the power draw large on its screen and the energy used today under it. A pulse LED blinks faster
  // the more power it reads.
  energy_meter: {
    icon: 'energy', size: 0.22, mountY: 1.4,
    edit: [{ field: 'watts', kind: 'slider', step: 10, unit: ' W', label: 'power' }],
    format: (s) => `${watts(s.watts)} · ${oneDecimal(s.kwh_today)} kWh today`,
    control: false,
    model({ THREE, P, M }) {
      const body = P.box(0.18, 0.24, 0.06, M.white, 0, 0, 0.03, 0.012);
      const face = P.box(0.15, 0.2, 0.006, M.black, 0, 0, 0.063, 0.008);
      const { c, tex, g } = screenTex(THREE, 320, 200);
      const screen = P.screenPlane(0.126, 0.079, tex); screen.position.set(0, 0.03, 0.0668); screen.material.emissiveIntensity = 0.7;
      const marks = [-0.04, -0.013, 0.013, 0.04].map((x) => P.box(0.018, 0.008, 0.003, M.steel, x, -0.065, 0.0665));
      const root = P.group(body, face, screen, ...marks);
      const led = statusLed(THREE, root, { at: [0.055, -0.092, 0.0665], r: 0.005, size: 0.03 });
      const draw = (big, small, alarm = false) => {
        g.fillStyle = alarm ? '#5A120D' : '#0B0F14'; g.fillRect(0, 0, c.width, c.height);
        g.textAlign = 'center'; g.textBaseline = 'middle';
        g.fillStyle = alarm ? '#FF7A62' : '#FFD27A'; g.font = '600 74px Inter, "Segoe UI", sans-serif'; g.fillText(big, 160, 78);
        g.fillStyle = alarm ? '#FFC2B6' : '#9FE3F0'; g.font = '600 38px Inter, "Segoe UI", sans-serif'; g.fillText(small, 160, 154);
        tex.needsUpdate = true;
      };
      let shown = null;
      return {
        root,
        update(s, t) {
          const w = Math.round(num(s.watts)), kwh = Math.round(num(s.kwh_today) * 10);
          const key = s.status === 'offline' ? 'off' : s.status === 'fault' ? 'fault' : `${w}/${kwh}`;
          if (key !== shown) {
            shown = key;
            if (key === 'off') draw('--', '');
            else if (key === 'fault') draw('!!', 'no reply', true);
            else draw(watts(s.watts), `${oneDecimal(s.kwh_today)} kWh today`);
          }
          const rate = Math.min(4, Math.max(0.3, num(s.watts) / 600)), on = cycle(t, 1 / rate) < 0.2;
          led.set(s, t, HEX.blue, on ? 5 : 0.5);
        },
      };
    },
  },

  // A wall charger: a white box with a row of five battery lights, a holster and a cable looping down to a plug. While
  // it charges the cable and the light bar glow green and pulse and the battery lights fill up with the charge.
  ev_charger: {
    icon: 'ev', size: 0.3, mountY: 1.2,
    edit: [{ field: 'charging', kind: 'switch' }, { field: 'battery', kind: 'slider', step: 1, unit: '%', label: 'battery' }],
    format: (s) => `${s.charging ? 'charging' : 'idle'} · battery ${percent(s.battery, 'full')}`,
    active: (s) => !!s.charging,
    control: { kind: 'switch', field: 'charging' },
    model({ THREE, P, M }) {
      const body = P.box(0.26, 0.42, 0.1, M.white, 0, 0, 0.05, 0.02);
      const bar = glow(THREE, new THREE.BoxGeometry(0.2, 0.012, 0.004), 0xDCE0DF, 0x2FE08A); bar.position.set(0, 0.17, 0.102);
      const litMat = new THREE.MeshStandardMaterial({ color: 0x1B3B2C, emissive: 0x39E58B, emissiveIntensity: 1.8 });
      const darkMat = new THREE.MeshStandardMaterial({ color: 0x2C3035, roughness: 0.6 });
      const cells = [];
      for (let i = 0; i < 5; i++) { const cl = P.box(0.03, 0.036, 0.004, darkMat, (i - 2) * 0.04, 0.07, 0.102); cl.castShadow = false; cells.push(cl); }
      const holster = P.box(0.09, 0.1, 0.05, M.black, 0, -0.1, 0.125, 0.012);
      const plug = P.cyl(0.024, 0.028, 0.11, M.anodized, 0, -0.095, 0.15, 16);
      const tip = P.cyl(0.03, 0.03, 0.02, M.steel, 0, -0.155, 0.15, 16);
      const cableMat = new THREE.MeshStandardMaterial({ color: 0x1F2A25, emissive: 0x2FE08A, emissiveIntensity: 0, roughness: 0.6 });
      const cable = P.tube([[0, -0.04, 0.16], [0.1, -0.08, 0.2], [0.13, -0.24, 0.17], [0.05, -0.37, 0.09], [0, -0.212, 0.06]], 0.011, cableMat);
      const root = P.group(body, bar, ...cells, holster, plug, tip, cable);
      const led = statusLed(THREE, root, { at: [0, 0.135, 0.102], r: 0.006, size: 0.035 });
      let shown = -1;
      return {
        root,
        update(s, t) {
          const on = !!s.charging && s.status !== 'offline';
          const lit = Math.round(clamp01(num(s.battery) / 100) * 5);
          if (lit !== shown) { shown = lit; cells.forEach((cl, i) => { cl.material = i < lit ? litMat : darkMat; }); }
          const pulse = on ? 0.75 + 0.25 * Math.sin(t * 4) : 0;
          cableMat.emissiveIntensity = on ? 1.2 + 1.8 * pulse : 0;
          bar.material.emissiveIntensity = on ? 2 + 2 * pulse : 0.25;
          bar.material.emissive.setHex(on ? 0x2FE08A : 0x6FB3C2);
          led.set(s, t, on ? HEX.green : HEX.blue, on ? 3 : 1);
        },
      };
    },
  },

  // A sprinkler valve (green box and a pipe) feeding a pop-up head on a small lawn. Open: the head rises, water arcs out
  // round it and the lawn darkens. Closed: the head sinks and the lawn dries.
  sprinkler_valve: {
    icon: 'sprinkler', size: 0.5,
    edit: [{ field: 'open', kind: 'switch' }],
    format: (s) => (s.open ? 'open · watering' : 'closed'),
    active: (s) => !!s.open,
    control: { kind: 'switch', field: 'open' },
    model({ THREE, P, M }) {
      const HX = 0.03;
      const lawnMat = new THREE.MeshStandardMaterial({ color: 0x6E9A4D, roughness: 1 });
      const lawn = P.cyl(0.22, 0.22, 0.012, lawnMat, HX, 0.006, 0, 36); lawn.castShadow = false;
      const valve = P.box(0.1, 0.09, 0.1, new THREE.MeshStandardMaterial({ color: 0x2F7A46, roughness: 0.6 }), -0.17, 0.057, -0.15, 0.012);
      const knob = P.cyl(0.022, 0.022, 0.03, M.black, -0.17, 0.117, -0.15, 16);
      const inlet = P.cyl(0.016, 0.016, 0.05, M.steel, -0.17, 0.06, -0.2, 14); inlet.rotation.x = Math.PI / 2;
      const pipe = P.tube([[-0.14, 0.03, -0.12], [-0.06, 0.022, -0.06], [HX, 0.022, 0.0]], 0.011, M.steel);
      const riser = P.cyl(0.018, 0.018, 0.07, M.black, HX, 0.047, 0, 16);
      const head = P.cyl(0.013, 0.013, 0.07, M.steel, HX, 0.035, 0, 14);
      const nozzle = P.box(0.034, 0.012, 0.012, M.black, HX, 0.075, 0.0);
      // the spray: one Points object, 6 arms x 4 drops
      const N = 24, pos = new Float32Array(N * 3), geo = new THREE.BufferGeometry();
      geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
      const dot = canvasTex(THREE, (g, w, h) => { g.clearRect(0, 0, w, h); g.fillStyle = '#fff'; g.beginPath(); g.arc(w / 2, h / 2, w / 2 - 1, 0, Math.PI * 2); g.fill(); }, 32, 32);
      const mist = new THREE.Points(geo, new THREE.PointsMaterial({ color: 0x9FD8F5, size: 0.016, map: dot, alphaTest: 0.3, transparent: true, opacity: 0.85, depthWrite: false }));
      mist.frustumCulled = false; mist.raycast = () => {}; mist.visible = false; mist.userData.noAO = true;
      const root = P.group(lawn, valve, knob, inlet, pipe, riser, head, nozzle, mist);
      const led = statusLed(THREE, root, { at: [-0.17, 0.103, -0.115], r: 0.006, size: 0.03 });
      const DRY = new THREE.Color(0x6E9A4D), WET = new THREE.Color(0x3E6B33);
      const dt = stepper(); let up = 0, wet = 0;
      return {
        root,
        update(s, t) {
          const d = dt(t), open = !!s.open && s.status !== 'offline';
          up = ease(up, open ? 1 : 0, d, 7);
          wet = ease(wet, open ? 1 : 0, d, 1);
          const hy = 0.035 + 0.07 * up; // the head's centre: 7 cm tall, sunk into the riser when closed
          head.position.y = hy; nozzle.position.y = hy + 0.04; nozzle.rotation.y = t * 2.2;
          lawnMat.color.lerpColors(DRY, WET, wet);
          mist.visible = up > 0.9;
          if (mist.visible) {
            const hs = hy + 0.05, spin = t * 0.9;
            for (let i = 0; i < N; i++) {
              const p = cycle(t, 1.1, Math.floor(i / 6) / 4), a = (i % 6) / 6 * Math.PI * 2 + spin;
              const y = hs + 0.3 * p - (0.3 + hs - 0.02) * p * p, r = 0.17 * p;
              pos[i * 3] = HX + Math.cos(a) * r; pos[i * 3 + 1] = Math.max(0.014, y); pos[i * 3 + 2] = Math.sin(a) * r;
            }
            geo.attributes.position.needsUpdate = true;
          }
          led.set(s, t, open ? HEX.blue : HEX.green, 2);
        },
      };
    },
  },
};
