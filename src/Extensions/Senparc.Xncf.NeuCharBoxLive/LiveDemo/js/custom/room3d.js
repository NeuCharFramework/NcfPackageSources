// The custom room in 3D: an 8 × 6 × 2.8 m shell with one window, the hub on its stand, and one model per layout device
// (devices/index.js). createRoom3d({ R, P, M, THREE, store }) →
//   { sync(layout), update(state, t), focus(id, hex), reset(), objectFor(id), hubObject, pickIds() }
// sync() matches the room to a layout (js/custom/layout.js): it builds what is new, moves what moved and drops what
// left, with its tap target (R.removePickable). Where a device goes follows its type's mount:
//   floor    (x, 0, z), turned rot° about y · ceiling (x, 2.8, z), turned the same way
//   wall     (x, mountY ?? 1.4, z): rot 0 is the back wall (z = -3), facing +z; rot 90 the left wall (x = -4), facing +x
// A wall model hides with its wall when the camera goes round behind it (P.onWall: the renderer owns its .visible).
// Every model sits in a holder that only room3d places, and hides when the device leaves: the renderer keeps a shadow
// twin of each wall-hung mesh, shown while every untagged ancestor of that mesh is visible, so a removed wall device
// takes its shadow with it.
// update() runs every frame; a device with no slice in `state` yet (one just added while building) shows its catalog
// initial state. Each model's update also gets the room's light outside, env = { hour, sky } (sky: 0 at night, 1 by day),
// for parts that show the daylight (the blinds' own pane). The page passes its `store` too; the room reads only the
// state update() is given.
import { typeOf } from './catalog.js';
import { ROOM, WINDOW } from './layout.js';
import { DEVICE_UI } from './devices/index.js';
import { mergeMeshes } from './devices/kit.js';
import { START_HOUR } from './scene.js';

const HX = ROOM.w / 2, HZ = ROOM.d / 2;
const MAPS = ['map', 'bumpMap', 'emissiveMap', 'roughnessMap', 'normalMap', 'alphaMap'];
// Real point lights in the room at once. Lit lights take the slots first (in layout order), then dark ones fill what is
// left; a light past the budget still glows (shade and bulb) but lights nothing: twenty point lights in every lit
// fragment would not stay smooth on a phone. The count of visible lights stays at the budget (a change in it would
// recompile every material), and the slots are only re-dealt when the set of lit lights changes, not every frame.
export const LIGHT_BUDGET = 4;
// Which of `lights` (in layout order, each with an intensity) get a slot: the lit ones first, then dark ones up to the budget.
export function dealLights(lights, budget = LIGHT_BUDGET) {
  const out = new Set(); let n = 0;
  for (const L of lights) if (L.intensity > 0.001 && n < budget) { out.add(L); n++; }
  for (const L of lights) if (n < budget && !out.has(L)) { out.add(L); n++; }
  return out;
}
// How light it is outside at an hour, 0 (night) to 1 (midday), with a little left at dawn and dusk: the renderer's
// daylight() curve, for the parts of a model that show the sky.
export function skyAt(hour) {
  const h = ((hour % 24) + 24) % 24, day = Math.max(0, Math.sin(((h - 6) / 12) * Math.PI));
  const dusk = Math.exp(-(((h - 18.5) / 1.2) ** 2)) + Math.exp(-(((h - 6) / 1.2) ** 2));
  return Math.min(1, day + 0.35 * dusk);
}

