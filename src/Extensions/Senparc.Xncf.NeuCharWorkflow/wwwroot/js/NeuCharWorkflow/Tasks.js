new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            loadingMore: false,
            tasks: [],
            hasMore: false,
            nextExecutionLogId: null,
            keyword: '',
            statusFilter: '',
            refreshTimer: null,
            abortingTaskId: '',
            cleaning: false,
            focusedRunId: '',
            workflowIdFilter: 0,
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
        const query = this.routeQuery();
        this.focusedRunId = query.get('runId') || '';
        const workflowId = Number(query.get('workflowId'));
        this.workflowIdFilter = Number.isInteger(workflowId) && workflowId > 0 ? workflowId : 0;
        const status = String(query.get('status') || '').trim().toLowerCase();
        this.statusFilter = ['running', 'success', 'failed'].includes(status) ? status : '';
        this.loadTasks();
        this.consumeNeuBellFromRoute();
    },
    mounted() {
        if (window && window.addEventListener) {
            window.addEventListener('scroll', this.handleScroll, { passive: true });
            window.addEventListener('resize', this.handleScroll);
        }
    },
    beforeDestroy() {
        this.clearRefreshTimer();
        if (window && window.removeEventListener) {
            window.removeEventListener('scroll', this.handleScroll);
            window.removeEventListener('resize', this.handleScroll);
        }
    },
    methods: {
        async loadTasks() {
            if (this.loading) return;
            this.loading = true;
            this.clearRefreshTimer();
            try {
                const page = await this.getTaskPage();
                this.tasks = this.sortTasks(page.items);
                this.hasMore = page.hasMore;
                this.nextExecutionLogId = page.nextExecutionLogId;
            } catch (error) {
                this.$message.error(this.errorMessage(error, '读取 Workflow 任务列表失败。'));
            } finally {
                this.loading = false;
                this.scheduleRefresh();
                this.$nextTick(() => this.handleScroll());
            }
        },
        async refreshTasks() {
            if (this.loading || this.loadingMore) return;
            this.loading = true;
            try {
                const page = await this.getTaskPage();
                const wasEmpty = this.tasks.length === 0;
                this.mergeLatestTasks(page.items);
                // 已加载到末尾时，刷新只合并顶部新增记录，不能重新把已加载历史标记为“未加载”。
                if (wasEmpty) {
                    this.hasMore = page.hasMore;
                    this.nextExecutionLogId = page.nextExecutionLogId;
                }
            } catch (error) {
                this.$message.error(this.errorMessage(error, '刷新 Workflow 任务列表失败。'));
            } finally {
                this.loading = false;
                this.scheduleRefresh();
                this.$nextTick(() => this.handleScroll());
            }
        },
        async loadMoreTasks() {
            if (!this.hasMore || this.loading || this.loadingMore) return;
            if (!this.nextExecutionLogId) {
                this.hasMore = false;
                return;
            }

            this.loadingMore = true;
            try {
                const previousCursor = this.nextExecutionLogId;
                const page = await this.getTaskPage(previousCursor);
                this.tasks = this.sortTasks(this.tasks.concat(page.items));
                this.hasMore = page.hasMore;
                this.nextExecutionLogId = page.nextExecutionLogId;
                // 防御异常服务端响应，避免滚动到底部后反复请求同一游标。
                if (page.hasMore && page.nextExecutionLogId === previousCursor) {
                    this.hasMore = false;
                }
            } catch (error) {
                this.$message.error(this.errorMessage(error, '加载更多 Workflow 任务失败。'));
            } finally {
                this.loadingMore = false;
                this.$nextTick(() => this.handleScroll());
            }
        },
        async getTaskPage(beforeExecutionLogId) {
            const query = [];
            if (beforeExecutionLogId) query.push(`beforeExecutionLogId=${encodeURIComponent(beforeExecutionLogId)}`);
            if (this.workflowIdFilter) query.push(`workflowId=${encodeURIComponent(this.workflowIdFilter)}`);
            if (this.statusFilter) query.push(`status=${encodeURIComponent(this.statusFilter)}`);
            const suffix = query.length ? `&${query.join('&')}` : '';
            const response = await service.get(`/Admin/NeuCharWorkflow/Tasks?handler=List${suffix}`);
            const body = NeuCharWorkflowUi.unwrap(response) || {};
            if (Array.isArray(body)) {
                return { items: body, hasMore: false, nextExecutionLogId: null };
            }
            return {
                items: Array.isArray(body.items) ? body.items : [],
                hasMore: Boolean(body.hasMore),
                nextExecutionLogId: body.nextExecutionLogId || null
            };
        },
        mergeLatestTasks(items) {
            const merged = new Map();
            // 最新页是实时状态的唯一来源，先移除旧 live 项以避免结束后仍显示“运行中”。
            this.tasks.filter(task => !this.isLiveTask(task)).forEach(task => merged.set(task.taskId, task));
            (items || []).forEach(task => merged.set(task.taskId, task));
            this.tasks = this.sortTasks(Array.from(merged.values()));
        },
        sortTasks(tasks) {
            return (tasks || []).slice().sort((left, right) => {
                const timeDifference = new Date(right.startedAt || 0).getTime() - new Date(left.startedAt || 0).getTime();
                if (timeDifference) return timeDifference;
                return Number(right.executionLogId || 0) - Number(left.executionLogId || 0);
            });
        },
        isLiveTask(task) {
            return String(task && task.taskId || '').startsWith('live:');
        },
        handleScroll() {
            if (!this.hasMore || this.loading || this.loadingMore || !window || !document) return;
            const documentElement = document.documentElement;
            const scrollTop = window.pageYOffset || documentElement.scrollTop || 0;
            const distanceToBottom = documentElement.scrollHeight - (scrollTop + window.innerHeight);
            if (distanceToBottom <= 180) {
                this.loadMoreTasks();
            }
        },
        scheduleRefresh() {
            this.clearRefreshTimer();
            if (this.hasRunningTasks) {
                this.refreshTimer = window.setTimeout(() => this.refreshTasks(), 1500);
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
                if (!task.runId) {
                    this.$message.warning('该运行缺少实时运行标识，可能由已退出的服务进程遗留；请先中止或等待任务状态更新。');
                    return;
                }
                window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${encodeURIComponent(task.workflowId)}&runId=${encodeURIComponent(task.runId)}`);
                return;
            }
            if (!task.replayAvailable || !task.executionLogId) {
                this.$message.warning('该任务没有可用的运行快照，可能产生于回看功能启用前。');
                return;
            }
            window.location.assign(`/Admin/NeuCharWorkflow/Replay?executionLogId=${encodeURIComponent(task.executionLogId)}`);
        },
        async abortTask(task) {
            if (!task || task.status !== 'running' || this.abortingTaskId) return;
            if (!task.runId && !task.executionLogId) {
                this.$message.error('该运行缺少可中止标识，请刷新任务列表后重试。');
                return;
            }
            try {
                if (this.$confirm) {
                    await this.$confirm(`将中止“${task.workflowName || '当前工作流'}”的运行。`, '确认中止运行', {
                        confirmButtonText: '中止',
                        cancelButtonText: '继续运行',
                        type: 'warning'
                    });
                }
            } catch (error) {
                if (error !== 'cancel' && error !== 'close') {
                    this.$message.error(this.errorMessage(error, '无法显示中止确认。'));
                }
                return;
            }

            this.abortingTaskId = task.taskId;
            try {
                await service.post('/Admin/NeuCharWorkflow/Tasks?handler=Abort', {
                    runId: task.runId || null,
                    executionLogId: task.executionLogId || null
                }, { customAlert: true });
                this.$message.success('已请求手动中止，任务将标记为失败。');
                await this.refreshTasks();
            } catch (error) {
                this.$message.error(this.errorMessage(error, '中止 Workflow 任务失败。'));
            } finally {
                this.abortingTaskId = '';
            }
        },
        async quickCleanTasks() {
            if (this.cleaning) return;
            this.cleaning = true;
            try {
                const previewResponse = await service.get('/Admin/NeuCharWorkflow/Tasks?handler=CleanupPreview');
                const preview = NeuCharWorkflowUi.unwrap(previewResponse) || {};
                const completedCount = Number(preview.completedCount || 0);
                if (completedCount <= 0) {
                    this.$message.info('没有可清理的已完成任务记录；运行中的任务会始终保留。');
                    return;
                }

                if (this.$confirm) {
                    await this.$confirm(
                        `将永久删除 ${completedCount} 条已完成任务记录（成功 ${Number(preview.succeededCount || 0)} 条，失败 ${Number(preview.failedCount || 0)} 条）。运行中的任务、工作流定义和版本不会受影响，删除后无法恢复。`,
                        '确认快速清理',
                        {
                            confirmButtonText: `清理 ${completedCount} 条`,
                            cancelButtonText: '取消',
                            type: 'warning',
                            closeOnClickModal: false
                        });
                }

                const resultResponse = await service.post('/Admin/NeuCharWorkflow/Tasks?handler=Cleanup', {
                    cutoff: preview.cutoff
                }, { customAlert: true });
                const result = NeuCharWorkflowUi.unwrap(resultResponse) || {};
                this.$message.success(`已清理 ${Number(result.deletedCount || 0)} 条已完成任务记录。`);
                await this.loadTasks();
            } catch (error) {
                if (error !== 'cancel' && error !== 'close') {
                    this.$message.error(this.errorMessage(error, '快速清理 Workflow 任务失败。'));
                }
            } finally {
                this.cleaning = false;
            }
        },
        errorMessage(error, fallback) {
            const data = error && error.response && error.response.data;
            return String((data && (data.title || data.detail || data)) || fallback);
        },
        routeQuery() {
            const search = window.location && window.location.search;
            return new URLSearchParams(search || '');
        },
        clearWorkflowFilter() {
            const query = this.statusFilter ? `?status=${encodeURIComponent(this.statusFilter)}` : '';
            window.location.assign(`/Admin/NeuCharWorkflow/Tasks${query}`);
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
