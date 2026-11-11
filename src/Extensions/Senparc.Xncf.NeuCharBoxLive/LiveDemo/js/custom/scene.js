// The custom room as a scene object for the engine (panel.js, live.js, whatif.js): its devices, in layout order, named
// by the server's rule (catalog.js nameOf), with each type's card UI (devices/index.js). No prompts: this scene only
// runs live requests.
import { typeOf, nameOf } from './catalog.js';
import { DEVICE_UI } from './devices/index.js';

export const START_HOUR = 16.5;
// Seen from outside the front-right corner (the front and right walls are cut away), the 8 × 6 m room fits at aspect 1.5;
// the renderer widens the vertical FOV on narrower canvases (fitAspect). Looking down at the floor's middle, so the room
// sits mid-frame on the near-square desktop canvas instead of under a band of sky (browser check, Task 8).
export const CAMERA = { position: [6.8, 4.7, 6.0], target: [-0.4, 0.25, -0.6], fov: 42, fitAspect: 1.5, azimuth: [0.62, 1.0], minDistance: 2, maxDistance: 12 };
// Portrait phones (the room is taller than wide): the same corner, but looking down more steeply and from closer in, so
// the floor fills the tall canvas instead of sitting in a band between sky and lawn (browser check, Task 16). Chosen once, when the page loads.
export const CAMERA_PHONE = { ...CAMERA, position: [5.2, 7.6, 4.5], target: [-0.3, 0, -0.5], fitAspect: 1.45, maxDistance: 12 };
export const isPhonePortrait = (w, h) => w <= 600 && h > w * 1.15;
export const cameraFor = (w, h) => (isPhonePortrait(w, h) ? CAMERA_PHONE : CAMERA);

// The card's hand edits (js/engine/controls.js editsFor): every catalog edit field, in the catalog's order, as DEVICE_UI
// presents it, with the catalog's range. A field DEVICE_UI doesn't present is left out (a test keeps there being none).
function editOf(t, ui) {
  const out = [];
  for (const e of t.edit || []) {
    const p = (ui.edit || []).find((x) => x.field === e.field); if (!p) continue;
    out.push(e.kind === 'number'
      ? { field: e.field, kind: 'slider', min: e.min, max: e.max, step: p.step, scale: p.scale ?? 1, unit: p.unit, label: p.label, top: p.top }
      : { field: e.field, kind: 'switch', label: p.label });
  }
  return out;
}

export function sceneFor(layout) {
  const devices = {}, deviceOrder = [];
  for (const d of layout?.devices || []) {
    const t = typeOf(d.type), ui = DEVICE_UI[d.type], names = t && nameOf(d.type, d.id);
    if (!t || !ui || !names) continue;
    devices[d.id] = { type: d.type, icon: ui.icon, name: names.name, ref: names.ref, initial: structuredClone(t.initial), format: ui.format, active: ui.active, control: ui.control, edit: editOf(t, ui), faultText: t.faultText, plural: !!ui.plural };
    deviceOrder.push(d.id);
  }
  return { id: 'custom', title: 'Build your own', startHour: START_HOUR, camera: CAMERA, deviceOrder, devices };
}

// The store's starting state for a fresh live session: every device online in its catalog initial state, the hub on.
export function initialFor(scene) {
  const init = { env: { hour: scene.startHour ?? START_HOUR }, hub: { status: 'online', led: 1 }, plan: {} };
  for (const [id, d] of Object.entries(scene.devices)) init[id] = { ...structuredClone(d.initial), status: 'online' };
  return init;
}
