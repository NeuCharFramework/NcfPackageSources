// Live AI demo: the API client (wire contract: docs/superpowers/specs/2026-11-07-live-api-contract.md; the automations
// route: 2026-11-08-automations-design.md §3).
// Every request sends ngrok-skip-browser-warning; protected routes send the invite code as a Bearer token.
// The code is never put in a URL and never logged.

export class LiveError extends Error {
  constructor(status, kind, message) {
    super(message || 'Live AI hit a problem. Please try again.');
    this.name = 'LiveError'; this.status = status; this.kind = kind;
  }
}

export function createLiveApi({ base, fetch: doFetch = globalThis.fetch, getCode = () => null, timeoutMs = 10000 } = {}) {
  const root = `${String(base || '').replace(/\/+$/, '')}/live/api/`;
  const isJson = (res) => /^application\/json\s*(;|$)/i.test(res.headers.get('content-type') || '');

  // One call: its own AbortController and timer (setTimeout, so test clocks apply). Throws LiveError.
  async function call(method, path, { body, auth = true, ms = timeoutMs } = {}) {
    const ctl = new AbortController();
    let timer, timedOut = false;
    const timeout = new Promise((_, rej) => { timer = setTimeout(() => { timedOut = true; ctl.abort(); rej(new LiveError(0, 'network')); }, ms); });
    const headers = { 'ngrok-skip-browser-warning': '1' };
    if (auth) headers.Authorization = `Bearer ${getCode() ?? ''}`;
    const init = { method, headers, signal: ctl.signal };
    if (body !== undefined) { headers['Content-Type'] = 'application/json'; init.body = JSON.stringify(body); }
    try {
      const run = (async () => {
        let res;
        try { res = await doFetch(root + path, init); } catch { throw new LiveError(0, 'network'); }
        if (!isJson(res)) throw new LiveError(res.status, 'bad_response');
        let data;
        try { data = await res.json(); } catch { throw new LiveError(res.status, 'bad_response'); }
        if (!res.ok) throw new LiveError(res.status, typeof data?.error === 'string' ? data.error : 'bad_response', typeof data?.message === 'string' ? data.message : undefined);
        return data;
      })();
      run.catch(() => {}); // a late failure after a timeout is not unhandled
      return await Promise.race([run, timeout]);
    } catch (e) {
      if (timedOut) throw new LiveError(0, 'network');
      throw e;
    } finally { clearTimeout(timer); }
  }
  const enc = encodeURIComponent;
  const req = (sid, rid, action, body = {}) => call('POST', `sessions/${enc(sid)}/requests/${enc(rid)}/${action}`, { body }).then(() => {});

  return {
    async health() {
      const off = { ok: false, liveEnabled: false, scenes: [] };
      try {
        const d = await call('GET', 'health', { auth: false, ms: 4000 });
        if (d?.ok !== true) return off;
        return { ok: true, liveEnabled: d.liveEnabled === true, scenes: Array.isArray(d.scenes) ? d.scenes : [] };
      } catch { return off; }
    },
    check: (code) => call('POST', 'access/check', { body: { code }, auth: false }).then((d) => ({ remainingToday: d.remainingToday })),
    apply: (form) => call('POST', 'apply', { body: form, auth: false }).then(() => {}),
    createSession: (scene, extra = {}) => call('POST', 'sessions', { body: { scene, ...extra } }),
    request: (sid, text) => call('POST', `sessions/${enc(sid)}/requests`, { body: { text } }),
    approve: (sid, rid) => req(sid, rid, 'approve'),
    decline: (sid, rid) => req(sid, rid, 'decline'),
    whatIf: (sid, rid, device) => call('POST', `sessions/${enc(sid)}/requests/${enc(rid)}/whatif`, { body: { device } }),
    set: (sid, path, value) => call('POST', `sessions/${enc(sid)}/set`, { body: { path, value } }),
    advance: (sid, hours) => call('POST', `sessions/${enc(sid)}/advance`, { body: { hours }, ms: 40000 }),
    automation: (sid, id, enabled) => call('POST', `sessions/${enc(sid)}/automations/${enc(id)}`, { body: { enabled } }),
    reset: (sid) => call('POST', `sessions/${enc(sid)}/reset`, { body: {} }),
    eventsUrl: (sid, after) => `${root}sessions/${enc(sid)}/events${after ? `?after=${Number(after)}` : ''}`,
    headers: () => ({ Authorization: `Bearer ${getCode() ?? ''}`, 'ngrok-skip-browser-warning': '1', Accept: 'text/event-stream' }),
  };
}
