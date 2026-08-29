new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            data: {
                summary: {},
                trend: [],
                resources: [],
                workflows: [],
                runs: [],
                resourceOptions: [],
                workflowOptions: [],
                page: 1,
                hasMore: false
            },
            filters: {
                dateRange: [],
                workflowId: '',
                status: '',
                resourceKey: '',
                resourceType: '',
                resourceProviderId: '',
                resourceId: '',
                resourceName: '',
                keyword: ''
            },
            page: 1,
            pageSize: 25
        };
    },
    computed: {
        summary() { return this.data.summary || {}; },
        trend() { return this.data.trend || []; },
        resources() { return this.data.resources || []; },
        workflows() { return this.data.workflows || []; },
        runs() { return this.data.runs || []; },
        resourceOptions() {
            const options = this.data.resourceOptions || [];
            return this.filters.resourceType
                ? options.filter(item => item.type === this.filters.resourceType)
                : options;
        },
        workflowOptions() { return this.data.workflowOptions || []; },
        resourceTypes() {
            return [
                { value: 'agent', label: 'Agent' },
                { value: 'agent-group', label: 'AgentGroup' },
                { value: 'a2a', label: 'A2A' },
                { value: 'function', label: 'FunctionRender' }
            ];
        },
        hasMore() { return Boolean(this.data.hasMore); },
        maxTrendTotal() {
            return Math.max(1, ...this.trend.map(item => Number(item.total || 0)));
        }
    },
    created() {
        this.readRoute();
        this.loadData();
    },
    methods: {
        async loadData(page) {
            if (this.loading) return;
            if (Number.isInteger(page) && page > 0) this.page = page;
            this.loading = true;
            try {
                const params = new URLSearchParams();
                const range = this.filters.dateRange || [];
                if (range[0]) params.set('from', range[0]);
                if (range[1]) params.set('to', range[1]);
                if (this.filters.workflowId) params.set('workflowId', this.filters.workflowId);
                if (this.filters.status) params.set('status', this.filters.status);
                if (this.filters.resourceType) params.set('resourceType', this.filters.resourceType);
                if (this.filters.resourceProviderId) params.set('resourceProviderId', this.filters.resourceProviderId);
                if (this.filters.resourceId) params.set('resourceId', this.filters.resourceId);
                if (this.filters.resourceName) params.set('resourceName', this.filters.resourceName);
                if (this.filters.keyword) params.set('keyword', this.filters.keyword.trim());
                params.set('page', String(this.page));
                params.set('pageSize', String(this.pageSize));
                const response = await service.get(`/Admin/NeuCharWorkflow/Analytics?handler=Data&${params.toString()}`);
                this.data = NeuCharWorkflowUi.unwrap(response) || this.data;
                this.page = Number(this.data.page || this.page);
                if (!this.filters.resourceKey &&
                    (this.filters.resourceType || this.filters.resourceProviderId || this.filters.resourceId || this.filters.resourceName)) {
                    const selected = this.resourceOptions.find(item =>
                        item.type === this.filters.resourceType &&
                        item.providerId === this.filters.resourceProviderId &&
                        (this.filters.resourceId ? item.objectId === this.filters.resourceId : item.name === this.filters.resourceName));
                    if (selected) this.filters.resourceKey = this.resourceKey(selected);
                }
            } catch (error) {
                this.$message.error(this.errorMessage(error, '读取 Workflow 统计失败。'));
            } finally {
                this.loading = false;
            }
        },
        applyFilters() {
            this.page = 1;
            this.syncRoute();
            this.loadData(1);
        },
        applyResourceFilter() {
            const selected = this.resourceOptions.find(item => this.resourceKey(item) === this.filters.resourceKey);
            if (selected) {
                this.filters.resourceType = selected.type || '';
                this.filters.resourceProviderId = selected.providerId || '';
                this.filters.resourceId = selected.objectId || '';
                this.filters.resourceName = selected.name || '';
            } else {
                this.filters.resourceType = '';
                this.filters.resourceProviderId = '';
                this.filters.resourceId = '';
                this.filters.resourceName = '';
            }
            this.applyFilters();
        },
        applyResourceType() {
            this.filters.resourceKey = '';
            this.filters.resourceProviderId = '';
            this.filters.resourceId = '';
            this.filters.resourceName = '';
            this.applyFilters();
        },
        selectStatus(status) {
            this.filters.status = status || '';
            this.applyFilters();
        },
        resetFilters() {
            this.filters.dateRange = this.defaultDateRange();
            this.filters.workflowId = '';
            this.filters.status = '';
            this.filters.resourceKey = '';
            this.filters.resourceType = '';
            this.filters.resourceProviderId = '';
            this.filters.resourceId = '';
            this.filters.resourceName = '';
            this.filters.keyword = '';
            this.applyFilters();
        },
        changePage(page) {
            if (page < 1 || (page > this.page && !this.hasMore)) return;
            this.syncRoute(page);
            this.loadData(page);
        },
        selectResourceRow(row) {
            if (!row) return;
            this.filters.resourceType = row.type || '';
            this.filters.resourceProviderId = row.providerId || '';
            this.filters.resourceId = row.objectId || '';
            this.filters.resourceName = row.name || '';
            this.filters.resourceKey = this.resourceKey(row);
            this.applyFilters();
        },
        openRun(run) {
            if (!run || !run.workflowId) return;
            if (run.status === 'running' && run.runId) {
                window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${encodeURIComponent(run.workflowId)}&runId=${encodeURIComponent(run.runId)}`);
                return;
            }
            if (run.executionLogId && run.replayAvailable) {
                window.location.assign(`/Admin/NeuCharWorkflow/Replay?executionLogId=${encodeURIComponent(run.executionLogId)}`);
                return;
            }
            this.openTasks();
        },
        openTasks() {
            const params = this.taskQuery();
            window.location.assign(`/Admin/NeuCharWorkflow/Tasks${params.toString() ? `?${params.toString()}` : ''}`);
        },
        openWorkflow(workflowId) {
            const id = Number(workflowId || 0);
            if (!Number.isInteger(id) || id <= 0) return;
            window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${encodeURIComponent(id)}`);
        },
        taskQuery() {
            const params = new URLSearchParams();
            const range = this.filters.dateRange || [];
            if (range[0]) params.set('from', range[0]);
            if (range[1]) params.set('to', range[1]);
            if (this.filters.workflowId) params.set('workflowId', this.filters.workflowId);
            if (this.filters.status) params.set('status', this.filters.status);
            return params;
        },
        readRoute() {
            const query = new URLSearchParams(window.location.search || '');
            const from = query.get('from');
            const to = query.get('to');
            this.filters.dateRange = from || to ? [from || '', to || ''] : this.defaultDateRange();
            this.filters.workflowId = query.get('workflowId') || '';
            this.filters.status = ['running', 'success', 'failed'].includes(query.get('status')) ? query.get('status') : '';
            this.filters.resourceType = query.get('resourceType') || '';
            this.filters.resourceProviderId = query.get('resourceProviderId') || '';
            this.filters.resourceId = query.get('resourceId') || '';
            this.filters.resourceName = query.get('resourceName') || '';
            this.filters.keyword = query.get('keyword') || '';
        },
        syncRoute(page) {
            const params = new URLSearchParams();
            const range = this.filters.dateRange || [];
            if (range[0]) params.set('from', range[0]);
            if (range[1]) params.set('to', range[1]);
            if (this.filters.workflowId) params.set('workflowId', this.filters.workflowId);
            if (this.filters.status) params.set('status', this.filters.status);
            if (this.filters.resourceType) params.set('resourceType', this.filters.resourceType);
            if (this.filters.resourceProviderId) params.set('resourceProviderId', this.filters.resourceProviderId);
            if (this.filters.resourceId) params.set('resourceId', this.filters.resourceId);
            if (this.filters.resourceName) params.set('resourceName', this.filters.resourceName);
            if (this.filters.keyword) params.set('keyword', this.filters.keyword.trim());
            if (page && page > 1) params.set('page', String(page));
            window.history.replaceState({}, '', `/Admin/NeuCharWorkflow/Analytics${params.toString() ? `?${params.toString()}` : ''}`);
        },
        defaultDateRange() {
            const today = new Date();
            const start = new Date(today.getTime() - 29 * 24 * 60 * 60 * 1000);
            return [this.dateInput(start), this.dateInput(today)];
        },
        dateInput(date) {
            const pad = item => String(item).padStart(2, '0');
            return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
        },
        resourceKey(item) {
            return [item.type || '', item.providerId || '', item.objectId || '', item.name || ''].join('|');
        },
        resourceNames(resources) {
            return (resources || []).map(item => item.name).filter(Boolean).join('、') || '—';
        },
        resourceSuccessRate(resource) {
            const total = Number(resource && resource.callCount || 0);
            return total ? `${Math.round(Number(resource.successCount || 0) * 100 / total)}%` : '—';
        },
        trendWidth(value) {
            return `${Math.max(0, Number(value || 0) * 100 / this.maxTrendTotal)}%`;
        },
        formatNumber(value) {
            return Number(value || 0).toLocaleString();
        },
        formatPercent(value) {
            return value === null || value === undefined ? '—' : `${Number(value).toFixed(1)}%`;
        },
        formatDuration(value) {
            if (value === null || value === undefined || Number.isNaN(Number(value))) return '—';
            const seconds = Math.max(0, Number(value));
            if (seconds < 60) return `${seconds.toFixed(1)} 秒`;
            const minutes = Math.floor(seconds / 60);
            const rest = Math.round(seconds % 60);
            return `${minutes} 分 ${rest} 秒`;
        },
        formatDate(value) {
            if (!value) return '—';
            const date = new Date(value);
            if (Number.isNaN(date.getTime())) return String(value);
            const pad = item => String(item).padStart(2, '0');
            return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
        },
        sourceText(source) {
            return { manual: '手动运行', webhook: 'Webhook', interval: '定时触发', history: '历史记录' }[source] || '运行记录';
        },
        statusText(status) {
            return { running: '运行中', success: '成功', failed: '失败' }[status] || '未知';
        },
        statusType(status) {
            return { running: 'warning', success: 'success', failed: 'danger' }[status] || 'info';
        },
        errorMessage(error, fallback) {
            const data = error && error.response && error.response.data;
            return String((data && (data.title || data.detail || data)) || (error && error.message) || fallback);
        }
    }
});
