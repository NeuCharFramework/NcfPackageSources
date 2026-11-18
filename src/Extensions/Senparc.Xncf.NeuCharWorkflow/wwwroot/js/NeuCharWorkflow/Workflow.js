// The page body is rendered inside the shared #app Vue root. Vue deliberately
// removes <script> tags while compiling that root, so resolve this template
// before mounting the root instead of using the deferred "#id" form.
window.ncfWorkflowText = window.ncfWorkflowText || function (source) {
    return typeof window.ncfST === 'function' ? window.ncfST(source) : source;
};
const MaxWorkflowConsoleEvents = 5000;
const MaxWorkflowLoopIterations = 100000;
const WorkflowFormulaDefinitions = [
    { name: 'if', label: 'Frontend.412', category: 'Frontend.414', arity: 3, description: 'Frontend.564' },
    { name: 'coalesce', label: 'Frontend.520', category: 'Frontend.414', arity: 2, description: 'Frontend.565' },
    { name: 'contains', label: 'Frontend.187', category: 'Frontend.414', arity: 2, description: 'Frontend.566' },
    { name: 'startsWith', label: 'Frontend.521', category: 'Frontend.414', arity: 2, description: 'Frontend.567' },
    { name: 'endsWith', label: 'Frontend.522', category: 'Frontend.414', arity: 2, description: 'Frontend.568' },
    { name: 'isEmpty', label: 'Frontend.523', category: 'Frontend.414', arity: 1, description: 'Frontend.569' },
    { name: 'isNull', label: 'Frontend.524', category: 'Frontend.414', arity: 1, description: 'Frontend.570' },
    { name: 'substring', label: 'Frontend.525', category: 'Frontend.560', arity: 3, description: 'Frontend.571' },
    { name: 'trim', label: 'Frontend.526', category: 'Frontend.560', arity: 1, description: 'Frontend.572' },
    { name: 'lower', label: 'Frontend.527', category: 'Frontend.560', arity: 1, description: 'Frontend.573' },
    { name: 'upper', label: 'Frontend.528', category: 'Frontend.560', arity: 1, description: 'Frontend.574' },
    { name: 'replace', label: 'Frontend.529', category: 'Frontend.560', arity: 3, description: 'Frontend.575' },
    { name: 'split', label: 'Frontend.530', category: 'Frontend.560', arity: 2, description: 'Frontend.576' },
    { name: 'concat', label: 'Frontend.531', category: 'Frontend.561', arity: 2, description: 'Frontend.577' },
    { name: 'length', label: 'Frontend.532', category: 'Frontend.410', arity: 1, description: 'Frontend.578' },
    { name: 'count', label: 'Frontend.533', category: 'Frontend.410', arity: 1, description: 'Frontend.579' },
    { name: 'toArray', label: 'Frontend.534', category: 'Frontend.410', arity: 1, description: 'Frontend.580' },
    { name: 'first', label: 'Frontend.535', category: 'Frontend.410', arity: 1, description: 'Frontend.581' },
    { name: 'last', label: 'Frontend.536', category: 'Frontend.410', arity: 1, description: 'Frontend.582' },
    { name: 'at', label: 'Frontend.537', category: 'Frontend.410', arity: 2, description: 'Frontend.583' },
    { name: 'join', label: 'Frontend.538', category: 'Frontend.410', arity: 2, description: 'Frontend.584' },
    { name: 'sort', label: 'Frontend.539', category: 'Frontend.410', arity: 3, description: 'Frontend.585' },
    { name: 'orderBy', label: 'Frontend.540', category: 'Frontend.410', arity: 3, description: 'Frontend.586' },
    { name: 'reverse', label: 'Frontend.541', category: 'Frontend.410', arity: 1, description: 'Frontend.587' },
    { name: 'take', label: 'Frontend.542', category: 'Frontend.410', arity: 2, description: 'Frontend.588' },
    { name: 'skip', label: 'Frontend.543', category: 'Frontend.410', arity: 2, description: 'Frontend.589' },
    { name: 'flatten', label: 'Frontend.544', category: 'Frontend.410', arity: 1, description: 'Frontend.590' },
    { name: 'sum', label: 'Frontend.545', category: 'Frontend.410', arity: 1, description: 'Frontend.591' },
    { name: 'min', label: 'Frontend.546', category: 'Frontend.410', arity: 1, description: 'Frontend.592' },
    { name: 'max', label: 'Frontend.547', category: 'Frontend.410', arity: 1, description: 'Frontend.593' },
    { name: 'unique', label: 'Frontend.548', category: 'Frontend.410', arity: 1, description: 'Frontend.594' },
    { name: 'keys', label: 'Frontend.549', category: 'Frontend.411', arity: 1, description: 'Frontend.595' },
    { name: 'values', label: 'Frontend.550', category: 'Frontend.411', arity: 1, description: 'Frontend.596' },
    { name: 'has', label: 'Frontend.551', category: 'Frontend.411', arity: 2, description: 'Frontend.597' },
    { name: 'toNumber', label: 'Frontend.552', category: 'Frontend.562', arity: 1, description: 'Frontend.598' },
    { name: 'toInt', label: 'Frontend.553', category: 'Frontend.562', arity: 1, description: 'Frontend.599' },
    { name: 'toLong', label: 'Frontend.554', category: 'Frontend.562', arity: 1, description: 'Frontend.600' },
    { name: 'toDecimal', label: 'Frontend.555', category: 'Frontend.562', arity: 1, description: 'Frontend.601' },
    { name: 'toBool', label: 'Frontend.556', category: 'Frontend.562', arity: 1, description: 'Frontend.602' },
    { name: 'toString', label: 'Frontend.557', category: 'Frontend.562', arity: 1, description: 'Frontend.603' },
    { name: 'now', label: 'Frontend.558', category: 'Frontend.563', arity: 0, description: 'Frontend.604' },
    { name: 'formatDate', label: 'Frontend.559', category: 'Frontend.563', arity: 2, description: 'Frontend.605' }
];
WorkflowFormulaDefinitions.forEach(definition => {
    definition.label = ncfWorkflowText(definition.label);
    definition.category = ncfWorkflowText(definition.category);
    definition.description = ncfWorkflowText(definition.description);
});
const workflowNodePickerTemplateElement = typeof document !== 'undefined'
    ? document.getElementById('workflow-node-picker-template')
    : null;
const workflowNodePickerTemplate = workflowNodePickerTemplateElement
    ? workflowNodePickerTemplateElement.innerHTML
    : '<div class="workflow-node-picker"></div>';

if (typeof Vue !== 'undefined' && typeof Vue.component === 'function') {
    Vue.component('workflow-node-picker', {
        template: workflowNodePickerTemplate,
        props: {
            functions: { type: Array, default: () => [] },
            objects: { type: Array, default: () => [] },
            pinnedFunctionKeys: { type: Array, default: () => [] },
            locked: { type: Boolean, default: false },
            edgeInsert: { type: Boolean, default: false }
        },
        data() {
            return {
                keyword: '',
                module: '',
                systemNodes: [
                    { type: 'condition', name: ncfWorkflowText('Frontend.116'), label: ncfWorkflowText('Frontend.413'), icon: 'el-icon-s-operation' },
                    { type: 'delay', name: ncfWorkflowText('Frontend.115'), label: ncfWorkflowText('Frontend.415'), icon: 'el-icon-time' },
                    { type: 'loop', name: ncfWorkflowText('Frontend.416'), label: ncfWorkflowText('Frontend.417'), icon: 'el-icon-refresh' },
                    { type: 'loop-end', name: ncfWorkflowText('Frontend.418'), label: ncfWorkflowText('Frontend.418'), icon: 'el-icon-circle-check' },
                    { type: 'sub-workflow', name: ncfWorkflowText('Frontend.419'), label: ncfWorkflowText('Frontend.420'), icon: 'el-icon-document' },
                    { type: 'code', name: ncfWorkflowText('Frontend.421'), label: ncfWorkflowText('Frontend.421'), icon: 'el-icon-edit-outline' },
                    { type: 'aggregate', name: ncfWorkflowText('Frontend.118'), label: ncfWorkflowText('Frontend.422'), icon: 'el-icon-collection' },
                    { type: 'merge', name: ncfWorkflowText('Frontend.423'), label: ncfWorkflowText('Frontend.423'), icon: 'el-icon-sort' },
                    { type: 'parallel', name: ncfWorkflowText('Frontend.424'), label: ncfWorkflowText('Frontend.424'), icon: 'el-icon-share' },
                    { type: 'console', name: ncfWorkflowText('Frontend.134'), label: ncfWorkflowText('Frontend.425'), icon: 'el-icon-monitor' },
                    { type: 'neubell', name: ncfWorkflowText('Frontend.119'), label: ncfWorkflowText('Frontend.427'), icon: 'el-icon-bell' },
                    { type: 'human-input', name: ncfWorkflowText('Frontend.137'), label: ncfWorkflowText('Frontend.429'), icon: 'el-icon-user-solid' },
                    { type: 'end', name: ncfWorkflowText('Frontend.124'), label: ncfWorkflowText('Frontend.430'), icon: 'el-icon-circle-check' }
                ]
            };
        },
        computed: {
            moduleNames() {
                return [...new Set(this.functions.map(fn => fn.moduleName).filter(Boolean))].sort();
            },
            filteredSystemNodes() {
                return this.systemNodes.filter(node => this.matchesKeyword([
                    '系统节点',
                    node.type,
                    node.name,
                    node.label
                ]));
            },
            filteredFunctions() {
                return this.functions.filter(fn => {
                    const moduleMatched = !this.module || fn.moduleName === this.module;
                    const keywordMatched = this.matchesKeyword([
                        fn.functionName,
                        fn.moduleName,
                        fn.description,
                        fn.functionKey
                    ]);
                    return moduleMatched && keywordMatched;
                }).sort((left, right) => {
                    const pinDiff = Number(this.isPinned(right)) - Number(this.isPinned(left));
                    return pinDiff || String(left.moduleName).localeCompare(String(right.moduleName), 'zh-CN') ||
                        String(left.functionName).localeCompare(String(right.functionName), 'zh-CN');
                });
            },
            filteredObjects() {
                return this.objects.filter(object => this.matchesKeyword([
                    object.providerId,
                    object.objectId,
                    object.kind,
                    this.objectKindLabel(object),
                    object.name,
                    object.description,
                    ...Object.values(object.metadata || {})
                ]));
            }
        },
        methods: {
            matchesKeyword(values) {
                const keyword = this.keyword.trim().toLowerCase();
                return !keyword || values.some(value => String(value || '').toLowerCase().includes(keyword));
            },
            objectKindLabel(object) {
                return object?.kind === 'a2a' ? ncfWorkflowText('Frontend.448') : object?.kind === 'agent-group' ? ncfWorkflowText('Frontend.117') : ncfWorkflowText('Frontend.449');
            },
            functionIdentity(fn) { return `${String(fn.moduleUid).toLowerCase()}|${String(fn.functionKey).toLowerCase()}`; },
            isPinned(fn) { return this.pinnedFunctionKeys.includes(this.functionIdentity(fn)); },
            nodePreviewKey(kind, payload) {
                if (kind === 'system') return `system:${String(payload?.type || '')}`;
                if (kind === 'function') return `function:${this.functionIdentity(payload || {})}`;
                return `object:${String(payload?.providerId || '').toLowerCase()}:${String(payload?.objectId || '')}`;
            },
            previewAnchor(event) {
                const target = event && (event.currentTarget || event.target);
                if (!target || typeof target.getBoundingClientRect !== 'function') return null;
                const rect = target.getBoundingClientRect();
                const left = Number(rect.left);
                const top = Number(rect.top);
                const right = Number(rect.right);
                const bottom = Number(rect.bottom);
                return {
                    left: Number.isFinite(left) ? left : 0,
                    top: Number.isFinite(top) ? top : 0,
                    right: Number.isFinite(right) ? right : left,
                    bottom: Number.isFinite(bottom) ? bottom : top,
                    width: Number.isFinite(Number(rect.width)) ? Number(rect.width) : Math.max(0, right - left),
                    height: Number.isFinite(Number(rect.height)) ? Number(rect.height) : Math.max(0, bottom - top)
                };
            },
            previewNode(kind, payload, mode, event) {
                if (!payload) return;
                this.$emit('preview-node', {
                    kind,
                    payload,
                    key: this.nodePreviewKey(kind, payload),
                    anchor: this.previewAnchor(event)
                }, mode === 'click' ? 'click' : 'hover');
            },
            hideNodePreview(kind, payload) {
                this.$emit('hide-preview-node', this.nodePreviewKey(kind, payload || {}));
            },
            previewSystem(node, mode, event) { this.previewNode('system', node, mode, event); },
            hideSystemPreview(node) { this.hideNodePreview('system', node); },
            selectSystem(node) {
                if (!this.locked && !(this.edgeInsert && node.type === 'end')) this.$emit('select-system', node.type, node.name);
            },
            selectFunction(fn) {
                if (!this.locked && fn && fn.moduleAvailable) this.$emit('select-function', fn);
            },
            selectObject(object) {
                if (!this.locked && object && object.enabled) this.$emit('select-object', object);
            },
            startDrag(event, kind, payload) {
                const unavailable = (kind === 'function' && !payload?.moduleAvailable) || (kind === 'object' && !payload?.enabled);
                if (this.locked || unavailable || (this.edgeInsert && kind === 'system' && payload && payload.type === 'end')) {
                    event.preventDefault();
                    return;
                }
                if (event.dataTransfer) {
                    event.dataTransfer.effectAllowed = 'copy';
                    event.dataTransfer.setData('text/plain', 'neucharworkflow-node');
                }
                this.$emit('hide-preview-node');
                this.$emit('drag-node', kind, payload);
            },
            startSystemDrag(event, node) { this.startDrag(event, 'system', node); },
            startFunctionDrag(event, fn) { this.startDrag(event, 'function', fn); },
            startObjectDrag(event, object) { this.startDrag(event, 'object', object); },
            finishDrag() { this.$emit('drag-end'); }
        }
    });

    Vue.component('workflow-rich-text-input', {
        props: {
            value: { type: [String, Number, Object], default: '' },
            disabled: { type: Boolean, default: false },
            multiline: { type: Boolean, default: false },
            rows: { type: Number, default: 4 },
            maxlength: { type: [Number, String], default: null },
            showWordLimit: { type: Boolean, default: false },
            placeholder: { type: String, default: '' },
            helpText: { type: String, default: '' },
            editable: { type: Boolean, default: true },
            editLabel: { type: String, default: ncfWorkflowText('Frontend.688') }
        },
        template: `
            <div :class="['workflow-rich-text-input', {'is-disabled': disabled, 'is-multiline': multiline}]">
                <div class="workflow-rich-text-input-header">
                    <span class="workflow-rich-text-badge"><i class="el-icon-magic-stick"></i>${ncfWorkflowText('Frontend.686')}</span>
                    <el-tooltip v-if="helpText" effect="dark" placement="top-start" :content="helpText">
                        <i class="el-icon-info workflow-rich-text-info" tabindex="0" :aria-label="${ncfWorkflowText('Frontend.687')}"></i>
                    </el-tooltip>
                </div>
                <div class="workflow-rich-text-input-control">
                    <el-input :value="value"
                              :type="multiline ? 'textarea' : 'text'"
                              :rows="rows"
                              :disabled="disabled"
                              :maxlength="maxlength"
                              :show-word-limit="showWordLimit"
                              :placeholder="placeholder"
                              @input="$emit('input', $event)"></el-input>
                    <el-button v-if="editable"
                               type="default"
                               icon="el-icon-connection"
                               :disabled="disabled"
                               :title="editLabel"
                               :aria-label="editLabel"
                               @mousedown.stop
                               @mouseup.stop
                               @click.stop="$emit('edit-template')">{{editLabel}}</el-button>
                </div>
            </div>`
    });
}

