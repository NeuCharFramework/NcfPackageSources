'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const admin = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const shared = path.join(admin, 'wwwroot/js/Admin/Shared');

function setup() {
    let component;
    let dialog;
    let creations = 0;
    const timers = new Map();
    const errors = [];
    const messages = [];
    let timerId = 0;
    const document = {
        documentElement: { lang: 'zh-CN' },
        createElement: () => ({}),
        body: { appendChild() {} }
    };
    function Vue(options) {
        creations++;
        dialog = options.data();
        for (const [key, value] of Object.entries(options.methods)) dialog[key] = value.bind(dialog);
        return dialog;
    }
    Vue.component = (name, value) => {
        assert.equal(name, 'ncf-doc-help');
        component = value;
    };
    Vue.prototype.$message = { error: message => messages.push(message) };
    const context = {
        window: { ncfT: key => key }, document, Vue,
        console: { error: (...args) => errors.push(args) },
        setTimeout: (callback, delay) => { timers.set(++timerId, { callback, delay }); return timerId; },
        clearTimeout: id => timers.delete(id)
    };
    for (const name of ['NcfDocHelpCatalog.js', 'NcfDocHelp.js']) {
        vm.runInNewContext(fs.readFileSync(path.join(shared, name), 'utf8'), context);
    }
    return {
        context, timers, errors, messages, component,
        api: context.window.NcfDocHelp,
        get dialog() { return dialog; },
        get creations() { return creations; }
    };
}

test('Chinese locales use /zh/; all other UI languages use the English root', () => {
    const { api } = setup();
    for (const culture of ['zh', 'zh-CN', 'zh-TW', 'ZH-hans']) {
        assert.equal(api.resolve('mcp.home', culture), 'https://doc.ncf.pub/zh/MCP/home/index.html');
    }
    for (const culture of ['en', 'en-US', 'ja', 'fr', 'es', 'ru', '', undefined, 'zhinvalid']) {
        assert.equal(api.resolve('mcp.home', culture), 'https://doc.ncf.pub/MCP/home/index.html');
    }
    assert.equal(decodeURI(api.resolve('modules.publish', 'zh-CN')).split('#')[1], '切换-发布-状态');
    assert.equal(api.resolve('modules.publish', 'fr').split('#')[1], 'switch-publish-status');
});

test('pilot routing accepts Razor Index aliases and ignores unrelated pages and module IDs', () => {
    const { api, component } = setup();
    for (const route of ['/Admin', '/Admin/', '/ADMIN/Index/?culture=ja#test']) {
        assert.equal(api.pageTopic(route), 'admin.home');
    }
    assert.equal(api.pageTopic('/Admin/XncfModule?uid=secret'), 'modules.manage');
    assert.equal(api.pageTopic('/Admin/MCP/Index/'), 'mcp.home');
    for (const route of ['/Admin/KnowledgeBase/Index', '/Admin/MCP/Other', '/Admin/MCPFake', '', null]) {
        assert.equal(api.pageTopic(route), null);
    }
    assert.equal(component.computed.resolvedTopic.call({ topic: 'modules.publish', pagePath: '/Admin' }), 'modules.publish');
});

test('invalid keys, paths and incomplete bilingual anchors fail explicitly without opening a dialog', () => {
    const state = setup();
    for (const key of ['missing', '__proto__', 'constructor']) assert.throws(() => state.api.resolve(key, 'en'));
    for (const invalid of ['https://evil.test/test.html', '//evil.test/a.html', '../a.html', 'a/../b.html',
        'zh/a.html', 'a.html?x=1', 'a.html#chapter', 'a\\b.html']) {
        state.context.window.NcfDocHelpCatalog.topics.bad = { path: invalid };
        assert.throws(() => state.api.resolve('bad', 'en'));
    }
    state.context.window.NcfDocHelpCatalog.topics.bad = { path: 'test.html', anchors: { zh: 'test' } };
    assert.throws(() => state.api.resolve('bad', 'en'));
    state.api.open('missing');
    assert.equal(state.errors.length, 1);
    assert.deepEqual(state.messages, ['DocHelp.InvalidTopic']);
    assert.equal(state.creations, 0);
});

test('dialog is lazy and shared; stale loads, timeout, retry, locale changes and close are handled', () => {
    const state = setup();
    const opener = { isConnected: true, focus() { this.focused = true; } };
    assert.equal(state.creations, 0);
    assert.equal(state.timers.size, 0);
    state.api.open('modules.publish', opener);
    assert.equal(state.creations, 1);
    const dialog = state.dialog;
    assert.equal(dialog.visible, true);
    assert.equal(dialog.loading, true);
    assert.equal(state.timers.values().next().value.delay, 15000);
    const oldVersion = dialog.frameVersion;
    state.context.document.documentElement.lang = 'ja';
    state.api.open('mcp.home', opener);
    assert.equal(state.creations, 1);
    assert.equal(dialog.url, 'https://doc.ncf.pub/MCP/home/index.html');
    assert.equal(state.timers.size, 1);
    dialog.frameLoaded(oldVersion);
    assert.equal(dialog.loading, true);
    state.timers.values().next().value.callback();
    assert.equal(dialog.failed, true);
    dialog.startLoading();
    assert.equal(dialog.failed, false);
    dialog.frameLoaded(dialog.frameVersion);
    assert.equal(dialog.loading, false);
    assert.equal(state.timers.size, 0);
    dialog.frameFailed(dialog.frameVersion);
    assert.equal(dialog.failed, true);
    dialog.visible = false;
    dialog.cleanup();
    assert.equal(dialog.url, '');
    assert.equal(state.timers.size, 0);
    assert.equal(opener.focused, true);
});

test('all supported UI locales contain every help resource and Razor loads help before page scripts', () => {
    const keys = ['Help', 'Title', 'OpenWindow', 'Close', 'Loading', 'Notice', 'LoadFailed', 'Retry', 'InvalidTopic'];
    for (const locale of ['', '.zh-CN', '.en', '.ja', '.fr', '.es', '.ru']) {
        const resource = fs.readFileSync(path.join(admin, 'Resources/AdminResource' + locale + '.resx'), 'utf8');
        for (const key of keys) assert.equal(resource.split('name="DocHelp.' + key + '"').length - 1, 1);
    }
    const layout = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/Shared/_Layout_Vue.cshtml'), 'utf8');
    assert.ok(layout.includes('page-path="@Context.Request.Path"'));
    assert.ok(layout.indexOf('NcfDocHelpCatalog.js') < layout.indexOf('NcfDocHelp.js'));
    assert.ok(layout.indexOf('NcfDocHelp.js') < layout.indexOf('@RenderSection("scripts"'));
    const page = fs.readFileSync(path.join(admin, 'Areas/Admin/Pages/XncfModule/Index.cshtml'), 'utf8');
    assert.ok(page.includes('<ncf-doc-help topic="modules.publish"></ncf-doc-help>'));
});

test('optional local documentation audit checks every configured topic in both languages', { skip: !process.env.NCF_DOCS_ROOT }, () => {
    const { context } = setup();
    for (const topic of Object.values(context.window.NcfDocHelpCatalog.topics)) {
        for (const locale of ['', 'zh']) {
            const file = path.join(process.env.NCF_DOCS_ROOT, 'docs', locale, topic.path.replace(/\.html$/, '.md'));
            assert.ok(fs.existsSync(file), 'Missing document: ' + file);
        }
    }
});
