'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const scriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/Workflow.js');
const commonScriptPath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/js/NeuCharWorkflow/common.js');
const pagePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/Areas/Admin/Pages/NeuCharWorkflow/Index.cshtml');
const stylePath = path.resolve(__dirname,
    '../../../../../src/Extensions/Senparc.Xncf.NeuCharWorkflow/wwwroot/css/NeuCharWorkflow/Workflow.css');
const moduleFunctionPagePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/Areas/Admin/Pages/XncfModule/Start.cshtml');
const moduleFunctionScriptPath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/js/Admin/Pages/XncfModule/start.js');
const moduleFunctionStylePath = path.resolve(__dirname,
    '../../../Senparc.Areas.Admin/wwwroot/css/Admin/XncfModule/XncfModule.css');

let vueOptions = null;
function Vue(options) { vueOptions = options; }

const sandbox = {
    Vue,
    window: { addEventListener() {}, removeEventListener() {}, setTimeout(callback) { callback(); }, clearTimeout() {} },
    localStorage: { getItem() { return null; }, setItem() {} },
    service: {},
    NeuCharWorkflowUi: {
        parseJson(value, fallback) {
            try { return value ? JSON.parse(value) : fallback; } catch { return fallback; }
        },
        normalizeParameterSchema(parameters) { return parameters; }
    },
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

const commonSandbox = { window: {}, console };
vm.createContext(commonSandbox);
vm.runInContext(fs.readFileSync(commonScriptPath, 'utf8'), commonSandbox, { filename: commonScriptPath });
const legacyParameter = commonSandbox.window.NeuCharWorkflowUi.normalizeParameterSchema([{}])[0];
assert.strictEqual(legacyParameter.name, 'parameter_1',
    'Legacy Function metadata without a field name should receive a deterministic draft key.');
assert.strictEqual(legacyParameter.title, '参数 1',
    'Legacy Function metadata without a field name should receive a visible parameter label.');

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

const draftContext = {
    form: {
        name: '草稿工作流',
        triggerType: 'manual',
        webhookMethod: 'any',
        webhookParameters: [],
        graph: {
            nodes: [
                { id: 'trigger', type: 'manual-trigger', name: '手动触发' },
                { id: 'orphan', type: 'delay', name: '草稿等待' }
            ],
            edges: []
        }
    },
    incomingEdges: vueOptions.methods.incomingEdges,
    supportsMultipleInputs: vueOptions.methods.supportsMultipleInputs,
    getDisconnectedNodes: vueOptions.methods.getDisconnectedNodes,
    workflowObjects: []
};
const disconnectedDraftNodes = vueOptions.methods.getDisconnectedNodes.call(draftContext);
assert.strictEqual(disconnectedDraftNodes.length, 1,
    'The designer should identify nodes that are not reachable from the trigger.');
assert.strictEqual(disconnectedDraftNodes[0].id, 'orphan',
    'The disconnected draft node should remain identifiable to the editor.');
assert.strictEqual(vueOptions.methods.validate.call(draftContext, { requireRunnable: false }), '',
    'Draft saves should permit disconnected nodes.');
assert.match(vueOptions.methods.validate.call(draftContext, { requireRunnable: true }), /未连接到触发器/,
    'Testing a workflow should continue to reject disconnected draft nodes.');

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
assert.strictEqual(vueOptions.methods.clampCanvasZoom.call({}, .001), .02,
    'Canvas zoom should allow a loaded large workflow to fit without dropping below the supported minimum.');

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
    $refs: { canvas: zoomCanvas, stage: { getBoundingClientRect() { return { left: 10, top: -64 }; } } },
    clampCanvasZoom: vueOptions.methods.clampCanvasZoom,
    stageContentTop: vueOptions.methods.stageContentTop,
    updateCanvasViewport() { this.viewportUpdated = true; },
    $nextTick(callback) { callback(); }
};
vueOptions.methods.setCanvasZoom.call(zoomContext, 1.5, 210, 180);
assert.strictEqual(zoomContext.canvasZoom, 1.5, 'Wheel and button zoom should update the scale.');
assert.strictEqual(zoomCanvas.scrollLeft, 250, 'Zooming should preserve the world point under the cursor horizontally.');
assert.strictEqual(zoomCanvas.scrollTop, 242, 'Zooming should preserve the world point under the cursor vertically.');
assert.strictEqual(zoomContext.viewportUpdated, true, 'Zooming should refresh minimap viewport data.');

