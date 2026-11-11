// The custom scene's mode controller: gate → build → run. A pure module (no DOM): the page gives it storage, a
// makeLive(scene, devices) that builds a createLive with sessionExtra: () => ({ devices }), and a small ui sink.
// Moving or rotating devices changes nothing on the server; adding or removing starts a fresh live session at Done.
// Removing a device the session has always does, even when the same type is added back: the page drops a removed
// device's state (mount.js), so the old session could no longer show it.
import { addDevice, removeDevice, moveDevice, moveHub, rotateDevice, clearDevices, devicesForSession, sameDevices, loadLayout, saveLayout } from './layout.js';
import { sceneFor } from './scene.js';
import { DEVICE_UI } from './devices/index.js';

const sizeOf = (t) => DEVICE_UI[t]?.size;

export function createCustomController({ storage, makeLive, ui }) {
  let layout = loadLayout(storage), mode = 'gate', live = null, liveLayout = null, dropped = false;

  const changed = () => !!live && (dropped || !sameDevices(layout, liveLayout));
  const inSession = (id) => !!liveLayout?.devices.some((d) => d.id === id);
  const setMode = (m) => { mode = m; ui.mode(m); };
  // A layout change: remember it, show it, and say once that the conversation will restart.
  function commit(next, { devices = false, drops = false } = {}) {
    const was = changed();
    if (drops) dropped = true;
    layout = next; saveLayout(storage, layout); ui.layout(layout);
    if (devices && !was && changed()) ui.notice('changed');
  }
  // Building waits for a request or a clock advance to end; an access card waiting for a code is not one (the room stays as built).
  const canBuild = () => !(live && live.active && (live.advancing || !['idle', 'off', 'access'].includes(live.phase)));

  return {
    get mode() { return mode; },
    get layout() { return layout; },
    get live() { return live; },
    get canBuild() { return canBuild(); },
    get changedSinceRun() { return changed(); },
    add(type) {
      const r = addDevice(layout, type, DEVICE_UI[type]?.size, sizeOf);
      if (r.error === 'full') { ui.notice('full'); return null; }
      if (r.error) return null;
      commit(r.layout, { devices: true });
      return r.id;
    },
    remove(id) { if (layout.devices.some((d) => d.id === id)) commit(removeDevice(layout, id), { devices: true, drops: !!live && inSession(id) }); },
    move(id, pos) {
      const d = layout.devices.find((x) => x.id === id);
      if (d) commit(moveDevice(layout, id, pos, DEVICE_UI[d.type]?.size));
    },
    moveHub(pos) { commit(moveHub(layout, pos)); },
    rotate(id) { commit(rotateDevice(layout, id)); },
    clear() { if (layout.devices.length) commit(clearDevices(layout), { devices: true, drops: !!live && layout.devices.some((d) => inSession(d.id)) }); },
    gated() { if (mode === 'gate') setMode('build'); },
    async done() {
      if (!layout.devices.length) return;
      // Only moves and rotations since the last run, or a device added and taken out again: the same conversation stays.
      if (live && live.active && !changed()) { setMode('run'); return; }
      live?.close(); // its stream ends before the new session is made
      const next = makeLive(sceneFor(layout), devicesForSession(layout));
      live = next; liveLayout = layout; dropped = false;
      setMode('run');
      await next.open();
    },
    toBuild() {
      if (!canBuild()) { ui.notice('buildBusy'); return false; }
      setMode('build');
      return true;
    },
    teardown() { live?.close(); live = null; liveLayout = null; dropped = false; },
  };
}
