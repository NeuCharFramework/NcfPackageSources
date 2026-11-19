window.ncfWorkflowText = window.ncfWorkflowText || function (source) {
    return typeof window.ncfST === 'function' ? window.ncfST(source) : source;
};

new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            loadError: '',
            replay: null,
            graph: { nodes: [], edges: [] },
            canvasViewportSize: { width: 0, height: 0 },
            stepIndex: -1,
            nodeStates: {},
            timelineRenderLimit: 300,
            timelineChunkSize: 300,
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
            if (!this.replay) return ncfWorkflowText('Frontend.107');
            if (this.stepIndex < 0) return ncfWorkflowText('Frontend.108');
            return ncfWorkflowText('Frontend.109').replace('{0}', this.stepIndex + 1).replace('{1}', this.replay.events.length);
        },
        canvasInset() { return 240; },
        canvasSize() {
            const nodes = this.graph.nodes || [];
            const right = Math.max(760, ...nodes.map(node => Number(node.x || 0) + 240));
            const bottom = Math.max(520, ...nodes.map(node => Number(node.y || 0) + 130));
            const viewportWidth = Number(this.canvasViewportSize && this.canvasViewportSize.width) || 0;
            const viewportHeight = Number(this.canvasViewportSize && this.canvasViewportSize.height) || 0;
            return {
                width: right + this.canvasInset * 2 + viewportWidth / 2,
                height: bottom + this.canvasInset * 2 + viewportHeight / 2
            };
        },
        canvasStyle() {
            return { width: `${this.canvasSize.width}px`, minHeight: `${this.canvasSize.height}px` };
        },
        visibleTimelineEvents() {
            return this.replay
                ? this.replay.events.slice(0, this.timelineRenderLimit)
                : [];
        },
        hasMoreTimelineEvents() {
            return this.replay && this.timelineRenderLimit < this.replay.events.length;
        }
    },
    created() {
        this.loadReplay();
    },
    mounted() {
        this.scheduleInitialCenter();
        window.addEventListener('resize', this.scheduleInitialCenter);
    },
    beforeDestroy() {
        this.stopPlayback();
        window.removeEventListener('resize', this.scheduleInitialCenter);
    },
    methods: {
        routeExecutionLogId() {
            const value = Number(new URLSearchParams(window.location.search || '').get('executionLogId'));
            return Number.isInteger(value) && value > 0 ? value : 0;
        },
        async loadReplay() {
            const executionLogId = this.routeExecutionLogId();
            if (!executionLogId) {
                this.loadError = ncfWorkflowText('Frontend.110');
                return;
            }
            this.loading = true;
            try {
                const response = await service.get(`/Admin/NeuCharWorkflow/Replay?handler=Data&executionLogId=${executionLogId}`);
                this.replay = NeuCharWorkflowUi.unwrap(response);
                if (!this.replay) throw new Error(ncfWorkflowText('Frontend.111'));
                this.replay.events = Array.isArray(this.replay.events) ? this.replay.events : [];
                this.timelineRenderLimit = Math.min(this.timelineChunkSize, this.replay.events.length);
                this.graph = NeuCharWorkflowUi.parseJson(this.replay.definition && this.replay.definition.graphJson, { nodes: [], edges: [] }) || { nodes: [], edges: [] };
                this.graph.nodes = Array.isArray(this.graph.nodes) ? this.graph.nodes : [];
                this.graph.edges = Array.isArray(this.graph.edges) ? this.graph.edges : [];
                this.scheduleInitialCenter();
            } catch (error) {
                this.loadError = this.errorMessage(error, ncfWorkflowText('Frontend.112'));
            } finally {
                this.loading = false;
            }
        },
        nodeStyle(node) {
            return {
                left: `${this.canvasInset + Math.max(24, Number(node.x || 0))}px`,
                top: `${this.canvasInset + Math.max(24, Number(node.y || 0))}px`
            };
        },
        isHorizontalLayout() {
            return !!(this.graph && this.graph.layout && this.graph.layout.direction === 'horizontal');
        },
        findNode(id) {
            return this.graph.nodes.find(node => node.id === id);
        },
        scheduleInitialCenter() {
            if (!this.graph.nodes.length) return;
            const retry = remaining => {
                if (this.centerGraph() || remaining <= 0) return;
                const next = () => retry(remaining - 1);
                if (typeof window.requestAnimationFrame === 'function') window.requestAnimationFrame(next);
                else window.setTimeout(next, 0);
            };
            const schedule = () => {
                if (typeof window.requestAnimationFrame === 'function') window.requestAnimationFrame(() => retry(3));
                else window.setTimeout(() => retry(3), 0);
            };
            if (typeof this.$nextTick === 'function') this.$nextTick(schedule);
            else schedule();
        },
        centerGraph() {
            const nodes = this.graph.nodes || [];
            const viewport = this.$refs && this.$refs.canvasViewport;
            if (!nodes.length || !viewport || !viewport.clientWidth || !viewport.clientHeight) return false;
            const viewportSize = { width: viewport.clientWidth, height: viewport.clientHeight };
            const previousSize = this.canvasViewportSize || {};
            if (previousSize.width !== viewportSize.width || previousSize.height !== viewportSize.height) {
                this.canvasViewportSize = viewportSize;
                return false;
            }
            const firstNode = nodes[0];
            const firstNodeLeft = this.canvasInset + Math.max(24, Number(firstNode.x || 0));
            const left = this.canvasInset + Math.min(...nodes.map(node => Math.max(24, Number(node.x || 0))));
            const top = this.canvasInset + Math.min(...nodes.map(node => Math.max(24, Number(node.y || 0))));
            const right = this.canvasInset + Math.max(...nodes.map(node => Math.max(24, Number(node.x || 0)) + 234));
            const bottom = this.canvasInset + Math.max(...nodes.map(node => Math.max(24, Number(node.y || 0)) + 92));
            const targetLeft = Math.max(0, firstNodeLeft + 117 - viewport.clientWidth / 2);
            const targetTop = Math.max(0, (top + bottom) / 2 - viewport.clientHeight / 2);
            viewport.scrollLeft = Math.min(targetLeft, Math.max(0, viewport.scrollWidth - viewport.clientWidth));
            viewport.scrollTop = Math.min(targetTop, Math.max(0, viewport.scrollHeight - viewport.clientHeight));
            return true;
        },
        centerCurrentNode() {
            const event = this.currentEvent;
            if (!event || !event.nodeId) return;
            const node = this.findNode(event.nodeId);
            if (!node) return;
            const center = () => {
                const viewport = this.$refs && this.$refs.canvasViewport;
                if (!viewport) return;
                const nodeLeft = this.canvasInset + Math.max(24, Number(node.x || 0));
                const nodeTop = this.canvasInset + Math.max(24, Number(node.y || 0));
                const left = Math.max(0, nodeLeft + 114 - viewport.clientWidth / 2);
                const top = Math.max(0, nodeTop + 46 - viewport.clientHeight / 2);
                const maxLeft = Math.max(0, viewport.scrollWidth - viewport.clientWidth);
                const maxTop = Math.max(0, viewport.scrollHeight - viewport.clientHeight);
                const target = { left: Math.min(left, maxLeft), top: Math.min(top, maxTop), behavior: 'smooth' };
                if (typeof viewport.scrollTo === 'function') viewport.scrollTo(target);
                else { viewport.scrollLeft = target.left; viewport.scrollTop = target.top; }
            };
            if (typeof this.$nextTick === 'function') this.$nextTick(center);
            else center();
        },
        edgeStartX(edge) {
            const source = this.findNode(edge.source);
            const left = this.canvasInset + Math.max(24, Number(source && source.x || 0));
            return left + (this.isHorizontalLayout() ? 234 : 117);
        },
        edgeStartY(edge) {
            const source = this.findNode(edge.source);
            const top = this.canvasInset + Math.max(24, Number(source && source.y || 0));
            return top + (this.isHorizontalLayout() ? 46 : 92);
        },
        edgeEndX(edge) {
            const target = this.findNode(edge.target);
            const left = this.canvasInset + Math.max(24, Number(target && target.x || 0));
            return left + (this.isHorizontalLayout() ? 0 : 117);
        },
        edgeEndY(edge) {
            const target = this.findNode(edge.target);
            const top = this.canvasInset + Math.max(24, Number(target && target.y || 0));
            return top + (this.isHorizontalLayout() ? 46 : 0);
        },
        nodeState(id) { return this.nodeStates[id] || 'pending'; },
        goToStep(index) {
            this.stopPlayback();
            this.stepIndex = Math.max(-1, Math.min(index, this.replay.events.length - 1));
            this.rebuildNodeStates();
            this.ensureTimelineRendered(this.stepIndex);
            this.centerCurrentNode();
        },
        previousStep() { this.goToStep(this.stepIndex - 1); },
        nextStep() {
            if (!this.replay || this.stepIndex >= this.replay.events.length - 1) {
                this.stopPlayback();
                return;
            }
            this.stepIndex += 1;
            this.applyEventNodeState(this.replay.events[this.stepIndex]);
            this.ensureTimelineRendered(this.stepIndex);
            this.centerCurrentNode();
        },
        resetPlayback() { this.goToStep(-1); },
        finishPlayback() { this.goToStep(this.replay.events.length - 1); },
        loadMoreTimelineEvents() {
            if (!this.replay) return;
            this.timelineRenderLimit = Math.min(
                this.replay.events.length,
                this.timelineRenderLimit + this.timelineChunkSize);
        },
        ensureTimelineRendered(index) {
            if (!this.replay || index < this.timelineRenderLimit) return;
            this.timelineRenderLimit = Math.min(
                this.replay.events.length,
                Math.max(index + 1, this.timelineRenderLimit + this.timelineChunkSize));
        },
        applyEventNodeState(event) {
            if (!event || !event.nodeId) return;
            const state = event.status === 'failed'
                ? 'failed'
                : event.status === 'running' ? 'running' : 'success';
            if (this.$set) this.$set(this.nodeStates, event.nodeId, state);
            else this.nodeStates = { ...this.nodeStates, [event.nodeId]: state };
        },
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
        eventSummary(event) {
            return window.ncfWorkflowText(event.message || this.eventStatusText(event.status));
        },
        eventStatusText(status) {
            const source = {
                running: ncfWorkflowText('Frontend.400'),
                success: ncfWorkflowText('Frontend.401'),
                failed: ncfWorkflowText('Frontend.402'),
                branch: ncfWorkflowText('Frontend.403'),
                console: ncfWorkflowText('Frontend.404')
            }[status] || ncfWorkflowText('Frontend.405');
            return window.ncfWorkflowText(source);
        },
        eventType(status) { return { success: 'success', failed: 'danger', running: 'warning', branch: 'primary' }[status] || 'info'; },
        statusText(status) {
            const source = { success: ncfWorkflowText('Frontend.406'), failed: ncfWorkflowText('Frontend.407') }[status] || ncfWorkflowText('Frontend.408');
            return window.ncfWorkflowText(source);
        },
        nodeTypeText(type) {
            return {
                'manual-trigger': ncfWorkflowText('Frontend.248'), 'interval-trigger': ncfWorkflowText('Frontend.471'), 'webhook-trigger': ncfWorkflowText('Frontend.470'),
                function: 'Function', delay: ncfWorkflowText('Frontend.115'), condition: ncfWorkflowText('Frontend.413'), agent: 'Agent', 'agent-group': ncfWorkflowText('Frontend.117'),
                aggregate: ncfWorkflowText('Frontend.422'), console: 'Console', neubell: ncfWorkflowText('Frontend.427'), end: ncfWorkflowText('Frontend.430')
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
                if (!workflow || !workflow.id) throw new Error(ncfWorkflowText('Frontend.114'));
                window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${workflow.id}`);
            } catch (error) {
                this.$message.error(this.errorMessage(error, ncfWorkflowText('Frontend.113')));
            } finally {
                this.copying = false;
            }
        },
        openLatestWorkflow() {
            if (this.replay && this.replay.workflowId) {
                window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${this.replay.workflowId}`);
            }
        },
        openWorkflowTasks() {
            if (this.replay && this.replay.workflowId) {
                window.location.assign(`/Admin/NeuCharWorkflow/Tasks?workflowId=${encodeURIComponent(this.replay.workflowId)}`);
            }
        },
        openWorkflowAnalytics() {
            if (this.replay && this.replay.workflowId) {
                window.location.assign(`/Admin/NeuCharWorkflow/Analytics?workflowId=${encodeURIComponent(this.replay.workflowId)}`);
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
        backToTasks() { window.location.assign('/Admin/NeuCharWorkflow/Tasks'); },
        errorMessage(error, fallback) {
            const data = error && error.response && error.response.data;
            return String((data && (data.title || data.detail || data)) || (error && error.message) || fallback);
        }
    }
});
