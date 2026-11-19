new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            loadingMore: false,
            tasks: [],
            summary: {},
            hasMore: false,
            nextExecutionLogId: null,
            keyword: '',
            statusFilter: '',
            refreshTimer: null,
            abortingTaskId: '',
            cleaning: false,
            focusedRunId: '',
            workflowIdFilter: 0,
            fromDate: '',
            toDate: '',
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
        this.fromDate = query.get('from') || '';
        this.toDate = query.get('to') || '';
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
                this.summary = page.summary || {};
                this.hasMore = page.hasMore;
                this.nextExecutionLogId = page.nextExecutionLogId;
            } catch (error) {
                this.$message.error(this.errorMessage(error, ncfST('Frontend.104')));
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
                this.summary = page.summary || this.summary;
                this.mergeLatestTasks(page.items);
                // Once history is loaded to the end, refresh only merges new
                // records at the top and does not reopen the loaded history.
                if (wasEmpty) {
                    this.hasMore = page.hasMore;
                    this.nextExecutionLogId = page.nextExecutionLogId;
                }
            } catch (error) {
                this.$message.error(this.errorMessage(error, ncfST('Frontend.105')));
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
                // Guard against an invalid server cursor so reaching the
                // bottom cannot request the same page repeatedly.
                if (page.hasMore && page.nextExecutionLogId === previousCursor) {
                    this.hasMore = false;
                }
            } catch (error) {
                this.$message.error(this.errorMessage(error, ncfST('Frontend.106')));
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
            if (this.fromDate) query.push(`from=${encodeURIComponent(this.fromDate)}`);
            if (this.toDate) query.push(`to=${encodeURIComponent(this.toDate)}`);
            const suffix = query.length ? `&${query.join('&')}` : '';
            const response = await service.get(`/Admin/NeuCharWorkflow/Tasks?handler=List${suffix}`);
            const body = NeuCharWorkflowUi.unwrap(response) || {};
            if (Array.isArray(body)) {
                return { items: body, hasMore: false, nextExecutionLogId: null, summary: {} };
            }
            return {
                items: Array.isArray(body.items) ? body.items : [],
                hasMore: Boolean(body.hasMore),
                nextExecutionLogId: body.nextExecutionLogId || null,
                summary: body.summary || {}
            };
        },
        mergeLatestTasks(items) {
            const merged = new Map();
            // The latest page is the source of truth for live state. Remove
            // stale live items before merging it so completed tasks do not
            // remain marked as running.
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
        summaryCount(status) {
            if (status === 'all') return Number(this.summary.totalRuns ?? this.tasks.length);
            const property = status === 'running' ? 'runningCount' : status === 'success' ? 'successCount' : 'failedCount';
            return Number(this.summary[property] ?? this.statusCount(status));
        },
        statusCount(status) {
            return this.tasks.filter(task => task.status === status).length;
        },
        selectStatus(status) {
            const nextStatus = status || '';
            if (this.statusFilter === nextStatus) return;
            this.statusFilter = nextStatus;
            this.syncRoute();
            this.loadTasks();
        },
        statusText(status) {
            return {
                running: ncfST('Frontend.077'),
                success: ncfST('Frontend.127'),
                failed: ncfST('Frontend.128')
            }[status] || ncfST('Frontend.652');
        },
        statusType(status) {
            return { running: 'warning', success: 'success', failed: 'danger' }[status] || 'info';
        },
        sourceText(source) {
            return {
                manual: ncfST('Frontend.467'),
                webhook: 'Webhook',
                interval: ncfST('Frontend.471'),
                history: ncfST('Frontend.468')
            }[source] || ncfST('Frontend.469');
        },
        summaryText(task) {
            return task.errorMessage || task.summary ||
                (task.status === 'running' ? ncfST('Frontend.653') : ncfST('Frontend.654'));
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
                    this.$message.warning(ncfST('Frontend.656'));
                    return;
                }
                window.location.assign(`/Admin/NeuCharWorkflow/Index?workflowId=${encodeURIComponent(task.workflowId)}&runId=${encodeURIComponent(task.runId)}`);
                return;
            }
            if (!task.replayAvailable || !task.executionLogId) {
                this.$message.warning(ncfST('Frontend.486'));
                return;
            }
            window.location.assign(`/Admin/NeuCharWorkflow/Replay?executionLogId=${encodeURIComponent(task.executionLogId)}`);
        },
        async abortTask(task) {
            if (!task || task.status !== 'running' || this.abortingTaskId) return;
            if (!task.runId && !task.executionLogId) {
                this.$message.error(ncfST('Frontend.655'));
                return;
            }
            try {
                if (this.$confirm) {
                    await this.$confirm(ncfST('Frontend.508', task.workflowName || ncfST('Frontend.507')), ncfST('Frontend.509'), {
                        confirmButtonText: ncfST('Frontend.510'),
                        cancelButtonText: ncfST('Frontend.511'),
                        type: 'warning'
                    });
                }
            } catch (error) {
                if (error !== 'cancel' && error !== 'close') {
                    this.$message.error(this.errorMessage(error, ncfST('Frontend.512')));
                }
                return;
            }

            this.abortingTaskId = task.taskId;
            try {
                await service.post('/Admin/NeuCharWorkflow/Tasks?handler=Abort', {
                    runId: task.runId || null,
                    executionLogId: task.executionLogId || null
                }, { customAlert: true });
                this.$message.success(ncfST('Frontend.513'));
                await this.refreshTasks();
            } catch (error) {
                this.$message.error(this.errorMessage(error, ncfST('Frontend.514')));
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
                    this.$message.info(ncfST('Frontend.515'));
                    return;
                }

                if (this.$confirm) {
                    await this.$confirm(
                        ncfST('Frontend.517', completedCount, Number(preview.succeededCount || 0), Number(preview.failedCount || 0)),
                        ncfST('Frontend.516'),
                        {
                            confirmButtonText: ncfST('Frontend.518', completedCount),
                            cancelButtonText: ncfST('Frontend.193'),
                            type: 'warning',
                            closeOnClickModal: false
                        });
                }

                const resultResponse = await service.post('/Admin/NeuCharWorkflow/Tasks?handler=Cleanup', {
                    cutoff: preview.cutoff
                }, { customAlert: true });
                const result = NeuCharWorkflowUi.unwrap(resultResponse) || {};
                this.$message.success(ncfST('Frontend.519', Number(result.deletedCount || 0)));
                await this.loadTasks();
            } catch (error) {
                if (error !== 'cancel' && error !== 'close') {
                    this.$message.error(this.errorMessage(error, ncfST('Frontend.657')));
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
        syncRoute() {
            const query = new URLSearchParams();
            if (this.workflowIdFilter) query.set('workflowId', String(this.workflowIdFilter));
            if (this.statusFilter) query.set('status', this.statusFilter);
            if (this.fromDate) query.set('from', this.fromDate);
            if (this.toDate) query.set('to', this.toDate);
            const url = `/Admin/NeuCharWorkflow/Tasks${query.toString() ? `?${query.toString()}` : ''}`;
            window.history.replaceState({}, '', url);
        },
        clearWorkflowFilter() {
            this.workflowIdFilter = 0;
            this.syncRoute();
            this.loadTasks();
        },
        openAnalytics() {
            const query = new URLSearchParams();
            if (this.workflowIdFilter) query.set('workflowId', String(this.workflowIdFilter));
            if (this.statusFilter) query.set('status', this.statusFilter);
            if (this.fromDate) query.set('from', this.fromDate);
            if (this.toDate) query.set('to', this.toDate);
            window.location.assign(`/Admin/NeuCharWorkflow/Analytics${query.toString() ? `?${query.toString()}` : ''}`);
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
                    ? (mode === 'provider' ? ncfST('Frontend.859', consumedCount) : ncfST('Frontend.860'))
                    : ncfST('Frontend.861');
            } catch (error) {
                this.neuBellConsumeError = true;
                this.neuBellConsumeMessage = this.errorMessage(error, ncfST('Frontend.862'));
            }
        }
    }
});