export function createRoom3d({ R, P, M, THREE }) {
  R.scene.background = new THREE.Color(0xDCEBF3);
  // The renderer's sun shadow covers 10 m across, sized for 5 m rooms. Seen from the sun, this room reaches 5.4 m from
  // the frustum's centre in the morning, so a corner would go unshadowed (sunlit through the wall): widen it.
  const sunCam = R.lights?.sun?.shadow?.camera;
  if (sunCam) { Object.assign(sunCam, { left: -6.4, right: 6.4, top: 6, bottom: -4.8 }); sunCam.updateProjectionMatrix(); }
  // The floor's planks at Home's scale (its 5 m floor repeats the texture 1.6 times).
  const floorMat = M.floor.clone();
  for (const k of ['map', 'bumpMap']) if (M.floor[k]) { floorMat[k] = M.floor[k].clone(); floorMat[k].repeat.set(1.6 * ROOM.w / 5, 1.6 * ROOM.d / 5); }
  P.roomShell({ w: ROOM.w, d: ROOM.d, h: ROOM.h, floorMat, window: WINDOW });
  // Skirting and crown on the right and front walls too, as in Home: they hide with their walls, a few mm lower than
  // the shell's so no two trims share a face in a corner.
  for (const [th, ty] of [[0.098, 0.049], [0.057, ROOM.h - 0.0295]]) {
    P.onWall(P.flatNormals(P.box(0.02, th, ROOM.d, M.trim, HX - 0.01, ty, 0), -1, 0, 0), -1, 0, -HX);
    P.onWall(P.flatNormals(P.box(ROOM.w - 0.04, th, 0.02, M.trim, 0, ty, HZ - 0.01), 0, 0, -1), 0, -1, -HZ);
  }

  // The hub on a small stand, so a 14 cm box reads from across an 8 m room; the whole stand is its tap target.
  const hub = (() => {
    const top = P.box(0.42, 0.03, 0.42, M.woodLight, 0, 0.435, 0, 0.008);
    const legs = mergeMeshes(THREE, [[-1, -1], [1, -1], [-1, 1], [1, 1]].map(([sx, sz]) => P.box(0.03, 0.42, 0.03, M.black, sx * 0.18, 0.21, sz * 0.18)), M.black, { consume: true });
    const unit = P.hub(0, 0.45, 0);
    const root = P.group(top, legs, unit.group);
    R.addPickable(root, 'hub');
    return { root, ledMat: unit.ledMat, hi: null, shells: null, cut: false };
  })();

  const objs = new Map(); // id → { type, holder, root, model, lights, idle, hi, shells, cut }
  let order = [], litKey = null; // the layout's device ids in order; what the slots were last dealt for
  function dealSlots() {
    const lights = []; let key = '';
    for (const id of order) for (const L of objs.get(id)?.lights ?? []) { lights.push(L); key += L.intensity > 0.001 ? '1' : '0'; }
    if (key === litKey) return;
    litKey = key;
    const give = dealLights(lights); for (const L of lights) L.visible = give.has(L);
  }

  // A small wall or ceiling device is a few pixels wide from across the room, too small for a fingertip: an unseen box
  // round it (at least TAP_MIN across its face) is part of its tap target, so a tap near it picks it. It sits on the
  // device's side of the wall (z 0 to the model's depth, or at least TAP_DEPTH), so it never reaches into the room.
  const TAP_MIN = 0.4, TAP_DEPTH = 0.1;
  function tapProxy(root, mount) {
    if (mount === 'floor') return;
    root.updateWorldMatrix(true, true);
    const b = new THREE.Box3().setFromObject(root); if (b.isEmpty()) return;
    const sz = b.getSize(new THREE.Vector3()), c = b.getCenter(new THREE.Vector3());
    const wall = mount === 'wall';
    if (wall ? sz.x >= TAP_MIN && sz.y >= TAP_MIN : sz.x >= TAP_MIN && sz.z >= TAP_MIN) return; // already a fingertip's size
    const w = Math.max(sz.x, TAP_MIN), h = wall ? Math.max(sz.y, TAP_MIN) : Math.max(sz.y, TAP_DEPTH), dp = wall ? Math.max(sz.z, TAP_DEPTH) : Math.max(sz.z, TAP_MIN);
    const p = new THREE.Mesh(new THREE.BoxGeometry(w, h, dp), new THREE.MeshBasicMaterial({ visible: false }));
    p.position.set(c.x, wall ? c.y : b.max.y - h / 2, wall ? dp / 2 : c.z); // wall: z from 0 · ceiling: down from the top of the model
    p.visible = false; p.castShadow = p.receiveShadow = false; p.userData.proxy = p.userData.noAO = p.userData.noHighlight = true;
    root.add(p);
  }
  function add(d, ui) {
    const model = ui.model({ THREE, P, M });
    tapProxy(model.root, typeOf(d.type)?.mount);
    const holder = new THREE.Group(); holder.add(model.root); R.scene.add(holder);
    R.addPickable(model.pick || model.root, d.id);
    const o = { type: d.type, holder, root: model.root, model, lights: [], idle: null, hi: null, shells: null, cut: false };
    model.root.traverse((x) => { if (x.isLight) o.lights.push(x); });
    objs.set(d.id, o);
    return o;
  }
  function place(o, d, mount, ui) {
    const h = o.holder;
    if (mount === 'wall') {
      const left = d.rot === 90;
      h.position.set(d.x, ui.mountY ?? 1.4, d.z); h.rotation.set(0, left ? Math.PI / 2 : 0, 0);
      if (left) P.onWall(o.root, 1, 0, -HX); else P.onWall(o.root, 0, 1, -HZ);
    } else {
      h.position.set(d.x, mount === 'ceiling' ? ROOM.h : 0, d.z); h.rotation.set(0, (d.rot * Math.PI) / 180, 0);
    }
  }
  function drop(id) {
    const o = objs.get(id); if (!o) return;
    R.removePickable(id); R.unping?.(id);
    o.holder.visible = false; o.holder.removeFromParent();
    // its meshes' own geometry (never a sprite's: three shares one geometry across every sprite, labels and glows too)
    o.holder.traverse((x) => { if (x.isMesh && !x.userData.isShell) x.geometry.dispose(); });
    // its own materials and their maps (a screen's CanvasTexture); never the shared M ones, nor a texture tagged shared (the LED glow)
    const shared = new Set(Object.values(M)), kept = new Set();
    for (const m of shared) for (const k of MAPS) if (m?.[k]) kept.add(m[k]);
    o.holder.traverse((x) => {
      for (const m of [].concat(x.material || [])) {
        if (shared.has(m)) continue;
        for (const k of MAPS) { const t = m[k]; if (t?.dispose && !kept.has(t) && !t.userData.shared) t.dispose(); }
        m.dispose();
      }
    });
    R.pruneTwins?.(); // the renderer's shadow twins of this device's wall-hung meshes
    objs.delete(id);
  }

  function sync(layout) {
    hub.root.position.set(layout.hub.x, 0, layout.hub.z);
    const want = new Map(layout.devices.map((d) => [d.id, d]));
    for (const [id, o] of [...objs]) if (want.get(id)?.type !== o.type) drop(id);
    order = [];
    for (const d of layout.devices) {
      const t = typeOf(d.type), ui = DEVICE_UI[d.type]; if (!t || !ui) continue;
      const o = objs.get(d.id) || add(d, ui);
      place(o, d, t.mount, ui); order.push(d.id);
    }
    litKey = null; dealSlots();
  }

  // A tapped device's highlight pulse (parts.js highlighter), made on its first focus: its shells are a second mesh per
  // part, so a room nobody taps carries none. reset() cuts the pulses still running; the next focus brings them back.
  const entry = (id) => (id === 'hub' ? hub : objs.get(id));
  function focus(id, hex) {
    const o = entry(id); if (!o) return;
    if (!o.hi) { o.hi = P.highlighter({ [id]: [o.root] }); o.shells = []; o.root.traverse((x) => { if (x.userData.isShell) o.shells.push(x); }); }
    o.cut = false; o.hi.focus(id, hex);
  }
  const pulse = (o) => { if (!o.hi) return; o.hi.update(); if (o.cut) for (const s of o.shells) s.visible = false; };

  return {
    sync, focus,
    update(state, t) {
      const h0 = state?.env?.hour, h = Number.isFinite(h0) ? h0 : START_HOUR; R.daylight(h);
      const env = { hour: h, sky: skyAt(h) };
      const hs = state?.hub, on = hs && (hs.status === 'online' || hs.status === 'on');
      hub.ledMat.emissiveIntensity = on ? (2.5 + Math.sin(t * 2.2) * 1.5) * (Number.isFinite(hs.led) ? hs.led : 1) : 0;
      pulse(hub);
      for (const [id, o] of objs) {
        const s = state?.[id] || (o.idle ||= { ...typeOf(o.type).initial, status: 'online' });
        o.model.update(s, t, env); pulse(o);
      }
      dealSlots();
    },
    reset() { for (const o of [hub, ...objs.values()]) if (o.hi) { o.cut = true; for (const s of o.shells) s.visible = false; } },
    objectFor: (id) => (id === 'hub' ? hub.root : objs.get(id)?.holder ?? null),
    hubObject: hub.root,
    pickIds: () => ['hub', ...objs.keys()],
  };
}
