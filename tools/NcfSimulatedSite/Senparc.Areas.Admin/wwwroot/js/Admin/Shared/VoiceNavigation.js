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
        return String(value || '').toLowerCase().replace(/[\s\-_'"’./／]+/g, '');
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

    function isSetupPage(name) {
        return compact(name) === compact('设置/执行');
    }

    function sameMenuNode(left, right) {
        if (!left || !right) {
            return false;
        }
        if (left.id && right.id) {
            return String(left.id) === String(right.id);
        }
        return compact(left.name) === compact(right.name);
    }

    function moduleNodes(entries) {
        const nodes = [];
        const seen = {};
        (entries || []).forEach(function (entry) {
            if (!isSetupPage(entry.name)) {
                return;
            }
            const ancestors = entry.ancestors || [];
            const moduleNode = ancestors[ancestors.length - 1];
            if (!moduleNode || !moduleNode.name) {
                return;
            }
            const key = moduleNode.id || moduleNode.name;
            if (seen[key]) {
                return;
            }
            seen[key] = true;
            nodes.push(moduleNode);
        });
        return nodes;
    }

    function pagesInside(entries, moduleNode) {
        return (entries || []).filter(function (entry) {
            return (entry.ancestors || []).some(function (ancestor) {
                return sameMenuNode(ancestor, moduleNode);
            });
        });
    }

    // XNCF 模块节点没有自己的地址。第一个子节点是「设置/执行」，真正的首页是它后面的第一个页面。
    function moduleLanding(entries, moduleNode) {
        const pages = pagesInside(entries, moduleNode);
        const setupIndex = pages.findIndex(function (page) {
            const parent = (page.ancestors || [])[page.ancestors.length - 1];
            return isSetupPage(page.name) && sameMenuNode(parent, moduleNode);
        });
        if (setupIndex < 0) {
            return null;
        }
        return pages[setupIndex + 1] || null;
    }

    function moduleFallback(phrase, entries) {
        const keyword = phrase;
        const ranked = [];
        moduleNodes(entries).forEach(function (moduleNode) {
            const phraseKey = compact(phrase);
            const moduleKey = compact(moduleNode.name);
            // 只有说出的就是模块名时才进入模块。更长的句子里偶然包含模块名，不能把具体页面请求改写成模块首页。
            if (!phraseKey || !moduleKey || phraseKey.length > moduleKey.length) {
                return;
            }
            const matchedName = nameScore(phrase, moduleNode.name);
            if (matchedName < 600) {
                return;
            }
            const page = moduleLanding(entries, moduleNode);
            if (!page) {
                return;
            }
            ranked.push({
                entry: page,
                score: matchedName,
                exact: matchedName === 1000
            });
            console.debug('[ncf-voice] module landing', moduleNode.name, page.label);
        });
        ranked.sort(function (left, right) {
            return right.score - left.score || (right.exact ? 1 : 0) - (left.exact ? 1 : 0);
        });
        if (!ranked.length) {
            return { status: 'none', target: null, choices: [], keyword: keyword, heard: phrase, score: 0, exact: false, weak: false };
        }
        const close = ranked.filter(function (item) {
            return ranked[0].score - item.score < 50;
        });
        const choices = close.slice(0, 6).map(function (item) { return item.entry; });
        if (close.length > 1) {
            return { status: 'choose', target: null, choices: choices, keyword: keyword, heard: phrase, score: ranked[0].score, exact: false, weak: false };
        }
        return {
            status: 'navigate',
            target: ranked[0].entry,
            choices: [],
            keyword: keyword,
            heard: phrase,
            score: ranked[0].score,
            exact: ranked[0].exact,
            weak: false
        };
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
            const direct = interpret(phrase, entries || []);
            const outcome = direct.status === 'none' ? moduleFallback(phrase, entries || []) : direct;
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
        zh: { title: '语音打开页面', stop: '结束并打开页面', close: '关闭', listening: '请说要打开的页面名称，说完后再次点击麦克风', placeholder: '或输入页面名称后回车', unsupported: '当前浏览器不能语音识别。请直接输入页面名称后回车。', requesting: '请在浏览器弹出的提示中允许使用麦克风。', denied: '需要允许麦克风后才能语音跳转，也可以直接输入页面名称。', blocked: '浏览器已经阻止了麦克风，不会再弹出申请。请在地址栏把麦克风改为允许；如果系统也拦截了浏览器，请在系统设置里允许该浏览器使用麦克风。也可以直接输入页面名称。', unavailable: '当前地址不能申请麦克风。请改用 HTTPS 打开，或直接输入页面名称。', system: '网站已经允许麦克风，但系统拦住了浏览器，所以不会再弹出申请。请到系统设置的隐私与安全性中，允许当前浏览器使用麦克风。也可以直接输入页面名称。', service: '浏览器没有开放语音识别。请直接输入页面名称后回车。', network: '语音服务暂时连不上。请直接输入页面名称后回车。', noSpeech: '没有听清。请再说一次，或输入页面名称后回车。', audio: '没有检测到麦克风。请直接输入页面名称后回车。', none: '没有找到页面', choose: '有多个相近页面，请选择', heard: '听到' },
        en: { title: 'Open a page by voice', stop: 'Finish and open the page', close: 'Close', listening: 'Say the page name, then click the microphone again', placeholder: 'Or type a page name and press Enter', unsupported: 'This browser cannot recognize speech. Type a page name and press Enter.', requesting: 'Allow the microphone in the browser prompt.', denied: 'Allow the microphone, or type a page name.', blocked: 'The microphone is blocked, so the browser will not ask again. Allow it in the address bar. If the system blocked the browser, allow the microphone there too, or type a page name.', unavailable: 'This address cannot request a microphone. Open it with HTTPS, or type a page name.', system: 'The site may use the microphone, but the system is blocking the browser, so no prompt will appear. Allow the browser under the system microphone privacy settings, or type a page name.', service: 'Speech recognition is turned off in this browser. Type a page name and press Enter.', network: 'The speech service is unreachable. Type a page name and press Enter.', noSpeech: 'Nothing was heard. Try again, or type a page name.', audio: 'No microphone was found. Type a page name and press Enter.', none: 'No matching page', choose: 'Several pages match. Choose one', heard: 'Heard' },
        ja: { title: '音声でページを開く', stop: '終了してページを開く', close: '閉じる', listening: 'ページ名を話してから、もう一度マイクを押してください', placeholder: 'またはページ名を入力して Enter', unsupported: 'このブラウザは音声認識に対応していません。ページ名を入力して Enter を押してください。', requesting: 'ブラウザの確認でマイクを許可してください。', denied: 'マイクを許可するか、ページ名を入力してください。', blocked: 'マイクは拒否されているため、ブラウザは再度確認しません。アドレスバーで許可してください。システムがブラウザを止めている場合は、システム設定でも許可してください。ページ名を入力することもできます。', unavailable: 'このアドレスではマイクを要求できません。HTTPS で開くか、ページ名を入力してください。', system: 'サイト側はマイクを許可していますが、システムがブラウザを止めているため、確認は出ません。システムのマイク設定でこのブラウザを許可するか、ページ名を入力してください。', service: 'このブラウザでは音声認識が無効です。ページ名を入力して Enter を押してください。', network: '音声サービスに接続できません。ページ名を入力して Enter を押してください。', noSpeech: '聞き取れませんでした。もう一度話すか、ページ名を入力してください。', audio: 'マイクが見つかりません。ページ名を入力して Enter を押してください。', none: '該当するページがありません', choose: '候補が複数あります。選択してください', heard: '認識' },
        fr: { title: 'Ouvrir une page à la voix', stop: 'Terminer et ouvrir la page', close: 'Fermer', listening: 'Dites le nom de la page, puis cliquez à nouveau sur le micro', placeholder: 'Ou saisissez un nom de page et appuyez sur Entrée', unsupported: 'Ce navigateur ne reconnaît pas la voix. Saisissez un nom de page.', requesting: 'Autorisez le microphone dans l’invite du navigateur.', denied: 'Autorisez le microphone, ou saisissez un nom de page.', blocked: 'Le microphone est bloqué : le navigateur ne redemandera pas. Autorisez-le dans la barre d’adresse. Si le système bloque le navigateur, autorisez aussi le microphone dans les réglages, ou saisissez un nom de page.', unavailable: 'Cette adresse ne peut pas demander le microphone. Ouvrez le site en HTTPS, ou saisissez un nom de page.', system: 'Le site peut utiliser le microphone, mais le système bloque le navigateur : aucune invite ne s’affichera. Autorisez le navigateur dans les réglages microphone du système, ou saisissez un nom de page.', service: 'La reconnaissance vocale est désactivée dans ce navigateur. Saisissez un nom de page.', network: 'Le service vocal est injoignable. Saisissez un nom de page.', noSpeech: 'Rien n’a été entendu. Réessayez ou saisissez un nom de page.', audio: 'Aucun microphone détecté. Saisissez un nom de page.', none: 'Aucune page correspondante', choose: 'Plusieurs pages correspondent. Choisissez-en une', heard: 'Entendu' },
        es: { title: 'Abrir una página por voz', stop: 'Terminar y abrir la página', close: 'Cerrar', listening: 'Diga el nombre de la página y pulse el micrófono otra vez', placeholder: 'O escriba un nombre y pulse Intro', unsupported: 'Este navegador no reconoce la voz. Escriba el nombre de la página.', requesting: 'Permita el micrófono en el aviso del navegador.', denied: 'Permita el micrófono o escriba el nombre de la página.', blocked: 'El micrófono está bloqueado y el navegador no volverá a preguntar. Permítalo en la barra de direcciones. Si el sistema bloquea el navegador, permítalo también en los ajustes, o escriba el nombre de la página.', unavailable: 'Esta dirección no puede solicitar el micrófono. Ábrala con HTTPS o escriba el nombre de la página.', system: 'El sitio ya puede usar el micrófono, pero el sistema bloquea el navegador y no aparecerá ningún aviso. Permita el navegador en los ajustes de micrófono del sistema, o escriba el nombre de la página.', service: 'El reconocimiento de voz está desactivado en este navegador. Escriba el nombre de la página.', network: 'No se puede conectar al servicio de voz. Escriba el nombre de la página.', noSpeech: 'No se oyó nada. Inténtelo de nuevo o escriba el nombre.', audio: 'No se detectó un micrófono. Escriba el nombre de la página.', none: 'No hay una página coincidente', choose: 'Hay varias páginas. Elija una', heard: 'Oído' },
        ru: { title: 'Открыть страницу голосом', stop: 'Завершить и открыть страницу', close: 'Закрыть', listening: 'Назовите страницу и нажмите микрофон ещё раз', placeholder: 'Или введите название и нажмите Enter', unsupported: 'Этот браузер не распознаёт речь. Введите название страницы.', requesting: 'Разрешите микрофон в запросе браузера.', denied: 'Разрешите микрофон или введите название страницы.', blocked: 'Микрофон запрещён, браузер больше не спросит. Разрешите его в адресной строке. Если система блокирует браузер, разрешите микрофон и в настройках системы, или введите название страницы.', unavailable: 'По этому адресу нельзя запросить микрофон. Откройте сайт по HTTPS или введите название страницы.', system: 'Сайт может использовать микрофон, но система блокирует браузер, поэтому запрос не появится. Разрешите браузер в системных настройках микрофона или введите название страницы.', service: 'Распознавание речи в этом браузере выключено. Введите название страницы.', network: 'Служба распознавания недоступна. Введите название страницы.', noSpeech: 'Речь не распознана. Повторите или введите название.', audio: 'Микрофон не найден. Введите название страницы.', none: 'Страница не найдена', choose: 'Подходит несколько страниц. Выберите одну', heard: 'Распознано' }
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

    function dismiss(vm) {
        stop(vm);
        if (!vm) {
            return;
        }
        vm.voiceNavPanelVisible = false;
        vm.voiceNavChoices = [];
        vm.voiceNavDraft = '';
        console.debug('[ncf-voice] dismiss');
    }

    function choose(vm, choice) {
        if (!choice || !isSafeUrl(choice.url)) {
            console.debug('[ncf-voice] blocked url', choice && choice.url);
            return;
        }
        dismiss(vm);
        console.debug('[ncf-voice] navigate', choice.label, choice.url);
        if (window.ncfOpenAdminPage && window.ncfOpenAdminPage(choice.url, choice.label)) {
            return;
        }
        window.location.assign(choice.url);
    }

    function pickTranscripts(session) {
        const finals = (session && session.finals || []).map(function (item) {
            return normalize(item);
        }).filter(Boolean);
        if (finals.length) {
            return finals;
        }
        const interim = normalize(session && session.interim || '');
        return interim ? [interim] : [];
    }

    function describeSpeechError(error, permissionState) {
        if (error === 'not-allowed') {
            if (permissionState === 'denied') {
                return text('blocked');
            }
            if (permissionState === 'granted') {
                return text('system');
            }
            return text('denied');
        }
        if (error === 'service-not-allowed') {
            return text('service');
        }
        if (error === 'unavailable') {
            return text('unavailable');
        }
        if (error === 'network') {
            return text('network');
        }
        if (error === 'audio-capture') {
            return text('audio');
        }
        if (error === 'no-speech') {
            return text('noSpeech');
        }
        return '';
    }

    function failureText(session) {
        return describeSpeechError(session && session.error, session && session.permissionState);
    }

    function releaseStream(stream) {
        const tracks = stream && typeof stream.getTracks === 'function' ? stream.getTracks() : [];
        tracks.forEach(function (track) {
            try {
                track.stop();
            } catch (error) {
                console.debug('[ncf-voice] release microphone', error && error.message);
            }
        });
    }

    function classifyMicrophoneError(error) {
        const name = error && error.name || '';
        console.debug('[ncf-voice] microphone', name || (error && error.message) || '');
        if (name === 'NotFoundError' || name === 'DevicesNotFoundError' || name === 'OverconstrainedError') {
            return 'missing';
        }
        if (name === 'SecurityError') {
            return 'unavailable';
        }
        return 'denied';
    }

    // 供测试和点击处理共用。必须在用户点击的同一轮里调用，浏览器才会弹出授权。
    function requestMicrophone(nav) {
        const devices = nav && nav.mediaDevices;
        if (!devices || typeof devices.getUserMedia !== 'function') {
            return Promise.resolve('unavailable');
        }
        try {
            return devices.getUserMedia({ audio: true }).then(function (stream) {
                releaseStream(stream);
                return 'granted';
            }).catch(classifyMicrophoneError);
        } catch (error) {
            return Promise.resolve(classifyMicrophoneError(error));
        }
    }

    function readMicrophonePermission(nav) {
        const permissions = nav && nav.permissions;
        if (!permissions || typeof permissions.query !== 'function') {
            return Promise.resolve('');
        }
        try {
            return Promise.resolve(permissions.query({ name: 'microphone' })).then(function (status) {
                return status && status.state || '';
            }).catch(function (error) {
                console.debug('[ncf-voice] permission query', error && error.message);
                return '';
            });
        } catch (error) {
            console.debug('[ncf-voice] permission query', error && error.message);
            return Promise.resolve('');
        }
    }

    // Chromium 的语音识别在网站尚未获权时会直接失败，且不弹出麦克风申请。
    // Safari 自己会在 start() 时申请，不能先 await，否则会丢掉这次点击。
    function speechPromptIsSeparate(nav) {
        const ua = nav && nav.userAgent || '';
        if (/CriOS|FxiOS|EdgiOS/.test(ua)) {
            return false;
        }
        return /Chrome|Chromium|Edg\/|OPR\//.test(ua);
    }

    function failSession(vm, session, error, permissionState) {
        if (!session || session.cancelled || session.finished) {
            return;
        }
        session.error = error;
        session.permissionState = permissionState || '';
        if (active === session) {
            active = null;
        }
        if (vm) {
            vm.voiceNavListening = false;
        }
        finish(vm, session);
    }

    function armRecognition(vm, session) {
        const recognition = session.recognition;
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
                session.interim = interim;
                vm.voiceNavMessage = text('heard') + ': ' + interim;
                console.debug('[ncf-voice] interim', interim);
            }
        };
        recognition.onerror = function (event) {
            const code = event && event.error || '';
            // 用户结束识别时浏览器可能回报 aborted，这时不能清掉已经听到的文本。
            if (code === 'aborted') {
                console.debug('[ncf-voice] aborted');
                return;
            }
            session.error = code;
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
            session.armed = true;
            console.debug('[ncf-voice] start', recognition.lang);
        } catch (error) {
            active = null;
            session.finished = true;
            vm.voiceNavListening = false;
            show(vm, text('unsupported'), []);
            console.debug('[ncf-voice] start failed', error && error.message);
        }
    }

    function present(vm, transcripts, menus) {
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
            keyword: best && best.keyword,
            status: best && best.status,
            target: best && best.target && best.target.label
        });
        if (best && best.status === 'navigate') {
            choose(vm, best.target);
            return;
        }
        const keyword = best && best.keyword || transcripts[0];
        const prefix = text('heard') + ': ' + keyword + '. ';
        show(vm, prefix + text(best && best.status === 'choose' ? 'choose' : 'none'), best ? best.choices : []);
    }

    function finish(vm, session) {
        if (!session || session.cancelled || session.finished) {
            return;
        }
        session.finished = true;
        const transcripts = pickTranscripts(session);
        if (!transcripts.length) {
            const message = failureText(session) || text('noSpeech');
            show(vm, message, []);
            console.debug('[ncf-voice] empty', session.error || '');
            return;
        }
        const pending = session.menus || Promise.resolve(currentMenus());
        pending.then(function (menus) {
            if (session.cancelled) {
                return;
            }
            present(vm, transcripts, menus);
        });
    }

    function toggle(vm) {
        if (active) {
            const session = active;
            if (!session.armed) {
                stop(vm);
                vm.voiceNavPanelVisible = false;
                vm.voiceNavChoices = [];
                console.debug('[ncf-voice] cancel before start');
                return;
            }
            try {
                // stop() 会把已经听到的内容整理成结果；abort() 会直接丢掉。
                session.recognition.stop();
                console.debug('[ncf-voice] commit');
            } catch (error) {
                console.debug('[ncf-voice] commit failed', error && error.message);
                if (active === session) {
                    active = null;
                }
                vm.voiceNavListening = false;
                finish(vm, session);
            }
            return;
        }
        const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Recognition) {
            show(vm, text('unsupported'), []);
            console.debug('[ncf-voice] unsupported');
            return;
        }
        const recognition = new Recognition();
        const session = {
            recognition: recognition,
            cancelled: false,
            finished: false,
            armed: false,
            finals: [],
            interim: '',
            error: '',
            permissionState: '',
            menus: loadLiveMenus()
        };
        active = session;
        const nav = window.navigator;
        if (!speechPromptIsSeparate(nav)) {
            armRecognition(vm, session);
            return;
        }
        vm.voiceNavListening = false;
        vm.voiceNavChoices = [];
        vm.voiceNavMessage = text('requesting');
        vm.voiceNavPanelVisible = true;
        console.debug('[ncf-voice] request microphone');
        Promise.all([readMicrophonePermission(nav), requestMicrophone(nav)]).then(function (result) {
            if (session.cancelled || active !== session) {
                return;
            }
            const permissionState = result[0];
            const outcome = result[1];
            if (outcome === 'granted') {
                armRecognition(vm, session);
                return;
            }
            const error = outcome === 'missing'
                ? 'audio-capture'
                : (outcome === 'unavailable' ? 'unavailable' : 'not-allowed');
            failSession(vm, session, error, permissionState);
        }).catch(function (error) {
            console.debug('[ncf-voice] microphone failed', error && error.message);
            failSession(vm, session, 'not-allowed', '');
        });
    }

    function submitText(vm, utterance) {
        const phrase = String(utterance || '').trim();
        const session = active;
        if (session) {
            session.cancelled = true;
            session.finished = true;
            try {
                session.recognition.abort();
            } catch (error) {
                console.debug('[ncf-voice] submit stop', error && error.message);
            }
            if (active === session) {
                active = null;
            }
        }
        if (vm) {
            vm.voiceNavListening = false;
        }
        if (!phrase) {
            show(vm, text('placeholder'), []);
            return;
        }
        console.debug('[ncf-voice] submit', phrase);
        const pending = session && session.menus || loadLiveMenus();
        pending.then(function (menus) {
            present(vm, [phrase], menus);
        });
    }

    window.NcfVoiceNavigation = {
        indexMenus: indexMenus,
        menuListFromResponse: menuListFromResponse,
        isSafeUrl: isSafeUrl,
        match: match,
        stripCommand: stripCommand,
        pickTranscripts: pickTranscripts,
        text: text,
        describeSpeechError: describeSpeechError,
        requestMicrophone: requestMicrophone,
        speechPromptIsSeparate: speechPromptIsSeparate,
        toggle: toggle,
        submitText: submitText,
        choose: choose,
        stop: stop,
        dismiss: dismiss
    };
})(window);