const fitCanvas = {
    clientWidth: 1000,
    clientHeight: 700,
    scrollLeft: 0,
    scrollTop: 0
};
const fitContext = {
    form: {
        graph: {
            nodes: [
                { id: 'first', x: 100, y: 100 },
                { id: 'last', x: 1600, y: 700 }
            ]
        }
    },
    canvasZoom: 1,
    $refs: { canvas: fitCanvas },
    clampCanvasZoom: vueOptions.methods.clampCanvasZoom,
    stageContentTop() { return 40; },
    updateCanvasViewport() { this.viewportUpdated = true; },
    $nextTick(callback) { callback(); }
};
assert.strictEqual(vueOptions.methods.fitCanvasToNodes.call(fitContext), true,
    'Loading a workflow with nodes should calculate a fit-to-content viewport.');
assert.strictEqual(fitContext.canvasZoom, .54,
    'Fit-to-content should zoom out just enough to include the full node bounds and padding.');
assert.ok(fitCanvas.scrollLeft > 0 && fitContext.viewportUpdated,
    'Fit-to-content should centre the loaded graph and refresh the visible viewport.');

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
assert.ok(page.includes('webhookHelpVisible=true') && page.includes('Webhook 使用说明'),
    'Webhook guidance should be available on demand in a help dialog instead of permanently consuming editor height.');
assert.ok(!page.includes('class="webhook-url-hint"'),
    'The lengthy inline Webhook URL guidance should be moved out of the always-visible configuration area.');
assert.ok(page.includes('class="workflow-meta-primary"') && page.includes('class="workflow-meta-secondary"'),
    'The workflow toolbar should separate identity, settings and feedback into readable groups.');
assert.ok(page.includes('class="workflow-meta-actions"') && page.includes('>保存</el-button>') && page.includes('>测试运行</el-button>'),
    'Primary save and run actions should remain visible with textual labels.');
assert.ok(page.includes('@@command="handleWorkflowAction"') && page.includes('删除工作流'),
    'Destructive workflow actions should live in a compact overflow menu.');
assert.ok(page.includes('selectedWorkflowObject'), 'Agent nodes should show the selected workflow object details.');
assert.ok(page.includes('openWorkflowObjectEditor'), 'Agent nodes should expose an edit action.');
assert.ok(page.includes('workflow-object-card'), 'Agent nodes should render a compact basic information card.');
assert.ok(vueOptions.methods.workflowObjectEditUrl, 'Workflow objects should expose a safe editor URL resolver.');
assert.strictEqual(vueOptions.methods.workflowObjectEditUrl.call({}, { editUrl: 'https://example.invalid' }), '',
    'Workflow object edit links must not open arbitrary external URLs.');
assert.strictEqual(vueOptions.methods.workflowObjectEditUrl.call({}, { providerId: 'agents-manager', objectId: 'agent:42' }),
    '/Admin/AgentsManager/Index#tab=first&view=edit&agentId=42',
    'AgentsManager objects should resolve to the in-app agent editor anchor.');
assert.strictEqual(vueOptions.methods.functionPageUrl(
    { moduleUid: 'Senparc.Xncf.SenMapic', functionKey: 'Crawl Page' },
    'run'),
    '/Admin/XncfModule/Start/?uid=Senparc.Xncf.SenMapic&functionKey=Crawl%20Page&action=run#function-Crawl%20Page',
    'Function execution links should use a same-site module page, a targeted action and a stable anchor.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({ title: '网址', name: 'Url' }), '网址',
    'A parameter title should be preferred when supplied by a module.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({ title: '   ', name: 'Url' }), 'Url',
    'A blank parameter title must fall back to the field name.');
assert.strictEqual(vueOptions.methods.parameterDisplayName({}, 0), '参数 1',
    'Incomplete legacy metadata should still expose an actionable parameter label.');
assert.strictEqual(vueOptions.methods.hasParameterFieldName({ title: '网址', name: 'Url' }), true,
    'A localized parameter title should retain the underlying field name as a visual aid.');
assert.strictEqual(vueOptions.methods.parameterDescription({ description: '  请输入要爬取的网址  ' }), '请输入要爬取的网址',
    'Parameter descriptions should be trimmed before they are rendered in the tooltip.');
const selectionSourceContext = {
    functionParameters() {
        return [{
            name: 'crawlMode',
            title: '抓取模式',
            parameterType: 1,
            systemType: 'String',
            options: [{ value: 'fast', text: '快速' }, { value: 'full', text: '完整' }]
        }, {
            name: 'tags',
            title: '标签',
            parameterType: 2,
            systemType: 'String[]',
            options: [{ value: 'news', text: '新闻' }]
        }];
    },
    expectedShape: vueOptions.methods.expectedShape,
    parameterDisplayName: vueOptions.methods.parameterDisplayName
};
const selectionFields = vueOptions.methods.functionSelectionInputFields.call(selectionSourceContext, {});
assert.strictEqual(selectionFields.length, 2,
    'Function dropdown and multi-select inputs should be available as binding sources.');
