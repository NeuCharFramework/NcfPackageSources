// The Creator desk types' card UI and 3D models for the custom room (contract: devices/index.js). Their look follows the
// creator scene (js/scenes/creator.js): a softbox on a stand, a camera on a tripod, a boom mic, a capture card, the
// overlay app on a monitor, the on-air sign and an LED strip. Each stands, hangs or mounts on its own with a status LED.
import { HEX, num, clamp01, pct, statusLed, pointLight, screenTex } from './kit.js';

const STRIP = { white: 0xFFFFFF, warm: 0xFFB46B, blue: 0x3A8CFF, purple: 0x8A5CFF, red: 0xFF3A3A, green: 0x3AE07A };
const level = (s) => { const r = pct(s.brightness); return r <= 0 ? 'off' : r >= 100 ? 'full' : `${r}%`; };


export default {
  // A softbox on a stand. The panel glows with the brightness, warmer at 2700 K and cooler at 6500 K.
  key_light: {
    icon: 'keylight', size: 0.6,
    edit: [{ field: 'brightness', kind: 'slider', step: 0.01, scale: 100, unit: '%' }],
    format: (s) => { const r = pct(s.brightness); return r <= 0 ? 'off' : `${r >= 100 ? 'full' : `${r}%`} · ${Math.round(num(s.kelvin) / 100) * 100}K`; },
    active: (s) => pct(s.brightness) > 0,
    control: { kind: 'bar', field: 'brightness', min: 0, max: 1 },
    model({ THREE, P, M }) {
      const base = P.cyl(0.2, 0.22, 0.03, M.black, 0, 0.015, 0, 24), pole = P.cyl(0.015, 0.015, 1.5, M.black, 0, 0.78, -0.04, 10);
      const arm = P.cyl(0.012, 0.012, 0.12, M.black, 0, 1.55, 0.0, 10); arm.rotation.x = Math.PI / 2;
      const box = P.box(0.5, 0.5, 0.06, M.black, 0, 1.62, 0.06, 0.01); box.rotation.x = 0.2;
      const panelMat = new THREE.MeshStandardMaterial({ color: 0x8C8C8C, emissive: 0xFFF6E8, emissiveIntensity: 0, roughness: 0.6 });
      const panel = new THREE.Mesh(new THREE.PlaneGeometry(0.46, 0.46), panelMat); panel.position.z = 0.031; box.add(panel);
      const ctl = P.box(0.045, 0.07, 0.035, M.white, 0, 1.1, 0.0, 0.004);
      const root = P.group(base, pole, arm, box, ctl);
      const led = statusLed(THREE, root, { at: [0, 1.118, 0.019], r: 0.007 });
      const light = pointLight(THREE, root, [0, 1.6, 0.4], { hex: 0xFFF6E8 }); // in front of the panel, so it lights the room it faces
      const WARM = new THREE.Color(0xFFB46B), COOL = new THREE.Color(0xDDEBFF), tmp = new THREE.Color();
      let shown = '';
      return {
        root,
        update(s, t) {
          const b = clamp01(s.brightness), k = clamp01((num(s.kelvin) - 2700) / 3800), key = `${b.toFixed(3)}|${k.toFixed(3)}`;
          if (key !== shown) { shown = key; tmp.lerpColors(WARM, COOL, k); panelMat.emissive.copy(tmp); panelMat.emissiveIntensity = b * 1.6; light.color.copy(tmp); light.intensity = b * 2.4; }
          led.set(s, t);
        },
      };
    },
  },

  // A camera on a tripod. The tally lamp is red on air and the lens ring glows while it is on.
  main_camera: {
    icon: 'camera', size: 0.6,
    edit: [{ field: 'on', kind: 'switch' }],
    format: (s) => (s.on ? 'on' : 'off'),
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const top = new THREE.Vector3(0, 1.22, 0), up = new THREE.Vector3(0, 1, 0), legs = [];
      for (let i = 0; i < 3; i++) {
        const a = Math.PI / 2 + i * (Math.PI * 2 / 3), foot = new THREE.Vector3(Math.cos(a) * 0.25, 0.01, Math.sin(a) * 0.25);
        const dir = top.clone().sub(foot), len = dir.length(), mid = top.clone().add(foot).multiplyScalar(0.5);
        const leg = P.cyl(0.011, 0.014, len, M.black, mid.x, mid.y, mid.z, 8); leg.quaternion.setFromUnitVectors(up, dir.normalize()); legs.push(leg);
      }
      const head = P.cyl(0.03, 0.03, 0.05, M.black, 0, 1.24, 0, 12);
      const body = P.box(0.1, 0.1, 0.16, M.black, 0, 1.31, 0, 0.012);
      const lens = P.cyl(0.036, 0.042, 0.07, M.anodized, 0, 1.31, 0.115, 18); lens.rotation.x = Math.PI / 2;
      const ringMat = new THREE.MeshStandardMaterial({ color: 0x1B1D20, emissive: 0x3AB7FF, emissiveIntensity: 0, roughness: 0.4 });
      const ring = new THREE.Mesh(new THREE.TorusGeometry(0.043, 0.004, 8, 24), ringMat); ring.position.set(0, 1.31, 0.152);
      const root = P.group(...legs, head, body, lens, ring);
      const led = statusLed(THREE, root, { at: [0, 1.364, -0.05], r: 0.009 });
      return {
        root,
        update(s, t) {
          ringMat.emissiveIntensity = s.on ? 2 : 0;
          led.set(s, t, s.on ? HEX.red : HEX.green, s.on ? 5 : 1.5);
        },
      };
    },
  },

  // A mic on a boom stand. Its band is red while muted and green while live.
  mic: {
    icon: 'mic', size: 0.6, control: (s) => ({ kind: 'switch', field: 'muted', on: !s.muted }), // on = live, so the switch reads opposite to the muted flag
    edit: [{ field: 'muted', kind: 'switch' }],
    format: (s) => (s.muted ? 'muted' : 'live'),
    active: (s) => !s.muted,
    model({ THREE, P, M }) {
      const base = P.cyl(0.14, 0.15, 0.025, M.black, 0, 0.0125, -0.11, 24), pole = P.cyl(0.012, 0.012, 1.2, M.black, 0, 0.62, -0.11, 10);
      const boom = P.cyl(0.01, 0.01, 0.3, M.black, 0, 1.22, 0.04, 8); boom.rotation.x = Math.PI / 2;
      const head = P.cyl(0.034, 0.034, 0.15, M.steel, 0, 1.22, 0.19, 20); head.rotation.x = Math.PI / 2 - 0.35;
      const bandMat = new THREE.MeshStandardMaterial({ color: 0x1B1D20, emissive: 0xE0563A, emissiveIntensity: 1.2, roughness: 0.5 });
      const band = new THREE.Mesh(new THREE.CylinderGeometry(0.0365, 0.0365, 0.022, 20), bandMat); head.add(band);
      const root = P.group(base, pole, boom, head);
      const led = statusLed(THREE, root, { at: [0, 0.0265, -0.03], r: 0.007 });
      let shown = null;
      return {
        root,
        update(s, t) {
          const m = !!s.muted;
          if (m !== shown) { shown = m; bandMat.emissive.setHex(m ? 0xE0563A : 0x2FBF71); bandMat.emissiveIntensity = m ? 1.2 : 2.4; }
          led.set(s, t, m ? HEX.amber : HEX.green, 2);
        },
      };
    },
  },

  // A capture card box with its inputs on the front; the top shows the chosen input and whether a signal arrives.
  capture_card: {
    icon: 'capture', size: 0.5,
    edit: [{ field: 'signal', kind: 'switch' }],
    format: (s) => `${s.input} · ${s.signal ? 'signal' : 'no signal'}`,
    active: (s) => !!s.signal,
    control: false,
    model({ THREE, P, M }) {
      const body = P.box(0.2, 0.035, 0.13, M.anodized, 0, 0.0175, 0, 0.006);
      const ports = [-0.045, 0.045].map((x) => P.box(0.032, 0.014, 0.004, M.steel, x, 0.018, 0.066));
      const lead = P.tube([[-0.045, 0.018, 0.07], [-0.05, 0.01, 0.14], [0.0, 0.01, 0.2]], 0.006, M.black);
      const { tex, g } = screenTex(THREE, 192, 80);
      const label = new THREE.Mesh(new THREE.PlaneGeometry(0.15, 0.0625), new THREE.MeshStandardMaterial({ map: tex, roughness: 0.6 }));
      label.rotation.x = -Math.PI / 2; label.position.set(-0.01, 0.0362, 0.005); label.userData.noAO = true;
      const root = P.group(body, ...ports, lead, label);
      const led = statusLed(THREE, root, { at: [0.085, 0.0365, -0.05], r: 0.006 });
      let shown = '';
      const draw = (input, sig) => {
        g.fillStyle = '#0F1317'; g.fillRect(0, 0, 192, 80);
        g.fillStyle = sig ? '#2FBF71' : '#6B747C'; g.beginPath(); g.arc(24, 40, 9, 0, Math.PI * 2); g.fill();
        g.fillStyle = '#E8EEF2'; g.font = '600 30px Inter, "Segoe UI", sans-serif'; g.textAlign = 'left'; g.textBaseline = 'middle'; g.fillText(String(input), 46, 40);
        tex.needsUpdate = true;
      };
      return {
        root,
        update(s, t) {
          const key = `${s.input}|${!!s.signal}`;
          if (key !== shown) { shown = key; draw(s.input ?? '', !!s.signal); }
          led.set(s, t, s.signal ? HEX.green : HEX.amber, 2);
        },
      };
    },
  },

  // The overlay app on a monitor stand: the screen shows the scene the stream is on. A fault leaves the last picture
  // dimmed under a red "no reply" pill; offline is a blank screen.
  overlay: {
    icon: 'display', size: 0.7,
    format: (s) => ({ idle: 'idle', starting: '"Starting soon"', live: 'LIVE scene', ending: '"Thanks for watching"', recording: 'REC' }[s.scene] || String(s.scene ?? 'idle')),
    active: (s) => !!s.scene && s.scene !== 'idle',
    control: false,
    model({ THREE, P, M }) {
      const base = P.box(0.32, 0.02, 0.22, M.black, 0, 0.01, 0, 0.006), post = P.cyl(0.02, 0.02, 0.81, M.black, 0, 0.425, -0.03, 10);
      const frame = P.box(0.62, 0.37, 0.03, M.black, 0, 1.0, 0, 0.012);
      const { tex, g } = screenTex(THREE, 320, 180);
      const screen = P.screenPlane(0.57, 0.32, tex); screen.position.set(0, 1.0, 0.0166); screen.material.emissiveIntensity = 0.75;
      const root = P.group(base, post, frame, screen);
      const led = statusLed(THREE, root, { at: [0.27, 0.827, 0.0155], r: 0.005 });
      let shown = '';
      const draw = (scene, status) => {
        const dead = status === 'offline', fault = status === 'fault';
        g.fillStyle = dead ? '#07090B' : '#10161C'; g.fillRect(0, 0, 320, 180);
        g.textAlign = 'center'; g.textBaseline = 'middle';
        if (!dead) {
          if (scene === 'live') { const gr = g.createLinearGradient(0, 0, 320, 180); gr.addColorStop(0, '#2B4A63'); gr.addColorStop(1, '#6C3F7A'); g.fillStyle = gr; g.fillRect(0, 0, 320, 180); g.fillStyle = '#E0563A'; g.beginPath(); g.roundRect(14, 12, 74, 30, 15); g.fill(); g.fillStyle = '#fff'; g.font = '700 20px Inter, "Segoe UI", sans-serif'; g.fillText('LIVE', 51, 28); }
          else if (scene === 'recording') { g.fillStyle = '#E0563A'; g.beginPath(); g.arc(144, 90, 14, 0, Math.PI * 2); g.fill(); g.fillStyle = '#E8EEF2'; g.font = '700 34px Inter, "Segoe UI", sans-serif'; g.textAlign = 'left'; g.fillText('REC', 168, 92); }
          else if (scene === 'starting') { g.fillStyle = '#2C2F6B'; g.fillRect(0, 0, 320, 180); g.fillStyle = '#E8EEF2'; g.font = '600 30px Inter, "Segoe UI", sans-serif'; g.fillText('Starting soon', 160, 90); }
          else if (scene === 'ending') { g.fillStyle = '#233B2E'; g.fillRect(0, 0, 320, 180); g.fillStyle = '#E8EEF2'; g.font = '600 26px Inter, "Segoe UI", sans-serif'; g.fillText('Thanks for watching', 160, 90); }
          else { g.fillStyle = '#5E6A74'; g.font = '600 28px Inter, "Segoe UI", sans-serif'; g.fillText('idle', 160, 90); }
        }
        if (fault) { g.fillStyle = 'rgba(0,0,0,.45)'; g.fillRect(0, 0, 320, 180); g.fillStyle = '#E0563A'; g.beginPath(); g.roundRect(100, 8, 120, 26, 13); g.fill(); g.fillStyle = '#fff'; g.font = '600 16px Inter, "Segoe UI", sans-serif'; g.textAlign = 'center'; g.fillText('no reply', 160, 21); }
        tex.needsUpdate = true;
      };
      return {
        root,
        update(s, t) {
          const key = `${s.scene}|${s.status}`;
          if (key !== shown) { shown = key; draw(s.scene, s.status); }
          led.set(s, t, HEX.green, 2);
        },
      };
    },
  },

  // A lit sign: "ON AIR" in red letters, glowing while on.
  on_air_sign: {
    icon: 'sign', size: 0.5, mountY: 2.0,
    edit: [{ field: 'on', kind: 'switch' }],
    format: (s) => (s.on ? 'ON AIR' : 'off'),
    active: (s) => !!s.on,
    control: { kind: 'switch', field: 'on' },
    model({ THREE, P, M }) {
      const box = P.box(0.46, 0.17, 0.04, M.black, 0, 0, 0.02, 0.01);
      const { tex, g } = screenTex(THREE, 420, 130);
      g.fillStyle = '#1A0A0A'; g.fillRect(0, 0, 420, 130); g.fillStyle = '#FF3030'; g.font = '700 70px Inter, "Segoe UI", sans-serif'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('ON AIR', 210, 68); tex.needsUpdate = true;
      const face = P.screenPlane(0.42, 0.13, tex); face.position.set(0, 0, 0.0425); face.material.emissiveIntensity = 0; face.material.roughness = 0.85;
      const root = P.group(box, face);
      const led = statusLed(THREE, root, { at: [0.2, -0.075, 0.0425], r: 0.005 });
      return {
        root,
        update(s, t) {
          face.material.emissiveIntensity = s.on ? 1.7 : 0;
          led.set(s, t, s.on ? HEX.red : HEX.green, s.on ? 3 : 1.5);
        },
      };
    },
  },

  // A strip of light along the wall, with a soft wash on the wall around it, in the chosen colour.
  led_strip: {
    icon: 'strip', size: 1.2, mountY: 1.0,
    edit: [{ field: 'brightness', kind: 'slider', step: 0.01, scale: 100, unit: '%' }],
    format: (s) => { const r = pct(s.brightness); return r <= 0 ? 'off' : `${s.color} · ${r >= 100 ? 'full' : `${r}%`}`; },
    active: (s) => pct(s.brightness) > 0,
    control: { kind: 'bar', field: 'brightness', min: 0, max: 1 },
    model({ THREE, P, M }) {
      const stripMat = new THREE.MeshStandardMaterial({ color: 0x151719, emissive: 0x3A8CFF, emissiveIntensity: 0, roughness: 0.5 });
      const strip = new THREE.Mesh(new THREE.BoxGeometry(1.0, 0.02, 0.014), stripMat); strip.position.set(-0.04, 0, 0.007);
      const { tex, g } = screenTex(THREE, 128, 32);
      const gr = g.createLinearGradient(0, 0, 0, 32); gr.addColorStop(0, 'rgba(255,255,255,0)'); gr.addColorStop(0.5, 'rgba(255,255,255,1)'); gr.addColorStop(1, 'rgba(255,255,255,0)'); g.fillStyle = gr; g.fillRect(0, 0, 128, 32); tex.needsUpdate = true;
      const washMat = new THREE.MeshBasicMaterial({ map: tex, color: 0x3A8CFF, transparent: true, opacity: 0, depthWrite: false, blending: THREE.AdditiveBlending });
      const wash = new THREE.Mesh(new THREE.PlaneGeometry(1.2, 0.36), washMat); wash.position.z = 0.004; wash.userData.noAO = true; wash.raycast = () => {};
      const ctl = P.box(0.07, 0.035, 0.02, M.white, 0.54, 0, 0.01, 0.004);
      const root = P.group(strip, wash, ctl);
      const led = statusLed(THREE, root, { at: [0.54, 0, 0.0205], r: 0.005 });
      let shown = '';
      return {
        root,
        update(s, t) {
          const b = clamp01(s.brightness), key = `${b.toFixed(3)}|${s.color}`;
          if (key !== shown) {
            shown = key; const hex = STRIP[s.color] ?? STRIP.white;
            stripMat.emissive.setHex(hex); stripMat.emissiveIntensity = b * 3.2; washMat.color.setHex(hex); washMat.opacity = b * 0.35;
          }
          led.set(s, t);
        },
      };
    },
  },
};
