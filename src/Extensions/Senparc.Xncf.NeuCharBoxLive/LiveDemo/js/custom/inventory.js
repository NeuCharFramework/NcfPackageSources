// The custom scene's device inventory (build mode): a search box, category chips and one tile per catalog type, in
// category order (CATALOG.categories), then catalog order. A tile adds a device of its type (onAdd); it shows ✓ while
// the room has one and "new" for a type no other scene has. Under the tiles: the room's count, Clear room (two-step:
// the first click asks, for 3 s; onClear on the second) and, while the room is empty, a note (into `note` when the
// page gives one, e.g. over the room; else at the foot of the inventory).
// On phones (css/custom.css) the inventory is a bottom sheet: collapsed, the search and one row of tiles; open, 60vh.
// The grip button and typing in the search open it.
//   createInventory(root, { onAdd(type), onClear(), inRoom: () => Set<type>, count: () => n, note? })
//     → { refresh(), setThumb(type, url), open(on), dispose() }
// Every tile is a single <button>: nothing interactive inside it (the QA page checks).
import { CATALOG } from './catalog.js';
import { CUSTOM_COPY as C } from './copy.js';
import { DEVICE_UI } from './devices/index.js';
import { icon } from '../engine/icons.js';

const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const CONFIRM_MS = 3000;

// The catalog's types in tile order: by category (the catalog's own list), then as the catalog lists them.
export function tileOrder(catalog = CATALOG) {
  const rank = (c) => { const i = catalog.categories.indexOf(c); return i < 0 ? catalog.categories.length : i; };
  return catalog.types.map((t, i) => ({ t, i })).sort((a, b) => rank(a.t.category) - rank(b.t.category) || a.i - b.i).map(({ t }) => t);
}
// A type matches a search when every word of it is in its name or its category (any case).
export function matches(t, query) {
  const words = String(query || '').toLowerCase().split(/\s+/).filter(Boolean);
  const hay = `${t.name} ${t.category}`.toLowerCase();
  return words.every((w) => hay.includes(w));
}

