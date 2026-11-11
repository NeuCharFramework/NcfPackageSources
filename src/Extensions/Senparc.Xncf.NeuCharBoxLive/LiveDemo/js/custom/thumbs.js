// Inventory pictures: each catalog type rendered once from its own 3D model (the same model the room uses), as a PNG data URL.
//   createThumbs({ THREE, parts, makeModel? , makeCanvas? }) → { get(type) → Promise<string|null>, dispose() }
//     THREE   the three namespace; parts  js/engine/parts.js (materials(), parts(scene, M))
//     makeModel(type, { THREE, P, M }) → { root, update? }   default: DEVICE_UI[type].model(ctx)
// One offscreen WebGLRenderer (192 × 144, preserveDrawingBuffer) draws one type at a time, from idle callbacks (setTimeout 0
// where there is none), in a small scene of its own: a fixed three-quarter camera fitted to the model's bounding box and the
// room's daylight colours. Each type's geometry and own materials go as soon as it is drawn (the shared M stays). Pictures
// are cached by type. get() resolves null where WebGL is missing, and the tile keeps its icon.
// dispose() gives the GL context back (page left, or build mode closed for a while) and settles queued types with null;
// the next get() builds the context again, and cached pictures stay.
import { CATALOG } from './catalog.js';
import { DEVICE_UI } from './devices/index.js';

export const THUMB_W = 192, THUMB_H = 144;
const FOV = 30, DIR = [0.62, 0.42, 0.66]; // a three-quarter view from the front right, a little above
const idle = (fn) => (typeof requestIdleCallback === 'function' ? requestIdleCallback(fn, { timeout: 300 }) : setTimeout(fn, 0));

export function createThumbs({ THREE, parts, makeModel = (type, ctx) => DEVICE_UI[type].model(ctx), makeCanvas = null } = {}) {
  const cache = new Map();      // type → Promise<string|null> (settled with a picture; null results are dropped, so a later get retries)
  const queue = [];             // { type, resolve }
  let gl = null, M = null, env = null, running = false, unavailable = false, generation = 0;

  const canvasOf = makeCanvas || (() => {
    if (typeof document === 'undefined') return null;
    const c = document.createElement('canvas'); c.width = THUMB_W; c.height = THUMB_H; return c;
  });

  async function ensure() {
    if (gl) return true;
    if (unavailable) return false;
    try {
      const canvas = canvasOf(); if (!canvas) throw new Error('no canvas');
      const r = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true, preserveDrawingBuffer: true, powerPreference: 'low-power' });
      r.setPixelRatio(1); r.setSize(THUMB_W, THUMB_H, false); r.setClearColor(0x000000, 0);
      r.toneMapping = THREE.ACESFilmicToneMapping; r.toneMappingExposure = 0.85; r.shadowMap.enabled = false;
      canvas.addEventListener?.('webglcontextlost', (e) => { e.preventDefault?.(); release(); });
      gl = r; M = parts.materials();
      try { // the room's own image-based light (renderer.js), when the addon loads
        const { RoomEnvironment } = await import('three/addons/environments/RoomEnvironment.js');
        const pm = new THREE.PMREMGenerator(r); env = pm.fromScene(new RoomEnvironment(), 0.04); pm.dispose();
      } catch { env = null; }
      return !!gl;
    } catch { unavailable = true; release(); return false; }
  }

  function release() {
    generation++;
    const r = gl; gl = null;
    env?.dispose?.(); env = null;
    if (M) { for (const m of Object.values(M)) m?.dispose?.(); M = null; }
    if (r) { try { r.dispose(); r.forceContextLoss(); } catch { /* already gone */ } }
    while (queue.length) queue.shift().resolve(null);
  }

  function lights(scene) { // renderer.js's daylight() at 14:00
    const day = Math.sin(((14 - 6) / 12) * Math.PI);
    const hemi = new THREE.HemisphereLight(0xEAF2F8, 0x6E6258, 0.15 + 0.45 * day);
    const sun = new THREE.DirectionalLight(0xFFF1DC, 2.8 * day); sun.color.setHSL(0.08, 0.6, 0.62 + 0.3 * day); sun.position.set(2.5, 5.5, 4);
    const fill = new THREE.DirectionalLight(0xDCE8F0, 0.1 + 0.3 * day); fill.position.set(-4, 3, 4);
    scene.add(hemi, sun, sun.target, fill);
    if (env) { scene.environment = env.texture; scene.environmentIntensity = 0.12 + 0.28 * day; }
  }

  function draw(type) {
    const scene = new THREE.Scene(); lights(scene);
    const shared = new Set(Object.values(M).filter((m) => m && m.isMaterial));
    try {
      const P = parts.parts(scene, M);
      const model = makeModel(type, { THREE, P, M });
      if (!model?.root) return null;
      const t = CATALOG.types.find((x) => x.type === type);
      try { model.update?.({ ...(t?.initial || {}), status: 'online' }, 0, { hour: 14, sky: 1 }); } catch { /* draw it as built */ }
      scene.updateMatrixWorld(true);
      const box = new THREE.Box3(), mb = new THREE.Box3();
      model.root.traverse((m) => { if (!m.isMesh || m.userData.isShell || m.visible === false) return; if (!m.geometry.boundingBox) m.geometry.computeBoundingBox(); box.union(mb.copy(m.geometry.boundingBox).applyMatrix4(m.matrixWorld)); });
      if (box.isEmpty()) return null;
      const sphere = box.getBoundingSphere(new THREE.Sphere()), cam = new THREE.PerspectiveCamera(FOV, THUMB_W / THUMB_H, 0.02, 40);
      const vHalf = (FOV * Math.PI) / 360, hHalf = Math.atan(Math.tan(vHalf) * (THUMB_W / THUMB_H));
      const dist = (sphere.radius * 1.08) / Math.sin(Math.min(vHalf, hHalf));
      const dir = new THREE.Vector3(...DIR).normalize();
      cam.position.copy(sphere.center).addScaledVector(dir, dist); cam.lookAt(sphere.center);
      gl.render(scene, cam);
      return gl.domElement.toDataURL('image/png');
    } finally {
      scene.traverse((o) => {
        o.geometry?.dispose?.();
        for (const m of [].concat(o.material || [])) {
          if (shared.has(m)) continue;
          for (const k of ['map', 'bumpMap', 'emissiveMap', 'roughnessMap', 'normalMap', 'alphaMap']) if (!m[k]?.userData?.shared) m[k]?.dispose?.(); // kit.js's glow texture is shared with the room
          m.dispose?.();
        }
      });
      scene.clear();
    }
  }

  async function pump() {
    if (running) return; running = true;
    try {
      while (queue.length) {
        await new Promise((r) => idle(r));
        const job = queue.shift(); if (!job) continue;
        const gen = generation;
        let url = null;
        try {
          const fresh = !gl;
          if (!(await ensure())) { job.resolve(null); continue; }
          if (fresh) await new Promise((r) => idle(r)); // the context and its light take a task of their own; the first picture the next
          if (gen === generation) url = draw(job.type);
        } catch (e) { console.warn('[ncb] thumbnail failed for', job.type, e); }
        job.resolve(url);
      }
    } finally { running = false; if (queue.length) pump(); }
  }

  return {
    get(type) {
      if (cache.has(type)) return cache.get(type);
      if (unavailable || (!makeCanvas && typeof document === 'undefined')) return Promise.resolve(null);
      const p = new Promise((resolve) => queue.push({ type, resolve })).then((url) => { if (!url && cache.get(type) === p) cache.delete(type); return url; });
      cache.set(type, p); pump(); return p;
    },
    dispose: release,
  };
}
