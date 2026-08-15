'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/wwwroot/js/AgentsManager/index.js');
const pagePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Areas/Admin/Pages/AgentsManager/Index.cshtml');
const responsePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/DTOs/ChatGroupResponse.cs');
const servicePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/ChatGroupAppService.cs');
const script = fs.readFileSync(scriptPath, 'utf8');
const page = fs.readFileSync(pagePath, 'utf8');
const responseSource = fs.readFileSync(responsePath, 'utf8');
const serviceSource = fs.readFileSync(servicePath, 'utf8');

let capturedOptions = null;
let capturedStartRequest = null;

function Vue(options) {
    capturedOptions = options;
    return options;
}
Vue.component = function () { };
Vue.directive = function () { };

const serviceAM = {
    async post(url, body) {
        capturedStartRequest = { url, body };
        return { data: { success: true } };
    }
};

const context = vm.createContext({
    Vue,
    serviceAM,
    console: { log() { }, warn() { }, error() { } },
    window: { location: { hash: '' } },
    document: {},
    setTimeout() { },
    clearTimeout() { },
    Date,
    Math,
    Number,
    Object,
    Array,
    Error,
    Promise,
    String,
    Map,
    Set
});

function createViewModel() {
    const viewModel = Object.assign({
        $refs: {},
        $set(target, key, value) { target[key] = value; },
        $nextTick(callback) { callback(); },
        $message: { warning() { } }
    }, capturedOptions.data());
    Object.keys(capturedOptions.methods).forEach(name => {
        viewModel[name] = capturedOptions.methods[name].bind(viewModel);
    });
    return viewModel;
}

vm.runInContext(script, context, { filename: scriptPath });
assert.ok(capturedOptions, 'AgentsManager Vue options should be captured.');

const viewModel = createViewModel();
const participants = viewModel.buildGroupStartParticipants({
    chatGroupDto: {
        adminAgentTemplateId: 2,
        adminAgentTemplateName: '主持人',
        enterAgentTemplateId: 1,
        enterAgentTemplateName: '分析师'
    },
    agentTemplateDtoList: [{ id: 1, name: '分析师' }],
    remoteMemberDtoList: [{
        enable: true,
        remoteAgentDto: { id: 8, name: '外部研究员', enable: true }
    }],
    roleAgentTemplateDtoList: [
        { roleName: '群主', agentTemplateDto: { id: 2, name: '主持人' } },
        { roleName: '对接人', agentTemplateDto: { id: 1, name: '分析师' } }
    ]
});

assert.strictEqual(participants.length, 3, 'Local members, role-only members, and remote A2A members should all be mentionable.');
assert.deepStrictEqual(Array.from(participants.find(item => item.name === '主持人').roles), ['群主']);
assert.deepStrictEqual(Array.from(participants.find(item => item.name === '分析师').roles), ['对接人']);
assert.strictEqual(participants.find(item => item.name === '外部研究员').agentKind, 'RemoteA2A');

const textarea = {
    selectionStart: 3,
    selectionEnd: 3,
    focus() { this.focused = true; },
    setSelectionRange(start, end) {
        this.selectionStart = start;
        this.selectionEnd = end;
    }
};
viewModel.$refs.groupStartPromptCommand = { $refs: { textarea } };
viewModel.groupStartForm.promptCommand = '请先';
viewModel.insertGroupStartMention({ name: '主持人' });
assert.strictEqual(viewModel.groupStartForm.promptCommand, '请先 @主持人');
assert.strictEqual(textarea.selectionStart, '请先 @主持人'.length, 'The caret should remain after the inserted mention.');
assert.strictEqual(textarea.focused, true, 'The task description should regain focus after insertion.');

assert.ok(page.includes('groupStartParticipants'), 'The group-start drawer should render mentionable members.');
assert.ok(page.includes('ref="groupStartPromptCommand"'), 'The task description must expose its textarea for caret-aware insertion.');
assert.ok(responseSource.includes('RoleAgentTemplateDtoList'), 'The group detail contract should return role members separately.');
assert.ok(serviceSource.includes('RoleName = "群主"') && serviceSource.includes('RoleName = "对接人"'),
    'The group detail service should supply both group roles.');

viewModel.$refs.groupStartELForm = { resetFields() { } };
viewModel.tabsActiveName = 'third';
viewModel.gettaskListData = async () => { };
const startRequest = {
    name: '从群组详情启动',
    chatGroupId: 5010,
    aiModelId: 1,
    promptCommand: '验证 Group ID 双通道提交'
};

viewModel.saveSubmitFormData('drawerGroupStart', startRequest).then(() => {
    assert.strictEqual(
        capturedStartRequest.url,
        '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.RunGroup?chatGroupId=5010',
        'Group start must submit the selected ID as an explicit query parameter.');
    assert.strictEqual(capturedStartRequest.body.chatGroupId, 5010,
        'Group start must retain the selected ID in the request body for compatibility.');
    assert.ok(serviceSource.includes('[FromBody] ChatGroup_RunGroupRequest request'),
        'RunGroup should explicitly bind its task payload from the request body.');
    assert.ok(serviceSource.includes('[FromQuery] int chatGroupId = 0'),
        'RunGroup should explicitly bind the fallback Group ID from the query string.');
    console.log('Agents group-start request binding tests passed.');
}).catch(error => {
    console.error(error);
    process.exitCode = 1;
});

console.log('Agents group-start mention tests passed.');
