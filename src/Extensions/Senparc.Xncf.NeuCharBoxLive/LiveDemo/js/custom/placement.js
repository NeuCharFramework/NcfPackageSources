// Build mode in the 3D room: drag a device (or the hub) to move it, tap it to select it. Pointer events, so mouse, pen
// and touch work alike.
//   createPlacement({ R, room3d, controller, canvas, selbar?, enabled? }) → { select(id), clear(), sync(layout), selected, dispose() }
// A pointerdown on a device's tap target turns the orbit controls off until its pointerup (the listener runs in the
// capture phase on the room, so OrbitControls never sees the press). Past a 6 px slop the device follows the pointer:
// a floor device and the hub over the floor plane, a wall device along the two back walls, a ceiling device over the
// ceiling plane (y = 2.8), keeping where it was grabbed. Each move goes through the controller (controller.move, or
// moveHub), at most once a frame, which clamps it inside the room and saves it. Over a plane, the pointer slides over
// the horizontal plane at the height where the device was grabbed (a lamp's shade stays under the pointer; the floor
// plane itself would make a high grab point run away with perspective). A press without movement selects the
// device and shows #selbar: its name, Rotate (not for wall mounts or the hub) and Remove (not for the hub). A tap on
// empty space clears the selection; a drag there orbits as usual. The room element carries .dragging while a drag runs.
// The pure helpers below (rays, planes, offsets, the selection bar's contents) take plain {x, y, z} objects; the QA page
// test/custom-build.html runs them.
import { typeOf, nameOf } from './catalog.js';
import { ROOM } from './layout.js';
import { CUSTOM_COPY as C } from './copy.js';

const HX = ROOM.w / 2, HZ = ROOM.d / 2;
export const TAP_SLOP = 6; // px, as the renderer's own tap test

// ---- pure helpers ----
// t along the ray o + t·d where it meets the plane {axis} = value, or null (parallel, or behind the origin).
export function rayPlane(o, d, axis, value) {
  if (Math.abs(d[axis]) < 1e-9) return null;
  const t = (value - o[axis]) / d[axis];
  return t > 1e-6 ? t : null;
}
const at = (o, d, t) => ({ x: o.x + d.x * t, y: o.y + d.y * t, z: o.z + d.z * t });

// Where a ray points for a device of this mount: { x, z } on the floor plane (the hub too) or the ceiling plane, or on
// the horizontal plane at height y when given (where the device was grabbed); for a wall mount { x, z, wall: 'back'|'left' }
// on whichever back wall it meets first inside the room, or else (the pointer over the floor or past a wall's end) the
// floor point with the nearer wall, which clampPos snaps to that wall.
export function hitOn(mount, o, d, y = null) {
  if (mount === 'wall') {
    let best = null;
    const tb = rayPlane(o, d, 'z', -HZ);
    if (tb !== null) { const p = at(o, d, tb); if (Math.abs(p.x) <= HX && p.y >= 0 && p.y <= ROOM.h) best = { t: tb, x: p.x, z: -HZ, wall: 'back' }; }
    const tl = rayPlane(o, d, 'x', -HX);
    if (tl !== null && (!best || tl < best.t)) { const p = at(o, d, tl); if (Math.abs(p.z) <= HZ && p.y >= 0 && p.y <= ROOM.h) best = { t: tl, x: -HX, z: p.z, wall: 'left' }; }
    if (best) return { x: best.x, z: best.z, wall: best.wall };
    const f = hitOn('floor', o, d);
    return f && { ...f, wall: Math.abs(f.x + HX) < Math.abs(f.z + HZ) ? 'left' : 'back' };
  }
  const t = rayPlane(o, d, 'y', Number.isFinite(y) ? y : mount === 'ceiling' ? ROOM.h : 0);
  if (t === null) return null;
  const p = at(o, d, t);
  return { x: p.x, z: p.z };
}

