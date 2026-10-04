'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const page = fs.readFileSync(path.resolve(__dirname,
    '../../Senparc.Xncf.WeixinManager/Areas/Admin/Pages/WeixinManager/WeixinClaw/Index.cshtml'), 'utf8');
const script = page.match(/<script>\s*([\s\S]*?)<\/script>/)[1]
    .replace(/accounts: @Html\.Raw\([^\n]+\),/, 'accounts: [],');

function harness(responder = async () => ({ data: { data: { list: [] } } })) {
    let options;
    const requests = [];
    const intervals = [];
    const cleared = [];
    const messages = [];
    const notifications = [];
    const context = vm.createContext({
        Vue: function (value) { options = value; },
        service: {
            get(url) { requests.push({ url }); return responder(url); },
            post(url, data) { requests.push({ url, data }); return responder(url, data); }
        },
        setInterval(callback, delay) { intervals.push({ callback, delay }); return intervals.length; },
        clearInterval(id) { cleared.push(id); },
        setTimeout() { return 99; },
        clearTimeout(id) { cleared.push(id); },
        console
    });
    vm.runInContext(script, context);
    const model = Object.assign(options.data(), {
        $message: {
            error: text => messages.push({ type: 'error', text }),
            success: text => messages.push({ type: 'success', text }),
            info: text => messages.push({ type: 'info', text })
        },
        $notify: value => notifications.push(value),
        $set(target, key, value) { target[key] = value; },
        $delete(target, key) { delete target[key]; },
        $nextTick(callback) { callback(); },
        $refs: {}
    });
    for (const [name, method] of Object.entries(options.methods)) model[name] = method.bind(model);
    return { options, model, requests, intervals, cleared, messages, notifications };
}

test('opening the page immediately refreshes connection state before any timer fires', async () => {
    const row = { id: 6, name: 'Weixin', status: '运行中', hasMessageContext: true };
    const h = harness(async () => ({ data: { data: { list: [row] } } }));
    h.options.mounted.call(h.model);
    assert.equal(h.requests.length, 1);
    assert.match(h.requests[0].url, /handler=Ajax$/);
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(h.model.accounts[0].status, '运行中');
    assert.equal(h.model.accounts[0].id, 6);
    assert.equal(h.model.accountLoaded, true);
    assert.equal(h.notifications.length, 0);
    assert.equal(h.intervals[0].delay, 3000);
    assert.match(page, /JsonSerializerDefaults\.Web/);
});

test('initial refresh failure is visible and does not erase known account data', async () => {
    const h = harness(async () => { throw new Error('Connection unavailable'); });
    h.model.accounts = [{ id: 6, status: '已连接' }];
    h.options.mounted.call(h.model);
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(h.model.accounts[0].id, 6);
    assert.equal(h.model.accountLoadError, 'Connection unavailable');
    assert.equal(h.model.accountLoading, false);
    assert.equal(h.messages[0].type, 'error');
    await h.model.loadAccounts();
    assert.equal(h.messages.length, 1);
});

test('refreshes do not overlap and a later inbound message still notifies', async () => {
    let complete;
    const h = harness(() => new Promise(resolve => { complete = resolve; }));
    const pending = h.model.loadAccounts();
    await h.model.loadAccounts();
    assert.equal(h.requests.length, 1);
    complete({ data: { data: { list: [{ id: 6, name: 'Weixin', lastMessageAt: 'first' }] } } });
    await pending;
    const next = h.model.loadAccounts();
    complete({ data: { data: { list: [{ id: 6, name: 'Weixin', lastMessageAt: 'second' }] } } });
    await next;
    assert.equal(h.notifications.length, 1);
});

test('the redundant send panel and its visibility/manual-context state are completely removed', () => {
    const h = harness();
    assert.doesNotMatch(page, /发送个人微信消息<\/h4>|sender\.|sender:|openSend|contextTokenAvailable/);
    assert.equal('sender' in h.model, false);
    h.model.resetComposer({ id: 6, lastMessageFromUserId: 'peer', hasMessageContext: true });
    assert.deepEqual(JSON.parse(JSON.stringify(h.model.composer)),
        { accountId: 6, toUserId: 'peer', replyToRecordId: null, text: '' });
    assert.equal('visible' in h.model.composer, false);
    assert.equal('contextToken' in h.model.composer, false);
    assert.match(page, /v-model="composer\.text"/);
});

test('the conversation composer still sends using the selected inbound record', async () => {
    const h = harness(async url => ({ data: { data: url.includes('handler=Send')
        ? { contextTokenUsed: true } : { list: [] } } }));
    h.model.composer = { accountId: 6, toUserId: 'peer', replyToRecordId: 42, text: 'reply' };
    await h.model.send();
    assert.match(h.requests[0].url, /handler=Send$/);
    assert.deepEqual(h.requests[0].data,
        { accountId: 6, toUserId: 'peer', replyToRecordId: 42, text: 'reply' });
    assert.equal(h.messages[0].type, 'success');
});

test('SILK and video use playback URLs while original downloads remain available', () => {
    assert.doesNotMatch(page, /contentType !== 'audio\/silk'/);
    assert.match(page, /<audio[\s\S]*?:src="media\.playbackUrl"/);
    assert.match(page, /<video[\s\S]*?:src="media\.playbackUrl"/);
    assert.match(page, /:href="media\.url"/);
    const h = harness();
    const media = { playbackUrl: '/media?playback=true' };
    h.model.mediaPlaybackFailed(media);
    assert.match(h.model.mediaPlaybackErrors[media.playbackUrl], /下载原始文件/);
    h.model.mediaPlaybackReady(media);
    assert.equal(h.model.mediaPlaybackErrors[media.playbackUrl], undefined);
});

test('destroying the page clears account, message and QR polling timers', () => {
    const h = harness();
    h.model.accountRefreshTimer = 1;
    h.model.messageRefreshTimer = 2;
    h.model.loginPollHandle = 99;
    h.options.beforeDestroy.call(h.model);
    assert.deepEqual(h.cleared, [1, 2, 99]);
});
