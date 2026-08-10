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
            paletteCollapsed: true,
            inspectorCollapsed: true,
            paletteModule: '',
            paletteSearch: '',
            pinnedFunctions: [],
            editing: false,
            discardConfirming: false,
            webhookHelpVisible: false,
            workflowSettingsVisible: false,
            templateEditorPlaceholder: '例如：请根据 {{value_1}} 生成一段摘要',
            templateEditorBindingHelp: '删除一个变量标签时，它在文本中的对应占位符也会一并删除。文本参数可以继续使用 {{input}} 引用触发器输入。',
            templateEditor: {
                visible: false,
                nodeId: '',
                parameterName: '',
                text: '',
                bindings: [],
                pendingSelection: []
            },
            selectedNodeId: '',
            connectionDraft: { sourceId: '', sourceHandle: '', x: 0, y: 0 },
            dragState: null,
            canvasPan: { active: false, startX: 0, startY: 0, startScrollLeft: 0, startScrollTop: 0 },
            contextMenu: { visible: false, x: 0, y: 0, node: null },
            canvasSize: { width: 1200, height: 760 },
            canvasZoom: 1,
            canvasViewport: { width: 0, height: 0, scrollLeft: 0, scrollTop: 0, left: 0, right: 0, bottom: 0, windowWidth: 0, windowHeight: 0 },
            form: { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, webhookMethod: 'any', webhookToken: '', webhookParameters: [], autoSaveMinutes: 3, revision: 0, graph: { nodes: [], edges: [] } },
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
                consoleOpen: false
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
        selectedWorkflowObject() {
            if (!this.selectedNode || !['agent', 'agent-group'].includes(this.selectedNode.type)) return null;
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
            if (this.saveState.saving) return '正在保存…';
            if (this.saveState.status === 'error') return this.saveState.error || '保存失败';
            if (this.saveDirty) return '有尚未保存的更改';
            if (this.disconnectedNodes.length) return `草稿：${this.disconnectedNodes.length} 个未连接节点`;
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
        },
        webhookUrl() {
            return this.form.id
                ? `${window.location.origin}/api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/webhook/${this.form.id}`
                : '';
        },
        canvasZoomPercent() { return Math.round(this.canvasZoom * 100); },
        scaledCanvasSize() {
            return {
                width: Math.max(1, Math.round(this.canvasSize.width * this.canvasZoom)),
                height: Math.max(1, Math.round(this.canvasSize.height * this.canvasZoom))
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
            const worldLeft = Math.max(0, Number(viewport.scrollLeft || 0) / this.canvasZoom);
            const worldTop = Math.max(0, Number(viewport.scrollTop || 0) / this.canvasZoom);
            const width = Math.min(metrics.width, Number(viewport.width || 0) / this.canvasZoom * metrics.scale);
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
        }
    },
    watch: {
        'form.autoSaveMinutes'() { this.scheduleAutoSave(); },
        listCollapsed() { this.refreshCanvasViewport(); },
        paletteCollapsed() { this.refreshCanvasViewport(); },
        inspectorCollapsed() { this.refreshCanvasViewport(); }
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
        if (this.canvasResizeObserver) this.canvasResizeObserver.disconnect();
        this.clearAutoSaveTimer();
        this.clearRunPoll();
    },
    methods: {
        emptyForm() {
            return { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, webhookMethod: 'any', webhookToken: '', webhookParameters: [], autoSaveMinutes: 3, revision: 0, graph: { nodes: [], edges: [] } };
        },
        async loadAll() {
            this.loading = true;
            try {
                const [listResponse, dataResponse] = await Promise.all([
                    service.get('/Admin/NeuCharWorkflow/Index?handler=List'),
                    service.get('/Admin/NeuCharWorkflow/Index?handler=DesignerData')
                ]);
                this.workflows = NeuCharWorkflowUi.unwrap(listResponse) || [];
                const data = NeuCharWorkflowUi.unwrap(dataResponse) || {};
                this.functions = data.functions || [];
                this.workflowObjects = data.objects || [];
            } finally { this.loading = false; }
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
        async createWorkflow() {
            if (this.editingLocked || this.saveState.saving || !await this.confirmDiscardChanges('新建工作流')) return;
            this.form = this.emptyForm();
            this.editing = true;
            this.workflowSettingsVisible = false;
            this.webhookHelpVisible = false;
            this.selectedNodeId = '';
            this.resetSaveState();
            this.cancelConnection();
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
                    autoSaveMinutes: Number(item.autoSaveMinutes ?? 3),
                    revision: Number(item.revision || 0)
                };
                this.editing = true;
                this.selectedNodeId = graph.nodes.length ? graph.nodes[0].id : '';
                this.cancelConnection();
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
                : this.form.triggerType === 'webhook' ? 'webhook-trigger' : 'manual-trigger';
            const existing = this.form.graph.nodes.find(node => String(node.type).endsWith('trigger'));
            if (existing) {
                existing.type = type;
                existing.name = type === 'interval-trigger' ? '间隔触发' : type === 'webhook-trigger' ? 'Webhook 触发' : '手动触发';
                existing.config = existing.config || {};
                if (type === 'webhook-trigger') {
                    existing.config.webhookParameters = (this.form.webhookParameters || []).map(parameter => ({ name: parameter.name, required: !!parameter.required, description: parameter.description || '' }));
                } else {
                    delete existing.config.webhookParameters;
                }
            } else {
                const trigger = { id: this.makeId('trigger'), type, name: type === 'interval-trigger' ? '间隔触发' : type === 'webhook-trigger' ? 'Webhook 触发' : '手动触发', x: 430, y: 60, config: type === 'webhook-trigger' ? { webhookParameters: [] } : {} };
                this.form.graph.nodes.unshift(trigger);
                this.selectedNodeId = trigger.id;
            }
            this.updateCanvasSize();
        },
        addSimpleNode(type, name) {
            if (this.editingLocked) return;
            const config = type === 'condition'
                ? { left: '{{input}}', operator: 'equals', right: '' }
                : type === 'delay' ? { seconds: 1 }
                    : type === 'neubell'
                        ? { title: 'Workflow 提醒', summary: '{{input}}', consumeMode: 'item' }
                        : {};
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
                    parameters: NeuCharWorkflowUi.createParameterValues(fn)
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
        canDeleteNode(node) {
            return !!node && !String(node.type || '').endsWith('trigger');
        },
        canDuplicateNode(node) {
            return !!node && !String(node.type || '').endsWith('trigger');
        },
        duplicateNode(node) {
            if (this.editingLocked || !this.canDuplicateNode(node)) return false;
            const copy = JSON.parse(JSON.stringify(node));
            copy.id = this.makeId(node.type || 'node');
            copy.name = `${node.name || '节点'}（副本）`;
            copy.x = Number(node.x || 0) + 40;
            copy.y = Number(node.y || 0) + 40;
            this.form.graph.nodes.push(copy);
            this.selectedNodeId = copy.id;
            this.cancelConnection();
            this.updateCanvasSize();
            this.scheduleAutoSave();
            return copy;
        },
        removeNode(node) {
            if (this.editingLocked || String(node.type).endsWith('trigger')) return;
            this.form.graph.nodes = this.form.graph.nodes.filter(item => item.id !== node.id);
            this.form.graph.edges = this.form.graph.edges.filter(edge => edge.source !== node.id && edge.target !== node.id);
            this.selectedNodeId = '';
            this.cancelConnection();
            this.updateCanvasSize();
            this.scheduleAutoSave();
        },
        openNodeContextMenu(event, node) {
            const documentElement = typeof document !== 'undefined' ? document.documentElement : null;
            const viewportWidth = Number(window.innerWidth || (documentElement && documentElement.clientWidth) || 0);
            const viewportHeight = Number(window.innerHeight || (documentElement && documentElement.clientHeight) || 0);
            const menuWidth = 156;
            const menuHeight = 92;
            this.contextMenu = {
                visible: true,
                x: Math.max(0, viewportWidth ? Math.min(event.clientX, viewportWidth - menuWidth) : event.clientX),
                y: Math.max(0, viewportHeight ? Math.min(event.clientY, viewportHeight - menuHeight) : event.clientY),
                node
            };
            this.selectedNodeId = node.id;
        },
        duplicateContextNode() {
            const node = this.contextMenu.node;
            this.closeContextMenu();
            this.duplicateNode(node);
        },
        removeContextNode() {
            const node = this.contextMenu.node;
            this.closeContextMenu();
            if (node && this.canDeleteNode(node)) this.removeNode(node);
        },
        closeContextMenu() {
            this.contextMenu.visible = false;
            this.contextMenu.node = null;
        },
        selectNode(node) {
            this.closeContextMenu();
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
        refreshCanvasViewport() {
            if (typeof this.$nextTick === 'function') this.$nextTick(() => this.updateCanvasViewport());
            else this.updateCanvasViewport();
        },
        updateCanvasViewport() {
            const canvas = this.$refs && this.$refs.canvas;
            if (!canvas) return;
            const rect = canvas.getBoundingClientRect();
            this.canvasViewport = {
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
            const localX = Number.isFinite(clientX) ? Math.max(0, Math.min(canvas.clientWidth, clientX - rect.left)) : canvas.clientWidth / 2;
            const localY = Number.isFinite(clientY)
                ? Math.max(0, Math.min(canvas.clientHeight, clientY - rect.top))
                : (canvas.clientHeight + Math.min(canvas.clientHeight, stageContentTop)) / 2;
            const worldX = (canvas.scrollLeft + localX) / currentZoom;
            const worldY = (canvas.scrollTop - stageContentTop + localY) / currentZoom;
            const nextScrollLeft = Math.max(0, worldX * nextZoom - localX);
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
            this.dragState = null;
            this.cancelConnection();
            this.canvasPan = {
                active: true,
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
            const point = this.canvasPoint(event);
            this.dragState = { node, offsetX: point.x - Number(node.x), offsetY: point.y - Number(node.y) };
            this.selectNode(node);
            event.preventDefault();
        },
        onPointerMove(event) {
            if (this.canvasPan.active) {
                const canvas = this.$refs.canvas;
                if (canvas) {
                    canvas.scrollLeft = Math.max(0, this.canvasPan.startScrollLeft - (event.clientX - this.canvasPan.startX));
                    canvas.scrollTop = Math.max(0, this.canvasPan.startScrollTop - (event.clientY - this.canvasPan.startY));
                }
            }
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
            this.canvasPan.active = false;
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
            const viewportWidth = Number(canvas.clientWidth || 0);
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
            canvas.scrollLeft = Math.max(0, worldX * this.canvasZoom - canvas.clientWidth / 2);
            canvas.scrollTop = Math.max(0, stageContentTop + worldY * this.canvasZoom - (canvas.clientHeight + stageContentTop) / 2);
            this.updateCanvasViewport();
        },
        nodeSummary(node) {
            if (node.type === 'function') { const fn = this.findFunction(node.config); return fn ? fn.moduleName : 'Function 已失效'; }
            if (node.type === 'interval-trigger') return `每 ${this.form.intervalSeconds} 秒`;
            if (node.type === 'webhook-trigger') return '等待外部 Webhook 请求';
            if (node.type === 'manual-trigger') return '由用户手动运行';
            if (node.type === 'delay') return `${node.config.seconds || 0} 秒`;
            if (node.type === 'condition') return `${node.config.operator || 'equals'} ${this.configValueLabel(node.config.right)}`;
            if (node.type === 'aggregate') return '合并多个上游输出为数组';
            if (node.type === 'console') return '输出到下方 Console';
            if (node.type === 'neubell') {
                const mode = String(node.config?.consumeMode || 'none');
                return mode === 'provider' ? '点击后消费本订阅全部提醒'
                    : mode === 'item' ? '点击后消费当前提醒' : '点击后仅查看任务';
            }
            if (node.type === 'end') return '流程在此结束';
            return node.type === 'agent-group' ? 'Agent 组' : node.type === 'agent' ? '独立 Agent' : node.type;
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
            }
            return '';
        },
        workflowObjectInfo(object) {
            if (!object) return [];
            const metadata = object.metadata || {};
            const rows = [
                { label: '类型', value: metadata.type || (object.kind === 'agent-group' ? 'Agent 组' : '独立 Agent') },
                { label: '状态', value: object.enabled ? '可用 / 已启用' : '不可用 / 已停用' },
                { label: '说明', value: object.description || '' }
            ];
            if (metadata.promptCode) rows.push({ label: 'Prompt Code', value: metadata.promptCode });
            if (metadata.functionCallNames) rows.push({ label: 'Function Calls', value: metadata.functionCallNames });
            if (metadata.knowledgeBaseId) rows.push({ label: '知识库 ID', value: metadata.knowledgeBaseId });
            if (metadata.state) rows.push({ label: '组状态', value: metadata.state });
            return rows;
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
        nodeOutputFields(node, visited) {
            visited = visited || new Set();
            if (!node || visited.has(node.id)) return [{ path: '$', label: '节点输出', typeName: 'any', isArray: false, requiresIndex: false }];
            visited.add(node.id);
            if (node.type === 'function') {
                const fn = this.findFunction(node.config);
                const outputFields = (fn && fn.output && Array.isArray(fn.output.fields))
                    ? fn.output.fields
                    : [{ path: '$', label: '完整输出', typeName: 'any', isArray: false, requiresIndex: false }];
                return [...outputFields, ...this.functionSelectionInputFields(fn)];
            }
            if (node.type === 'aggregate') return [{ path: '$', label: '聚合结果', typeName: 'any', isArray: true, requiresIndex: false }];
            if (node.type === 'webhook-trigger') {
                const parameters = this.form.webhookParameters || [];
                return parameters.length
                    ? parameters.filter(parameter => String(parameter.name || '').trim()).map(parameter => ({ path: `$.${String(parameter.name).trim()}`, label: parameter.name, typeName: 'any', isArray: false, requiresIndex: false }))
                    : [{ path: '$', label: 'Webhook 输入', typeName: 'object', isArray: false, requiresIndex: false }];
            }
            if (['manual-trigger', 'interval-trigger', 'webhook-trigger', 'agent', 'agent-group'].includes(node.type)) return [{ path: '$', label: '文本输出', typeName: 'string', isArray: false, requiresIndex: false }];
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
                    label: `${field.label} · ${field.typeName}${field.isArray ? '[]' : ''}`
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
        openParameterTemplateEditor(node, parameter) {
            if (this.editingLocked || !node || !parameter || !this.canUseTemplate(parameter)) return;
            const currentValue = node.config.parameters?.[parameter.name];
            const template = this.templateFor(currentValue);
            this.templateEditor = {
                visible: true,
                nodeId: node.id,
                parameterName: parameter.name,
                text: template ? String(template.text || '') : (typeof currentValue === 'string' ? currentValue : ''),
                bindings: template && Array.isArray(template.bindings)
                    ? template.bindings.filter(item => item && item.source).map(item => ({
                        token: String(item.token || ''),
                        source: { ...item.source }
                    }))
                    : [],
                pendingSelection: []
            };
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
        removeTemplateBinding(token) {
            const placeholder = `{{${token}}}`;
            this.templateEditor.text = String(this.templateEditor.text || '').split(placeholder).join('');
            this.templateEditor.bindings = this.templateEditor.bindings.filter(item => item.token !== token);
        },
        saveParameterTemplate() {
            const editor = this.templateEditor;
            const node = this.form.graph.nodes.find(item => item.id === editor.nodeId);
            if (!node || !editor.parameterName || !node.config.parameters) {
                editor.visible = false;
                return;
            }
            const text = String(editor.text || '');
            const bindings = editor.bindings.map(item => ({ token: item.token, source: { ...item.source } }));
            this.$set(node.config.parameters, editor.parameterName,
                bindings.length ? { $template: { text, bindings } } : text);
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
            const value = node && node.config?.parameters?.[parameter.name];
            if (this.isTemplateValue(value)) {
                if (!this.canUseTemplate(parameter)) return { level: 'danger', text: '此参数不支持在文本中嵌入变量' };
                const bindings = this.parameterTemplateBindings(value);
                if (!bindings.length) return { level: 'manual', text: '手动输入' };
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
                    : { level: 'success', text: `文本中嵌入 ${bindings.length} 个上游值` };
            }
            const rawBinding = this.bindingFor(node, parameter);
            if (!rawBinding) return { level: 'manual', text: '手动输入' };
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
        setConfigBinding(node, key, selection) {
            if (!selection || selection.length < 2) { this.$set(node.config, key, ''); return; }
            const source = this.form.graph.nodes.find(item => item.id === selection[0]);
            const field = this.nodeOutputFields(source).find(item => item.path === selection[1]);
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
            const triggers = this.form.graph.nodes.filter(node => String(node.type).endsWith('trigger'));
            if (triggers.length !== 1) return '工作流必须且只能包含一个触发器。';
            const trigger = triggers[0];
            if (!['manual-trigger', 'interval-trigger', 'webhook-trigger'].includes(trigger.type)) return '触发器节点类型无效。';
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
            const disconnected = this.getDisconnectedNodes();
            if (requireRunnable && disconnected.length) return '画布中仍有未连接到触发器的节点。';
            for (const node of this.form.graph.nodes) {
                const incoming = this.incomingEdges(node.id);
                if (!this.supportsMultipleInputs(node) && incoming.length > 1) return `节点“${node.name}”只允许一个上游；多对一目标请使用 Function 或聚合节点。`;
            }
            if (!requireRunnable) return '';
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
            for (const node of this.form.graph.nodes.filter(item => item.type === 'agent' || item.type === 'agent-group')) {
                const object = this.workflowObjects.find(item => item.providerId === node.config.providerId && item.objectId === node.config.objectId);
                if (!object || !object.enabled) return `节点“${node.name}”引用的 Agent 不可用。`;
            }
            return '';
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
            return {};
        },
        async copyText(value) {
            if (!value) return;
            try {
                await navigator.clipboard.writeText(value);
                this.$notify({ title: 'Webhook', message: '已复制到剪贴板。', type: 'success' });
            } catch {
                this.$notify({ title: 'Webhook', message: '复制失败，请手动复制。', type: 'warning' });
            }
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
            if (saved.graphJson) {
                const graph = NeuCharWorkflowUi.parseJson(saved.graphJson, this.form.graph);
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
            const error = this.validate({ requireRunnable: false });
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
                this.syncTriggerNode();
                const response = await service.post('/Admin/NeuCharWorkflow/Index?handler=Save', {
                    id: this.form.id || 0,
                    name: this.form.name,
                    description: this.form.description,
                    graphJson: JSON.stringify(this.form.graph),
                    enabled: !!this.form.enabled,
                    triggerType: this.form.triggerType,
                    triggerConfigJson: JSON.stringify(this.buildTriggerConfig()),
                    autoSaveMinutes: Number(this.form.autoSaveMinutes || 0),
                    expectedRevision: this.form.id ? Number(this.form.revision || 0) : null,
                    saveSource: options.source || 'manual'
                }, { customAlert: true });
                const saved = NeuCharWorkflowUi.unwrap(response);
                this.applySavedWorkflow(saved);
                if (!options.silent) {
                    const draftCount = this.disconnectedNodes.length;
                    const message = draftCount
                        ? `草稿已保存。${draftCount} 个未连接节点会阻止运行；已自动停用工作流，请连接完成后重新启用。`
                        : (options.source === 'shortcut' ? '已使用快捷键保存。' : '工作流已保存。');
                    this.$notify({ title: 'Workflow', message, type: draftCount ? 'warning' : 'success' });
                }
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
            const localError = this.validate({ requireRunnable: true });
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
                const message = this.errorMessage(error, '运行前校验失败。');
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
            this.form = this.emptyForm(); this.editing = false; this.resetSaveState(); this.resetRunState(); await this.loadAll();
        }
    }
});