// The grab point's offset from the device, so the device doesn't jump to the pointer: both axes over a plane, along the
// wall for a wall mount (none when it was grabbed from the other wall's side of the corner).
export function grabOffset(mount, dev, hit) {
  if (mount !== 'wall') return { dx: dev.x - hit.x, dz: dev.z - hit.z };
  const wall = dev.rot === 90 ? 'left' : 'back';
  return { wall, along: hit.wall !== wall ? 0 : wall === 'left' ? dev.z - hit.z : dev.x - hit.x };
}
// The position to ask the controller for (it clamps it): the hit plus the grab offset.
export function dragTo(mount, hit, grab) {
  if (mount !== 'wall') return { x: hit.x + (grab?.dx ?? 0), z: hit.z + (grab?.dz ?? 0) };
  const along = grab && grab.wall === hit.wall ? grab.along : 0;
  return hit.wall === 'left' ? { x: hit.x, z: hit.z + along } : { x: hit.x + along, z: hit.z };
}
export const isTap = (a, b, slop = TAP_SLOP) => Math.hypot(b.x - a.x, b.y - a.y) <= slop;

// What the selection bar shows for an id in this layout, or null when it isn't there.
export function selection(layout, id) {
  if (id === 'hub') return { id, name: 'NeuCharBox', rotate: false, remove: false };
  const d = layout?.devices?.find((x) => x.id === id), t = d && typeOf(d.type);
  if (!t) return null;
  return { id, name: nameOf(d.type, d.id).name, rotate: t.mount !== 'wall', remove: true };
}

