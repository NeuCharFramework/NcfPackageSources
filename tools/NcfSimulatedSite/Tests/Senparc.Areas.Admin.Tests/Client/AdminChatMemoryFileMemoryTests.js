'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');

const adminPath = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const script = fs.readFileSync(
    path.join(adminPath, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'),
    'utf8');
const template = fs.readFileSync(
    path.join(adminPath, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'),
    'utf8');
const readme = fs.readFileSync(
    path.join(adminPath, 'readme.md'),
    'utf8');

const harnessItemsStart = script.indexOf('harnessOptionItems() {');
const harnessItemsEnd = script.indexOf('\n    },', harnessItemsStart);
const harnessItems = script.slice(harnessItemsStart, harnessItemsEnd);

assert.ok(!harnessItems.includes("key: 'fileMemory'"),
    'FileMemory must not be exposed as a duplicate checkbox beside the Memory mode selector.');
assert.ok(script.includes("value: '/harness file-memory on'") &&
    script.includes("value: '/harness file-memory off'"),
    'The /harness command must retain FileMemory controls.');
assert.ok(script.includes('FileMemory → on') && script.includes('FileMemory → off'),
    'The /harness command must use the unified FileMemory name.');
assert.strictEqual(
    template.split('AdminChat.Settings.CapabilityNotice').length - 1,
    2,
    'Both mode popovers must use the localized FileMemory capability notice.');
assert.ok(!readme.includes('MAF FileMemory'),
    'AdminChat documentation must use the unified FileMemory name.');
