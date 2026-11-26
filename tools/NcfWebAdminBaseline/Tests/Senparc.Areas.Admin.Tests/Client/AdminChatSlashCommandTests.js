'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js');
let options;
let openedAdminPage = '';
const sandbox = {
    Vue: function (value) { options = value; },
    window: {
        ChatLauncherMixin: {},
        INITIAL_DATA: {},
        innerWidth: 1440,
        innerHeight: 500,
        parent: {
            ncfOpenAdminPage(url) {
                openedAdminPage = url;
                return true;
            }
        }
    },
    ncfT(key) { return key; },
    document: {
        addEventListener() {},
        removeEventListener() {},
        body: { classList: { add() {}, remove() {} } },
        querySelector() { return null; }
    },
    getComputedStyle() { return { getPropertyValue() { return '360px'; } }; },
    service: { get: async () => ({ data: { data: { options: [] } } }) },
    console,
    setTimeout,
    clearTimeout
};
vm.runInNewContext(fs.readFileSync(scriptPath, 'utf8'), sandbox, { filename: scriptPath });

const app = options.data();
Object.entries(options.methods).forEach(([name, method]) => { app[name] = method.bind(app); });
app.$nextTick = callback => callback();
app.$message = { error() {}, success() {}, info() {} };
app.$refs = {
    promptLauncherInput: {
        $el: { closest() { return null; }, querySelector() { return null; } }
    }
};
app.chatCommandSuggestions = [{
    command: '/run-agent-group',
    label: 'Run an Agent group',
    hint: 'Choose a group',
    moduleName: 'Agents',
    accentColor: '#6374d7',
    parameters: [{
        name: 'group',
        label: 'Agent group',
        hint: 'Choose an enabled group',
        placeholder: 'ChatGroupID',
        prefix: '--group=',
        required: false,
        isPositional: false,
        allowFreeform: true,
        hasOptionsProvider: false,
        options: [
            { value: '12', label: 'Support Team', description: 'Handles support requests' },
            { value: '27', label: 'Research Team', description: 'Performs research' }
        ]
    }]
}];

const coreSuggestions = [{
    command: '/mode',
    label: 'Switch mode',
    hint: 'Mode',
    values: [],
    parameters: []
}];
app.chatCommandSuggestions = coreSuggestions.concat(app.chatCommandSuggestions);

app.chatInputText = '/mode';
app.chatCommandMenuOpen = true;
app.chatCommandActiveIndex = 0;
const enter = { key: 'Enter', preventDefault() { this.defaultPrevented = true; } };
assert.strictEqual(app.handleSlashCommandKeydown(enter, 'chatInputText'), true);
assert.strictEqual(app.launcherChatMode, 'simple', 'Selecting a built-in mode must execute it immediately.');
assert.strictEqual(app.chatInputText, '');

app.chatInputText = '/run-agent-group';
let choices = app.slashCommandOptions('chatInputText');
assert.strictEqual(choices.length, 2, 'An exact command must immediately show its first parameter choices.');
assert.strictEqual(choices[0].displayValue, '--group=12');

const highlighted = app.highlightSlashCommandText('Research Team', 'search');
assert.strictEqual(highlighted, 'Re<mark>search</mark> Team');
assert.strictEqual(
    app.highlightSlashCommandText('<Research>', 'Research'),
    '&lt;<mark>Research</mark>&gt;',
    'Highlighted module data must remain HTML-escaped.');

app.chatCommandMenuOpen = true;
app.chatCommandActiveIndex = 1;
const tab = { key: 'Tab', preventDefault() { this.defaultPrevented = true; } };
assert.strictEqual(app.handleSlashCommandKeydown(tab, 'chatInputText'), true);
assert.strictEqual(app.chatInputText, '/run-agent-group --group=27 ');
assert.strictEqual(tab.defaultPrevented, true);

