// Three.js setup shared by every scene: renderer, camera, controls, lights,
// post-processing, quality tiers, device picking, attention markers and the frame loop.
//
// Wall-hung parts. Room walls are single-sided planes (a dollhouse cut-away: from outside, a wall is see-through), so
// whatever hangs on a wall must hide with it or it floats in the air. Any Object3D with
//   userData.wall = { nx, nz, d }        (optional: ny, for a ceiling; span: [lo, hi])
// is visible only while nx*camera.x + ny*camera.y + nz*camera.z > d, i.e. while the camera is on the room side of that
// wall's plane: a back wall at z = zb facing +z is { nx: 0, nz: 1, d: zb }, a left wall at x = xl is { nx: 1, nz: 0, d: xl },
// a ceiling at y = h is { nx: 0, ny: -1, nz: 0, d: -h }. With span (the wall's extent along it: x for a back or front wall,
// z for a side wall), an object behind the wall plane (a window casing) also needs the line from the camera to it to
// cross the plane within the wall, so it doesn't show past the wall's end. The renderer owns .visible of every tagged
// object: it re-evaluates them each frame after the controls move the camera, right before rendering (and in step()).
// Tag a group to hide its children with it; to combine with a device's own on/off visibility, tag a parent and toggle
// the child. Lights are never tagged (the room's lighting must not change with the view). A hidden part still casts its
// shadow (closed blinds keep the sun off the rug seen from behind the house), no longer catches taps (a tap goes through
// to whatever is behind it), and a ring or label on it hides until it shows again.
//
// Lamp shades without shadows. A bulb inside a shade relies on its shadow to keep light off the wall behind the shade and
// the ceiling above a pendant. A point light may list the shade's openings, as seen from the bulb, in
//   userData.shadeCones = [{ dir: [x, y, z], angle, penumbra }]    (optional: userData.shadeReach, metres)
// Whenever it casts no shadow (the low tier), the renderer hides it and lights the room through one SpotLight per
// opening (aimed along dir in the light's parent space, half-angle `angle`), plus, with shadeReach, a point light that
// reaches no further than the shade so the shade still glows from inside. A downward opening's spot casts shadows (spot
// lights keep them on every tier), so the lamp doesn't light the lawn through the walls; an upward one lights only the
// ceiling above it. The stand-ins copy the light's colour, intensity, distance, decay and position every frame; scenes
// keep driving the point light as usual.
//
// Panning (right-drag, two fingers, arrow keys) moves the target and the camera together, and the target stays within
//   camOpts.panBounds = { x: [min, max], y: [min, max], z: [min, max] }
// (by default 2.5 m either side of the home target, and from 0.3 m up to 1.5 m above it), so no pan takes the view under
// the floor or off into empty space.
import './compat.js';
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { GTAOPass } from 'three/addons/postprocessing/GTAOPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js';

export { THREE };

export function detectQuality() {
  const touch = matchMedia('(pointer: coarse)').matches;
  const small = Math.min(innerWidth, innerHeight) < 700;
  const mem = navigator.deviceMemory || 8;
  return touch || small || mem <= 4 ? 'low' : 'high';
}

