'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/NeuCharPivot/Workflow.js');
const pagePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/NeuCharPivot/Workflow.cshtml');
const stylePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/css/Admin/Pages/NeuCharPivot/Workflow.css');

let vueOptions = null;
function Vue(options) { vueOptions = options; }

const sandbox = {
    Vue,
    window: { addEventListener() {}, removeEventListener() {}, setTimeout(callback) { callback(); }, clearTimeout() {} },
    localStorage: { getItem() { return null; }, setItem() {} },
    service: {},
    NeuCharPivotUi: {},
    Set,
    Math,
    Number,
    Object,
    String,
    Promise,
    console
};

vm.createContext(sandbox);
vm.runInContext(fs.readFileSync(scriptPath, 'utf8'), sandbox, { filename: scriptPath });

assert.ok(vueOptions && vueOptions.methods, 'Workflow designer should register a Vue view model.');

const cyclicContext = {
    form: {
        graph: {
            nodes: [
                { id: 'trigger', type: 'manual-trigger', x: 0, y: 0 },
                { id: 'a', type: 'delay', x: 0, y: 0 },
                { id: 'b', type: 'condition', x: 0, y: 0 }
            ],
            edges: [
                { source: 'trigger', target: 'a' },
                { source: 'a', target: 'b' },
                { source: 'b', target: 'a' }
            ]
        }
    },
    canvasSize: {},
    updateCanvasSize: vueOptions.methods.updateCanvasSize
};

vueOptions.methods.autoLayout.call(cyclicContext);
assert.ok(cyclicContext.form.graph.nodes.every(node => Number.isFinite(node.x) && Number.isFinite(node.y)),
    'Auto layout must terminate safely even for a legacy malformed cycle.');

const cycleContext = {
    form: { graph: { edges: [{ source: 'b', target: 'a' }] } }
};
assert.strictEqual(vueOptions.methods.wouldCreateCycle.call(cycleContext, 'a', 'b'), true,
    'The designer must reject a connection that closes a cycle.');

const source = { id: 'source', type: 'function' };
const oldTarget = { id: 'old', type: 'delay' };
const newTarget = { id: 'new', type: 'console' };
const connectionContext = {
    editingLocked: false,
    form: { graph: { nodes: [source, oldTarget, newTarget], edges: [{ id: 'old-edge', source: 'source', target: 'old', sourceHandle: 'default' }] } },
    makeId() { return 'new-edge'; },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
connectionContext.canConnect = (...args) => vueOptions.methods.canConnect.call(connectionContext, ...args);
assert.strictEqual(vueOptions.methods.setTarget.call(connectionContext, source, 'default', 'new', true), true,
    'Dragging a normal output to a new target should replace its previous edge.');
assert.deepStrictEqual(connectionContext.form.graph.edges.map(edge => edge.target), ['new']);

const branchSource = { id: 'condition', type: 'condition' };
const branchTarget = { id: 'target', type: 'delay' };
const branchContext = {
    form: { graph: { nodes: [branchSource, branchTarget], edges: [{ source: 'condition', target: 'target', sourceHandle: 'true' }] } },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
assert.strictEqual(vueOptions.methods.canConnect.call(branchContext, branchSource, branchTarget, 'false'), false,
    'A second condition branch must not create two inputs on an ordinary node.');

const functionTarget = { id: 'function-target', type: 'function' };
const functionBranchContext = {
    form: { graph: { nodes: [branchSource, functionTarget], edges: [{ source: 'condition', target: 'function-target', sourceHandle: 'true' }] } },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    wouldCreateCycle: vueOptions.methods.wouldCreateCycle
};
assert.strictEqual(vueOptions.methods.canConnect.call(functionBranchContext, branchSource, functionTarget, 'false'), true,
    'A Function node should accept multiple upstream branches.');

let shortcutSource = null;
let shortcutPrevented = false;
vueOptions.methods.onSaveShortcut.call({
    editing: true,
    editingLocked: false,
    saveWorkflow(options) { shortcutSource = options.source; }
}, {
    metaKey: true,
    ctrlKey: false,
    key: 's',
    preventDefault() { shortcutPrevented = true; }
});
assert.strictEqual(shortcutSource, 'shortcut', 'Command+S should trigger a shortcut save.');
assert.strictEqual(shortcutPrevented, true, 'Command+S should prevent the browser save dialog.');

const page = fs.readFileSync(pagePath, 'utf8');
const styles = fs.readFileSync(stylePath, 'utf8');
assert.ok(page.includes('class="workflow-stage"'), 'Workflow page should render the visual graph stage.');
assert.ok(page.includes("addSimpleNode('condition','条件判断')"), 'Workflow palette should expose condition nodes.');
assert.ok(page.includes("addSimpleNode('aggregate','聚合')"), 'Workflow palette should expose multi-input aggregate nodes.');
assert.ok(page.includes("addSimpleNode('console','Console 打印')"), 'Workflow palette should expose console output nodes.');
assert.ok(page.includes("addSimpleNode('end','结束')"), 'Workflow palette should expose end nodes.');
assert.ok(page.includes('class="edge-delete"'), 'Every edge should expose a midpoint delete control.');
assert.ok(page.includes('workflow-run-dock'), 'Workflow execution should use a persistent status dock instead of a modal.');
assert.ok(page.includes('关联上游 Output'), 'Node parameters should expose upstream output binding controls.');
assert.ok(page.includes('自动保存'), 'Workflow settings should expose the auto-save interval.');
assert.ok(page.includes('Command/Ctrl + S'), 'Workflow save should advertise the system save shortcut.');
assert.ok(!page.includes('<el-dialog'), 'Workflow execution should not keep a modal dialog open.');
assert.match(styles, /\.workflow-list\s*\{[^}]*overflow-y:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The workflow name list should scroll independently without chaining to the editor.');
assert.match(styles, /\.palette-content,\s*\.inspector-content\s*\{[^}]*overflow-y:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The node palette and inspector should each own their vertical scroll area.');
assert.match(styles, /\.workflow-canvas\s*\{[^}]*height:\s*100%;[^}]*overflow:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The canvas should stay inside its own scroll container.');
assert.match(styles, /\.workflow-meta\s*\{[^}]*flex:\s*0 0 auto;/s,
    'The save toolbar should remain outside all scrolling panels.');

console.log('NeuChar Workflow designer tests passed.');
