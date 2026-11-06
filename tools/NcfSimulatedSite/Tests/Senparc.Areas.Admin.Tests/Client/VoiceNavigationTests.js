'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const file = path.resolve(__dirname, '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Shared/VoiceNavigation.js');

function load() {
    const context = { window: {}, console };
    vm.runInNewContext(fs.readFileSync(file, 'utf8'), context);
    return context.window.NcfVoiceNavigation;
}

const menus = [
    {
        menuName: '系统管理', url: '', children: [
            { menuName: '用户管理', url: '/Admin/AdminUserInfo/Index', id: 'sys-users', children: [] },
            { menuName: '菜单管理', url: '/Admin/Menu/Index', id: 'sys-menu', children: [] }
        ]
    },
    {
        menuName: '扩展模块', url: '', children: [
            { menuName: '用户管理', url: '/Admin/Xncf/Users', id: 'xncf-users', children: [] },
            { menuName: '知识库', url: '/Admin/KnowledgeBase/Index', id: 'kb', children: [] },
            { menuName: '首页', url: '/Admin/Index', id: 'home', children: [] }
        ]
    },
    { menuName: 'Knowledge Base', url: '/Admin/KnowledgeBase/En', id: 'kb-en', children: [] },
    { menuName: 'User Management', url: '/Admin/Users/En', id: 'users-en', children: [] },
    {
        menuName: 'System Management', url: '', children: [
            { menuName: 'User Management', url: '/Admin/System/Users', id: 'sys-users-en', children: [] }
        ]
    },
    { menuName: '知識ベース', url: '/Admin/KnowledgeBase/Ja', id: 'kb-ja', children: [] },
    { menuName: 'Base de connaissances', url: '/Admin/KnowledgeBase/Fr', id: 'kb-fr', children: [] },
    { menuName: 'Base de conocimiento', url: '/Admin/KnowledgeBase/Es', id: 'kb-es', children: [] },
    { menuName: 'База знаний', url: '/Admin/KnowledgeBase/Ru', id: 'kb-ru', children: [] },
    { menuName: '打开记录', url: '/Admin/OpenLog/Index', id: 'open-log', children: [] },
    { menuName: '危险入口', url: 'javascript:alert(1)', id: 'bad', children: [] }
];

function urls(result) {
    return Array.from(result.choices || [], item => item.url).sort();
}

test('command wrappers are removed and an exact page opens', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    assert.equal(api.stripCommand('请帮我打开一下知识库页面'), '知识库');
    const result = api.match('请打开知识库页面', entries);
    assert.equal(result.status, 'navigate');
    assert.equal(result.target.url, '/Admin/KnowledgeBase/Index');
    assert.equal(api.match('打开首页', entries).target.url, '/Admin/Index');
    assert.equal(entries.some(entry => entry.url.indexOf('javascript:') >= 0), false);
});

test('parent qualifier selects one of two menus with the same name', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    const ambiguous = api.match('打开用户管理', entries);
    assert.equal(ambiguous.status, 'choose');
    assert.deepEqual(urls(ambiguous), ['/Admin/AdminUserInfo/Index', '/Admin/Xncf/Users']);

    const qualified = api.match('打开系统管理的用户管理页面', entries);
    assert.equal(qualified.status, 'navigate');
    assert.equal(qualified.target.url, '/Admin/AdminUserInfo/Index');
    assert.equal(qualified.target.label, '系统管理 / 用户管理');

    const nested = api.match('扩展模块的用户管理', entries);
    assert.equal(nested.status, 'navigate');
    assert.equal(nested.target.url, '/Admin/Xncf/Users');
});

test('english, japanese, french, spanish and russian commands keep the page keyword', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    assert.equal(api.match('please open the knowledge base page', entries).target.url, '/Admin/KnowledgeBase/En');
    assert.equal(api.match("open system management's user management", entries).target.url, '/Admin/System/Users');
    assert.equal(api.match('user management of system management', entries).target.url, '/Admin/System/Users');

    const englishUsers = api.match('can you open the user management page', entries);
    assert.equal(englishUsers.status, 'choose');
    assert.deepEqual(urls(englishUsers), ['/Admin/System/Users', '/Admin/Users/En']);

    assert.equal(api.match('知識ベースを開いて', entries).target.url, '/Admin/KnowledgeBase/Ja');
    assert.equal(api.match('ouvre la page base de connaissances', entries).target.url, '/Admin/KnowledgeBase/Fr');
    assert.equal(api.match('abre la página base de conocimiento', entries).target.url, '/Admin/KnowledgeBase/Es');
    assert.equal(api.match('пожалуйста, открой страницу база знаний', entries).target.url, '/Admin/KnowledgeBase/Ru');
});

test('a menu whose name starts with a command word is not stripped away', () => {
    const api = load();
    const entries = api.indexMenus(menus);
    const result = api.match('打开记录', entries);
    assert.equal(result.status, 'navigate');
    assert.equal(result.target.url, '/Admin/OpenLog/Index');
});

test('menu names come from the current menu response', () => {
    const api = load();
    const source = fs.readFileSync(file, 'utf8');
    assert.equal(source.includes('menuName:'), false);
    const list = api.menuListFromResponse({
        data: {
            success: true,
            data: {
                menuList: [
                    {
                        menuName: '新模块', url: '', children: [
                            { menuName: '新页面', url: '/Admin/New/Index', children: [] }
                        ]
                    }
                ]
            }
        }
    });
    const result = api.match('打开新模块的新页面', api.indexMenus(list));
    assert.equal(result.status, 'navigate');
    assert.equal(result.target.url, '/Admin/New/Index');
});

test('an unfinished recognition result is still usable', () => {
    const api = load();
    const interimOnly = api.pickTranscripts({ finals: [], interim: '打开知识库。' });
    assert.equal(interimOnly.length, 1);
    assert.equal(interimOnly[0], '打开知识库');

    const finalWins = api.pickTranscripts({ finals: ['打开菜单管理'], interim: '打开' });
    assert.equal(finalWins.length, 1);
    assert.equal(finalWins[0], '打开菜单管理');

    const blankFinal = api.pickTranscripts({ finals: ['。'], interim: '用户管理' });
    assert.equal(blankFinal.length, 1);
    assert.equal(blankFinal[0], '用户管理');
});

test('unknown speech does not navigate', () => {
    const api = load();
    const result = api.match('打开一个不存在的页面', api.indexMenus(menus));
    assert.equal(result.status, 'none');
    assert.equal(result.target, null);
});
