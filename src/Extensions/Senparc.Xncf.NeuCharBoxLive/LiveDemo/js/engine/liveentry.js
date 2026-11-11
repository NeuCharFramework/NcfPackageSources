// The way into live mode from a scripted scene (spec 2026-11-08 §4), without the DOM: the scene header's "Live AI" button
// and the guided end cards' live button share it, so both do the same thing.
//   createLiveEntry({ live, flow, show, alive }) → { sync(), open() }
//     live   the scene's createLive, or null with no API base (js/live-config.js): then the button never shows.
//     flow   the guided request flow (js/engine/flow.js); open() calls flow.stop() first: a guided run stops where it is
//            (setup plays to its end at once), nothing guided stays pressable, and the room is back to right after setup.
//     show(on)  shows or hides the header button: on in every scene while live mode is not open, and not while open()
//            is getting there. Nothing is asked of the server before a press: live.open() reads /health then, and its
//            offline card says why when the NCF backend is missing, off, or can't run the scene. main.js calls sync()
//            from live's ui.availability hook (js/engine/live.js announces every change of available / active).
// The 6th scene ("Build your own") has no such button: its own gate and "Done building →" already open live mode.
export function createLiveEntry({ live, flow, show, alive = () => true }) {
  let opening = false;
  const sync = () => { if (alive()) show(!!live && !live.active && !opening); };
  async function open() {
    if (!live || live.active || opening || !alive()) return;
    opening = true; sync();
    try { await flow.stop(); } catch (e) { console.error(e); } finally { opening = false; } // live mode opens even so
    if (!alive()) return;
    if (!live.active) await live.open(); // its access card, or straight in with a saved code
    sync();
  }
  return { sync, open };
}
