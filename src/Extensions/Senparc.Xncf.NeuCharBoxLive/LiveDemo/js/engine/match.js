// Typed requests → authored prompts. The demo is scripted, so it never guesses. A request runs only when it clearly
// matches one chip and asks for nothing that chip doesn't do: no other numbers or units ("15 mm", "5 W" vs 5.0 mW), no
// other device, axis or lane ("M1", "pitch", "cart B"), nothing the chip rules out ("a camera" vs "without a camera"),
// no state its plan undoes ("keep the mic muted" vs "Mic unmuted at the switch to live"), and no stop or switch-off
// unless the chip is itself a stop that switches that very thing off. A negated request ("don't go live") is refused
// rather than turned into its opposite, and so is a request for the chip's opposite ("close the shutter"). Anything
// else gets the chips back, with the closest request named when there is one. main.js and the tests use interpret().
//
// Routing fields a prompt in js/scenes/*.js may carry, besides chip and steps (all optional):
//   keywords  more words that pick the prompt. A phrase is read like typed text: 'turn off' is one word, 'turnoff'.
//   avoid     words that mean the visitor wants something else: one of them in the text, not negated there, scores the
//             prompt 0. An entry of several words is a phrase: its words in that order, in one clause, at most two words
//             apart once stop words are dropped ('boil kettle': "boil the kettle", not "the kettle might boil dry"; 'still
//             asleep': "is she still asleep", not "…still on when she's asleep"). A particle or only/just in it may also
//             stand at the other end ('put up': "put the sign up"; 'video only': "only video"). Exact words: 'falls'
//             doesn't catch 'fall'.
//   rulesOut  what the prompt won't do, besides what its chip already rules out ("…no stream, no sign"): named in the
//             text and not negated, the prompt can't run ('partial'), and it can't make a tie on words the text rules
//             out ("go live without the sign" is no tie with "Recording only — …no sign"). An entry of several words is
//             a phrase: its words in that order, with no punctuation between them, at most two words apart once stop
//             words are dropped, none of them negated ('blinds night': "close the blinds at night", "…the blinds when it
//             gets dark" with 'blinds dark'; not "I'm away for a few nights"). Exact words, as in avoid.
//   touches   plan-like lines for a prompt whose actions live in fn steps ("Scanner disarmed"): read like its plan
//             lines by the plan-conflict and stop checks, so "stop everything except the carts" asks back.

const STOP = new Set('a an the i my me it is to and or of in on at for with be please can you make let while when if do dont not this that its im keep like she her tell'.split(' '));
const URL_RE = /\b(?:https?:\/\/|www\.)\S+/g;

