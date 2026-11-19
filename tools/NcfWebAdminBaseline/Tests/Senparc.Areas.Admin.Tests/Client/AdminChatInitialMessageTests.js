'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');

const launcherPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/ChatLauncherMixin.js');
const launcher = fs.readFileSync(launcherPath, 'utf8');

const directOpenIndex = launcher.indexOf("if (typeof this.openCreatedChatSession === 'function')");
const fallbackNavigationIndex = launcher.indexOf("window.location.href = '/Admin/AdminChat/Chat?sessionId='", directOpenIndex);
const handoffWriteIndex = launcher.indexOf('sessionStorage.setItem(`ncf.admin.chat.initialMessage.${sessionId}`, pendingMessage)');

assert.ok(directOpenIndex >= 0, 'Created sessions must support opening in the current chat page.');
assert.ok(fallbackNavigationIndex > directOpenIndex,
    'The fallback navigation must remain after the direct open path.');
assert.ok(handoffWriteIndex > directOpenIndex && handoffWriteIndex < fallbackNavigationIndex,
    'The initial-message handoff must only be written before the fallback navigation.');
assert.ok(
    launcher.slice(directOpenIndex, fallbackNavigationIndex).includes(
        'sessionStorage.removeItem(`ncf.admin.chat.initialMessage.${sessionId}`);'),
    'The direct open path must clear any stale initial-message handoff before sending.');
