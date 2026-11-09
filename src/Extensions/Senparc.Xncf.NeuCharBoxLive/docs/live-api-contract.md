<!-- Module copy of ncb/docs/superpowers/specs/2026-11-07-live-api-contract.md, with the automations additions of
2026-11-08-automations-design.md section 3 already folded in (the "Automations" section and the notes dated 2026-11-08).
Module edits: the 2026-11-09 notes (the module's /live/api branch, which replaces the tunnel-guard line of the standalone
host, and apply/access-check while the module isn't serving), and the standalone host's details brought up to the module
(the example address, the ngrok header, what live_off means, the work-time and model-call caps). The wire format is unchanged. -->

# Live AI demo: wire contract (server ⇄ demo)

Date: 2026-11-07. Pins the names that section 4.3 of `2026-11-07-live-ai-demo-design.md` left to
the builder, so the server plan and the demo plan build the same thing. If this file and a plan
disagree, this file wins; change it here first.

## Transport

- Base: `{apiBase}/live/api/` where `apiBase` comes from the demo's `js/live-config.js`
  (e.g. `https://ncf.example.com` for an NCF site, or `http://localhost:5000` for one on this machine). JSON bodies,
  UTF-8, `camelCase` field names.
- Invite-protected routes take `Authorization: Bearer <code>`. Codes are compared
  case-insensitively and with spaces stripped; the canonical form is `NCB-XXXX-XXXX`
  (alphabet `23456789ABCDEFGHJKMNPQRSTUVWXYZ`, 8 random characters).
- The demo may send `ngrok-skip-browser-warning: 1` (a leftover of the standalone host's ngrok tunnel, whose free tier
  otherwise answers with an HTML interstitial). The module accepts the header (it is in the allowed headers below) but
  never requires it.
- **CORS** (added by the demo plan): answer `OPTIONS` preflights for every route with
  `Access-Control-Allow-Origin` = the request's `Origin` when it is on the allowlist (spec 4.4),
  `Access-Control-Allow-Methods: GET, POST, OPTIONS`, `Access-Control-Allow-Headers:
  Authorization, Content-Type, Accept, ngrok-skip-browser-warning`, `Access-Control-Max-Age: 600`.
  A custom header (`Authorization`, or `ngrok-skip-browser-warning` when the demo sends it) makes even a `GET`
  preflighted. The SSE response carries the same `Access-Control-Allow-Origin`.
- Error body for every non-2xx: `{ "error": "<kind>", "message": "<plain sentence for the visitor>" }`.
  The demo shows its own copy per `error` kind; `message` is only a fallback for unknown kinds.

| HTTP | `error` kinds |
|---|---|
| 400 | `too_long` (text > 400 chars), `bad_request`, `bad_room` (`POST sessions` with scene `custom`: a missing or invalid device list), `bad_value` (`set`: the path isn't editable or the value is the wrong type or out of range; `advance`: `hours` isn't a number with 0 < hours ≤ 168). Message: "That value isn't allowed for this device." |
| 401 | `bad_code` (unknown or disabled code, or missing header) |
| 404 | `no_session` (unknown or expired session), `no_request`, `no_automation` (`automations/{id}`: no rule or schedule with that id in the room) |
| 409 | `busy` (a request is already active in this session), `not_waiting` (approve/decline/whatif when nothing is waiting for it), `device_unavailable` (`set`: the device is faulted or busy; message "That device can't be changed right now."). `advance` also answers `busy` while a request or another advance is running |
| 429 | `code_limit` (code's daily limit, or all 5 of the code's live sessions busy on `POST sessions`), `global_limit` (all-codes daily cap, or the server's session cap on `POST sessions`), `budget` (token kill switch), `rate` (apply or access/check rate limit; also `set`, `advance` and `automations/{id}`, 10 per 5 s per session together) |
| 503 | `live_off` (live is off: see the first server note; for `apply` and `access/check`, only while the module isn't serving (2026-11-09 notes) or its store can't be read) |
| 500 | `internal` (an unexpected server error; generic message, never a stack trace) |

Server notes (added by the server plan):
- Once today's token budget is spent, `POST sessions` and `POST …/requests` answer `429 budget`, and `GET health` reports `liveEnabled: false`. `503 live_off` is for live being off: switched off on the module's Settings page, no usable AI Kernel model (none chosen, deleted, or not a chat model on a supported platform), or the module not installed, closed or multi-tenant; also while its `NcbLive` settings can't work, its state can't be read, or the site isn't ready yet.
- `too_long` counts `text.Trim().Length`, in UTF-16 units like the browser's `maxlength`, against 400.
- A sensor read while running emits `reading`, then the usual `call` and `result` (outcome `reading`). While planning it emits `reading` only.
- Server-made labels and texts never contain the copy-guard terms: the blinds read `"Blinds → closed"`, not `"… 100% closed"`.
- `call.step` and `result.step` are `null` for a read or safe-state call made without a step (spec 3.5: a safe state needs no approval). The key is still sent. The demo shows such a pill as its own row ("made safe", or the reading), not nested under a step.
- Re-plan step ids are final when `replan` is emitted. An id already in the plan replaces that step; any other step gets the next free id `s{n+1}`, `s{n+2}`… (n = the plan's step count so far). Later `step`, `call` and `result` events use these ids. A merged plan never has more than 6 steps: the server refuses a re-plan that would pass that, and emits no `replan` for it.
- 2026-11-07, server final review: `POST sessions` answers `429 global_limit` when the server already holds its maximum number of live sessions; a code holds at most 5 live sessions, and opening a 6th ends that code's least-recently-active idle session (its tab then gets `404 no_session`). `POST access/check` is rate-limited per IP (`429 rate`). A wrong method on a live route answers `405` with a JSON body `{"error":"bad_request","message":…}`, never an empty body.
- 2026-11-07, server Task 10 review: added `500 internal` for unexpected server errors (generic message, no details).
- 2026-11-09, NCF module (`Senparc.Xncf.NeuCharBoxLive`): the standalone host's tunnel guard (clean `[A-Za-z0-9_-]` paths, GET/POST/OPTIONS only, from outside) is gone. The module adds a branch for `/live/api` and below only (the path match is case-insensitive; `/live/apix` is not in it), ahead of the host's routing, so the host's `Program.cs` needs no edit:
  - CORS is answered inside the branch from the origins of the runtime snapshot of that moment (the published demo, plus the extra origins from the Settings page), so a new origin works on the next request. No policy is registered with the host and the host's own CORS is untouched.
  - A preflight (`OPTIONS` with `Origin` and `Access-Control-Request-Method`) is answered `204` right there and goes no further: an allowed origin gets `Access-Control-Allow-Origin`, `-Methods`, `-Headers` and `-Max-Age`, and `Access-Control-Allow-Private-Network: true` when the request carried `Access-Control-Request-Private-Network: true` (Chrome's private network access, for the public demo calling a site on localhost or the LAN); any other origin gets none of them. Other requests with an allowed `Origin` get `Access-Control-Allow-Origin` when the response starts, errors and the SSE stream included.
  - A wrong method on a live route is a JSON `405` `bad_request` with the `Allow` header. A path under `/live/api` that no route matches is a JSON `404` `no_request`, never the host's page. A `HEAD` is left alone.
  - Nothing outside `/live/api` is touched. Protecting the site's admin and its other pages is the site's job (the NCF admin login does it for the module's own pages).
- 2026-11-09, NCF module: while the module isn't serving at all (not installed, closed, or NCF multi-tenancy on), `POST apply` and `POST access/check` answer `503 live_off` before anything else (honeypot, rate limit, body), so no visitor email or text is stored. For every other reason live is off (switched off, no model, a model missing or unsupported, settings that can't work, starting, unavailable) both stay open: applications keep coming in while the owner picks a model.
- 2026-11-07, server Task 9 review: `POST …/whatif` also answers `429 budget` when today's token budget is spent (what-ifs still don't count against the request limits). `reset` clears the session's replayable request, so a what-if after a reset answers `409 not_waiting`.
- 2026-11-07, client branch review: `whatif` targets the session's latest **replayable** request: the latest non-what-if request that ended `ok` or `failed` after an approved plan. A later request that ends `declined`, `cannot_do`, `expired` or `limit`, or a planning-only failure, does not replace it. Any other rid answers `409 not_waiting`. `fault.text` is the short reason only, without the device name (e.g. `"not responding"`); the demo prefixes the name.
- 2026-11-07, client plan pre-flight: a what-if never becomes the session's latest finished request, so `whatif` on the same `rid` replays that request again from its own `Before`. Declining a re-plan emits no event: the server tells the model "declined; carry on without these changes", the run continues, and the demo marks the re-plan card itself. `state` in `POST sessions`/`reset`/`room` is applied per field (see Routes). `done {limit}` shows the end card without the what-if.
- 2026-11-07, server plan pre-flight: added the two notes above (stepless calls, re-plan ids) and dropped the SSE rule's exception for `clock` events sent by `reset`. `reset` answers over HTTP and emits no event.
- 2026-11-07, real-model smoke: a model call that the per-request work time cuts off now ends `limit {kind:"time"}` then `done {outcome:"limit"}`, not `error {model}`; a `say` may come before it. The work time and the model-call caps per request and per what-if are configurable (`NcbLive:MaxWork`, `MaxCalls`, `MaxCallsWhatIf`); the module's defaults are 30 min, 60 and 60 (the standalone host had 3 min, 12 and 14; earlier 8 / 10 calls).
- 2026-11-08, custom scene (server Task 4): `GET health` lists `["home","custom"]`. `POST sessions` with scene `custom` takes `devices: [{type,id}]` and answers `400 bad_room` for an unknown type, a bad or duplicate id, a missing or empty list, or more than 20 devices. Rebuilding a room is a new session; the 6th session for a code still ends its least recently active idle one.

- 2026-11-08, editable cards (server Task 6): `POST sessions/{sid}/set` and `POST sessions/{sid}/advance` added (see Routes); new error kinds `bad_value` (400) and `device_unavailable` (409); `set` and `advance` share a rate limit of 10 per 5 s per session (`429 rate`). Events caused by an edit or an advance carry `"src":"edit"`; `timeline` rows gain optional `origin` and `device`.
- 2026-11-08, automations (server, spec `2026-11-08-automations-design.md` §3): `Automation` shape and the `automations` event added (see "Automations" below); `POST sessions` and `reset` answers and the `room` event gain `"automations"`; `timeline` rows with an `origin` gain `"automation"` (the rule's or schedule's id); new route `POST sessions/{sid}/automations/{id}` with error kind `no_automation` (404), sharing the `set`/`advance` rate limit. Two orderings change: a what-if's events now start `room`, `automations`, `plan`; and `reset`, which emitted no event, now emits one `automations` event (empty list, no `rid`, `"src":"edit"`) when it cleared automations.
- 2026-11-08, automations server review round: `Automation` gains `"done": bool` (always present; true for a one-off schedule whose time has passed). A switched-on below/above rule fires at the next tick that finds its value in the zone (no longer waits for it to leave first). Card text reads switches as what they do and names number fields (`When Floor lamp turns off`, `When Door camera detects motion`, `When Blinds closed is above 50%`). At most 20 automations per room (the AI's add past that is refused).

## Routes

| Method & path | Auth | Body → 2xx response |
|---|---|---|
| `GET health` | none | → `200 { "ok": true, "liveEnabled": bool, "scenes": ["home", "custom"] }` |
| `POST access/check` | none | `{ "code" }` → `200 { "ok": true, "remainingToday": int }`; bad code → 401. While the module isn't serving → 503 `live_off` (2026-11-09 note). |
| `POST apply` | none | `{ "email", "name"?, "interest": "home"\|"lab"\|"warehouse"\|"business"\|"other", "wouldTry", "scene", "website" }` → `202 {}`. `website` is the honeypot: non-empty → still `202`, nothing stored. 5 per IP per hour → 429 `rate`. While the module isn't serving → 503 `live_off` first (2026-11-09 note). |
| `POST sessions` | code | `{ "scene": "home" }` → `201 { "sessionId", "state": {…}, "clock": Clock, "suggestions": [string], "automations": [] }` (`automations` is always empty in a new room). `suggestions` = the scene manifest's "Try:" lines (spec 5.1), 2–4 of them (a custom room gives 1–4). For scene `custom` the body also carries `"devices": [{ "type", "id" }]` (1–20 devices from the catalog; ids are the type, then `_2`…`_20`); `state` then holds exactly those devices, and a missing or invalid list answers `400 bad_room`. |
| `GET sessions/{sid}/events?after={seq}` | code | SSE stream (below). `after` omitted = from the start. |
| `POST sessions/{sid}/requests` | code | `{ "text" }` → `202 { "requestId" }`. Counts against the code's and the global daily limits. |
| `POST sessions/{sid}/requests/{rid}/approve` | code | `{}` → `202 {}`. Answers whichever plan or re-plan card is waiting. |
| `POST sessions/{sid}/requests/{rid}/decline` | code | `{}` → `202 {}` |
| `POST sessions/{sid}/requests/{rid}/whatif` | code | `{ "device": "<deviceId>" }` → `202 { "requestId": "<new rid>" }`. Only for the session's latest finished request. Does not count against limits. |
| `POST sessions/{sid}/reset` | code | `{}` → `200 { "state": {…}, "clock": Clock, "automations": [] }` (reset clears every automation). Refused with 409 `busy` mid-request. |
| `POST sessions/{sid}/set` | code | `{ "path": "<device>.<field>", "value": number\|bool }` → `202 {}` (applied, or queued behind a running request or advance and applied there). Editable paths: `lamp.brightness` 0–1, `blinds.closed` 0–1, `soil.moisture` 0–100, `pump.running`, `camera.armed`, `camera.motion` (bool), `thermostat.target` 10–28, `thermostat.current` 5–35; nothing else, never `status`. Order of checks: code, session (`no_session`), rate (`429 rate`), body (`bad_request`: unreadable, not an object, missing `path` or `value`), `400 bad_value`, `409 device_unavailable`. Never calls the model, meters tokens or counts against a limit; not gated on live being on. The change streams as `state` events (and `timeline` rows if a rule fires) with `"src":"edit"`. |
| `POST sessions/{sid}/advance` | code | `{ "hours": number }` (0 < hours ≤ 168) → `200 { "clock": Clock }` once the gap has played. Same checks as `set`, then `400 bad_value` for `hours` out of range or not a number, and `409 busy` while a request or another advance is active. While it plays, a request, a what-if and a reset answer `409 busy`; an edit queues and applies at the current tick. Events (`clock`, `state`, `timeline`, `automations`) carry `"src":"edit"`. Same limits as `set`. |
| `POST sessions/{sid}/automations/{id}` | code | `{ "enabled": bool }` → `202 {}`: the automation card's on/off switch. Order of checks: code, session (`404 no_session`), rate (`429 rate`, the same 10 per 5 s per session as `set` and `advance`, checked before the body is read), body (`400 bad_request`: unreadable, not an object, `enabled` missing or not a boolean), `404 no_automation` (no rule or schedule with that id in the room). Never `409 busy`: allowed any time, mid-request and mid-advance too. Like `set` it is applied at once when the room is idle, else queued for the room's owner (a request applies it at its next wait, an advance at the current tick) and answered 202 once applied or after 2 s. An `automations` event follows (`"src":"edit"`; the running request's `rid` when one applied it), even when the switch was already in that position. Never calls the model, meters tokens or counts against a limit; not gated on live being on. |

`state` in `POST sessions` and `reset` is the whole scene state, keyed exactly as the demo
store's device slices: `{ "lamp": { "on": false, "brightness": 0, "status": "online" }, …,
"env": { "hour": 16.5 } }`. The browser applies it per field (`store.set(`<device>.<field>`, value)`) onto the post-setup snapshot, so room-only fields the server doesn't send (e.g. `soil.probeOut`) keep their values.

`Clock` = `{ "iso": "2026-11-13T16:30:00", "label": "Fri 16:30", "hour": 16.5 }`. `hour` is
`env.hour` (hours since the scene's start-day midnight, so Saturday 07:10 = 31.17).

## SSE framing

```
id: 17
event: state
data: {"path":"lamp.brightness","value":0.2,"ms":1200}

```

- `id` is a per-session sequence number starting at 1. Events are kept for the session's life,
  so `?after=17` replays 18 onwards. A comment line `: ping` every 15 s keeps the stream open.
- Every event's `data` also carries `"rid"` (the request it belongs to), except events caused by
  a hand edit or an advance outside a request: those carry no `rid`. Events they cause carry
  `"src":"edit"`; during a request an edit's events carry that request's `rid` and `"src":"edit"`.
  An edit's delayed `src:"edit"` frames made during a request can arrive after that request's `done`,
  still carrying its `rid`: clients apply every `src:"edit"` event whatever its `rid`.
  `reset` emits no event except one `automations` event (empty list, no `rid`, `"src":"edit"`) when the room
  had automations to clear: its HTTP response carries the state, clock and automations.
- The browser reads it with `fetch` streaming (it needs the `Authorization` header), not
  `EventSource`.

## Event kinds (`event:` → `data` fields, besides `rid`)

| Event | Data | Meaning |
|---|---|---|
| `room` | `{ "state": {…}, "clock": Clock, "automations": [Automation] }` | Replace the whole room, applied like the `POST sessions` state. Sent first in a what-if request (the room goes back to just before the replayed request, spec 3.4); `automations` is that room's list, with each one's on/off state then. An `automations` event with the same list follows it. |
| `automations` | `{ "items": [Automation] }` | The room's full automations list, in creation order (see "Automations"). Replace the section with it: the client always applies it directly (never through `holding`); the last one wins, by SSE id. |
| `planning` | `{}` | Planning started. |
| `reading` | `{ "device" }` | The model is reading a sensor (planning or running). Card shows WORKING. |
| `plan` | `{ "steps": [Step], "horizonHours": number\|null }` | Plan card. `Step` = `{ "id": "s1", "text": "Dim the floor lamp to 20%", "devices": ["lamp"] }`, at most 6 steps. |
| `awaiting_approval` | `{ "kind": "plan"\|"replan" }` | Approve / Not this are live now. |
| `cannot_do` | `{ "reason", "suggestions": [string] }` | Nothing planned. Ends the request (a `done` follows). |
| `declined` | `{}` | Visitor said Not this. Nothing ran (a `done` follows). |
| `expired` | `{}` | No answer for 10 minutes (a `done` follows). |
| `step` | `{ "id", "status": "running"\|"done"\|"failed"\|"skipped" }` | Checklist row status. |
| `call` | `{ "step", "callId", "device", "action", "args", "label" }` | A device call made. `label` = pill text, e.g. `"Floor lamp → 20%"`. Card shows WORKING. |
| `state` | `{ "path", "value", "ms" }` | Apply to the store: number with `ms > 0` → `store.tween(path, value, ms)`, else `store.set(path, value)`. `path` may be `<device>.status` (`"online"`, `"busy"`, `"fault"`). |
| `result` | `{ "callId", "step", "device", "ok", "outcome": "confirmed"\|"commanded"\|"failed"\|"reading"\|"made_safe", "text" }` | Ends a call: pill suffix (`"confirmed"`, `"34%"`, `"not responding"`). Card shows JUST SET for `confirmed`/`made_safe`. |
| `fault` | `{ "device", "text" }` | First failing call to a device in a what-if. Card turns red (a `state` for `<device>.status = "fault"` comes with it). |
| `clock` | `Clock` | Simulated clock moved. The browser sets `env.hour` itself from `hour` (tween over the gap since the last clock event, ≤ 800 ms) and shows `label` in the status line. |
| `timeline` | `{ "at": "Sat 18:00", "text": "Floor lamp → 35%", "ok": bool, "kind": "action"\|"notify"\|"fault" }` | A fast-forward row. `notify` rows show 📩. Optional `origin`: `"rule"` (a rule fired), `"schedule"` (a schedule ran) or `"held"` (a rule was held back by `max_per_day`), and optional `device` (the device the row is about). A row with an `origin` also carries `"automation"`: the id of the rule or schedule (an `Automation.id`), so its card can flash JUST RAN / HELD. |
| `replan` | `{ "steps": [Step], "reason" }` | Re-plan card (an `awaiting_approval {kind:"replan"}` follows). |
| `say` | `{ "delta" }` | Streamed summary text. |
| `summary` | `{ "text" }` | The summary, final and checked. Replaces the streamed text. |
| `done` | `{ "outcome": "ok"\|"declined"\|"cannot_do"\|"failed"\|"expired"\|"limit", "usage": { "promptTokens", "completionTokens", "calls" }, "remainingToday": int }` | Request over. Live end card after `ok` or `failed`; after `limit` the demo shows it without "What if something fails?". |
| `limit` | `{ "kind": "calls"\|"time"\|"budget" }` | The request hit a per-request bound or the kill switch mid-run (a `done {outcome:"limit"}` follows). |
| `error` | `{ "kind": "model"\|"internal", "message" }` | Something broke server-side (a `done {outcome:"failed"}` follows). |

Ordering guarantees: a `call` always precedes its `state` events, which precede its `result`.
Every `step running` precedes that step's calls. `done` is always the last event of a request.

**What-if requests** (added by the demo plan): the new `rid`'s events start with `room`, then
`automations` (the replayed room's list; added 2026-11-08), then a
`plan` carrying the approved steps of the replayed request (and any re-plan steps it approved),
with **no** `awaiting_approval`; the run follows at once. A re-plan the AI proposes during the
replay is asked as usual (`replan` + `awaiting_approval {kind:"replan"}`).

## Automations (added 2026-11-08)

`Automation` = `{ "id": "rule1"|"sched1", "kind": "rule"|"schedule", "text": string, "devices": [deviceId…], "enabled": bool, "firedToday": int, "done": bool }`.

- One per rule (`add_rule`) and schedule (`schedule`) in the room, in the order the AI made them (rules and schedules
  interleaved). Ids are the ones the AI's tools return. A re-plan's drop can free an id for reuse later. The visitor never
  sees ids, and the AI is told never to name one.
- A room holds at most 20 automations. The AI's `add_rule` or `schedule` past that is refused with a tool error, so a list
  never has more than 20 items.
- `text` is built server-side from the scene manifest (device names, the action's pill label), for Home and custom rooms
  alike. Examples:
  - `When Soil sensor is below 30% → Water pump: run 10 s`
  - `When Door camera detects motion → notify: Someone is at the door`
  - `When Floor lamp turns off → …`
  - `When Blinds closed is above 50% → Floor lamp: 40%`
  - `When Thermostat temperature is above 24 °C → Blinds: closed`
  - `Every day at 19:00 → Floor lamp → 35%`
  - `Mon, Wed, Fri at 07:00 → Blinds → open`
  - `Once, Sat 08:00 → Thermostat → 19 °C`
- How a rule's condition reads:
  - A switch reads as what it does: `turns on`/`turns off`, `detects motion`, `stops running`, `opens`, `locks`… Other
    switches read as `<field> turns on`.
  - A number names its field (`brightness`, `closed`, `temperature`…). The exception is a sensor's one number, which its
    unit already names (`Soil sensor is below 30%`).
  - A 0–1 card field reads as a percent. A number reached reads `reaches`; any other value reads `becomes`.
  - Texts pass the copy guard (a `100%` threshold reads `100 percent`).
- `devices`: a rule's watched device, then the device it acts on (if another; a notify rule has only the watched one). For a
  schedule, its acted-on device.
- `firedToday`: runs on the room's current sim day (`floor(env.hour / 24)`). For a rule this counts its fires (a failed fire
  counts, a held one doesn't); for a schedule, its runs. It drops back to 0 when the sim clock passes midnight.
- `done`: true only for a one-off schedule whose time has passed, whether it ran or its time went by while it was off. It
  never runs again; show it as done, not pending. It is false for every rule and daily schedule, and always present.
- The `automations` event is sent whenever the list or any `enabled`/`firedToday`/`done` changes:

  | Cause | `rid` | `src` |
  |---|---|---|
  | The AI adds one, or a re-plan drops one | the request's | none |
  | A rule or schedule fires, or a new sim day starts, during the AI's fast-forward | the request's | none |
  | The same during a clock advance | none | `"edit"` |
  | A hand edit fires a rule | the request's, mid-request | `"edit"` |
  | A reset clears them | none | `"edit"` |
  | A what-if swaps the room in | the what-if's | none |

  A switch (`automations/{id}`) always sends it, changed or not. Otherwise an unchanged list is not re-sent.
- **Off** never fires: not on a hand edit, not in a clock advance, not in the AI's fast-forward. An off rule also reports no
  fault for its sensor.
  - An off schedule's time passes without it. Switched back on, it runs at its next time; a one-off whose time passed
    doesn't catch up.
  - The AI's prompts list at most 20 automations (the newest), each as `on`, `off` or `done`. The AI is told not to set up
    one identical to one listed, that it may set up a changed version, and that it can't switch them itself.
- **Back on:** switching on fires nothing by itself.
  - A below/above rule whose value is already in its zone fires at the next fast-forward or clock-advance tick that finds it
    in the zone, like a just-made rule. The 6 h gap and `max_per_day` still apply.
  - A hand edit fires a rule only when it moves the value into the zone, not for an edit inside the zone.
  - A "becomes" rule keeps following the value while off and takes the current value as seen when switched on, so it
    doesn't fire for a value it already equals.
  - `firedToday` and the per-day cap are kept across off and on.
- **When a switch lands:** a switch made during a request lands at one of the runner's waits. A fast-forward's waits fall
  between its steps. A fault turn's model call or re-plan wait falls inside a step, but after that step's rules and
  schedules were evaluated. So a switch can come between a step's fires and its last rows, and a card that was just turned
  off can still flash JUST RAN for a fire from earlier in that step.

## Scene ids and state paths (home)

`lamp.on` bool, `lamp.brightness` 0–1 · `blinds.closed` 0–1 · `soil.moisture` % ·
`pump.running` bool · `camera.armed` bool, `camera.motion` bool · `thermostat.target` °C,
`thermostat.current` °C · `<device>.status` · `env.hour`. Other scenes reuse their own store keys
(`js/scenes/<scene>.js`, `devices.*.initial`) when they come online.
