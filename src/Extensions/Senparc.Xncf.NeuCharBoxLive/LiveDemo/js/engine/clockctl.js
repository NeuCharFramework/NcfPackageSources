// The clock control beside the status line (live mode): "🕓 Fri 16:30 · +1 h · +6 h · Set time…".
// `Set time…` opens a day + time picker; Go sends the hours from the clock's label to the chosen moment.
// The label is the server's ("Fri 16:30"), so the day and time are read from it. The clock only moves forward, by more than 0 and at most 168 h.
import { LIVE_COPY } from './live.js';

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const MAX_MIN = 168 * 60;
const parse = (label) => { const m = /^(Mon|Tue|Wed|Thu|Fri|Sat|Sun)[a-z]*\s+(\d{1,2}):(\d{2})/i.exec(String(label || '').trim()); return m ? { day: DAYS.findIndex((d) => d.toLowerCase() === m[1].slice(0, 3).toLowerCase()), min: Number(m[2]) * 60 + Number(m[3]) } : null; };
const pad = (n) => String(n).padStart(2, '0');

export function createClockCtl(el, { onAdvance }) {
  el.innerHTML = `<span class="clk-label"></span><span class="clk-why" hidden></span><button type="button" class="clk-btn" data-h="1">${LIVE_COPY.clockPlus1}</button><button type="button" class="clk-btn" data-h="6">${LIVE_COPY.clockPlus6}</button><button type="button" class="clk-btn clk-set" aria-expanded="false">${LIVE_COPY.clockSet}</button>
    <span class="clk-pick" hidden><select class="clk-day" aria-label="Day"></select><input class="clk-time" type="time" step="900" aria-label="Time"><button type="button" class="clk-btn clk-go">${LIVE_COPY.clockGo}</button></span>`;
  const $ = (s) => el.querySelector(s);
  const label = $('.clk-label'), why = $('.clk-why'), set = $('.clk-set'), pick = $('.clk-pick'), day = $('.clk-day'), time = $('.clk-time'), go = $('.clk-go'), fixed = [...el.querySelectorAll('[data-h]')];
  let cur = null, enabled = false, reason = null, active = false, text = null;

  // Minutes from now to the chosen day + time (null if the time field is empty).
  const minutes = () => { const m = /^(\d{2}):(\d{2})/.exec(time.value); return cur && m ? Number(day.value) * 1440 + Number(m[1]) * 60 + Number(m[2]) - cur.min : null; };
  const refresh = () => {
    const mins = minutes();
    el.hidden = !(active && text);
    // Disabled for a request: the reason is text on the row, in place of the buttons (a title never shows on touch, and a
    // disabled button can't be focused). Disabled for anything else (an advance, a reset): the buttons stay, greyed.
    const note = !enabled && reason ? reason : '';
    why.textContent = note; why.hidden = !note;
    for (const b of [...fixed, set]) { b.hidden = !!note; b.disabled = !enabled || (b === set && !cur); }
    go.disabled = !enabled || mins == null || mins <= 0 || mins > MAX_MIN;
    go.title = enabled && go.disabled ? 'Pick a time after the clock, within 7 days.' : '';
    day.disabled = time.disabled = !enabled;
  };
  const fillDays = () => {
    const keep = day.value; day.textContent = '';
    for (let k = 0; k <= 7; k++) { const o = document.createElement('option'); o.value = String(k); o.textContent = k === 0 ? `${DAYS[cur.day]} (today)` : DAYS[(cur.day + k) % 7]; day.appendChild(o); }
    day.value = keep || '0';
  };
  const open = (on) => { pick.hidden = !on; set.setAttribute('aria-expanded', String(on)); };
  set.onclick = () => {
    if (!pick.hidden) return open(false);
    if (cur) { const t = Math.min(cur.min + 60, 1439); day.value = '0'; time.value = `${pad(Math.floor(t / 60))}:${pad(t % 60)}`; } // an hour on, as a start
    open(true); refresh(); time.focus();
  };
  for (const b of fixed) b.onclick = () => { if (enabled) onAdvance(Number(b.dataset.h)); };
  go.onclick = () => { const mins = minutes(); if (go.disabled || mins == null) return; open(false); onAdvance(mins / 60); };
  day.onchange = time.oninput = time.onchange = refresh;

  return {
    // c: { label, enabled, reason } from the live controller
    render(c) {
      text = c?.label || null; enabled = !!c?.enabled; reason = c?.reason || null;
      const was = cur; cur = parse(text);
      label.textContent = text ? `🕓 ${text}` : '';
      if (cur) { if (!was || was.day !== cur.day) fillDays(); } else open(false);
      if (!enabled) open(false);
      refresh();
    },
    // Shown only in live mode with a clock to show.
    active(on) { active = !!on; if (!active) open(false); refresh(); },
  };
}
