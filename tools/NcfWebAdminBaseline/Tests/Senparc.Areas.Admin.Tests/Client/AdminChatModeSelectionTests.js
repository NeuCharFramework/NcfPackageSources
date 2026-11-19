'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');

const scriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js');
const script = fs.readFileSync(scriptPath, 'utf8');

const launcherMethodStart = script.indexOf('selectLauncherMode(mode) {');
const launcherMethodEnd = script.indexOf('\n    },', launcherMethodStart);
const chatMethodStart = script.indexOf('selectChatMode(mode) {');
const chatMethodEnd = script.indexOf('\n    },', chatMethodStart);
const launcherMethod = script.slice(launcherMethodStart, launcherMethodEnd);
const chatMethod = script.slice(chatMethodStart, chatMethodEnd);

assert.ok(launcherMethod.includes('const wasSelected = this.launcherChatMode === mode;'),
    'The launcher should detect repeated clicks on the selected mode.');
assert.ok(launcherMethod.includes("this.launcherModePopoverOpen = !wasSelected && mode !== 'simple';"),
    'Selecting a new launcher mode must keep the mode popover open for Harness settings.');
assert.ok(chatMethod.includes('const wasSelected = this.chatMode === mode;'),
    'The current-session selector should detect repeated clicks on the selected mode.');
assert.ok(chatMethod.includes("this.chatModePopoverOpen = !wasSelected && mode !== 'simple';"),
    'Selecting a new current-session mode must keep the mode popover open for Harness settings.');
