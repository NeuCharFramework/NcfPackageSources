new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            tasks: [],
            keyword: '',
            statusFilter: '',
            refreshTimer: null
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
        this.loadTasks();
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
            const query = new URLSearchParams({ workflowId: String(task.workflowId) });
            if (task.runId) query.set('runId', String(task.runId));
            window.location.assign(`/Admin/NeuCharWorkflow/Index?${query.toString()}`);
        },
        errorMessage(error, fallback) {
            const data = error && error.response && error.response.data;
            return String((data && (data.title || data.detail || data)) || fallback);
        }
    }
});
