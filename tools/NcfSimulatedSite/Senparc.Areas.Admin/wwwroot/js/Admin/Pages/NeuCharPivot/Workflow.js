new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            workflows: [],
            functions: [],
            workflowObjects: [],
            keyword: '',
            paletteModule: '',
            editing: false,
            selectedNodeId: '',
            connectionDraft: { sourceId: '', sourceHandle: '' },
            dragState: null,
            canvasSize: { width: 1200, height: 760 },
            form: { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, graph: { nodes: [], edges: [] } },
            run: { visible: false, loading: false, input: '', result: '' }
        };
    },
    computed: {
        filteredWorkflows() {
            const keyword = this.keyword.trim().toLowerCase();
            return keyword ? this.workflows.filter(item => String(item.name).toLowerCase().includes(keyword)) : this.workflows;
        },
        moduleNames() { return [...new Set(this.functions.map(fn => fn.moduleName))].sort(); },
        filteredFunctions() { return this.paletteModule ? this.functions.filter(fn => fn.moduleName === this.paletteModule) : this.functions; },
        selectedNode() { return this.form.graph.nodes.find(node => node.id === this.selectedNodeId); },
        selectedFunction() {
            return this.selectedNode && this.selectedNode.type === 'function'
                ? this.functions.find(fn => fn.id === Number(this.selectedNode.config.functionId))
                : null;
        },
        connectionSourceName() {
            const node = this.form.graph.nodes.find(item => item.id === this.connectionDraft.sourceId);
            return node ? node.name : '';
        }
    },
    created() { this.loadAll(); },
    mounted() {
        window.addEventListener('mousemove', this.onDrag);
        window.addEventListener('mouseup', this.stopDrag);
    },
    beforeDestroy() {
        window.removeEventListener('mousemove', this.onDrag);
        window.removeEventListener('mouseup', this.stopDrag);
    },
    methods: {
        emptyForm() {
            return { id: 0, name: '', description: '', enabled: false, triggerType: 'manual', intervalSeconds: 300, graph: { nodes: [], edges: [] } };
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
            this.form = this.emptyForm();
            this.editing = true;
            this.selectedNodeId = '';
            this.connectionDraft = { sourceId: '', sourceHandle: '' };
            this.syncTriggerNode();
            this.$nextTick(this.autoLayout);
        },
        async editWorkflow(id) {
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
                this.form = { ...item, graph, intervalSeconds: Number(trigger.intervalSeconds || 300) };
                this.editing = true;
                this.selectedNodeId = graph.nodes.length ? graph.nodes[0].id : '';
                this.connectionDraft = { sourceId: '', sourceHandle: '' };
                if (graph.nodes.length > 1 && graph.nodes.every(node => Number(node.x) === 0)) {
                    this.$nextTick(this.autoLayout);
                } else {
                    this.updateCanvasSize();
                }
            } finally { this.loading = false; }
        },
        makeId(prefix) { return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}`; },
        syncTriggerNode() {
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
            const config = type === 'condition'
                ? { left: '{{input}}', operator: 'equals', right: '' }
                : type === 'delay' ? { seconds: 1 } : {};
            this.appendNode({ id: this.makeId(type), type, name, x: 80, y: 80, config });
        },
        addFunctionNode(fn) {
            if (!fn.moduleAvailable) return;
            this.appendNode({
                id: this.makeId('function'),
                type: 'function',
                name: fn.functionName,
                x: 80,
                y: 80,
                config: { functionId: fn.id, parameters: NeuCharPivotUi.createParameterValues(fn) }
            });
        },
        addObjectNode(object) {
            if (!object.enabled) return;
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
            if (previous && !String(node.type).endsWith('trigger')) {
                this.setTarget(previous, 'default', node.id);
            }
            this.selectedNodeId = node.id;
            this.autoLayout();
        },
        removeNode(node) {
            if (String(node.type).endsWith('trigger')) return;
            this.form.graph.nodes = this.form.graph.nodes.filter(item => item.id !== node.id);
            this.form.graph.edges = this.form.graph.edges.filter(edge => edge.source !== node.id && edge.target !== node.id);
            this.selectedNodeId = '';
            this.cancelConnection();
            this.updateCanvasSize();
        },
        selectNode(node) { this.selectedNodeId = node.id; },
        targetFor(node, sourceHandle) {
            const edge = this.form.graph.edges.find(item => item.source === node.id && item.sourceHandle === sourceHandle);
            return edge ? edge.target : '';
        },
        availableTargets(node) {
            return this.form.graph.nodes.filter(target =>
                target.id !== node.id &&
                !String(target.type).endsWith('trigger') &&
                !this.wouldCreateCycle(node.id, target.id));
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
        setTarget(node, sourceHandle, targetId) {
            const handle = node.type === 'condition' ? sourceHandle : 'default';
            if (targetId && this.wouldCreateCycle(node.id, targetId)) {
                this.$notify({ title: '无法连接', message: '此连接会形成循环，请改用 Loop Task 或间隔触发器。', type: 'warning' });
                return;
            }
            this.form.graph.edges = this.form.graph.edges.filter(edge =>
                !(edge.source === node.id && edge.sourceHandle === handle));
            if (targetId) {
                this.form.graph.edges.push({ id: this.makeId('edge'), source: node.id, target: targetId, sourceHandle: handle });
            }
        },
        beginConnection(node, sourceHandle) {
            if (node.type === 'end') return;
            this.connectionDraft = { sourceId: node.id, sourceHandle: node.type === 'condition' ? sourceHandle : 'default' };
        },
        connectTo(node) {
            if (!this.connectionDraft.sourceId) return;
            const source = this.form.graph.nodes.find(item => item.id === this.connectionDraft.sourceId);
            if (source && this.availableTargets(source).some(item => item.id === node.id)) {
                this.setTarget(source, this.connectionDraft.sourceHandle, node.id);
            }
            this.cancelConnection();
        },
        cancelConnection() { this.connectionDraft = { sourceId: '', sourceHandle: '' }; },
        startDrag(event, node) {
            if (event.button !== 0 || event.target.closest('button,.node-port')) return;
            const canvas = this.$refs.canvas;
            if (!canvas) return;
            const rect = canvas.getBoundingClientRect();
            this.dragState = {
                node,
                offsetX: event.clientX - rect.left + canvas.scrollLeft - Number(node.x),
                offsetY: event.clientY - rect.top + canvas.scrollTop - Number(node.y)
            };
            this.selectNode(node);
            event.preventDefault();
        },
        onDrag(event) {
            if (!this.dragState || !this.$refs.canvas) return;
            const canvas = this.$refs.canvas;
            const rect = canvas.getBoundingClientRect();
            this.dragState.node.x = Math.max(20, event.clientX - rect.left + canvas.scrollLeft - this.dragState.offsetX);
            this.dragState.node.y = Math.max(50, event.clientY - rect.top + canvas.scrollTop - this.dragState.offsetY);
            this.updateCanvasSize();
        },
        stopDrag() { this.dragState = null; },
        edgePath(edge) {
            const source = this.form.graph.nodes.find(node => node.id === edge.source);
            const target = this.form.graph.nodes.find(node => node.id === edge.target);
            if (!source || !target) return '';
            const sourceOffset = source.type === 'condition' ? (edge.sourceHandle === 'false' ? 145 : 75) : 110;
            const x1 = Number(source.x) + sourceOffset;
            const y1 = Number(source.y) + 92;
            const x2 = Number(target.x) + 110;
            const y2 = Number(target.y);
            const bend = Math.max(45, Math.abs(y2 - y1) / 2);
            return `M ${x1} ${y1} C ${x1} ${y1 + bend}, ${x2} ${y2 - bend}, ${x2} ${y2}`;
        },
        edgeLabelX(edge) {
            const source = this.form.graph.nodes.find(node => node.id === edge.source);
            return source ? Number(source.x) + (edge.sourceHandle === 'false' ? 150 : 80) : 0;
        },
        edgeLabelY(edge) {
            const source = this.form.graph.nodes.find(node => node.id === edge.source);
            return source ? Number(source.y) + 112 : 0;
        },
        autoLayout() {
            if (!this.form.graph.nodes.length) return;
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
            nodes.forEach(node => {
                if (typeof level[node.id] === 'undefined') level[node.id] = disconnectedLevel++;
            });
            const groups = {};
            nodes.forEach(node => {
                const key = level[node.id];
                if (!groups[key]) groups[key] = [];
                groups[key].push(node);
            });
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
            const maxX = Math.max(900, ...this.form.graph.nodes.map(node => Number(node.x) + 280));
            const maxY = Math.max(620, ...this.form.graph.nodes.map(node => Number(node.y) + 180));
            this.canvasSize = { width: maxX, height: maxY };
        },
        nodeSummary(node) {
            if (node.type === 'function') { const fn = this.functions.find(item => item.id === Number(node.config.functionId)); return fn ? fn.moduleName : 'Function 已失效'; }
            if (node.type === 'interval-trigger') return `每 ${this.form.intervalSeconds} 秒`;
            if (node.type === 'manual-trigger') return '由用户手动运行';
            if (node.type === 'delay') return `${node.config.seconds || 0} 秒`;
            if (node.type === 'condition') return `${node.config.operator || 'equals'} ${node.config.right || ''}`;
            if (node.type === 'end') return '流程在此结束';
            return node.type === 'agent-group' ? 'Agent 组' : node.type === 'agent' ? '独立 Agent' : node.type;
        },
        functionParameters(fn) { return NeuCharPivotUi.parseJson(fn.parameterSchemaJson, []); },
        validate() {
            if (!this.form.name.trim()) return '请输入工作流名称。';
            const trigger = this.form.graph.nodes.find(node => String(node.type).endsWith('trigger'));
            if (!trigger) return '工作流必须包含触发器。';
            const visited = new Set();
            const queue = [trigger.id];
            while (queue.length) {
                const current = queue.shift();
                if (visited.has(current)) continue;
                visited.add(current);
                this.form.graph.edges.filter(edge => edge.source === current).forEach(edge => queue.push(edge.target));
            }
            if (visited.size !== this.form.graph.nodes.length) return '画布中仍有未连接到触发器的节点。';
            for (const node of this.form.graph.nodes.filter(item => item.type === 'function')) {
                const fn = this.functions.find(item => item.id === Number(node.config.functionId));
                if (!fn || !fn.moduleAvailable) return `节点“${node.name}”引用的模块未开启。`;
                const missing = NeuCharPivotUi.firstMissingRequired(fn, node.config.parameters || {});
                if (missing) return `节点“${node.name}”缺少必填参数“${missing.title || missing.name}”。`;
            }
            for (const node of this.form.graph.nodes.filter(item => item.type === 'agent' || item.type === 'agent-group')) {
                const object = this.workflowObjects.find(item => item.providerId === node.config.providerId && item.objectId === node.config.objectId);
                if (!object || !object.enabled) return `节点“${node.name}”引用的 Agent 不可用。`;
            }
            return '';
        },
        async saveWorkflow() {
            const error = this.validate();
            if (error) { this.$notify({ title: '无法保存', message: error, type: 'warning' }); return; }
            this.loading = true;
            try {
                const response = await service.post('/Admin/NeuCharPivot/Workflow?handler=Save', {
                    id: this.form.id || 0,
                    name: this.form.name,
                    description: this.form.description,
                    graphJson: JSON.stringify(this.form.graph),
                    enabled: !!this.form.enabled,
                    triggerType: this.form.triggerType,
                    triggerConfigJson: JSON.stringify({ intervalSeconds: Number(this.form.intervalSeconds || 300) })
                }, { customAlert: true });
                const saved = NeuCharPivotUi.unwrap(response);
                this.form.id = saved.id;
                this.$notify({ title: 'Workflow', message: '工作流已保存。', type: 'success' });
                await this.loadAll();
            } catch (error) {
                const message = error.response && error.response.data ? error.response.data : '请检查节点配置。';
                this.$notify({ title: '保存失败', message: String(message), type: 'error' });
            } finally { this.loading = false; }
        },
        openRun() { this.run.visible = true; this.run.result = ''; },
        async runWorkflow() {
            this.run.loading = true;
            try {
                const response = await service.post('/Admin/NeuCharPivot/Workflow?handler=Run', { id: this.form.id, input: this.run.input }, { customAlert: true });
                const result = NeuCharPivotUi.unwrap(response) || {};
                this.run.result = `${result.success ? '执行成功' : '执行失败'}\n${result.output || result.errorMessage || ''}\n\n${(result.trace || []).join('\n')}`;
            } finally { this.run.loading = false; }
        },
        async deleteWorkflow() {
            await service.post('/Admin/NeuCharPivot/Workflow?handler=Delete', { id: this.form.id }, { customAlert: true });
            this.form = this.emptyForm(); this.editing = false; await this.loadAll();
        }
    }
});