// ---- the page side ----
export function createPlacement({ R, room3d, controller, canvas, selbar = document.getElementById('selbar'), enabled = () => controller.mode === 'build' }) {
  const THREE = R.THREE, host = canvas.parentElement;
  const caster = new THREE.Raycaster(), ndc = new THREE.Vector2();
  selbar.innerHTML = `<span class="sel-name"></span><button class="btn small sel-rotate" type="button">${C.rotate}</button><button class="btn small sel-remove" type="button">${C.remove}</button>`;
  selbar.hidden = true;
  const nameEl = selbar.querySelector('.sel-name'), rotBtn = selbar.querySelector('.sel-rotate'), remBtn = selbar.querySelector('.sel-remove');
  const pointers = new Set();
  let drag = null, press = null, selected = null, pending = null, raf = 0, hoverRaf = 0, hoverEv = null;

  const mountOf = (id) => (id === 'hub' ? 'floor' : typeOf(controller.layout.devices.find((d) => d.id === id)?.type)?.mount ?? 'floor');
  const posOf = (id) => (id === 'hub' ? { ...controller.layout.hub, rot: 0 } : controller.layout.devices.find((d) => d.id === id) || null);
  function rayAt(e) {
    const r = canvas.getBoundingClientRect();
    ndc.set(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
    caster.setFromCamera(ndc, R.camera);
    return caster.ray;
  }
  // The device under the pointer: the nearest hit on a tap target that is showing (a wall device hides with its wall
  // when the camera goes round behind it; three's raycaster ignores .visible).
  function pick(e) {
    const ray = rayAt(e), owners = new Map();
    for (const id of room3d.pickIds()) { const o = room3d.objectFor(id); if (o) owners.set(o, id); }
    for (const h of caster.intersectObjects([...owners.keys()], true)) {
      let id = null, shown = true;
      for (let a = h.object; a; a = a.parent) { if (!a.visible && !a.userData.proxy) { shown = false; break; } if (id === null && owners.has(a)) id = owners.get(a); }
      if (shown && id) return { id, ray, point: h.point };
    }
    return { id: null, ray, point: null };
  }

  function show(s) {
    nameEl.textContent = s.name; rotBtn.hidden = !s.rotate; remBtn.hidden = !s.remove; selbar.hidden = false;
  }
  function select(id) {
    const s = selection(controller.layout, id);
    if (!s) return clear();
    selected = id; show(s); room3d.focus(id);
  }
  function clear() { selected = null; selbar.hidden = true; }
  rotBtn.onclick = () => { if (selected && selected !== 'hub') { controller.rotate(selected); room3d.focus(selected); } };
  remBtn.onclick = () => { if (selected && selected !== 'hub') { const id = selected; clear(); controller.remove(id); } };

  function flush() {
    raf = 0;
    if (!pending || !drag) return;
    const p = pending; pending = null;
    if (drag.id === 'hub') controller.moveHub(p); else controller.move(drag.id, p);
  }
  function endDrag(e, tapped) {
    if (raf) { cancelAnimationFrame(raf); raf = 0; }
    flush();
    const d = drag; drag = null;
    R.controls.enabled = true; host.classList.remove('dragging');
    try { if (canvas.hasPointerCapture?.(d.pointerId)) canvas.releasePointerCapture(d.pointerId); } catch { /* already released */ }
    if (tapped || d.moved) select(d.id);
  }

  function onDown(e) {
    if (e.target !== canvas) return;
    if (e.isPrimary) pointers.clear(); // a first finger (or the mouse): nothing else is down, whatever a lost pointerup left
    pointers.add(e.pointerId);
    if (drag || pointers.size > 1 || !enabled() || (e.pointerType === 'mouse' && e.button !== 0)) { press = null; return; }
    const { id, ray, point } = pick(e);
    press = { x: e.clientX, y: e.clientY, pointerId: e.pointerId, id };
    if (!id) return; // empty space: the orbit controls take it; a tap there clears the selection (onUp)
    const mount = mountOf(id), dev = posOf(id), planeY = mount === 'wall' ? null : Math.min(ROOM.h, Math.max(0, point.y));
    const hit = hitOn(mount, ray.origin, ray.direction, planeY);
    drag = { id, pointerId: e.pointerId, start: { x: e.clientX, y: e.clientY }, moved: false, mount, planeY, grab: hit && dev ? grabOffset(mount, dev, hit) : null };
    R.controls.enabled = false; canvas.style.cursor = ''; // the room's .dragging cursor, not the hover one
    try { canvas.setPointerCapture(e.pointerId); } catch { /* not capturable: the window listeners still end the drag */ }
  }
  function onMove(e) {
    if (!drag) { if (e.pointerType === 'mouse' && enabled()) { hoverEv = e; if (!hoverRaf) hoverRaf = requestAnimationFrame(hover); } return; }
    if (e.pointerId !== drag.pointerId) return;
    if (!drag.moved) {
      if (isTap(drag.start, { x: e.clientX, y: e.clientY })) return;
      drag.moved = true; host.classList.add('dragging');
    }
    const ray = rayAt(e), hit = hitOn(drag.mount, ray.origin, ray.direction, drag.planeY);
    if (!hit) return;
    if (!drag.grab) { const dev = posOf(drag.id); if (dev) drag.grab = grabOffset(drag.mount, dev, hit); }
    pending = dragTo(drag.mount, hit, drag.grab);
    if (!raf) raf = requestAnimationFrame(flush);
  }
  function onUp(e) {
    pointers.delete(e.pointerId);
    if (drag && e.pointerId === drag.pointerId) { endDrag(e, e.type === 'pointerup' && !drag.moved); press = null; return; }
    const p = press; press = null;
    if (p && !p.id && e.pointerId === p.pointerId && e.type === 'pointerup' && enabled() && isTap(p, { x: e.clientX, y: e.clientY })) clear();
  }
  // Desktop: a grab cursor over a device (one pick a frame at most).
  function hover() {
    hoverRaf = 0;
    const e = hoverEv; hoverEv = null;
    if (!e || drag || !enabled()) { canvas.style.cursor = ''; return; }
    canvas.style.cursor = pick(e).id ? 'grab' : '';
  }
  const onKey = (e) => { if (e.key === 'Escape' && selected) clear(); };
  const onLeave = () => { if (!drag) canvas.style.cursor = ''; };

  host.addEventListener('pointerdown', onDown, true);
  canvas.addEventListener('pointermove', onMove);
  addEventListener('pointerup', onUp, true);
  addEventListener('pointercancel', onUp, true);
  canvas.addEventListener('keydown', onKey);
  canvas.addEventListener('pointerleave', onLeave);

  return {
    select, clear,
    get selected() { return selected; },
    get dragging() { return !!drag; },
    // A layout change: the selected device may have gone (removed, cleared).
    sync(layout) { if (selected && !selection(layout, selected)) clear(); },
    dispose() {
      if (raf) cancelAnimationFrame(raf); if (hoverRaf) cancelAnimationFrame(hoverRaf);
      raf = hoverRaf = 0; drag = null; pending = null; R.controls.enabled = true; host.classList.remove('dragging');
      host.removeEventListener('pointerdown', onDown, true);
      canvas.removeEventListener('pointermove', onMove);
      removeEventListener('pointerup', onUp, true);
      removeEventListener('pointercancel', onUp, true);
      canvas.removeEventListener('keydown', onKey);
      canvas.removeEventListener('pointerleave', onLeave);
    },
  };
}