app.chatInputText = '/run-agent-group';
app.chatCommandActiveIndex = 0;
app.chatCommandMenuOpen = true;
const number = { key: '1', altKey: false, preventDefault() { this.defaultPrevented = true; } };
assert.strictEqual(app.handleSlashCommandKeydown(number, 'chatInputText'), true);
assert.strictEqual(app.chatInputText, '/run-agent-group --group=12 ');

app.chatCommandMenuOpen = false;
app.chatInputText = '/run-agent-group';
options.watch.chatInputText.call(app, '/run-agent-group');
assert.strictEqual(app.chatCommandMenuOpen, true, 'Programmatic footer typing must refresh the command popover.');

app.chatCommandMenuOpen = true;
app.$refs.promptLauncherInput.$el.closest = () => ({
    getBoundingClientRect() { return { top: 40, bottom: 320 }; }
});
app.positionSlashCommandPopover('chatInputText');
assert.strictEqual(app.chatCommandPopoverBelow, true, 'Limited space above the input must flip the popover below.');
assert.ok(app.chatCommandPopoverMaxHeight <= 162, 'Popover height must respect the available space below.');

const page = fs.readFileSync(path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml'),
'utf8');
const styles = fs.readFileSync(path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/css/Admin/Pages/AdminChat/ChatLauncher.css'),
'utf8');
assert.ok(page.includes(":class=\"{ 'is-below': chatCommandPopoverBelow }\""));
assert.ok(styles.includes('overflow: visible;'), 'The launcher card must not clip the adaptive popover.');

app.workspaceArtifactExpandedMessages = {};
app.$set = (target, key, value) => { target[key] = value; };
const changedMessage = {
    id: 9,
    attachments: [
        { relativePath: 'src/a.cs', isWorkspaceArtifact: true, changeType: 'created' },
        { relativePath: 'src/b.cs', isWorkspaceArtifact: true, changeType: 'changed' },
        { relativePath: 'src/c.cs', isWorkspaceArtifact: true, changeType: 'changed' },
        { relativePath: 'src/d.cs', isWorkspaceArtifact: true, changeType: 'deleted' }
    ]
};
assert.deepStrictEqual(
    JSON.parse(JSON.stringify(app.workspaceArtifactStats(changedMessage))),
    { total: 4, created: 1, changed: 2, deleted: 1 });
assert.strictEqual(app.workspaceArtifactsExpanded(changedMessage), false);
app.toggleWorkspaceArtifacts(changedMessage);
assert.strictEqual(app.workspaceArtifactsExpanded(changedMessage), true);
app.openMessageAttachmentAction({
    actionType: 'agents-task',
    actionTargetId: 42,
    name: 'Support Group'
});
assert.ok(openedAdminPage.endsWith('#tab=third&taskId=42'), 'Agent task actions must reuse or open an Admin tab.');
const previewAction = { actionType: 'agents-task', actionTargetId: 42, name: 'Support Group' };
app.openAgentTaskPreview(previewAction);
assert.strictEqual(app.agentTaskPreviewVisible, true);
assert.strictEqual(app.agentTaskPreviewAction, previewAction);
assert.ok(
    app.agentTaskUrl(previewAction, true).includes('?embedded=1&ncfTab=1#tab=third&taskId=42'),
    'Live preview must embed the real Agents Manager task page so stream and HIL UI remain available.');

app.currentSessionId = 7;
app.chatCommandMenuOpen = false;
app.agentTaskPreviewVisible = false;
app.agentTaskPreviewAction = null;
const liveMessages = [];
app.sessionRuns[7] = {
    messages: liveMessages,
    liveTrajectoryId: 0,
    liveTrajectoryEvents: [],
    livePendingApprovals: []
};
app.finishActiveTask = () => {};
app.handleLiveStreamEvent('assistant-message', {
    id: 123,
    roleType: 1,
    content: '任务已创建。',
    attachments: [previewAction]
}, 'temp-1', 7);
assert.strictEqual(app.agentTaskPreviewVisible, true,
    'A completed chat tool response containing an Agent task must automatically open live preview.');

console.log('AdminChat slash command tests passed.');
