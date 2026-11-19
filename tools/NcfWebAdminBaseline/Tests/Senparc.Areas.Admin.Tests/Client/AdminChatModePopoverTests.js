'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');

const adminPath = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const template = fs.readFileSync(
    path.join(adminPath, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'),
    'utf8');
const script = fs.readFileSync(
    path.join(adminPath, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'),
    'utf8');
const launcher = fs.readFileSync(
    path.join(adminPath, 'wwwroot/js/Admin/Pages/AdminChat/ChatLauncherMixin.js'),
    'utf8');
const styles = fs.readFileSync(
    path.join(adminPath, 'wwwroot/css/Admin/Pages/AdminChat/Chat.css'),
    'utf8');

assert.strictEqual(
    (template.match(/popper-class="admin-chat-mode-popper"/g) || []).length,
    2,
    'Launcher and current-session mode settings must use the same popover.');
assert.ok(template.includes('chatModeOptions'), 'Mode popover must render shared mode metadata.');
assert.ok(template.includes('harnessOptionItems'), 'Mode popover must expose Harness option explanations.');
assert.ok(script.includes('this.chatModeFromValue(responseMode)'),
    'Created-session response mode must be applied instead of assuming Simple.');
assert.ok(launcher.includes('this.setSessionMode(sessionId, responseMode)'),
    'Created-session mode must be retained for later session switches.');
assert.ok(script.includes('当前 ${status(\'todo\')}'),
    'Harness slash-command choices must show the current Todo on/off state.');
assert.ok(script.includes('当前 ${status(\'webSearch\')}'),
    'Harness slash-command choices must show the current Web Search on/off state.');
assert.ok(launcher.includes('harnessOptions: typeof this.buildHarnessOptions'),
    'New-session requests must carry the selected Harness options.');
assert.ok(styles.includes('.admin-chat-container .chat-mode-trigger'),
    'Mode trigger layout must retain a dedicated modern layout.');
assert.ok(styles.includes('.admin-chat-mode-popper .chat-mode-popover-content'),
    'Teleported mode popover content must have a body-level style scope.');
assert.ok(styles.includes('appearance: none'),
    'Teleported mode buttons must reset browser-native button styling.');
assert.ok(!template.includes('launcher-mode-toggle'),
    'The broken three-option radio group must not remain in the new-session launcher.');