assert.deepStrictEqual(
    { path: selectionFields[0].path, sourceKind: selectionFields[0].sourceKind, sourceParameterName: selectionFields[0].sourceParameterName },
    { path: '$.__functionInput.crawlMode', sourceKind: 'function-selection', sourceParameterName: 'crawlMode' },
    'Selection bindings should preserve the source parameter identity for runtime resolution.');
assert.strictEqual(selectionFields[1].isArray, true,
    'A multi-select Function input should remain an array when bound downstream.');
assert.ok(page.includes('workflow-run-dock'), 'Workflow execution should use a persistent status dock instead of a modal.');
assert.ok(page.includes('关联上游 Output'), 'Node parameters should expose upstream output binding controls.');
assert.ok(page.includes('Function 输入选择'), 'Function parameters should explain that upstream Selection values can be bound.');
assert.ok(page.includes("openFunctionPage(selectedFunction,'settings')") && page.includes("openFunctionPage(selectedFunction,'run')"),
    'Function nodes should offer separate settings and execution links.');
assert.ok(page.includes('@@wheel.prevent="zoomCanvas"'), 'The workflow canvas should use mouse-wheel zooming.');
assert.ok(page.includes('class="canvas-zoom-controls"'), 'The workflow canvas should render zoom controls.');
assert.ok(page.includes('type="range"'), 'The zoom controls should include a slider.');
assert.ok(page.includes('min="0.02"'), 'The zoom slider should expose the low zoom range used to fit large loaded workflows.');
assert.ok(page.includes('class="canvas-minimap"'), 'Zoomed canvases should render a minimap.');
assert.ok(page.includes('workflow-draft-warning'), 'Disconnected draft nodes should be explicitly warned about before saving.');
assert.ok(page.includes('disconnectedNodes.length>0'), 'Disconnected draft nodes should disable test execution in the page.');
assert.ok(page.includes('class="parameter-field-name"'), 'Function parameter field names should be visible in the node settings.');
assert.ok(page.includes('parameter-description-icon'), 'Function parameter descriptions should have an info icon.');
assert.ok(page.includes('parameterDisplayName(parameter)'), 'Function parameters should always resolve to a visible name.');
assert.ok(page.includes('parameterDescription(parameter)'), 'Function parameter descriptions should be shown through a tooltip.');
assert.ok(sourceIncludesFitOnLoad(), 'Editing an existing workflow should fit all nodes after its canvas has rendered.');
assert.ok(page.includes('自动保存'), 'Workflow settings should expose the auto-save interval.');
assert.ok(page.includes('Command/Ctrl + S'), 'Workflow save should advertise the system save shortcut.');
assert.ok(!page.includes(':visible.sync="runDialogVisible"'),
    'Workflow execution should remain in the persistent dock instead of using a modal dialog.');
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
assert.match(styles, /\.workflow-draft-warning\s*\{[^}]*color:\s*#e6a23c;/s,
    'Draft warnings should remain visually distinct in the workflow toolbar.');
assert.match(styles, /\.workflow-context-menu\s*\{[^}]*position:\s*fixed;/s,
    'The node context menu should stay anchored to the viewport instead of scrolling with the canvas.');
assert.match(styles, /\.workflow-meta\s*\{[^}]*flex:\s*0 0 auto;/s,
    'The save toolbar should remain outside all scrolling panels.');
assert.match(styles, /\.workflow-meta-primary,[\s\S]*?\.workflow-meta-actions,[\s\S]*?display:\s*flex;/s,
    'The grouped workflow toolbar should use flexible action and field containers.');
