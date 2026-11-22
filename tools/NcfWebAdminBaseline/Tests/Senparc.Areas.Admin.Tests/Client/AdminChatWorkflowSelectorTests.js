'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');

const selectorPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/Shared/_ChatModuleSelectorDialog.cshtml');
const workflowScriptPath = path.resolve(
    __dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/Workflow.js');
const chatPagePath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml');
const chatScriptPath = path.resolve(
    __dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js');

const selector = fs.readFileSync(selectorPath, 'utf8');
const workflowScript = fs.readFileSync(workflowScriptPath, 'utf8');
const chatPage = fs.readFileSync(chatPagePath, 'utf8');
const chatScript = fs.readFileSync(chatScriptPath, 'utf8');

assert.match(
    selector,
    /\/Admin\/NeuCharWorkflow\/Index\?workflowId=' \+ encodeURIComponent\(item\.id\)/,
    'AdminChat Workflow links must pass workflowId as a query parameter.');
assert.ok(
    !selector.includes('/Admin/NeuCharWorkflow/Index#workflow-'),
    'AdminChat must not use the obsolete hash-only Workflow locator.');
assert.ok(
    workflowScript.includes('const workflowId = Number(query.get(\'workflowId\'));'),
    'The Workflow designer must consume the workflowId query parameter emitted by AdminChat.');
assert.match(
    chatPage,
    /class="chat-modules chat-context-references"[\s\S]*v-for="reference in visibleSessionContextReferences"/,
    'AdminChat must render Modules and Workflows in one context reference row.');
assert.match(
    chatPage,
    /popper-class="chat-context-overflow-popover"[\s\S]*v-for="reference in overflowSessionContextReferences"/,
    'AdminChat must expose folded references through a shared overflow panel.');
assert.match(
    chatPage,
    /v-for="reference in overflowSessionContextReferences"[\s\S]*placement="right-start"[\s\S]*trigger="hover"[\s\S]*chat-module-detail-wrapper[\s\S]*reference\.detail\.functions[\s\S]*reference\.detail\.parameters/,
    'Folded Module and Workflow entries must hover-open the same detailed content as visible chips.');
assert.ok(
    chatPage.includes('popper-class="chat-context-overflow-popover"') &&
        chatPage.includes('class="chat-context-popover-list"') &&
        !chatPage.includes('toggleSessionContextReference(reference.key)') &&
        !chatScript.includes('toggleSessionContextReference(key)') &&
        chatScript.includes('visibleSessionContextReferences()') &&
        chatScript.includes('overflowSessionContextReferences()'),
    'AdminChat must provide hover-only folded reference state without manual accordion interaction.');

console.log('AdminChat Workflow selector tests passed.');
