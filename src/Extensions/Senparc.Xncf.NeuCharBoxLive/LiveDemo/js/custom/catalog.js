// The device catalog (a generated copy of the server's, see tools/sync-catalog.mjs) and the id / name rule shared with the server.
import data from './catalog.data.js';

export const CATALOG = data;
export const MAX_DEVICES = data.maxDevices ?? 20;

const BY_TYPE = new Map(data.types.map((t) => [t.type, t]));
export const typeOf = (type) => BY_TYPE.get(type) ?? null;

// {type} for the first device of a type, {type}_{n} (n 2-20, no zero padding) for the rest.
export const idFor = (type, n) => (n <= 1 ? type : `${type}_${n}`);

// The number n encoded in an id of this type (1 for the bare type), or 0 when the id is not valid for it.
export function numberOf(type, id) {
  if (!BY_TYPE.has(type) || typeof id !== 'string') return 0;
  if (id === type) return 1;
  if (!id.startsWith(`${type}_`)) return 0;
  const tail = id.slice(type.length + 1);
  if (!/^[1-9][0-9]*$/.test(tail)) return 0;
  const n = Number(tail);
  return n >= 2 && n <= MAX_DEVICES ? n : 0;
}
export const validId = (type, id) => numberOf(type, id) > 0;

// The server's CustomRoom.NameOf: the catalog name and ref, plus " {n}" from the second device on.
export function nameOf(type, id) {
  const t = typeOf(type); if (!t) return null;
  const n = numberOf(type, id) || 1;
  return n === 1 ? { name: t.name, ref: t.ref } : { name: `${t.name} ${n}`, ref: `${t.ref} ${n}` };
}
