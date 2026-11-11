## Senparc.Xncf.NeuCharBoxLive

NeuCharBox live AI demo as an XNCF module of the NCF host: invite-only live sessions where a visitor asks an AI to run a
simulated room (lamp, blinds, thermostat, ...), approves its plan, and watches the result stream in. The module is both
sides: the live API, and the demo's pages served by the site itself at `/live/demo/`. The public demo
(`https://leozheng265.github.io`) is the same pages, guided requests only.

### What it does

- **Live API** at `/live/api` (health, access check, apply, sessions, SSE events, requests, approve/decline, what-if, reset,
  hand edits, clock advance, automations). The wire contract is [docs/live-api-contract.md](docs/live-api-contract.md).
- **Demo pages** at `/live/demo/`: the demo's own pages (3D scenes, guided requests), with a "Try it live" button in
  every scene that talks to this site's `/live/api`. Same origin, so there is nothing to allow in the browser and it
  works on any address the site has. The files are the `LiveDemo/` folder, embedded in the module; it is a copy of the
  demo repo written by that repo's `node tools/sync-module.mjs <this folder>` and is never edited here.
- **Admin pages** inside the NCF admin (module menu "NeuCharBox Live", English and Simplified Chinese): Settings (the module's
  home), Codes, Applications, Runs.
- **Stores** in the NCF database: invite codes, applications, the run log and one settings row (tables
  `Senparc_NeuCharBoxLive_InviteCode`, `_Application`, `_Run`, `_Settings`). Migrations for Sqlite, SqlServer, MySql,
  PostgreSQL, Oracle and Dm are in `Domain/Migrations/`.
- **Models** come from AI Kernel: pick an AI Kernel chat model (NeuCharAI, AzureOpenAI, OpenAI or DeepSeek platform) on the
  Settings page. The module holds no API key of its own.
- Visitor text (request text, plan, summary) is blanked 30 days after a run; token, call and latency numbers stay.

The module hooks its own `/live/api` branch and the `/live/demo` files in from `UseXncfModule`, so the host's `Program.cs`
needs no edit. Under `/live/demo` only GET and HEAD for a file the demo has are answered. Under `/live/api`: CORS for the allowed origins (preflights answered there, including
`Access-Control-Allow-Private-Network`), a JSON `404 no_request` for an unknown live path and a JSON `405 bad_request` for a
wrong method. Everything else on the site, including its admin login, is the host's.

### Live is on only when

1. the module is installed and enabled in NCF (Install, then Enable), and multi-tenancy is off;
2. the Settings page has "Live AI is on" ticked (the default); and
3. a plan model is chosen and its AI Kernel model can be called.

Otherwise `GET /live/api/health` answers `{"ok":true,"liveEnabled":false,...}` and live requests answer `503 live_off`.
The Settings page says which of these is missing. The runtime re-reads the settings every 5 seconds and right after a save,
but reads nothing until NCF reports the site ready (a site in maintenance mode, or whose database isn't updated yet, keeps
live off as "starting"). On a site installed in this run NCF only reports ready after a restart; opening the Settings page
wakes the module instead (its successful read counts as ready), so no restart is needed.

While the module isn't serving at all (not installed, not enabled, or multi-tenancy on), `POST apply` and
`POST access/check` answer `503 live_off` too, so no visitor email or text is stored. For every other reason (switched
off, no model yet, ...) they stay open: applications keep coming in while you set things up.

### Settings page

| Field | Default | Meaning |
|---|---|---|
| Live AI is on | on | Master switch. Off stops new live requests; the demo falls back to its scripted mode. |
| Plan model | none | AI Kernel model that plans (required for live). |
| Run model | same as plan | AI Kernel model that runs the steps. |
| Requests per code per day | 20 | Limit a new invite code starts with. |
| Requests per day, all codes | 300 | Cap for every code together. |
| Daily token budget | 1,000,000 | Token kill switch for the usage day; at the limit new sessions and requests answer `429 budget`. |
| Extra allowed origins | none | One per line, `http(s)://host[:port]`, no path and no `*`. Added to the built-in ones (below). |

Built-in allowed origins: the published demo, `https://leozheng265.github.io`; in the Development environment only, also
`http://localhost:8765` (a local copy of the demo).

The page also shows the status, today's token spend, and a "Try it" link to the demo pages on this site (`/live/demo/`). If AI Kernel can't be
read (not installed or not enabled), the model pickers are empty and the page says so.

### Configuration (`NcbLive` section of appsettings)