new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            workflows: [],
            functions: [],
            workflowObjects: [],
            workflowFileSelections: {},
            chatModels: [],
            observedOutputSchemas: [],
            keyword: '',
            workflowClock: Date.now(),
            workflowClockTimer: null,
            listCollapsed: false,
            paletteCollapsed: true,
            inspectorCollapsed: true,
            pinnedFunctions: [],
            editing: false,
            discardConfirming: false,
            webhookHelpVisible: false,
            workflowSettingsVisible: false,
            templateEditorPlaceholder: ncfWorkflowText('Frontend.689'),
            templateEditorBindingHelp: ncfWorkflowText('Frontend.690'),
            templateEditor: {
                visible: false,
                nodeId: '',
                configKey: '',
                parameterName: '',
                targetType: '',
                assignmentIndex: -1,
                fieldLabel: '',
                allowBindings: true,
                text: '',
                bindings: [],
                pendingSelection: [],
                selectionStart: 0,
                selectionEnd: 0
            },
            loopHighlight: {
                nodeIds: [],
                labelNodeIds: [],
                edgeIds: []
            },
            loopHighlightTimer: null,
            loopHighlightSequence: 0,
            selectedNodeId: '',
            selectedNodeIds: [],
            selectionBox: { active: false, startX: 0, startY: 0, endX: 0, endY: 0, additive: false },
            connectionDraft: { sourceId: '', sourceHandle: '', x: 0, y: 0 },
            dragState: null,
            canvasPan: { active: false, moved: false, startX: 0, startY: 0, startScrollLeft: 0, startScrollTop: 0 },
            suppressCanvasContextMenuUntil: 0,
            contextMenu: { visible: false, x: 0, y: 0, node: null },
            canvasContextMenu: { visible: false, x: 0, y: 0, point: null },
            edgeInsertMenu: { visible: false, edge: null, x: 0, y: 0 },
            canvasNodeInsertMenu: { visible: false, x: 0, y: 0, point: null },
            paletteDrag: { active: false, kind: '', payload: null, hoverEdgeId: '' },
            nodePreview: { visible: false, kind: '', payload: null, key: '', mode: 'hover', anchor: null },
            nodePreviewTimer: null,
            canvasSize: { width: 1200, height: 760 },
            canvasSafeInsets: { left: 0, right: 0 },
            canvasZoom: 1,
            canvasViewport: { width: 0, height: 0, scrollLeft: 0, scrollTop: 0, left: 0, right: 0, bottom: 0, windowWidth: 0, windowHeight: 0 },
            form: { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, webhookMethod: 'any', webhookToken: '', webhookParameters: [], chatTitle: '', chatGreeting: '', chatAllowGuest: true, autoSaveMinutes: 3, revision: 0, runningCount: 0, graph: { nodes: [], edges: [], variables: [], layout: { direction: 'vertical' } } },
            saveState: {
                saving: false,
                lastSavedSignature: '',
                lastSavedLabel: '',
                status: 'idle',
                error: '',
                autoSaveBlockedSignature: '',
                timer: null
            },
            validation: { message: '', nodeIds: [], source: '' },
            run: {
                running: false,
                validating: false,
                aborting: false,
                input: '',
                runId: '',
                status: 'idle',
                events: [],
                lastSequence: 0,
                nodeStates: {},
                finalOutput: '',
                error: '',
                pollTimer: null,
                consoleOpen: false,
                humanInteractions: [],
                humanReplyVisible: false,
                humanReplyRequest: null,
                humanReplyInput: '',
                humanReplySubmitting: false
            }
        };
    },
    computed: {
        filteredWorkflows() {
            const keyword = this.keyword.trim().toLowerCase();
            return keyword ? this.workflows.filter(item => String(item.name).toLowerCase().includes(keyword)) : this.workflows;
        },
        selectedNode() { return this.form.graph.nodes.find(node => node.id === this.selectedNodeId); },
        selectedNodes() {
            const selectedIds = new Set(this.selectedNodeIds || []);
            return this.form.graph.nodes.filter(node => selectedIds.has(node.id));
        },
        selectedDuplicableNodes() { return this.selectedNodes.filter(node => this.canDuplicateNode(node)); },
        selectedDeletableNodes() { return this.selectedNodes.filter(node => this.canDeleteNode(node)); },
        selectedFunction() {
            return this.selectedNode && this.selectedNode.type === 'function'
                ? this.findFunction(this.selectedNode.config)
                : null;
        },
        selectedWorkflowObject() {
            if (!this.selectedNode || !['agent', 'agent-group', 'a2a'].includes(this.selectedNode.type)) return null;
            const config = this.selectedNode.config || {};
            return this.workflowObjects.find(item =>
                String(item.providerId || '').toLowerCase() === String(config.providerId || '').toLowerCase() &&
                String(item.objectId || '') === String(config.objectId || '')) || null;
        },
        disconnectedNodes() { return this.getDisconnectedNodes(); },
        connectionSourceName() {
            const node = this.form.graph.nodes.find(item => item.id === this.connectionDraft.sourceId);
            return node ? node.name : '';
        },
        editingLocked() { return this.run.running; },
        layoutDirection() {
            return this.form.graph && this.form.graph.layout && this.form.graph.layout.direction === 'horizontal'
                ? 'horizontal'
                : 'vertical';
        },
        activeInsertionEdgeId() {
            return this.dragState?.hoverEdgeId || this.paletteDrag.hoverEdgeId || '';
        },
        subWorkflowTargets() {
            const currentId = Number(this.form.id || 0);
            return (this.workflows || []).filter(workflow => Number(workflow.id) !== currentId);
        },
        nodePreviewDetails() {
            return this.describeNodePreview(this.nodePreview);
        },
        nodePreviewStyle() {
            const anchor = this.nodePreview && this.nodePreview.anchor;
            if (!anchor) return { left: '50%', top: '50%', transform: 'translate(-50%, -50%)' };

            const root = typeof document !== 'undefined' ? document.documentElement : null;
            const viewportWidth = Number((typeof window !== 'undefined' && window.innerWidth) || (root && root.clientWidth) || 1280);
            const viewportHeight = Number((typeof window !== 'undefined' && window.innerHeight) || (root && root.clientHeight) || 800);
            const margin = 16;
            const gap = 12;
            const popupWidth = Math.min(420, Math.max(280, viewportWidth * 0.26));
            const popupHeight = Math.min(360, Math.max(220, viewportHeight * 0.36));
            const left = Number(anchor.left) || 0;
            const top = Number(anchor.top) || 0;
            const right = Number(anchor.right) || left;
            const bottom = Number(anchor.bottom) || top;
            const width = Number(anchor.width) || Math.max(0, right - left);
            const height = Number(anchor.height) || Math.max(0, bottom - top);
            const clamp = (value, minimum, maximum) => Math.max(minimum, Math.min(value, Math.max(minimum, maximum)));
            const clampLeft = value => clamp(value, margin, viewportWidth - popupWidth - margin);
            const clampTop = value => clamp(value, margin, viewportHeight - popupHeight - margin);
            const centeredTop = clampTop(top + (height - popupHeight) / 2);
            const centeredLeft = clampLeft(left + (width - popupWidth) / 2);
            const anchored = (x, y) => ({ left: `${x}px`, top: `${y}px`, transform: 'translate(0, 0)' });

            // Keep the source button outside the popup horizontally whenever possible.
            if (right + gap + popupWidth <= viewportWidth - margin) {
                return anchored(right + gap, centeredTop);
            }
            if (left - gap - popupWidth >= margin) {
                return anchored(left - gap - popupWidth, centeredTop);
            }

            // If neither side has enough room, use the vertical space around the button.
            const below = bottom + gap;
            if (below + popupHeight <= viewportHeight - margin) {
                return anchored(centeredLeft, below);
            }
            const above = top - gap - popupHeight;
            if (above >= margin) {
                return anchored(centeredLeft, above);
            }

            // Very small viewports may not have a complete side or vertical slot;
            // clamp the best side to the viewport while keeping the anchor visible as much as possible.
            const rightRoom = viewportWidth - right;
            const leftRoom = left;
            return anchored(clampLeft(rightRoom >= leftRoom ? right + gap : left - gap - popupWidth), centeredTop);
        },
        selectionBoxStyle() {
            const selection = this.selectionBox;
            const left = Math.min(Number(selection.startX || 0), Number(selection.endX || 0));
            const top = Math.min(Number(selection.startY || 0), Number(selection.endY || 0));
            return {
                left: `${left}px`,
                top: `${top}px`,
                width: `${Math.abs(Number(selection.endX || 0) - Number(selection.startX || 0))}px`,
                height: `${Math.abs(Number(selection.endY || 0) - Number(selection.startY || 0))}px`
            };
        },
        currentSaveSignature() {
            return JSON.stringify({
                name: String(this.form.name || '').trim(),
                description: String(this.form.description || '').trim(),
                enabled: !!this.form.enabled,
                triggerType: this.form.triggerType,
                intervalSeconds: Number(this.form.intervalSeconds || 300),
                webhookMethod: this.form.webhookMethod,
                webhookToken: this.form.webhookToken,
                webhookParameters: this.form.webhookParameters,
                autoSaveMinutes: Number(this.form.autoSaveMinutes || 0),
                graph: this.form.graph
            });
        },
        saveDirty() {
            return this.editing && this.currentSaveSignature !== this.saveState.lastSavedSignature;
        },
        saveStatusText() {
            if (this.saveState.saving) return ncfWorkflowText('Frontend.659');
            if (this.saveState.status === 'error') return this.saveState.error || ncfWorkflowText('Frontend.660');
            if (this.saveDirty) return ncfWorkflowText('Frontend.661');
            if (this.disconnectedNodes.length) return ncfWorkflowText('Frontend.662').replace('{0}', this.disconnectedNodes.length);
            if (this.saveState.lastSavedLabel) return ncfWorkflowText('Frontend.663').replace('{0}', this.saveState.lastSavedLabel);
            return this.form.id ? ncfWorkflowText('Frontend.664') : ncfWorkflowText('Frontend.665');
        },
        validationIssues() {
            const message = String(this.validation?.message || '').trim();
            if (!message) return [];
            const nodeIds = Array.isArray(this.validation?.nodeIds) ? this.validation.nodeIds : [];
            if (!nodeIds.length) return [{ nodeId: '', label: ncfWorkflowText('Frontend.011'), message }];
            return nodeIds.map(nodeId => {
                const node = this.form.graph.nodes.find(item => item.id === nodeId);
                return { nodeId, label: node ? (node.name || node.id) : nodeId, message };
            });
        },
        shellClasses() {
            return {
                'list-collapsed': this.listCollapsed,
                'palette-collapsed': this.paletteCollapsed,
                'inspector-collapsed': this.inspectorCollapsed,
                'is-running': this.run.running
            };
        },
        runStatusText() {
            if (this.run.validating) return window.ncfWorkflowText('Frontend.392');
            if (this.run.aborting) return window.ncfWorkflowText('Frontend.393');
            if (this.run.running && this.run.humanInteractions.length) return window.ncfWorkflowText('Frontend.394');
            if (this.run.running) return window.ncfWorkflowText('Frontend.395');
            if (this.run.status === 'success') return window.ncfWorkflowText('Frontend.396');
            if (this.run.status === 'failed') return window.ncfWorkflowText('Frontend.397');
            return window.ncfWorkflowText('Frontend.391');
        },
        webhookUrl() {
            return this.form.id
                ? `${window.location.origin}/api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/webhook/${this.form.id}`
                : '';
        },
        chatUrl() {
            return this.form.id
                ? `${window.location.origin}/api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/chat/${this.form.id}/page`
                : '';
        },
        templateEditorTitle() {
            return this.templateEditor.fieldLabel
                ? `${ncfWorkflowText('Frontend.666')}${this.templateEditor.fieldLabel}`
                : ncfWorkflowText('Frontend.667');
        },
        templateEditorBindingHelpText() {
            return this.templateEditor.allowBindings
                ? this.templateEditorBindingHelp
                : ncfWorkflowText('Frontend.668');
        },
        templateExpressionHelp() {
            const names = WorkflowFormulaDefinitions.map(item => item.name).join('、');
            return ncfWorkflowText('Frontend.691').replace('{0}', names);
        },
        templateEditorFormulaGroups() {
            const groups = [];
            const byCategory = new Map();
            WorkflowFormulaDefinitions.forEach(formula => {
                if (!byCategory.has(formula.category)) {
                    const group = { category: formula.category, items: [] };
                    byCategory.set(formula.category, group);
                    groups.push(group);
                }
                byCategory.get(formula.category).items.push(formula);
            });
            return groups;
        },
        canvasZoomPercent() { return Math.round(this.canvasZoom * 100); },
        scaledCanvasSize() {
            return {
                width: Math.max(1, Math.round(this.canvasSize.width * this.canvasZoom)),
                height: Math.max(1, Math.round(this.canvasSize.height * this.canvasZoom))
            };
        },
        canvasSurfaceStyle() {
            const insets = this.canvasSafeInsets || {};
            const left = Math.max(0, Number(insets.left) || 0);
            const right = Math.max(0, Number(insets.right) || 0);
            return {
                width: `${this.scaledCanvasSize.width + left + right}px`,
                height: `${this.scaledCanvasSize.height}px`
            };
        },
        canvasStageStyle() {
            const insets = this.canvasSafeInsets || {};
            return {
                width: `${this.canvasSize.width}px`,
                height: `${this.canvasSize.height}px`,
                left: `${Math.max(0, Number(insets.left) || 0)}px`,
                transform: `scale(${this.canvasZoom})`
            };
        },
        showMinimap() {
            const viewport = this.canvasViewport;
            const scaled = this.scaledCanvasSize;
            return this.canvasZoom > 1 && !!viewport.width &&
                (scaled.width > viewport.width + 1 || scaled.height > viewport.height + 1);
        },
        minimapMetrics() {
            const worldWidth = Math.max(1, Number(this.canvasSize.width) || 1);
            const worldHeight = Math.max(1, Number(this.canvasSize.height) || 1);
            const scale = Math.min(180 / worldWidth, 124 / worldHeight);
            return { scale, width: worldWidth * scale, height: worldHeight * scale };
        },
        minimapViewportStyle() {
            const metrics = this.minimapMetrics;
            const viewport = this.canvasViewport;
            const canvas = this.$refs && this.$refs.canvas;
            const stageContentTop = this.stageContentTop ? this.stageContentTop(canvas) : 0;
            const insets = this.canvasSafeInsets || {};
            const leftInset = Math.max(0, Number(insets.left) || 0);
            const worldWidth = Math.max(1, Number(this.canvasSize.width) || 1);
            const worldLeft = Math.max(0, (Number(viewport.scrollLeft || 0) - leftInset) / this.canvasZoom);
            const worldRight = Math.min(worldWidth,
                (Number(viewport.scrollLeft || 0) + Number(viewport.width || 0) - leftInset) / this.canvasZoom);
            const worldTop = Math.max(0, Number(viewport.scrollTop || 0) / this.canvasZoom);
            const width = Math.max(0, worldRight - worldLeft) * metrics.scale;
            const height = Math.min(metrics.height, Math.max(0, Number(viewport.height || 0) - stageContentTop) / this.canvasZoom * metrics.scale);
            return {
                left: `${Math.max(0, Math.min(metrics.width - width, worldLeft * metrics.scale))}px`,
                top: `${Math.max(0, Math.min(metrics.height - height, worldTop * metrics.scale))}px`,
                width: `${Math.max(0, width)}px`,
                height: `${Math.max(0, height)}px`
            };
        },
        canvasZoomControlsStyle() {
            const viewport = this.canvasViewport;
            if (!viewport.width || !viewport.height) return { display: 'none' };
            return {
                left: `${Math.round(viewport.left + 14)}px`,
                bottom: `${Math.max(14, Math.round(viewport.windowHeight - viewport.bottom + 14))}px`
            };
        },
        minimapStyle() {
            const viewport = this.canvasViewport;
            if (!viewport.width || !viewport.height) return { display: 'none' };
            return {
                right: `${Math.max(14, Math.round(viewport.windowWidth - viewport.right + 14))}px`,
                bottom: `${Math.max(14, Math.round(viewport.windowHeight - viewport.bottom + 14))}px`
            };
        },
        edgeInsertMenuStyle() {
            return {
                left: `${this.edgeInsertMenu.x}px`,
                top: `${this.edgeInsertMenu.y}px`
            };
        },
        canvasNodeInsertMenuStyle() {
            return {
                left: `${this.canvasNodeInsertMenu.x}px`,
                top: `${this.canvasNodeInsertMenu.y}px`
            };
        }
    },
    watch: {
        'form.autoSaveMinutes'() { this.scheduleAutoSave(); },
        listCollapsed() { this.refreshCanvasViewport(); },
        paletteCollapsed() { this.refreshCanvasViewportAfterPaneTransition(); },
        inspectorCollapsed() { this.refreshCanvasViewportAfterPaneTransition(); }
    },
    async created() {
        this.loadPinnedFunctions();
        await this.loadAll();
        await this.openTaskRoute();
    },
    mounted() {
        window.addEventListener('mousemove', this.onPointerMove);
        window.addEventListener('mouseup', this.onPointerUp);
        window.addEventListener('keydown', this.onSaveShortcut);
        window.addEventListener('beforeunload', this.onBeforeUnload);
        window.addEventListener('resize', this.updateCanvasViewport);
        window.addEventListener('scroll', this.updateCanvasViewport, true);
        if (typeof document !== 'undefined') document.addEventListener('click', this.onDocumentClick, true);
        if (typeof window !== 'undefined' && typeof window.setInterval === 'function') {
            this.workflowClockTimer = window.setInterval(() => { this.workflowClock = Date.now(); }, 30000);
        }
        this.$nextTick(() => {
            this.updateCanvasViewport();
            if (typeof ResizeObserver !== 'undefined' && this.$refs.canvas) {
                this.canvasResizeObserver = new ResizeObserver(() => this.updateCanvasViewport());
                this.canvasResizeObserver.observe(this.$refs.canvas);
            }
        });
    },
    beforeDestroy() {
        window.removeEventListener('mousemove', this.onPointerMove);
        window.removeEventListener('mouseup', this.onPointerUp);
        window.removeEventListener('keydown', this.onSaveShortcut);
        window.removeEventListener('beforeunload', this.onBeforeUnload);
        window.removeEventListener('resize', this.updateCanvasViewport);
        window.removeEventListener('scroll', this.updateCanvasViewport, true);
        if (typeof document !== 'undefined') document.removeEventListener('click', this.onDocumentClick, true);
        if (this.canvasResizeObserver) this.canvasResizeObserver.disconnect();
        if (this.workflowClockTimer !== null && this.workflowClockTimer !== undefined && typeof window !== 'undefined') {
            window.clearInterval(this.workflowClockTimer);
        }
        this.dismissNodePreview();
        this.clearLoopHighlight();
        this.clearAutoSaveTimer();
        this.clearRunPoll();
    },
    methods: {
        isIntervalWorkflow(item) {
            return String(item && item.triggerType || '').toLowerCase() === 'interval';
        },
        workflowTriggerLabel(item) {
            const triggerType = String(item && item.triggerType || '').toLowerCase();
            if (triggerType === 'interval') return ncfWorkflowText('Frontend.471');
            if (triggerType === 'webhook') return ncfWorkflowText('Frontend.470');
            if (triggerType === 'chat') return ncfWorkflowText('Frontend.132');
            return ncfWorkflowText('Frontend.467');
        },
        workflowTriggerTagType(item) {
            const triggerType = String(item && item.triggerType || '').toLowerCase();
            return triggerType === 'interval' ? 'warning' : triggerType === 'webhook' ? 'primary' : triggerType === 'chat' ? 'success' : 'info';
        },
        workflowNextRunDate(item) {
            if (!item || !item.nextRunAt) return null;
            const parsed = new Date(item.nextRunAt);
            return Number.isNaN(parsed.getTime()) ? null : parsed;
        },
        workflowScheduleText(item) {
            // Reference the reactive clock so relative text updates even when the list is not reloaded.
            const now = Number(this.workflowClock || Date.now());
            if (!item || !item.enabled) return ncfWorkflowText('Frontend.606');
            const nextRun = this.workflowNextRunDate(item);
            if (!nextRun) return ncfWorkflowText('Frontend.115');
            const seconds = Math.ceil((nextRun.getTime() - now) / 1000);
            if (seconds <= 0) return ncfWorkflowText('Frontend.391');
            if (seconds < 60) return ncfWorkflowText('Frontend.692');
            const minutes = Math.ceil(seconds / 60);
            if (minutes < 60) return ncfWorkflowText('Frontend.693').replace('{0}', minutes);
            const hours = Math.ceil(minutes / 60);
            if (hours < 24) return ncfWorkflowText('Frontend.694').replace('{0}', hours);
            const days = Math.ceil(hours / 24);
            return ncfWorkflowText('Frontend.695').replace('{0}', days);
        },
        workflowScheduleTitle(item) {
            if (!this.isIntervalWorkflow(item)) return '';
            if (!item || !item.enabled) return ncfWorkflowText('Frontend.606');
            const nextRun = this.workflowNextRunDate(item);
            if (!nextRun) return ncfWorkflowText('Frontend.115');
            const pad = value => String(value).padStart(2, '0');
            return ncfWorkflowText('Frontend.696').replace('{0}', `${nextRun.getFullYear()}-${pad(nextRun.getMonth() + 1)}-${pad(nextRun.getDate())} ${pad(nextRun.getHours())}:${pad(nextRun.getMinutes())}`);
        },
        emptyForm() {
            return { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, webhookMethod: 'any', webhookToken: '', webhookParameters: [], chatTitle: '', chatGreeting: '', chatAllowGuest: true, autoSaveMinutes: 3, revision: 0, runningCount: 0, graph: { nodes: [], edges: [], variables: [], layout: { direction: 'vertical' } } };
        },
        ensureGraphLayout(graph) {
            const target = graph || { nodes: [], edges: [] };
            target.layout = target.layout && typeof target.layout === 'object' ? target.layout : {};
            target.layout.direction = target.layout.direction === 'horizontal' ? 'horizontal' : 'vertical';
            target.variables = Array.isArray(target.variables) ? target.variables.map(variable => ({
                name: String(variable?.name || ''),
                value: variable?.value == null ? '' : variable.value
            })) : [];
            return target;
        },
        systemNodePreview(type, fallbackName) {
            const definitions = {
                condition: {
                    title: ncfST('Frontend.413'),
                    description: ncfST('Frontend.608'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.621') }, { label: ncfST('Frontend.442'), value: ncfST('Frontend.622') }]
                },
                delay: {
                    title: ncfST('Frontend.415'),
                    description: ncfST('Frontend.609'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.623') }]
                },
                loop: {
                    title: ncfST('Frontend.416'),
                    description: ncfST('Frontend.610'),
                    rows: [{ label: ncfST('Frontend.443'), value: ncfST('Frontend.624') }, { label: ncfST('Frontend.444'), value: ncfST('Frontend.625') }]
                },
                'loop-end': {
                    title: ncfST('Frontend.418'),
                    description: ncfST('Frontend.611'),
                    rows: [{ label: ncfST('Frontend.626'), value: ncfST('Frontend.627') }, { label: ncfST('Frontend.628'), value: ncfST('Frontend.629') }]
                },
                'sub-workflow': {
                    title: ncfST('Frontend.419'),
                    description: ncfST('Frontend.612'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.630') }, { label: ncfST('Frontend.445'), value: ncfST('Frontend.631') }]
                },
                code: {
                    title: ncfST('Frontend.421'),
                    description: ncfST('Frontend.613'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.632') }, { label: ncfST('Frontend.628'), value: ncfST('Frontend.633') }]
                },
                aggregate: {
                    title: ncfST('Frontend.422'),
                    description: ncfST('Frontend.614'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.634') }, { label: ncfST('Frontend.628'), value: ncfST('Frontend.635') }]
                },
                merge: {
                    title: ncfST('Frontend.423'),
                    description: ncfST('Frontend.615'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.636') }, { label: ncfST('Frontend.628'), value: ncfST('Frontend.637') }]
                },
                parallel: {
                    title: ncfST('Frontend.424'),
                    description: ncfST('Frontend.616'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.638') }, { label: ncfST('Frontend.628'), value: ncfST('Frontend.639') }]
                },
                console: {
                    title: ncfST('Frontend.425'),
                    description: ncfST('Frontend.617'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.640') }]
                },
                neubell: {
                    title: ncfST('Frontend.427'),
                    description: ncfST('Frontend.618'),
                    rows: [{ label: ncfST('Frontend.441'), value: ncfST('Frontend.641') }, { label: ncfST('Frontend.642'), value: ncfST('Frontend.643') }]
                },
                'human-input': {
                    title: ncfST('Frontend.428'),
                    description: ncfST('Frontend.619'),
                    rows: [{ label: ncfST('Frontend.628'), value: ncfST('Frontend.644') }, { label: ncfST('Frontend.645'), value: ncfST('Frontend.646') }]
                },
                end: {
                    title: ncfST('Frontend.430'),
                    description: ncfST('Frontend.620'),
                    rows: [{ label: ncfST('Frontend.445'), value: ncfST('Frontend.647') }]
                }
            };
            return definitions[type] || { title: fallbackName || ncfST('Frontend.431'), description: ncfST('Frontend.455'), rows: [] };
        },
        describeNodePreview(preview) {
            const safePreview = preview || {};
            const payload = safePreview.payload || {};
            if (safePreview.kind === 'system') {
                const definition = this.systemNodePreview(payload.type, payload.name);
                return {
                    kind: ncfST('Frontend.432'),
                    title: definition.title,
                    description: definition.description,
                    rows: definition.rows,
                    actionText: payload.type === 'end' ? ncfST('Frontend.648') : ncfST('Frontend.438')
                };
            }
            if (safePreview.kind === 'function') {
                const parameters = this.functionParameters(payload) || [];
                const rows = [
                    { label: ncfWorkflowText('Frontend.697'), value: String(payload.moduleName || ncfWorkflowText('Frontend.699')) },
                    { label: ncfWorkflowText('Frontend.698'), value: String(payload.functionKey || ncfWorkflowText('Frontend.700')) },
                    { label: ncfWorkflowText('Frontend.012'), value: payload.moduleAvailable ? ncfWorkflowText('Frontend.435') : ncfWorkflowText('Frontend.437') }
                ];
                parameters.slice(0, 4).forEach((parameter, index) => {
                    const name = this.parameterDisplayName(parameter, index);
                    const description = this.parameterDescription(parameter);
                    rows.push({ label: `${ncfWorkflowText('Frontend.701')} · ${name}`, value: description || (parameter.required ? ncfWorkflowText('Frontend.446') : ncfWorkflowText('Frontend.460')) });
                });
                if (parameters.length > 4) rows.push({ label: ncfWorkflowText('Frontend.702'), value: `${parameters.length - 4} ${ncfWorkflowText('Frontend.702')}` });
                return {
                    kind: 'NeuCharPivot Function',
                    title: String(payload.functionName || ncfWorkflowText('Frontend.703')),
                    description: String(payload.description || ncfWorkflowText('Frontend.704')),
                    rows,
                    actionText: payload.moduleAvailable ? ncfWorkflowText('Frontend.438') : ncfWorkflowText('Frontend.705')
                };
            }
            if (safePreview.kind === 'object') {
                const metadata = payload.metadata || {};
                const objectType = this.objectKindLabel(payload);
                const description = String(metadata.description || payload.description || `${objectType} 节点会在工作流中处理上游输入。`);
                return {
                    kind: payload.kind === 'a2a' ? ncfWorkflowText('Frontend.706') : 'AgentsManager',
                    title: String(payload.name || objectType),
                    description,
                    rows: [
                        { label: ncfWorkflowText('Frontend.707'), value: objectType },
                        { label: ncfWorkflowText('Frontend.697'), value: String(payload.moduleName || 'AgentsManager') },
                        { label: ncfWorkflowText('Frontend.012'), value: payload.enabled ? ncfWorkflowText('Frontend.435') : ncfWorkflowText('Frontend.436') }
                    ],
                    actionText: payload.enabled ? ncfWorkflowText('Frontend.438') : ncfWorkflowText('Frontend.451')
                };
            }
            return { kind: ncfWorkflowText('Frontend.719'), title: ncfWorkflowText('Frontend.305'), description: '', rows: [], actionText: ncfWorkflowText('Frontend.438') };
        },
        clearNodePreviewTimer() {
            if (this.nodePreviewTimer !== null && this.nodePreviewTimer !== undefined) window.clearTimeout(this.nodePreviewTimer);
            this.nodePreviewTimer = null;
        },
        showNodePreview(descriptor, mode) {
            if (!descriptor || !descriptor.kind || !descriptor.payload) return;
            const previewMode = mode === 'click' ? 'click' : 'hover';
            if (previewMode === 'hover' && this.nodePreview.visible && this.nodePreview.mode === 'click') return;
            this.clearNodePreviewTimer();
            this.nodePreview = {
                visible: true,
                kind: descriptor.kind,
                payload: descriptor.payload,
                key: descriptor.key || `${descriptor.kind}:${Date.now()}`,
                mode: previewMode,
                anchor: descriptor.anchor || null
            };
            if (previewMode === 'click') {
                this.nodePreviewTimer = window.setTimeout(() => this.dismissNodePreview(), 15000);
            }
        },
        dismissNodePreview() {
            this.clearNodePreviewTimer();
            this.nodePreview = { visible: false, kind: '', payload: null, key: '', mode: 'hover', anchor: null };
        },
        hideHoveredNodePreview(key) {
            if (this.nodePreview.visible && this.nodePreview.mode === 'hover' && (!key || key === this.nodePreview.key)) {
                this.dismissNodePreview();
            }
        },
        onDocumentClick(event) {
            if (!this.nodePreview.visible) return;
            const target = event && event.target;
            if (target && typeof target.closest === 'function' && target.closest('.workflow-node-picker, .workflow-node-preview')) return;
            this.dismissNodePreview();
        },
        async loadAll() {
            await this.refreshWorkflowList();
            // The catalog is independent of the workflow list. Let the page become
            // interactive while the optional designer metadata loads in the background.
            this.loadDesignerData();
            this.loadChatModels();
        },
        async refreshWorkflowList() {
            this.loading = true;
            try {
                const listResponse = await service.get('/Admin/NeuCharWorkflow/Index?handler=List');
                this.workflows = NeuCharWorkflowUi.unwrap(listResponse) || [];
            } catch (error) {
                this.$notify({ title: '工作流列表刷新失败', message: this.errorMessage(error, '请稍后重试。'), type: 'error' });
            } finally { this.loading = false; }
        },
        async loadDesignerData() {
            try {
                const response = await service.get('/Admin/NeuCharWorkflow/Index?handler=DesignerData');
                const data = NeuCharWorkflowUi.unwrap(response) || {};
                this.functions = data.functions || [];
                this.workflowObjects = data.objects || [];
            } catch (error) {
                this.functions = [];
                this.workflowObjects = [];
                this.$notify({
                    title: '节点目录暂不可用',
                    message: this.errorMessage(error, 'Function、Agent 或 A2A 节点目录加载失败。'),
                    type: 'warning'
                });
            }
        },
        async loadObservedOutputSchemas(workflowId) {
            const id = Number(workflowId || 0);
            if (!id) return;
            try {
                const response = await service.get(`/Admin/NeuCharWorkflow/Index?handler=ObservedOutputSchemas&id=${id}`);
                if (Number(this.form.id) !== id) return;
                this.observedOutputSchemas = NeuCharWorkflowUi.unwrap(response) || [];
            } catch (_) {
                // Observed fields are optional editor hints; a history read must not
                // delay or prevent opening the workflow.
            }
        },
        async loadChatModels() {
            try {
                const response = await service.post(
                    '/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync',
                    {
                        page: 0,
                        size: 0,
                        order: 'Alias asc'
                    },
                    { customAlert: true });
                this.chatModels = (NeuCharWorkflowUi.unwrap(response) || [])
                    .filter(model => Number(model.configModelType) === 2);
            } catch (error) {
                this.chatModels = [];
                this.$notify({
                    title: '模型列表暂不可用',
                    message: this.errorMessage(error, '请确认 AIKernel 模块及当前账号的访问权限。'),
                    type: 'warning'
                });
            }
        },
        async openTaskRoute() {
            const search = window.location && window.location.search;
            if (!search) return;
            const query = new URLSearchParams(search);
            const workflowId = Number(query.get('workflowId'));
            const runId = String(query.get('runId') || '').trim();
            if (!Number.isInteger(workflowId) || workflowId <= 0) return;

            try {
                await this.editWorkflow(workflowId);
                if (Number(this.form.id) !== workflowId || !runId) return;
                this.run.consoleOpen = true;
                this.run.runId = runId;
                this.run.running = true;
                this.run.status = 'running';
                this.run.lastSequence = 0;
                this.pollRun();
            } catch (error) {
                this.$notify({ title: '无法打开任务', message: this.errorMessage(error, '任务或工作流已不存在。'), type: 'warning' });
            }
        },
        openSubWorkflow(workflowId) {
            const id = Number(workflowId || 0);
            if (!Number.isInteger(id) || id <= 0) return;
            const url = '/Admin/NeuCharWorkflow/Index?workflowId=' + encodeURIComponent(id);
            const viewer = window.open(url, '_blank', 'noopener,noreferrer');
            if (viewer) viewer.opener = null;
        },
        openWorkflowTasks(status) {
            const workflowId = Number(this.form && this.form.id || 0);
            if (!Number.isInteger(workflowId) || workflowId <= 0) return;
            const query = new URLSearchParams({ workflowId: String(workflowId) });
            if (status) query.set('status', status);
            window.location.assign(`/Admin/NeuCharWorkflow/Tasks?${query.toString()}`);
        },
        openWorkflowAnalytics() {
            const workflowId = Number(this.form && this.form.id || 0);
            if (!Number.isInteger(workflowId) || workflowId <= 0) return;
            window.location.assign(`/Admin/NeuCharWorkflow/Analytics?workflowId=${encodeURIComponent(workflowId)}`);
        },
        exportWorkflow(format) {
            const workflowId = Number(this.form && this.form.id || 0);
            if (!Number.isInteger(workflowId) || workflowId <= 0) return;
            const selectedFormat = String(format || 'json').toLowerCase() === 'yaml' ? 'yaml' : 'json';
            window.location.assign(`/Admin/NeuCharWorkflow/Index?handler=Export&id=${encodeURIComponent(workflowId)}&format=${selectedFormat}`);
        },
        chooseWorkflowImport() {
            const input = this.$refs && this.$refs.workflowImportInput;
            if (input) input.click();
        },
        async importWorkflowFile(event) {
            const input = event && event.target;
            const file = input && input.files && input.files[0];
            if (!file) return;
            const extension = String(file.name || '').toLowerCase().endsWith('.yaml') ||
                String(file.name || '').toLowerCase().endsWith('.yml') ? 'yaml' : 'json';
            try {
                const content = await file.text();
                const previewResponse = await service.post('/Admin/NeuCharWorkflow/Index?handler=ValidateImport', {
                    content,
                    format: extension
                }, { customAlert: true });
                const preview = NeuCharWorkflowUi.unwrap(previewResponse);
                const errors = (preview && preview.errors) || [];
                if (errors.length || !preview || preview.valid !== true) {
                    const message = errors.map(error => error.message).join('；') || 'Workflow 文件校验失败。';
                    this.$notify({ title: '无法导入', message, type: 'error', duration: 6000 });
                    return;
                }
                const secrets = (preview.secrets || []).length;
                const warning = secrets ? `其中 ${secrets} 个敏感字段需要导入后重新配置。` : '';
                await this.$confirm(
                    `将以禁用草稿导入“${preview.name || 'Imported Workflow'}”。${warning}`,
                    '确认导入 Workflow',
                    { confirmButtonText: '导入草稿', cancelButtonText: '取消', type: 'warning' });
                const response = await service.post('/Admin/NeuCharWorkflow/Index?handler=Import', {
                    content,
                    format: extension
                }, { customAlert: true });
                const saved = NeuCharWorkflowUi.unwrap(response);
                this.applySavedWorkflow(saved);
                this.editing = true;
                await this.refreshWorkflowList();
                this.$notify({ title: 'Workflow', message: '已导入为禁用草稿，请检查配置后再启用。', type: 'success' });
            } catch (error) {
                if (!(error && error.message === 'cancel')) {
                    this.$notify({ title: '导入失败', message: this.errorMessage(error, 'Workflow 文件导入失败。'), type: 'error' });
                }
            } finally {
                if (input) input.value = '';
            }
        },
        hasAgentGroupConversation(event) {
            const reference = event && event.objectReference;
            return reference && reference.kind === 'agent-group' && Number(reference.chatTaskId) > 0;
        },
        openAgentGroupConversation(reference) {
            const chatTaskId = Number(reference && reference.chatTaskId || 0);
            const chatGroupId = Number(reference && reference.chatGroupId || 0);
            if (!Number.isInteger(chatTaskId) || chatTaskId <= 0) return;
            const query = new URLSearchParams({ tab: 'second', taskId: String(chatTaskId) });
            if (Number.isInteger(chatGroupId) && chatGroupId > 0) query.set('groupId', String(chatGroupId));
            const viewer = window.open(`/Admin/AgentsManager/Index#${query.toString()}`, '_blank', 'noopener,noreferrer');
            if (viewer) viewer.opener = null;
        },
        async createWorkflow() {
            if (this.editingLocked || this.saveState.saving || !await this.confirmDiscardChanges('新建工作流')) return;
            this.form = this.emptyForm();
            this.observedOutputSchemas = [];
            this.editing = true;
            this.workflowSettingsVisible = false;
            this.webhookHelpVisible = false;
            this.setSelectedNodes([], { openInspector: false });
            this.selectionBox = this.emptySelectionBox();
            this.resetSaveState();
            this.cancelConnection();
            this.closeEdgeInsertMenu();
            this.resetRunState();
            this.syncTriggerNode();
            this.$nextTick(this.autoLayout);
        },
        async editWorkflow(id) {
            if (this.editingLocked || this.saveState.saving || Number(id) === Number(this.form.id)) return;
            if (!await this.confirmDiscardChanges('切换工作流')) return;
            this.workflowSettingsVisible = false;
            this.webhookHelpVisible = false;
            this.loading = true;
            try {
                const response = await service.get(`/Admin/NeuCharWorkflow/Index?handler=Detail&id=${id}`);
                const item = NeuCharWorkflowUi.unwrap(response);
                const graph = NeuCharWorkflowUi.parseJson(item.graphJson, { nodes: [], edges: [] });
                graph.nodes = graph.nodes || [];
                graph.edges = graph.edges || [];
                this.ensureGraphLayout(graph);
                graph.nodes.forEach(node => {
                    node.config = node.config || {};
                    node.x = Number.isFinite(Number(node.x)) ? Number(node.x) : 80;
                    node.y = Number.isFinite(Number(node.y)) ? Number(node.y) : 80;
                    if (node.type === 'function') node.config.parameters = node.config.parameters || {};
                    this.ensureWorkflowObjectPolicyConfig(node);
                });
                graph.edges.forEach(edge => {
                    const source = graph.nodes.find(node => node.id === edge.source);
                    const target = graph.nodes.find(node => node.id === edge.target);
                    edge.sourceHandle = source && source.type === 'condition'
                        ? (['false', 'break'].includes(edge.sourceHandle) ? edge.sourceHandle : 'true')
                        : 'default';
                    edge.targetHandle = target && target.type === 'loop-end'
                        ? (edge.targetHandle === 'break' ? 'break' : 'continue')
                        : 'default';
                });
                const trigger = NeuCharWorkflowUi.parseJson(item.triggerConfigJson, {});
                this.form = {
                    ...item,
                    graph,
                    intervalSeconds: Number(trigger.intervalSeconds || 300),
                    webhookMethod: String(trigger.method || 'any').toLowerCase(),
                    webhookToken: String(trigger.token || ''),
                    webhookParameters: (trigger.parameters || []).map(parameter => ({
                        _id: this.makeId('webhook-parameter'),
                        name: parameter.name || '',
                        required: !!parameter.required,
                        description: parameter.description || ''
                    })),
                    chatTitle: String(trigger.title || ''),
                    chatGreeting: String(trigger.greeting || ''),
                    chatAllowGuest: trigger.allowGuest !== false,
                    autoSaveMinutes: Number(item.autoSaveMinutes ?? 3),
                    revision: Number(item.revision || 0)
                };
                this.observedOutputSchemas = Array.isArray(item.observedOutputSchemas)
                    ? item.observedOutputSchemas
                    : [];
                this.editing = true;
                this.loadObservedOutputSchemas(item.id || id);
                this.setSelectedNodes(graph.nodes.length ? [graph.nodes[0]] : [], { openInspector: false });
                this.selectionBox = this.emptySelectionBox();
                this.cancelConnection();
                this.closeEdgeInsertMenu();
                this.resetRunState();
                this.markSaved();
                this.$nextTick(() => {
                    if (graph.nodes.length > 1 && graph.nodes.every(node => Number(node.x) === 0)) {
                        this.autoLayout();
                    } else {
                        this.updateCanvasSize();
                    }
                    this.$nextTick(() => this.fitCanvasToNodes());
                });
            } finally { this.loading = false; }
        },
        makeId(prefix) { return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}`; },
        syncTriggerNode() {
            if (this.editingLocked) return;
            const type = this.form.triggerType === 'interval'
                ? 'interval-trigger'
                : this.form.triggerType === 'webhook'
                    ? 'webhook-trigger'
                    : this.form.triggerType === 'chat' ? 'chat-trigger' : 'manual-trigger';
            const labels = { 'manual-trigger': '手动触发', 'interval-trigger': '间隔触发', 'webhook-trigger': 'Webhook 触发', 'chat-trigger': 'Chat 触发' };
            const existing = this.form.graph.nodes.find(node => String(node.type).endsWith('trigger'));
            if (existing) {
                existing.type = type;
                existing.name = labels[type] || '手动触发';
                existing.config = existing.config || {};
                if (type === 'webhook-trigger') {
                    existing.config.webhookParameters = (this.form.webhookParameters || []).map(parameter => ({ name: parameter.name, required: !!parameter.required, description: parameter.description || '' }));
                } else {
                    delete existing.config.webhookParameters;
                }
            } else {
                const trigger = { id: this.makeId('trigger'), type, name: labels[type] || '手动触发', x: 430, y: 60, config: type === 'webhook-trigger' ? { webhookParameters: [] } : {} };
                this.form.graph.nodes.unshift(trigger);
                this.setSelectedNodes([trigger]);
            }
            this.updateCanvasSize();
        },
        createSimpleNode(type, name) {
            const config = type === 'condition'
                ? { left: '{{input}}', operator: 'equals', right: '', breakOn: '' }
                : type === 'delay' ? { seconds: 1 }
                    : type === 'loop' ? { count: 3 }
                    : type === 'loop-end' ? { loopId: '' }
                    : type === 'sub-workflow' ? { workflowId: 0, prompt: '{{input}}' }
                    : type === 'code' ? { assignments: [] }
                    : type === 'aggregate' ? { outputTemplate: '' }
                    : type === 'console' ? { printTemplate: '{{input}}' }
                    : type === 'neubell'
                        ? { title: 'Workflow 提醒', summary: '{{input}}', consumeMode: 'item' }
                        : type === 'human-input'
                            ? { title: 'Workflow 等待人工输入', prompt: '请补充必要信息：{{input}}', externalResumeEnabled: false, externalResumeKey: '' }
                        : {};
            return { id: this.makeId(type), type, name, x: 80, y: 80, config };
        },
        generateHumanInputExternalKey(node) {
            if (this.editingLocked || !node || node.type !== 'human-input') return;
            const cryptoApi = window.crypto;
            if (!cryptoApi || typeof cryptoApi.getRandomValues !== 'function') {
                this.$message.error('当前浏览器无法安全生成恢复密钥，请手动填写高强度随机密钥。');
                return;
            }
            const bytes = new Uint8Array(32);
            cryptoApi.getRandomValues(bytes);
            node.config.externalResumeKey = Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('');
            this.$message.success('已生成新的外部恢复密钥；保存后请交给可信的外部调用方。');
        },
        createFunctionNode(fn) {
            return {
                id: this.makeId('function'),
                type: 'function',
                name: fn.functionName,
                x: 80,
                y: 80,
                config: {
                    functionId: Number(fn.id || 0),
                    moduleUid: fn.moduleUid,
                    functionKey: fn.functionKey,
                    parameters: NeuCharWorkflowUi.createParameterValues(fn)
                }
            };
        },
        createObjectNode(object) {
            const supportsHumanInTheLoop = String(object?.metadata?.supportsHumanInTheLoop || '').toLowerCase() === 'true';
            const supportsHumanParticipant = String(object?.metadata?.supportsHumanParticipant || '').toLowerCase() === 'true';
            return {
                id: this.makeId(object.kind),
                type: object.kind,
                name: object.name,
                x: 80,
                y: 80,
                config: {
                    providerId: object.providerId,
                    objectId: object.objectId,
                    prompt: '处理以下输入：{{input}}',
                    aiModelId: null,
                    personality: true,
                    allowFunctionCalls: object.kind === 'agent' ? false : supportsHumanInTheLoop,
                    humanInTheLoopLevel: 0,
                    pluginToolPermission: 0,
                    mcpToolPermission: 0,
                    includeHumanParticipant: false,
                    chatMaxRound: 20
                }
            };
        },
        ensureWorkflowObjectPolicyConfig(node) {
            if (!node || !['agent', 'agent-group'].includes(node.type)) return;
            node.config = node.config || {};
            const defaults = {
                allowFunctionCalls: node.type === 'agent' ? false : true,
                aiModelId: 0,
                // Existing Workflow definitions used the task/default model for every Agent.
                // Keep those persisted graphs behaviorally stable; newly inserted nodes opt in.
                personality: false,
                humanInTheLoopLevel: 0,
                pluginToolPermission: 0,
                mcpToolPermission: 0,
                includeHumanParticipant: false,
                chatMaxRound: 20
            };
            Object.entries(defaults).forEach(([key, value]) => {
                if (node.config[key] === undefined || node.config[key] === null) {
                    this.$set(node.config, key, value);
                }
            });
        },
        addSimpleNode(type, name, insertionEdge) {
            if (this.editingLocked) return;
            this.dismissNodePreview();
            return this.appendNode(this.createSimpleNode(type, name), insertionEdge);
        },
        addFunctionNode(fn, insertionEdge) {
            if (this.editingLocked || !fn.moduleAvailable) return;
            this.dismissNodePreview();
            return this.appendNode(this.createFunctionNode(fn), insertionEdge);
        },
        addObjectNode(object, insertionEdge) {
            if (this.editingLocked || !object.enabled) return;
            this.dismissNodePreview();
            return this.appendNode(this.createObjectNode(object), insertionEdge);
        },
        insertSimpleNode(type, name) { return this.addSimpleNode(type, name, this.edgeInsertMenu.edge); },
        insertFunctionNode(fn) { return this.addFunctionNode(fn, this.edgeInsertMenu.edge); },
        insertObjectNode(object) { return this.addObjectNode(object, this.edgeInsertMenu.edge); },
        beginPaletteNodeDrag(kind, payload) {
            if (this.editingLocked || !kind || !payload) return;
            this.dismissNodePreview();
            this.closeContextMenu();
            this.closeCanvasContextMenu();
            this.closeCanvasNodeInsertMenu();
            this.closeEdgeInsertMenu();
            this.paletteDrag = { active: true, kind, payload, hoverEdgeId: '' };
        },
        endPaletteNodeDrag() {
            this.paletteDrag = { active: false, kind: '', payload: null, hoverEdgeId: '' };
        },
        onCanvasDragOver(event) {
            if (!this.paletteDrag.active || this.editingLocked) return;
            const edge = this.findEdgeAtPoint(this.canvasPoint(event));
            this.paletteDrag.hoverEdgeId = edge ? edge.id : '';
            if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
        },
        onCanvasDrop(event) {
            if (!this.paletteDrag.active || this.editingLocked) return;
            const drag = this.paletteDrag;
            const point = this.canvasPoint(event);
            const edge = this.findEdgeAtPoint(point);
            this.endPaletteNodeDrag();
            if (edge) {
                if (drag.kind === 'system') this.addSimpleNode(drag.payload.type, drag.payload.name, edge);
                else if (drag.kind === 'function') this.addFunctionNode(drag.payload, edge);
                else if (drag.kind === 'object') this.addObjectNode(drag.payload, edge);
                return;
            }
            this.placePaletteNodeAtCanvas(drag.kind, drag.payload, point);
        },
        placePaletteNodeAtCanvas(kind, payload, point) {
            if (this.editingLocked || !payload) return null;
            let node = null;
            if (kind === 'system') node = this.createSimpleNode(payload.type, payload.name);
            else if (kind === 'function' && payload.moduleAvailable) node = this.createFunctionNode(payload);
            else if (kind === 'object' && payload.enabled) node = this.createObjectNode(payload);
            if (!node) return null;
            const snap = 20;
            node.x = Math.max(20, Math.round((Number(point?.x || 0) - 110) / snap) * snap);
            node.y = Math.max(50, Math.round((Number(point?.y || 0) - 46) / snap) * snap);
            this.form.graph.nodes.push(node);
            this.setSelectedNodes([node]);
            this.cancelConnection();
            this.closeEdgeInsertMenu();
            this.updateCanvasSize();
            this.scheduleAutoSave();
            return node;
        },
        appendNode(node, insertionEdge) {
            if (insertionEdge) return this.insertNodeIntoEdge(node, insertionEdge);
            const previous = [...this.form.graph.nodes].reverse().find(item =>
                item.type !== 'end' && item.type !== 'condition' && !this.form.graph.edges.some(edge => edge.source === item.id));
            this.form.graph.nodes.push(node);
            if (previous && !String(node.type).endsWith('trigger') && this.canConnect(previous, node, 'default')) {
                this.setTarget(previous, 'default', node.id, true);
            }
            this.setSelectedNodes([node]);
            this.autoLayout();
            return node;
        },
        insertNodeIntoEdge(node, edge, existingNode) {
            if (this.editingLocked || !node || node.type === 'end' || String(node.type || '').endsWith('trigger')) {
                if (!this.editingLocked) this.$notify({ title: '无法插入节点', message: '结束或触发器节点不能插入到已有连线中。', type: 'warning' });
                return null;
            }
            const liveEdge = this.form.graph.edges.find(item => item.id === edge.id);
            const source = liveEdge && this.form.graph.nodes.find(item => item.id === liveEdge.source);
            const target = liveEdge && this.form.graph.nodes.find(item => item.id === liveEdge.target);
            if (!liveEdge || !source || !target) {
                this.closeEdgeInsertMenu();
                this.$notify({ title: '无法插入节点', message: '原连线已变化，请重新打开插入菜单。', type: 'warning' });
                return null;
            }

            const sourceHandle = source.type === 'condition'
                ? (['false', 'break'].includes(liveEdge.sourceHandle) ? liveEdge.sourceHandle : 'true')
                : 'default';
            const insertedHandle = node.type === 'condition' ? 'true' : 'default';
            const originalTargetHandle = liveEdge.targetHandle || 'default';
            const originalEdges = this.form.graph.edges;
            this.form.graph.edges = originalEdges.filter(item => item.id !== liveEdge.id);
            const valid = this.canConnect(source, node, sourceHandle, this.targetHandleFor(source, node, sourceHandle)) &&
                this.canConnect(node, target, insertedHandle, originalTargetHandle);
            if (!valid) {
                this.form.graph.edges = originalEdges;
                this.$notify({ title: '无法插入节点', message: '该节点无法同时连接原连线的上下游，请选择其他节点。', type: 'warning' });
                return null;
            }

            const position = this.findInsertedNodePosition(source, target);
            node.x = position.x;
            node.y = position.y;
            if (!existingNode) this.form.graph.nodes.push(node);
            this.form.graph.edges.push(
                { id: this.makeId('edge'), source: source.id, target: node.id, sourceHandle, targetHandle: this.targetHandleFor(source, node, sourceHandle) },
                { id: this.makeId('edge'), source: node.id, target: target.id, sourceHandle: insertedHandle, targetHandle: originalTargetHandle });
            this.setSelectedNodes([node]);
            this.cancelConnection();
            this.closeEdgeInsertMenu();
            this.updateCanvasSize();
            this.scheduleAutoSave();
            return node;
        },
        insertExistingNodeIntoEdge(node, edge) {
            if (!node || !edge || this.editingLocked) return null;
            const relatedEdges = this.form.graph.edges.filter(item => item.source === node.id || item.target === node.id);
            if (relatedEdges.length) {
                this.$notify({
                    title: '请先断开节点',
                    message: '为避免静默改变已有流程，只能将尚未连接的节点拖入连线。',
                    type: 'warning'
                });
                return null;
            }
            return this.insertNodeIntoEdge(node, edge, true);
        },
        findInsertedNodePosition(source, target) {
            const nodeWidth = 220;
            const nodeHeight = 92;
            const nodeGap = 20;
            const gridSize = 40;
            const sourceX = Number.isFinite(Number(source.x)) ? Number(source.x) : 20;
            const sourceY = Number.isFinite(Number(source.y)) ? Number(source.y) : 20;
            const targetX = Number.isFinite(Number(target.x)) ? Number(target.x) : sourceX + 280;
            const targetY = Number.isFinite(Number(target.y)) ? Number(target.y) : sourceY + 160;
            const preferred = {
                x: Math.max(20, Math.round(((sourceX + targetX) / 2) / gridSize) * gridSize),
                y: Math.max(20, Math.round(((sourceY + targetY) / 2) / gridSize) * gridSize)
            };
            const occupied = this.form.graph.nodes.map(item => ({ x: Number(item.x) || 0, y: Number(item.y) || 0 }));
            const isFree = candidate => !occupied.some(position =>
                Math.abs(candidate.x - position.x) < nodeWidth + nodeGap &&
                Math.abs(candidate.y - position.y) < nodeHeight + nodeGap);
            let best = null;
            let bestScore = Number.POSITIVE_INFINITY;
            for (let ring = 0; ring <= 12; ring++) {
                for (let offsetX = -ring; offsetX <= ring; offsetX++) {
                    for (let offsetY = -ring; offsetY <= ring; offsetY++) {
                        if (ring && Math.abs(offsetX) !== ring && Math.abs(offsetY) !== ring) continue;
                        const candidate = {
                            x: Math.max(20, preferred.x + offsetX * gridSize),
                            y: Math.max(20, preferred.y + offsetY * gridSize)
                        };
                        if (!isFree(candidate)) continue;
                        const distance = Math.hypot(candidate.x - preferred.x, candidate.y - preferred.y);
                        const directionalPenalty = this.isHorizontalLayout()
                            ? Math.abs(candidate.y - preferred.y) * .18
                            : Math.abs(candidate.x - preferred.x) * .18;
                        const score = distance + directionalPenalty;
                        if (score < bestScore) {
                            best = candidate;
                            bestScore = score;
                        }
                    }
                }
            }
            return best || preferred;
        },
        canDeleteNode(node) {
            return !!node && !String(node.type || '').endsWith('trigger');
        },
        canDuplicateNode(node) {
            return !!node && !String(node.type || '').endsWith('trigger');
        },
        isNodeBeingEdited(node) {
            return !!node &&
                this.editing &&
                !this.editingLocked &&
                !this.inspectorCollapsed &&
                this.selectedNodeId === node.id;
        },
        isNodeSelected(node) {
            return !!node && (this.selectedNodeIds || []).includes(node.id);
        },
        setSelectedNodes(nodes, options) {
            const selected = [...new Set((nodes || []).filter(Boolean).map(node => node.id))];
            const current = this.selectedNodeIds || [];
            const selectionChanged = selected.length !== current.length || selected.some((id, index) => id !== current[index]);
            const selectedNodeId = selected.length ? selected[selected.length - 1] : '';
            if (selectionChanged) this.selectedNodeIds = selected;
            if (this.selectedNodeId !== selectedNodeId) this.selectedNodeId = selectedNodeId;
            if (options?.openInspector !== false && selected.length && this.inspectorCollapsed) this.inspectorCollapsed = false;
        },
        duplicateNodes(nodes) {
            if (this.editingLocked) return [];
            const sourceNodes = (nodes || []).filter(node => this.canDuplicateNode(node));
            if (!sourceNodes.length) return [];
            const copiedIds = new Set(sourceNodes.map(node => node.id));
            const nodeIdMap = new Map();
            const copies = sourceNodes.map(node => {
                const copy = JSON.parse(JSON.stringify(node));
                copy.id = this.makeId(node.type || 'node');
                copy.name = `${node.name || '节点'}（副本）`;
                copy.x = Number(node.x || 0) + 40;
                copy.y = Number(node.y || 0) + 40;
                nodeIdMap.set(node.id, copy.id);
                return copy;
            });
            const copiedEdges = this.form.graph.edges
                .filter(edge => copiedIds.has(edge.source) && copiedIds.has(edge.target))
                .map(edge => ({
                    ...JSON.parse(JSON.stringify(edge)),
                    id: this.makeId('edge'),
                    source: nodeIdMap.get(edge.source),
                    target: nodeIdMap.get(edge.target)
                }));
            this.form.graph.nodes.push(...copies);
            this.form.graph.edges.push(...copiedEdges);
            this.setSelectedNodes(copies);
            this.cancelConnection();
            this.closeEdgeInsertMenu();
            this.updateCanvasSize();
            this.scheduleAutoSave();
            return copies;
        },
        duplicateNode(node) {
            return this.duplicateNodes([node])[0] || false;
        },
        removeNodes(nodes) {
            if (this.editingLocked) return [];
            const removable = (nodes || []).filter(node => this.canDeleteNode(node));
            if (!removable.length) return [];
            const removedIds = new Set(removable.map(node => node.id));
            this.form.graph.nodes = this.form.graph.nodes.filter(node => !removedIds.has(node.id));
            this.form.graph.edges = this.form.graph.edges.filter(edge => !removedIds.has(edge.source) && !removedIds.has(edge.target));
            const remainingSelection = (this.selectedNodeIds || []).filter(id => !removedIds.has(id));
            const remainingNodes = this.form.graph.nodes.filter(node => remainingSelection.includes(node.id));
            this.setSelectedNodes(remainingNodes, { openInspector: false });
            this.cancelConnection();
            this.closeEdgeInsertMenu();
            this.updateCanvasSize();
            this.scheduleAutoSave();
            return removable;
        },
        removeNode(node) {
            this.removeNodes([node]);
        },
        openNodeContextMenu(event, node) {
            const documentElement = typeof document !== 'undefined' ? document.documentElement : null;
            const viewportWidth = Number(window.innerWidth || (documentElement && documentElement.clientWidth) || 0);
            const viewportHeight = Number(window.innerHeight || (documentElement && documentElement.clientHeight) || 0);
            const menuWidth = 156;
            if (!this.isNodeSelected(node)) this.setSelectedNodes([node]);
            const menuHeight = this.selectedNodeIds && this.selectedNodeIds.length > 1 ? 116 : 92;
            this.contextMenu = {
                visible: true,
                x: Math.max(0, viewportWidth ? Math.min(event.clientX, viewportWidth - menuWidth) : event.clientX),
                y: Math.max(0, viewportHeight ? Math.min(event.clientY, viewportHeight - menuHeight) : event.clientY),
                node
            };
            this.closeEdgeInsertMenu();
            this.closeCanvasContextMenu();
            this.closeCanvasNodeInsertMenu();
        },
        duplicateContextNode() {
            this.closeContextMenu();
            this.duplicateNodes(this.selectedDuplicableNodes);
        },
        removeContextNode() {
            this.closeContextMenu();
            this.removeNodes(this.selectedDeletableNodes);
        },
        closeContextMenu() {
            this.contextMenu.visible = false;
            this.contextMenu.node = null;
        },
        closeCanvasContextMenu() {
            this.canvasContextMenu = { visible: false, x: 0, y: 0, point: null };
        },
        closeCanvasNodeInsertMenu() {
            this.canvasNodeInsertMenu = { visible: false, x: 0, y: 0, point: null };
        },
        onCanvasContextMenu(event) {
            const target = event && event.target;
            if (Date.now() < Number(this.suppressCanvasContextMenuUntil || 0)) {
                this.suppressCanvasContextMenuUntil = 0;
                return;
            }
            if (this.canvasPan.active && this.canvasPan.moved) return;
            if (target && typeof target.closest === 'function' &&
                target.closest('.workflow-node, button, .workflow-edge-insert-menu, .workflow-context-menu, .canvas-zoom-controls, .canvas-minimap')) return;
            const tagName = String(target?.tagName || '').toLowerCase();
            if (['path', 'text', 'line'].includes(tagName)) return;
            this.openCanvasContextMenu(event);
        },
        openCanvasContextMenu(event) {
            const documentElement = typeof document !== 'undefined' ? document.documentElement : null;
            const viewportWidth = Number(window.innerWidth || (documentElement && documentElement.clientWidth) || 0);
            const viewportHeight = Number(window.innerHeight || (documentElement && documentElement.clientHeight) || 0);
            const menuWidth = 196;
            const menuHeight = 168;
            const point = this.canvasPoint(event);
            this.closeContextMenu();
            this.closeEdgeInsertMenu();
            this.closeCanvasNodeInsertMenu();
            this.cancelConnection();
            this.canvasContextMenu = {
                visible: true,
                x: Math.max(0, viewportWidth ? Math.min(event.clientX, viewportWidth - menuWidth) : event.clientX),
                y: Math.max(0, viewportHeight ? Math.min(event.clientY, viewportHeight - menuHeight) : event.clientY),
                point
            };
        },
        openCanvasNodeInsertMenu() {
            if (this.editingLocked || !this.canvasContextMenu.point) return;
            const point = this.canvasContextMenu.point;
            const menuWidth = 372;
            const menuHeight = 520;
            this.closeCanvasContextMenu();
            this.closeContextMenu();
            this.closeEdgeInsertMenu();
            this.canvasNodeInsertMenu = {
                visible: true,
                point,
                x: Math.max(18, Math.min(Math.max(18, this.canvasSize.width - menuWidth - 18), Math.round(point.x + 12))),
                y: Math.max(18, Math.min(Math.max(18, this.canvasSize.height - menuHeight - 18), Math.round(point.y + 12)))
            };
        },
        addCanvasContextNode(kind, payload) {
            const point = this.canvasNodeInsertMenu.point;
            this.closeCanvasNodeInsertMenu();
            this.dismissNodePreview();
            if (!point || !payload) return null;
            return this.placePaletteNodeAtCanvas(kind, payload, point);
        },
        addCanvasContextSimpleNode(type, name) {
            return this.addCanvasContextNode('system', { type, name });
        },
        addCanvasContextFunctionNode(fn) {
            return this.addCanvasContextNode('function', fn);
        },
        addCanvasContextObjectNode(object) {
            return this.addCanvasContextNode('object', object);
        },
        autoLayoutFromCanvasMenu() {
            this.closeCanvasContextMenu();
            this.autoLayout();
        },
        alignGridFromCanvasMenu() {
            this.closeCanvasContextMenu();
            this.alignToNearbyGrid();
        },
        fitCanvasFromCanvasMenu() {
            this.closeCanvasContextMenu();
            this.fitCanvasToNodes();
        },
        selectNode(node, event) {
            this.closeContextMenu();
            this.closeEdgeInsertMenu();
            this.closeCanvasContextMenu();
            this.closeCanvasNodeInsertMenu();
            if (!node) return;
            const current = this.selectedNodeIds || [];
            const additive = !!(event && (event.metaKey || event.ctrlKey));
            let next = current;
            if (additive) {
                next = current.includes(node.id)
                    ? current.filter(id => id !== node.id)
                    : [...current, node.id];
            } else if (!current.includes(node.id)) {
                next = [node.id];
            }
            const nodes = this.form.graph.nodes.filter(item => next.includes(item.id));
            this.setSelectedNodes(nodes);
            this.highlightLoopContext(node);
        },
        supportsMultipleInputs(node) { return node && ['aggregate', 'merge', 'function', 'loop-end'].includes(node.type); },
        supportsMultipleOutputs(node) { return node && ['condition', 'parallel'].includes(node.type); },
        targetFor(node, sourceHandle) {
            const edge = this.form.graph.edges.find(item => item.source === node.id && item.sourceHandle === sourceHandle);
            return edge ? edge.target : '';
        },
        incomingEdges(nodeId) { return this.form.graph.edges.filter(edge => edge.target === nodeId); },
        outgoingEdges(nodeId) { return this.form.graph.edges.filter(edge => edge.source === nodeId); },
        targetHandleFor(source, target, sourceHandle) {
            if (target?.type !== 'loop-end') return 'default';
            return source?.type === 'condition' && sourceHandle === 'break' ? 'break' : 'continue';
        },
        availableTargets(node, sourceHandle) {
            const handle = node.type === 'condition' ? sourceHandle : 'default';
            return this.form.graph.nodes.filter(target =>
                this.form.graph.edges.some(edge => edge.source === node.id && edge.target === target.id && edge.sourceHandle === handle) ||
                this.canConnect(node, target, handle, this.targetHandleFor(node, target, handle)));
        },
        canConnect(source, target, sourceHandle, targetHandle) {
            if (!source || !target || source.id === target.id || source.type === 'end' || String(target.type).endsWith('trigger')) return false;
            if (this.wouldCreateCycle(source.id, target.id)) return false;
            const handle = source.type === 'condition' ? sourceHandle : 'default';
            const targetHandleValue = targetHandle || this.targetHandleFor(source, target, handle);
            if (this.form.graph.edges.some(edge => edge.source === source.id && edge.target === target.id && edge.sourceHandle === handle && edge.targetHandle === targetHandleValue)) return false;
            const incoming = this.incomingEdges(target.id).filter(edge =>
                !(edge.source === source.id && edge.sourceHandle === handle && edge.targetHandle === targetHandleValue));
            if (incoming.length && !this.supportsMultipleInputs(target)) return false;
            if (target.type === 'loop-end' && incoming.some(edge => edge.targetHandle === targetHandleValue)) return false;
            if (source.type === 'condition' && handle === 'break' && target.type !== 'loop-end') return false;
            if (target.type === 'loop-end' && targetHandleValue === 'break' &&
                !(source.type === 'condition' && handle === 'break')) return false;
            return true;
        },
        wouldCreateCycle(sourceId, targetId) {
            const queue = [targetId];
            const visited = new Set();
            while (queue.length) {
                const current = queue.shift();
                if (current === sourceId) return true;
                if (visited.has(current)) continue;
                visited.add(current);
                this.form.graph.edges.filter(edge => edge.source === current).forEach(edge => queue.push(edge.target));
            }
            return false;
        },
        setTarget(node, sourceHandle, targetId, silent, targetHandle) {
            if (this.editingLocked) return false;
            const handle = node.type === 'condition' ? sourceHandle : 'default';
            const target = this.form.graph.nodes.find(item => item.id === targetId);
            const targetHandleValue = targetHandle || this.targetHandleFor(node, target, handle);
            if (targetId && this.form.graph.edges.some(edge =>
                edge.source === node.id && edge.target === targetId && edge.sourceHandle === handle && edge.targetHandle === targetHandleValue)) return true;
            if (targetId && !this.canConnect(node, target, handle, targetHandleValue)) {
                if (!silent) this.$notify({ title: '无法连接', message: '目标已有上游、连接会形成循环，或节点不支持该连接方式。多对一目标可使用 Function、聚合或逐项合流节点。', type: 'warning' });
                return false;
            }
            if (node.type !== 'parallel') {
                this.form.graph.edges = this.form.graph.edges.filter(edge =>
                    !(edge.source === node.id && edge.sourceHandle === handle));
            }
            if (targetId) {
                this.form.graph.edges.push({ id: this.makeId('edge'), source: node.id, target: targetId, sourceHandle: handle, targetHandle: targetHandleValue });
            }
            return true;
        },
        beginConnection(event, node, sourceHandle) {
            if (this.editingLocked || node.type === 'end') return;
            const point = this.canvasPoint(event);
            this.connectionDraft = {
                sourceId: node.id,
                sourceHandle: node.type === 'condition' ? sourceHandle : 'default',
                x: point.x,
                y: point.y
            };
            event.preventDefault();
        },
        completeConnection(node, targetHandle) {
            if (!this.connectionDraft.sourceId || this.editingLocked) return;
            const source = this.form.graph.nodes.find(item => item.id === this.connectionDraft.sourceId);
            this.setTarget(source, this.connectionDraft.sourceHandle, node.id, false, targetHandle);
            this.cancelConnection();
        },
        cancelConnection() { this.connectionDraft = { sourceId: '', sourceHandle: '', x: 0, y: 0 }; },
        removeEdge(edge) {
            if (this.editingLocked) return;
            this.form.graph.edges = this.form.graph.edges.filter(item => item.id !== edge.id);
            if (this.edgeInsertMenu.edge && this.edgeInsertMenu.edge.id === edge.id) this.closeEdgeInsertMenu();
            this.scheduleAutoSave();
        },
        openEdgeInsertMenu(edge) {
            if (this.editingLocked || !edge) return;
            const liveEdge = this.form.graph.edges.find(item => item.id === edge.id);
            if (!liveEdge) return;
            const start = this.edgeStart(liveEdge);
            const end = this.edgeEnd(liveEdge);
            const menuWidth = 372;
            const menuHeight = 520;
            const x = (start.x + end.x) / 2 - menuWidth / 2;
            const y = (start.y + end.y) / 2 + 20;
            this.closeContextMenu();
            this.closeCanvasContextMenu();
            this.closeCanvasNodeInsertMenu();
            this.cancelConnection();
            this.edgeInsertMenu = {
                visible: true,
                edge: liveEdge,
                x: Math.max(18, Math.min(Math.max(18, this.canvasSize.width - menuWidth - 18), Math.round(x))),
                y: Math.max(18, Math.min(Math.max(18, this.canvasSize.height - menuHeight - 18), Math.round(y)))
            };
        },
        closeEdgeInsertMenu() {
            this.edgeInsertMenu = { visible: false, edge: null, x: 0, y: 0 };
        },
        closeCanvasOverlays() {
            this.cancelConnection();
            this.closeContextMenu();
            this.closeCanvasContextMenu();
            this.closeEdgeInsertMenu();
            this.closeCanvasNodeInsertMenu();
        },
        emptySelectionBox() {
            return { active: false, startX: 0, startY: 0, endX: 0, endY: 0, additive: false };
        },
        clearSelectionBox() {
            if (!this.selectionBox || !this.selectionBox.active) return false;
            this.selectionBox = this.emptySelectionBox();
            return true;
        },
        clearCanvasPointerInteraction() {
            this.clearSelectionBox();
            this.dragState = null;
            if (this.canvasPan) {
                this.canvasPan.active = false;
                this.canvasPan.moved = false;
            }
        },
        onCanvasMouseDown(event) {
            if (event.button === 2) {
                this.startCanvasPan(event);
                return;
            }
            if (event.button !== 0) return;
            const target = event.target;
            if (target && typeof target.closest === 'function' &&
                target.closest('.workflow-node, button, .workflow-edge-insert-menu, .workflow-context-menu, .canvas-zoom-controls, .canvas-minimap')) return;
            this.startSelectionBox(event);
        },
        startSelectionBox(event) {
            if (event.button !== 0 || !this.$refs.canvas) return;
            const point = this.canvasPoint(event);
            this.closeCanvasOverlays();
            this.dragState = null;
            this.selectionBox = {
                active: true,
                startX: point.x,
                startY: point.y,
                endX: point.x,
                endY: point.y,
                additive: !!(event.metaKey || event.ctrlKey)
            };
            event.preventDefault();
        },
        nodeIntersectsSelection(node, selection) {
            const left = Math.min(selection.startX, selection.endX);
            const right = Math.max(selection.startX, selection.endX);
            const top = Math.min(selection.startY, selection.endY);
            const bottom = Math.max(selection.startY, selection.endY);
            const nodeLeft = Number(node.x || 0);
            const nodeTop = Number(node.y || 0);
            const nodeRight = nodeLeft + 220;
            const nodeBottom = nodeTop + 92;
            return nodeLeft < right && nodeRight > left && nodeTop < bottom && nodeBottom > top;
        },
        completeSelectionBox() {
            const selection = { ...(this.selectionBox || this.emptySelectionBox()) };
            this.clearSelectionBox();
            if (!selection.active) return;
            const intersected = this.form.graph.nodes.filter(node => this.nodeIntersectsSelection(node, selection));
            const existing = selection.additive
                ? this.form.graph.nodes.filter(node => (this.selectedNodeIds || []).includes(node.id))
                : [];
            const selected = [...existing, ...intersected.filter(node => !existing.some(item => item.id === node.id))];
            this.setSelectedNodes(selected, { openInspector: selected.length === 1 });
        },
        refreshCanvasViewport() {
            if (typeof this.$nextTick === 'function') this.$nextTick(() => this.updateCanvasViewport());
            else this.updateCanvasViewport();
        },
        refreshCanvasViewportAfterPaneTransition() {
            this.refreshCanvasViewport();
            if (typeof window !== 'undefined') {
                window.setTimeout(() => this.refreshCanvasViewport(), 240);
            }
        },
        updateCanvasViewport() {
            const canvas = this.$refs && this.$refs.canvas;
            if (!canvas) return;
            const rect = canvas.getBoundingClientRect();
            const palette = this.$refs && this.$refs.palette;
            const inspector = this.$refs && this.$refs.inspector;
            const paletteRect = palette && typeof palette.getBoundingClientRect === 'function'
                ? palette.getBoundingClientRect()
                : null;
            const inspectorRect = inspector && typeof inspector.getBoundingClientRect === 'function'
                ? inspector.getBoundingClientRect()
                : null;
            const canvasWidth = Math.max(0, Number(canvas.clientWidth || rect.width || 0));
            const overlayGap = 18;
            const leftOverlayWidth = paletteRect && Number(paletteRect.left || 0) <= Number(rect.left || 0) + 1
                ? Math.max(0, Math.min(canvasWidth, Number(paletteRect.right || 0) - Number(rect.left || 0)))
                : 0;
            const rightOverlayWidth = inspectorRect && Number(inspectorRect.right || 0) >= Number(rect.right || 0) - 1
                ? Math.max(0, Math.min(canvasWidth, Number(rect.right || 0) - Number(inspectorRect.left || 0)))
                : 0;
            const nextSafeInsets = {
                left: leftOverlayWidth ? leftOverlayWidth + overlayGap : 0,
                right: rightOverlayWidth ? rightOverlayWidth + overlayGap : 0
            };
            const nextViewport = {
                width: Number(canvas.clientWidth || 0),
                height: Number(canvas.clientHeight || 0),
                scrollLeft: Number(canvas.scrollLeft || 0),
                scrollTop: Number(canvas.scrollTop || 0),
                left: Number(rect.left || 0),
                right: Number(rect.right || 0),
                bottom: Number(rect.bottom || 0),
                windowWidth: typeof window === 'undefined' ? 0 : Number(window.innerWidth || 0),
                windowHeight: typeof window === 'undefined' ? 0 : Number(window.innerHeight || 0)
            };
            const hasChanged = (current, next) => Object.keys(next).some(key => !current || current[key] !== next[key]);
            if (hasChanged(this.canvasSafeInsets, nextSafeInsets)) this.canvasSafeInsets = nextSafeInsets;
            if (hasChanged(this.canvasViewport, nextViewport)) this.canvasViewport = nextViewport;
        },
        clampCanvasZoom(value) {
            return Math.round(Math.min(2, Math.max(.02, Number(value) || 1)) * 100) / 100;
        },
        stageContentTop(canvas, canvasRect) {
            const stage = this.$refs && this.$refs.stage;
            if (!canvas || !stage || typeof stage.getBoundingClientRect !== 'function') return 0;
            const stageRect = stage.getBoundingClientRect();
            const rect = canvasRect || canvas.getBoundingClientRect();
            return Math.max(0, Number(stageRect.top || 0) - Number(rect.top || 0) + Number(canvas.scrollTop || 0));
        },
        setCanvasZoom(value, clientX, clientY) {
            const nextZoom = this.clampCanvasZoom(value);
            const currentZoom = this.canvasZoom || 1;
            const canvas = this.$refs && this.$refs.canvas;
            if (!canvas || nextZoom === currentZoom) {
                this.canvasZoom = nextZoom;
                return;
            }

            const rect = canvas.getBoundingClientRect();
            const stageContentTop = this.stageContentTop(canvas, rect);
            const insets = this.canvasSafeInsets || {};
            const safeLeft = Math.max(0, Number(insets.left) || 0);
            const safeRight = Math.max(0, Number(insets.right) || 0);
            const usableViewportWidth = Math.max(1, Number(canvas.clientWidth || 0) - safeLeft - safeRight);
            const localX = Number.isFinite(clientX)
                ? Math.max(0, Math.min(canvas.clientWidth, clientX - rect.left))
                : safeLeft + usableViewportWidth / 2;
            const localY = Number.isFinite(clientY)
                ? Math.max(0, Math.min(canvas.clientHeight, clientY - rect.top))
                : (canvas.clientHeight + Math.min(canvas.clientHeight, stageContentTop)) / 2;
            const worldX = (canvas.scrollLeft + localX - safeLeft) / currentZoom;
            const worldY = (canvas.scrollTop - stageContentTop + localY) / currentZoom;
            const nextScrollLeft = Math.max(0, safeLeft + worldX * nextZoom - localX);
            const nextScrollTop = Math.max(0, stageContentTop + worldY * nextZoom - localY);
            this.canvasZoom = nextZoom;

            const applyScrollPosition = () => {
                canvas.scrollLeft = nextScrollLeft;
                canvas.scrollTop = nextScrollTop;
                this.updateCanvasViewport();
            };
            if (typeof this.$nextTick === 'function') this.$nextTick(applyScrollPosition);
            else applyScrollPosition();
        },
        changeCanvasZoom(delta) { this.setCanvasZoom((this.canvasZoom || 1) + delta); },
        resetCanvasZoom() { this.setCanvasZoom(1); },
        onCanvasZoomInput(event) { this.setCanvasZoom(event?.target?.value); },
        zoomCanvas(event) {
            if (!event || !Number.isFinite(event.deltaY) || event.deltaY === 0) return;
            this.setCanvasZoom((this.canvasZoom || 1) + (event.deltaY < 0 ? .1 : -.1), event.clientX, event.clientY);
        },
        canvasPoint(event) {
            const canvas = this.$refs.canvas;
            if (!canvas) return { x: 0, y: 0 };
            const stage = this.$refs.stage;
            const rect = stage ? stage.getBoundingClientRect() : canvas.getBoundingClientRect();
            return {
                x: (event.clientX - rect.left) / this.canvasZoom,
                y: (event.clientY - rect.top) / this.canvasZoom
            };
        },
        startCanvasPan(event) {
            if (event.button !== 2) return;
            const canvas = this.$refs.canvas;
            if (!canvas) return;
            this.closeContextMenu();
            this.closeCanvasContextMenu();
            this.closeCanvasNodeInsertMenu();
            this.closeEdgeInsertMenu();
            this.dragState = null;
            this.selectionBox = this.emptySelectionBox();
            this.cancelConnection();
            this.canvasPan = {
                active: true,
                moved: false,
                startX: event.clientX,
                startY: event.clientY,
                startScrollLeft: canvas.scrollLeft,
                startScrollTop: canvas.scrollTop
            };
            event.preventDefault();
        },
        startDrag(event, node) {
            if (this.editingLocked || event.button !== 0 || event.target.closest('button,.node-port')) return;
            const canvas = this.$refs.canvas;
            if (!canvas) return;
            this.selectionBox = this.emptySelectionBox();
            this.selectNode(node, event);
            if (event.metaKey || event.ctrlKey) return;
            const point = this.canvasPoint(event);
            const nodes = this.form.graph.nodes.filter(item => (this.selectedNodeIds || []).includes(item.id));
            const positions = nodes.map(item => ({ id: item.id, x: Number(item.x || 0), y: Number(item.y || 0) }));
            this.dragState = {
                node,
                nodes,
                positions,
                startX: point.x,
                startY: point.y,
                offsetX: point.x - Number(node.x),
                offsetY: point.y - Number(node.y),
                hoverEdgeId: ''
            };
            event.preventDefault();
        },
        onPointerMove(event) {
            if (this.canvasPan.active) {
                const canvas = this.$refs.canvas;
                if (canvas) {
                    if (Math.abs(event.clientX - this.canvasPan.startX) > 3 || Math.abs(event.clientY - this.canvasPan.startY) > 3) {
                        this.canvasPan.moved = true;
                    }
                    canvas.scrollLeft = Math.max(0, this.canvasPan.startScrollLeft - (event.clientX - this.canvasPan.startX));
                    canvas.scrollTop = Math.max(0, this.canvasPan.startScrollTop - (event.clientY - this.canvasPan.startY));
                }
            }
            if (this.selectionBox.active) {
                const point = this.canvasPoint(event);
                this.selectionBox.endX = point.x;
                this.selectionBox.endY = point.y;
            }
            if (this.dragState) {
                const point = this.canvasPoint(event);
                if (this.dragState.nodes.length > 1) {
                    const deltaX = point.x - this.dragState.startX;
                    const deltaY = point.y - this.dragState.startY;
                    this.dragState.positions.forEach(position => {
                        const node = this.dragState.nodes.find(item => item.id === position.id);
                        if (!node) return;
                        node.x = Math.max(20, position.x + deltaX);
                        node.y = Math.max(50, position.y + deltaY);
                    });
                    this.dragState.hoverEdgeId = '';
                } else {
                    this.dragState.node.x = Math.max(20, point.x - this.dragState.offsetX);
                    this.dragState.node.y = Math.max(50, point.y - this.dragState.offsetY);
                    const candidate = this.findEdgeAtPoint({
                        x: Number(this.dragState.node.x) + 110,
                        y: Number(this.dragState.node.y) + 46
                    }, this.dragState.node.id);
                    this.dragState.hoverEdgeId = candidate ? candidate.id : '';
                }
                this.updateCanvasSize();
            }
            if (this.connectionDraft.sourceId) {
                const point = this.canvasPoint(event);
                this.connectionDraft.x = point.x;
                this.connectionDraft.y = point.y;
            }
        },
        onPointerUp(event) {
            const panMoved = !!(this.canvasPan.active && this.canvasPan.moved);
            const target = event && event.target;
            const pointerOnFormulaControl = !!(target && target.closest && target.closest('.workflow-rich-text-input, .parameter-template-actions'));
            this.canvasPan.active = false;
            this.canvasPan.moved = false;
            if (panMoved) this.suppressCanvasContextMenuUntil = Date.now() + 600;
            if (this.selectionBox.active) {
                if ((this.templateEditor && this.templateEditor.visible) || pointerOnFormulaControl) this.clearSelectionBox();
                else this.completeSelectionBox();
            }
            const dragState = this.dragState;
            this.dragState = null;
            if (dragState?.hoverEdgeId && dragState.nodes.length === 1) {
                const edge = this.form.graph.edges.find(item => item.id === dragState.hoverEdgeId);
                if (edge) this.insertExistingNodeIntoEdge(dragState.node, edge);
            }
            if (dragState && typeof this.scheduleAutoSave === 'function') this.scheduleAutoSave();
            if (this.connectionDraft.sourceId) window.setTimeout(() => this.cancelConnection(), 0);
        },
        isHorizontalLayout() {
            return !!(this.form && this.form.graph && this.form.graph.layout &&
                this.form.graph.layout.direction === 'horizontal');
        },
        cubicPoint(start, end, t) {
            const horizontal = this.isHorizontalLayout();
            const bend = Math.max(45, horizontal ? Math.abs(end.x - start.x) / 2 : Math.abs(end.y - start.y) / 2);
            const control1 = horizontal
                ? { x: start.x + bend, y: start.y }
                : { x: start.x, y: start.y + bend };
            const control2 = horizontal
                ? { x: end.x - bend, y: end.y }
                : { x: end.x, y: end.y - bend };
            const inverse = 1 - t;
            return {
                x: inverse ** 3 * start.x + 3 * inverse ** 2 * t * control1.x + 3 * inverse * t ** 2 * control2.x + t ** 3 * end.x,
                y: inverse ** 3 * start.y + 3 * inverse ** 2 * t * control1.y + 3 * inverse * t ** 2 * control2.y + t ** 3 * end.y
            };
        },
        pointToSegmentDistance(point, start, end) {
            const deltaX = end.x - start.x;
            const deltaY = end.y - start.y;
            const denominator = deltaX * deltaX + deltaY * deltaY;
            if (!denominator) return Math.hypot(point.x - start.x, point.y - start.y);
            const ratio = Math.max(0, Math.min(1, ((point.x - start.x) * deltaX + (point.y - start.y) * deltaY) / denominator));
            return Math.hypot(point.x - (start.x + ratio * deltaX), point.y - (start.y + ratio * deltaY));
        },
        findEdgeAtPoint(point, ignoredNodeId) {
            if (!point || !this.form?.graph?.edges?.length) return null;
            let closestEdge = null;
            let closestDistance = 18;
            this.form.graph.edges.forEach(edge => {
                if (ignoredNodeId && (edge.source === ignoredNodeId || edge.target === ignoredNodeId)) return;
                const start = this.edgeStart(edge);
                const end = this.edgeEnd(edge);
                let previous = start;
                for (let step = 1; step <= 28; step++) {
                    const current = this.cubicPoint(start, end, step / 28);
                    const distance = this.pointToSegmentDistance(point, previous, current);
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestEdge = edge;
                    }
                    previous = current;
                }
            });
            return closestEdge;
        },
        edgeStart(edge) {
            const source = this.form.graph.nodes.find(node => node.id === edge.source);
            if (!source) return { x: 0, y: 0 };
            if (this.isHorizontalLayout()) {
                const offset = source.type === 'condition'
                    ? (edge.sourceHandle === 'false' ? 46 : edge.sourceHandle === 'break' ? 79 : 13)
                    : 46;
                return { x: Number(source.x) + 220, y: Number(source.y) + offset };
            }
            const offset = source.type === 'condition'
                ? (edge.sourceHandle === 'false' ? 110 : edge.sourceHandle === 'break' ? 177 : 43)
                : 110;
            return { x: Number(source.x) + offset, y: Number(source.y) + 92 };
        },
        edgeEnd(edge) {
            const target = this.form.graph.nodes.find(node => node.id === edge.target);
            if (target && target.type === 'loop-end') {
                if (this.isHorizontalLayout()) {
                    return { x: Number(target.x), y: Number(target.y) + (edge.targetHandle === 'break' ? 68 : 24) };
                }
                return { x: Number(target.x) + (edge.targetHandle === 'break' ? 149 : 71), y: Number(target.y) };
            }
            if (target && this.isHorizontalLayout()) return { x: Number(target.x), y: Number(target.y) + 46 };
            return target ? { x: Number(target.x) + 110, y: Number(target.y) } : { x: 0, y: 0 };
        },
        curvePath(start, end) {
            if (this.isHorizontalLayout()) {
                const bend = Math.max(45, Math.abs(end.x - start.x) / 2);
                return `M ${start.x} ${start.y} C ${start.x + bend} ${start.y}, ${end.x - bend} ${end.y}, ${end.x} ${end.y}`;
            }
            const bend = Math.max(45, Math.abs(end.y - start.y) / 2);
            return `M ${start.x} ${start.y} C ${start.x} ${start.y + bend}, ${end.x} ${end.y - bend}, ${end.x} ${end.y}`;
        },
        edgePath(edge) { return this.curvePath(this.edgeStart(edge), this.edgeEnd(edge)); },
        draftEdgePath() {
            if (!this.connectionDraft.sourceId) return '';
            return this.curvePath(this.edgeStart({ source: this.connectionDraft.sourceId, sourceHandle: this.connectionDraft.sourceHandle }), this.connectionDraft);
        },
        edgeActionPosition(edge, offsetX) {
            const start = this.edgeStart(edge); const end = this.edgeEnd(edge);
            return {
                left: `${(start.x + end.x) / 2 + offsetX}px`,
                top: `${(start.y + end.y) / 2 - 11}px`
            };
        },
        edgeDeletePosition(edge) { return this.edgeActionPosition(edge, -25); },
        edgeInsertPosition(edge) { return this.edgeActionPosition(edge, 3); },
        edgeLabelX(edge) { const start = this.edgeStart(edge); return start.x + (this.isHorizontalLayout() ? 12 : 5); },
        edgeLabelY(edge) { const start = this.edgeStart(edge); return start.y + (this.isHorizontalLayout() ? -8 : 20); },
        autoLayout(direction) {
            if (this.editingLocked || !this.form.graph.nodes.length) return;
            const graph = this.form.graph;
            graph.layout = graph.layout && typeof graph.layout === 'object' ? graph.layout : {};
            const layoutDirection = direction === 'horizontal' ||
                (direction !== 'vertical' && graph.layout.direction === 'horizontal')
                ? 'horizontal'
                : 'vertical';
            graph.layout.direction = layoutDirection;
            const nodes = graph.nodes;
            const nodeById = new Map(nodes.map(node => [String(node.id), node]));
            const nodeIndex = new Map(nodes.map((node, index) => [String(node.id), index]));
            const trigger = nodes.find(node => String(node.type).endsWith('trigger')) || nodes[0];
            const triggerId = String(trigger.id);
            const edges = (graph.edges || [])
                .map((edge, index) => ({
                    ...edge,
                    sourceId: String(edge.source),
                    targetId: String(edge.target),
                    index
                }))
                .filter(edge => nodeById.has(edge.sourceId) && nodeById.has(edge.targetId) &&
                    edge.sourceId !== edge.targetId);
            const outgoing = new Map(nodes.map(node => [String(node.id), []]));
            const incoming = new Map(nodes.map(node => [String(node.id), []]));
            edges.forEach(edge => {
                outgoing.get(edge.sourceId).push(edge);
                incoming.get(edge.targetId).push(edge);
            });
            const edgeOrder = edge => {
                const source = nodeById.get(edge.sourceId);
                if (source && source.type === 'condition') {
                    if (edge.sourceHandle === 'true') return 0;
                    if (edge.sourceHandle === 'false') return 1;
                    if (edge.sourceHandle === 'break') return 2;
                }
                return 0;
            };
            const compareEdges = (left, right) => edgeOrder(left) - edgeOrder(right) ||
                left.index - right.index ||
                (nodeIndex.get(left.targetId) || 0) - (nodeIndex.get(right.targetId) || 0);
            outgoing.forEach(list => list.sort(compareEdges));

            // Keep the trigger-reachable graph in reading order. Condition branches are
            // deliberately visited true -> false so their semantic order remains visible.
            const preferredOrder = new Map();
            const visited = new Set();
            let preferredIndex = 0;
            const visit = nodeId => {
                if (visited.has(nodeId)) return;
                visited.add(nodeId);
                preferredOrder.set(nodeId, preferredIndex++);
                outgoing.get(nodeId).forEach(edge => visit(edge.targetId));
            };
            visit(triggerId);
            nodes.forEach(node => visit(String(node.id)));

            const reachable = new Set();
            const reachabilityQueue = [triggerId];
            while (reachabilityQueue.length) {
                const current = reachabilityQueue.shift();
                if (reachable.has(current)) continue;
                reachable.add(current);
                outgoing.get(current).forEach(edge => {
                    if (!reachable.has(edge.targetId)) reachabilityQueue.push(edge.targetId);
                });
            }

            // Use longest-path levels for a DAG. BFS puts a merge at the first available
            // level, which creates long diagonal edges and needless crossings. Legacy malformed
            // cycles fall back to the old bounded BFS behavior so auto layout always terminates.
            const level = new Map([[triggerId, 0]]);
            const indegree = new Map([...reachable].map(nodeId => [nodeId, 0]));
            edges.forEach(edge => {
                if (reachable.has(edge.sourceId) && reachable.has(edge.targetId)) {
                    indegree.set(edge.targetId, indegree.get(edge.targetId) + 1);
                }
            });
            const topologicalQueue = [...reachable].filter(nodeId => indegree.get(nodeId) === 0)
                .sort((left, right) => preferredOrder.get(left) - preferredOrder.get(right));
            const topologicalOrder = [];
            while (topologicalQueue.length) {
                const current = topologicalQueue.shift();
                topologicalOrder.push(current);
                outgoing.get(current).forEach(edge => {
                    if (!reachable.has(edge.targetId)) return;
                    const nextIndegree = indegree.get(edge.targetId) - 1;
                    indegree.set(edge.targetId, nextIndegree);
                    if (nextIndegree === 0) topologicalQueue.push(edge.targetId);
                });
            }
            if (topologicalOrder.length === reachable.size) {
                topologicalOrder.forEach(nodeId => {
                    if (nodeId === triggerId) return;
                    const predecessorLevels = incoming.get(nodeId)
                        .filter(edge => level.has(edge.sourceId))
                        .map(edge => level.get(edge.sourceId) + 1);
                    level.set(nodeId, predecessorLevels.length ? Math.max(...predecessorLevels) : 0);
                });
            } else {
                const boundedQueue = [triggerId];
                while (boundedQueue.length) {
                    const current = boundedQueue.shift();
                    outgoing.get(current).forEach(edge => {
                        if (!level.has(edge.targetId)) {
                            level.set(edge.targetId, level.get(current) + 1);
                            boundedQueue.push(edge.targetId);
                        }
                    });
                }
            }

            let disconnectedLevel = Math.max(0, ...level.values()) + 1;
            nodes.forEach(node => {
                const nodeId = String(node.id);
                if (!level.has(nodeId)) level.set(nodeId, disconnectedLevel++);
            });

            const layers = [];
            [...new Set(level.values())].sort((left, right) => left - right).forEach(layerNumber => {
                const layer = nodes.filter(node => level.get(String(node.id)) === layerNumber)
                    .map(node => String(node.id))
                    .sort((left, right) => preferredOrder.get(left) - preferredOrder.get(right) ||
                        (nodeIndex.get(left) || 0) - (nodeIndex.get(right) || 0));
                layers.push(layer);
            });
            const positions = new Map();
            const refreshPositions = () => {
                layers.forEach(layer => layer.forEach((nodeId, index) => positions.set(nodeId, index)));
            };
            refreshPositions();

            // Long edges are treated as virtual nodes during ordering: interpolate their
            // position at each crossed layer. This gives a direct edge to a later merge a
            // routing position without actually adding anything to the saved graph.
            const edgePositionAtLayer = (edge, layerNumber) => {
                const sourceLayer = level.get(edge.sourceId);
                const targetLayer = level.get(edge.targetId);
                if (sourceLayer >= targetLayer || layerNumber < sourceLayer || layerNumber > targetLayer) return null;
                const sourcePosition = positions.get(edge.sourceId);
                const targetPosition = positions.get(edge.targetId);
                if (typeof sourcePosition !== 'number' || typeof targetPosition !== 'number') return null;
                const ratio = (layerNumber - sourceLayer) / Math.max(1, targetLayer - sourceLayer);
                return sourcePosition + (targetPosition - sourcePosition) * ratio;
            };
            const median = values => {
                if (!values.length) return null;
                const sorted = values.slice().sort((left, right) => left - right);
                const middle = Math.floor(sorted.length / 2);
                return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
            };
            const reorderLayer = (layerIndex, direction) => {
                const layer = layers[layerIndex];
                const ordered = layer.map((nodeId, stableIndex) => {
                    const referenceLayer = direction === 'down' ? layerIndex - 1 : layerIndex + 1;
                    const relatedEdges = direction === 'down' ? incoming.get(nodeId) : outgoing.get(nodeId);
                    const relatedPositions = relatedEdges
                        .map(edge => edgePositionAtLayer(edge, referenceLayer))
                        .filter(position => typeof position === 'number');
                    return {
                        nodeId,
                        stableIndex,
                        targetPosition: median(relatedPositions)
                    };
                });
                ordered.sort((left, right) => {
                    const leftPosition = left.targetPosition == null ? left.stableIndex : left.targetPosition;
                    const rightPosition = right.targetPosition == null ? right.stableIndex : right.targetPosition;
                    return leftPosition - rightPosition || left.stableIndex - right.stableIndex;
                });
                layers[layerIndex] = ordered.map(item => item.nodeId);
                refreshPositions();
            };

            // Barycenter sweeps reduce crossings between neighboring layers. A few passes are
            // enough for the small editor graphs and keep the operation responsive for large
            // imported workflows.
            for (let pass = 0; pass < 4; pass++) {
                for (let layerIndex = 1; layerIndex < layers.length; layerIndex++) {
                    reorderLayer(layerIndex, 'down');
                }
                for (let layerIndex = layers.length - 2; layerIndex > 0; layerIndex--) {
                    reorderLayer(layerIndex, 'up');
                }
            }

            const crossingScore = () => {
                let score = 0;
                for (let boundary = 0; boundary < layers.length - 1; boundary++) {
                    const activeEdges = edges.filter(edge => {
                        const sourceLayer = level.get(edge.sourceId);
                        const targetLayer = level.get(edge.targetId);
                        return sourceLayer <= boundary && targetLayer > boundary;
                    });
                    for (let leftIndex = 0; leftIndex < activeEdges.length; leftIndex++) {
                        const left = activeEdges[leftIndex];
                        const leftStart = edgePositionAtLayer(left, boundary);
                        const leftEnd = edgePositionAtLayer(left, boundary + 1);
                        if (leftStart == null || leftEnd == null) continue;
                        for (let rightIndex = leftIndex + 1; rightIndex < activeEdges.length; rightIndex++) {
                            const right = activeEdges[rightIndex];
                            const rightStart = edgePositionAtLayer(right, boundary);
                            const rightEnd = edgePositionAtLayer(right, boundary + 1);
                            if (rightStart == null || rightEnd == null) continue;
                            if ((leftStart - rightStart) * (leftEnd - rightEnd) < 0) score++;
                        }
                    }
                }
                return score;
            };

            // Resolve remaining local inversions by adjacent transposition. Unlike a global
            // sort this preserves the semantic branch order whenever swapping would not help.
            let optimizationBudget = Math.max(24, Math.min(180, nodes.length * 3));
            for (let layerIndex = 1; layerIndex < layers.length; layerIndex++) {
                if (optimizationBudget <= 0) break;
                let improved = true;
                let attempts = 0;
                while (improved && attempts++ < 3 && optimizationBudget > 0) {
                    improved = false;
                    for (let index = 0; index < layers[layerIndex].length - 1; index++) {
                        if (optimizationBudget-- <= 0) break;
                        const before = crossingScore();
                        [layers[layerIndex][index], layers[layerIndex][index + 1]] =
                            [layers[layerIndex][index + 1], layers[layerIndex][index]];
                        refreshPositions();
                        const after = crossingScore();
                        if (after < before) {
                            improved = true;
                        } else {
                            [layers[layerIndex][index], layers[layerIndex][index + 1]] =
                                [layers[layerIndex][index + 1], layers[layerIndex][index]];
                            refreshPositions();
                        }
                    }
                }
            }

            const crossSpacing = layoutDirection === 'horizontal' ? 142 : 270;
            const layerSpacing = layoutDirection === 'horizontal' ? 300 : 165;
            const maximumLayerSize = Math.max(1, ...layers.map(layer => layer.length));
            const crossCenter = layoutDirection === 'horizontal'
                ? Math.max(360, 60 + (maximumLayerSize - 1) * crossSpacing / 2)
                : Math.max(500, 80 + (maximumLayerSize - 1) * crossSpacing / 2);
            layers.forEach((layer, layerIndex) => {
                const totalCrossSize = (layer.length - 1) * crossSpacing;
                layer.forEach((nodeId, index) => {
                    const node = nodeById.get(nodeId);
                    if (layoutDirection === 'horizontal') {
                        node.x = 60 + layerIndex * layerSpacing;
                        node.y = Math.max(60, crossCenter - totalCrossSize / 2 + index * crossSpacing);
                    } else {
                        node.x = Math.max(40, crossCenter - totalCrossSize / 2 + index * crossSpacing);
                        node.y = 60 + layerIndex * layerSpacing;
                    }
                });
            });
            this.updateCanvasSize();
        },
        alignToNearbyGrid() {
            if (this.editingLocked || !this.form.graph.nodes.length) return;
            const graph = this.form.graph;
            const nodes = graph.nodes;
            const edges = graph.edges || [];
            const gridSize = 40;
            const nodeWidth = 220;
            const nodeHeight = 92;
            const nodeGap = 20;
            const maximumSearchRings = 6;
            const maximumPreferredEdgeLength = this.isHorizontalLayout() ? 440 : 360;
            const snap = value => Math.max(gridSize, Math.round((Number(value) || gridSize) / gridSize) * gridSize);
            const original = new Map(nodes.map(node => [node.id, { x: snap(node.x), y: snap(node.y) }]));
            const positions = new Map();
            const occupied = [];
            const orderedNodes = [...nodes].sort((left, right) => {
                const topDifference = (Number(left.y) || 0) - (Number(right.y) || 0);
                if (topDifference) return topDifference;
                const leftDifference = (Number(left.x) || 0) - (Number(right.x) || 0);
                return leftDifference || String(left.id).localeCompare(String(right.id));
            });
            const isFree = candidate => !occupied.some(position =>
                Math.abs(candidate.x - position.x) < nodeWidth + nodeGap &&
                Math.abs(candidate.y - position.y) < nodeHeight + nodeGap);
            const edgeLengthPenalty = (node, candidate) => edges.reduce((penalty, edge) => {
                const otherId = edge.source === node.id ? edge.target : edge.target === node.id ? edge.source : '';
                if (!otherId) return penalty;
                const other = positions.get(otherId) || original.get(otherId);
                if (!other) return penalty;
                const distance = Math.hypot(candidate.x - other.x, candidate.y - other.y);
                const excess = Math.max(0, distance - maximumPreferredEdgeLength);
                return penalty + excess * excess * 0.4;
            }, 0);

            orderedNodes.forEach(node => {
                const base = original.get(node.id);
                let best = null;
                let bestScore = Number.POSITIVE_INFINITY;
                for (let ring = 0; ring <= maximumSearchRings; ring++) {
                    for (let offsetX = -ring; offsetX <= ring; offsetX++) {
                        for (let offsetY = -ring; offsetY <= ring; offsetY++) {
                            if (ring && Math.abs(offsetX) !== ring && Math.abs(offsetY) !== ring) continue;
                            const candidate = {
                                x: Math.max(gridSize, base.x + offsetX * gridSize),
                                y: Math.max(gridSize, base.y + offsetY * gridSize)
                            };
                            if (!isFree(candidate)) continue;
                            const movement = Math.abs(candidate.x - base.x) + Math.abs(candidate.y - base.y);
                            const score = movement * 18 + edgeLengthPenalty(node, candidate);
                            if (score < bestScore) {
                                best = candidate;
                                bestScore = score;
                            }
                        }
                    }
                }
                if (!best) {
                    const rightmost = occupied.length
                        ? Math.max(...occupied.map(position => position.x + nodeWidth + nodeGap))
                        : base.x;
                    best = { x: snap(rightmost), y: base.y };
                }
                node.x = best.x;
                node.y = best.y;
                positions.set(node.id, best);
                occupied.push(best);
            });
            this.updateCanvasSize();
        },
        updateCanvasSize() {
            const maxX = Math.max(1100, ...this.form.graph.nodes.map(node => Number(node.x) + 320));
            const maxY = Math.max(680, ...this.form.graph.nodes.map(node => Number(node.y) + 210));
            this.canvasSize = { width: maxX, height: maxY };
            if (typeof this.refreshCanvasViewport === 'function') this.refreshCanvasViewport();
        },
        fitCanvasToNodes() {
            const canvas = this.$refs && this.$refs.canvas;
            const nodes = this.form.graph.nodes || [];
            if (!canvas || !nodes.length) return false;

            const nodeWidth = 220;
            const nodeHeight = 92;
            const padding = 54;
            const positions = nodes.map(node => ({
                x: Number.isFinite(Number(node.x)) ? Number(node.x) : 0,
                y: Number.isFinite(Number(node.y)) ? Number(node.y) : 0
            }));
            const left = Math.min(...positions.map(position => position.x)) - padding;
            const top = Math.min(...positions.map(position => position.y)) - padding;
            const right = Math.max(...positions.map(position => position.x + nodeWidth)) + padding;
            const bottom = Math.max(...positions.map(position => position.y + nodeHeight)) + padding;
            const stageContentTop = this.stageContentTop ? this.stageContentTop(canvas) : 0;
            const insets = this.canvasSafeInsets || {};
            const safeLeft = Math.max(0, Number(insets.left) || 0);
            const safeRight = Math.max(0, Number(insets.right) || 0);
            const viewportWidth = Math.max(1, Number(canvas.clientWidth || 0) - safeLeft - safeRight);
            const viewportHeight = Math.max(0, Number(canvas.clientHeight || 0) - stageContentTop);
            if (viewportWidth <= 0 || viewportHeight <= 0) return false;

            const requestedZoom = Math.min(
                viewportWidth / Math.max(1, right - left),
                viewportHeight / Math.max(1, bottom - top));
            // Never round a fit value up: doing so can clip an edge node by a few pixels.
            const nextZoom = this.clampCanvasZoom(Math.floor(requestedZoom * 100) / 100);
            this.canvasZoom = nextZoom;
            const centerX = (left + right) / 2;
            const centerY = (top + bottom) / 2;
            const applyScrollPosition = () => {
                canvas.scrollLeft = Math.max(0, centerX * nextZoom - viewportWidth / 2);
                canvas.scrollTop = Math.max(0,
                    stageContentTop + centerY * nextZoom - (Number(canvas.clientHeight || 0) + stageContentTop) / 2);
                this.updateCanvasViewport();
            };
            if (typeof this.$nextTick === 'function') this.$nextTick(applyScrollPosition);
            else applyScrollPosition();
            return true;
        },
        minimapNodeStyle(node) {
            const metrics = this.minimapMetrics;
            return {
                left: `${Math.max(0, Number(node.x || 0) * metrics.scale)}px`,
                top: `${Math.max(0, Number(node.y || 0) * metrics.scale)}px`,
                width: `${Math.max(3, 220 * metrics.scale)}px`,
                height: `${Math.max(3, 92 * metrics.scale)}px`
            };
        },
        minimapEdgeStart(edge) {
            const node = this.form.graph.nodes.find(item => item.id === edge.source);
            const metrics = this.minimapMetrics;
            return { x: (Number(node?.x || 0) + 110) * metrics.scale, y: (Number(node?.y || 0) + 92) * metrics.scale };
        },
        minimapEdgeEnd(edge) {
            const node = this.form.graph.nodes.find(item => item.id === edge.target);
            const metrics = this.minimapMetrics;
            return { x: (Number(node?.x || 0) + 110) * metrics.scale, y: Number(node?.y || 0) * metrics.scale };
        },
        moveCanvasFromMinimap(event) {
            const canvas = this.$refs && this.$refs.canvas;
            const surface = event && event.currentTarget;
            if (!canvas || !surface) return;
            const metrics = this.minimapMetrics;
            const rect = surface.getBoundingClientRect();
            const worldX = Math.max(0, Math.min(metrics.width, event.clientX - rect.left)) / metrics.scale;
            const worldY = Math.max(0, Math.min(metrics.height, event.clientY - rect.top)) / metrics.scale;
            const stageContentTop = this.stageContentTop(canvas);
            const insets = this.canvasSafeInsets || {};
            const safeLeft = Math.max(0, Number(insets.left) || 0);
            const safeRight = Math.max(0, Number(insets.right) || 0);
            const usableViewportWidth = Math.max(1, Number(canvas.clientWidth || 0) - safeLeft - safeRight);
            canvas.scrollLeft = Math.max(0, worldX * this.canvasZoom - usableViewportWidth / 2);
            canvas.scrollTop = Math.max(0, stageContentTop + worldY * this.canvasZoom - (canvas.clientHeight + stageContentTop) / 2);
            this.updateCanvasViewport();
        },
        nodeSummary(node) {
            if (node.type === 'function') { const fn = this.findFunction(node.config); return fn ? fn.moduleName : ncfWorkflowText('Frontend.495'); }
            if (node.type === 'interval-trigger') return `${ncfWorkflowText('Frontend.101').replace('{0}', this.form.intervalSeconds)}`;
            if (node.type === 'webhook-trigger') return ncfST('Frontend.649');
            if (node.type === 'manual-trigger') return ncfST('Frontend.650');
            if (node.type === 'chat-trigger') return ncfST('Frontend.651');
            if (node.type === 'delay') return ncfWorkflowText('Frontend.101').replace('{0}', node.config.seconds || 0);
            if (node.type === 'loop') return this.isTemplateValue(node.config?.count)
                ? ncfWorkflowText('Frontend.720')
                : this.isBinding(node.config?.count)
                ? ncfWorkflowText('Frontend.721')
                : ncfWorkflowText('Frontend.722').replace('{0}', node.config?.count || 3);
            if (node.type === 'loop-end') return ncfWorkflowText('Frontend.611');
            if (node.type === 'sub-workflow') {
                const workflow = (this.workflows || []).find(item => Number(item.id) === Number(node.config?.workflowId || 0));
                return workflow ? ncfWorkflowText('Frontend.710').replace('{0}', workflow.name) : ncfWorkflowText('Frontend.711');
            }
            if (node.type === 'code') return ncfWorkflowText('Frontend.712').replace('{0}', Array.isArray(node.config?.assignments) ? node.config.assignments.length : 0);
            if (node.type === 'condition') return `${node.config.operator || 'equals'} ${this.configValueLabel(node.config.right)}`;
            if (node.type === 'aggregate') return node.config.outputTemplate ? ncfWorkflowText('Frontend.713') : ncfWorkflowText('Frontend.714');
            if (node.type === 'merge') return ncfWorkflowText('Frontend.615');
            if (node.type === 'parallel') return ncfWorkflowText('Frontend.616');
            if (node.type === 'console') return node.config?.printTemplate ? ncfWorkflowText('Frontend.452') : ncfWorkflowText('Frontend.453');
            if (node.type === 'neubell') {
                const mode = String(node.config?.consumeMode || 'none');
                return mode === 'provider' ? ncfWorkflowText('Frontend.715')
                    : mode === 'item' ? ncfWorkflowText('Frontend.716') : ncfWorkflowText('Frontend.717');
            }
            if (node.type === 'human-input') return node.config?.externalResumeEnabled ? ncfWorkflowText('Frontend.718') : ncfWorkflowText('Frontend.428');
            if (node.type === 'end') return ncfWorkflowText('Frontend.454');
            return node.type === 'agent-group' ? ncfWorkflowText('Frontend.117') : node.type === 'agent' ? ncfWorkflowText('Frontend.449') : node.type === 'a2a' ? ncfWorkflowText('Frontend.448') : node.type;
        },
        nodeValidationError(node) {
            if (!node || !this.validation || !Array.isArray(this.validation.nodeIds)) return '';
            return this.validation.nodeIds.includes(node.id) ? String(this.validation.message || '') : '';
        },
        extractValidationNodeIds(message) {
            const text = String(message || '');
            const nodes = this.form && this.form.graph && Array.isArray(this.form.graph.nodes)
                ? this.form.graph.nodes
                : [];
            if (!text || !nodes.length) return [];
            const labels = [];
            const pattern = /节点[“"']([^”"']+)[”"']/g;
            let match;
            while ((match = pattern.exec(text)) !== null) labels.push(match[1]);
            return [...new Set(labels
                .map(label => nodes.find(node => String(node.id) === label || String(node.name || '') === label)?.id)
                .filter(Boolean))];
        },
        inferValidationNodeIds(message) {
            const directMatches = this.extractValidationNodeIds(message);
            if (directMatches.length) return directMatches;
            const nodes = this.form && this.form.graph && Array.isArray(this.form.graph.nodes)
                ? this.form.graph.nodes
                : [];
            if (/未连接到触发器/.test(String(message || '')) && typeof this.getDisconnectedNodes === 'function') {
                return this.getDisconnectedNodes().map(node => node.id);
            }
            if (/触发器节点类型无效/.test(String(message || ''))) {
                return nodes.filter(node => String(node.type || '').endsWith('trigger')).map(node => node.id);
            }
            if (/必须且只能包含一个触发器/.test(String(message || ''))) {
                return nodes.filter(node => String(node.type || '').endsWith('trigger')).map(node => node.id);
            }
            return [];
        },
        validationIssueFromError(error, fallback) {
            const responseData = error && error.response ? error.response.data : error;
            let message = '';
            let nodeIds = [];
            if (typeof responseData === 'string') {
                message = responseData;
            } else if (responseData && typeof responseData === 'object') {
                message = responseData.message || responseData.detail || responseData.title || responseData.error || '';
                const validation = responseData.validation || responseData.errors;
                const rawNodeIds = responseData.nodeIds || responseData.nodeId ||
                    (validation && (validation.nodeIds || validation.nodeId));
                nodeIds = Array.isArray(rawNodeIds) ? rawNodeIds : (rawNodeIds ? [rawNodeIds] : []);
                if (!message && validation && typeof validation === 'object' && !Array.isArray(validation)) {
                    message = Object.values(validation).flat().join('；');
                }
            }
            if (!message && typeof error === 'string') message = error;
            message = String(message || fallback || '节点检查失败。');
            nodeIds = [...new Set(nodeIds.map(nodeId => String(nodeId)).filter(Boolean))];
            return {
                message,
                nodeIds: [...new Set([...nodeIds, ...this.inferValidationNodeIds(message)])]
            };
        },
        showValidationIssue(error, fallback, options) {
            options = options || {};
            const issue = error && typeof error === 'object' && typeof error.message === 'string' && Array.isArray(error.nodeIds)
                ? error
                : this.validationIssueFromError(error, fallback);
            const nodeIds = options.nodeIds || issue.nodeIds || [];
            this.validation = {
                message: issue.message,
                nodeIds,
                source: options.source || ''
            };
            if (options.focus !== false && nodeIds.length) {
                this.focusValidationIssue(nodeIds[0]);
            }
            return { ...issue, nodeIds };
        },
        clearValidationIssue() {
            this.validation = { message: '', nodeIds: [], source: '' };
        },
        focusValidationIssue(issue) {
            const nodeId = typeof issue === 'string' ? issue : issue && issue.nodeId;
            const node = this.form.graph.nodes.find(item => item.id === nodeId);
            if (!node) return false;
            this.setSelectedNodes([node]);
            const focus = () => {
                const canvas = this.$refs && this.$refs.canvas;
                if (!canvas) return;
                const insets = this.canvasSafeInsets || {};
                const safeLeft = Math.max(0, Number(insets.left) || 0);
                const safeRight = Math.max(0, Number(insets.right) || 0);
                const stageContentTop = this.stageContentTop ? this.stageContentTop(canvas) : 0;
                const usableWidth = Math.max(1, Number(canvas.clientWidth || 0) - safeLeft - safeRight);
                const nodeCenterX = (Number(node.x || 0) + 110) * this.canvasZoom;
                const nodeCenterY = (Number(node.y || 0) + 46) * this.canvasZoom;
                canvas.scrollLeft = Math.max(0, safeLeft + nodeCenterX - usableWidth / 2);
                canvas.scrollTop = Math.max(0,
                    stageContentTop + nodeCenterY - (Number(canvas.clientHeight || 0) + stageContentTop) / 2);
                this.updateCanvasViewport();
            };
            if (typeof this.$nextTick === 'function') this.$nextTick(focus);
            else focus();
            return true;
        },
        nodeState(node) { return this.run.nodeStates[node.id] || ''; },
        functionParameters(fn) { return NeuCharWorkflowUi.normalizeParameterSchema(NeuCharWorkflowUi.parseJson(fn.parameterSchemaJson, [])); },
        parameterDisplayName(parameter, index) {
            const title = String(parameter?.title || '').trim();
            const name = String(parameter?.name || '').trim();
            return title || name || `参数 ${Number(index || 0) + 1}`;
        },
        hasParameterFieldName(parameter) {
            const title = String(parameter?.title || '').trim();
            const name = String(parameter?.name || '').trim();
            return !!title && !!name && title !== name;
        },
        parameterDescription(parameter) { return String(parameter?.description || '').trim(); },
        findFunction(config) {
            return this.functions.find(fn => (Number(config.functionId || 0) > 0 && Number(fn.id) === Number(config.functionId)) ||
                (String(fn.moduleUid).toLowerCase() === String(config.moduleUid || '').toLowerCase() &&
                 String(fn.functionKey).toLowerCase() === String(config.functionKey || '').toLowerCase()));
        },
        workflowObjectEditUrl(object) {
            if (!object) return '';
            const declaredUrl = String(object.editUrl || '');
            if (declaredUrl.startsWith('/') && !declaredUrl.startsWith('//')) return declaredUrl;
            if (String(object.providerId || '').toLowerCase() === 'agents-manager') {
                const objectId = String(object.objectId || '');
                if (objectId.startsWith('agent:')) return `/Admin/AgentsManager/Index#tab=first&view=edit&agentId=${encodeURIComponent(objectId.substring(6))}`;
                if (objectId.startsWith('group:')) return `/Admin/AgentsManager/Index#tab=second&view=edit&groupId=${encodeURIComponent(objectId.substring(6))}`;
                if (objectId.startsWith('a2a:')) return `/Admin/AgentsManager/Index#tab=remoteA2A&view=edit&remoteAgentId=${encodeURIComponent(objectId.substring(4))}`;
            }
            return '';
        },
        workflowObjectInfo(object) {
            if (!object) return [];
            const metadata = object.metadata || {};
            const rows = [
                { label: '类型', value: metadata.type || (object.kind === 'a2a' ? '远程 A2A Agent' : object.kind === 'agent-group' ? 'Agent 组' : '独立 Agent') },
                { label: '状态', value: object.enabled ? '可用 / 已启用' : '不可用 / 已停用' },
                { label: '说明', value: object.description || '' }
            ];
            if (metadata.promptCode) rows.push({ label: 'Prompt Code', value: metadata.promptCode });
            if (metadata.functionCallNames) rows.push({ label: 'Function Calls', value: metadata.functionCallNames });
            if (metadata.knowledgeBaseId) rows.push({ label: '知识库 ID', value: metadata.knowledgeBaseId });
            if (metadata.state) rows.push({ label: '组状态', value: metadata.state });
            return rows;
        },
        workflowModelLabel(model) {
            if (!model) return '';
            const alias = String(model.alias || model.modelId || `AIModel #${model.id}`);
            const deployment = String(model.deploymentName || model.modelId || '');
            return deployment && deployment !== alias ? `${alias} (${deployment})` : alias;
        },
        workflowModelPlatformLabel(model) {
            const platforms = {
                1: 'OpenAI',
                2: 'Azure OpenAI',
                3: 'Hugging Face',
                4: 'NeuCharAI'
            };
            return platforms[Number(model?.aiPlatform)] || `${ncfWorkflowText('平台')} ${model?.aiPlatform ?? ncfWorkflowText('未知')}`;
        },
        openWorkflowObjectEditor(object) {
            const url = this.workflowObjectEditUrl(object);
            if (!url) return;
            const editor = window.open(url, '_blank', 'noopener,noreferrer');
            if (editor) editor.opener = null;
        },
        functionAnchorId(fn) {
            const functionKey = String(fn?.functionKey || fn?.name || '').trim();
            return functionKey ? `function-${encodeURIComponent(functionKey)}` : '';
        },
        functionPageUrl(fn, action) {
            const moduleUid = String(fn?.moduleUid || '').trim();
            const functionKey = String(fn?.functionKey || '').trim();
            if (!moduleUid || !functionKey) return '';
            const pageAction = action === 'run' ? 'run' : 'settings';
            const anchor = `function-${encodeURIComponent(functionKey)}`;
            return `/Admin/XncfModule/Start/?uid=${encodeURIComponent(moduleUid)}&functionKey=${encodeURIComponent(functionKey)}&action=${pageAction}#${anchor}`;
        },
        openFunctionPage(fn, action) {
            const url = this.functionPageUrl(fn, action);
            if (!url) return;
            const page = window.open(url, '_blank', 'noopener,noreferrer');
            if (page) page.opener = null;
        },
        functionIdentity(fn) { return `${String(fn.moduleUid).toLowerCase()}|${String(fn.functionKey).toLowerCase()}`; },
        loadPinnedFunctions() {
            try { this.pinnedFunctions = JSON.parse(localStorage.getItem('ncf.neucharworkflow.pins') || '[]'); }
            catch { this.pinnedFunctions = []; }
        },
        isPinned(fn) { return this.pinnedFunctions.includes(this.functionIdentity(fn)); },
        togglePin(fn) {
            const key = this.functionIdentity(fn);
            this.pinnedFunctions = this.isPinned(fn)
                ? this.pinnedFunctions.filter(item => item !== key)
                : [...this.pinnedFunctions, key];
            localStorage.setItem('ncf.neucharworkflow.pins', JSON.stringify(this.pinnedFunctions));
        },
        upstreamNodes(targetNode) {
            if (!targetNode) return [];
            const ids = new Set(); const queue = [targetNode.id];
            while (queue.length) {
                const current = queue.shift();
                this.form.graph.edges.filter(edge => edge.target === current).forEach(edge => {
                    if (!ids.has(edge.source)) { ids.add(edge.source); queue.push(edge.source); }
                });
            }
            return this.form.graph.nodes.filter(node => ids.has(node.id));
        },
        observedOutputIdentity(node) {
            const config = node?.config || {};
            return [node?.type || '', config.moduleUid || '', config.functionKey || '',
                config.providerId || '', config.objectId || ''].join('|');
        },
        observedOutputFields(node) {
            const identity = this.observedOutputIdentity(node);
            const schema = (this.observedOutputSchemas || []).find(item =>
                item && item.nodeId === node?.id && item.identity === identity);
            if (!schema || !Array.isArray(schema.fields)) return [];
            return schema.fields.map(field => ({
                ...field,
                sourceKind: 'observed-output',
                observed: true
            }));
        },
        withObservedOutputFields(node, fields) {
            const knownPaths = new Set(fields.map(field => field.path));
            return [...fields, ...this.observedOutputFields(node).filter(field => !knownPaths.has(field.path))];
        },
        nodeOutputFields(node, visited) {
            visited = visited || new Set();
            if (!node || visited.has(node.id)) return [{ path: '$', label: '节点输出', typeName: 'any', isArray: false, requiresIndex: false }];
            visited.add(node.id);
            if (node.type === 'function') {
                const fn = this.findFunction(node.config);
                const outputFields = (fn && fn.output && Array.isArray(fn.output.fields))
                    ? fn.output.fields
                    : [{ path: '$', label: '完整输出', typeName: 'any', isArray: false, requiresIndex: false }];
                return this.withObservedOutputFields(node, [...outputFields, ...this.functionSelectionInputFields(fn)]);
            }
            if (node.type === 'aggregate') {
                const rawArray = String(node.config?.outputTemplate || '').trim() === '{{input}}';
                return this.withObservedOutputFields(node,
                    [{ path: '$', label: rawArray ? '聚合数组' : '聚合输出', typeName: 'any', isArray: rawArray, requiresIndex: false }]);
            }
            if (node.type === 'merge') return this.withObservedOutputFields(node,
                [{ path: '$', label: '当前输入项', typeName: 'any', isArray: false, requiresIndex: false }]);
            const observed = this.observedOutputFields(node);
            if (observed.length) return observed;
            if (node.type === 'webhook-trigger') {
                const parameters = this.form.webhookParameters || [];
                return parameters.length
                    ? parameters.filter(parameter => String(parameter.name || '').trim()).map(parameter => ({ path: `$.${String(parameter.name).trim()}`, label: parameter.name, typeName: 'any', isArray: false, requiresIndex: false }))
                    : [{ path: '$', label: 'Webhook 输入', typeName: 'object', isArray: false, requiresIndex: false }];
            }
            if (['manual-trigger', 'interval-trigger', 'webhook-trigger', 'chat-trigger', 'agent', 'agent-group', 'a2a', 'sub-workflow', 'human-input'].includes(node.type)) return [{ path: '$', label: '文本输出', typeName: 'string', isArray: false, requiresIndex: false }];
            const incoming = this.form.graph.edges.find(edge => edge.target === node.id);
            const source = incoming && this.form.graph.nodes.find(item => item.id === incoming.source);
            return source ? this.nodeOutputFields(source, visited) : [{ path: '$', label: '节点输出', typeName: 'any', isArray: false, requiresIndex: false }];
        },
        functionSelectionInputFields(fn) {
            if (!fn) return [];
            return this.functionParameters(fn)
                .filter(parameter => [1, 2].includes(Number(parameter.parameterType)) &&
                    String(parameter.name || '').trim() && !parameter.hasSyntheticName)
                .map(parameter => {
                    const isMultiple = Number(parameter.parameterType) === 2;
                    const options = Array.isArray(parameter.options) ? parameter.options : [];
                    const optionNames = options
                        .map(option => String(option.text || option.value || '').trim())
                        .filter(Boolean)
                        .slice(0, 3);
                    const optionSuffix = optionNames.length
                        ? `：${optionNames.join('、')}${options.length > optionNames.length ? '…' : ''}`
                        : '；选项元数据暂不可用';
                    const shape = this.expectedShape(parameter);
                    return {
                        path: `$.__functionInput.${parameter.name}`,
                        label: `预载输入选择 · ${this.parameterDisplayName(parameter)}（${isMultiple ? '多选' : '单选'}${optionSuffix}）`,
                        typeName: shape.typeName,
                        isArray: shape.isArray,
                        requiresIndex: false,
                        sourceKind: 'function-selection',
                        sourceParameterName: parameter.name
                    };
                });
        },
        upstreamOutputOptions(parameter) {
            return this.upstreamNodes(this.selectedNode).map(node => ({
                value: node.id,
                label: node.name,
                children: this.nodeOutputFields(node).map(field => ({
                    value: field.path,
                    label: `${field.label} · ${field.typeName}${field.isArray ? '[]' : ''}${field.observed ? ' · 运行观察' : ''}`
                }))
            })).filter(option => option.children.length);
        },
        isBinding(value) { return !!(value && typeof value === 'object' && !Array.isArray(value) && value.$source); },
        isTemplateValue(value) {
            return !!(value && typeof value === 'object' && !Array.isArray(value) &&
                value.$template && typeof value.$template === 'object');
        },
        templateFor(value) {
            return this.isTemplateValue(value) ? value.$template : null;
        },
        canUseTemplate(parameter) {
            return Number(parameter?.parameterType) === 0 && !this.expectedShape(parameter).isArray;
        },
        formulaValueText(value) {
            const template = this.templateFor(value);
            if (template) return String(template.text || '');
            if (value == null) return '';
            if (typeof value === 'string') return value;
            if (typeof value === 'object') {
                try { return JSON.stringify(value); } catch { return String(value); }
            }
            return String(value);
        },
        runtimeTextLength(value) {
            return this.formulaValueText(value).length;
        },
        isPureFormulaExpression(text) {
            const trimmed = String(text || '').trim();
            return /^\{\{=[\s\S]*\}\}$/.test(trimmed);
        },
        inferredFormulaType(text) {
            if (!this.isPureFormulaExpression(text)) return 'string';
            const expression = String(text).trim().slice(3, -2).trim();
            if (/^(toNumber|toInt|toLong|toDecimal)\s*\(/i.test(expression) ||
                /^[+-]?(?:\d+(?:\.\d+)?|\.\d+)$/.test(expression)) return 'number';
            if (/^toBool\s*\(/i.test(expression) || /^(true|false)$/i.test(expression)) return 'boolean';
            if (/^toString\s*\(/i.test(expression) || /^(['"]).*\1$/s.test(expression)) return 'string';
            return 'any';
        },
        formulaParameterCompatibility(parameter, value) {
            const expected = this.expectedShape(parameter);
            if (expected.isArray || ['any', 'object', 'string'].includes(expected.typeName)) return null;
            const text = this.formulaValueText(value);
            if (!text.includes('{{=')) return null;
            if (!this.isPureFormulaExpression(text)) {
                return {
                    level: 'danger',
                    text: `目标参数需要 ${expected.typeName}；类型转换公式必须独占整个输入，例如 {{= ${expected.typeName === 'boolean' ? 'toBool' : 'toInt'}(value_1) }}`
                };
            }
            const actual = this.inferredFormulaType(text);
            if (actual !== 'any' && actual !== expected.typeName) {
                return { level: 'warning', text: `纯公式结果为 ${actual}，但目标参数需要 ${expected.typeName}` };
            }
            return {
                level: 'success',
                text: actual === 'any'
                    ? `纯公式模式：运行时结果会保留类型；目标参数需要 ${expected.typeName}`
                    : `纯公式结果：${actual} → 目标参数 ${expected.typeName}`
            };
        },
        functionParameterFormulaHelp(parameter) {
            const expected = this.expectedShape(parameter);
            const basic = '可使用 {{input}} 引用当前输入；也可以打开编辑器插入上游输出。';
            if (expected.isArray || ['any', 'object', 'string'].includes(expected.typeName)) return `${basic} 组合前后文本时结果为字符串。`;
            const converter = expected.typeName === 'boolean' ? 'toBool' : 'toInt';
            return `${basic} 目标参数需要 ${expected.typeName}：只填写完整公式（例如 {{= ${converter}(value_1) }}）会保留其类型；加入前后文本后结果会变成字符串。`;
        },
        createSourceBinding(selection) {
            if (!selection || selection.length < 2) return null;
            const source = this.form.graph.nodes.find(item => item.id === selection[0]);
            const field = source && this.nodeOutputFields(source).find(item => item.path === selection[1]);
            if (!source || !field) return null;
            return {
                nodeId: source.id,
                path: field.path,
                sourceType: field.typeName,
                isArray: !!field.isArray,
                requiresIndex: !!field.requiresIndex,
                sourceKind: field.sourceKind || 'output',
                sourceParameterName: field.sourceParameterName || null,
                collectionIndex: null,
                itemIndex: null
            };
        },
        templateBindingLabel(binding) {
            const source = this.form.graph.nodes.find(node => node.id === binding?.nodeId);
            const field = source && this.nodeOutputFields(source).find(item => item.path === (binding.path || '$'));
            return source && field ? `${source.name} · ${field.label}` : '已失效的上游来源';
        },
        templatePlaceholder(token) { return `{{${String(token || '')}}}`; },
        templateUsesBindingToken(text, token) {
            const value = String(text || '');
            if (value.includes(this.templatePlaceholder(token))) return true;
            const escapedToken = String(token || '').replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            if (!escapedToken) return false;
            const tokenPattern = new RegExp(`(^|[^A-Za-z0-9_])${escapedToken}(?=$|[^A-Za-z0-9_])`, 'i');
            let position = 0;
            while (true) {
                const start = value.indexOf('{{=', position);
                if (start < 0) return false;
                const end = value.indexOf('}}', start + 3);
                if (end < 0) return false;
                if (tokenPattern.test(value.substring(start + 3, end))) return true;
                position = end + 2;
            }
        },
        openTemplateEditorForValue(node, currentValue, options = {}) {
            if (this.editingLocked || !node) return;
            if (this.isBinding(currentValue)) return;
            this.clearCanvasPointerInteraction();
            const template = this.templateFor(currentValue);
            this.templateEditor = {
                visible: true,
                nodeId: node.id,
                configKey: options.configKey || '',
                parameterName: options.parameterName || '',
                targetType: options.targetType || 'node',
                assignmentIndex: Number.isInteger(options.assignmentIndex) ? options.assignmentIndex : -1,
                fieldLabel: options.fieldLabel || options.configKey || '',
                allowBindings: options.allowBindings !== false,
                text: template ? String(template.text || '') : (typeof currentValue === 'string' ? currentValue : ''),
                bindings: template && Array.isArray(template.bindings)
                    ? template.bindings.filter(item => item && item.source).map(item => ({
                        token: String(item.token || ''),
                        source: { ...item.source }
                    }))
                    : [],
                pendingSelection: [],
                selectionStart: 0,
                selectionEnd: 0
            };
        },
        openNodeTemplateEditor(node, configKey, options = {}) {
            if (!node || !configKey) return;
            this.openTemplateEditorForValue(node, node.config?.[configKey], {
                ...options,
                configKey,
                targetType: 'node'
            });
        },
        openParameterTemplateEditor(node, parameter) {
            if (this.editingLocked || !node || !parameter || !this.canUseTemplate(parameter)) return;
            const currentValue = node.config.parameters?.[parameter.name];
            this.openTemplateEditorForValue(node, currentValue, {
                configKey: 'parameters',
                parameterName: parameter.name,
                fieldLabel: `${this.parameterDisplayName(parameter)}文本`,
                allowBindings: true,
                targetType: 'parameter'
            });
        },
        openCodeAssignmentTemplateEditor(node, assignmentIndex) {
            if (this.editingLocked || !node || !Array.isArray(node.config?.assignments)) return;
            const assignment = node.config.assignments[assignmentIndex];
            if (!assignment) return;
            this.openTemplateEditorForValue(node, assignment.value, {
                configKey: 'assignments',
                assignmentIndex,
                fieldLabel: `变量赋值 ${assignment.name || ''}`.trim(),
                allowBindings: false,
                targetType: 'code-assignment'
            });
        },
        normalizeTemplateBindings(text, bindings) {
            const kept = [];
            const tokens = new Set();
            const value = String(text || '');
            (Array.isArray(bindings) ? bindings : []).forEach(item => {
                const token = String(item?.token || '').trim();
                if (!item?.source || !/^[A-Za-z][A-Za-z0-9_-]{0,63}$/.test(token) || tokens.has(token.toLowerCase())) return;
                if (!this.templateUsesBindingToken(value, token)) return;
                tokens.add(token.toLowerCase());
                kept.push({ token, source: { ...item.source } });
            });
            return kept;
        },
        appendTemplateBinding(selection) {
            const source = this.createSourceBinding(selection);
            this.templateEditor.pendingSelection = [];
            if (!source) return;
            let index = this.templateEditor.bindings.length + 1;
            let token = `value_${index}`;
            while (this.templateEditor.bindings.some(item => item.token === token)) {
                index += 1;
                token = `value_${index}`;
            }
            const placeholder = `{{${token}}}`;
            const separator = this.templateEditor.text && !/\s$/.test(this.templateEditor.text) ? ' ' : '';
            this.templateEditor.text += `${separator}${placeholder}`;
            this.templateEditor.bindings.push({ token, source });
        },
        templateEditorInputElement() {
            const input = this.$refs && this.$refs.templateEditorInput;
            if (!input) return null;
            if (input.$refs && (input.$refs.textarea || input.$refs.input)) return input.$refs.textarea || input.$refs.input;
            if (input.$el && input.$el.querySelector) return input.$el.querySelector('textarea,input');
            return null;
        },
        captureTemplateSelection() {
            const input = this.templateEditorInputElement();
            if (!input || typeof input.selectionStart !== 'number') return;
            this.templateEditor.selectionStart = input.selectionStart;
            this.templateEditor.selectionEnd = input.selectionEnd;
        },
        insertTemplateFormula(formula) {
            if (this.editingLocked || !formula) return;
            const text = String(this.templateEditor.text || '');
            const max = text.length;
            const start = Math.max(0, Math.min(Number(this.templateEditor.selectionStart) || 0, max));
            const end = Math.max(start, Math.min(Number(this.templateEditor.selectionEnd) || start, max));
            const selected = text.slice(start, end);
            const wholeFormula = selected.match(/^\s*\{\{=\s*([\s\S]*?)\s*\}\}\s*$/);
            let replacementStart = start;
            let replacementEnd = end;
            let selectedArgument = wholeFormula ? wholeFormula[1].trim() : selected;
            if (!wholeFormula) {
                const formulaStart = text.lastIndexOf('{{=', start);
                const formulaEndOffset = text.indexOf('}}', end);
                const formulaPrefix = formulaStart >= 0 ? text.slice(formulaStart + 3, start) : '';
                const formulaSuffix = formulaEndOffset >= 0 ? text.slice(end, formulaEndOffset) : '';
                if (formulaStart >= 0 &&
                    formulaEndOffset >= 0 &&
                    /^\s*$/.test(formulaPrefix) &&
                    /^\s*$/.test(formulaSuffix)) {
                    replacementStart = formulaStart;
                    replacementEnd = formulaEndOffset + 2;
                    selectedArgument = selected.trim();
                }
            }
            const argumentsText = Array.from({ length: Number(formula.arity) || 0 }, (_, index) =>
                index === 0 && selectedArgument ? selectedArgument : '');
            const snippet = `{{= ${formula.name}(${argumentsText.join(', ')}) }}`;
            this.templateEditor.text = text.slice(0, replacementStart) + snippet + text.slice(replacementEnd);
            this.templateEditor.selectionStart = replacementStart;
            this.templateEditor.selectionEnd = replacementStart + snippet.length;
            if (typeof this.$nextTick !== 'function') return;
            this.$nextTick(() => {
                const input = this.templateEditorInputElement();
                if (!input || typeof input.focus !== 'function') return;
                input.focus();
                if (typeof input.setSelectionRange === 'function') {
                    input.setSelectionRange(this.templateEditor.selectionStart, this.templateEditor.selectionEnd);
                }
            });
        },
        removeTemplateBinding(token) {
            const placeholder = `{{${token}}}`;
            this.templateEditor.text = String(this.templateEditor.text || '').split(placeholder).join('');
            this.templateEditor.bindings = this.templateEditor.bindings.filter(item => item.token !== token);
        },
        saveParameterTemplate() {
            const editor = this.templateEditor;
            const node = this.form.graph.nodes.find(item => item.id === editor.nodeId);
            const isAssignment = editor.targetType === 'code-assignment';
            const isParameter = !isAssignment && (!editor.configKey || editor.configKey === 'parameters');
            const target = isAssignment
                ? node?.config?.assignments?.[editor.assignmentIndex]
                : isParameter ? node?.config?.parameters : node?.config;
            const targetKey = isAssignment ? 'value' : isParameter ? editor.parameterName : editor.configKey;
            if (!node || !targetKey || !target) {
                editor.visible = false;
                return;
            }
            const text = String(editor.text || '');
            const bindings = editor.allowBindings === false
                ? []
                : this.normalizeTemplateBindings(text, editor.bindings);
            const hasTemplate = bindings.length > 0 ? true : text.includes('{{=');
            this.$set(target, targetKey,
                hasTemplate ? { $template: { text, bindings } } : text);
            editor.visible = false;
        },
        parameterTemplateSummary(value) {
            const template = this.templateFor(value);
            const text = String(template?.text || '');
            return text.length > 96 ? `${text.substring(0, 96)}…` : (text || '（空文本）');
        },
        parameterTemplateBindings(value) {
            const template = this.templateFor(value);
            return template && Array.isArray(template.bindings) ? template.bindings : [];
        },
        resetNodeTemplateManual(node, configKey) {
            if (!node || !node.config || !configKey) return;
            this.$set(node.config, configKey, '');
        },
        bindingFor(node, parameter) { return node && node.config.parameters && this.isBinding(node.config.parameters[parameter.name]) ? node.config.parameters[parameter.name].$source : null; },
        resolvedBindingFor(node, parameter) {
            const binding = this.bindingFor(node, parameter);
            if (!binding) return null;
            const source = this.form.graph.nodes.find(item => item.id === binding.nodeId);
            const field = source && this.nodeOutputFields(source).find(item => item.path === (binding.path || '$'));
            return field ? { ...binding, sourceType: field.typeName, isArray: !!field.isArray, requiresIndex: !!field.requiresIndex } : binding;
        },
        bindingSelection(node, parameter) {
            const binding = this.bindingFor(node, parameter);
            return binding ? [binding.nodeId, binding.path || '$'] : [];
        },
        setParameterBinding(node, parameter, selection) {
            if (!selection || selection.length < 2) { this.resetParameterManual(node, parameter); return; }
            const source = this.createSourceBinding(selection);
            if (!source) return;
            this.$set(node.config.parameters, parameter.name, {
                $source: source
            });
        },
        resetParameterManual(node, parameter) {
            const ui = typeof NeuCharWorkflowUi !== 'undefined' ? NeuCharWorkflowUi : null;
            const value = ui && typeof ui.defaultParameterValue === 'function'
                ? ui.defaultParameterValue(parameter)
                : (parameter && (parameter.parameterType === 2 ||
                    (parameter.parameterType === 6 && parameter.allowMultiple))
                    ? []
                    : parameter && parameter.parameterType === 4 ? false : '');
            this.$set(node.config.parameters, parameter.name, value);
        },
        formatFileSize(value) {
            const bytes = Number(value || 0);
            if (bytes < 1024) return `${bytes} B`;
            if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
            if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
            return `${(bytes / 1024 / 1024 / 1024).toFixed(1)} GB`;
        },
        async uploadFunctionFiles(fileList, maxFiles) {
            const files = Array.from(fileList || []);
            if (!files.length) return [];
            if (maxFiles && files.length > Number(maxFiles)) {
                throw new Error(`一次最多选择 ${maxFiles} 个文件。`);
            }
            const form = new FormData();
            files.forEach(file => form.append('files', file, file.name));
            const response = await service.post(
                '/api/Senparc.Areas.Admin/FunctionFiles/upload',
                form,
                { customAlert: true });
            const uploaded = response && response.data && response.data.files;
            if (!Array.isArray(uploaded) || uploaded.length !== files.length) {
                throw new Error('文件上传响应无效。');
            }
            return uploaded;
        },
        async handleWorkflowFileSelection(event, node, parameter) {
            const selectedFiles = event && event.target ? event.target.files : [];
            if (!selectedFiles.length || !node || !parameter || this.editingLocked) return;
            this.loading = true;
            try {
                const uploaded = await this.uploadFunctionFiles(selectedFiles, parameter.maxFiles);
                const references = parameter.allowMultiple ? uploaded : uploaded.slice(0, 1);
                this.$set(node.config.parameters, parameter.name,
                    parameter.allowMultiple
                        ? references.map(file => file.fileId)
                        : (references[0] ? references[0].fileId : ''));
                if (!this.workflowFileSelections[node.id]) {
                    this.$set(this.workflowFileSelections, node.id, {});
                }
                this.$set(this.workflowFileSelections[node.id], parameter.name, references);
                this.scheduleAutoSave();
            } catch (error) {
                this.$notify({
                    title: '文件上传失败',
                    message: error && error.message ? error.message : '文件上传失败。',
                    type: 'error'
                });
            } finally {
                this.loading = false;
                if (event && event.target) event.target.value = '';
            }
        },
        expectedShape(parameter) {
            const systemType = String(parameter.systemType || '').toLowerCase();
            const isArray = parameter.parameterType === 2 || systemType.includes('[]') || systemType.includes('list') || systemType.includes('collection');
            const typeName = systemType.includes('bool') ? 'boolean'
                : (systemType.includes('date') || systemType.includes('time')) ? 'datetime'
                : /int|decimal|double|single|float|number/.test(systemType) ? 'number'
                : /string|char|guid/.test(systemType) ? 'string' : 'any';
            return { typeName, isArray };
        },
        bindingCompatibility(node, parameter) {
            const value = node && node.config?.parameters?.[parameter.name];
            const formulaCompatibility = this.formulaParameterCompatibility(parameter, value);
            if (formulaCompatibility && ['danger', 'warning'].includes(formulaCompatibility.level)) return formulaCompatibility;
            if (this.isTemplateValue(value)) {
                if (!this.canUseTemplate(parameter)) return { level: 'danger', text: '此参数不支持在文本中嵌入变量' };
                const bindings = this.parameterTemplateBindings(value);
                if (!bindings.length) return formulaCompatibility || { level: 'manual', text: '手动输入' };
                const invalid = bindings.find(item => {
                    const source = this.form.graph.nodes.find(node => node.id === item?.source?.nodeId);
                    const field = source && this.nodeOutputFields(source).find(candidate =>
                        candidate.path === (item?.source?.path || '$'));
                    return !source || !this.upstreamNodes(node).some(upstream => upstream.id === source.id) ||
                        !field ||
                        String(item?.source?.sourceKind || 'output') !== String(field.sourceKind || 'output') ||
                        (field.requiresIndex && (item?.source?.collectionIndex === null ||
                            typeof item?.source?.collectionIndex === 'undefined'));
                });
                return invalid
                    ? { level: 'danger', text: '文本中的变量来源已失效，或缺少上游列表索引' }
                    : formulaCompatibility || { level: 'success', text: `文本中嵌入 ${bindings.length} 个上游值` };
            }
            const rawBinding = this.bindingFor(node, parameter);
            if (!rawBinding) return formulaCompatibility || { level: 'manual', text: '手动输入' };
            const source = this.form.graph.nodes.find(item => item.id === rawBinding.nodeId);
            const field = source && this.nodeOutputFields(source).find(item => item.path === (rawBinding.path || '$'));
            if (!source || !this.upstreamNodes(node).some(item => item.id === source.id)) return { level: 'danger', text: '关联节点已不是有效上游节点' };
            if (!field) return rawBinding.sourceKind === 'function-selection'
                ? { level: 'danger', text: '关联的 Function 选择参数已在模块更新后删除或不可用' }
                : { level: 'danger', text: '关联的输出字段已不存在' };
            if (String(rawBinding.sourceKind || 'output') !== String(field.sourceKind || 'output')) {
                return { level: 'danger', text: '关联字段类型已在模块更新后发生变化，请重新选择来源' };
            }
            const binding = this.resolvedBindingFor(node, parameter);
            if (!binding) return { level: 'manual', text: '手动输入' };
            const expected = this.expectedShape(parameter);
            if (binding.requiresIndex && (binding.collectionIndex === null || typeof binding.collectionIndex === 'undefined')) return { level: 'danger', text: '上游是对象列表，请选择列表索引' };
            const actualArray = binding.isArray && (binding.itemIndex === null || typeof binding.itemIndex === 'undefined');
            if (expected.isArray !== actualArray) return actualArray
                ? { level: 'danger', text: '类型不匹配：请选择数组索引后再传入单值' }
                : { level: 'danger', text: '类型不匹配：目标参数要求数组' };
            if (!['any', 'object'].includes(expected.typeName) && !['any', 'object'].includes(binding.sourceType) && expected.typeName !== binding.sourceType) return { level: 'warning', text: `类型不匹配：需要 ${expected.typeName}，当前为 ${binding.sourceType}` };
            return { level: 'success', text: `已关联 ${binding.sourceType}${actualArray ? '[]' : ''}` };
        },
        configBindingSelection(node, key) {
            const value = node && node.config[key];
            return this.isBinding(value) ? [value.$source.nodeId, value.$source.path || '$'] : [];
        },
        loopScopeNodeIds(loop) {
            if (!loop || !this.form?.graph) return [];
            const edges = this.form.graph.edges || [];
            const nodes = new Map((this.form.graph.nodes || []).map(node => [node.id, node]));
            const ids = new Set([loop.id]);
            const visited = new Set();
            const queue = edges.filter(edge => edge.source === loop.id).map(edge => edge.target);
            while (queue.length) {
                const currentId = queue.shift();
                if (visited.has(currentId)) continue;
                visited.add(currentId);
                const current = nodes.get(currentId);
                if (!current) continue;
                ids.add(currentId);
                // An explicitly owned loop-end closes this scope. A nested loop-end is
                // transparent to the parent scope so the parent still includes the child.
                if (current.type === 'loop-end' &&
                    (!current.config?.loopId || current.config.loopId === loop.id)) continue;
                edges.filter(edge => edge.source === currentId).forEach(edge => queue.push(edge.target));
            }
            return [...ids];
        },
        loopScopeLabelNodeIds(loop) {
            if (!loop) return [];
            const ids = new Set([loop.id]);
            this.loopBoundaryNodes(loop).forEach(node => ids.add(node.id));
            return [...ids];
        },
        loopsAffectingNode(node) {
            if (!node || !this.form?.graph) return [];
            const loops = this.form.graph.nodes.filter(item => item.type === 'loop');
            return loops.filter(loop => this.loopScopeNodeIds(loop).includes(node.id));
        },
        isLoopScopeHighlighted(node) {
            return !!node && (this.loopHighlight.nodeIds || []).includes(node.id);
        },
        isLoopLabelHighlighted(node) {
            return !!node && (this.loopHighlight.labelNodeIds || []).includes(node.id);
        },
        isLoopScopeEdgeHighlighted(edge) {
            return !!edge && (this.loopHighlight.edgeIds || []).includes(edge.id);
        },
        clearLoopHighlight() {
            if (this.loopHighlightTimer !== null && this.loopHighlightTimer !== undefined && typeof window !== 'undefined') {
                window.clearTimeout(this.loopHighlightTimer);
            }
            this.loopHighlightTimer = null;
            this.loopHighlightSequence += 1;
            this.loopHighlight.nodeIds = [];
            this.loopHighlight.labelNodeIds = [];
            this.loopHighlight.edgeIds = [];
        },
        highlightLoopContext(node) {
            this.clearLoopHighlight();
            const loops = this.loopsAffectingNode(node);
            if (!loops.length) return;

            const nodeIds = new Set();
            const labelNodeIds = new Set();
            const edgeIds = new Set();
            const edges = this.form.graph.edges || [];
            loops.forEach(loop => {
                this.loopScopeNodeIds(loop).forEach(nodeId => nodeIds.add(nodeId));
                this.loopScopeLabelNodeIds(loop).forEach(nodeId => labelNodeIds.add(nodeId));
                const scope = new Set(this.loopScopeNodeIds(loop));
                edges.filter(edge => scope.has(edge.source) && scope.has(edge.target))
                    .forEach(edge => edgeIds.add(edge.id));
            });
            this.loopHighlight.nodeIds = [...nodeIds];
            this.loopHighlight.labelNodeIds = [...labelNodeIds];
            this.loopHighlight.edgeIds = [...edgeIds];

            const sequence = this.loopHighlightSequence;
            if (typeof window !== 'undefined' && typeof window.setTimeout === 'function') {
                this.loopHighlightTimer = window.setTimeout(() => {
                    if (this.loopHighlightSequence === sequence) this.clearLoopHighlight();
                }, 3500);
            }
        },
        loopBoundaryNodes(loop) {
            if (!loop || !this.form?.graph) return [];
            const nodes = this.form.graph.nodes || [];
            const edges = this.form.graph.edges || [];
            const byId = new Map(nodes.map(node => [node.id, node]));
            const queue = edges.filter(edge => edge.source === loop.id).map(edge => edge.target);
            const visited = new Set();
            const boundaries = [];
            while (queue.length) {
                const current = queue.shift();
                if (visited.has(current)) continue;
                visited.add(current);
                const node = byId.get(current);
                if (!node) continue;
                if (node.type === 'loop-end') {
                    const owner = String(node.config?.loopId || '');
                    if (!owner || owner === String(loop.id)) {
                        boundaries.push(node);
                        continue;
                    }
                }
                edges.filter(edge => edge.source === current).forEach(edge => queue.push(edge.target));
            }
            return boundaries;
        },
        loopOwnerOptions(loopEnd) {
            if (!loopEnd || !this.form?.graph) return [];
            return (this.form.graph.nodes || []).filter(loop =>
                loop.type === 'loop' && this.loopBoundaryNodes(loop).some(node => node.id === loopEnd.id));
        },
        loopBoundaryValidationError(loop) {
            const graph = this.form?.graph;
            const boundaries = this.loopBoundaryNodes(loop);
            if (!boundaries.length) return '';
            if (boundaries.length > 1) return `循环节点“${loop.name}”的循环体只能有一个“循环结束”节点。`;
            const boundary = boundaries[0];
            const byId = new Map((graph.nodes || []).map(node => [node.id, node]));
            const outgoing = (graph.edges || []).filter(edge => edge.source === loop.id);
            if (outgoing.length !== 1) return `循环节点“${loop.name}”必须连接一个循环体入口。`;
            const walk = (currentId, visited) => {
                if (currentId === boundary.id) return '';
                if (visited.has(currentId)) return `循环节点“${loop.name}”的循环体路径无效。`;
                visited.add(currentId);
                const current = byId.get(currentId);
                if (!current) return '循环体引用了不存在的节点。';
                if (current.type === 'end' || ['parallel', 'aggregate', 'merge'].includes(current.type)) {
                    return `循环节点“${loop.name}”的循环体不支持结束、并行、聚合或逐项合流节点。`;
                }
                if (current.type === 'loop-end') {
                    const next = (graph.edges || []).filter(edge => edge.source === currentId);
                    return next.length === 1 ? walk(next[0].target, new Set(visited)) : '嵌套循环结束后必须连接一个后续节点。';
                }
                if (current.type === 'loop') {
                    const nested = this.loopBoundaryNodes(current);
                    if (nested.length !== 1) return `嵌套循环“${current.name}”必须明确配置唯一的循环结束节点。`;
                    const next = (graph.edges || []).filter(edge => edge.source === nested[0].id);
                    return next.length === 1 ? walk(next[0].target, new Set(visited)) : `嵌套循环“${current.name}”结束后必须连接一个后续节点。`;
                }
                const next = (graph.edges || []).filter(edge => edge.source === currentId);
                if (current.type === 'condition') {
                    for (const edge of next) {
                        if (edge.sourceHandle === 'break' && (edge.target !== boundary.id || edge.targetHandle !== 'break')) {
                            return `条件节点“${current.name}”的 break 输出必须连接当前循环的 break 输入端。`;
                        }
                        if (edge.sourceHandle !== 'break') {
                            const error = walk(edge.target, new Set(visited));
                            if (error) return error;
                        }
                    }
                    return next.some(edge => edge.sourceHandle === 'true' || edge.sourceHandle === 'false') ? '' : `循环体内的条件节点“${current.name}”必须连接真/假分支。`;
                }
                return next.length === 1 ? walk(next[0].target, visited) : `循环节点“${loop.name}”的循环体必须是一条单一路径。`;
            };
            return walk(outgoing[0].target, new Set());
        },
        loopCountOutputOptions() {
            return this.upstreamNodes(this.selectedNode).map(node => ({
                value: node.id,
                label: node.name,
                children: this.nodeOutputFields(node)
                    .filter(field => !field.isArray && !field.requiresIndex)
                    .map(field => ({
                        value: field.path,
                        label: `${field.label} · 运行时必须为 1–100000 的整数${field.observed ? ' · 运行观察' : ''}`
                    }))
            })).filter(option => option.children.length);
        },
        loopCountSourceLabel(node) {
            const binding = node?.config?.count?.$source;
            return binding ? this.templateBindingLabel(binding) : '已失效的上游来源';
        },
        loopCountFormulaValidationError(node) {
            const value = node?.config?.count;
            if (!this.isTemplateValue(value)) return '';
            const template = this.templateFor(value);
            const text = String(template?.text || '').trim();
            if (!text) return `循环节点“${node.name}”的次数语法不能为空。`;
            if (!text.includes('{{=') && !(Array.isArray(template?.bindings) && template.bindings.length)) {
                return `循环节点“${node.name}”的次数语法必须是数值公式或上游数值引用。`;
            }
            return '';
        },
        setLoopCountBinding(node, selection) {
            if (!selection || selection.length < 2) {
                this.$set(node.config, 'count', 3);
                return;
            }
            this.setConfigBinding(node, 'count', selection);
        },
        resetLoopCountManual(node) {
            if (!node || !node.config) return;
            this.$set(node.config, 'count', 3);
        },
        setConfigBinding(node, key, selection) {
            if (!selection || selection.length < 2) { this.$set(node.config, key, ''); return; }
            const source = this.form.graph.nodes.find(item => item.id === selection[0]);
            const field = source && this.nodeOutputFields(source).find(item => item.path === selection[1]);
            if (!source || !field) return;
            this.$set(node.config, key, {
                $source: {
                    nodeId: source.id,
                    path: field.path,
                    sourceType: field.typeName,
                    isArray: !!field.isArray,
                    requiresIndex: !!field.requiresIndex,
                    sourceKind: field.sourceKind || 'output',
                    sourceParameterName: field.sourceParameterName || null,
                    collectionIndex: null,
                    itemIndex: null
                }
            });
        },
        configValueLabel(value) { return this.isBinding(value) ? `关联：${value.$source.nodeId}` : String(value || ''); },
        getDisconnectedNodes() {
            const nodes = this.form.graph.nodes || [];
            const nodeIds = new Set(nodes.map(node => node.id));
            const trigger = nodes.find(node => String(node.type).endsWith('trigger'));
            if (!trigger) return [];
            const visited = new Set(); const queue = [trigger.id];
            while (queue.length) {
                const current = queue.shift();
                if (visited.has(current)) continue;
                visited.add(current);
                this.form.graph.edges
                    .filter(edge => edge.source === current && nodeIds.has(edge.target))
                    .forEach(edge => queue.push(edge.target));
            }
            return nodes.filter(node => !visited.has(node.id));
        },
        validate(options) {
            const requireRunnable = !options || options.requireRunnable !== false;
            if (!this.form.name.trim()) return '请输入工作流名称。';
            const variables = this.form.graph?.variables || [];
            if (variables.length > 30) return '单个工作流最多允许 30 个变量。';
            const variableNames = new Set();
            for (const variable of variables) {
                const name = String(variable?.name || '').trim();
                if (!/^[A-Za-z_][A-Za-z0-9_]{0,63}$/.test(name) || /^(input|vars)$/i.test(name)) {
                    return '工作流变量名必须唯一，以字母或下划线开头，仅包含字母、数字、下划线；不能使用 input 或 vars。';
                }
                const key = name.toLowerCase();
                if (variableNames.has(key)) return `工作流变量“${name}”重复。`;
                variableNames.add(key);
                if (this.runtimeTextLength(variable?.value) > 8000) return `工作流变量“${name}”的值不能超过 8000 个字符。`;
            }
            const triggers = this.form.graph.nodes.filter(node => String(node.type).endsWith('trigger'));
            if (triggers.length !== 1) return '工作流必须且只能包含一个触发器。';
            const trigger = triggers[0];
            if (!['manual-trigger', 'interval-trigger', 'webhook-trigger', 'chat-trigger'].includes(trigger.type)) return '触发器节点类型无效。';
            if (this.form.triggerType === 'webhook') {
                if (!['any', 'get', 'post'].includes(String(this.form.webhookMethod || '').toLowerCase())) return 'Webhook 请求方法无效。';
                const names = new Set();
                for (const parameter of this.form.webhookParameters || []) {
                    const name = String(parameter.name || '').trim();
                    if (!/^[A-Za-z_][A-Za-z0-9_.-]{0,63}$/.test(name)) return 'Webhook 参数名格式无效。';
                    const key = name.toLowerCase();
                    if (names.has(key)) return `Webhook 参数“${name}”重复。`;
                    names.add(key);
                    if (String(parameter.description || '').length > 500) return `Webhook 参数“${name}”的说明不能超过 500 个字符。`;
                }
            }
            if (this.form.triggerType === 'chat') {
                if (String(this.form.chatTitle || '').length > 100) return 'Chat 标题不能超过 100 个字符。';
                if (String(this.form.chatGreeting || '').length > 500) return 'Chat 欢迎语不能超过 500 个字符。';
            }
            const disconnected = this.getDisconnectedNodes();
            if (requireRunnable && disconnected.length) return '画布中仍有未连接到触发器的节点。';
            for (const node of this.form.graph.nodes) {
                const incoming = this.incomingEdges(node.id);
                if (!this.supportsMultipleInputs(node) && incoming.length > 1) return `节点“${node.name}”只允许一个上游；多对一目标请使用 Function、聚合或逐项合流节点。`;
            }
            if (!requireRunnable) return '';
            for (const node of this.form.graph.nodes.filter(item => item.type === 'aggregate')) {
                if (!this.formulaValueText(node.config?.outputTemplate).trim()) return `聚合节点“${node.name}”必须设置输出内容。`;
                if (this.runtimeTextLength(node.config.outputTemplate) > 8000) return `聚合节点“${node.name}”的输出内容不能超过 8000 个字符。`;
            }
            for (const node of this.form.graph.nodes.filter(item => item.type === 'loop')) {
                const loopFormulaError = typeof this.loopCountFormulaValidationError === 'function'
                    ? this.loopCountFormulaValidationError(node)
                    : '';
                if (loopFormulaError) return loopFormulaError;
                if (this.isBinding(node.config?.count)) {
                    const loopBoundaryError = this.loopBoundaryValidationError(node);
                    if (loopBoundaryError) return loopBoundaryError;
                    continue;
                }
                if (this.isTemplateValue(node.config?.count)) {
                    const loopBoundaryError = this.loopBoundaryValidationError(node);
                    if (loopBoundaryError) return loopBoundaryError;
                    continue;
                }
                const count = Number(node.config?.count);
                if (!Number.isInteger(count) || count < 1 || count > MaxWorkflowLoopIterations) {
                    return `循环节点“${node.name}”的次数必须为 1 到 ${MaxWorkflowLoopIterations} 的整数，或引用上游单值。`;
                }
                const loopBoundaryError = this.loopBoundaryValidationError(node);
                if (loopBoundaryError) return loopBoundaryError;
            }
            for (const node of this.form.graph.nodes.filter(item => item.type === 'code')) {
                const assignments = Array.isArray(node.config?.assignments) ? node.config.assignments : [];
                if (!assignments.length || assignments.length > 30) return `安全代码节点“${node.name}”必须设置 1 到 30 条变量赋值。`;
                for (const assignment of assignments) {
                    if (!variableNames.has(String(assignment?.name || '').trim().toLowerCase())) {
                        return `安全代码节点“${node.name}”只能给已定义的工作流变量赋值。`;
                    }
                    if (this.runtimeTextLength(assignment?.value) > 8000) return `安全代码节点“${node.name}”的赋值不能超过 8000 个字符。`;
                }
            }
            for (const node of this.form.graph.nodes.filter(item => item.type === 'sub-workflow')) {
                const targetId = Number(node.config?.workflowId || 0);
                const target = (this.workflows || []).find(item => Number(item.id) === targetId);
                if (!targetId || !target) return `调用工作流节点“${node.name}”必须选择一个已保存的目标工作流。`;
                if (!target.enabled) return `调用工作流节点“${node.name}”的目标工作流尚未启用。`;
                if (targetId === Number(this.form.id || 0)) return `调用工作流节点“${node.name}”不能调用当前工作流。`;
            }
            for (const node of this.form.graph.nodes.filter(item => item.type === 'function')) {
                const fn = this.findFunction(node.config);
                if (!fn || !fn.moduleAvailable) return `节点“${node.name}”引用的模块未开启或 Function 已移除。`;
                const missing = NeuCharWorkflowUi.firstMissingRequired(fn, node.config.parameters || {});
                if (missing) return `节点“${node.name}”缺少必填参数“${missing.title || missing.name}”。`;
                for (const parameter of this.functionParameters(fn)) {
                    if (parameter.hasSyntheticName || parameter.metadataError) {
                        return `节点“${node.name}”参数“${this.parameterDisplayName(parameter)}”缺少原始字段名，当前仅可保存草稿；请修复或更新对应 XNCF 模块。`;
                    }
                    const compatibility = this.bindingCompatibility(node, parameter);
                    if (compatibility.level === 'danger' || compatibility.level === 'warning') return `节点“${node.name}”参数“${parameter.title || parameter.name}”：${compatibility.text}`;
                }
            }
            for (const node of this.form.graph.nodes.filter(item => ['agent', 'agent-group', 'a2a'].includes(item.type))) {
                const object = this.workflowObjects.find(item => item.providerId === node.config.providerId && item.objectId === node.config.objectId);
                if (!object || !object.enabled) return `节点“${node.name}”引用的 Agent 或 A2A 连接不可用。`;
            }
            return '';
        },
        addWorkflowVariable() {
            if (this.editingLocked || this.form.graph.variables.length >= 30) return;
            this.form.graph.variables.push({ name: '', value: '' });
        },
        removeWorkflowVariable(index) {
            if (this.editingLocked) return;
            this.form.graph.variables.splice(index, 1);
        },
        addCodeAssignment(node) {
            if (this.editingLocked || !node) return;
            node.config = node.config || {};
            node.config.assignments = Array.isArray(node.config.assignments) ? node.config.assignments : [];
            if (node.config.assignments.length < 30) node.config.assignments.push({ name: '', value: '' });
        },
        setCodeAssignmentValue(assignment, value) {
            if (!assignment) return;
            this.$set(assignment, 'value', value == null ? '' : value);
        },
        removeCodeAssignment(node, index) {
            if (this.editingLocked || !Array.isArray(node?.config?.assignments)) return;
            node.config.assignments.splice(index, 1);
        },
        addWebhookParameter() {
            if (this.editingLocked || this.form.webhookParameters.length >= 50) return;
            this.form.webhookParameters.push({ _id: this.makeId('webhook-parameter'), name: '', required: false, description: '' });
        },
        removeWebhookParameter(index) {
            if (this.editingLocked) return;
            this.form.webhookParameters.splice(index, 1);
        },
        buildTriggerConfig() {
            if (this.form.triggerType === 'interval') return { intervalSeconds: Number(this.form.intervalSeconds || 300) };
            if (this.form.triggerType === 'webhook') {
                return {
                    method: String(this.form.webhookMethod || 'any').toLowerCase(),
                    token: String(this.form.webhookToken || ''),
                    parameters: (this.form.webhookParameters || []).map(parameter => ({
                        name: String(parameter.name || '').trim(),
                        required: !!parameter.required,
                        description: String(parameter.description || '').trim()
                    }))
                };
            }
            if (this.form.triggerType === 'chat') {
                return {
                    title: String(this.form.chatTitle || '').trim(),
                    greeting: String(this.form.chatGreeting || '').trim(),
                    allowGuest: !!this.form.chatAllowGuest
                };
            }
            return {};
        },
        async copyText(value, title = 'Webhook') {
            if (!value) return;
            try {
                await navigator.clipboard.writeText(value);
                this.$notify({ title, message: '已复制到剪贴板。', type: 'success' });
            } catch {
                this.$notify({ title, message: '复制失败，请手动复制。', type: 'warning' });
            }
        },
        openChatPage() {
            if (!this.chatUrl) return;
            window.open(this.chatUrl, '_blank', 'noopener');
        },
        setAutoSaveEnabled(enabled) {
            this.form.autoSaveMinutes = enabled
                ? Math.max(1, Math.round(Number(this.form.autoSaveMinutes || 3)))
                : 0;
        },
        normalizedAutoSaveMinutes() {
            const value = Number(this.form.autoSaveMinutes);
            if (!Number.isFinite(value) || value <= 0) return 0;
            return Math.min(1440, Math.max(1, Math.round(value)));
        },
        resetSaveState() {
            this.clearAutoSaveTimer();
            this.saveState.saving = false;
            this.saveState.lastSavedSignature = '';
            this.saveState.lastSavedLabel = '';
            this.saveState.status = 'idle';
            this.saveState.error = '';
            this.saveState.autoSaveBlockedSignature = '';
        },
        markSaved() {
            this.saveState.lastSavedSignature = this.currentSaveSignature;
            this.saveState.lastSavedLabel = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            this.saveState.status = 'saved';
            this.saveState.error = '';
            this.saveState.autoSaveBlockedSignature = '';
            this.scheduleAutoSave();
        },
        clearAutoSaveTimer() {
            if (this.saveState.timer) window.clearTimeout(this.saveState.timer);
            this.saveState.timer = null;
        },
        scheduleAutoSave() {
            this.clearAutoSaveTimer();
            const minutes = this.normalizedAutoSaveMinutes();
            const signature = this.currentSaveSignature;
            if (this.saveState.autoSaveBlockedSignature && this.saveState.autoSaveBlockedSignature !== signature) {
                this.saveState.autoSaveBlockedSignature = '';
            }
            if (this.saveState.autoSaveBlockedSignature === signature) return;
            if (!this.editing || !this.form.id || minutes <= 0) return;
            this.saveState.timer = window.setTimeout(this.runAutoSave, minutes * 60 * 1000);
        },
        async runAutoSave() {
            this.saveState.timer = null;
            if (!this.editingLocked && this.saveDirty && !this.saveState.saving) {
                await this.saveWorkflow({ silent: true, automatic: true, source: 'auto' });
            }
            this.scheduleAutoSave();
        },
        onSaveShortcut(event) {
            if (!this.editing || !(event.metaKey || event.ctrlKey) || String(event.key).toLowerCase() !== 's') return;
            event.preventDefault();
            if (!this.editingLocked) this.saveWorkflow({ source: 'shortcut' });
        },
        async confirmDiscardChanges(action) {
            if (!this.saveDirty) return true;
            if (this.discardConfirming) return false;
            this.discardConfirming = true;
            try {
                await this.$confirm(
                    '当前工作流有未保存的更改。离开后这些更改将丢失。',
                    action || '确认离开当前工作流？',
                    {
                        confirmButtonText: '放弃更改',
                        cancelButtonText: '继续编辑',
                        type: 'warning',
                        distinguishCancelAndClose: true
                    });
                return true;
            } catch {
                return false;
            } finally {
                this.discardConfirming = false;
            }
        },
        onBeforeUnload(event) {
            if (!this.saveDirty) return undefined;
            event.preventDefault();
            event.returnValue = '';
            return '';
        },
        applySavedWorkflow(saved) {
            this.form.id = Number(saved.id || this.form.id || 0);
            this.form.revision = Number(saved.revision || this.form.revision || 0);
            this.form.autoSaveMinutes = Number(saved.autoSaveMinutes ?? this.form.autoSaveMinutes ?? 3);
            if (typeof saved.enabled === 'boolean') this.form.enabled = saved.enabled;
            if (saved.triggerType) this.form.triggerType = saved.triggerType;
            const trigger = NeuCharWorkflowUi.parseJson(saved.triggerConfigJson, {});
            this.form.intervalSeconds = Number(trigger.intervalSeconds || this.form.intervalSeconds || 300);
            this.form.webhookMethod = String(trigger.method || this.form.webhookMethod || 'any').toLowerCase();
            this.form.webhookToken = String(trigger.token || this.form.webhookToken || '');
            this.form.webhookParameters = (trigger.parameters || this.form.webhookParameters || []).map(parameter => ({
                _id: parameter._id || this.makeId('webhook-parameter'),
                name: parameter.name || '',
                required: !!parameter.required,
                description: parameter.description || ''
            }));
            this.form.chatTitle = String(trigger.title ?? this.form.chatTitle ?? '');
            this.form.chatGreeting = String(trigger.greeting ?? this.form.chatGreeting ?? '');
            if (typeof trigger.allowGuest === 'boolean') this.form.chatAllowGuest = trigger.allowGuest;
            if (saved.runningCount !== undefined && saved.runningCount !== null) {
                this.form.runningCount = Math.max(0, Number(saved.runningCount) || 0);
            }
            if (saved.graphJson) {
                const graph = NeuCharWorkflowUi.parseJson(saved.graphJson, this.form.graph);
                graph.nodes = graph.nodes || [];
                graph.edges = graph.edges || [];
                this.ensureGraphLayout(graph);
                this.form.graph = graph;
            }
            this.markSaved();
        },
        async saveWorkflow(options) {
            options = options || {};
            if (this.editingLocked || this.saveState.saving) return null;
            const saveSignature = this.currentSaveSignature;
            if (this.form.id && !this.saveDirty) {
                if (!options.silent) this.$notify({ title: 'Workflow', message: '当前没有需要保存的更改。', type: 'info' });
                this.scheduleAutoSave();
                return { id: this.form.id, revision: this.form.revision, unchanged: true };
            }
            const error = this.validate({ requireRunnable: false });
            if (error) {
                const issue = this.showValidationIssue(error, '请检查节点配置。', {
                    source: 'save',
                    focus: !options.automatic
                });
                this.saveState.status = 'error';
                this.saveState.error = issue.message;
                if (!options.automatic) this.$notify({ title: '无法保存', message: issue.message, type: 'warning' });
                return null;
            }
            this.saveState.saving = true;
            this.saveState.status = 'saving';
            this.saveState.error = '';
            try {
                this.syncTriggerNode();
                const response = await service.post('/Admin/NeuCharWorkflow/Index?handler=Save', {
                    id: this.form.id || 0,
                    name: this.form.name,
                    description: this.form.description,
                    graphJson: JSON.stringify(this.form.graph),
                    enabled: !!this.form.enabled,
                    triggerType: this.form.triggerType,
                    triggerConfigJson: JSON.stringify(this.buildTriggerConfig()),
                    autoSaveMinutes: this.normalizedAutoSaveMinutes(),
                    expectedRevision: this.form.id ? Number(this.form.revision || 0) : null,
                    saveSource: options.source || 'manual'
                }, { customAlert: true });
                const saved = NeuCharWorkflowUi.unwrap(response);
                this.applySavedWorkflow(saved);
                this.clearValidationIssue();
                if (!options.silent) {
                    const draftCount = this.disconnectedNodes.length;
                    const message = draftCount
                        ? `草稿已保存。${draftCount} 个未连接节点会阻止运行；已自动停用工作流，请连接完成后重新启用。`
                        : (options.source === 'shortcut' ? '已使用快捷键保存。' : '工作流已保存。');
                    this.$notify({ title: 'Workflow', message, type: draftCount ? 'warning' : 'success' });
                }
                await this.refreshWorkflowList();
                return saved;
            } catch (error) {
                const issue = this.showValidationIssue(error, '请检查节点配置。', {
                    source: 'save',
                    focus: !options.automatic
                });
                const message = issue.message;
                this.saveState.status = 'error';
                this.saveState.error = message;
                if (options.automatic && this.currentSaveSignature === saveSignature) {
                    this.saveState.autoSaveBlockedSignature = saveSignature;
                    this.$notify({ title: '自动保存已暂停', message: `${message}。请修正后手动保存。`, type: 'warning' });
                }
                if (!options.automatic) this.$notify({ title: '保存失败', message, type: 'error' });
                return null;
            } finally { this.saveState.saving = false; }
        },
        async startWorkflow() {
            if (this.run.running || this.run.validating) return;
            const localError = this.validate({ requireRunnable: true });
            if (localError) {
                const issue = this.showValidationIssue(localError, '运行前校验失败。', {
                    source: 'run',
                    nodeIds: /未连接到触发器/.test(localError) ? this.getDisconnectedNodes().map(node => node.id) : undefined
                });
                this.appendConsole('validation', issue.message, 'failed');
                this.$notify({ title: '运行前校验失败', message: issue.message, type: 'warning' });
                return;
            }
            this.clearValidationIssue();
            this.run.validating = true;
            this.run.error = '';
            this.run.finalOutput = '';
            this.run.events = [];
            this.run.nodeStates = {};
            this.run.humanInteractions = [];
            this.run.humanReplyVisible = false;
            this.run.humanReplyRequest = null;
            this.run.humanReplyInput = '';
            this.appendConsole('validation', '正在保存并校验当前工作流……', 'running');
            try {
                const saved = await this.saveWorkflow({ silent: true });
                if (!saved) return;
                await service.post('/Admin/NeuCharWorkflow/Index?handler=ValidateRun', { id: this.form.id, input: this.run.input }, { customAlert: true });
                this.appendConsole('validation', '参数、引用和类型校验通过。', 'success');
                const response = await service.post('/Admin/NeuCharWorkflow/Index?handler=StartRun', { id: this.form.id, input: this.run.input }, { customAlert: true });
                const data = NeuCharWorkflowUi.unwrap(response) || {};
                this.run.runId = data.runId;
                this.run.running = true;
                this.run.status = 'running';
                this.run.lastSequence = 0;
                this.pollRun();
            } catch (error) {
                const issue = this.showValidationIssue(error, '运行前校验失败。', { source: 'run' });
                const message = issue.message;
                this.run.status = 'failed'; this.run.error = message;
                this.appendConsole('validation', message, 'failed');
                this.$notify({ title: '无法运行', message, type: 'error' });
            } finally { this.run.validating = false; }
        },
        async pollRun() {
            if (!this.run.runId) return;
            try {
                const response = await service.get(`/Admin/NeuCharWorkflow/Index?handler=RunStatus&runId=${encodeURIComponent(this.run.runId)}&afterSequence=${this.run.lastSequence}`);
                const snapshot = NeuCharWorkflowUi.unwrap(response) || {};
                if (snapshot.runningCount !== undefined && snapshot.runningCount !== null) {
                    this.form.runningCount = Math.max(0, Number(snapshot.runningCount) || 0);
                }
                (snapshot.events || []).forEach(event => {
                    this.run.lastSequence = Math.max(this.run.lastSequence, Number(event.sequence || 0));
                    this.applyRunEvent(event);
                });
                this.run.humanInteractions = Array.isArray(snapshot.humanInteractions)
                    ? snapshot.humanInteractions
                    : [];
                this.syncHumanInteractionDialog();
                if (snapshot.running) {
                    this.run.pollTimer = window.setTimeout(this.pollRun, 450);
                    return;
                }
                this.run.running = false;
                this.run.aborting = false;
                this.run.status = snapshot.succeeded ? 'success' : 'failed';
                this.run.finalOutput = snapshot.finalOutput || '';
                this.run.error = snapshot.errorMessage || '';
                this.appendConsole('workflow', snapshot.succeeded ? '工作流运行完成。' : (snapshot.errorMessage || '工作流运行失败。'), snapshot.succeeded ? 'success' : 'failed', snapshot.finalOutput);
            } catch (error) {
                this.run.running = false; this.run.aborting = false; this.run.status = 'failed';
                this.run.error = this.errorMessage(error, '读取运行状态失败。');
                this.appendConsole('workflow', this.run.error, 'failed');
            }
        },
        syncHumanInteractionDialog() {
            const pending = this.run.humanInteractions || [];
            const currentId = this.run.humanReplyRequest && this.run.humanReplyRequest.requestId;
            if (currentId && !pending.some(item => item.requestId === currentId)) {
                this.run.humanReplyVisible = false;
                this.run.humanReplyRequest = null;
                this.run.humanReplyInput = '';
            }
            if (!this.run.humanReplyRequest && pending.length) {
                this.openHumanInteraction(pending[0]);
            }
        },
        openHumanInteraction(request) {
            if (!request) return;
            this.run.humanReplyRequest = request;
            this.run.humanReplyInput = this.requiresHumanTextInput(request) ? '' : (request.prompt || '');
            this.run.humanReplyVisible = true;
        },
        requiresHumanTextInput(request) {
            return ['humanTurn', 'workflowInput'].includes(String(request?.requestType || ''));
        },
        async resolveHumanInteraction(approved) {
            const request = this.run.humanReplyRequest;
            if (!request || this.run.humanReplySubmitting || !this.run.runId) return;
            const requiresTextInput = this.requiresHumanTextInput(request);
            if (requiresTextInput && !String(this.run.humanReplyInput || '').trim()) {
                this.$notify({ title: '需要 Human 输入', message: '请输入文本后再继续 Workflow。', type: 'warning' });
                return;
            }
            this.run.humanReplySubmitting = true;
            try {
                await service.post('/Admin/NeuCharWorkflow/Index?handler=ResolveHuman', {
                    runId: this.run.runId,
                    requestId: request.requestId,
                    approved: !!approved,
                    input: requiresTextInput ? String(this.run.humanReplyInput || '').trim() : '',
                    reason: requiresTextInput ? 'Workflow 快速输入' : 'Workflow 快速审批'
                }, { customAlert: true });
                this.run.humanInteractions = this.run.humanInteractions.filter(item => item.requestId !== request.requestId);
                this.run.humanReplyVisible = false;
                this.run.humanReplyRequest = null;
                this.run.humanReplyInput = '';
                this.$notify({ title: 'Workflow', message: approved ? 'Human 处理已提交，流程继续等待/执行。' : '已拒绝本次工具调用，流程继续处理。', type: approved ? 'success' : 'warning' });
            } catch (error) {
                this.$notify({ title: 'Human 处理失败', message: this.errorMessage(error, '请求可能已由另一入口处理。'), type: 'error' });
            } finally {
                this.run.humanReplySubmitting = false;
            }
        },
        closeHumanInteractionDialog() {
            this.run.humanReplyVisible = false;
        },
        async abortWorkflow() {
            if (!this.run.running || this.run.aborting || !this.run.runId) return;
            try {
                if (this.$confirm) {
                    await this.$confirm('将停止当前工作流。已启动的外部调用可能需要等待其自身响应取消。', '确认中止运行', {
                        confirmButtonText: '中止',
                        cancelButtonText: '继续运行',
                        type: 'warning'
                    });
                }
            } catch (_) {
                return;
            }

            this.run.aborting = true;
            try {
                await service.post('/Admin/NeuCharWorkflow/Index?handler=AbortRun', { runId: this.run.runId }, { customAlert: true });
                this.appendConsole('workflow', '已请求手动中止，正在等待当前节点响应取消。', 'running');
                this.clearRunPoll();
                this.pollRun();
            } catch (error) {
                const message = this.errorMessage(error, '中止工作流失败。');
                this.run.aborting = false;
                this.$notify({ title: '无法中止', message, type: 'error' });
            }
        },
        applyRunEvent(event) {
            if (event && event.outputSchema) {
                const schema = NeuCharWorkflowUi.parseJson(event.outputSchema, null);
                if (schema && schema.nodeId && Array.isArray(schema.fields)) {
                    const index = this.observedOutputSchemas.findIndex(item => item.nodeId === schema.nodeId);
                    if (index >= 0) this.$set(this.observedOutputSchemas, index, schema);
                    else this.observedOutputSchemas.push(schema);
                }
            }
            if (event.nodeId && ['running', 'success', 'failed'].includes(event.status)) this.$set(this.run.nodeStates, event.nodeId, event.status);
            this.run.events.push(event);
            if (this.run.events.length > MaxWorkflowConsoleEvents) {
                this.run.events.splice(0, this.run.events.length - MaxWorkflowConsoleEvents);
            }
            this.$nextTick(() => { const el = this.$refs.consoleLog; if (el) el.scrollTop = el.scrollHeight; });
        },
        appendConsole(nodeName, message, status, output) {
            this.applyRunEvent({ sequence: 0, nodeId: '', nodeName, message, status, output: output || '', timestamp: new Date().toISOString() });
        },
        clearConsole() { if (!this.run.running) this.run.events = []; },
        clearRunPoll() { if (this.run.pollTimer) window.clearTimeout(this.run.pollTimer); this.run.pollTimer = null; },
        resetRunState() {
            this.clearRunPoll();
            this.run.running = false; this.run.validating = false; this.run.aborting = false; this.run.runId = ''; this.run.status = 'idle';
            this.run.events = []; this.run.lastSequence = 0; this.run.nodeStates = {}; this.run.finalOutput = ''; this.run.error = '';
            this.run.humanInteractions = []; this.run.humanReplyVisible = false; this.run.humanReplyRequest = null;
            this.run.humanReplyInput = ''; this.run.humanReplySubmitting = false;
        },
        errorMessage(error, fallback) {
            return this.validationIssueFromError(error, fallback).message;
        },
        async handleWorkflowAction(action) {
            if (action === 'new') {
                await this.createWorkflow();
                return;
            }
            if (action === 'settings') {
                if (!this.editingLocked) this.workflowSettingsVisible = true;
                return;
            }
            if (action === 'auto-layout') {
                this.autoLayout();
                return;
            }
            if (action === 'align-grid') {
                this.alignToNearbyGrid();
                return;
            }
            if (action === 'fit-canvas') {
                this.fitCanvasToNodes();
                return;
            }
            if (action === 'tasks') {
                if (await this.confirmDiscardChanges('进入任务列表')) window.location.assign('/Admin/NeuCharWorkflow/Tasks');
                return;
            }
            if (action !== 'delete' || !this.form.id || this.editingLocked || this.saveState.saving) return;
            try {
                await this.$confirm(
                    `确认删除工作流“${this.form.name || this.form.id}”？此操作无法恢复。`,
                    '删除工作流',
                    {
                        confirmButtonText: '删除',
                        cancelButtonText: '取消',
                        type: 'warning'
                    });
            } catch {
                return;
            }
            await this.deleteWorkflow();
        },
        async deleteWorkflow() {
            if (!this.form.id || this.editingLocked || this.saveState.saving) return;
            await service.post('/Admin/NeuCharWorkflow/Index?handler=Delete', { id: this.form.id }, { customAlert: true });
            this.form = this.emptyForm(); this.observedOutputSchemas = []; this.editing = false; this.resetSaveState(); this.resetRunState(); await this.refreshWorkflowList();
        }
    }
});