export function tokens(text) {
  return String(text).toLowerCase()
    .replace(URL_RE, ' ')
    .replace(/[’']/g, '')
    .replace(/(^|[^\d.])\.(\d)/g, '$10.$2').replace(/°[cf]\b/g, '°')          // ".05" → "0.05", "21°C" → "21°"
    .replace(/[^a-z0-9°.\s]/g, ' ')
    .replace(/\.(?!\d)/g, ' ').replace(/(^|\s)\./g, ' ')                        // sentence dots, keep decimals
    .replace(/\b(turn|switch|shut)\s+((?:[a-z0-9]+\s+){0,2}?)(on|off)\b/g, '$1$3 $2') // "turn the sign off" → "turnoff sign"
    .replace(/\bgo\s+live\b/g, 'golive')
    .split(/\s+/).filter((w) => w.length > 1 && !STOP.has(w));               // one letter ("lane B", "stage X") never decides
}

// Typed text: lower case, URLs gone, at most 4000 characters. "Shut the dock" (a door, lid, gate or shutter) is "close
// the dock", not a stop.
// "12,4 mm" is 12.4 mm (a decimal comma; "1,000 W" stays a thousand). "Kick off the stream" is a start, not an "off";
// "light it up" switches lights on: it is no "wrap up". "Stop everything but the carts" (or "…except for the carts",
// "turn everything off but the carts") leaves the carts out, like "…except the carts"; "…but keep/leave/don't…" is
// another clause. "She's in bed" / "gets in bed" is getting into bed (not "still in bed"): "into" says so.
const norm = (t) => String(t ?? '').slice(0, 4000).trim().toLowerCase().replace(/’/g, "'").replace(URL_RE, ' ')
  .replace(/(\d),(\d{1,2})(?![\d,])/g, '$1.$2')
  .replace(/\b(she's|he's|shes|hes|is|are|gets|get|got|getting)\s+in\s+bed\b/g, '$1 into bed')
  .replace(/\bshut(?=\s+(?:(?:the|my|that|this|her)\s+)?(?:dock|docks|door|doors|shutter|gate|gates|blinds?|lid|window|windows|curtains?)\b)/g, 'close')
  .replace(/\bkick(?:s|ed|ing)?\s+(?:it\s+|things\s+)?off\b/g, 'start')
  .replace(/\blights?\s+(?:(?:it|them|everything|the\s+(?:room|place|studio))\s+)?up\b/g, 'lights on')
  .replace(/\b(everything|all|anything|every\s+[a-z]+)(\s+(?:off|down|out))?\s*,?\s+(?:but|except)(?:\s+for)?\s+(?!not\b|don'?t\b|do\s+not\b|keep\b|leave\b|let\b)/g, '$1$2 except ');
const words = (t) => t.replace(/['’]/g, '').split(/[^a-z0-9]+/).filter(Boolean);
const sameWord = (a, b) => a === b || (a.length > 3 && b.length > 3 && (a.startsWith(b) || b.startsWith(a)));
const NEGATOR = /^(?:no|not|without|never|dont|nothing|zero|isnt|arent|wasnt|doesnt|didnt|wont|cant|cannot|shouldnt|wouldnt|couldnt|mustnt|neither|nor)$/;
const NOSCORE = /^(?:no|not|without|never|none|nothing|just|only|now|today|tonight|ok|okay|pls|plz|thanks|thank|yes|yeah|yep|sure|fine|again|too|also|then|asap|quickly|immediately)$/; // what not to do, how much, when: never picks a chip
const NEG_GAP = /^(?:turn|switch|the|a|an|on|any)$/;
// "Leave the heating alone" rules the heating out, like "don't touch the heating"; so does "leave the rest" (or
// "everything else", "the others").
const LEAVE_ALONE = /\bleave\s+((?:the|my|her|his|a|an|our|their)\s+)?([a-z0-9-]+(?:\s+[a-z0-9-]+)?)\s+alone\b/g;
const LEAVE_REST = /\bleave\s+(the\s+rest|everything\s+else|the\s+others?|all\s+the\s+others?)\b/g;
// "Close" as nearness or care ("gets close to the door", "a close eye", "closely") closes nothing.
const nearNotClose = (t) => t.replace(/\bclosely\b/g, 'carefully').replace(/\b(comes?|came|coming|gets?|got|getting|up|too|so|very|stays?|draws?|drew)\s+close\b/g, '$1 near').replace(/\bclose(?=\s+(?:to|by|eye|call|enough)\b)/g, 'near');

// Words a negation rules out within the next three words: "no cameras in her room" → cameras, in, room. Turn, switch,
// on, the, a, an and any don't use up the three ("don't turn on the sign" → sign), and "go live" counts as one word.
function negatedWords(t) {
  const w = words(t.replace(/\bgo\s+live\b/g, 'golive').replace(LEAVE_ALONE, 'dont touch $1$2').replace(LEAVE_REST, 'dont touch $1')).filter((x) => !NEG_GAP.test(x));
  return w.filter((x, i) => w.slice(Math.max(0, i - 3), i).some((y) => NEGATOR.test(y))).flatMap((x) => (x === 'golive' ? [x, 'go', 'live'] : [x]));
}
// Words the chip itself rules out: "without a camera" → camera; "no stream, no sign" → stream, sign.
const CHIP_NEG = /\b(?:not|never|dont|don't|do not|no|without)\s+(?:(?:a|an|the|any|my|her|his|your)\s+)?([a-z]+)/g;
function chipNegated(chip) { const out = []; for (const m of norm(chip).matchAll(CHIP_NEG)) if (!/^(?:let|forget|allow)$/.test(m[1])) out.push(m[1]); return out; }

// Per-prompt word lists, computed once: chip words, chip + keyword words, everything it rules out, its avoid entries.
const INFO = new WeakMap();
function info(p) {
  let v = INFO.get(p);
  if (!v) {
    const chipT = [...new Set(tokens(p.chip))];
    const ro = (p.rulesOut || []).map(tokens);
    v = { chipT, bag: [...new Set([...chipT, ...(p.keywords || []).flatMap(tokens)])], excl: [...chipNegated(p.chip), ...ro.filter((a) => a.length === 1).flat()], exclPh: ro.filter((a) => a.length > 1), avoid: (p.avoid || []).map(tokens).filter((a) => a.length) };
    INFO.set(p, v);
  }
  return v;
}

// 1 for the same word, ½ for a close form ("rotate" ~ "rotating").
const likeness = (w, b) => (w === b ? 1 : b.length > 4 && ((w.length > 3 && b.startsWith(w)) || w.startsWith(b)) ? 0.5 : 0);
const closest = (w, list) => list.reduce((m, b) => Math.max(m, likeness(w, b)), 0);
const isNeg = (w, negated) => negated.some((b) => sameWord(w, b));
// What the text names as something it wants off, muted or closed ("leave the sign off"): for a chip that rules the
// thing out, that agrees with the chip.
const offWanted = (t) => wanted(plain(t)).filter((k) => /^(?:off|closed|shut|muted|stopped|disarmed)$/.test(k.state)).map((k) => k.thing);
// A quantifier the prompt rules out ('everything', 'every', 'all' for a fragile-only rule) rules it out only when it
// covers the things the prompt acts on: alone or before a pronoun ("divert all to lane B", "all of them"), or before a
// thing ("all parcels", "the whole line", "the entire batch") that no word of the prompt's own narrows ("all parcels
// scanned as fragile" is the chip). Before one of the prompt's own words ("every fragile parcel", "all glass"), before
// something that isn't a thing on the line ("the whole shift", "every time", "every label", "any chance"), or after a
// verb the prompt does to every parcel anyway ("scan everything", "check every label"), it doesn't. "Some", "most" and
// "half" rule out only a thing: "some of them are glass" is no "divert some".
const QUANT = /^(?:everything|every|all|whole|entire|anything|any|each|full|rest|lot|some|most|half)$/, NARROW_SKIP = /^(?:the|of|my|our|that|thats|which|is|are|single|a|an|these|those|this)$/;
const Q_PARTIAL = /^(?:some|most|half)$/, Q_READS = /^(?:scan|scans|scanning|read|reads|reading|check|checks|checking|inspect|inspecting)$/;
const Q_ALONE = /^(?:to|into|onto|down|in|on|through|and|else|but|except|from|off|it|them|non|at|for|with)$/;
const Q_THING = /^(?:parcels?|packages?|box|boxes|items?|things?|stuff|goods|cartons?|pieces?|orders?|lines?|batch|batches|lots?|shipments?|loads?|pallets?|rest)$/;
const Q_QUALIFY = /^(?:scanned|labelled|labeled|marked|tagged|flagged|read|as|that|thats|which|is|are|with|being|a|an|the)$/;
function narrowed(ww, i, bag) {
  if (!QUANT.test(ww[i])) return false;
  if (i > 0 && Q_READS.test(ww[i - 1])) return true;
  let j = i + 1; while (j < ww.length && j <= i + 3 && NARROW_SKIP.test(ww[j])) j++;
  const head = ww[j], own = (w) => bag.some((c) => sameWord(w, c)), thing = head != null && Q_THING.test(head);
  if (head != null && own(head)) return true;
  if (thing || (head != null && Q_QUALIFY.test(head))) for (let k = thing ? j + 1 : j; k < ww.length && k <= j + 3; k++) { if (own(ww[k])) return true; if (!Q_QUALIFY.test(ww[k])) break; }
  if (thing) return false;
  if (Q_PARTIAL.test(ww[i])) return true;
  return !(head == null || Q_ALONE.test(head));
}
const OFF_VERB = /\b(?:turn|switch|shut|power)\s+(?:[a-z0-9']+\s+){0,4}?off\b/;
// The text names, un-negated, something the prompt rules out ("put a camera in her room", "go live" for "Recording only").
// "Turn" alone is not "turn on", nor is a turn/switch/shut left over from a switch-off ("turn the kettle plug off").
const compound = (x, b) => { if (x.length < b.length + 4 || !x.startsWith(b)) return false; let rest = x.slice(b.length); if (rest[0] === b[b.length - 1]) rest = rest.slice(1); return !/^(?:s|es|d|ed|ing|ings|er|ers|ies|ied|ly)$/.test(rest); };
function rulesOutText(t, p, negated = negatedWords(t)) {
  const { excl, exclPh, bag } = info(p); if (!excl.length && !exclPh.length) return false;
  t = nearNotClose(t);
  if (exclPh.length) { const q = tokens(t); if (exclPh.some((a) => a.every((x) => q.includes(x) && !isNeg(x, negated))) && t.split(/[,;.!?]/).map(tokens).some((w) => exclPh.some((a) => inOrder(w, a) && a.every((x) => !isNeg(x, negated))))) return true; }
  if (!excl.length) return false;
  // A quantifier rules out only itself: "some" is not "something" or "sometimes", "rest" not "restart", "most" not
  // "mostly". A word with four or more letters more than the other that aren't an ending is another word, either way
  // round: "feedback" is no "feed" and "over" no "overnight", while "recordings" is still "record" and "lamp" "lamps".
  const rules = (x, b) => (QUANT.test(b) ? x === b : sameWord(x, b) && !compound(x, b) && !compound(b, x));
  const off = offWanted(t), ww = words(t), hit = (x) => excl.some((b) => rules(x, b)) && !isNeg(x, negated) && !isNeg(x, off);
  if (ww.some((x, i) => !/^(?:turn|switch|shut)$/.test(x) && hit(x) && !narrowed(ww, i, bag))) return true;
  const offVerb = OFF_VERB.test(t);
  return tokens(t).some((x) => !QUANT.test(x) && !(offVerb && /^(?:turn|switch|shut)$/.test(x)) && hit(x)); // merged words too: "turnoff", "golive"
}

// An avoid entry in the text (routing fields, above): a single word anywhere; several words as a phrase in one clause.
const AVOID_CLAUSE = /[,;.!?]|\b(?:when|whenever|if|but|while|because|then|until|once|unless)\b|\b(?:and|or|so)\s+(?=(?:she|he|it|they|i|we|you|mum|her|his|the|my)\b)/;
const MOVABLE = /^(?:up|down|on|off|out|over|only|just)$/;
function inOrder(c, v) {
  for (let k = 0; k < c.length; k++) {
    if (c[k] !== v[0]) continue;
    let at = k, j = 1;
    for (; j < v.length; j++) { let f = -1; for (let q = at + 1; q <= at + 3 && q < c.length; q++) if (c[q] === v[j]) { f = q; break; } if (f < 0) break; at = f; }
    if (j === v.length) return true;
  }
  return false;
}
const VARIANTS = new WeakMap(); // avoid entry → the word orders it matches
function hasAvoid(clauses, q, a) {
  if (a.length === 1) return q.includes(a[0]);
  if (!a.every((x) => q.includes(x))) return false;
  let vs = VARIANTS.get(a);
  if (!vs) { vs = [a]; const m = a.findIndex((x) => MOVABLE.test(x)); if (m >= 0) { const rest = a.filter((_, i) => i !== m); vs.push([...rest, a[m]], [a[m], ...rest]); } VARIANTS.set(a, vs); }
  return clauses().some((w) => vs.some((v) => inOrder(w, v)));
}

// Every prompt scored against the text: score counts shared words; chip counts the ones found in the chip itself
// (it orders candidates; an equal score is still a tie). A word some chip rules out ("stream" in "…no stream")
// counts for that chip only when the text rules it out too ("no stream tonight"), and for the others only when it
// doesn't ("start the stream"). `ruled`: the text names something the prompt rules out; `onlyNeg`: all it matched is
// something the text rules out.
function scoreAll(text, prompts) {
  const t = norm(text), q = [...new Set(tokens(t))], negated = negatedWords(t);
  const infos = prompts.map(info); const anyExcl = infos.flatMap((x) => x.excl);
  let ct = null; const clauses = () => (ct ??= t.split(AVOID_CLAUSE).map(tokens)); // for avoid phrases, split once
  return prompts.map((p, i) => {
    const { chipT, bag, excl, avoid } = infos[i];
    const ruled = rulesOutText(t, p, negated);
    if (avoid.some((a) => hasAvoid(clauses, q, a) && a.every((x) => !isNeg(x, negated)))) return { prompt: p, i, score: 0, chip: 0, hit: [], ruled, onlyNeg: false };
    let score = 0, chip = 0; const hit = [];
    for (const w of q) {
      if (NOSCORE.test(w)) continue;
      if (anyExcl.some((b) => sameWord(w, b)) && excl.some((b) => sameWord(w, b)) !== isNeg(w, negated)) continue;
      const s = closest(w, bag); if (!s) continue;
      score += s; hit.push(w); chip += Math.min(s, closest(w, chipT));
    }
    return { prompt: p, i, score, chip, hit, ruled, onlyNeg: hit.length > 0 && hit.every((w) => isNeg(w, negated)) };
  });
}

// Asking for something to stop or go off. Such text only runs a chip that is itself a stop ("Stop everything…",
// "I'm done. Wrap up."): "stop the laser" must never run "Open the shutter…", "stop cart B" never "Divert…". A stop
// with a condition is a rule, not a stop ("stop if it drops", "turn the kettle off after 10 minutes").
const HALT = new Set(['stop', 'halt', 'abort', 'kill', 'cancel', 'pause', 'end', 'off', 'turnoff', 'switchoff', 'shutoff', 'shut', 'disable', 'disarm', 'mute', 'unplug', 'deactivate', 'quit']);
const HALT_CHIP = new Set([...HALT, 'done', 'wrap', 'finish']);
const isHaltChip = (chip) => tokens(chip).slice(0, 2).some((w) => HALT_CHIP.has(w));
// `rule: false` keeps a switch-off that follows a condition ("if it's on for 10 minutes, switch it off"), `neg: false` a
// negated one ("make sure the line doesn't stop"): decide() uses them to check what such text may run.
// Only a person's don't/never/not/won't negates any stop ("don't switch it off", "never stop"); a thing's doesn't/didn't/
// can't… only a switch-off ("the kettle doesn't turn off"), since "make sure the line doesn't stop" must not run a stop.
const NEG_HALT = /\b(?:don'?t|do\s+not|never|not|won'?t|no\s+need\s+to)\s+(?:(?:turn|switch|shut|power)\s+(?:[a-z0-9']+\s+){0,3}?off|stop|halt|pause|cancel|kill|unplug|mute|disarm|disable)\b|\b(?:doesn'?t|didn'?t|does\s+not|did\s+not|can'?t|cannot|isn'?t|wasn'?t|hasn'?t)\s+(?:turn|switch|shut|power)\s+(?:[a-z0-9']+\s+){0,3}?off\b/g;
const RULE_HALT = /\b(?:if|when|whenever|once|after)\b[^.;!?]*?(?:,|\band\b|\bthen\b)\s*(?:then\s+)?(?:turn|switch|shut|power)\s+(?:it|them|that|the\s+[a-z]+(?:\s+[a-z]+)?)\s+off\b/g;
// "…and keep the lights off" to a prompt that rules the lights out: a state its plan agrees with, not a stop.
const KEPT_OFF = /\b(?:keep|keeping|leave|leaving)\s+((?:[a-z0-9']+\s+){0,3}?)off\b/g;
const keptOffOut = (t, p) => { const { excl } = info(p); return t.replace(KEPT_OFF, (m, obj) => (words(obj).some((x) => excl.some((b) => sameWord(x, b))) ? ' ' : m)); };
function asksToHalt(t, { rule = true, neg = true } = {}) {
  let s = t.replace(/\b(?:stop|halt|end|pause)\s+(?:if|when|once|after|before|at)\b/g, ' ')
    .replace(/\b(?:stop|halt|pause)\s+(?:on|upon)\s+(?:an?\s+|any\s+|the\s+first\s+)?(?:[\d.]+\s*%|drop|fall|loss|decrease)/g, ' ') // "stop on a 20% drop"
    .replace(/\bhands\s+off\b/g, ' ')                                                                  // "hands off M2" leaves M2 alone (refused() reads it)
    .replace(/\boff\s+by\b/g, ' ')                                                                     // "M2 is off by 0.12°"
    .replace(/\b(?:without|no|with\s+no)\s+(?:a\s+|any\s+|the\s+)?stop(?:ping)?\s+(?:rule|condition|criteri(?:on|a))s?\b/g, ' ') // "walk M2 without a stop rule"
    .replace(/\b(?:turn|switch|shut|power)\s+(?:[a-z0-9']+\s+){0,3}?off\s+(?:if|when|whenever|once|after|before|unless|by(?=\s+\d)|at(?=\s+\d))\b/g, ' ') // "turn the kettle off after…", not "…off at night"
    .replace(/\b(?:turn|switch|shut|power)\s+off\s+(?:[a-z0-9'°.%]+\s+){0,4}?(?:if|when|whenever|once|after|before|unless)\b/g, ' ') // "switch off the kettle if…"
    .replace(/\b(?:nod(?:s|ded|ding)?|doz(?:e|es|ed|ing)|drop(?:s|ped|ping)?)\s+off\b|\bauto[-\s]?off\b|\bon\s*\/\s*off\b/g, ' ') // "she nods off", "the auto-off", "the on/off switch"
    .replace(/\bend\s+of\b/g, ' ')                                                         // "the end of the week"
    .replace(/\b(?:i'?m|we'?re|i\s+am|we\s+are|be|being|going|head(?:ing)?|set(?:ting)?)\s+off\b/g, ' '); // "I'm off to Spain"
  if (neg) s = s.replace(NEG_HALT, ' ');   // "don't switch it off"
  if (rule) s = s.replace(RULE_HALT, ' '); // "if it's on for more than 10 minutes, switch it off"
  return tokens(s).some((w) => HALT.has(w));
}

// Ranked candidates. Halt chips go last unless the text asks for a stop, and first when it does. A candidate that
// only matched what the text rules out, while the text names something it rules out, goes after its equals.
const weak = (r) => r.ruled && r.onlyNeg;
function rank(text, prompts, halting) {
  return scoreAll(text, prompts).map((r) => ({ ...r, pen: isHaltChip(r.prompt.chip) === halting ? 0 : 1 }))
    .sort((a, b) => b.score - a.score || a.pen - b.pen || weak(a) - weak(b) || b.chip - a.chip || a.i - b.i);
}

export function matchPrompt(text, prompts) {
  const top = rank(norm(text), prompts, false)[0];
  return top && top.score > 0 ? { prompt: top.prompt, score: top.score } : { prompt: prompts[0], score: 0 };
}

const SKIP = new Set(['be', 'been', 'being', 'to', 'a', 'an', 'the', 'it', 'so', 'too', 'even', 'ever', 'really', 'yet', 'just']);
const COND = new Set(['if', 'when', 'whenever', 'unless', 'whether', 'until']);
const LEAD = new Set(['please', 'i', 'we', "i'm", "we're", 'im', 'were', 'just', 'really', 'and', 'so', 'then', 'actually', 'but', 'no', 'ok', 'okay', 'oh', 'hey', 'wait', 'can', 'could', 'would', 'will', 'you', 'u']); // "can you not…" is a polite "don't"
const AWAY = new Set(['home', 'here', 'there', 'around', 'back', 'in', 'available']); // "I won't be home" describes an absence
// Chip verb → words that ask for the opposite (unless the text also asks for the verb: "open it, then close it").
const OPPOSITE = [[/\bopen\b/, /\b(?:close|closed|closing|shut|block|off)\b/, /\bopen\b/],
  [/\b(?:someone|lived\s+in)\b/, /\b(?:nobody|no\s*one)(?:'?s|\s+is|\s+was)?\s+(?:at\s+)?home\b|\b(?:look|looks|looking|seem|seems|appear|appears)\s+(?:like\s+)?(?:it'?s\s+|it\s+is\s+|the\s+[a-z]+\s+is\s+)?(?:empty|unoccupied|vacant|deserted|abandoned)\b|\bnot\s+lived\s+in\b/, /\b(?:someone|somebody)(?:'?s|\s+is)?\s+(?:at\s+)?home\b|\b(?:look|looks)\s+lived\s+in\b|\boccupied\b/]];
// What a protective chip prevents ("don't let my plants die", keyword 'freeze'), asked for: "let the pipes freeze".
const BAD = /^(?:die|dies|dying|freeze|freezes|freezing|fall|falls|falling|dry|burn|burns|wilt|rot|starve|overheat|flood|trip|trips)$/;
// A last clause that takes it all back: "go live? no."
const TAKEN_BACK = /[,.;!?]\s*(?:no|nope|nah|not really|never\s*mind|cancel(?: that)?|scratch that|forget (?:it|that))\s*[.!]*$/;

// 'opposite' when the text asks for the chip's opposite ("close the shutter" for "Open the shutter…", "let the plants
// die" for "…don't let my plants die"); 'negated' when it negates the chip: a negation at the start ("don't go live",
// "we won't go live"), before want/need ("I really don't want to go live"), or right before one of the chip's own
// words ("we're not going live"). Not after if/when ("tell me if she's not up"), not before let/forget/allow ("don't
// let my plants die"), not before a word the chip itself rules out ("no cameras" for "…without a camera"), and not
// "I won't be home". With chip = '' only the first two negation forms count (used when nothing matched).
function negation(text, chip, bag = []) {
  const c = norm(chip), t = norm(text); const negd = negatedWords(t);
  for (const [verb, opp, same] of OPPOSITE) { const m = t.match(opp); if (verb.test(c) && m && !same.test(t)) return negd.includes(m[0]) ? 'negated' : 'opposite'; } // "could you not close it" is no request to open it
  const lw = words(t);
  for (let i = 0; i < lw.length; i++) {
    if (lw[i] !== 'let' || NEGATOR.test(lw[i - 1] || '') || NEGATOR.test(lw[i - 2] || '')) continue;
    if (lw.slice(i + 1, i + 5).some((x) => BAD.test(x) && closest(x, bag) > 0)) return 'opposite';
  }
  const cneg = chipNegated(c);
  const w = t.replace(/\b(?:won'?t|can'?t|cannot|shouldn'?t|mustn'?t|wouldn'?t|couldn'?t|isn'?t|aren'?t|no\s+need\s+to)(?![a-z])/g, 'not')
    .replace(/\bdo\s+not\b/g, 'dont').replace(/\bdon'?t\b/g, 'dont').split(/[^a-z0-9']+/).filter(Boolean);
  const chipWords = words(c).filter((x) => !STOP.has(x)); // plain words ("go", "live"), not merged tokens
  for (let i = 0; i < w.length; i++) {
    if (!/^(?:not|never|dont|no)$/.test(w[i]) || /^(?:let|forget|allow)$/.test(w[i + 1] || '')) continue;
    if (w[i] === 'no' && w[i - 1] === 'with') continue; // "record with no mic" leaves a part out (conflict() checks it); it doesn't negate the request
    if (w.slice(i + 1, i + 6).some((x) => BAD.test(x) && closest(x, bag) > 0)) continue; // "make sure my plants don't die", "I don't want her to fall": what a protective chip prevents
    const next = w.slice(i + 1).find((x) => !SKIP.has(x)) || '';
    if (cneg.some((b) => sameWord(next, b)) || AWAY.has(next)) continue;
    if (w.slice(0, i).every((x) => LEAD.has(x))) return 'negated';
    if (w.slice(0, i).some((x) => COND.has(x))) continue;
    const tk = tokens(next)[0];
    if (/^(?:want|need|wanna)$/.test(next) || (tk && chipWords.some((b) => b === tk || (b.length > 1 && tk.startsWith(b))))) return 'negated';
  }
  return null;
}
export function negates(text, chip) { return negation(text, chip) !== null; }

// Details a chip is exact about. Numbers keep their sign and unit ("5 W" is not 5.0 mW, "12.4 cm" not 12.400 mm,
// "19°F" not 19°C; a bare number or "degrees" fits either); ids (M1, C2), axes and labelled things ("lane B",
// "dock 2", "stage X", "cart A") must be the chip's own.
const NUM_RE = /(^|[^a-z0-9.])([+\-−]?)(\d+(?:\.\d+)?)\s*(°\s*[cf]|°|degrees?(?:\s*(?:c|f|celsius|fahrenheit))?|deg|celsius|fahrenheit|milliwatts?|mw|kw|watts?|w|mm|cm|µm|um|nm|mrad|rad|metres?|meters?|m|ms|seconds?|secs?|s|minutes?|mins?|hours?|hrs?|h|%|percent|pm|am)?(?![a-z])/g;
const UNITS = [[/^(?:°\s*c|degrees?\s*c|degrees?\s*celsius|celsius)$/, 'c'], [/^(?:°\s*f|degrees?\s*f|degrees?\s*fahrenheit|fahrenheit)$/, 'f'], [/^(?:°|degrees?|deg)$/, 'deg'], [/^(?:mw|milliwatts?)$/, 'mw'], [/^kw$/, 'kw'], [/^(?:w|watts?)$/, 'w'], [/^mm$/, 'mm'], [/^cm$/, 'cm'], [/^(?:µm|um)$/, 'um'], [/^nm$/, 'nm'], [/^mrad$/, 'mrad'], [/^rad$/, 'rad'], [/^(?:m|metres?|meters?)$/, 'm'], [/^ms$/, 'ms'], [/^(?:s|secs?|seconds?)$/, 's'], [/^(?:mins?|minutes?)$/, 'min'], [/^(?:h|hrs?|hours?)$/, 'h'], [/^(?:%|percent)$/, '%'], [/^(?:am|pm)$/, 'clock']];
const unitOf = (u) => (u ? (UNITS.find(([re]) => re.test(u)) || [0, u])[1] : '');
const unitsFit = (a, b) => !a || !b || a === b || (a === 'deg' && /^[cf]$/.test(b)) || (b === 'deg' && /^[cf]$/.test(a));
// A clock time is one number, in minutes ("9:30", "09:30" and "9:30 am" are the same time; "7 pm" is 19:00), and "every
// minute" is every 1 minute. Each number carries its role, from the words right before it: under a threshold (below,
// under, less than, <), over one (above, over, more than, >), a duration (for), a window (within), a period (every), a
// deadline (by, before, until) or a delay (after); anything else ("at 19°C", "to 30%", "40 parcels") has none.
const CLOCK = /\b(\d{1,2})(?::(\d{2}))?\s*(am|pm)\b|\b(\d{1,2}):(\d{2})\b/g;
const ROLE = [[/(?:\b(?:below|under|beneath|less\s+than|lower\s+than|fewer\s+than)|<)\s*$/, 'lt'], [/(?:\b(?:above|over|more\s+than|greater\s+than|higher\s+than|exceeds?|exceeding)|>)\s*$/, 'gt'], [/\bfor\s*$/, 'for'], [/\bwithin\s*$/, 'within'], [/\b(?:every|each)\s*$/, 'every'], [/\b(?:by|before|until|till)\s*$/, 'by'], [/\bafter\s*$/, 'after']];
const roleOf = (before) => { const tail = before.slice(-24); for (const [re, r] of ROLE) if (re.test(tail)) return r; return ''; };
function numbers(s) {
  const x = s.replace(/(^|[^\d.])\.(\d)/g, '$10.$2'), out = [];
  const rest = x.replace(CLOCK, (m, h1, m1, ap, h2, m2, at) => { const h = Number(h1 ?? h2); out.push({ v: (ap ? (h % 12) + (ap === 'pm' ? 12 : 0) : h) * 60 + Number(m1 ?? m2 ?? 0), u: 'clock', role: roleOf(x.slice(0, at)) }); return ' '.repeat(m.length); });
  for (const m of rest.matchAll(NUM_RE)) out.push({ v: (m[2] && m[2] !== '+' ? -1 : 1) * Number(m[3]), u: unitOf(m[4]), role: roleOf(x.slice(0, m.index + m[1].length)) });
  for (const m of x.matchAll(/\bevery\s+(second|minute|hour)\b/g)) out.push({ v: 1, u: unitOf(m[1]), role: 'every' });
  return out;
}
const LABELLED = /\b(cart|agv|lane|stage|dock|bay|conveyor|belt|mirror|axis|zone|gate)s?\s+(?:no\.\s*|no\s+(?=\d)|number\s+|#\s*)?([a-z]|\d{1,2})\b/g; // "no" only before a digit: "carts now" is no cart W
const LABEL_NUM = /\b(?:cart|agv|lane|stage|dock|bay|conveyor|belt|mirror|axis|zone|gate)s?\s+(?:no\.\s*|no\s+(?=\d)|number\s+|#\s*)?\d{1,2}\b(?!\.\d)/g; // "dock 2" is a name (names() checks it), not the 12 in "stage 12.4 mm"
// A number the chip doesn't name is still the request's own when the plan card shows it in the same role. A threshold,
// duration or deadline ("below 30 percent", "more than 2 degrees", "by 9:30") also needs the same action around it: "water
// the plants when the soil is below 20%" is not a plan that waters below 30% and messages you below 20%. A set point
// ("leave the key light at 30%") needs the same unit and a word of the plan sentence it is in, that sentence may set
// nothing else in that unit, and the chip may not have a number of its own in that unit: "hold the house at 2 degrees"
// is not "…drifts more than 2°", and "keep the heating at 21°C" is not "held at 18°C, back to 21°C the afternoon…".
const NOTIFY = /^(?:message|messages|messaged|text|texts|texted|tell|tells|notify|notifies|alert|alerts|warn|warns|know|email|emails|report|reports|ping|pings)$/;
const WATER = /^(?:water|waters|watered|watering|irrigate|irrigates|irrigated|irrigating)$/;
const actsOf = (s) => { const w = words(s); return [NOTIFY, WATER].filter((re) => w.some((x) => re.test(x))); };
const NUM_WORD = /^(?:\d.*|at|to|of|from|by|in|on|percent|degrees?|deg|mm|cm|mw|w|min|mins|minutes?|secs?|seconds?|hours?|hrs?|am|pm|c|f)$/;
const NUM_PARTS = /[,;!?]|\.(?!\d)|\b(?:and|then|but|also|plus)\b/;
// Returns null when every number fits, 'values' when one doesn't, and 'partial' when a set point fits only a rule chip's
// plan while the text sets no condition ("hall light at 30%" is not "If she gets up at night, … hall light at 30%").
function numbersFit(t, prompt, chip) {
  const cn = numbers(chip.replace(LABEL_NUM, ' ')), same = (a, b) => Math.abs(a.v - b.v) < 1e-9;
  const ruleOnly = RULE_CHIP.test(chip) && !CONDITION.test(t);
  let plan = null, why = null;
  for (const c of t.split(NUM_PARTS)) {
    for (const n of numbers(c.replace(LABEL_NUM, ' '))) {
      if (cn.some((x) => same(x, n) && unitsFit(x.u, n.u))) continue;
      plan ??= planLines(prompt).flatMap((l) => norm(l).split(/;|\.(?!\d)/)).filter((x) => x.trim()).map((x) => ({ w: words(x), acts: actsOf(x), nums: numbers(x.replace(LABEL_NUM, ' ')) }));
      const acts = actsOf(c), cw = words(c).filter((x) => !STOP.has(x) && !NUM_WORD.test(x));
      let setPoint = false;
      const fits = plan.some((sn) => sn.nums.some((p) => {
        if (!same(p, n) || p.role !== n.role) return false;
        if (n.role) return unitsFit(p.u, n.u) && (acts.length || sn.acts.length ? acts.some((a) => sn.acts.includes(a)) : true);
        return (setPoint = !!n.u && p.u === n.u && !cn.some((x) => x.u && unitsFit(x.u, n.u)) && sn.nums.filter((q) => q.u === n.u).length === 1 && cw.some((x) => sn.w.some((y) => like(x, y))));
      }));
      if (!fits) return 'values';
      if (setPoint && ruleOnly) why = 'partial';
    }
  }
  return why;
}
function names(s) {
  const out = new Set();
  for (const m of s.matchAll(/\b[a-z]{1,3}\d{1,2}\b/g)) out.add(m[0]);
  for (const m of s.matchAll(/\b(?:yaw|pitch|roll)\b/g)) out.add(m[0]);
  for (const m of s.matchAll(LABELLED)) out.add(`${m[1]} ${m[2]}`);
  return out;
}
const ACTIVATE = new Set(['turnon', 'switchon']);
const SWITCH_ON = /\b(?:turn|switch|power|put)\s+(?:[a-z0-9'-]+\s+){0,4}?on\b/, CONDITION = /\b(?:if|when|whenever|once|after|before|until|while|unless|as\s+soon\s+as)\b/;
const RULE_CHIP = /^(?:if|when|whenever|once)\b|\b(?:tell\s+me|let\s+me\s+know)\s+(?:if|when|whenever)\b/;
const INCREASE = /\b(?:turn|crank|pump|ramp)(?:s|ed|ing)?\s+(?:[a-z0-9'-]+\s+){0,2}?up\b/; // asks for more of something: never a stop
// "Tell me if she gets up at night" asks to be told: a chip that never tells you anything (not in its chip, not on its
// plan card) doesn't do that, however close its words are.
const ASKS_TO_BE_TOLD = /^(?:please\s+)?(?:(?:tell|message|text|notify|alert|warn|email|ping)\s+me\b|let\s+me\s+know\b)/;
const tells = (p) => [p.chip, ...planLines(p)].some((l) => words(norm(l)).some((x) => NOTIFY.test(x)) || REPORTS_BACK.test(norm(l)));
const RESTRICT = /\b(?:just|only)\s+(?:the|my|her|his|a|an|your|our)\s+[a-z]|\b(?:the|my)\s+[a-z-]+(?:\s+[a-z-]+)?\s+only\b/; // "just the X", "the X only"
// 'values': other numbers or units than the chip's; 'partial': another device/axis/lane, something the chip rules
// out, only a bare "turn on" in common ("turn on the key light" is not "Go live."), "just the X" where X is only a
// side part of the chip (a keyword, not in the chip itself), or only the follow-up half of a "do X, then Y" chip
// ("report the power meter" is not "Tilt M2 by +0.050° in yaw, then report the power meter").
function mismatch(text, top) {
  const t = text.replace(/\bmirror\s+(\d)\b/g, 'm$1'), chip = norm(top.prompt.chip);
  // "turn on the night light" is not "If she gets up at night, light the way…": a switch-on with no condition of its own
  // runs no rule chip (one that starts with a condition or tells you when something happens) that doesn't switch it on itself
  if (SWITCH_ON.test(t) && !CONDITION.test(t) && RULE_CHIP.test(chip) && !SWITCH_ON.test(chip) && !tokens(chip).some((w) => ACTIVATE.has(w))) return 'partial';
  const nums = numbersFit(t, top.prompt, chip); if (nums) return nums;
  const cnames = names(chip), negd = [...negatedWords(t), ...refused(t).flat()];
  for (const n of names(t)) if (!cnames.has(n) && !negd.includes(n.split(' ').pop())) return 'partial'; // "…but don't touch M1" agrees with a plan that doesn't
  if (rulesOutText(t, top.prompt)) return 'partial';
  if (top.hit.every((w) => ACTIVATE.has(w))) return 'partial';
  if (ASKS_TO_BE_TOLD.test(t.replace(LEADING, '')) && !tells(top.prompt)) return 'partial';
  if (RESTRICT.test(t) && !top.chip) return 'partial'; // "just the heating": only a side part of "Hold the house at 19°C and tell me…"
  const then = chip.split(/,?\s+then\s+/)[1];
  if (then) { const after = tokens(then), first = info(top.prompt).bag.filter((b) => !after.some((a) => likeness(a, b) || likeness(b, a))); if (!top.hit.some((w) => closest(w, first) > 0)) return 'partial'; }
  return null;
}

// Plan lines, as the plan card shows them (plus a prompt's `touches`), and their clauses ("Camera off; verify…").
const planLines = (p) => { const out = [...(p.touches || [])]; const walk = (steps) => { for (const s of steps || []) { if (s.plan) out.push(...s.plan.steps.map((x) => x.text)); if (s.parallel) walk(s.parallel); } }; walk(p.steps); return out; };
const plain = (s) => norm(s).replace(/\bon[-\s]air\b/g, 'onair').replace(/\bgo\s+live\b/g, 'golive');
const STATE_WORD = /^(?:on|off|open|opened|close|closed|shut|muted|unmuted|mute|unmute|running|stopped|stop|armed|disarmed|dim|live)$/;
// Clause words; an on-air sign named with no state ("then the on-air sign") is being switched on.
const clauseWords = (c) => { const w = words(c); if (w.includes('onair') && !w.some((x) => STATE_WORD.test(x))) w.push('on'); return w; };
const clausesOf = (p, split) => planLines(p).flatMap((line) => plain(line).split(split).map((c) => ({ line, w: clauseWords(c) })));

// Same word up to an inflection: open ~ opens ~ opening, scan ~ scanned ~ scanner, use ~ using; not sign ~ signal.
function sameForm(a, b) {
  if (a === b) return true;
  const [x, y] = a.length <= b.length ? [a, b] : [b, a]; if (x.length < 3) return false;
  if (x.endsWith('e') && y.startsWith(x.slice(0, -1)) && /^(?:ing|ed|er)$/.test(y.slice(x.length - 1))) return true;
  if (!y.startsWith(x)) return false;
  let rest = y.slice(x.length); if (rest.length > 1 && rest[0] === x[x.length - 1]) rest = rest.slice(1);
  return /^(?:s|es|d|ed|ing|er|ers)$/.test(rest);
}
// …also two inflections of one word (scanning ~ scanned) and the few names a plan uses for what visitors call
// something else (lights ~ lamp, belts ~ conveyors).
const SYN = [['photo', 'photos', 'picture', 'pictures', 'image', 'images', 'snapshot', 'snapshots', 'pic', 'pics'], ['light', 'lights', 'lamp', 'lamps', 'lighting'], ['camera', 'cameras', 'cam', 'webcam'], ['mic', 'mics', 'microphone'], ['conveyor', 'conveyors', 'belt', 'belts', 'line'], ['cart', 'carts', 'agv', 'agvs', 'robot', 'robots'], ['heating', 'heater', 'thermostat'], ['blinds', 'blind', 'shades', 'curtains'], ['stream', 'streaming', 'broadcast'], ['parcel', 'parcels', 'box', 'boxes', 'package', 'packages', 'order', 'orders', 'item', 'items']];
const stem = (w) => { let s = w.replace(/(?:ing|ers|er|ed|es|s)$/, ''); if (s.length > 3 && s[s.length - 1] === s[s.length - 2]) s = s.slice(0, -1); return s; };
const like = (a, b) => sameForm(a, b) || (stem(a).length >= 3 && stem(a) === stem(b)) || SYN.some((g) => g.includes(a) && g.includes(b));

// A stop asked of one thing ("mic off", "kill the lights") runs a stop chip only when its plan switches that very
// thing off or mutes it: "key light off" is not "I'm done. Wrap up.", whose plan ends with the key light at 30%, and
// "stop recording" is not either (its plan never mentions a recording). "Turn everything off" isn't, while the plan
// leaves something on at a level.
const OFF_STATE = /^(?:off|muted|mute|stop|stops|stopped|closed|close|disarmed|disarm|disabled|disable|parked|halted|ended)$/;
const FILLER = new Set(['now', 'right', 'away', 'immediately', 'asap', 'quick', 'quickly', 'fast', 'pls', 'plz', 'thanks', 'thank', 'ok', 'okay', 'hey', 'all', 'everything', 'things', 'thing', 'stuff', 'down', 'up', 'guys', 'we', 'were', 'are', 'am', 'was', 'going', 'gonna', 'want', 'wanna', 'need', 'lets', 'today', 'tonight', 'here', 'there', 'then', 'too', 'also', 'again', 'already', 'ncb', 'neucharbox', 'hub', 'emergency', 'safely', 'safe', 'sure', 'go', 'get', 'got', 'no', 'not', 'just', 'only', 'everyone', 'folks', 'chat', 'bye', 'oh', 'well', 'so', 'yes', 'yeah', 'yep', 'will', 'would', 'could', 'should', 'done', 'press', 'push', 'hit', 'button', 'whole', 'entire']);
const ALL_OFF = /\b(?:everything|all)\s+(?:off|down)\b|\b(?:turn|switch|shut|power)\s+(?:off|down)\s+(?:everything|all)\b|\b(?:turn|switch|shut|power)\s+(?:everything|it\s+all|all)\s+(?:off|down)\b|\bkill\s+(?:everything|it\s+all|all)\b/;
function haltFit(text, top) {
  const lines = planLines(top.prompt); if (!lines.length) return true;
  if (ALL_OFF.test(text) && lines.some((l) => /\b[1-9]\d*\s*%|\bdim\b/.test(l))) return false;
  const clauses = clausesOf(top.prompt, /[.;,—–]|\bbut\b|\bthen\b/), { bag } = info(top.prompt);
  const objs = tokens(text).filter((w) => !HALT.has(w) && !FILLER.has(w) && !/^\d/.test(w) && !closest(w, bag));
  return objs.every((o) => clauses.some((c) => c.w.some((y) => like(o, y)) && c.w.some((y) => OFF_STATE.test(y))));
}

// Something the text says not to do that the chip's plan card does anyway: "don't open the shutter, just move stage X"
// must not run a procedure whose first line is "Open the shutter…". Returns that plan line, or null. Each negation in
// the text (not, don't, no, without, except, skip) rules out the verb and object right after it ("don't open the
// shutter" → open + shutter, "without the mic" → mic, "not fall at night" → fall); a plan clause conflicts when it has
// all of them (after a generic verb — turn on, use, start — the object alone). "Don't let/forget" asks for the
// opposite, and a plan clause that is itself negated ("Do not touch M1 or the stage") or a word the chip rules out
// ("no stream") agrees with the text.
const NEG_SKIP = new Set(['a', 'an', 'the', 'any', 'my', 'her', 'his', 'your', 'our', 'their', 'it', 'them', 'me', 'us', 'him', 'to', 'be', 'even', 'ever', 'really', 'want', 'wanna', 'need', 'like', 'you', 'we', 'i', 'please', 'do', 'go', 'get', 'anything', 'everything', 'something', 'too', 'ones', 'rule', 'rules', 'condition', 'conditions', 'criterion', 'criteria']);
const NEG_END = new Set(['at', 'in', 'on', 'off', 'by', 'for', 'from', 'with', 'while', 'when', 'if', 'until', 'and', 'or', 'but', 'just', 'then', 'instead', 'so', 'because', 'unless', 'before', 'after', 'during', 'now', 'today', 'tonight', 'please', 'pls', 'thanks', 'either', 'anymore', 'yet', 'again', 'too', 'should', 'must', 'can', 'could', 'would', 'will', 'shall', 'may', 'might']);
const GENERIC = new Set(['turn', 'switch', 'use', 'using', 'start', 'run', 'enable', 'activate', 'involve', 'touch', 'touching', 'power', 'take', 'taking']);
// A negation inside a condition describes a person's state and refuses nothing: "tell me if she's not up", "let me know
// if mum is still not up by nine". It follows is/has/does/she's… (with still, yet, really or even between at most) after
// an if/when earlier in the clause, about a person; "if she's up don't call Pat" still refuses, and so does "tell me if
// the shutter is not open" (a device's state is what the plan sets).
const COND_AUX = /^(?:is|was|are|were|has|have|had|does|did|will|would|shes|hes|its|theyre|mums)$/, COND_ADV = /^(?:still|yet|really|even|just|already)$/;
const PERSON = /^(?:she|he|mum|mom|mother|dad|father|they|someone|somebody|anyone|nobody|nan|gran|grandma|grandad)$/;
function inCondition(w, i) { let k = i - 1; while (k >= 0 && COND_ADV.test(w[k])) k--; return k > 0 && COND_AUX.test(w[k]) && (/^(?:shes|hes|mums|theyre)$/.test(w[k]) || PERSON.test(w[k - 1] || '')) && w.slice(0, k).some((x) => COND.has(x)); }
function refused(t) {
  const out = [];
  t = t.replace(/\bleave\s+((?:the|my|her|his|a|an)\s+)?([a-z0-9-]+(?:\s+[a-z0-9-]+)?)\s+alone\b/g, 'dont touch $1$2').replace(/\bhands\s+off(?:\s+of)?\s+/g, 'dont touch '); // "leave M2 alone", "hands off the stage"
  for (const clause of t.replace(/\b(?:do\s+not|don'?t)\b/g, 'dont').split(/[,;.!?—–]/)) {
    const w = words(clause);
    for (let i = 0; i < w.length; i++) {
      if (!/^(?:no|not|never|dont|without|except|wont|cant|cannot|shouldnt|mustnt|skip|skipping|bypass|avoid|avoiding)$/.test(w[i]) || /^(?:let|forget|allow)$/.test(w[i + 1] || '')) continue;
      if (inCondition(w, i)) continue;
      const ph = [];
      for (let j = i + 1; j < w.length && ph.length < 2; j++) {
        if (/^(?:on|off|up|down)$/.test(w[j]) && /^(?:turn|switch|shut|power)$/.test(w[j - 1])) continue; // "turn on the sign"
        if (NEG_END.has(w[j]) && !(w[j] === 'thanks' && j < w.length - 1)) break; // "…, thanks" ends it; "skip the thanks for watching" doesn't
        if (!NEG_SKIP.has(w[j]) && !/^\d/.test(w[j])) ph.push(w[j]);
      }
      if (ph.length) out.push(GENERIC.has(ph[0]) && ph.length > 1 ? ph.slice(1) : ph);
    }
  }
  return out;
}
// States the text wants that a plan clause would undo. Kept ("leave the camera on", "keep the mic muted", "with the
// shutter closed", "mic muted"): any clause that sets the thing to the opposite state conflicts. Asked for ("mute the
// mic", "close the shutter", "turn the sign off"): a clause that sets the opposite conflicts unless the plan also sets
// the asked state ("Blinds close as the sun sets, open again at 08:00" does close them; "Lights off 5 minutes after
// she's back in bed" doesn't undo "turn on the hall light when she gets up").
const HALTED = ['stop', 'stops', 'stopped', 'off', 'disarmed', 'disarm', 'halted', 'paused', 'parked', 'down', 'brakes'];
const OPPOSITE_STATE = { on: ['off', 'muted', 'mute', 'stop', 'stops', 'stopped', 'disarmed', 'disarm', 'halted', 'parked'], off: ['on', 'unmuted', 'unmute', 'armed'], open: ['close', 'closed', 'shut'], closed: ['open'], shut: ['open'], muted: ['unmuted', 'unmute', 'open', 'live', 'on'], unmuted: ['muted', 'mute'], running: HALTED, moving: HALTED, going: HALTED, stopped: ['start', 'started', 'running'], armed: ['disarmed', 'disarm', 'off'], disarmed: ['arm', 'armed'] };
const STATE = '(on|off|open|closed|shut|muted|unmuted|running|moving|going|stopped|armed|disarmed)';
const OBJ = '(?:(?:the|my|her|his|its|a|an|all|both)\\s+)?([a-z0-9-]+(?:\\s+[a-z0-9-]+)?)';
const KEPT = new RegExp(`\\b(?:leave|leaving|keep|keeping|with)\\s+${OBJ}\\s+${STATE}\\b`, 'g');
const KEPT_PART = new RegExp(`^(?:leave|leaving|keep|keeping|with)\\s+${OBJ}\\s+${STATE}\\b`);
const BARE = /\b([a-z0-9-]+)\s+(muted|unmuted)\b/g;
const GOING = new RegExp(`\\b${OBJ}\\s+(?:can\\s+)?(?:keep|keeps|stay|stays)\\s+(moving|going|running)\\b`, 'g'); // "the carts keep moving"
const ASKED = [
  [new RegExp(`\\b(close|shut|open|mute|unmute)\\s+${OBJ}`, 'g'), (m) => ({ close: 'closed', shut: 'closed', open: 'open', mute: 'muted', unmute: 'unmuted' })[m[1]], 2],
  [new RegExp(`\\b(?:turn|switch|power)\\s+(on|off)\\s+${OBJ}`, 'g'), (m) => m[1], 2],
  [new RegExp(`\\b(?:turn|switch|power)\\s+${OBJ}\\s+(on|off)\\b`, 'g'), (m) => m[2], 1],
];
const NOT_THING = /^(?:it|them|that|this|everything|all|up|down|keep|leave|stay|stays|remain|remains|is|be|been|was|get|gets|mic's)$/;
// A time word after the thing ends it too: "close the blinds overnight" is about the blinds, not "overnight".
const TIME_END = /^(?:overnight|nightly|daily|weekly|tomorrow|tonight|today|later|early|late)$/;
const thingOf = (s) => { const w = words(s); const k = w.findIndex((x) => NEG_END.has(x) || STOP.has(x) || TIME_END.test(x)); return (k < 0 ? w : w.slice(0, k)).filter((x) => x.length > 1 && !/^\d/.test(x)).pop(); };
// "What if she goes out with the kettle on" describes what someone might do, not a state to keep: a with-state after
// if/when + she/he/mum/they… in the same clause is skipped ("go live with the mic muted" still keeps the mic muted).
const HYPOTHETICAL = /\b(?:if|when|whenever|unless|until|once)\s+(?:she|he|mum|mom|mother|dad|father|they|someone|somebody|anyone|nobody)\b/;
function wanted(t) {
  const out = [];
  for (const m of t.matchAll(KEPT)) { if (/^with\b/.test(m[0]) && HYPOTHETICAL.test(t.slice(0, m.index).split(/[,;.!?]/).pop())) continue; out.push({ thing: thingOf(m[1]), state: m[2], kept: true }); }
  for (const m of t.matchAll(BARE)) out.push({ thing: m[1], state: m[2], kept: true });
  for (const m of t.matchAll(GOING)) out.push({ thing: thingOf(m[1]), state: m[2], kept: true });
  for (const [re, stateOf, g] of ASKED) for (const m of t.matchAll(re)) {
    if (words(t.slice(0, m.index)).slice(-2).some((x) => /^(?:no|not|never|dont|without|wont|cant|cannot|shouldnt|mustnt)$/.test(x))) continue; // "don't turn on the sign" is refused(), not asked
    out.push({ thing: thingOf(m[g]), state: stateOf(m), kept: false });
  }
  return out.filter((k) => k.thing && !NOT_THING.test(k.thing) && !STOP.has(k.thing) && OPPOSITE_STATE[k.state]);
}
const stateWord = (s) => (s === 'closed' ? 'close' : s);
function conflict(text, prompt) {
  const { excl } = info(prompt), t = plain(text);
  const no = refused(t).filter((ph) => !ph.some((x) => excl.some((b) => sameForm(x, b)))); // "don't message Pat", "don't raise the limit": the chip rules that out
  const want = wanted(t); if (!no.length && !want.length) return null;
  const clauses = clausesOf(prompt, /[.;—–]|\bbut\b/).filter((c) => !c.w.some((x) => /^(?:no|not|never|dont|nothing|without|none)$/.test(x)));
  const has = (c, s) => c.w.some((y) => like(s, y));
  const sets = (k, state) => clauses.some((c) => has(c, k.thing) && has(c, state));
  const sends = (c, k) => /^(?:closed|shut|stopped|off)$/.test(k.state) && c.w.some((y, i) => like(k.thing, y) && c.w[i + 1] === 'to' && /^(?:lane|dock|bay|zone|side|position)$/.test(c.w[i + 2] || '')); // "keep the gate closed" vs "gate to lane B"
  for (const c of clauses) {
    if (no.some((ph) => ph.every((x) => has(c, x)))) return c.line;
    if (want.some((k) => has(c, k.thing) && OPPOSITE_STATE[k.state].some((o) => has(c, o)) && (k.kept || !sets(k, stateWord(k.state))))) return c.line;
    if (want.some((k) => k.kept && sends(c, k))) return c.line;
  }
  return null;
}

// Asked to start or move something the plan keeps parked or stopped ("start the carts" for a plan that says "Carts A and
// B stay parked unless a conveyor drops out"): the chip doesn't do just that. Only a plan clause about that thing (it
// names it first) counts, so "start the line" still runs that plan.
const STARTS = new RegExp(`\\b(?:start|restart|run|move|drive|dispatch|send)\\s+${OBJ}`, 'g');
const HELD = /^(?:parked|stopped|idle|off|closed|disarmed|halted|paused)$/;
function startsHeld(t, prompt) {
  const things = [...plain(t).matchAll(STARTS)].map((m) => thingOf(m[1])).filter((k) => k && !NOT_THING.test(k) && !STOP.has(k));
  if (!things.length) return false;
  return clausesOf(prompt, /[.;,—–]|\bbut\b/).some((c) => { const first = c.w.find((x) => x.length > 1 && !STOP.has(x)); return first && things.some((k) => like(k, first)) && c.w.some((y) => HELD.test(y)); });
}

// How telling a candidate's matches are, to pick which two of three tied chips to name: a word few prompts share
// counts more ("camera" over "away"), and each typed word its plan lines mention adds a little. Never decides a run.
function relevance(r, text, prompts) {
  let s = 0;
  for (const w of r.hit) s += 1 / Math.max(1, prompts.filter((p) => closest(w, info(p).bag) > 0).length);
  const lw = planLines(r.prompt).flatMap(tokens);
  for (const w of new Set(tokens(text))) if (!NOSCORE.test(w) && lw.some((y) => sameForm(w, y))) s += 0.25;
  return s;
}
// "Only"/"just": of two chips that matched the same words, the narrower one (it says just/only itself, or has fewer
// plan lines): "only the plants please" is "Just keep the plants alive…", not the whole away-for-a-week plan.
const narrowness = (p) => (/\b(?:just|only)\b/.test(norm(p.chip)) ? 0 : 100) + planLines(p).length;
const sameHits = (a, b) => a.hit.length === b.hit.length && a.hit.every((w) => b.hit.includes(w));
// A second request in the same text ("water the plants and hold the house at 19", "go live, then wrap up"): a clause
// that on its own runs another chip, and that the top chip covers nowhere (its words are neither the top chip's nor in
// one of its plan clauses). "Open the shutter and move stage X to 12.4 mm" is one request: the move's plan opens the
// shutter. "…and report the power" is too: on its own that clause runs nothing.
const PARTS = /\s*(?:[,;!?]|\.(?!\d)|\band\b|\bthen\b|\balso\b|\bplus\b)\s*/;
const WEAK_WORD = new Set(['everything', 'anything', 'something', 'all', 'thing', 'things', 'stuff', 'been', 'being', 'more', 'than', 'each', 'one', 'any', 'way', 'some', 'very', 'much', 'many', 'over', 'into', 'from', 'about', 'after', 'before', 'still', 'back', 'out', 'off', 'up', 'down', 'here', 'there', 'get', 'got', 'gets', 'know', 'make', 'take', 'want', 'need', 'look', 'like']);
function secondRequest(text, top, prompts, depth) {
  const parts = text.split(PARTS).filter((s) => s && tokens(s).length); if (parts.length < 2 || depth > 0) return null;
  const { bag } = info(top.prompt), plan = clausesOf(top.prompt, /[.;—–]/);
  const covered = (s) => tokens(s).filter((w) => !NOSCORE.test(w) && !WEAK_WORD.has(w) && !NUMERIC.test(w)).every((w) => closest(w, bag) > 0 || plan.some((c) => c.w.some((y) => like(w, y))));
  for (const part of parts.slice(0, 12)) {
    const r = decide(part, prompts, depth + 1);
    if (r.action === 'run' && r.prompt !== top.prompt && !covered(part)) return r.prompt;
  }
  return null;
}

// Questions. One asking for a fact ("what's the power reading", "who's at the door", "is anyone at the door") is never a
// request; a yes/no one runs only a chip that itself reports back, on a strong match ("is she up yet" → "Let me know
// when mum's up…"); an action chip never answers a question about state, safety or permission ("is the shutter open?",
// "is it safe to open the shutter?", "should I…", "can I…", "tell me if the shutter is open", "shutter open?"). "What
// if she…" and "what about…" are requests, and so is a polite question ("is it possible to open the shutter?", "do
// you think you could go live?"). A statement of fact ("the shutter is open", "I already opened the shutter") is no
// request either.
const INFO_Q = /^(?:(?:is|was|did|has|have)\s+(?:any|some)(?:one|body)\b|what(?!\s+(?:if|about)\b)|what's|whats|which|where|who|whose|how\s+(?:much|many|high|low|hot|cold|warm|bright|long|far|often|big)\b|when\s+(?:is|does|did|will|was|do|are)\b)/;
const QUESTION = /^(?:what(?!\s+(?:if|about)\b)|what's|whats|how|why|is|are|am\s+i|does|did|do\s+(?:you|i|we|they|she|he)\b|has|have|had|was|were|who|where|which|will\s+(?:it|she|he|they|mum|the)\b)\b/;
const POLITE = /^(?:is\s+it\s+possible\s+(?:for\s+you\s+)?to|would\s+it\s+be\s+possible\s+(?:for\s+you\s+)?to|are\s+you\s+able\s+to|would\s+you\s+be\s+able\s+to|do\s+you\s+think\s+you\s+(?:could|can)|how\s+about\s+you|would\s+you\s+mind)\s+/;
const REPORTS_BACK = /\b(?:let me know|tell me|notify|alert me|warn me|keep an eye)\b/;
const SHOULD_Q = /^(?:should|shall)\s+(?:i|we)\b/;
const PERMISSION_Q = /^(?:can|may|could|must)\s+i\b|^(?:am|are)\s+(?:i|we)\s+(?:allowed|ok|okay|safe|meant|supposed)\b/;
// A check of how something is right now ("tell me if the shutter is open", "check whether the laser is on", "verify
// the shutter is open", "check the shutter"), not a standing rule ("tell me if she gets up at night").
const STATE_WORD_Q = '(?:open|opened|closed|shut|on|off|aligned|misaligned|blocked|live|running|stopped|armed|disarmed|locked|unlocked|muted|unmuted)';
const REPORT_REQ = new RegExp(`^(?:(?:can|could|would|will)\\s+you\\s+)?(?:please\\s+)?(?:(?:tell\\s+me|let\\s+me\\s+know|check|verify|confirm|see|find\\s+out)\\s+(?:if|whether)|verify|confirm|check)\\s+(?:that\\s+)?(?:the\\s+|my\\s+)?[a-z0-9-]+(?:\\s+[a-z0-9-]+)?(?:'s|\\s+(?:is|are|was|were))\\s+(?:still\\s+|already\\s+|now\\s+)?${STATE_WORD_Q}\\b[^,;.!?]*[?.!]*$|^(?:(?:can|could|would|will)\\s+you\\s+)?check\\s+(?:on\\s+)?(?:the\\s+)?[a-z0-9-]+\\s*[?.!]*$`);
const STATE_Q = /^(?:the\s+)?[a-z0-9-]+(?:\s+[a-z0-9-]+)?\s+(?:open|closed|shut|on|off|aligned|blocked|live)\s*\?+\s*$/;
// A statement is the whole text: a subject, is/are/'s and a state, with at most a short tail ("M2 is off by 0.12
// degrees", "the beam is on the meter"), or "I already opened the shutter". Not "when she's up, turn on the hall light".
const STATE_TAIL = '(?:\\s+(?:now|already|again|too|here|there|still|yet|today|tonight|(?:(?:by|at|to)\\s+)?[+\\-−]?[\\d.,:]+\\s*(?:°\\s*[cf]?|%|[a-z]{1,8})?|(?:on|in|at)\\s+(?:the|my|her)\\s+[a-z-]+))*\\s*[.!]*$';
const STATEMENT = new RegExp(`^(?!(?:when|whenever|if|once|as|so|and|but|then|until|after|before|because)\\b)(?:(?:the|my|our|this|that|it|m\\d)\\b[^,.;!?]{0,40}?\\b(?:is|are|was|were|seems?)|(?:the\\s+)?[a-z0-9-]+(?:\\s+[a-z0-9-]+)?(?:'s|\\s+(?:is|are|was|were|seems?)))\\s+(?:still\\s+|already\\s+|now\\s+)?(?:${STATE_WORD_Q.slice(3, -1)}|up|at)${STATE_TAIL}|^(?:i|we|someone|somebody|he|she|they)\\s+(?:already\\s+|just\\s+)?(?:opened|closed|moved|tilted|turned|switched|walked|aligned|shut)\\b[^,;.!?]*[.!]*$`);
const CLAUSE = /\s*(?:[,;!?—–]|\.(?!\d)|\b(?:just|but|instead|rather)\b)\s*/;         // "12.4 mm" stays whole
const CONTRAST = /\s*\b(?:but|except|although|though)\b\s*/;
const NUMERIC = /^[+\-−]?\d[\d.]*°?$/;
const FRAGMENT = new Set(['now', 'safely', 'safe', 'pls', 'plz', 'quickly', 'immediately', 'asap', 'thanks', 'thank', 'ok', 'okay', 'right', 'too', 'also', 'again', 'then', 'today', 'tonight', 'yes', 'yeah', 'sure', 'fine']);
const LEADING = /^(?:(?:please|pls|ok|okay|so|hey|hi|um|uh|well|and|oh|right)\b[\s,.!]*)+/; // "ok, what's…" is still a question

// Decide what to do with typed text: { action: 'run' | 'refuse' | 'clarify', prompt, score, via?, also?, line? }.
// `via` on a run: 'clause' when another clause of the text ran ("I don't want to stream tonight, just record"). On a
// refuse: 'opposite' when the text asks for the chip's opposite ("close the shutter"); none when it negates it. On a
// clarify: 'halt' (asks for a stop no chip does), 'tie' (two chips fit equally; `also` is the other), 'values' (other
// numbers or units), 'partial' (asks for something the closest chip doesn't do), 'conflict' (its plan does something
// the text says not to; `line` is that plan line), 'question' (a question, not a request), 'check' (asks to check how
// something is, which no chip does), 'statement' (says how things are); none = vague. Never throws: any input gets an
// answer (the tests pass { strict: true } so that a bug surfaces instead of a quiet "clarify").
export function interpret(text, prompts, { strict = false } = {}) {
  try { return decide(text, prompts, 0); } catch (e) { if (strict) throw e; return { action: 'clarify', prompt: prompts && prompts[0], score: 0 }; }
}
function decide(text, prompts, depth) {
  const t = norm(text);
  if (!prompts || !prompts.length) return { action: 'clarify', prompt: undefined, score: 0 };
  // "go live but keep the mic muted": the request is the part before a "but"; the rest is checked further down.
  const [head, ...rest] = t.split(CONTRAST); const main = rest.length && rank(head, prompts, asksToHalt(head))[0].score >= 1 ? head : t;
  const halting = asksToHalt(main);
  const ranked = rank(main, prompts, halting);
  const pool = halting ? ranked.filter((r) => isHaltChip(r.prompt.chip)) : ranked;
  let top = pool[0] && pool[0].score > 0 ? pool[0] : ranked[0].score > 0 ? ranked[0] : null;
  const out = (action, extra = {}) => ({ action, prompt: (top || ranked[0]).prompt, score: top ? top.score : 0, ...extra });
  if (TAKEN_BACK.test(t)) return out('refuse');
  const neg = negation(t, top ? top.prompt.chip : '', top ? info(top.prompt).bag : []);
  if (neg) {
    // "I don't want to stream tonight, just record", "divert fragile to lane B, don't stop the line": act on another
    // clause that isn't itself negated or the opposite (the last one first)
    const parts = t.split(CLAUSE).filter(Boolean);
    for (let i = parts.length - 1; i >= Math.max(0, parts.length - 12) && parts.length > 1 && depth < 2; i--) { // the last 12 clauses at most
      if (negates(parts[i], '') || KEPT_PART.test(parts[i]) || tokens(parts[i]).every((w) => FRAGMENT.has(w))) continue; // not "safely", "now", "leave the sign off"
      if (top && negation(parts[i], top.prompt.chip, info(top.prompt).bag) === 'opposite') continue;
      const r = decide(parts[i], prompts, depth + 1);
      if (r.action === 'run') { const line = conflict(t, r.prompt); return line ? { action: 'clarify', prompt: r.prompt, score: r.score, via: 'conflict', line } : { ...r, via: 'clause' }; }
      if (r.action === 'clarify' && r.via) return r;
    }
    return neg === 'opposite' ? out('refuse', { via: 'opposite' }) : out('refuse');
  }
  const bare = t.replace(LEADING, '').replace(POLITE, '');
  if (!top || top.score < 1 || top.hit.every((w) => NUMERIC.test(w)) || (isHaltChip(top.prompt.chip) && INCREASE.test(t)) || (/^what\s+about\b/.test(bare) && top.score < 2)) return out('clarify'); // "turn it up" names no "Wrap up"; "what about the plants?" is too thin to name one
  // a chip that reports back, as good a match as the top one: "tell me if the kettle is left on at night" is a tie, not a question
  const reports = pool.some((r) => r.score === top.score && r.pen === top.pen && REPORTS_BACK.test(norm(r.prompt.chip)));
  if (INFO_Q.test(bare) || SHOULD_Q.test(bare) || PERMISSION_Q.test(bare) || (QUESTION.test(bare) && (top.score < 2 || !reports)) || (STATE_Q.test(bare) && !reports)) return out('clarify', { via: 'question' });
  if (REPORT_REQ.test(bare) && !reports) return out('clarify', { via: 'check' });
  if (STATEMENT.test(bare)) return out('clarify', { via: 'statement' });
  const halts = halting && asksToHalt(keptOffOut(main, top.prompt)); // "…and keep the lights off" to a chip that rules the lights out is no stop
  if (halts && !isHaltChip(top.prompt.chip)) return out('clarify', { via: 'halt' });
  let ties = pool.filter((r) => r !== top && r.score === top.score && r.pen === top.pen && !weak(r));
  if (ties.length && /\b(?:only|just)\b/.test(t)) {
    const all = [top, ...ties], narrow = all.filter((r) => all.every((o) => o === r || (sameHits(o, r) && narrowness(r.prompt) < narrowness(o.prompt))));
    if (narrow.length === 1) { top = narrow[0]; ties = []; }
  }
  if (ties.length) {
    const byRel = [top, ...ties].map((r) => ({ r, rel: relevance(r, main, prompts) })).sort((a, b) => b.rel - a.rel);
    if (byRel.length > 2 && byRel[1].rel === byRel[2].rel) return out('clarify'); // three or more alike: no pair stands out
    return { action: 'clarify', prompt: byRel[0].r.prompt, score: top.score, via: 'tie', also: byRel[1].r.prompt };
  }
  const other = secondRequest(main, top, prompts, depth); if (other) return out('clarify', { via: 'tie', also: other });
  const why = mismatch(t, top); if (why) return out('clarify', { via: why });
  if (startsHeld(t, top.prompt)) return out('clarify', { via: 'partial' });
  if (halts && !haltFit(main, top)) return out('clarify', { via: 'partial' });
  // A stop that is a rule ("if it's on for 10 minutes, switch it off") or a negated one ("make sure the line doesn't
  // stop") never runs a stop chip on that word alone, and a rule runs only a chip whose plan switches that thing off and
  // never on ("when she's up, turn the night light off" is not the night light).
  if (!halting) {
    const rule = asksToHalt(main, { rule: false }), negd = asksToHalt(main, { neg: false });
    if ((rule || negd) && isHaltChip(top.prompt.chip) && top.hit.every((w) => HALT.has(w))) return out('clarify', { via: 'partial' });
    if (rule) {
      const offs = wanted(plain(main)).filter((k) => k.state === 'off' && !k.kept).map((k) => k.thing), cl = clausesOf(top.prompt, /[.;,—–]|\bbut\b|\bthen\b/);
      if (offs.some((x) => cl.some((c) => c.w.some((y) => like(x, y)) && c.w.includes('on'))) || !haltFit(main.replace(/\b(?:turn|switch|shut|power)\b/g, ' '), top)) return out('clarify', { via: 'partial' });
    }
  }
  const line = conflict(t, top.prompt); if (line) return out('clarify', { via: 'conflict', line });
  // "end the stream but keep recording": a contrasting clause that clearly asks for another chip
  if (main === head) for (const part of rest) {
    if (KEPT_PART.test(part) || /^(?:leave|leaving)\s.*\balone\b|^hands\s+off\b/.test(part)) continue; // "…but leave the sign off" is a state and "…but leave the stage alone" a refusal, both checked by conflict(); "…but keep recording" isn't
    const r = rank(part, prompts, asksToHalt(part)); if (!r[0] || r[0].score < 1 || r[0].prompt === top.prompt) continue;
    if ((r[1] && r[1].score === r[0].score && r[1].pen === r[0].pen) || negates(part, r[0].prompt.chip)) continue;
    return out('clarify', { via: 'tie', also: r[0].prompt });
  }
  return out('run');
}

// A typed "what if …" about a device (or "something") failing: "what if the hall light stops working?", "what happens
// if the kettle breaks". Failures are picked from a request's end card, so the page says how rather than running the
// closest request clean. The failing thing must be a device of the scene (a word of its name or ref) or something,
// anything, a device, a sensor or "it", right before the verb: "what if mum falls" or "what if she dies" is not one.
const WHATIF_FAIL = /\bwhat\s+(?:if|happens\s+if|happens\s+when|would\s+happen\s+if)\s+((?:[a-z0-9'-]+\s+){0,4}?)(?:fail(?:s|ed)?|(?:stops?|stopped)\s+working|breaks?|broke|dies|died|crash(?:es|ed)?|malfunctions?|malfunctioned|disconnects?|disconnected|(?:cuts?|gives?|gave|conks?)\s+out|(?:is|was|goes|went|gets|got)\s+(?:broken|dead|offline|disconnected)|(?:goes|went)\s+(?:down|dark|quiet|silent)|drops?\s+(?:off|out)|dropped\s+(?:off|out)|(?:loses?|lost)\s+(?:its\s+)?(?:power|connection|signal))\b/;
const WHATIF_ANY = new Set(['something', 'anything', 'device', 'devices', 'it', 'one', 'sensor', 'sensors', 'thing']);
// Asking to be told when a device fails ("tell me if the soil sensor stops working", "let me know if the kettle plug
// isn't working", "…when the bed sensor loses power") is the same what-if: every request already flags a device that
// stops, so the page says how to see it rather than run the closest request on the device's name. Here the failing
// thing must end, right before the verb (has, ever, just… aside), in a device word (or something, a device, a sensor,
// it): "tell me if the kettle's auto-off fails" is a kettle request, not the plug failing, and "…fails to read" is no
// failure. "Goes dark" or "goes quiet" is not one either: "tell me if the hall light goes dark" is the light going off.
const TELL_FAIL = /\b(?:tell|let|message|text|alert|warn|notify|email|ping)\s+(?:me|us)\s+(?:know\s+)?(?:if|when|whenever|once|as\s+soon\s+as)\s+((?:[a-z0-9'-]+\s+){1,5}?)(?:fail(?:s|ed)?(?!\s+to\b)|(?:stops?|stopped)\s+(?:working|responding)|breaks?(?:\s+down)?|broke|dies|died|crash(?:es|ed)?|malfunctions?|malfunctioned|disconnects?|disconnected|(?:is|was|goes|went|gets|got)\s+(?:broken|dead|offline|disconnected|faulty)|(?:isn'?t|is\s+not|wasn'?t|was\s+not)\s+(?:working|responding)|(?:doesn'?t|does\s+not|didn'?t)\s+work|(?:cuts?|gives?|gave|conks?)\s+out|drops?\s+(?:off|out)|dropped\s+(?:off|out)|(?:loses?|lost)\s+(?:its\s+)?(?:power|connection|signal))\b/;
const TELL_AUX = /^(?:has|have|had|is|was|ever|just|suddenly|really|completely|actually|then|also)$/;
export function asksWhatIfFails(text, devices = {}) {
  const t = norm(text), own = new Set(Object.values(devices).flatMap((d) => words(norm(`${d?.name || ''} ${d?.ref || ''}`))).filter((w) => !STOP.has(w)));
  const isDev = (w) => WHATIF_ANY.has(w) || own.has(w) || own.has(w.replace(/s$/, ''));
  const m = WHATIF_FAIL.exec(t); if (m && words(m[1]).some(isDev)) return true;
  const k = TELL_FAIL.exec(t); if (!k) return false;
  return isDev(words(k[1]).filter((w) => !TELL_AUX.test(w)).pop() || '');
}
