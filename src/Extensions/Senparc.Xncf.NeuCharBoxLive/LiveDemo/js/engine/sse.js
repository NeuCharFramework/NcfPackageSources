// Live AI demo: the event stream (wire contract: docs/superpowers/specs/2026-11-07-live-api-contract.md).
// Read with fetch streaming (EventSource can't send the Authorization header). Reconnects with ?after=lastId.

// SSE text → events. Fields: id, event, data (lines joined with \n, parsed as JSON). Comment lines (": ping") are ignored.
export function createSseParser(onEvent) {
  let buf = '', id = null, event = 'message', data = [];
  const dispatch = () => {
    if (data.length) { try { onEvent({ id, event, data: JSON.parse(data.join('\n')) }); } catch { console.warn('live: bad event data', event); } }
    id = null; event = 'message'; data = [];
  };
  const line = (l) => {
    if (l === '') return dispatch();
    if (l[0] === ':') return;                       // comment / ping
    const i = l.indexOf(':'), f = i < 0 ? l : l.slice(0, i), v = i < 0 ? '' : l.slice(i + 1).replace(/^ /, '');
    if (f === 'id') id = Number(v); else if (f === 'event') event = v; else if (f === 'data') data.push(v);
  };
  return {
    push(t) { buf += t; let n; while ((n = buf.search(/\r\n|\r|\n/)) >= 0) { const m = buf.slice(n).match(/^(\r\n|\r|\n)/)[0]; if (m === '\r' && n + 1 === buf.length) break; line(buf.slice(0, n)); buf = buf.slice(n + m.length); } },
    end() { buf = ''; id = null; event = 'message'; data = []; },   // WHATWG: an event cut off by the end of the stream is dropped
  };
}

// Open the session's event stream and keep it open. onStatus: 'open' | 'retrying' | 'lost' | 'gone' (404) | 'unauthorized' (401).
// Events with id <= lastId are dropped, so a resume never repeats one. close() aborts the fetch and stops every timer;
// after it no callback runs.
export function openEventStream({ api, sessionId, after = 0, fetch: doFetch = globalThis.fetch, onEvent, onStatus, backoff = [500, 1000, 2000, 4000, 8000], idleMs = 45000 }) {
  let closed = false, lastId = after, ctl = null, retryTimer = null, k = 0;
  const status = (s) => { if (!closed) { try { onStatus?.(s); } catch (e) { console.warn('live: onStatus threw', e); } } };
  const deliver = (e) => {
    if (closed) return;
    if (e.id != null) { if (!(e.id > lastId)) return; lastId = e.id; }
    try { onEvent?.(e); } catch (err) { console.warn('live: onEvent threw', err); }
  };

  async function attempt() {
    const mine = ctl = new AbortController();
    let idle = null;
    const arm = () => { clearTimeout(idle); idle = setTimeout(() => mine.abort(), idleMs); }; // no bytes (not even a ping) for idleMs
    const parser = createSseParser(deliver);
    let final = 'retry';
    try {
      arm(); // also covers a connection that never answers
      const res = await doFetch(api.eventsUrl(sessionId, lastId || undefined), { headers: api.headers(), signal: mine.signal });
      if (closed) return;
      if (res.status === 404) final = 'gone';
      else if (res.status === 401) final = 'unauthorized';
      else if (res.ok && /^text\/event-stream\s*(;|$)/i.test(res.headers.get('content-type') || '') && res.body) {
        k = 0; status('open'); arm();
        const reader = res.body.getReader(), dec = new TextDecoder('utf-8');
        for (;;) {
          const r = await reader.read();
          if (closed || r.done) break;
          arm();
          parser.push(dec.decode(r.value, { stream: true }));
          if (closed) break;
        }
        parser.end();
      }
    } catch { /* network error, idle abort or close(): handled below */ }
    finally { clearTimeout(idle); }
    if (closed) return;
    if (final === 'gone' || final === 'unauthorized') return status(final);
    if (k >= backoff.length) return status('lost');
    status('retrying');
    if (closed) return;
    retryTimer = setTimeout(() => { retryTimer = null; if (!closed) attempt(); }, backoff[k++]);
  }
  attempt();

  return {
    close() { closed = true; clearTimeout(retryTimer); retryTimer = null; try { ctl?.abort(); } catch { /* already done */ } },
    get lastId() { return lastId; },
  };
}
