(function (window) {
    'use strict';

    // 本地菜单匹配。识别只在点击后由浏览器进行，不把音频或文本送到服务端。
    const leadingPhrases = byLength([
        '请帮我', '请帮忙', '帮我', '帮忙', '我想要', '我想', '我要', '给我', '请你', '请',
        '打开', '进入', '跳转到', '跳转至', '跳转', '前往', '切换到', '切换至', '切换', '访问', '浏览', '去到',
        '一下', '页面', '界面', '菜单',
        's\'il vous plait', 's\'il te plait', 'por favor', 'пожалуйста',
        'can you', 'could you', 'would you', 'i want to', 'i want', 'i wanna', 'let me', 'let us',
        'please', 'kindly',
        'navigate to', 'switch to', 'take me to', 'jump to', 'go to', 'open up', 'show me',
        'navigate', 'switch', 'open', 'show', 'enter', 'visit', 'launch', 'goto',
        'ouvre moi', 'ouvre-moi', 'affiche moi', 'montre moi', 'allez a', 'aller a', 'va a',
        'ouvrez', 'ouvrir', 'ouvre', 'affiche', 'montre', 'allez', 'aller',
        'abreme', 'muestrame', 'por favor', 'ir a', 've a', 'abrir', 'abre', 'muestra',
        'откройте', 'перейди на', 'перейди к', 'перейти на', 'перейти к', 'открой', 'открыть', 'перейди', 'перейти', 'покажи',
        'を開いてください', 'を開いて', 'を表示してください', 'を表示して', 'に移動して', 'へ移動して',
        '開いて', '開く', '表示して',
        'the', 'a', 'an', 'les', 'des', 'une', 'las', 'los', 'una', 'le', 'la', 'un', 'el',
        'page', 'screen', 'menu', 'view', 'pages',
        'ecran', 'écran', 'pagina', 'página', 'pantalla', 'menu', 'menú',
        'страницу', 'страница', 'странице', 'экран',
        'ページ', '画面', 'メニュー'
    ]);
    const trailingPhrases = byLength([
        'を開いてください', 'を開いて下さい', 'を表示してください', 'を開いて', 'を表示して', 'を開く', 'を表示',
        'に移動して', 'へ移動して', 'に移動', 'へ移動', 'に行って', 'へ行って',
        's\'il vous plait', 's\'il te plait', 'por favor', 'пожалуйста', 'please',
        '页面', '界面', '菜单', '一下',
        'pages', 'page', 'screen', 'menu', 'view',
        'ecran', 'écran', 'pagina', 'página', 'pantalla', 'menú',
        'страницу', 'страница', 'странице', 'меню', 'экран',
        'ページ', '画面', 'メニュー'
    ]);
    const weakPrepositions = ['dentro de', 'de la', 'under', 'dans', 'del', 'des', 'of', 'in', 'de', 'en', 'в'];

    function byLength(list) {
        return list.filter(function (item, index) {
            return item && list.indexOf(item) === index;
        }).sort(function (left, right) {
            return right.length - left.length;
        });
    }

    function compact(value) {
        return String(value || '').toLowerCase().replace(/[\s\-_'"’.]+/g, '');
    }

    function isAscii(phrase) {
        return /^[\u0000-\u007f]+$/.test(phrase);
    }

    function normalize(text) {
        return String(text || '')
            .normalize('NFKC')
            .replace(/[‘’]/g, "'")
            .toLowerCase()
            .replace(/[\u0021-\u0026\u0028-\u002f\u003a-\u0040\u005b-\u0060\u007b-\u007e\u3000-\u303f\uff00-\uff0f\uff1a-\uff20\uff3b-\uff40\uff5b-\uff65「」『』【】（）]/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function stripEdge(text, phrases, atEnd) {
        for (let index = 0; index < phrases.length; index++) {
            const phrase = phrases[index];
            if (!phrase || text.length <= phrase.length) {
                continue;
            }
            if (atEnd) {
                if (isAscii(phrase) ? text.endsWith(' ' + phrase) : text.endsWith(phrase)) {
                    return text.slice(0, -(phrase.length + (isAscii(phrase) ? 1 : 0))).trim();
                }
            } else if (isAscii(phrase) ? text.startsWith(phrase + ' ') : text.startsWith(phrase)) {
                return text.slice(phrase.length).trim();
            }
        }
        return text;
    }

    function stripCommand(text) {
        let current = normalize(text);
        for (let pass = 0; pass < 8; pass++) {
            const next = stripEdge(stripEdge(current, leadingPhrases, false), trailingPhrases, true);
            if (next === current) {
                return current;
            }
            current = next;
        }
        return current;
    }

    function isSafeUrl(url) {
        if (typeof url !== 'string') {
            return false;
        }
        const value = url.trim();
        return value.startsWith('/')
            && !value.startsWith('//')
            && value.indexOf('\\') < 0
            && !/[\s<>"']/.test(value)
            && !/^\/+javascript:/i.test(value);
    }

    function nodeName(node) {
        return node.menuName || node.MenuName || node.name || node.Name || '';
    }

    function nodeChildren(node) {
        return node.children || node.Children || [];
    }

    function indexMenus(nodes, ancestors) {
        const entries = [];
        (nodes || []).forEach(function (node) {
            const name = String(nodeName(node) || '').trim();
            const url = node.url || node.Url || '';
            const id = node.id || node.Id || '';
            const path = ancestors || [];
            if (name && isSafeUrl(url)) {
                entries.push({
                    id: id,
                    name: name,
                    url: url.trim(),
                    ancestors: path,
                    label: path.map(function (ancestor) { return ancestor.name; }).concat(name).join(' / ')
                });
            }
            const nextAncestors = name ? path.concat([{ id: id, name: name }]) : path;
            indexMenus(nodeChildren(node), nextAncestors).forEach(function (entry) {
                entries.push(entry);
            });
        });
        return entries;
    }

    function nameScore(query, name) {
        const left = compact(query);
        const right = compact(name);
        if (!left || !right) {
            return 0;
        }
        if (left === right) {
            return 1000;
        }
        if (left.length < 2) {
            return 0;
        }
        if (right.indexOf(left) >= 0) {
            return Math.max(600, 860 - (right.length - left.length) * 15);
        }
        if (left.indexOf(right) >= 0 && right.length >= 2) {
            return Math.max(600, 720 - (left.length - right.length) * 12);
        }
        return 0;
    }

    function bestAncestorScore(segment, entry) {
        return (entry.ancestors || []).reduce(function (best, ancestor) {
            return Math.max(best, nameScore(segment, ancestor.name));
        }, 0);
    }

    function splitBySeparators(phrase) {
        const separators = ['的', 'の', '/', '／', '>'];
        let selected = '';
        separators.forEach(function (separator) {
            if (!selected && phrase.indexOf(separator) >= 0) {
                selected = separator;
            }
        });
        if (!selected) {
            return null;
        }
        const parts = phrase.split(selected).map(function (part) {
            return part.trim();
        }).filter(Boolean);
        if (parts.length < 2) {
            return null;
        }
        return { parents: parts.slice(0, -1), target: parts[parts.length - 1], explicit: true, weak: false };
    }

    function splitPath(phrase) {
        const hierarchical = splitBySeparators(phrase);
        if (hierarchical) {
            return hierarchical;
        }
        const possessiveMark = "'s ";
        const possessiveAt = phrase.lastIndexOf(possessiveMark);
        if (possessiveAt > 0) {
            const parent = phrase.slice(0, possessiveAt).trim();
            const target = phrase.slice(possessiveAt + possessiveMark.length).trim();
            if (parent && target) {
                return { parents: [parent], target: target, explicit: true, weak: false };
            }
        }
        for (let index = 0; index < weakPrepositions.length; index++) {
            const mark = ' ' + weakPrepositions[index] + ' ';
            const at = phrase.indexOf(mark);
            if (at > 0) {
                const target = phrase.slice(0, at).trim();
                const parent = phrase.slice(at + mark.length).trim();
                if (target && parent) {
                    return { parents: [parent], target: target, explicit: true, weak: true };
                }
            }
        }
        return { parents: [], target: phrase, explicit: false, weak: false };
    }

    function splitSupported(parsed, entries) {
        if (!parsed.weak) {
            return true;
        }
        const targetMatches = entries.some(function (entry) {
            return nameScore(parsed.target, entry.name) >= 750;
        });
        const parentsMatch = parsed.parents.every(function (segment) {
            return entries.some(function (entry) {
                return nameScore(segment, entry.name) >= 750 || bestAncestorScore(segment, entry) >= 750;
            });
        });
        return targetMatches && parentsMatch;
    }

    function scoreEntries(parsed, entries, phrase) {
        const ranked = [];
        entries.forEach(function (entry) {
            const matchedName = nameScore(parsed.target, entry.name);
            if (matchedName < 600) {
                return;
            }
            let parentScore = 0;
            if (parsed.parents.length) {
                const matched = parsed.parents.every(function (segment) {
                    const score = bestAncestorScore(segment, entry);
                    if (score < 750) {
                        return false;
                    }
                    parentScore += score;
                    return true;
                });
                if (!matched) {
                    return;
                }
            }
            ranked.push({
                entry: entry,
                score: matchedName + Math.round(parentScore / Math.max(1, parsed.parents.length) / 10)
            });
        });
        ranked.sort(function (left, right) {
            return right.score - left.score;
        });
        const keyword = parsed.target || phrase;
        if (!ranked.length) {
            return { status: 'none', target: null, choices: [], keyword: keyword, heard: phrase, score: 0, exact: false, weak: !!parsed.weak };
        }
        const close = ranked.filter(function (item) {
            return ranked[0].score - item.score < 50;
        });
        const choices = close.slice(0, 6).map(function (item) { return item.entry; });
        if (close.length > 1) {
            return { status: 'choose', target: null, choices: choices, keyword: keyword, heard: phrase, score: ranked[0].score, exact: false, weak: !!parsed.weak };
        }
        const exact = nameScore(keyword, ranked[0].entry.name) === 1000;
        return { status: 'navigate', target: ranked[0].entry, choices: [], keyword: keyword, heard: phrase, score: ranked[0].score, exact: exact, weak: !!parsed.weak };
    }

    function rank(outcome) {
        if (!outcome || outcome.status === 'none') {
            return outcome ? outcome.score : 0;
        }
        return (outcome.status === 'navigate' ? 100000 : 20000)
            + (outcome.exact ? 10000 : 0)
            + outcome.score
            + (outcome.weak ? -100 : 0);
    }

    function interpret(phrase, entries) {
        const parsed = splitPath(phrase);
        const plain = scoreEntries({ parents: [], target: phrase, explicit: false, weak: false }, entries, phrase);
        if (!parsed.explicit || (parsed.weak && !splitSupported(parsed, entries))) {
            return plain;
        }
        const hierarchical = scoreEntries(parsed, entries, phrase);
        return rank(hierarchical) >= rank(plain) ? hierarchical : plain;
    }

    function match(utterance, entries) {
        const normalized = normalize(utterance);
        const stripped = stripCommand(normalized);
        const phrases = [];
        [stripped, normalized].forEach(function (phrase) {
            if (phrase && phrases.indexOf(phrase) < 0) {
                phrases.push(phrase);
            }
        });
        let best = { status: 'none', target: null, choices: [], keyword: stripped || normalized, heard: stripped || normalized, score: 0, exact: false, weak: false };
        phrases.forEach(function (phrase) {
            const outcome = interpret(phrase, entries || []);
            if (rank(outcome) > rank(best)) {
                best = outcome;
            }
        });
        return best;
    }

    function language() {
        const root = typeof document !== 'undefined' ? document.documentElement : null;
        const lang = ((root && root.lang) || 'zh').toLowerCase();
        if (lang.indexOf('zh') === 0) return 'zh';
        if (lang.indexOf('ja') === 0) return 'ja';
        if (lang.indexOf('fr') === 0) return 'fr';
        if (lang.indexOf('es') === 0) return 'es';
        if (lang.indexOf('ru') === 0) return 'ru';
        return 'en';
    }

    const texts = {
        zh: { title: '语音打开页面', listening: '请说要打开的页面名称', unsupported: '当前浏览器不能语音识别。请使用 Chrome 或 Edge。', denied: '需要允许麦克风后才能语音跳转。', none: '没有找到页面', choose: '有多个相近页面，请选择', heard: '听到' },
        en: { title: 'Open a page by voice', listening: 'Say the name of the page to open', unsupported: 'This browser cannot recognize speech. Use Chrome or Edge.', denied: 'Allow the microphone to jump by voice.', none: 'No matching page', choose: 'Several pages match. Choose one', heard: 'Heard' },
        ja: { title: '音声でページを開く', listening: '開きたいページ名を話してください', unsupported: 'このブラウザは音声認識に対応していません。Chrome または Edge を使ってください。', denied: '音声で移動するにはマイクを許可してください。', none: '該当するページがありません', choose: '候補が複数あります。選択してください', heard: '認識' },
        fr: { title: 'Ouvrir une page à la voix', listening: 'Dites le nom de la page à ouvrir', unsupported: 'Ce navigateur ne reconnaît pas la voix. Utilisez Chrome ou Edge.', denied: 'Autorisez le microphone pour la navigation vocale.', none: 'Aucune page correspondante', choose: 'Plusieurs pages correspondent. Choisissez-en une', heard: 'Entendu' },
        es: { title: 'Abrir una página por voz', listening: 'Diga el nombre de la página que desea abrir', unsupported: 'Este navegador no reconoce la voz. Use Chrome o Edge.', denied: 'Permita el micrófono para saltar por voz.', none: 'No hay una página coincidente', choose: 'Hay varias páginas. Elija una', heard: 'Oído' },
        ru: { title: 'Открыть страницу голосом', listening: 'Назовите страницу, которую нужно открыть', unsupported: 'Этот браузер не распознаёт речь. Используйте Chrome или Edge.', denied: 'Разрешите микрофон для голосового перехода.', none: 'Страница не найдена', choose: 'Подходит несколько страниц. Выберите одну', heard: 'Распознано' }
    };

    function text(key) {
        const table = texts[language()] || texts.en;
        return table[key] || texts.en[key] || '';
    }

    function recognitionLang() {
        const lang = ((document.documentElement && document.documentElement.lang) || 'zh-CN').toLowerCase();
        if (lang.indexOf('zh') === 0) return 'zh-CN';
        if (lang.indexOf('ja') === 0) return 'ja-JP';
        if (lang.indexOf('fr') === 0) return 'fr-FR';
        if (lang.indexOf('es') === 0) return 'es-ES';
        if (lang.indexOf('ru') === 0) return 'ru-RU';
        return 'en-US';
    }

    function currentMenus() {
        const store = window.Store;
        const list = store && store.state && store.state.navMenu && store.state.navMenu.navMenuList;
        return indexMenus(list || []);
    }

    // 菜单名只来自当前账号的菜单接口，不在脚本中保存页面清单。
    function menuListFromResponse(response) {
        const body = response && response.data;
        const payload = body && body.data && typeof body.data === 'object' ? body.data : body;
        const list = payload && (payload.menuList || payload.MenuList);
        return Array.isArray(list) ? list : null;
    }

    function loadLiveMenus() {
        const http = window.service;
        if (!http || typeof http.get !== 'function') {
            console.debug('[ncf-voice] menu source', 'sidebar');
            return Promise.resolve(currentMenus());
        }
        return http.get('/Admin/index?handler=MenuResource', { customAlert: true }).then(function (response) {
            const list = menuListFromResponse(response);
            if (!list) {
                console.debug('[ncf-voice] menu source', 'sidebar-fallback');
                return currentMenus();
            }
            const menus = indexMenus(list);
            console.debug('[ncf-voice] menu source', 'live', menus.length);
            return menus;
        }).catch(function (error) {
            console.debug('[ncf-voice] menu refresh failed', error && error.message);
            return currentMenus();
        });
    }

    let active = null;

    function show(vm, message, choices) {
        vm.voiceNavMessage = message;
        vm.voiceNavChoices = choices || [];
        vm.voiceNavPanelVisible = true;
    }

    function stop(vm) {
        const session = active;
        active = null;
        if (session) {
            session.cancelled = true;
            try {
                session.recognition.abort();
            } catch (error) {
                console.debug('[ncf-voice] stop', error && error.message);
            }
        }
        if (vm) {
            vm.voiceNavListening = false;
        }
    }

    function choose(vm, choice) {
        if (!choice || !isSafeUrl(choice.url)) {
            console.debug('[ncf-voice] blocked url', choice && choice.url);
            return;
        }
        stop(vm);
        console.debug('[ncf-voice] navigate', choice.label, choice.url);
        window.location.assign(choice.url);
    }

    function finish(vm, session) {
        if (session.error === 'not-allowed' || session.error === 'service-not-allowed') {
            show(vm, text('denied'), []);
            console.debug('[ncf-voice] denied', session.error);
            return;
        }
        const transcripts = (session.finals || []).map(function (item) { return normalize(item); }).filter(Boolean);
        if (!transcripts.length) {
            show(vm, text('none'), []);
            console.debug('[ncf-voice] empty', session.error || '');
            return;
        }
        const pending = session.menus || Promise.resolve(currentMenus());
        pending.then(function (menus) {
            if (session.cancelled) {
                return;
            }
            let best = null;
            transcripts.forEach(function (transcript) {
                const outcome = match(transcript, menus || []);
                outcome.transcript = transcript;
                if (!best || rank(outcome) > rank(best)) {
                    best = outcome;
                }
            });
            console.debug('[ncf-voice] result', {
                transcripts: transcripts,
                keyword: best.keyword,
                status: best.status,
                target: best.target && best.target.label
            });
            if (best.status === 'navigate') {
                choose(vm, best.target);
                return;
            }
            const prefix = text('heard') + ': ' + (best.keyword || transcripts[0]) + '. ';
            show(vm, prefix + text(best.status === 'choose' ? 'choose' : 'none'), best.choices);
        });
    }

    function toggle(vm) {
        if (active) {
            stop(vm);
            vm.voiceNavPanelVisible = false;
            vm.voiceNavMessage = '';
            vm.voiceNavChoices = [];
            console.debug('[ncf-voice] cancelled');
            return;
        }
        const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Recognition) {
            show(vm, text('unsupported'), []);
            console.debug('[ncf-voice] unsupported');
            return;
        }
        const recognition = new Recognition();
        const session = { recognition: recognition, cancelled: false, finals: [], error: '', menus: loadLiveMenus() };
        active = session;
        recognition.lang = recognitionLang();
        recognition.continuous = false;
        recognition.interimResults = true;
        recognition.maxAlternatives = 3;
        vm.voiceNavListening = true;
        vm.voiceNavChoices = [];
        vm.voiceNavMessage = text('listening');
        vm.voiceNavPanelVisible = true;
        recognition.onresult = function (event) {
            const finals = [];
            let interim = '';
            for (let index = 0; index < event.results.length; index++) {
                const result = event.results[index];
                if (result.isFinal) {
                    for (let alternative = 0; alternative < result.length; alternative++) {
                        finals.push(result[alternative].transcript);
                    }
                } else if (result[0]) {
                    interim += result[0].transcript;
                }
            }
            if (finals.length) {
                session.finals = finals;
            }
            if (interim) {
                vm.voiceNavMessage = text('heard') + ': ' + interim;
            }
        };
        recognition.onerror = function (event) {
            session.error = event && event.error || '';
            console.debug('[ncf-voice] error', session.error);
        };
        recognition.onend = function () {
            if (session.cancelled) {
                return;
            }
            if (active === session) {
                active = null;
            }
            vm.voiceNavListening = false;
            finish(vm, session);
        };
        try {
            recognition.start();
            console.debug('[ncf-voice] start', recognition.lang);
        } catch (error) {
            active = null;
            vm.voiceNavListening = false;
            show(vm, text('unsupported'), []);
            console.debug('[ncf-voice] start failed', error && error.message);
        }
    }

    window.NcfVoiceNavigation = {
        indexMenus: indexMenus,
        menuListFromResponse: menuListFromResponse,
        isSafeUrl: isSafeUrl,
        match: match,
        stripCommand: stripCommand,
        text: text,
        toggle: toggle,
        choose: choose,
        stop: stop
    };
})(window);
