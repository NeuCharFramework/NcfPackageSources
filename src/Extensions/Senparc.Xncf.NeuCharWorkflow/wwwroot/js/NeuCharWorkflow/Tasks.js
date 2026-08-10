new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            tasks: [],
            keyword: '',
            statusFilter: '',
            refreshTimer: null,
            focusedRunId: '',
            neuBellConsumeMessage: '',
            neuBellConsumeError: false
        };
    },
    computed: {
        filteredTasks() {
            const keyword = String(this.keyword || '').trim().toLowerCase();
            return this.tasks.filter(task => {
                const statusMatched = !this.statusFilter || task.status === this.statusFilter;
                const keywordMatched = !keyword || [task.workflowName, task.workflowId, task.status, task.source, task.summary, task.errorMessage]
                    .some(value => String(value || '').toLowerCase().includes(keyword));
                return statusMatched && keywordMatched;
            });
        },
        hasRunningTasks() {
            return this.tasks.some(task => task.status === 'running');
        }
    },
    created() {
        this.focusedRunId = this.routeQuery().get('runId') || '';
        this.loadTasks();
        this.consumeNeuBellFromRoute();
    },
    beforeDestroy() {
        this.clearRefreshTimer();
    },
    methods: {
        async loadTasks() {
            this.loading = true;
            try {
                const response = await service.get('/Admin/NeuCharWorkflow/Tasks?handler=List');
                this.tasks = NeuCharWorkflowUi.unwrap(response) || [];
            } catch (error) {
                this.$message.error(this.errorMessage(error, '读取 Workflow 任务列表失败。'));
            } finally {
                this.loading = false;
                this.scheduleRefresh();
            }
        },
        scheduleRefresh() {
            this.clearRefreshTimer();
            if (this.hasRunningTasks) {
                this.refreshTimer = window.setTimeout(() => this.loadTasks(), 1500);
            }
        },
        clearRefreshTimer() {
            if (this.refreshTimer) window.clearTimeout(this.refreshTimer);
            this.refreshTimer = null;
        },
        statusCount(status) {
            return this.tasks.filter(task => task.status === status).length;
        },
        statusText(status) {
            return { running: '运行中', success: '成功', failed: '失败' }[status] || '未知';
        },
        statusType(status) {
            return { running: 'warning', success: 'success', failed: 'danger' }[status] || 'info';
        },
        sourceText(source) {
            return { manual: '手动运行', webhook: 'Webhook', interval: '定时触发', history: '历史记录' }[source] || '运行记录';
        },
        summaryText(task) {
            return task.errorMessage || task.summary || (task.status === 'running' ? '正在准备执行…' : '没有可显示的结果摘要。');
        },
        formatDate(value) {
            if (!value) return '—';
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) return String(value);
            const pad = item => String(item).padStart(2, '0');
            return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
        },
        openTask(task) {
            if (!task || !task.workflowId) return;
            if (task.status === 'running') {
                this.$message.info('该工作流仍在运行，请在运行结束后再启动回看。');
                return;
            }
            if (!task.replayAvailable || !task.executionLogId) {
                this.$message.warning('该任务没有可用的运行快照，可能产生于回看功能启用前。');
                return;
            }
            window.location.assign(`/Admin/NeuCharWorkflow/Replay?executionLogId=${encodeURIComponent(task.executionLogId)}`);
        },
        errorMessage(error, fallback) {
            const data = error && error.response && error.response.data;
            return String((data && (data.title || data.detail || data)) || fallback);
        },
        routeQuery() {
            const search = window.location && window.location.search;
            return new URLSearchParams(search || '');
        },
        taskRowClass({ row }) {
            return this.focusedRunId && String(row.runId || '').replace(/-/g, '').toLowerCase() ===
                this.focusedRunId.replace(/-/g, '').toLowerCase()
                ? 'workflow-task-current' : '';
        },
        async consumeNeuBellFromRoute() {
            const query = this.routeQuery();
            const providerId = String(query.get('neuBellProvider') || '').trim();
            const itemId = String(query.get('neuBellItem') || '').trim();
            const mode = String(query.get('neuBellConsume') || 'none').trim().toLowerCase();
            if (!providerId || mode === 'none' || (mode === 'item' && !itemId)) return;

            try {
                const response = await service.post('/api/Senparc.Areas.Admin/neubell/consume', {
                    providerId,
                    itemId,
                    consumeAll: mode === 'provider'
                }, { customAlert: true });
                const body = NeuCharWorkflowUi.unwrap(response) || {};
                const consumedCount = Number(body && body.consumedCount || 0);
                this.neuBellConsumeMessage = consumedCount > 0
                    ? (mode === 'provider' ? `已消费当前订阅下 ${consumedCount} 条纽铃提醒。` : '已消费当前这一条纽铃提醒。')
                    : '该纽铃提醒已被消费，或不再属于当前账号。';
            } catch (error) {
                this.neuBellConsumeError = true;
                this.neuBellConsumeMessage = this.errorMessage(error, '纽铃提醒无法自动消费；已保留业务状态，请在对应页面处理。');
            }
        }
    }
});
