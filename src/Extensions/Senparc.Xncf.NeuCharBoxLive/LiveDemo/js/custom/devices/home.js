// The Home types' card UI and 3D models for the custom room (contract: devices/index.js). Their look follows Home's
// room (js/scenes/home.js), rebuilt as self-contained models: each stands, hangs or mounts on its own, with its own
// status LED, since here nothing else (a wall plug, a door) is there to carry it.
import { HEX, num, clamp01, pct, oneDecimal, statusLed, mergeMeshes } from './kit.js';

export default {
  // Off and full at the ends of the dimmer, never "0%" or "100%".
  floor_lamp: {
    icon: 'lamp', size: 0.6,
    edit: [{ field: 'brightness', kind: 'slider', step: 0.01, scale: 100, unit: '%' }],
    format: (s) => { const r = pct(s.brightness); return r <= 0 ? 'off' : r >= 100 ? 'full' : `on · ${r}%`; },
    active: (s) => pct(s.brightness) > 0,
    control: { kind: 'bar', field: 'brightness', min: 0, max: 1 },
    // P.floorLamp unshaded: no point-light shadows (six cube renders a frame each, and a light added after the renderer
    // starts would keep them on every tier). A smart dimmer module on the pole carries the status LED. With no shade
    // shadow the bulb lights the wall behind it at full strength: 2.8 at full keeps that wall from blowing out (and
    // blooming over the wall top) at night, and still lights the floor around it.
    model({ THREE, P, M }) {
      const lamp = P.floorLamp(0, 0);
      lamp.bulb.castShadow = false; lamp.bulb.userData.wantsShadow = false; lamp.bulb.distance = 6;
      const dimmer = P.box(0.045, 0.075, 0.03, M.white, 0, 0.95, 0.035, 0.006);
      const root = P.group(lamp.group, dimmer);
      const led = statusLed(THREE, root, { at: [0, 0.972, 0.052], r: 0.005 });
      let shown = -1;
      return {
        root,
        update(s, t) {
          const b = clamp01(s.brightness);
          if (b !== shown) { shown = b; lamp.bulb.intensity = b * 2.8; lamp.bulbMesh.material.emissiveIntensity = b * 3; lamp.shadeMat.emissiveIntensity = b * 0.35; }
          led.set(s, t); // the light follows the brightness in a fault too: dark, or lit and stuck on
        },
      };
    },
  },

  // A window blind on its own window (frame and a pane), so open slats show daylight and closed ones block it. The pane
  // shows the sky (env.sky): pale by day, a dark night blue after dusk.
  // "open" and "closed" at the ends of the travel, as in Home.
  blinds: {
    icon: 'blinds', size: 1.2, mountY: 1.5, plural: true,
    edit: [{ field: 'closed', kind: 'slider', step: 0.01, scale: 100, unit: '%', top: 'closed' }],
    format: (s) => { const r = pct(s.closed); return r <= 0 ? 'open' : r >= 100 ? 'closed' : `${r}% closed`; },
    control: { kind: 'bar', field: 'closed', min: 0, max: 1 },
    model({ THREE, P, M }) {
      const W = 1.1, H = 1.12, Z = 0.085; // frame outside; the slats' plane, in front of the frame (0–0.035)
      const frame = mergeMeshes(THREE, [
        P.box(W, 0.05, 0.035, M.trim, 0, H / 2 - 0.025, 0.0175), P.box(W, 0.05, 0.035, M.trim, 0, -H / 2 + 0.025, 0.0175),
        P.box(0.05, H - 0.1, 0.035, M.trim, -W / 2 + 0.025, 0, 0.0175), P.box(0.05, H - 0.1, 0.035, M.trim, W / 2 - 0.025, 0, 0.0175),
        P.box(W + 0.06, 0.03, 0.07, M.trim, 0, -H / 2 - 0.015, 0.035), // sill
      ], M.trim, { consume: true });
      const pane = new THREE.Mesh(new THREE.PlaneGeometry(W - 0.1, H - 0.1), new THREE.MeshStandardMaterial({ color: 0xCFE3EE, emissive: 0xBFDDEE, emissiveIntensity: 0.45, roughness: 0.3 }));
      pane.position.z = 0.004; pane.receiveShadow = true;
      const DAY = new THREE.Color(0xCFE3EE), NIGHT = new THREE.Color(0x1E2A3A);
      // 16 slats at a 7 cm pitch, as Home's: tilted to 1.3 rad a 7.5 cm slat covers more than the pitch, so "closed" closes.
      // P.blinds returns the slats; its headrail is the next object it added, right after the last slat.
      const slats = P.blinds(0, H / 2 - 0.04, Z, W - 0.06, 16, 0.07);
      const host = slats[0].parent, rail = host.children[host.children.indexOf(slats[slats.length - 1]) + 1];
      // One shared slat geometry whose thin front/back faces take the broad face's normal (no dashed line per closed
      // slat on phones without MSAA), with a light-to-darker tint across each slat so closed slats still read apart.
      const geo = slats[0].geometry.clone(), nrm = geo.attributes.normal, pos = geo.attributes.position, col = [];
      for (let i = 0; i < nrm.count; i++) { if (Math.abs(nrm.getZ(i)) > 0.5) nrm.setXYZ(i, 0, 1, 0); const c = 0.84 + 0.16 * (0.5 - pos.getZ(i) / 0.075); col.push(c, c, c); }
      geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
      const slatMat = M.slat.clone(); slatMat.vertexColors = true;
      for (const s of slats) { s.geometry.dispose(); s.geometry = geo; s.material = slatMat; }
      const root = P.group(frame, pane, ...slats, rail);
      const led = statusLed(THREE, root, { at: [-W / 2 + 0.07, H / 2 + 0.02, Z + 0.044], r: 0.006 }); // motor LED, headrail front
      let shown = -1, sky = -1;
      return {
        root,
        update(s, t, env) {
          const a = clamp01(s.closed) * 1.3;
          if (a !== shown) { shown = a; for (const sl of slats) sl.rotation.x = a; }
          const k = Math.round(clamp01(env?.sky ?? 1) * 50) / 50; // in 2% steps: no material write every frame
          if (k !== sky) { sky = k; pane.material.color.lerpColors(NIGHT, DAY, k); pane.material.emissiveIntensity = 0.45 * k; }
          led.set(s, t);
        },
      };
    },
  },

  // Soil moisture probe in a potted plant; the soil darkens as it takes up water (20% reads dry and pale, 45% dark).
  soil_sensor: {
    icon: 'moisture', size: 0.8,
    edit: [{ field: 'moisture', kind: 'slider', step: 1, unit: '%', top: 'saturated' }],
    format: (s) => { const m = Math.round(num(s.moisture)); return m >= 100 ? 'saturated' : `${Math.max(0, m)}% moisture`; },
    control: false,
    model({ THREE, P, M }) {
      const S = 0.8, plant = P.plant(0, 0, { scale: S });
      // 16 leaves and 16 stems → one mesh each (each leaf keeps its tint as a vertex colour)
      const stems = plant.group.children.filter((c) => c.isMesh && c.material === M.stem);
      const leafMat = M.leaf.clone(); leafMat.color.setHex(0xFFFFFF); leafMat.vertexColors = true;
      plant.group.add(mergeMeshes(THREE, plant.leaves, leafMat, { tint: true, consume: true }), mergeMeshes(THREE, stems, M.stem, { consume: true }));
      const soilMat = M.soil.clone(); plant.soil.material = soilMat;
      const stake = P.box(0.016, 0.3, 0.016, M.metal, 0.1, 0.4, 0.06), head = P.box(0.05, 0.042, 0.026, M.hub, 0.1, 0.57, 0.06, 0.006);
      const root = P.group(plant.group, stake, head);
      const led = statusLed(THREE, root, { at: [0.1, 0.578, 0.076], r: 0.006 });
      const DRY = new THREE.Color(0x7B5B40), WET = new THREE.Color(0x2A1C12); let shown = null;
      return {
        root,
        update(s, t) {
          const m = num(s.moisture);
          if (m !== shown) { shown = m; const k = THREE.MathUtils.clamp((m - 20) / 25, 0, 1); soilMat.color.lerpColors(DRY, WET, k); soilMat.roughness = 1 - 0.3 * k; }
          led.set(s, t, HEX.green, 2);
        },
      };
    },
  },

  // Pump with its water tank on top (as Home's) and an impeller window on the front that spins while it runs; the
  // hose runs out along the floor.
  water_pump: {
    icon: 'pump', size: 0.5,
    edit: [{ field: 'running', kind: 'switch' }],
    format: (s) => (s.running ? 'running' : 'idle'),
    active: (s) => !!s.running,
    control: { kind: 'switch', field: 'running' },
    model({ THREE, P, M }) {
      const body = P.box(0.26, 0.2, 0.2, M.black, 0, 0.1, 0, 0.02);
      const tank = P.box(0.24, 0.2, 0.18, new THREE.MeshPhysicalMaterial({ color: 0xE8F4F8, roughness: 0.08, transparent: true, opacity: 0.28, depthWrite: false }), 0, 0.3, 0, 0.012);
      tank.castShadow = false; tank.userData.noAO = true; tank.renderOrder = 1; // drawn after the water inside it
      const water = P.box(0.22, 0.13, 0.16, new THREE.MeshStandardMaterial({ color: 0x4FA8CF, roughness: 0.2, transparent: true, opacity: 0.75 }), 0, 0.27, 0);
      const lid = P.box(0.25, 0.012, 0.19, M.black, 0, 0.406, 0, 0.004);
      const hose = P.tube([[0.09, 0.07, -0.09], [0.12, 0.035, -0.15], [0.1, 0.018, -0.2], [0.04, 0.018, -0.22]], 0.012);
      const disc = P.cyl(0.048, 0.048, 0.008, M.steel, 0, 0, 0.004, 18); disc.rotation.x = Math.PI / 2;
      const vanes = [P.box(0.08, 0.012, 0.005, M.white, 0, 0, 0.011), P.box(0.012, 0.08, 0.005, M.white, 0, 0, 0.011)];
      const rotor = P.group(disc, ...vanes); rotor.position.set(-0.03, 0.1, 0.1);
      const root = P.group(body, tank, water, lid, hose, rotor);
      const led = statusLed(THREE, root, { at: [0.085, 0.16, 0.104], r: 0.007 });
      return {
        root,
        update(s, t) {
          if (s.running) rotor.rotation.z = -((t * 14) % (Math.PI * 2));
          led.set(s, t, s.running ? HEX.blue : HEX.green, s.running ? 4 : 2);
        },
      };
    },
  },

  // A wall-mounted camera on a short bracket (P.smallCamera), tilted down at the room. Its LED: green standby, red
  // armed, amber motion (blinking while armed), as in Home.
  door_camera: {
    icon: 'camera', size: 0.3, mountY: 2.0,
    edit: [{ field: 'armed', kind: 'switch', label: 'armed' }, { field: 'motion', kind: 'switch', label: 'motion' }],
    format: (s) => (s.armed ? (s.motion ? 'motion at door' : 'armed · no motion') : s.motion ? 'standby · motion' : 'standby'),
    active: (s) => !!s.armed,
    control: { kind: 'switch', field: 'armed' },
    model({ THREE, P, M }) {
      const plate = P.box(0.07, 0.1, 0.012, M.white, 0, 0, 0.006, 0.004), arm = P.box(0.018, 0.018, 0.07, M.white, 0, 0, 0.045);
      const cam = P.smallCamera(0, 0, 0.12); cam.group.rotation.x = 0.3;
      const root = P.group(plate, arm, cam.group);
      const led = statusLed(THREE, root, { mesh: cam.led });
      return {
        root,
        update(s, t) {
          const blink = Math.floor(t * 3) % 2 === 0;
          if (s.motion) led.set(s, t, HEX.amber, s.armed ? (blink ? 6 : 3) : 2.5);
          else led.set(s, t, s.armed ? HEX.red : HEX.green, s.armed ? 3 : 2);
        },
      };
    },
  },

  // Set point large and the room's temperature small on its screen (one canvas, redrawn on change); a fault fills the
  // screen red so it reads as an alarm from across the room, and offline shows dashes.
  thermostat: {
    icon: 'thermostat', size: 0.3, mountY: 1.45,
    edit: [{ field: 'target', kind: 'slider', step: 0.5, unit: '°C', label: 'set' }, { field: 'current', kind: 'slider', step: 0.5, unit: '°C', label: 'now' }],
    format: (s) => `${oneDecimal(s.target)}°C set · ${num(s.current).toFixed(1)}°C now`,
    control: { kind: 'bar', field: 'target', min: 10, max: 28 }, // the set point on the thermostat's 10–28 °C range
    model({ THREE, P, M }) {
      const body = P.box(0.11, 0.11, 0.03, M.white, 0, 0, 0.015, 0.01);
      const c = document.createElement('canvas'); c.width = 256; c.height = 144;
      const tex = new THREE.CanvasTexture(c); tex.colorSpace = THREE.SRGBColorSpace;
      const screen = P.screenPlane(0.07, 0.04, tex); screen.position.set(0, 0.008, 0.032); screen.material.emissiveIntensity = 0.7;
      const root = P.group(body, screen);
      const led = statusLed(THREE, root, { at: [0.042, -0.042, 0.032], r: 0.004 });
      const draw = (big, small, alarm = false) => {
        const g = c.getContext('2d'); g.fillStyle = alarm ? '#5A120D' : '#0B0F14'; g.fillRect(0, 0, 256, 144);
        g.textAlign = 'center'; g.textBaseline = 'middle';
        g.fillStyle = alarm ? '#FF7A62' : '#9FE3F0'; g.font = '600 64px Inter, "Segoe UI", sans-serif'; g.fillText(big, 128, small ? 56 : 72);
        if (small) { g.fillStyle = alarm ? '#FFC2B6' : '#6FB3C2'; g.font = '30px Inter, "Segoe UI", sans-serif'; g.fillText(small, 128, 116); }
        tex.needsUpdate = true;
      };
      let shown = null; // what the screen shows: -1 offline, -2 fault, else target and current in tenths
      return {
        root,
        update(s, t) {
          const key = s.status === 'offline' ? -1 : s.status === 'fault' ? -2 : Math.round(num(s.target) * 10) * 1e5 + Math.round(num(s.current) * 10) + 5e4;
          if (key !== shown) {
            shown = key;
            if (key === -1) draw('--', ''); else if (key === -2) draw('!!', 'no reply', true);
            else draw(`${oneDecimal(s.target)}°C`, `now ${num(s.current).toFixed(1)}°`);
          }
          led.set(s, t);
        },
      };
    },
  },
};
