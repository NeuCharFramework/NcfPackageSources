// Device cards: decides what control a tile shows, from the device's own state, so scenes need no extra wiring.
// A bar if the state has one of BAR_FIELDS (first match wins), else a switch if it has one of SWITCH_FIELDS, else none (text only).
// A scene can override per device with `control`: false (none) · { kind: 'bar', field, min, max } · { kind: 'switch', field } · a function (slice, state) => control | null.
export const BAR_FIELDS = ['brightness', 'closed', 'open', 'speed'];
export const SWITCH_FIELDS = ['on', 'armed', 'running', 'pressed', 'active'];

const num = (v) => (Number.isFinite(v) ? v : 0); // null, NaN and text read as 0
const clamp01 = (v) => Math.min(1, Math.max(0, v));
const bar = (slice, field, min = 0, max = 1) => ({ kind: 'bar', field, value: clamp01((num(slice[field]) - min) / (max - min)) });

export function controlFor(device, slice, state) {
  if (!slice || typeof slice !== 'object') return null;
  const c = device && device.control;
  if (c === false) return null;
  if (typeof c === 'function') return c(slice, state);
  if (c && c.kind === 'bar') return bar(slice, c.field, c.min, c.max);
  if (c && c.kind === 'switch') return { kind: 'switch', field: c.field, on: !!slice[c.field] };
  const b = BAR_FIELDS.find((f) => f in slice);
  if (b) return bar(slice, b);
  const s = SWITCH_FIELDS.find((f) => f in slice);
  return s ? { kind: 'switch', field: s, on: !!slice[s] } : null;
}

// Hand edits: what a device card lets the visitor change. A scene lists them on the device as `edit: [{ field, kind: 'slider'|'switch', min, max, step, scale, unit, label, top? }]`
// (`top`: a percent slider's readout at 100, default "full"; see panel.js editValueText).
// Returns one entry per field, in that order, with the live value in store units (`scale` is only for showing it, e.g. 100 for a 0–1 value shown as a percent).
export function editsFor(id, device, slice) {
  const list = device && device.edit;
  if (!Array.isArray(list) || !slice || typeof slice !== 'object') return [];
  return list.map((e) => ({ kind: e.kind, path: `${id}.${e.field}`, value: slice[e.field], min: e.min, max: e.max, step: e.step, scale: e.scale ?? 1, unit: e.unit, label: e.label, top: e.top }));
}