assert.match(styles, /@media \(max-width:\s*980px\)\s*\{[\s\S]*?\.workflow-meta-primary\s*\{\s*flex-wrap:\s*wrap;/s,
    'The workflow toolbar should wrap its groups before labels can be clipped on narrower screens.');
assert.match(styles, /\.workflow-page\s*\{[^}]*height:\s*100%;[^}]*overflow:\s*hidden;/s,
    'The Workflow page should fit the available Admin content area without outer scrolling.');
assert.match(styles, /\.admin-content:has\(\.workflow-page\)\s*\{[^}]*overflow:\s*hidden;/s,
    'The Admin content scroller should be disabled for the fixed Workflow editor.');
assert.match(styles, /\.admin-content:has\(\.workflow-page\) \.ifram-wrapper\s*\{[^}]*height:\s*100%;/s,
    'The Workflow host should provide a definite height to the fixed editor.');

const moduleFunctionPage = fs.readFileSync(moduleFunctionPagePath, 'utf8');
const moduleFunctionScript = fs.readFileSync(moduleFunctionScriptPath, 'utf8');
const moduleFunctionStyles = fs.readFileSync(moduleFunctionStylePath, 'utf8');
assert.ok(moduleFunctionPage.includes(':id="functionAnchorId(item)"'),
    'The XNCF Function page should provide a stable target anchor for Workflow navigation.');
assert.ok(moduleFunctionScript.includes('applyFunctionNavigation') && moduleFunctionScript.includes('scrollIntoView'),
    'The XNCF Function page should scroll to the requested Function after loading it.');
assert.ok(moduleFunctionScript.includes("requestedFunctionAction === 'run'") && moduleFunctionScript.includes('this.openRun(item'),
    'A Function execution link should open the corresponding run panel after navigation.');
assert.match(moduleFunctionStyles, /\.function-card-highlight\s*\{[^}]*animation:\s*function-card-highlight/s,
    'The anchored Function card should visibly flash after navigation.');

async function verifyUnsavedChangeGuards() {
    let modalArguments = null;
    const dirtyContext = {
        saveDirty: true,
        discardConfirming: false,
        $confirm(...args) {
            modalArguments = args;
            return Promise.resolve();
        }
    };
    assert.strictEqual(await vueOptions.methods.confirmDiscardChanges.call(dirtyContext, '新建工作流'), true,
        'Confirming the warning should allow replacing a dirty workflow.');
    assert.strictEqual(dirtyContext.discardConfirming, false,
        'The replacement confirmation lock should be released after the dialog closes.');
    assert.match(modalArguments[0], /未保存的更改/,
        'The replacement warning should clearly explain that unsaved work will be lost.');
    assert.strictEqual(modalArguments[2].confirmButtonText, '放弃更改',
        'The destructive dialog action should be explicit.');

    const cancelledContext = {
        saveDirty: true,
        discardConfirming: false,
        $confirm() { return Promise.reject(new Error('cancelled')); }
    };
    assert.strictEqual(await vueOptions.methods.confirmDiscardChanges.call(cancelledContext, '切换工作流'), false,
        'Cancelling the warning should keep the user on the dirty workflow.');

    const blockedNewWorkflowContext = {
        editingLocked: false,
        saveState: { saving: false },
        confirmDiscardChanges() { return Promise.resolve(false); },
        emptyForm() { throw new Error('A cancelled confirmation must not replace the current form.'); }
    };
    await vueOptions.methods.createWorkflow.call(blockedNewWorkflowContext);

    const unloadEvent = {
        prevented: false,
        preventDefault() { this.prevented = true; },
        returnValue: undefined
    };
    assert.strictEqual(vueOptions.methods.onBeforeUnload.call({ saveDirty: true }, unloadEvent), '',
        'Leaving a page with unsaved changes should request the browser confirmation prompt.');
    assert.strictEqual(unloadEvent.prevented, true,
        'The browser navigation event should be cancelled while it asks for confirmation.');
    assert.strictEqual(unloadEvent.returnValue, '',
        'The browser confirmation prompt requires returnValue to be assigned.');
    assert.strictEqual(vueOptions.methods.onBeforeUnload.call({ saveDirty: false }, {}), undefined,
        'A saved workflow should not block normal page navigation.');

    const source = fs.readFileSync(scriptPath, 'utf8');
    assert.ok(source.includes("window.addEventListener('beforeunload', this.onBeforeUnload)"),
        'The workflow editor should subscribe to browser leave-page confirmation.');
    assert.ok(source.includes("window.removeEventListener('beforeunload', this.onBeforeUnload)"),
        'The workflow editor should remove its leave-page confirmation listener when destroyed.');
    assert.match(source, /async editWorkflow\(id\)\s*\{\s*if \(this\.editingLocked \|\| this\.saveState\.saving \|\| Number\(id\) === Number\(this\.form\.id\)\) return;\s*if \(!await this\.confirmDiscardChanges\('切换工作流'\)\) return;/s,
        'Switching workflows should ask before discarding unsaved changes.');

    let deleted = false;
    const deleteActionContext = {
        form: { id: 7, name: '待删除工作流' },
        editingLocked: false,
        saveState: { saving: false },
        $confirm() { return Promise.resolve(); },
        deleteWorkflow() { deleted = true; return Promise.resolve(); }
    };
    await vueOptions.methods.handleWorkflowAction.call(deleteActionContext, 'delete');
    assert.strictEqual(deleted, true,
        'The overflow menu should require confirmation, then invoke the existing workflow deletion action.');

    const viewModel = vueOptions.data();
    assert.strictEqual(viewModel.webhookHelpVisible, false,
        'Webhook help should stay collapsed until the user explicitly requests it.');
}

verifyUnsavedChangeGuards()
    .then(() => console.log('NeuChar Workflow designer tests passed.'))
    .catch(error => {
        console.error(error);
        process.exitCode = 1;
    });

function sourceIncludesFitOnLoad() {
    const source = fs.readFileSync(scriptPath, 'utf8');
    return source.includes('this.$nextTick(() => this.fitCanvasToNodes())');
}
