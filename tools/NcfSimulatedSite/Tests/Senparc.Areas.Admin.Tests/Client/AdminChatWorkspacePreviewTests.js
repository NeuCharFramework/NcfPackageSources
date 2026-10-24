'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const adminPath = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const page = fs.readFileSync(path.join(adminPath, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
const selector = fs.readFileSync(path.join(adminPath, 'Areas/Admin/Pages/Shared/_ChatModuleSelectorDialog.cshtml'), 'utf8');
const scriptPath = path.join(adminPath, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js');
const template = page.slice(page.indexOf('<div class="admin-chat-container">'), page.indexOf('@section scripts'))
    .replace(/@await Html\.PartialAsync\("[^"]+_ChatModuleSelectorDialog\.cshtml"\)/g, selector)
    .replace(/@AR\["([^"]+)"\]/g, '$1')
    .replace(/<!--[\s\S]*?-->/g, '');

function validateTemplate(markup) {
    const voidTags = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr']);
    const stack = [];
    const tags = /<\/?([a-z][a-z0-9-]*)\b(?:[^>"']|"[^"]*"|'[^']*')*>/gi;
    let panelCount = 0;
    for (const token of markup.matchAll(tags)) {
        const tag = token[1].toLowerCase();
        if (token[0].startsWith('</')) {
            assert.strictEqual(stack.pop()?.tag, tag,
                `Unbalanced ${tag} closing tag can move Workspace UI into an inactive Vue branch.`);
        } else if (!voidTags.has(tag) && !token[0].endsWith('/>')) {
            if (/\bclass="chat-workspace-preview"/.test(token[0])) {
                panelCount++;
                assert.strictEqual(stack[stack.length - 1].tag, 'el-container',
                    'Workspace panel must be a direct child of the Chat split container.');
                assert.ok(!stack.some(parent => parent.launcher),
                    'Workspace panel must not be nested in the no-session v-else branch.');
            }
            stack.push({ tag, launcher: /\bclass="chat-empty-launcher"/.test(token[0]) });
        }
    }
    assert.strictEqual(stack.length, 0, 'Chat template must close every container.');
    assert.strictEqual(panelCount, 1, 'Render exactly one Workspace panel without redundant Drawer fallbacks.');
}

validateTemplate(template);
assert.throws(() => validateTemplate(template.replace(
    /<\/div>\s*(<el-dialog\s+title="AdminChat.AddReferenceModule")/,
    '$1'
)), /Unbalanced|Workspace panel must/,
'The template regression must detect the missing launcher closing div.');
assert.ok(template.includes('workspaceEntryName(entry.relativePath)'),
    'File names must use the WorkspaceEntry RelativePath contract.');
assert.ok(template.includes('formatWorkspaceFileSize(entry.length)'),
    'File sizes must use the WorkspaceEntry Length contract.');

let options;
const requests = [];
const listeners = new Map();
const classes = new Set();
const container = {
    clientWidth: 1800,
    querySelector() {
        return { getBoundingClientRect() { return { width: 250 }; } };
    }
};
const sandbox = {
    Vue: function (value) { options = value; },
    window: { ChatLauncherMixin: {}, innerWidth: 1440 },
    getComputedStyle() {
        return { getPropertyValue() { return '360px'; } };
    },
    document: {
        addEventListener(name, handler) { listeners.set(name, handler); },
        removeEventListener(name) { listeners.delete(name); },
        body: { classList: { add(name) { classes.add(name); }, remove(name) { classes.delete(name); } } }
    },
    service: {
        async get(url, config) {
            requests.push({ url, params: config.params });
            return { data: { success: true, data: url.includes('GetWorkspaceEntriesAsync')
                ? { entries: [{ relativePath: 'output/readme.txt', isDirectory: false, length: 12 }] }
                : {
                    path: config.params.relativePath, isText: true,
                    contentType: config.params.relativePath.endsWith('.md') ? 'text/markdown' : 'text/plain',
                    content: config.params.relativePath.endsWith('.md') ? '# Workspace guide' : 'Workspace text'
                } } };
        }
    },
    console
};
vm.runInNewContext(fs.readFileSync(scriptPath, 'utf8'), sandbox, { filename: scriptPath });
const app = options.data();
Object.entries(options.methods).forEach(([name, method]) => { app[name] = method.bind(app); });
Object.entries(options.computed).forEach(([name, getter]) => {
    Object.defineProperty(app, name, { get: getter.bind(app) });
});
app.$nextTick = callback => callback();
app.$refs = { workspaceContainer: { $el: container } };
app.$notify = notification => { throw new Error(notification.message); };
app.currentSessionId = 42;
app.currentSessionWorkspaceId = 'ws_test';

async function run() {
    app.toggleWorkspacePreview();
    await new Promise(resolve => setImmediate(resolve));
    assert.strictEqual(app.workspacePreviewOpen, true);
    assert.strictEqual(requests[0].params.sessionId, 42);
    assert.strictEqual(requests[0].params.relativePath, '');
    assert.strictEqual(app.workspaceEntries[0].relativePath, 'output/readme.txt');

    await app.handleWorkspaceEntryClick({ relativePath: 'output', isDirectory: true, length: 0 });
    assert.strictEqual(app.workspacePreviewPath, 'output');
    assert.strictEqual(requests[1].params.relativePath, 'output');
    await app.handleWorkspaceEntryClick(app.workspaceEntries[0]);
    assert.strictEqual(requests[2].params.relativePath, 'output/readme.txt');
    assert.strictEqual(app.workspacePreviewData.content, 'Workspace text');

    app.startWorkspacePreviewResize({
        preventDefault() {},
        clientX: 900,
        currentTarget: { parentElement: { getBoundingClientRect() { return { width: 420 }; } } }
    });
    listeners.get('mousemove')({ clientX: 800 });
    assert.strictEqual(app.workspacePreviewWidth, 520, 'Drag width must follow pointer delta, not viewport right edge.');
    listeners.get('mouseup')();
    assert.strictEqual(listeners.size, 0);
    assert.strictEqual(classes.size, 0);
    app.expandWorkspacePreview();
    assert.strictEqual(app.workspacePreviewWidth, 1190,
        'Assets must use all remaining container width, even above 720px or 55vw.');
    assert.strictEqual(container.clientWidth - 250 - app.workspacePreviewWidth, 360,
        'Maximizing assets must preserve the Chat minimum width.');
    container.clientWidth = 1200;
    app.updateWorkspacePreviewLayout();
    assert.strictEqual(app.workspacePreviewWidth, 590, 'Resizing the container must clamp the preview.');
    assert.strictEqual(app.workspacePreviewOverlay, false);
    container.clientWidth = 900;
    app.updateWorkspacePreviewLayout();
    assert.strictEqual(app.workspacePreviewOverlay, true,
        'Use an overlay when the actual container cannot fit both usable panes.');
    app.expandWorkspacePreview();
    assert.strictEqual(app.workspacePreviewWidth, 900);
    app.toggleWorkspacePreview();
    assert.strictEqual(app.workspacePreviewOpen, false);
    assert.strictEqual(requests.length, 3, 'Closing must not reload assets.');

    let renderedMarkdown = null;
    app.formatMessageContent = content => {
        assert.strictEqual(content, '# Workspace guide');
        return '<h1>Workspace guide</h1>';
    };
    app.buildWorkspaceHtmlPreview = async (content, filePath, isMarkdown) => {
        renderedMarkdown = { content, filePath, isMarkdown };
        return '<!doctype html><h1>Workspace guide</h1>';
    };
    app.workspacePreviewSource = true;
    await app.handleWorkspaceEntryClick({ relativePath: 'guide.md', isDirectory: false });
    assert.strictEqual(app.workspacePreviewSource, false, 'Opening Markdown must default to rendered preview.');
    assert.strictEqual(app.workspacePreviewIsDocument, true);
    assert.strictEqual(app.workspacePreviewCanShowSource, true);
    assert.strictEqual(renderedMarkdown.content, '<h1>Workspace guide</h1>');
    assert.strictEqual(renderedMarkdown.filePath, 'guide.md');
    assert.strictEqual(renderedMarkdown.isMarkdown, true, 'Markdown must use the isolated HTML preview pipeline.');
    assert.strictEqual(app.workspacePreviewData.content, '# Workspace guide', 'Source must remain unchanged.');
    assert.strictEqual(app.workspacePreviewSrcdoc, '<!doctype html><h1>Workspace guide</h1>');
    app.workspacePreviewSource = true;
    app.resetWorkspacePreview();
    assert.strictEqual(app.workspaceEntries.length, 0);
    assert.strictEqual(app.workspacePreviewData, null);
    assert.strictEqual(app.workspacePreviewSource, false);
    console.log('AdminChat Workspace preview tests passed.');
}

run().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
