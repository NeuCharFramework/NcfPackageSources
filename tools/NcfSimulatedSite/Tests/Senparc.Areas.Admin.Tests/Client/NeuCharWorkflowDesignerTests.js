'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/Workflow.js');
const pagePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Areas/Admin/Pages/NeuCharWorkflow/Index.cshtml');
const stylePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/css/NeuCharWorkflow/Workflow.css');

let vueOptions = null;
function Vue(options) { vueOptions = options; }

const sandbox = {
    Vue,
    window: { addEventListener() {}, removeEventListener() {}, setTimeout(callback) { callback(); }, clearTimeout() {} },
    localStorage: { getItem() { return null; }, setItem() {} },
    service: {},
    NeuCharWorkflowUi: {},
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

const duplicateNode = { id: 'node-1', type: 'delay', name: '等待', x: 80, y: 120, config: { seconds: 3 } };
const duplicateContext = {
    editingLocked: false,
    form: { graph: { nodes: [duplicateNode], edges: [] } },
    selectedNodeId: '',
    makeId() { return 'delay-copy'; },
    canDuplicateNode: vueOptions.methods.canDuplicateNode,
    cancelConnection: vueOptions.methods.cancelConnection,
    updateCanvasSize() {},
    scheduleAutoSave() {}
};
const duplicate = vueOptions.methods.duplicateNode.call(duplicateContext, duplicateNode);
assert.strictEqual(duplicate.id, 'delay-copy', 'Copying a node should assign a new node id.');
assert.strictEqual(duplicate.name, '等待（副本）', 'Copying a node should make the duplicate recognizable.');
assert.deepStrictEqual([duplicate.x, duplicate.y], [120, 160], 'Copying a node should offset the duplicate on the canvas.');
assert.strictEqual(vueOptions.methods.canDuplicateNode.call({}, { type: 'manual-trigger' }), false,
    'The workflow trigger must not be duplicated into an invalid second trigger.');

const panCanvas = { scrollLeft: 100, scrollTop: 80 };
let panPrevented = false;
const panContext = {
    $refs: { canvas: panCanvas },
    contextMenu: { visible: true, node: { id: 'node-1' } },
    dragState: { node: duplicateNode },
    connectionDraft: { sourceId: '', sourceHandle: '', x: 0, y: 0 },
    closeContextMenu: vueOptions.methods.closeContextMenu,
    cancelConnection: vueOptions.methods.cancelConnection,
    startCanvasPan: vueOptions.methods.startCanvasPan,
    onPointerMove: vueOptions.methods.onPointerMove,
    onPointerUp: vueOptions.methods.onPointerUp
};
vueOptions.methods.startCanvasPan.call(panContext, {
    button: 2,
    clientX: 240,
    clientY: 160,
    preventDefault() { panPrevented = true; }
});
vueOptions.methods.onPointerMove.call(panContext, { clientX: 210, clientY: 120 });
assert.deepStrictEqual([panCanvas.scrollLeft, panCanvas.scrollTop], [130, 120],
    'Right-button dragging should move the canvas scroll position.');
assert.strictEqual(panPrevented, true, 'Canvas panning should suppress the browser context menu gesture.');
vueOptions.methods.onPointerUp.call(panContext);
assert.strictEqual(panContext.canvasPan.active, false, 'Canvas panning should stop on mouse release.');

assert.strictEqual(vueOptions.methods.clampCanvasZoom.call({}, 3), 2,
    'Canvas zoom should not exceed the supported maximum.');
assert.strictEqual(vueOptions.methods.clampCanvasZoom.call({}, .1), .5,
    'Canvas zoom should not go below the supported minimum.');

const pointCanvas = { getBoundingClientRect() { return { left: 10, top: 20 }; } };
const pointStage = { getBoundingClientRect() { return { left: -90, top: -180 }; } };
const pointContext = { $refs: { canvas: pointCanvas, stage: pointStage }, canvasZoom: 2 };
const scaledPoint = vueOptions.methods.canvasPoint.call(pointContext, { clientX: 110, clientY: 20 });
assert.strictEqual(scaledPoint.x, 100, 'Scaled pointer X should convert back to world coordinates.');
assert.strictEqual(scaledPoint.y, 100, 'Scaled pointer Y should convert back to world coordinates.');

const zoomCanvas = {
    clientWidth: 400,
    clientHeight: 260,
    scrollLeft: 100,
    scrollTop: 120,
    getBoundingClientRect() { return { left: 10, top: 20 }; }
};
const zoomContext = {
    canvasZoom: 1,
    $refs: { canvas: zoomCanvas, stage: { offsetTop: 36 } },
    clampCanvasZoom: vueOptions.methods.clampCanvasZoom,
    updateCanvasViewport() { this.viewportUpdated = true; },
    $nextTick(callback) { callback(); }
};
vueOptions.methods.setCanvasZoom.call(zoomContext, 1.5, 210, 180);
assert.strictEqual(zoomContext.canvasZoom, 1.5, 'Wheel and button zoom should update the scale.');
assert.strictEqual(zoomCanvas.scrollLeft, 250, 'Zooming should preserve the world point under the cursor horizontally.');
assert.strictEqual(zoomCanvas.scrollTop, 242, 'Zooming should preserve the world point under the cursor vertically.');
assert.strictEqual(zoomContext.viewportUpdated, true, 'Zooming should refresh minimap viewport data.');

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
assert.ok(page.includes('startCanvasPan'), 'The canvas should support right-button panning.');
assert.ok(page.includes('openNodeContextMenu'), 'Nodes should expose a context menu on right click.');
assert.ok(page.includes('class="workflow-context-menu"'), 'The node context menu should be rendered in the workflow page.');
assert.ok(page.includes('>复制</button>') && page.includes('>删除</button>'), 'The node context menu should expose copy and delete actions.');
assert.ok(page.includes('value="webhook"'), 'Workflow trigger settings should expose a Webhook mode.');
assert.ok(page.includes('webhookMethod'), 'Webhook settings should allow choosing the HTTP method.');
assert.ok(page.includes('addWebhookParameter'), 'Webhook settings should allow defining request parameters.');
assert.ok(page.includes('X-NeuChar-Webhook-Token'), 'Webhook settings should document the secure request header.');
assert.ok(page.includes('selectedWorkflowObject'), 'Agent nodes should show the selected workflow object details.');
assert.ok(page.includes('openWorkflowObjectEditor'), 'Agent nodes should expose an edit action.');
assert.ok(page.includes('workflow-object-card'), 'Agent nodes should render a compact basic information card.');
assert.ok(vueOptions.methods.workflowObjectEditUrl, 'Workflow objects should expose a safe editor URL resolver.');
assert.strictEqual(vueOptions.methods.workflowObjectEditUrl.call({}, { editUrl: 'https://example.invalid' }), '',
    'Workflow object edit links must not open arbitrary external URLs.');
assert.strictEqual(vueOptions.methods.workflowObjectEditUrl.call({}, { providerId: 'agents-manager', objectId: 'agent:42' }),
    '/Admin/AgentsManager/Index#tab=first&view=edit&agentId=42',
    'AgentsManager objects should resolve to the in-app agent editor anchor.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({ title: '网址', name: 'Url' }), '网址',
    'A parameter title should be preferred when supplied by a module.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({ title: '   ', name: 'Url' }), 'Url',
    'A blank parameter title must fall back to the field name.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({}), '未命名参数',
    'Incomplete legacy metadata should never leave a parameter visually unnamed.');
assert.strictEqual(vueOptions.methods.hasParameterFieldName({ title: '网址', name: 'Url' }), true,
    'A localized parameter title should retain the underlying field name as a visual aid.');
assert.strictEqual(vueOptions.methods.parameterDescription({ description: '  请输入要爬取的网址  ' }), '请输入要爬取的网址',
    'Parameter descriptions should be trimmed before they are rendered in the tooltip.');
assert.ok(page.includes('workflow-run-dock'), 'Workflow execution should use a persistent status dock instead of a modal.');
assert.ok(page.includes('关联上游 Output'), 'Node parameters should expose upstream output binding controls.');
assert.ok(page.includes('@@wheel.prevent="zoomCanvas"'), 'The workflow canvas should use mouse-wheel zooming.');
assert.ok(page.includes('class="canvas-zoom-controls"'), 'The workflow canvas should render zoom controls.');
assert.ok(page.includes('type="range"'), 'The zoom controls should include a slider.');
assert.ok(page.includes('class="canvas-minimap"'), 'Zoomed canvases should render a minimap.');
assert.ok(page.includes('class="parameter-field-name"'), 'Function parameter field names should be visible in the node settings.');
assert.ok(page.includes('parameter-description-icon'), 'Function parameter descriptions should have an info icon.');
assert.ok(page.includes('parameterDisplayName(parameter)'), 'Function parameters should always resolve to a visible name.');
assert.ok(page.includes('parameterDescription(parameter)'), 'Function parameter descriptions should be shown through a tooltip.');
assert.ok(page.includes('自动保存'), 'Workflow settings should expose the auto-save interval.');
assert.ok(page.includes('Command/Ctrl + S'), 'Workflow save should advertise the system save shortcut.');
assert.ok(!page.includes('<el-dialog'), 'Workflow execution should not keep a modal dialog open.');
assert.match(styles, /\.workflow-list\s*\{[^}]*overflow-y:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The workflow name list should scroll independently without chaining to the editor.');
assert.match(styles, /\.palette-content,\s*\.inspector-content\s*\{[^}]*overflow-y:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The node palette and inspector should each own their vertical scroll area.');
assert.match(styles, /\.workflow-canvas\s*\{[^}]*height:\s*100%;[^}]*overflow:\s*auto;[^}]*overscroll-behavior:\s*contain;/s,
    'The canvas should stay inside its own scroll container.');
assert.match(styles, /\.canvas-zoom-controls,[\s\S]*?\.canvas-minimap\s*\{[^}]*position:\s*fixed;[^}]*opacity:\s*\.48;/s,
    'Canvas navigation controls should stay in the viewport and be translucent by default.');
assert.match(styles, /\.canvas-zoom-controls:hover,[\s\S]*?\.canvas-minimap:focus-within\s*\{[^}]*opacity:\s*1;/s,
    'Canvas navigation controls should become opaque on hover or focus.');
assert.match(styles, /\.workflow-context-menu\s*\{[^}]*position:\s*fixed;/s,
    'The node context menu should stay anchored to the viewport instead of scrolling with the canvas.');
assert.match(styles, /\.workflow-meta\s*\{[^}]*flex:\s*0 0 auto;/s,
    'The save toolbar should remain outside all scrolling panels.');
assert.match(styles, /\.workflow-page\s*\{[^}]*height:\s*100%;[^}]*overflow:\s*hidden;/s,
    'The Workflow page should fit the available Admin content area without outer scrolling.');
assert.match(styles, /\.admin-content:has\(\.workflow-page\)\s*\{[^}]*overflow:\s*hidden;/s,
    'The Admin content scroller should be disabled for the fixed Workflow editor.');
assert.match(styles, /\.admin-content:has\(\.workflow-page\) \.ifram-wrapper\s*\{[^}]*height:\s*100%;/s,
    'The Workflow host should provide a definite height to the fixed editor.');

console.log('NeuChar Workflow designer tests passed.');
