new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            loadError: '',
            replay: null,
            graph: { nodes: [], edges: [] },
            stepIndex: -1,
            nodeStates: {},
            playing: false,
            playTimer: null,
            copying: false
        };
    },
    computed: {
        currentEvent() {
            return this.replay && this.stepIndex >= 0 ? this.replay.events[this.stepIndex] || null : null;
        },
        stepLabel() {
            if (!this.replay) return '正在读取运行记录…';
            if (this.stepIndex < 0) return '运行开始前';
            return `步骤 ${this.stepIndex + 1} / ${this.replay.events.length}`;
        },
        canvasSize() {
            const nodes = this.graph.nodes || [];
            const right = Math.max(760, ...nodes.map(node => Number(node.x || 0) + 240));
            const bottom = Math.max(520, ...nodes.map(node => Number(node.y || 0) + 130));
            return { width: right, height: bottom };
        },
        canvasStyle() {
            return { width: `${this.canvasSize.width}px`, minHeight: `${this.canvasSize.height}px` };
        }
    },
    created() {
        this.loadReplay();
    },
    beforeDestroy() {
        this.stopPlayback();
    },
    methods: {
        routeExecutionLogId() {
            const value = Number(new URLSearchParams(window.location.search || '').get('executionLogId'));
            return Number.isInteger(value) && value > 0 ? value : 0;
        },
        async loadReplay() {
            const executionLogId = this.routeExecutionLogId();
            if (!executionLogId) {
                this.loadError = '缺少需要回看的任务标识。';
                return;
            }
            this.loading = true;
            try {
                const response = await service.get(`/Admin/NeuCharWorkflow/Replay?handler=Data&executionLogId=${executionLogId}`);
                this.replay = NeuCharWorkflowUi.unwrap(response);
                if (!this.replay) throw new Error('未找到任务回看数据。');
                this.replay.events = Array.isArray(this.replay.events) ? this.replay.events : [];
                this.graph = NeuCharWorkflowUi.parseJson(this.replay.definition && this.replay.definition.graphJson, { nodes: [], edges: [] }) || { nodes: [], edges: [] };
                this.graph.nodes = Array.isArray(this.graph.nodes) ? this.graph.nodes : [];
                this.graph.edges = Array.isArray(this.graph.edges) ? this.graph.edges : [];
            } catch (error) {
                this.loadError = this.errorMessage(error, '无法读取任务回看。运行中的任务需要等待结束，旧任务可能尚未保存快照。');
            } finally {
                this.loading = false;
            }
        },
        nodeStyle(node) {
            return { left: `${Math.max(24, Number(node.x || 0))}px`, top: `${Math.max(24, Number(node.y || 0))}px` };
        },
        findNode(id) {
            return this.graph.nodes.find(node => node.id === id);
        },
        edgeStartX(edge) {
            const source = this.findNode(edge.source);
            return Number(source && source.x || 0) + 208;
        },
        edgeStartY(edge) {
            const source = this.findNode(edge.source);
            return Number(source && source.y || 0) + 43;
        },
        edgeEndX(edge) {
            const target = this.findNode(edge.target);
            return Number(target && target.x || 0) + 12;
        },
        edgeEndY(edge) {
            const target = this.findNode(edge.target);
            return Number(target && target.y || 0) + 43;
        },
        nodeState(id) { return this.nodeStates[id] || 'pending'; },
        goToStep(index) {
            this.stopPlayback();
            this.stepIndex = Math.max(-1, Math.min(index, this.replay.events.length - 1));
            this.rebuildNodeStates();
        },
        previousStep() { this.goToStep(this.stepIndex - 1); },
        nextStep() {
            if (!this.replay || this.stepIndex >= this.replay.events.length - 1) {
                this.stopPlayback();
                return;
            }
            this.stepIndex += 1;
            this.rebuildNodeStates();
        },
        resetPlayback() { this.goToStep(-1); },
        finishPlayback() { this.goToStep(this.replay.events.length - 1); },
        rebuildNodeStates() {
            const states = {};
            if (!this.replay) return;
            this.replay.events.slice(0, this.stepIndex + 1).forEach(event => {
                states[event.nodeId] = event.status === 'failed'
                    ? 'failed'
                    : event.status === 'running' ? 'running' : 'success';
            });
            this.nodeStates = states;
        },
        togglePlayback() {
            if (this.playing) {
                this.stopPlayback();
                return;
            }
            if (this.stepIndex >= this.replay.events.length - 1) this.resetPlayback();
            this.playing = true;
            this.playTimer = window.setInterval(() => this.nextStep(), 850);
            this.nextStep();
        },
        stopPlayback() {
            if (this.playTimer) window.clearInterval(this.playTimer);
            this.playTimer = null;
            this.playing = false;
        },
        eventSummary(event) { return event.message || this.eventStatusText(event.status); },
        eventStatusText(status) {
            return { running: '执行中', success: '执行完成', failed: '执行失败', branch: '选择分支', console: 'Console 输出' }[status] || '状态更新';
        },
        eventType(status) { return { success: 'success', failed: 'danger', running: 'warning', branch: 'primary' }[status] || 'info'; },
        statusText(status) { return { success: '运行成功', failed: '运行失败' }[status] || '运行完成'; },
        nodeTypeText(type) {
            return {
                'manual-trigger': '手动触发', 'interval-trigger': '定时触发', 'webhook-trigger': 'Webhook 触发',
                function: 'Function', delay: '等待', condition: '条件判断', agent: 'Agent', 'agent-group': 'Agent 组',
                aggregate: '聚合', console: 'Console', neubell: '发送纽铃', end: '结束'
            }[type] || type;
        },
        formatDate(value) {
            if (!value) return '—';
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) return String(value);
            const pad = item => String(item).padStart(2, '0');
            return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
        },
        async copyReplay() {
            if (!this.replay) return;
            this.copying = true;
            try {
                const response = await service.post('/Admin/NeuCharWorkflow/Replay?handler=Copy', {
                    executionLogId: this.replay.executionLogId
                }, { customAlert: true });
                const workflow = NeuCharWorkflowUi.unwrap(response);
                if (!workflow || !workflow.id) throw new Error('复制工作流失败。');
                window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${workflow.id}`);
            } catch (error) {
                this.$message.error(this.errorMessage(error, '无法从当前回看创建工作流草稿。'));
            } finally {
                this.copying = false;
            }
        },
        openLatestWorkflow() {
            if (this.replay && this.replay.workflowId) {
                window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${this.replay.workflowId}`);
            }
        },
        backToTasks() { window.location.assign('/Admin/NeuCharWorkflow/Tasks'); },
        errorMessage(error, fallback) {
            const data = error && error.response && error.response.data;
            return String((data && (data.title || data.detail || data)) || (error && error.message) || fallback);
        }
    }
});