export function createInventory(root, { onAdd, onClear, inRoom = () => new Set(), count = () => 0, note = null } = {}) {
  const types = tileOrder(), byType = new Map(types.map((t) => [t.type, t]));
  const cats = CATALOG.categories.filter((c) => types.some((t) => t.category === c));
  root.innerHTML = `
    <div class="inv">
      <div class="inv-head">
        <button class="inv-grip" type="button" aria-expanded="false" aria-label="${esc(C.sheetOpen)}"><span aria-hidden="true"></span></button>
        <input class="inv-search" type="search" placeholder="${esc(C.search)}" aria-label="${esc(C.search.replace(/…$/, ''))}" autocomplete="off" spellcheck="false" enterkeyhint="search">
      </div>
      <div class="inv-cats" role="group" aria-label="Categories">
        <button class="inv-cat" type="button" data-cat="" aria-pressed="true">${esc(C.all)}</button>${cats.map((c) => `<button class="inv-cat" type="button" data-cat="${esc(c)}" aria-pressed="false">${esc(c)}</button>`).join('')}
      </div>
      <div class="inv-grid">${types.map((t) => `<button class="inv-tile" type="button" data-type="${esc(t.type)}" data-cat="${esc(t.category)}" title="Add ${esc(t.name)}"><span class="pic" aria-hidden="true">${icon(DEVICE_UI[t.type]?.icon)}</span><span class="nm">${esc(t.name)}</span><span class="cat">${esc(t.category)}</span>${t.new ? `<span class="new">${esc(C.isNew)}</span>` : ''}<span class="in" aria-hidden="true">✓</span><span class="sr"></span></button>`).join('')}
        <p class="inv-nomatch" hidden>${esc(C.noMatch)}</p>
      </div>
      <div class="inv-foot"><span class="inv-count" role="status"></span><button class="inv-clear" type="button"></button></div>
      ${note ? '' : '<p class="inv-empty" hidden></p>'}
    </div>`;
  const $ = (s) => root.querySelector(s);
  const inv = $('.inv'), search = $('.inv-search'), grip = $('.inv-grip'), clearBtn = $('.inv-clear'), countEl = $('.inv-count'), noMatch = $('.inv-nomatch');
  const tiles = [...root.querySelectorAll('.inv-tile')], chips = [...root.querySelectorAll('.inv-cat')];
  const noteEl = note || $('.inv-empty');
  let cat = '', confirmUntil = 0, confirmTimer = 0;

  function filter() {
    const q = search.value; let shown = 0;
    for (const b of tiles) {
      const on = (!cat || b.dataset.cat === cat) && matches(byType.get(b.dataset.type), q);
      b.hidden = !on; if (on) shown++;
    }
    noMatch.hidden = shown > 0;
  }
  function setCat(c) {
    cat = c; for (const b of chips) b.setAttribute('aria-pressed', String(b.dataset.cat === c));
    filter();
  }
  function open(on) {
    inv.classList.toggle('open', !!on); root.classList.toggle('open', !!on); // the root is the sheet on phones (css/custom.css)
    grip.setAttribute('aria-expanded', String(!!on)); grip.setAttribute('aria-label', on ? C.sheetClose : C.sheetOpen);
  }
  function disarm() { clearTimeout(confirmTimer); confirmTimer = 0; confirmUntil = 0; }
  // Cheap to call on every layout change (a drag moves the room once a frame): it touches only what changed.
  let shownKey = null;
  function refresh() {
    const have = inRoom(), n = count(), arm = n > 0 && confirmUntil > performance.now();
    if (!n) disarm();
    const key = `${n}|${arm}|${[...have].sort().join(',')}`;
    if (key === shownKey) return;
    shownKey = key;
    for (const b of tiles) {
      const has = have.has(b.dataset.type);
      if (b.classList.contains('has') !== has) { b.classList.toggle('has', has); b.querySelector('.sr').textContent = has ? ' (in your room)' : ''; }
    }
    countEl.textContent = C.count(n);
    clearBtn.textContent = arm ? C.clearConfirm(n) : C.clear;
    clearBtn.classList.toggle('confirm', arm); clearBtn.disabled = !n;
    noteEl.hidden = n > 0;
    if (!n) noteEl.innerHTML = `<span class="wide">${esc(C.empty)}</span><span class="narrow">${esc(C.emptyBelow)}</span>`;
  }

  search.addEventListener('input', () => { filter(); if (search.value) open(true); });
  search.addEventListener('keydown', (e) => { if (e.key === 'Escape' && search.value) { search.value = ''; filter(); e.stopPropagation(); } });
  grip.onclick = () => open(!inv.classList.contains('open'));
  for (const b of chips) b.onclick = () => setCat(b.dataset.cat);
  for (const b of tiles) b.onclick = () => onAdd?.(b.dataset.type);
  clearBtn.onclick = () => {
    if (!count()) return;
    if (confirmUntil > performance.now()) { disarm(); onClear?.(); refresh(); return; }
    confirmUntil = performance.now() + CONFIRM_MS; clearTimeout(confirmTimer);
    confirmTimer = setTimeout(() => { confirmTimer = 0; confirmUntil = 0; refresh(); }, CONFIRM_MS);
    refresh();
  };

  filter(); refresh();
  return {
    refresh,
    open,
    // A rendered picture of the type (js/custom/thumbs.js, Task 10) in place of its icon.
    setThumb(type, url) {
      const b = tiles.find((x) => x.dataset.type === type); if (!b || !url) return;
      const pic = b.querySelector('.pic'), img = document.createElement('img');
      img.alt = ''; img.decoding = 'async'; img.src = url; pic.replaceChildren(img); pic.classList.add('img');
    },
    dispose() { disarm(); },
  };
}
