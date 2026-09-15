var app = new Vue({
    el: "#app",
    data() {
        return {
            page: { page: 1, size: 10 },
            total: 0,
            tableLoading: false,
            tableData: [],
            stats: {},
            modelList: [],
            runForm: { modelId: null, prompt: "" },
            runLoading: false,
            runId: null,
            progressEvents: [],
            liveUsage: { inputTokens: 0, outputTokens: 0, totalTokens: 0, elapsedMs: 0, preview: "" },
            dailyChart: null,
            modelChart: null
        };
    },
    computed: {
        progressPercent() {
            var last = this.lastProgress;
            if (!last) { return this.runLoading ? 5 : 0; }
            if (last.status === 1) { return 100; }
            if (last.status === 2) { return 100; }
            // running：按已耗时估算，最多到 95
            var p = Math.min(95, Math.floor(last.elapsedMs / 20));
            return Math.max(5, p);
        },
        progressStatus() {
            var last = this.lastProgress;
            if (!last) { return undefined; }
            if (last.status === 1) { return "success"; }
            if (last.status === 2) { return "exception"; }
            return undefined;
        },
        lastProgress() {
            return this.progressEvents.length > 0 ? this.progressEvents[this.progressEvents.length - 1] : null;
        }
    },
    mounted() {
        this.refreshAll();
        this.loadModels();
    },
    methods: {
        async refreshAll() {
            await Promise.all([this.loadStats(), this.getDataList()]);
        },
        async loadStats() {
            try {
                await service.post('/api/Senparc.Xncf.AIKernel/AITokenMonitorAppService/Xncf.AIKernel_AITokenMonitorAppService.GetStatsAsync', {})
                    .then(res => {
                        this.stats = (res.data && res.data.data) || {};
                        this.renderCharts();
                    });
            } catch (e) {
                console.error("loadStats error", e);
            }
        },
        async loadModels() {
            try {
                await service.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', { show: true })
                    .then(res => {
                        this.modelList = (res.data && res.data.data) || [];
                    });
            } catch (e) {
                console.error("loadModels error", e);
            }
        },
        async getDataList() {
            this.tableLoading = true;
            try {
                await service.post('/api/Senparc.Xncf.AIKernel/AITokenMonitorAppService/Xncf.AIKernel_AITokenMonitorAppService.GetPagedListAsync', {
                    page: this.page.page,
                    size: this.page.size
                }).then(res => {
                    this.tableData = (res.data && res.data.data && res.data.data.data) || [];
                    this.total = (res.data && res.data.data && res.data.data.total) || 0;
                });
            } finally {
                this.tableLoading = false;
            }
        },
        async runWithMonitor() {
            if (!this.runForm.modelId) {
                return this.$message.warning(ncfT('AIKernel.TokenMonitor.SelectModel'));
            }
            if (!this.runForm.prompt) {
                return this.$message.warning(ncfT('AIKernel.TokenMonitor.InputPrompt'));
            }
            this.runLoading = true;
            this.progressEvents = [];
            this.liveUsage = { inputTokens: 0, outputTokens: 0, totalTokens: 0, elapsedMs: 0, preview: "" };

            // 轮询异步进度（在运行期间实时刷新）
            var pollTimer = setInterval(() => { this.pollProgress(); }, 500);

            try {
                await service.post('/api/Senparc.Xncf.AIKernel/AITokenMonitorAppService/Xncf.AIKernel_AITokenMonitorAppService.RunModelWithMonitorAsync', {
                    modelId: this.runForm.modelId,
                    prompt: this.runForm.prompt,
                    source: "Monitor",
                    maxTokens: 2000
                }).then(res => {
                    var data = (res.data && res.data.data) || {};
                    this.runId = data.runId;
                    if (data.error) {
                        this.$message.error(data.error);
                    } else {
                        this.$message.success(ncfT('AIKernel.TokenMonitor.RunDone') + " (" + data.totalTokens + " tokens)");
                    }
                    this.loadStats();
                    this.getDataList();
                });
            } catch (e) {
                console.error("runWithMonitor error", e);
            } finally {
                this.runLoading = false;
                clearInterval(pollTimer);
                // 运行结束后，回放完整缓冲进度
                this.replayBufferedProgress();
            }
        },
        async pollProgress() {
            if (!this.runId) { return; }
            try {
                await service.post('/api/Senparc.Xncf.AIKernel/AITokenMonitorAppService/Xncf.AIKernel_AITokenMonitorAppService.GetProgressAsync', { runId: this.runId })
                    .then(res => {
                        var data = (res.data && res.data.data);
                        if (data) {
                            this.applyLiveUsage(data);
                            this.progressEvents.push(data);
                        }
                    });
            } catch (e) {
                // 忽略轮询异常
            }
        },
        async replayBufferedProgress() {
            if (!this.runId) { return; }
            try {
                await service.post('/api/Senparc.Xncf.AIKernel/AITokenMonitorAppService/Xncf.AIKernel_AITokenMonitorAppService.GetBufferedProgressAsync', { runId: this.runId })
                    .then(res => {
                        var list = (res.data && res.data.data) || [];
                        if (list.length > 0) {
                            this.progressEvents = list;
                            this.applyLiveUsage(list[list.length - 1]);
                        }
                    });
            } catch (e) {
                // 忽略
            }
        },
        applyLiveUsage(evt) {
            this.liveUsage = {
                inputTokens: evt.inputTokens || 0,
                outputTokens: evt.outputTokens || 0,
                totalTokens: evt.totalTokens || 0,
                elapsedMs: evt.elapsedMs || 0,
                preview: evt.outputPreview || ""
            };
        },
        statusText(status) {
            if (status === 0) { return ncfT('AIKernel.TokenMonitor.Running'); }
            if (status === 1) { return ncfT('AIKernel.TokenMonitor.Completed'); }
            if (status === 2) { return ncfT('AIKernel.TokenMonitor.Failed'); }
            return "";
        },
        async deleteRecord(row) {
            this.$confirm(ncfT('AIKernel.TokenMonitor.ConfirmDelete'), ncfT('Vector.Delete'), { type: "warning" }).then(() => {
                service.delete('/api/Senparc.Xncf.AIKernel/AITokenMonitorAppService/Xncf.AIKernel_AITokenMonitorAppService.DeleteAsync', { id: row.id })
                    .then(() => {
                        this.$message.success(ncfT('AIKernel.Frontend.005'));
                        this.refreshAll();
                    });
            }).catch(() => { });
        },
        handleCurrentChange(val) { this.page.page = val; this.getDataList(); },
        handleSizeChange(val) { this.page.size = val; this.getDataList(); },
        renderCharts() {
            this.renderDailyChart();
            this.renderModelChart();
        },
        renderDailyChart() {
            var el = document.getElementById('dailyChart');
            if (!el) { return; }
            if (!this.dailyChart) { this.dailyChart = echarts.init(el); }
            var daily = this.stats.daily || [];
            this.dailyChart.setOption({
                title: { text: ncfT('AIKernel.TokenMonitor.DailyTitle'), left: 'center' },
                tooltip: { trigger: 'axis' },
                xAxis: { type: 'category', data: daily.map(d => d.dateText) },
                yAxis: { type: 'value' },
                series: [
                    { name: ncfT('AIKernel.TokenMonitor.TotalTokens'), type: 'line', areaStyle: { color: '#91c7ae' }, color: '#91c7ae', data: daily.map(d => d.totalTokens) }
                ]
            });
        },
        renderModelChart() {
            var el = document.getElementById('modelChart');
            if (!el) { return; }
            if (!this.modelChart) { this.modelChart = echarts.init(el); }
            var byModel = (this.stats.byModel || []).slice(0, 10);
            this.modelChart.setOption({
                title: { text: ncfT('AIKernel.TokenMonitor.ModelTitle'), left: 'center' },
                tooltip: { trigger: 'axis' },
                legend: { data: [ncfT('AIKernel.TokenMonitor.InputTokens'), ncfT('AIKernel.TokenMonitor.OutputTokens')] },
                xAxis: { type: 'category', data: byModel.map(m => m.modelAlias || '(?)'), axisLabel: { interval: 0, rotate: 20 } },
                yAxis: { type: 'value' },
                series: [
                    { name: ncfT('AIKernel.TokenMonitor.InputTokens'), type: 'bar', stack: 'total', data: byModel.map(m => m.inputTokens) },
                    { name: ncfT('AIKernel.TokenMonitor.OutputTokens'), type: 'bar', stack: 'total', data: byModel.map(m => m.outputTokens) }
                ]
            });
        }
    }
});
