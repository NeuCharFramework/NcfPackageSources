'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');

const adminRoot = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const pagePath = path.join(adminRoot, 'Areas/Admin/Pages/AdminChat/Chat.cshtml');
const scriptPath = path.join(adminRoot, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js');
const page = fs.readFileSync(pagePath, 'utf8');
const script = fs.readFileSync(scriptPath, 'utf8');
const shottingServicePath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.PromptRange/Domain/Services/PromptShottingService.cs');
const shottingService = fs.readFileSync(shottingServicePath, 'utf8');

assert.match(page, /message\.roleType === 0[\s\S]*class="message-hover-actions"/);
assert.match(page, /class="chat-selection-shot"[\s\S]*openPromptShotting\(selectionShotText\)/);
assert.match(page, /prompt-shotting-dialog[\s\S]*savePromptShot\(true\)/);
assert.match(page, /promptShottingResult\.promptCode[\s\S]*openPromptShottingTarget/);
assert.match(script, /GetPromptShottingRangesAsync/);
assert.match(script, /handleMessageSelectionEnd[\s\S]*getRangeAt\(0\)/);
assert.match(script, /savePromptShot\(run\)[\s\S]*shotting: \{[\s\S]*run: !!run[\s\S]*messageId: this\.promptShottingLength > 0/);
assert.match(script, /payload\.message[\s\S]*this\.\$set\(this\.messageList/);
assert.match(script, /openPromptShottingTarget[\s\S]*\/Admin\/PromptRange\/Prompt#/);
assert.match(script, /async copyMessageContent[\s\S]*showChatToast\('success'/);
assert.match(script, /async writeClipboardText[\s\S]*Clipboard API copy failed; using the DOM fallback/);
assert.match(script, /showChatToast\(type, message\)[\s\S]*this\.\$message\(\{ type, message \}\)/);

assert.match(script, /```\(html\|htm\|xhtml\|svg\)/);
assert.match(script, /sandbox="" referrerpolicy="no-referrer"/);
assert.match(script, /default-src 'none'; img-src data: blob:/);
assert.match(script, /FORBID_TAGS: \['script', 'foreignObject', 'iframe', 'object', 'embed'\]/);
assert.match(script, /data-rich-action="toggle"/);
assert.match(script, /data-rich-action="download"/);
assert.ok(!shottingService.includes('Task.WhenAll'), 'Range loading must not query one scoped DbContext concurrently.');
assert.match(shottingService, /Note = "AdminChat\.Shot"/, 'PromptItem notes must stay within the 20-character column limit.');

const resourceDirectory = path.join(adminRoot, 'Resources');
for (const file of fs.readdirSync(resourceDirectory).filter(name => /^AdminResource(?:\..+)?\.resx$/.test(name))) {
    const resource = fs.readFileSync(path.join(resourceDirectory, file), 'utf8');
    assert.match(resource, /name="AdminChat\.PromptShotting"/, `${file} must localize Prompt Shotting.`);
    assert.match(resource, /name="AdminChat\.PromptPreview\.Download"/, `${file} must localize preview downloads.`);
}

console.log('AdminChat Prompt Shotting tests passed.');
