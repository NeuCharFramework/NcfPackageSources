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

const selector = fs.readFileSync(selectorPath, 'utf8');
const workflowScript = fs.readFileSync(workflowScriptPath, 'utf8');

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

console.log('AdminChat Workflow selector tests passed.');
