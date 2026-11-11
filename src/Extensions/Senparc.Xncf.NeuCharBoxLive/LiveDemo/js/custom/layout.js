// The room layout model: where each device stands. Pure data, no DOM and no THREE. Every function returns a new layout.
//   layout = { v: 1, hub: {x, z}, devices: [{ id, type, x, z, rot }] }
//   The floor is centred at the origin (x -4..4, z -3..3). Wall devices sit on a back wall: z = -3 (rot 0) or x = -4 (rot 90),
//   never across the window (WINDOW, in the back wall).
import { typeOf, validId, idFor, MAX_DEVICES } from './catalog.js';

export const ROOM = { w: 8, d: 6, h: 2.8 };
export const HUB_HOME = { x: 0, z: -2.4 };
export const STORAGE_KEY = 'ncb.customRoom';
// The room's one window, in the back wall (z = -3): centre x and y, width and height (room3d.js builds it from this).
export const WINDOW = { x: 2.2, y: 1.55, ww: 1.8, wh: 1.36 };
const HX = ROOM.w / 2, HZ = ROOM.d / 2, HUB_SIZE = 0.5, GRID = 0.5;
const WIN_MARGIN = 0.04; // the window's casing stands 2.5 cm past its opening

const num = (v) => (typeof v === 'number' && Number.isFinite(v));
const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));
const r4 = (v) => Math.round(v * 1e4) / 1e4;
const mountOf = (type) => typeOf(type)?.mount ?? 'floor';

export const emptyLayout = () => ({ v: 1, hub: { ...HUB_HOME }, devices: [] });

// Inside the room with a size/2 margin. A wall mount snaps to the nearer back wall (z = -3 or x = -4) and slides along it;
// on the back wall it keeps off the window, on whichever side of it is nearer (the left when the right is too narrow).
export function clampPos(mount, pos, size = 0.5) {
  const h = size / 2, x = num(pos?.x) ? pos.x : 0, z = num(pos?.z) ? pos.z : 0;
  if (mount === 'wall') {
    const toBack = Math.abs(z + HZ), toLeft = Math.abs(x + HX);
    if (toLeft < toBack) return { x: -HX, z: clamp(z, -HZ + h, HZ - h) };
    let bx = clamp(x, -HX + h, HX - h);
    const lo = WINDOW.x - WINDOW.ww / 2 - WIN_MARGIN - h, hi = WINDOW.x + WINDOW.ww / 2 + WIN_MARGIN + h;
    if (bx > lo && bx < hi) bx = r4(bx >= WINDOW.x && hi <= HX - h ? hi : lo);
    return { x: bx, z: -HZ };
  }
  return { x: clamp(x, -HX + h, HX - h), z: clamp(z, -HZ + h, HZ - h) };
}
const wallRot = (p) => (p.x === -HX ? 90 : 0);

// The first spot on a 0.5 m grid whose footprint clears every device and the hub (centre distance >= (size + other size) / 2),
// else the candidate with the most clearance. sizeOf(type) gives a device's footprint (default: the new device's own).
// A floor and a ceiling device may share a spot; a wall device never falls back to a spot off its wall.
export function freeSpot(layout, mount, size = 0.5, sizeOf = () => size) {
  const taken = [{ x: layout.hub.x, z: layout.hub.z, s: HUB_SIZE, m: 'hub' },
    ...layout.devices.map((d) => ({ x: d.x, z: d.z, s: sizeOf(d.type) ?? size, m: mountOf(d.type) }))]
    .filter((t) => !((mount === 'ceiling' && t.m === 'floor') || (mount === 'floor' && t.m === 'ceiling')));
  const clearance = (p) => taken.reduce((c, t) => Math.min(c, Math.hypot(p.x - t.x, p.z - t.z) - (size + t.s) / 2), Infinity);
  const cands = [];
  if (mount === 'wall') {
    for (let x = -HX + GRID; x <= HX - size / 2 + 1e-9; x += GRID) cands.push({ x, z: -HZ });
    for (let z = -HZ + GRID; z <= HZ - size / 2 + 1e-9; z += GRID) cands.push({ x: -HX, z });
  } else {
    for (let z = -HZ + size / 2; z <= HZ - size / 2 + 1e-9; z += GRID) for (let x = -HX + size / 2; x <= HX - size / 2 + 1e-9; x += GRID) cands.push({ x, z });
  }
  let best = null, bestC = -Infinity;
  for (const c of cands) {
    const p = clampPos(mount, c, size), cl = clearance(p);
    if (cl >= -1e-9) return { x: r4(p.x), z: r4(p.z) };
    if (cl > bestC) { best = p; bestC = cl; }
  }
  const p = clampPos(mount, best ?? { x: 0, z: mount === 'wall' ? -HZ : 0 }, size);
  return { x: r4(p.x), z: r4(p.z) };
}

