new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            workflows: [],
            functions: [],
            workflowObjects: [],
            keyword: '',
            listCollapsed: false,
            paletteCollapsed: false,
            inspectorCollapsed: false,
            paletteModule: '',
            paletteSearch: '',
            pinnedFunctions: [],
            editing: false,
            selectedNodeId: '',
            connectionDraft: { sourceId: '', sourceHandle: '', x: 0, y: 0 },
            dragState: null,
            canvasSize: { width: 1200, height: 760 },
            form: { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, autoSaveMinutes: 3, revision: 0, graph: { nodes: [], edges: [] } },
            saveState: {
                saving: false,
                lastSavedSignature: '',
                lastSavedLabel: '',
                status: 'idle',
                error: '',
                timer: null
            },
            run: {
                running: false,
                validating: false,
                input: '',
                runId: '',
                status: 'idle',
                events: [],
                lastSequence: 0,
                nodeStates: {},
                finalOutput: '',
                error: '',
                pollTimer: null,
                consoleOpen: true
            }
        };
    },
    computed: {
        filteredWorkflows() {
            const keyword = this.keyword.trim().toLowerCase();
            return keyword ? this.workflows.filter(item => String(item.name).toLowerCase().includes(keyword)) : this.workflows;
        },
        moduleNames() { return [...new Set(this.functions.map(fn => fn.moduleName))].sort(); },
        filteredFunctions() {
            const keyword = this.paletteSearch.trim().toLowerCase();
            return this.functions.filter(fn => {
                const moduleMatched = !this.paletteModule || fn.moduleName === this.paletteModule;
                const keywordMatched = !keyword || [fn.functionName, fn.moduleName, fn.description, fn.functionKey]
                    .some(value => String(value || '').toLowerCase().includes(keyword));
                return moduleMatched && keywordMatched;
            }).sort((left, right) => {
                const pinDiff = Number(this.isPinned(right)) - Number(this.isPinned(left));
                return pinDiff || String(left.moduleName).localeCompare(String(right.moduleName), 'zh-CN') ||
                    String(left.functionName).localeCompare(String(right.functionName), 'zh-CN');
            });
        },
        selectedNode() { return this.form.graph.nodes.find(node => node.id === this.selectedNodeId); },
        selectedFunction() {
            return this.selectedNode && this.selectedNode.type === 'function'
                ? this.findFunction(this.selectedNode.config)
                : null;
        },
        connectionSourceName() {
            const node = this.form.graph.nodes.find(item => item.id === this.connectionDraft.sourceId);
            return node ? node.name : '';
        },
        editingLocked() { return this.run.running; },
        currentSaveSignature() {
            return JSON.stringify({
                name: String(this.form.name || '').trim(),
                description: String(this.form.description || '').trim(),
                enabled: !!this.form.enabled,
                triggerType: this.form.triggerType,
                intervalSeconds: Number(this.form.intervalSeconds || 300),
                autoSaveMinutes: Number(this.form.autoSaveMinutes || 0),
                graph: this.form.graph
            });
        },
        saveDirty() {
            return this.editing && this.currentSaveSignature !== this.saveState.lastSavedSignature;
        },
        saveStatusText() {
            if (this.saveState.saving) return '正在保存…';
            if (this.saveState.status === 'error') return this.saveState.error || '保存失败';
            if (this.saveDirty) return '有尚未保存的更改';
            if (this.saveState.lastSavedLabel) return `已保存 ${this.saveState.lastSavedLabel}`;
            return this.form.id ? '已保存' : '尚未保存';
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
            if (this.run.validating) return '正在校验参数和节点引用';
            if (this.run.running) return '工作流运行中，编辑已锁定';
            if (this.run.status === 'success') return '最近一次测试运行成功';
            if (this.run.status === 'failed') return '最近一次测试运行失败';
            return '测试运行就绪';
        }
    },
    watch: {
        'form.autoSaveMinutes'() { this.scheduleAutoSave(); }
    },
    created() {
        this.loadPinnedFunctions();
        this.loadAll();
    },
    mounted() {
        window.addEventListener('mousemove', this.onPointerMove);
        window.addEventListener('mouseup', this.onPointerUp);
        window.addEventListener('keydown', this.onSaveShortcut);
    },
    beforeDestroy() {
        window.removeEventListener('mousemove', this.onPointerMove);
        window.removeEventListener('mouseup', this.onPointerUp);
        window.removeEventListener('keydown', this.onSaveShortcut);
        this.clearAutoSaveTimer();
        this.clearRunPoll();
    },
    methods: {
        emptyForm() {
            return { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, autoSaveMinutes: 3, revision: 0, graph: { nodes: [], edges: [] } };
        },
        async loadAll() {
            this.loading = true;
            try {
                const [listResponse, dataResponse] = await Promise.all([
                    service.get('/Admin/NeuCharPivot/Workflow?handler=List'),
                    service.get('/Admin/NeuCharPivot/Workflow?handler=DesignerData')
                ]);
                this.workflows = NeuCharPivotUi.unwrap(listResponse) || [];
                const data = NeuCharPivotUi.unwrap(dataResponse) || {};
                this.functions = data.functions || [];
                this.workflowObjects = data.objects || [];
            } finally { this.loading = false; }
        },
        createWorkflow() {
            if (this.editingLocked) return;
            this.form = this.emptyForm();
            this.editing = true;
            this.selectedNodeId = '';
            this.resetSaveState();
            this.cancelConnection();
            this.resetRunState();
            this.syncTriggerNode();
            this.$nextTick(this.autoLayout);
        },
        async editWorkflow(id) {
            if (this.editingLocked) return;
            this.loading = true;
            try {
                const response = await service.get(`/Admin/NeuCharPivot/Workflow?handler=Detail&id=${id}`);
                const item = NeuCharPivotUi.unwrap(response);
                const graph = NeuCharPivotUi.parseJson(item.graphJson, { nodes: [], edges: [] });
                graph.nodes = graph.nodes || [];
                graph.edges = graph.edges || [];
                graph.nodes.forEach(node => {
                    node.config = node.config || {};
                    node.x = Number.isFinite(Number(node.x)) ? Number(node.x) : 80;
                    node.y = Number.isFinite(Number(node.y)) ? Number(node.y) : 80;
                    if (node.type === 'function') node.config.parameters = node.config.parameters || {};
                });
                graph.edges.forEach(edge => {
                    const source = graph.nodes.find(node => node.id === edge.source);
                    edge.sourceHandle = source && source.type === 'condition'
                        ? (edge.sourceHandle === 'false' ? 'false' : 'true')
                        : 'default';
                });
                const trigger = NeuCharPivotUi.parseJson(item.triggerConfigJson, {});
                this.form = {
                    ...item,
                    graph,
                    intervalSeconds: Number(trigger.intervalSeconds || 300),
                    autoSaveMinutes: Number(item.autoSaveMinutes ?? 3),
                    revision: Number(item.revision || 0)
                };
                this.editing = true;
                this.selectedNodeId = graph.nodes.length ? graph.nodes[0].id : '';
                this.cancelConnection();
                this.resetRunState();
                this.markSaved();
                if (graph.nodes.length > 1 && graph.nodes.every(node => Number(node.x) === 0)) {
                    this.$nextTick(this.autoLayout);
                } else {
                    this.updateCanvasSize();
                }
            } finally { this.loading = false; }
        },
        makeId(prefix) { return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}`; },
        syncTriggerNode() {
            if (this.editingLocked) return;
            const type = this.form.triggerType === 'interval' ? 'interval-trigger' : 'manual-trigger';
            const existing = this.form.graph.nodes.find(node => String(node.type).endsWith('trigger'));
            if (existing) {
                existing.type = type;
                existing.name = type === 'interval-trigger' ? '间隔触发' : '手动触发';
            } else {
                const trigger = { id: this.makeId('trigger'), type, name: type === 'interval-trigger' ? '间隔触发' : '手动触发', x: 430, y: 60, config: {} };
                this.form.graph.nodes.unshift(trigger);
                this.selectedNodeId = trigger.id;
            }
            this.updateCanvasSize();
        },
        addSimpleNode(type, name) {
            if (this.editingLocked) return;
            const config = type === 'condition'
                ? { left: '{{input}}', operator: 'equals', right: '' }
                : type === 'delay' ? { seconds: 1 } : {};
            this.appendNode({ id: this.makeId(type), type, name, x: 80, y: 80, config });
        },
        addFunctionNode(fn) {
            if (this.editingLocked || !fn.moduleAvailable) return;
            this.appendNode({
                id: this.makeId('function'),
                type: 'function',
                name: fn.functionName,
                x: 80,
                y: 80,
                config: {
                    functionId: Number(fn.id || 0),
                    moduleUid: fn.moduleUid,
                    functionKey: fn.functionKey,
                    parameters: NeuCharPivotUi.createParameterValues(fn)
                }
            });
        },
        addObjectNode(object) {
            if (this.editingLocked || !object.enabled) return;
            this.appendNode({
                id: this.makeId(object.kind),
                type: object.kind,
                name: object.name,
                x: 80,
                y: 80,
                config: { providerId: object.providerId, objectId: object.objectId, prompt: '处理以下输入：{{input}}' }
            });
        },
        appendNode(node) {
            const previous = [...this.form.graph.nodes].reverse().find(item =>
                item.type !== 'end' && item.type !== 'condition' && !this.form.graph.edges.some(edge => edge.source === item.id));
            this.form.graph.nodes.push(node);
            if (previous && !String(node.type).endsWith('trigger') && this.canConnect(previous, node, 'default')) {
                this.setTarget(previous, 'default', node.id, true);
            }
            this.selectedNodeId = node.id;
            this.autoLayout();
        },
        removeNode(node) {
            if (this.editingLocked || String(node.type).endsWith('trigger')) return;
            this.form.graph.nodes = this.form.graph.nodes.filter(item => item.id !== node.id);
            this.form.graph.edges = this.form.graph.edges.filter(edge => edge.source !== node.id && edge.target !== node.id);
            this.selectedNodeId = '';
            this.cancelConnection();
            this.updateCanvasSize();
        },
        selectNode(node) {
            this.selectedNodeId = node.id;
            if (!this.inspectorCollapsed) return;
            this.inspectorCollapsed = false;
        },
        supportsMultipleInputs(node) { return node && ['aggregate', 'function'].includes(node.type); },
        supportsMultipleOutputs(node) { return node && node.type === 'condition'; },
        targetFor(node, sourceHandle) {
            const edge = this.form.graph.edges.find(item => item.source === node.id && item.sourceHandle === sourceHandle);
            return edge ? edge.target : '';
        },
        incomingEdges(nodeId) { return this.form.graph.edges.filter(edge => edge.target === nodeId); },
        availableTargets(node, sourceHandle) {
            const handle = node.type === 'condition' ? sourceHandle : 'default';
            return this.form.graph.nodes.filter(target => this.canConnect(node, target, handle));
        },
        canConnect(source, target, sourceHandle) {
            if (!source || !target || source.id === target.id || source.type === 'end' || String(target.type).endsWith('trigger')) return false;
            if (this.wouldCreateCycle(source.id, target.id)) return false;
            const handle = source.type === 'condition' ? sourceHandle : 'default';
            const incoming = this.incomingEdges(target.id).filter(edge =>
                !(edge.source === source.id && edge.sourceHandle === handle));
            if (incoming.length && !this.supportsMultipleInputs(target)) return false;
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
        setTarget(node, sourceHandle, targetId, silent) {
            if (this.editingLocked) return false;
            const handle = node.type === 'condition' ? sourceHandle : 'default';
            const target = this.form.graph.nodes.find(item => item.id === targetId);
            if (targetId && !this.canConnect(node, target, handle)) {
                if (!silent) this.$notify({ title: '无法连接', message: '目标已有上游、连接会形成循环，或节点不支持该连接方式。多对一目标可使用 Function 或聚合节点。', type: 'warning' });
                return false;
            }
            this.form.graph.edges = this.form.graph.edges.filter(edge =>
                !(edge.source === node.id && edge.sourceHandle === handle));
            if (targetId) {
                this.form.graph.edges.push({ id: this.makeId('edge'), source: node.id, target: targetId, sourceHandle: handle });
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
        completeConnection(node) {
            if (!this.connectionDraft.sourceId || this.editingLocked) return;
            const source = this.form.graph.nodes.find(item => item.id === this.connectionDraft.sourceId);
            this.setTarget(source, this.connectionDraft.sourceHandle, node.id, false);
            this.cancelConnection();
        },
        cancelConnection() { this.connectionDraft = { sourceId: '', sourceHandle: '', x: 0, y: 0 }; },
        removeEdge(edge) {
            if (this.editingLocked) return;
            this.form.graph.edges = this.form.graph.edges.filter(item => item.id !== edge.id);
        },
        canvasPoint(event) {
            const canvas = this.$refs.canvas;
            if (!canvas) return { x: 0, y: 0 };
            const rect = canvas.getBoundingClientRect();
            return {
                x: event.clientX - rect.left + canvas.scrollLeft,
                y: event.clientY - rect.top + canvas.scrollTop
            };
        },
        startDrag(event, node) {
            if (this.editingLocked || event.button !== 0 || event.target.closest('button,.node-port')) return;
            const canvas = this.$refs.canvas;
            if (!canvas) return;
            const point = this.canvasPoint(event);
            this.dragState = { node, offsetX: point.x - Number(node.x), offsetY: point.y - Number(node.y) };
            this.selectNode(node);
            event.preventDefault();
        },
        onPointerMove(event) {
            if (this.dragState) {
                const point = this.canvasPoint(event);
                this.dragState.node.x = Math.max(20, point.x - this.dragState.offsetX);
                this.dragState.node.y = Math.max(50, point.y - this.dragState.offsetY);
                this.updateCanvasSize();
            }
            if (this.connectionDraft.sourceId) {
                const point = this.canvasPoint(event);
                this.connectionDraft.x = point.x;
                this.connectionDraft.y = point.y;
            }
        },
        onPointerUp() {
            this.dragState = null;
            if (this.connectionDraft.sourceId) window.setTimeout(() => this.cancelConnection(), 0);
        },
        edgeStart(edge) {
            const source = this.form.graph.nodes.find(node => node.id === edge.source);
            if (!source) return { x: 0, y: 0 };
            const offset = source.type === 'condition' ? (edge.sourceHandle === 'false' ? 145 : 75) : 110;
            return { x: Number(source.x) + offset, y: Number(source.y) + 92 };
        },
        edgeEnd(edge) {
            const target = this.form.graph.nodes.find(node => node.id === edge.target);
            return target ? { x: Number(target.x) + 110, y: Number(target.y) } : { x: 0, y: 0 };
        },
        curvePath(start, end) {
            const bend = Math.max(45, Math.abs(end.y - start.y) / 2);
            return `M ${start.x} ${start.y} C ${start.x} ${start.y + bend}, ${end.x} ${end.y - bend}, ${end.x} ${end.y}`;
        },
        edgePath(edge) { return this.curvePath(this.edgeStart(edge), this.edgeEnd(edge)); },
        draftEdgePath() {
            if (!this.connectionDraft.sourceId) return '';
            return this.curvePath(this.edgeStart({ source: this.connectionDraft.sourceId, sourceHandle: this.connectionDraft.sourceHandle }), this.connectionDraft);
        },
        edgeDeletePosition(edge) {
            const start = this.edgeStart(edge); const end = this.edgeEnd(edge);
            return { left: `${(start.x + end.x) / 2 - 11}px`, top: `${(start.y + end.y) / 2 - 11}px` };
        },
        edgeLabelX(edge) { const start = this.edgeStart(edge); return start.x + 5; },
        edgeLabelY(edge) { const start = this.edgeStart(edge); return start.y + 20; },
        autoLayout() {
            if (this.editingLocked || !this.form.graph.nodes.length) return;
            const nodes = this.form.graph.nodes;
            const trigger = nodes.find(node => String(node.type).endsWith('trigger')) || nodes[0];
            const level = { [trigger.id]: 0 };
            const queue = [trigger.id];
            while (queue.length) {
                const current = queue.shift();
                this.form.graph.edges.filter(edge => edge.source === current).forEach(edge => {
                    if (typeof level[edge.target] === 'undefined') {
                        level[edge.target] = (level[current] || 0) + 1;
                        queue.push(edge.target);
                    }
                });
            }
            let disconnectedLevel = Math.max(0, ...Object.values(level)) + 1;
            nodes.forEach(node => { if (typeof level[node.id] === 'undefined') level[node.id] = disconnectedLevel++; });
            const groups = {};
            nodes.forEach(node => { const key = level[node.id]; if (!groups[key]) groups[key] = []; groups[key].push(node); });
            Object.keys(groups).sort((a, b) => Number(a) - Number(b)).forEach(levelKey => {
                const group = groups[levelKey];
                const spacing = 270;
                const totalWidth = (group.length - 1) * spacing;
                group.forEach((node, index) => {
                    node.x = Math.max(40, 500 - totalWidth / 2 + index * spacing);
                    node.y = 60 + Number(levelKey) * 165;
                });
            });
            this.updateCanvasSize();
        },
        updateCanvasSize() {
            const maxX = Math.max(1100, ...this.form.graph.nodes.map(node => Number(node.x) + 320));
            const maxY = Math.max(680, ...this.form.graph.nodes.map(node => Number(node.y) + 210));
            this.canvasSize = { width: maxX, height: maxY };
        },
        nodeSummary(node) {
            if (node.type === 'function') { const fn = this.findFunction(node.config); return fn ? fn.moduleName : 'Function 已失效'; }
            if (node.type === 'interval-trigger') return `每 ${this.form.intervalSeconds} 秒`;
            if (node.type === 'manual-trigger') return '由用户手动运行';
            if (node.type === 'delay') return `${node.config.seconds || 0} 秒`;
            if (node.type === 'condition') return `${node.config.operator || 'equals'} ${this.configValueLabel(node.config.right)}`;
            if (node.type === 'aggregate') return '合并多个上游输出为数组';
            if (node.type === 'console') return '输出到下方 Console';
            if (node.type === 'end') return '流程在此结束';
            return node.type === 'agent-group' ? 'Agent 组' : node.type === 'agent' ? '独立 Agent' : node.type;
        },
        nodeState(node) { return this.run.nodeStates[node.id] || ''; },
        functionParameters(fn) { return NeuCharPivotUi.parseJson(fn.parameterSchemaJson, []); },
        findFunction(config) {
            return this.functions.find(fn => (Number(config.functionId || 0) > 0 && Number(fn.id) === Number(config.functionId)) ||
                (String(fn.moduleUid).toLowerCase() === String(config.moduleUid || '').toLowerCase() &&
                 String(fn.functionKey).toLowerCase() === String(config.functionKey || '').toLowerCase()));
        },
        functionIdentity(fn) { return `${String(fn.moduleUid).toLowerCase()}|${String(fn.functionKey).toLowerCase()}`; },
        loadPinnedFunctions() {
            try { this.pinnedFunctions = JSON.parse(localStorage.getItem('ncf.neucharpivot.workflow.pins') || '[]'); }
            catch { this.pinnedFunctions = []; }
        },
        isPinned(fn) { return this.pinnedFunctions.includes(this.functionIdentity(fn)); },
        togglePin(fn) {
            const key = this.functionIdentity(fn);
            this.pinnedFunctions = this.isPinned(fn)
                ? this.pinnedFunctions.filter(item => item !== key)
                : [...this.pinnedFunctions, key];
            localStorage.setItem('ncf.neucharpivot.workflow.pins', JSON.stringify(this.pinnedFunctions));
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
        nodeOutputFields(node, visited) {
            visited = visited || new Set();
            if (!node || visited.has(node.id)) return [{ path: '$', label: '节点输出', typeName: 'any', isArray: false, requiresIndex: false }];
            visited.add(node.id);
            if (node.type === 'function') {
                const fn = this.findFunction(node.config);
                return (fn && fn.output && fn.output.fields) || [{ path: '$', label: '完整输出', typeName: 'any', isArray: false, requiresIndex: false }];
            }
            if (node.type === 'aggregate') return [{ path: '$', label: '聚合结果', typeName: 'any', isArray: true, requiresIndex: false }];
            if (['manual-trigger', 'interval-trigger', 'agent', 'agent-group'].includes(node.type)) return [{ path: '$', label: '文本输出', typeName: 'string', isArray: false, requiresIndex: false }];
            const incoming = this.form.graph.edges.find(edge => edge.target === node.id);
            const source = incoming && this.form.graph.nodes.find(item => item.id === incoming.source);
            return source ? this.nodeOutputFields(source, visited) : [{ path: '$', label: '节点输出', typeName: 'any', isArray: false, requiresIndex: false }];
        },
        upstreamOutputOptions(parameter) {
            return this.upstreamNodes(this.selectedNode).map(node => ({
                value: node.id,
                label: node.name,
                children: this.nodeOutputFields(node).map(field => ({
                    value: field.path,
                    label: `${field.label} · ${field.typeName}${field.isArray ? '[]' : ''}`
                }))
            })).filter(option => option.children.length);
        },
        isBinding(value) { return !!(value && typeof value === 'object' && !Array.isArray(value) && value.$source); },
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
            const source = this.form.graph.nodes.find(item => item.id === selection[0]);
            const field = this.nodeOutputFields(source).find(item => item.path === selection[1]);
            this.$set(node.config.parameters, parameter.name, {
                $source: {
                    nodeId: source.id,
                    path: field.path,
                    sourceType: field.typeName,
                    isArray: !!field.isArray,
                    requiresIndex: !!field.requiresIndex,
                    collectionIndex: null,
                    itemIndex: null
                }
            });
        },
        resetParameterManual(node, parameter) {
            let value = parameter.parameterType === 2 ? [] : parameter.parameterType === 4 ? false : '';
            this.$set(node.config.parameters, parameter.name, value);
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
            const rawBinding = this.bindingFor(node, parameter);
            if (!rawBinding) return { level: 'manual', text: '手动输入' };
            const source = this.form.graph.nodes.find(item => item.id === rawBinding.nodeId);
            const field = source && this.nodeOutputFields(source).find(item => item.path === (rawBinding.path || '$'));
            if (!source || !this.upstreamNodes(node).some(item => item.id === source.id)) return { level: 'danger', text: '关联节点已不是有效上游节点' };
            if (!field) return { level: 'danger', text: '关联的输出字段已不存在' };
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
        setConfigBinding(node, key, selection) {
            if (!selection || selection.length < 2) { this.$set(node.config, key, ''); return; }
            const source = this.form.graph.nodes.find(item => item.id === selection[0]);
            const field = this.nodeOutputFields(source).find(item => item.path === selection[1]);
            this.$set(node.config, key, { $source: { nodeId: source.id, path: field.path, sourceType: field.typeName, isArray: !!field.isArray, requiresIndex: !!field.requiresIndex, collectionIndex: null, itemIndex: null } });
        },
        configValueLabel(value) { return this.isBinding(value) ? `关联：${value.$source.nodeId}` : String(value || ''); },
        validate() {
            if (!this.form.name.trim()) return '请输入工作流名称。';
            const triggers = this.form.graph.nodes.filter(node => String(node.type).endsWith('trigger'));
            if (triggers.length !== 1) return '工作流必须且只能包含一个触发器。';
            const trigger = triggers[0];
            const visited = new Set(); const queue = [trigger.id];
            while (queue.length) {
                const current = queue.shift(); if (visited.has(current)) continue; visited.add(current);
                this.form.graph.edges.filter(edge => edge.source === current).forEach(edge => queue.push(edge.target));
            }
            if (visited.size !== this.form.graph.nodes.length) return '画布中仍有未连接到触发器的节点。';
            for (const node of this.form.graph.nodes) {
                const incoming = this.incomingEdges(node.id);
                if (!this.supportsMultipleInputs(node) && incoming.length > 1) return `节点“${node.name}”只允许一个上游；多对一目标请使用 Function 或聚合节点。`;
            }
            for (const node of this.form.graph.nodes.filter(item => item.type === 'function')) {
                const fn = this.findFunction(node.config);
                if (!fn || !fn.moduleAvailable) return `节点“${node.name}”引用的模块未开启或 Function 已移除。`;
                const missing = NeuCharPivotUi.firstMissingRequired(fn, node.config.parameters || {});
                if (missing) return `节点“${node.name}”缺少必填参数“${missing.title || missing.name}”。`;
                for (const parameter of this.functionParameters(fn)) {
                    const compatibility = this.bindingCompatibility(node, parameter);
                    if (compatibility.level === 'danger' || compatibility.level === 'warning') return `节点“${node.name}”参数“${parameter.title || parameter.name}”：${compatibility.text}`;
                }
            }
            for (const node of this.form.graph.nodes.filter(item => item.type === 'agent' || item.type === 'agent-group')) {
                const object = this.workflowObjects.find(item => item.providerId === node.config.providerId && item.objectId === node.config.objectId);
                if (!object || !object.enabled) return `节点“${node.name}”引用的 Agent 不可用。`;
            }
            return '';
        },
        setAutoSaveEnabled(enabled) {
            this.form.autoSaveMinutes = enabled
                ? Math.max(1, Number(this.form.autoSaveMinutes || 3))
                : 0;
        },
        resetSaveState() {
            this.clearAutoSaveTimer();
            this.saveState.saving = false;
            this.saveState.lastSavedSignature = '';
            this.saveState.lastSavedLabel = '';
            this.saveState.status = 'idle';
            this.saveState.error = '';
        },
        markSaved() {
            this.saveState.lastSavedSignature = this.currentSaveSignature;
            this.saveState.lastSavedLabel = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            this.saveState.status = 'saved';
            this.saveState.error = '';
            this.scheduleAutoSave();
        },
        clearAutoSaveTimer() {
            if (this.saveState.timer) window.clearTimeout(this.saveState.timer);
            this.saveState.timer = null;
        },
        scheduleAutoSave() {
            this.clearAutoSaveTimer();
            const minutes = Number(this.form.autoSaveMinutes || 0);
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
        applySavedWorkflow(saved) {
            this.form.id = Number(saved.id || this.form.id || 0);
            this.form.revision = Number(saved.revision || this.form.revision || 0);
            this.form.autoSaveMinutes = Number(saved.autoSaveMinutes ?? this.form.autoSaveMinutes ?? 3);
            if (saved.graphJson) {
                const graph = NeuCharPivotUi.parseJson(saved.graphJson, this.form.graph);
                graph.nodes = graph.nodes || [];
                graph.edges = graph.edges || [];
                this.form.graph = graph;
            }
            this.markSaved();
        },
        async saveWorkflow(options) {
            options = options || {};
            if (this.editingLocked || this.saveState.saving) return null;
            if (this.form.id && !this.saveDirty) {
                if (!options.silent) this.$notify({ title: 'Workflow', message: '当前没有需要保存的更改。', type: 'info' });
                this.scheduleAutoSave();
                return { id: this.form.id, revision: this.form.revision, unchanged: true };
            }
            const error = this.validate();
            if (error) {
                this.saveState.status = 'error';
                this.saveState.error = error;
                if (!options.automatic) this.$notify({ title: '无法保存', message: error, type: 'warning' });
                return null;
            }
            this.saveState.saving = true;
            this.saveState.status = 'saving';
            this.saveState.error = '';
            try {
                const response = await service.post('/Admin/NeuCharPivot/Workflow?handler=Save', {
                    id: this.form.id || 0,
                    name: this.form.name,
                    description: this.form.description,
                    graphJson: JSON.stringify(this.form.graph),
                    enabled: !!this.form.enabled,
                    triggerType: this.form.triggerType,
                    triggerConfigJson: JSON.stringify({ intervalSeconds: Number(this.form.intervalSeconds || 300) }),
                    autoSaveMinutes: Number(this.form.autoSaveMinutes || 0),
                    expectedRevision: this.form.id ? Number(this.form.revision || 0) : null,
                    saveSource: options.source || 'manual'
                }, { customAlert: true });
                const saved = NeuCharPivotUi.unwrap(response);
                this.applySavedWorkflow(saved);
                if (!options.silent) this.$notify({ title: 'Workflow', message: options.source === 'shortcut' ? '已使用快捷键保存。' : '工作流已保存。', type: 'success' });
                await this.loadAll();
                return saved;
            } catch (error) {
                const message = this.errorMessage(error, '请检查节点配置。');
                this.saveState.status = 'error';
                this.saveState.error = message;
                if (!options.automatic) this.$notify({ title: '保存失败', message, type: 'error' });
                return null;
            } finally { this.saveState.saving = false; }
        },
        async startWorkflow() {
            if (this.run.running || this.run.validating) return;
            const localError = this.validate();
            if (localError) { this.appendConsole('validation', localError, 'failed'); this.$notify({ title: '运行前校验失败', message: localError, type: 'warning' }); return; }
            this.run.validating = true;
            this.run.error = '';
            this.run.finalOutput = '';
            this.run.events = [];
            this.run.nodeStates = {};
            this.appendConsole('validation', '正在保存并校验当前工作流……', 'running');
            try {
                const saved = await this.saveWorkflow({ silent: true });
                if (!saved) return;
                await service.post('/Admin/NeuCharPivot/Workflow?handler=ValidateRun', { id: this.form.id, input: this.run.input }, { customAlert: true });
                this.appendConsole('validation', '参数、引用和类型校验通过。', 'success');
                const response = await service.post('/Admin/NeuCharPivot/Workflow?handler=StartRun', { id: this.form.id, input: this.run.input }, { customAlert: true });
                const data = NeuCharPivotUi.unwrap(response) || {};
                this.run.runId = data.runId;
                this.run.running = true;
                this.run.status = 'running';
                this.run.lastSequence = 0;
                this.pollRun();
            } catch (error) {
                const message = this.errorMessage(error, '运行前校验失败。');
                this.run.status = 'failed'; this.run.error = message;
                this.appendConsole('validation', message, 'failed');
                this.$notify({ title: '无法运行', message, type: 'error' });
            } finally { this.run.validating = false; }
        },
        async pollRun() {
            if (!this.run.runId) return;
            try {
                const response = await service.get(`/Admin/NeuCharPivot/Workflow?handler=RunStatus&runId=${encodeURIComponent(this.run.runId)}&afterSequence=${this.run.lastSequence}`);
                const snapshot = NeuCharPivotUi.unwrap(response) || {};
                (snapshot.events || []).forEach(event => {
                    this.run.lastSequence = Math.max(this.run.lastSequence, Number(event.sequence || 0));
                    this.applyRunEvent(event);
                });
                if (snapshot.running) {
                    this.run.pollTimer = window.setTimeout(this.pollRun, 450);
                    return;
                }
                this.run.running = false;
                this.run.status = snapshot.succeeded ? 'success' : 'failed';
                this.run.finalOutput = snapshot.finalOutput || '';
                this.run.error = snapshot.errorMessage || '';
                this.appendConsole('workflow', snapshot.succeeded ? '工作流运行完成。' : (snapshot.errorMessage || '工作流运行失败。'), snapshot.succeeded ? 'success' : 'failed', snapshot.finalOutput);
            } catch (error) {
                this.run.running = false; this.run.status = 'failed';
                this.run.error = this.errorMessage(error, '读取运行状态失败。');
                this.appendConsole('workflow', this.run.error, 'failed');
            }
        },
        applyRunEvent(event) {
            if (event.nodeId && ['running', 'success', 'failed'].includes(event.status)) this.$set(this.run.nodeStates, event.nodeId, event.status);
            this.run.events.push(event);
            if (this.run.events.length > 500) this.run.events.splice(0, this.run.events.length - 500);
            this.$nextTick(() => { const el = this.$refs.consoleLog; if (el) el.scrollTop = el.scrollHeight; });
        },
        appendConsole(nodeName, message, status, output) {
            this.applyRunEvent({ sequence: 0, nodeId: '', nodeName, message, status, output: output || '', timestamp: new Date().toISOString() });
        },
        clearConsole() { if (!this.run.running) this.run.events = []; },
        clearRunPoll() { if (this.run.pollTimer) window.clearTimeout(this.run.pollTimer); this.run.pollTimer = null; },
        resetRunState() {
            this.clearRunPoll();
            this.run.running = false; this.run.validating = false; this.run.runId = ''; this.run.status = 'idle';
            this.run.events = []; this.run.lastSequence = 0; this.run.nodeStates = {}; this.run.finalOutput = ''; this.run.error = '';
        },
        errorMessage(error, fallback) {
            const data = error && error.response && error.response.data;
            return String((data && (data.title || data.detail || data)) || fallback);
        },
        async deleteWorkflow() {
            if (this.editingLocked) return;
            await service.post('/Admin/NeuCharPivot/Workflow?handler=Delete', { id: this.form.id }, { customAlert: true });
            this.form = this.emptyForm(); this.editing = false; this.resetSaveState(); this.resetRunState(); await this.loadAll();
        }
    }
});