Optional; every key has a default. On/off, the models, the daily limits and the origins are **not** here: they live on the
Settings page. Values that cannot work together (for example `SessionIdle` shorter than `MaxWork` plus `ApprovalWait`) are
logged as an error at startup and live stays off (`config`) until they are fixed. So is a value that can't be read at all
(for example `MaxWork` = `30m` instead of `00:30:00`): the error names the key, every `NcbLive` setting is at its default
until it is fixed, and the rest of the site is unaffected.

| Key | Default | Meaning |
|---|---|---|
| `NcbLive:PlanToolMode` | `required` | `required` makes the planning call pick a tool; `auto` lets it answer in text. |
| `NcbLive:MaxCalls` | 0 | Model calls per request; 0 = no limit (a request is never cut short for it). |
| `NcbLive:MaxCallsWhatIf` | 0 | Model calls per what-if; 0 = no limit. |
| `NcbLive:MaxChars` | 400 | Longest request text. |
| `NcbLive:MaxWork` | `00:00:00` | Server work time per request, approval waits excluded; zero = no limit. |
| `NcbLive:MaxCompletionTokens` | 1500 | Completion token cap per model call. |
| `NcbLive:ApprovalWait` | `00:00:00` | How long a plan waits for approval before it expires; zero = it waits for as long as it takes. |
| `NcbLive:SessionIdle` | `00:00:00` | Idle time after which a session is dropped; zero = never. With a timeout set, it must be longer than `MaxWork` plus `ApprovalWait`, and both of those must be set. |
| `NcbLive:HistoryTurns` | 6 | Earlier turns the model sees. |
| `NcbLive:MaxSessions` | 200 | Live sessions at once, all codes. At the cap, with no idle timeout, the least recently active idle session is ended to make room; `POST sessions` is `429 global_limit` only when every session is busy (or, with an idle timeout, whenever the cap is reached). |
| `NcbLive:AccessCheckPerMinute` | 30 | `POST access/check` calls per IP per minute. |
| `NcbLive:UsageTimeZone` | `UTC` | Time zone whose midnight resets the daily limits and the token budget. |
| `NcbLive:Pacing:IdleHourSeconds` | 0.1 | Real seconds per idle simulated hour in a fast-forward. |
| `NcbLive:Pacing:EventSeconds` | 0.6 | Real seconds per timeline row in a fast-forward. |
| `NcbLive:Pacing:WeekCapSeconds` | 25 | Most a simulated week may take. |

#### Upgrading from the standalone host

The standalone live host (`ncb/live`) read more `NcbLive` keys. The module ignores these, and names any still present in one
warning at startup: `Enabled`, `PlanModel`, `RunModel`, `PerCodeDaily`, `GlobalDaily`, `DailyTokenBudget` and
`ExtraOrigins` are now on the Settings page, and `Endpoint` (with its key) is the AI Kernel model's. The usage day also
moves from New York to UTC: set `NcbLive:UsageTimeZone` to `America/New_York` to keep the old midnight.

### Trying it

1. In the site's admin, install and enable AI Kernel first (the model pickers read it), and add a chat model there. Then
   install "NeuCharBox Live" (Install, then Enable) and choose the model on the module's Settings page.
2. On the Codes page, create an invite code.
3. Open the "Try it" link on the Settings page (`/live/demo/` on this site), press "Try it live" in the Home scene and
   enter the code.

Live AI runs the Home scene and "Build your own" today. In the other scenes the button says the scene isn't live yet;
without a chosen model, or with the module not enabled, it says live is off and points here.

The public demo can still be pointed at a `http://localhost` or `http://127.0.0.1` site with the `?liveApi=` address
parameter (that origin is on the built-in allowed list, and the module answers the browser's private-network
preflight), but the site's own `/live/demo/` needs none of that.

### Layout

- `Register.cs`, `Register.Area.cs`, `Register.Database.cs`: module registration, admin menu, database.
- `Api/`: the HTTP side (controller, SSE, `/live/api` pipeline, CORS, error bodies, retry handler for model calls, the
  `/live/demo` files).
- `LiveDemo/`: the demo's pages, copied from the demo repo (see "What it does").
- `Engine/`: the live engine (scenes, simulator, rules and schedules, sessions, AI request runner, access and limits). It has
  no dependency on ASP.NET Core or NCF; the runtime and stores are plugged in from the module.
- `Domain/`: entities, migrations, and services (settings, EF stores, module runtime, AI Kernel clients, retention).
- `Areas/Admin/Pages/NeuCharBoxLive/`: Settings, Codes, Applications, Runs.
- `Resources/`: admin text (neutral English, `en`, `zh-CN`).

Scaffolded with `dotnet new XNCF` (Senparc.Xncf.XncfBuilder.Template 0.14.0), then trimmed. Uninstalling keeps the tables, so
codes sent out work again after a reinstall.