export function addDevice(layout, type, size = 0.5, sizeOf) {
  const t = typeOf(type);
  if (!t) return { layout, error: 'unknown' };
  if (layout.devices.length >= MAX_DEVICES) return { layout, error: 'full' };
  const used = new Set(layout.devices.map((d) => d.id));
  let n = 1; while (used.has(idFor(type, n))) n++;
  const id = idFor(type, n), p = freeSpot(layout, t.mount, size, sizeOf);
  const dev = { id, type, x: p.x, z: p.z, rot: t.mount === 'wall' ? wallRot(p) : 0 };
  return { layout: { ...layout, hub: { ...layout.hub }, devices: [...layout.devices.map((d) => ({ ...d })), dev] }, id };
}

const copy = (layout, devices) => ({ ...layout, hub: { ...layout.hub }, devices: devices.map((d) => ({ ...d })) });
export const removeDevice = (layout, id) => copy(layout, layout.devices.filter((d) => d.id !== id));
export const clearDevices = (layout) => copy(layout, []);

export function moveDevice(layout, id, pos, size = 0.5) {
  return copy(layout, layout.devices.map((d) => {
    if (d.id !== id) return d;
    const m = mountOf(d.type), p = clampPos(m, pos, size);
    return { ...d, x: p.x, z: p.z, rot: m === 'wall' ? wallRot(p) : d.rot };
  }));
}
export function moveHub(layout, pos) { const p = clampPos('floor', pos, HUB_SIZE); return { ...copy(layout, layout.devices), hub: { x: p.x, z: p.z } }; }
export function rotateDevice(layout, id) {
  return copy(layout, layout.devices.map((d) => (d.id === id && mountOf(d.type) !== 'wall' ? { ...d, rot: (d.rot + 90) % 360 } : d)));
}

export const devicesForSession = (layout) => layout.devices.map((d) => ({ type: d.type, id: d.id }));
export const sameDevices = (a, b) => a.devices.length === b.devices.length && a.devices.every((d, i) => d.id === b.devices[i].id && d.type === b.devices[i].type);

// A saved room from an older catalog or damaged storage must still load: bad entries are dropped, the room is capped, bad positions are re-placed.
export function loadLayout(storage) {
  let raw = null;
  try { raw = storage?.getItem(STORAGE_KEY) ?? null; } catch { raw = null; }
  let data = null;
  try { data = raw == null ? null : JSON.parse(raw); } catch { data = null; }
  if (!data || typeof data !== 'object' || Array.isArray(data) || data.v !== 1) return emptyLayout();
  const hubIn = data.hub && num(data.hub.x) && num(data.hub.z) ? data.hub : HUB_HOME;
  const hub = clampPos('floor', hubIn, HUB_SIZE);
  let L = { v: 1, hub: { x: hub.x, z: hub.z }, devices: [] };
  if (!Array.isArray(data.devices)) return L;
  const seen = new Set();
  for (const d of data.devices) {
    if (L.devices.length >= MAX_DEVICES) break;
    if (!d || typeof d !== 'object' || !typeOf(d.type) || !validId(d.type, d.id) || seen.has(d.id)) continue;
    seen.add(d.id);
    const m = mountOf(d.type);
    const pos = num(d.x) && num(d.z) ? clampPos(m, d, 0.5) : freeSpot(L, m, 0.5);
    const rot = m === 'wall' ? wallRot(pos) : (num(d.rot) ? ((Math.round(d.rot / 90) * 90) % 360 + 360) % 360 : 0);
    L = { ...L, devices: [...L.devices, { id: d.id, type: d.type, x: pos.x, z: pos.z, rot }] };
  }
  return L;
}

export function saveLayout(storage, layout) {
  try { storage?.setItem(STORAGE_KEY, JSON.stringify(layout)); } catch { /* storage unavailable: the room just won't be remembered */ }
}