// camOpts: { position, target, fov, azimuth: [min, max] auto-sway arc, minDistance, maxDistance,
//            fitAspect: below this canvas aspect the vertical FOV widens so the horizontal framing is kept }
export function createRenderer(canvas, { quality = detectQuality(), camera: camOpts = {}, lockQuality = false } = {}) {
  const pr0 = Math.min(devicePixelRatio, quality === 'high' ? 2 : 1.5);
  // Anti-aliasing happens in the composer's multisampled target; the default framebuffer only gets a full-screen quad.
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: 'high-performance' });
  renderer.setPixelRatio(pr0);
  renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFShadowMap; // r186: PCFSoft was folded into PCF
  renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 0.85;

  const scene = new THREE.Scene(); scene.background = new THREE.Color(0xCFDDE6);
  const pmrem = new THREE.PMREMGenerator(renderer);
  scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture; scene.environmentIntensity = 0.35;

  const baseFov = camOpts.fov ?? 42;
  const camera = new THREE.PerspectiveCamera(baseFov, 1.5, 0.05, 80);
  camera.position.set(...(camOpts.position ?? [2.9, 1.75, 3.3]));
  const controls = new OrbitControls(camera, canvas);
  controls.target.set(...(camOpts.target ?? [-0.1, 0.85, -0.9]));
  controls.enableDamping = true; controls.autoRotate = true; controls.autoRotateSpeed = 0.25;
  controls.maxPolarAngle = Math.PI / 2 - 0.02; controls.minPolarAngle = 0.15; controls.minDistance = camOpts.minDistance ?? 1.5; controls.maxDistance = (camOpts.maxDistance ?? 7) * 1.6;
  controls.enablePan = true; controls.panSpeed = 0.8; controls.screenSpacePanning = true; controls.zoomSpeed = 0.9;
  // Arrow keys pan the room only while the room has focus (click it first), never while typing in the chat.
  canvas.tabIndex = 0; canvas.setAttribute('aria-label', '3D view of the room. Drag to orbit; with the room focused, arrow keys pan.');
  controls.listenToKeyEvents(canvas); controls.keyPanSpeed = 12;
  // Free orbit: walls are single-sided so any angle looks into the room (dollhouse cut-away). Auto-sway stays in a pleasant arc.
  const [azMin, azMax] = camOpts.azimuth ?? [-0.35, 1.3]; controls.update();
  const home = { pos: camera.position.clone(), target: controls.target.clone() };
  // Pan bounds (header). OrbitControls also calls update() from its own pointer and key handlers, so the bound lives in
  // update() itself: each update may not take the target further out than it was just before (a target that code put
  // outside, resetView or a QA view, stays where it is), and the camera moves with the target, keeping the view angle.
  const PB = camOpts.panBounds ?? { x: [home.target.x - 2.5, home.target.x + 2.5], y: [0.3, home.target.y + 1.5], z: [home.target.z - 2.5, home.target.z + 2.5] };
  const _was = new THREE.Vector3(), _pan = new THREE.Vector3(), orbitUpdate = controls.update.bind(controls);
  const lim = (v, was, [lo, hi]) => (v < lo && v < was ? Math.min(was, lo) : v > hi && v > was ? Math.max(was, hi) : v);
  controls.update = (...a) => { const T = controls.target; _was.copy(T); const moved = orbitUpdate(...a); _pan.copy(T); T.set(lim(T.x, _was.x, PB.x), lim(T.y, _was.y, PB.y), lim(T.z, _was.z, PB.z)); camera.position.add(_pan.subVectors(T, _pan)); return moved; };
  // Taking focus from a tap or drag (arrow keys pan the room; on phones it takes focus from the chat box) shows no ring:
  // that is for keyboard focus (Tab).
  canvas.addEventListener('pointerdown', () => { controls.autoRotate = false; canvas.focus({ preventScroll: true, focusVisible: false }); }, { passive: true });
  canvas.addEventListener('wheel', () => { controls.autoRotate = false; }, { passive: true });
  // A reset also stops what is left of a flick (damping); otherwise the view carries on orbiting from home.
  function resetView() { controls._sphericalDelta?.set(0, 0, 0); controls._panOffset?.set(0, 0, 0); if ('_scale' in controls) controls._scale = 1; camera.position.copy(home.pos); controls.target.copy(home.target); controls.autoRotate = true; controls.update(); }

  const hemi = new THREE.HemisphereLight(0xEAF2F8, 0x6E6258, 0.55); scene.add(hemi);
  const sun = new THREE.DirectionalLight(0xFFF1DC, 2.6); sun.position.set(2.2, 5.5, -7.5); sun.target.position.set(0.3, 0.3, 0.2); scene.add(sun.target); scene.add(sun);
  sun.castShadow = true; Object.assign(sun.shadow.camera, { left: -5, right: 5, top: 5.5, bottom: -4.5, near: 1, far: 24 });
  sun.shadow.bias = -0.00015; sun.shadow.normalBias = 0.015; sun.shadow.radius = 2;
  const fill = new THREE.DirectionalLight(0xDCE8F0, 0.35); fill.position.set(4, 3, 4); scene.add(fill);

  const msaa = quality === 'high' && devicePixelRatio < 1.5 ? 4 : 0;
  const composer = new EffectComposer(renderer, new THREE.WebGLRenderTarget(1, 1, { type: THREE.HalfFloatType, samples: msaa }));
  composer.setPixelRatio(pr0);
  composer.addPass(new RenderPass(scene, camera));
  const gtao = new GTAOPass(scene, camera, 1, 1); gtao.output = GTAOPass.OUTPUT.Default; gtao.blendIntensity = 0.85;
  gtao.updateGtaoMaterial({ radius: 0.35, distanceExponent: 1, thickness: 1, scale: 1.2, distanceFallOff: 1, screenSpaceRadius: false });
  // AO grain: with 12 samples (3 directions × 4 steps) the halos round cabinets, window casings and partition tops came
  // out speckled. On an ordinary drawing buffer 48 samples (5 × 10) take out about half the error, for under 1 ms more
  // on a mid-range GPU; on a big hi-dpi buffer 24 samples and a wider denoise keep the cost down (its grain is finer).
  let aoBig = null;
  function tuneAO(pixels) {
    const big = pixels > 1.2e6; if (big === aoBig) return; aoBig = big;
    gtao.updateGtaoMaterial({ samples: big ? 24 : 48 }); gtao.updatePdMaterial(big ? { radius: 12, samples: 24, rings: 3 } : { radius: 8, samples: 16, rings: 2 });
  }
  // Keep overlays (ping labels/rings, highlight shells, LED halos) out of the AO normal pre-pass: sprites have no
  // normals (NaN → black slab) and additive shells would cast AO halos. Anything with userData.noAO is skipped.
  const aoPrepass = gtao._overrideVisibility.bind(gtao);
  gtao._overrideVisibility = function () { aoPrepass(); this.scene.traverse((o) => { if (o.visible && (o.isSprite || o.userData.noAO)) { o.visible = false; this._visibilityCache.push(o); } }); };
  composer.addPass(gtao);
  const bloom = new UnrealBloomPass(new THREE.Vector2(1, 1), 0.3, 0.5, 0.92); composer.addPass(bloom);
  composer.addPass(new OutputPass());

  let currentQuality = null;
  // Point-light shadows cost six shadow renders each per frame; phones and slow machines go without them.
  // Only called at start() and on a tier change (castShadow changes recompile lit materials).
  const shades = []; // { light, spots }: point lights with userData.shadeCones and their shadowless stand-ins (header)
  function applyLightTier() {
    const shaded = [];
    scene.traverse((o) => {
      if (!(o.isPointLight || o.isSpotLight)) return;
      if (o.userData.wantsShadow === undefined) o.userData.wantsShadow = o.castShadow;
      o.castShadow = !!(o.userData.wantsShadow && (currentQuality === 'high' || o.isSpotLight));
      if (!o.castShadow && o.shadow.map) { o.shadow.map.dispose(); o.shadow.map = null; }
      if (o.isPointLight && o.userData.shadeCones?.length && o.parent) shaded.push(o);
    });
    for (const L of shaded) { // after the traverse: adding the stand-ins during it would visit them too
      let rec = shades.find((r) => r.light === L);
      if (!rec) {
        rec = { light: L, spots: L.userData.shadeCones.map(({ dir, angle, penumbra = 0.2 }) => {
          const sp = new THREE.SpotLight(L.color, 0, L.distance, angle, penumbra, L.decay);
          const down = dir[1] < 0; sp.castShadow = down; sp.userData.wantsShadow = down; sp.target.position.set(...dir); sp.add(sp.target); L.parent.add(sp);
          if (down) { sp.shadow.mapSize.set(512, 512); sp.shadow.camera.near = 0.05; sp.shadow.bias = -0.0005; sp.shadow.normalBias = 0.02; }
          return sp;
        }) };
        if (L.userData.shadeReach) { const near = new THREE.PointLight(L.color, 0, L.userData.shadeReach, L.decay); near.castShadow = false; near.userData.wantsShadow = false; near.userData.reach = L.userData.shadeReach; L.parent.add(near); rec.spots.push(near); }
        shades.push(rec);
      }
      const stand = !L.castShadow; L.visible = !stand; for (const sp of rec.spots) sp.visible = stand;
    }
    syncShades();
  }
  function syncShades() {
    for (const { light: L, spots } of shades) {
      if (!spots[0].visible) continue;
      for (const sp of spots) { sp.color.copy(L.color); sp.intensity = L.intensity; sp.distance = sp.userData.reach || L.distance; sp.decay = L.decay; sp.position.copy(L.position); if (sp.castShadow) { sp.shadow.autoUpdate = L.intensity > 0; if (!sp.shadow.map) sp.shadow.needsUpdate = true; } } // redrawn only while lit, but drawn once: a shadow sampler with no map fails every lit draw
    }
  }
  // Wall-hung parts (header): shown only while the camera is on the room side of their wall. Hidden, they still cast
  // shadows: three leaves invisible objects out of its shadow passes, so closed blinds let the sun and the lamp through
  // once the camera went round the back, and a window lost its casing's shadow. Each casting mesh under a tagged object
  // casts through a shadow-only twin in the scene root instead. A twin follows its mesh every frame (slats turn), hides
  // only when something other than the cut-away hides the mesh (a scene switching a part off), and its material is
  // visible only inside shadow passes, so it draws nothing in the room's own render; AO and picking skip it too.
  const _wp = new THREE.Vector3();
  const twinMats = new Map(), twins = [], twinned = new WeakSet();
  function twinOf(m) {
    const src = Array.isArray(m.material) ? m.material[0] : m.material;
    let mat = twinMats.get(src);
    if (!mat) { mat = new THREE.MeshBasicMaterial({ colorWrite: false, depthWrite: false, side: src.side, alphaMap: src.alphaMap || null, alphaTest: src.alphaTest || 0, map: src.alphaTest ? src.map || null : null }); mat.shadowSide = src.shadowSide ?? null; mat.visible = false; twinMats.set(src, mat); }
    const t = new THREE.Mesh(m.geometry, mat); t.castShadow = true; t.receiveShadow = false; t.matrixAutoUpdate = false; t.userData.noAO = true; t.raycast = () => {};
    scene.add(t); m.castShadow = false; twins.push({ m, t });
  }
  const shadowRender = renderer.shadowMap.render;
  renderer.shadowMap.render = function (...a) { for (const mt of twinMats.values()) mt.visible = true; try { return shadowRender.apply(this, a); } finally { for (const mt of twinMats.values()) mt.visible = false; } };
  function applyWalls() {
    const c = camera.position;
    scene.traverse((o) => {
      const w = o.userData.wall; if (!w) return;
      if (!twinned.has(o)) { twinned.add(o); o.traverse((m) => { if (m.isMesh && m.castShadow && !m.userData.isShell) twinOf(m); }); }
      const sc = w.nx * c.x + (w.ny || 0) * c.y + w.nz * c.z - w.d; let vis = sc > 0;
      if (vis && w.span) {
        o.getWorldPosition(_wp); const sp = w.nx * _wp.x + (w.ny || 0) * _wp.y + w.nz * _wp.z - w.d;
        if (sp < 0) { const k = sc / (sc - sp), u = Math.abs(w.nz) > Math.abs(w.nx) ? c.x + (_wp.x - c.x) * k : c.z + (_wp.z - c.z) * k; vis = u >= w.span[0] && u <= w.span[1]; }
      }
      o.visible = vis;
    });
    for (const { m, t } of twins) {
      let on = true; for (let a = m; a && on; a = a.parent) if (!a.userData.wall && !a.visible) on = false;
      t.visible = on; if (!on) continue;
      m.updateWorldMatrix(true, false); t.matrix.copy(m.matrixWorld); t.matrixWorldNeedsUpdate = true;
    }
  }
  // Drop the twins of meshes that left the scene (a device taken out of a room that is edited live): each twin sits in
  // the scene root, so without this it would stay there, hidden, for good. Cheap; called when something is removed.
  function pruneTwins() {
    for (let i = twins.length - 1; i >= 0; i--) {
      let a = twins[i].m; while (a.parent) a = a.parent;
      if (a === scene) continue;
      twins[i].t.removeFromParent(); twins.splice(i, 1);
    }
  }
  function setQuality(q) {
    if (q === currentQuality) return; const first = currentQuality === null; currentQuality = q;
    if (!first) applyLightTier();
    gtao.enabled = q === 'high';
    sun.shadow.mapSize.set(q === 'high' ? 4096 : 1536, q === 'high' ? 4096 : 1536);
    if (sun.shadow.map) { sun.shadow.map.dispose(); sun.shadow.map = null; }
    if (q !== 'high') for (const t of [composer.renderTarget1, composer.renderTarget2]) if (t.samples) { t.samples = 0; t.dispose(); }
    const pr = Math.min(devicePixelRatio, q === 'high' ? 2 : 1.5); renderer.setPixelRatio(pr); composer.setPixelRatio(pr);
    resize();
  }

  let topInset = 0; // CSS px from the canvas top covered by the room's own overlays (⟲ view, the hint pill)
  function measureInsets() {
    const cr = canvas.getBoundingClientRect(); topInset = 0;
    for (const el of canvas.parentElement?.children || []) { if (el === canvas) continue; const r = el.getBoundingClientRect(); if (r.height && r.top - cr.top < 60) topInset = Math.max(topInset, r.bottom - cr.top); }
  }
  function resize() {
    measureInsets();
    const w = canvas.clientWidth || 300, h = canvas.clientHeight || Math.round(w * 0.75);
    renderer.setSize(w, h, false); composer.setSize(w, h); camera.aspect = w / h; tuneAO(w * h * renderer.getPixelRatio() ** 2);
    const fit = camOpts.fitAspect;
    camera.fov = fit && camera.aspect < fit ? THREE.MathUtils.radToDeg(2 * Math.atan(Math.tan(THREE.MathUtils.degToRad(baseFov / 2)) * (fit / camera.aspect))) : baseFov;
    camera.updateProjectionMatrix();
  }
  const ro = new ResizeObserver(resize); ro.observe(canvas.parentElement);
  setQuality(quality);

  // ---- picking ----
  const pickables = []; const pickHandlers = [];
  const ray = new THREE.Raycaster(); const ptr = new THREE.Vector2(); let down = null;
  canvas.addEventListener('pointerdown', (e) => { down = [e.clientX, e.clientY]; });
  canvas.addEventListener('pointerup', (e) => {
    if (!down || Math.hypot(e.clientX - down[0], e.clientY - down[1]) > 6) return; down = null;
    const r = canvas.getBoundingClientRect(); ptr.set(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
    ray.setFromCamera(ptr, camera);
    // three's raycaster ignores .visible: leave out what the cut-away hides (a wall part seen from outside), but not a
    // deliberately invisible tap target (elder's motion sensor hit area) or a transparent one
    const hits = ray.intersectObjects(pickables.map((p) => p.obj), true).filter((h) => { for (let o = h.object; o; o = o.parent) if (o.userData.wall && !o.visible) return false; return true; });
    if (!hits.length) return;
    let o = hits[0].object; while (o && !o.userData.deviceId) o = o.parent;
    if (o) pickHandlers.forEach((fn) => fn(o.userData.deviceId));
  });
  // Register a tappable part for a device. The first part registered for an id (or one passed { anchor: true })
  // is where rings and labels appear.
  function addPickable(obj, id, { anchor = false } = {}) { obj.userData.deviceId = id; obj.traverse((c) => { c.userData.deviceId = id; }); const rec = { obj, id }; if (anchor) pickables.unshift(rec); else pickables.push(rec); }
  // Drop every tappable part registered for a device (one taken out of the room): taps, rings and labels skip it.
  function removePickable(id) { for (let i = pickables.length - 1; i >= 0; i--) if (pickables[i].id === id) pickables.splice(i, 1); }

  // ---- discovery / attention markers: an expanding ring plus a floating label at a device ----
  const markers = []; const box3 = new THREE.Box3(); const _bb = new THREE.Box3(); const tmp = new THREE.Vector3(); const ndc = new THREE.Vector3(); const viewPos = new THREE.Vector3();
  const ringGeo = new THREE.RingGeometry(0.42, 0.5, 48), dotGeo = new THREE.SphereGeometry(0.035, 12, 12);
  function anchorOf(id) {
    const p = pickables.find((q) => q.id === id); if (!p) return null;
    for (let o = p.obj.parent; o; o = o.parent) if (!o.visible) return null; // hung on a wall the camera is outside of
    p.obj.updateWorldMatrix(true, true); box3.makeEmpty();
    p.obj.traverseVisible((o) => { if (!o.geometry || o.userData.isShell || o.isSprite) return; if (!o.geometry.boundingBox) o.geometry.computeBoundingBox(); box3.union(_bb.copy(o.geometry.boundingBox).applyMatrix4(o.matrixWorld)); });
    if (box3.isEmpty()) return null; box3.getCenter(tmp); tmp.y = box3.max.y; return tmp.clone();
  }
  const LABEL_FONT = '600 44px Inter, "Segoe UI", system-ui, -apple-system, sans-serif';
  function labelSprite(text, color) {
    const c = document.createElement('canvas'); let g = c.getContext('2d'); g.font = LABEL_FONT;
    c.width = Math.ceil(g.measureText(text).width) + 116; c.height = 128; g = c.getContext('2d'); g.font = LABEL_FONT; // resizing resets the context
    g.fillStyle = 'rgba(18,34,42,0.92)'; g.beginPath(); g.roundRect(4, 24, c.width - 8, 80, 40); g.fill();
    g.fillStyle = color; g.beginPath(); g.arc(44, 64, 12, 0, Math.PI * 2); g.fill();
    g.fillStyle = '#fff'; g.textBaseline = 'middle'; g.fillText(text, 70, 64);
    const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace;
    const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: t, transparent: true, depthTest: false, depthWrite: false }));
    sp.userData.aspect = c.width / c.height; sp.userData.noAO = true; sp.renderOrder = 999; return sp;
  }
  function dropMarker(m) { for (const o of [m.ring, m.dot, m.label]) { if (!o) continue; scene.remove(o); o.material.map?.dispose(); o.material.dispose(); } }
  // A ring and label at a device; `kind` names what it says ('fault'), so unping(id, kind) can take back just that one.
  // `ring: false` leaves out the growing ring (a calm relabel beside another device's fault ring): label and dot only.
  // A device its wall hides right now still gets its marker: it shows if the device comes back into view while it lasts.
  function ping(id, text, { color = '#29EEE5', hex = 0x29EEE5, hold = 2600, kind = '', ring: withRing = true } = {}) {
    if (!pickables.some((q) => q.id === id)) return;
    const a = anchorOf(id);
    unping(id); // one marker per device
    const ring = new THREE.Mesh(ringGeo, new THREE.MeshBasicMaterial({ color: hex, transparent: true, opacity: 0.9, depthTest: false, depthWrite: false, side: THREE.DoubleSide })); ring.renderOrder = 998; ring.userData.noAO = true; scene.add(ring);
    const dot = new THREE.Mesh(dotGeo, new THREE.MeshBasicMaterial({ color: hex, depthTest: false })); dot.renderOrder = 998; dot.userData.noAO = true; scene.add(dot);
    const label = text ? labelSprite(text, color) : null; if (label) scene.add(label);
    if (a) { ring.position.set(a.x, a.y + 0.02, a.z); dot.position.copy(a); ring.visible = withRing; } else { ring.visible = dot.visible = false; if (label) label.visible = false; }
    markers.push({ id, ring, dot, label, kind, t0: performance.now(), hold, withRing });
  }
  function unping(id, kind) { for (let i = markers.length - 1; i >= 0; i--) if (markers[i].id === id && (!kind || markers[i].kind === kind)) { dropMarker(markers[i]); markers.splice(i, 1); } }
  const MARGIN_PX = 6, RING_MAX = 0.2, DOT_D = 0.07; // DOT_D: the dot's diameter in metres (dotGeo)
  function updateMarkers(now) {
    const cw = Math.max(1, canvas.clientWidth), ch = Math.max(1, canvas.clientHeight);
    const LABEL_PX = THREE.MathUtils.clamp(ch * 0.1, 30, 40), PILL_PX = LABEL_PX * 80 / 128 + 4; // ~10–14 px text
    const DOT_PX = THREE.MathUtils.clamp(ch * 0.018, 8, 13); // the dot never grows past this on screen, however close the camera
    const placed = []; // screen rects of labels already laid out this frame (oldest first), so newer ones step aside
    for (let i = 0; i < markers.length; i++) {
      const m = markers[i]; const age = (now - m.t0) / 1000; const k = (age % 1.1) / 1.1;
      const a = anchorOf(m.id); m.dot.visible = !!a; m.ring.visible = !!a && m.withRing !== false; if (m.label) m.label.visible = !!a;
      if (!a) continue; // its device is hidden (a wall part seen from outside): nothing to point at, and no label slot taken
      m.dot.position.copy(a); m.ring.position.set(a.x, a.y + 0.02, a.z); // follow moving devices
      // Sprite and ring size on screen follow view-space depth (not straight-line distance), so scale from depth.
      const pxAt = (pos) => { viewPos.copy(pos).applyMatrix4(camera.matrixWorldInverse); return (2 * Math.max(0.05, -viewPos.z) * Math.tan(THREE.MathUtils.degToRad(camera.fov / 2))) / ch; };
      // The pulse grows to 1.6 m across at most, and to no more than RING_MAX of the canvas height on screen.
      const ringMax = Math.min(1.6, RING_MAX * ch * pxAt(m.ring.position));
      m.ring.lookAt(camera.position); m.ring.scale.setScalar((0.2 + k * 1.4) * ringMax / 1.6); m.ring.material.opacity = (1 - k) * 0.9 * Math.min(1, (m.hold / 1000 + 0.6 - age) / 0.6);
      const dk = Math.min(1, (DOT_PX * pxAt(m.dot.position)) / DOT_D); m.dot.scale.setScalar(dk); // capped like the ring, so a close view never hides the device under a disc
      if (m.label) {
        // Constant on-screen size (LABEL_PX tall), sitting just above the dot, and pushed back inside the frame.
        let hgt = LABEL_PX * pxAt(m.dot.position);
        m.label.position.set(m.dot.position.x, m.dot.position.y + (DOT_D / 2) * dk + 0.45 * hgt, m.dot.position.z);
        ndc.copy(m.label.position).project(camera);
        m.label.material.opacity = age * 1000 > m.hold ? Math.max(0, 1 - (age * 1000 - m.hold) / 500) : 1; m.label.renderOrder = 1000 + i; // newer on top
        if (ndc.z < 1) {
          // Work in CSS pixels: clamp inside the canvas (below "⟲ view" and the hint pill), then take the nearest free
          // slot below or above the labels already placed. On a short phone canvas with no slot left, the older label
          // it lands on yields (fades right down) so the newest one stays readable.
          const w = LABEL_PX * m.label.userData.aspect, hw = w / 2 + MARGIN_PX, hh = PILL_PX / 2 + MARGIN_PX;
          const top = Math.min(ch / 2, Math.max(hh, topInset + 4 + PILL_PX / 2)), bottom = ch - hh, step = PILL_PX + 4;
          const sx = THREE.MathUtils.clamp((ndc.x + 1) / 2 * cw, hw, cw - hw), sy0 = THREE.MathUtils.clamp((1 - ndc.y) / 2 * ch, top, bottom);
          const under = (y) => placed.filter((r) => r.m.label.material.opacity >= 0.35 && Math.abs(r.x - sx) < (r.w + w) / 2 + 4 && Math.abs(r.y - y) < PILL_PX + 2);
          // A label that stepped aside keeps its slot while that slot stays free: once the older label it stepped away
          // from fades under 0.35 it no longer counts, and the newer one would jump back onto it mid-fade.
          let sy = null; const kept = m.slot ? sy0 + m.slot * step : null;
          if (kept !== null && kept >= top && kept <= bottom && !under(kept).length) sy = kept;
          for (let n = 0; n <= 12 && sy === null; n++) for (const c of n ? [sy0 + n * step, sy0 - n * step] : [sy0]) if (c >= top && c <= bottom && !under(c).length) { sy = c; break; }
          if (sy === null) { sy = sy0; for (const r of under(sy)) r.m.label.material.opacity = Math.min(r.m.label.material.opacity, 0.15); }
          m.slot = Math.round((sy - sy0) / step);
          placed.push({ x: sx, y: sy, w, m });
          ndc.x = (sx / cw) * 2 - 1; ndc.y = 1 - (sy / ch) * 2; m.label.position.copy(ndc.unproject(camera));
        }
        hgt = LABEL_PX * pxAt(m.label.position); m.label.scale.set(hgt * m.label.userData.aspect, hgt, 1);
      }
    }
    for (let i = markers.length - 1; i >= 0; i--) { const m = markers[i]; if ((now - m.t0) > m.hold + 600) { dropMarker(m); markers.splice(i, 1); } }
  }

  // ---- daylight helper for scenes with windows ----
  // After dusk the sky light drops to a low floor (about half the twilight level), so the room's own lamps are what
  // light it at night: a lamp, a 30% night light or a hall light that fails to come on all show. The sky itself goes
  // to a dark navy then (at 0.12 it stayed a slate blue, brighter than the dark room it showed through the window).
  function daylight(hour) {
    const h = ((hour % 24) + 24) % 24;
    const day = Math.max(0, Math.sin(((h - 6) / 12) * Math.PI));         // 0 at 6/18, 1 at noon
    const dawn = Math.exp(-Math.pow((h - 6) / 1.2, 2)), dusk = Math.exp(-Math.pow((h - 18.5) / 1.2, 2)) + dawn;
    const night = 1 - Math.min(1, day * 3 + dusk);                        // 0 by day and at dusk, 1 from about 21:00 to 04:30
    // Before sunrise the sky starts to lighten, but no sunlight comes in: the dawn part of the sun fades in from about
    // 04:40 to 05:40, so a room at 04:30 shows no sun stripes through its blinds.
    sun.intensity = 2.8 * day + 1.2 * (dusk - dawn * (1 - THREE.MathUtils.smoothstep(h, 4.6, 5.6)));
    sun.color.setHSL(0.08, 0.6, 0.62 + 0.3 * day - 0.15 * dusk);
    hemi.intensity = 0.15 + 0.45 * day - 0.08 * night; fill.intensity = 0.1 + 0.3 * day - 0.07 * night;
    if (scene.background?.isColor) scene.background.setHSL(0.6, 0.35, 0.12 + 0.62 * day + 0.2 * dusk - 0.07 * night);
    scene.environmentIntensity = 0.12 + 0.28 * day - 0.06 * night;
    const ang = ((h - 6) / 12) * Math.PI; sun.position.set(Math.cos(ang) * 6 + 1, 2 + Math.sin(ang) * 5, -7.5);
  }

  // ---- loop with fps watchdog ----
  // The watchdog ignores the first window (shader compile, texture uploads) and any window containing a long
  // frame gap (hidden tab, stall), and only downgrades during windows 2–4 of genuinely slow rendering.
  const frameFns = []; let running = false; let frames = 0, fpsStart = 0, lastFrame = 0, windows = 0;
  const t0 = performance.now(); const clock = { getElapsedTime: () => (performance.now() - t0) / 1000 }; const failed = new Set();
  const guard = (key, fn) => { try { fn(); } catch (e) { if (!failed.has(key)) { failed.add(key); console.error('[ncb] frame error in', key, e); } } };
  function loop() {
    if (!running) return;
    requestAnimationFrame(loop); // schedule first, so one bad frame can never stop the loop (and the store's tweens)
    const now = performance.now(); const t = clock.getElapsedTime();
    if (now - lastFrame > 1000) { frames = 0; fpsStart = now; } lastFrame = now;
    frameFns.forEach((fn, i) => guard(`frame#${i}`, () => fn(t, now)));
    guard('markers', () => updateMarkers(now));
    guard('render', () => {
      if (controls.autoRotate) { const az = controls.getAzimuthalAngle(); if (az <= azMin + 0.02 && controls.autoRotateSpeed > 0) controls.autoRotateSpeed *= -1; if (az >= azMax - 0.02 && controls.autoRotateSpeed < 0) controls.autoRotateSpeed *= -1; }
      controls.update(); guard('walls', () => { applyWalls(); syncShades(); }); composer.render();
    });
    frames++;
    if (now - fpsStart > 2500) { const fps = (frames * 1000) / (now - fpsStart); frames = 0; fpsStart = now; windows++; if (!lockQuality && windows >= 2 && windows <= 4 && fps < 38 && currentQuality === 'high') setQuality('low'); }
  }

  return {
    THREE, renderer, scene, camera, controls, lights: { hemi, sun, fill }, composer, gtao, bloom,
    addPickable, removePickable, onPick: (fn) => pickHandlers.push(fn), onFrame: (fn) => frameFns.push(fn), ping, unping, anchorOf, resetView,
    clearMarkers() { for (const m of markers) dropMarker(m); markers.length = 0; },
    daylight, setQuality, resize, pruneTwins, get twinCount() { return twins.length; }, get quality() { return currentQuality; },
    // Debug/QA: render one frame on demand (works while the tab is hidden and requestAnimationFrame is paused).
    step(n = 1) { for (let k = 0; k < n; k++) { const now = performance.now(); const t = clock.getElapsedTime(); frameFns.forEach((fn, i) => guard(`frame#${i}`, () => fn(t, now))); guard('markers', () => updateMarkers(now)); controls.update(); } guard('walls', () => { applyWalls(); syncShades(); }); composer.render(); },
    start() {
      running = true; applyLightTier(); resize();
      try { renderer.compile(scene, camera); } catch (e) { console.warn('[ncb] precompile skipped', e); }
      frames = 0; fpsStart = lastFrame = performance.now(); loop();
    },
    stop() { running = false; },
    dispose() {
      running = false; ro.disconnect();
      let dfg = null; scene.traverse((o) => { for (const m of [].concat(o.material || [])) dfg = dfg || renderer.properties.get(m).uniforms?.dfgLUT?.value || null; });
      controls.stopListenToKeyEvents?.(); controls.dispose();
      for (const m of markers) dropMarker(m); markers.length = 0; ringGeo.dispose(); dotGeo.dispose();
      scene.traverse((o) => {
        if (o.geometry) o.geometry.dispose();
        const mats = o.material ? (Array.isArray(o.material) ? o.material : [o.material]) : [];
        for (const m of mats) { for (const k of ['map', 'bumpMap', 'emissiveMap', 'roughnessMap', 'normalMap', 'alphaMap']) m[k]?.dispose?.(); m.dispose(); }
        if (o.shadow?.map) o.shadow.map.dispose();
      });
      scene.environment?.dispose?.(); pmrem.dispose();
      composer.dispose?.(); renderer.dispose(); renderer.forceContextLoss();
      dfg?.dispose(); // three shares one DFG LUT across renderers; the next renderer uploads it again
    },
  };
}
