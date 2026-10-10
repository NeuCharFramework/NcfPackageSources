/* Copyright (C) 2026 Senparc. Created 20261002: local fine-tuning administration. */
var app = new Vue({
    el: "#app",
    data() {
        return {
            workers: [],
            workerAlias: new URL(window.location.href).searchParams.get("worker") || "",
            workerEditor: { id: 0, alias: "", name: "", endpoint: "", requestTimeoutSeconds: 30, enabled: true, note: "" },
            workerEditorVisible: false,
            workerSaveBusy: false,
            workerVersion: 0,
            health: null,
            models: [],
            datasets: [],
            datasetTotal: 0,
            datasetOffset: 0,
            datasetMoreBusy: false,
            jobs: [],
            jobsPage: 1,
            jobsTotal: 0,
            job: null,
            selectedId: new URL(window.location.href).searchParams.get("id") || "",
            selectionVersion: 0,
            events: [],
            validation: { status: "Idle", startedUtc: null, finishedUtc: null, completed: 0, total: 0, results: [], error: null },
            validationPrompts: "",
            validationMaxTokens: 128,
            validationBusy: false,
            cursor: 0,
            catchingUp: false,
            eventsTruncated: false,
            followLog: true,
            chartPoints: [],
            chartAvailable: typeof echarts !== "undefined",
            pollBusy: false,
            refreshPending: false,
            catalogPending: true,
            workersPending: true,
            detailLoading: false,
            connectionError: "",
            detailError: "",
            actionError: "",
            storageNotice: "",
            lastConnectedUtc: null,
            now: Date.now(),
            uploadBusy: false,
            createBusy: false,
            cancelBusy: false,
            fileReading: false,
            fileName: "",
            datasetName: "",
            datasetContent: "",
            helpOpen: ["workflow"],
            workflowSteps: ["Worker", "Data", "Training", "Monitor", "Publish"],
            datasetExample: "prompt",
            datasetExamples: {
                prompt: [
                    { prompt: "Classify: I cannot sign in.", completion: "account_access" },
                    { prompt: "Classify: Please send my invoice.", completion: "billing" },
                    { prompt: "Classify: Reset my password.", completion: "account_access" },
                    { prompt: "Classify: My payment failed.", completion: "billing" }
                ],
                messages: [
                    { messages: [{ role: "user", content: "Return ticket A as JSON." }, { role: "assistant", content: '{"ticket":"A","status":"open"}' }] },
                    { messages: [{ role: "user", content: "Return ticket B as JSON." }, { role: "assistant", content: '{"ticket":"B","status":"closed"}' }] },
                    { messages: [{ role: "user", content: "Return ticket C as JSON." }, { role: "assistant", content: '{"ticket":"C","status":"open"}' }] },
                    { messages: [{ role: "user", content: "Return ticket D as JSON." }, { role: "assistant", content: '{"ticket":"D","status":"closed"}' }] }
                ]
            },
            advancedOpen: [],
            form: {
                name: "", modelId: "", datasetId: "", evalDatasetId: null,
                backend: "cpu", method: "lora", epochs: 1, maxSteps: 10,
                learningRate: 0.0002, batchSize: 1, gradientAccumulationSteps: 1,
                maxSequenceLength: 256, loraRank: 8, loraAlpha: 16, loraDropout: 0.05,
                targetModules: "all-linear", warmupRatio: 0.03, weightDecay: 0,
                loggingSteps: 1, saveSteps: 20, evalSteps: 20, seed: 42, maxDurationMinutes: 60
            },
            parameters: [
                { key: "epochs", min: 1, max: 100 },
                { key: "maxSteps", min: 0, max: 1000000 },
                { key: "learningRate", min: 0.00000001, max: 0.1, step: 0.00001, precision: 8 },
                { key: "batchSize", min: 1, max: 64 },
                { key: "gradientAccumulationSteps", min: 1, max: 1024 },
                { key: "maxSequenceLength", min: 32, max: 32768 },
                { key: "loraRank", min: 1, max: 256 },
                { key: "loraAlpha", min: 1, max: 1024 },
                { key: "loraDropout", min: 0, max: 0.9, step: 0.01, precision: 2 },
                { key: "warmupRatio", min: 0, max: 0.5, step: 0.01, precision: 2 },
                { key: "weightDecay", min: 0, max: 1, step: 0.01, precision: 2 },
                { key: "loggingSteps", min: 1, max: 10000 },
                { key: "saveSteps", min: 1, max: 100000 },
                { key: "evalSteps", min: 1, max: 100000 },
                { key: "seed", min: 0, max: 2147483647 },
                { key: "maxDurationMinutes", min: 1, max: 10080 }
            ],
            metricCards: [
                { key: "trainLoss" }, { key: "evalLoss" }, { key: "learningRate" },
                { key: "rss", unit: "MiB" }, { key: "cpu", unit: "%" },
                { key: "gpuUtil", unit: "%" }, { key: "gpuMemory", unit: "MiB" },
                { key: "gpuTemperature", unit: "\u00b0C" }, { key: "gpuAllocated", unit: "MiB" },
                { key: "gradNorm" }, { key: "tokensPerSecond", unit: "token/s" },
                { key: "elapsedSeconds", unit: "s" }, { key: "diskFree", unit: "MiB" },
                { key: "jobOutput", unit: "MiB" }
            ]
        };
    },
    computed: {
        workerSetupRequired() {
            return !this.workersPending && !this.workerAlias;
        },
        sampleContent() {
            return this.datasetExamples[this.datasetExample].map(row => JSON.stringify(row)).join("\n") + "\n";
        },
        effectiveBatchSize() {
            return this.form.batchSize * this.form.gradientAccumulationSteps;
        },
        connectionHealthy() {
            return !!this.health && !this.connectionError && !!this.lastConnectedUtc
                && this.now - Date.parse(this.lastConnectedUtc) < 10000;
        },
        pagedJobs() {
            return this.jobs;
        },
        compatibilityError() {
            if (!this.health) { return this.t("AwaitingHealth"); }
            if (this.health.status !== "ok") { return this.t("WorkerNotReady"); }
            if (!this.health.storeId) { return this.t("WorkerVersionMismatch"); }
            var capability = this.health.capabilities.find(cap => cap.backend === this.form.backend);
            if (!capability || !capability.available) {
                return this.t("BackendUnavailable") + (capability && capability.reason ? ": " + capability.reason : "");
            }
            var model = this.models.find(item => item.id === this.form.modelId);
            if (model && model.backends.indexOf(this.form.backend) < 0) { return this.t("ModelBackendMismatch"); }
            if (capability.methods.indexOf(this.form.method) < 0) { return this.t("MethodMismatch"); }
            return "";
        },
        progressPercent() {
            var step = this.metric("step");
            var total = this.metric("totalSteps");
            if (step === null || total === null || total <= 0 || step < 0) { return null; }
            return Math.min(100, Math.max(0, Math.round(step / total * 1000) / 10));
        },
        progressStatus() {
            if (!this.job) { return undefined; }
            if (this.job.state === "Succeeded") { return "success"; }
            if (["Failed", "Interrupted"].indexOf(this.job.state) >= 0) { return "exception"; }
            return undefined;
        },
        workflowCurrentStep() {
            if (!this.workerAlias || !this.health) { return 0; }
            if (!this.datasets.length) { return 1; }
            if (!this.job) { return 2; }
            if (this.job.state === "Succeeded") { return 4; }
            return 3;
        }
    },
    mounted() {
        this._disposed = false;
        this._pollTimer = null;
        this._chart = null;
        this._clockTimer = setInterval(() => { this.now = Date.now(); }, 1000);
        this._resizeHandler = () => { if (this._chart) { this._chart.resize(); } };
        this._visibilityHandler = () => {
            if (document.visibilityState === "visible") { this.refresh(); }
        };
        window.addEventListener("resize", this._resizeHandler);
        document.addEventListener("visibilitychange", this._visibilityHandler);
        this.poll();
    },
    beforeDestroy() {
        this._disposed = true;
        clearTimeout(this._pollTimer);
        clearInterval(this._clockTimer);
        window.removeEventListener("resize", this._resizeHandler);
        document.removeEventListener("visibilitychange", this._visibilityHandler);
        if (this._chart) { this._chart.dispose(); }
    },
    methods: {
        t(key) {
            var localizedKey = "AIKernel.FineTuning." + key;
            if (window.AIKernelI18n && Object.prototype.hasOwnProperty.call(window.AIKernelI18n, localizedKey)) {
                return window.AIKernelI18n[localizedKey];
            }
            return typeof ncfT === "function" ? ncfT(localizedKey) : localizedKey;
        },
        workflowStepState(index) {
            var current = this.workflowCurrentStep;
            if (index < current) { return "done"; }
            if (index === current) { return "current"; }
            return "upcoming";
        },
        isRecord(value) {
            return value !== null && typeof value === "object" && !Array.isArray(value);
        },
        validMetrics(metrics) {
            return this.isRecord(metrics) && Object.keys(metrics).every(key =>
                metrics[key] === null || (typeof metrics[key] === "number" && Number.isFinite(metrics[key])));
        },
        validateResponse(method, data) {
            var valid = false;
            if (method === "GetWorkersAsync") {
                valid = Array.isArray(data) && data.every(worker => this.validWorker(worker));
            } else if (method === "SaveWorkerAsync") {
                valid = this.validWorker(data);
            } else if (method === "HealthAsync") {
                valid = this.isRecord(data) && typeof data.status === "string" && typeof data.version === "string"
                    && Number.isInteger(data.activeJobs) && data.activeJobs >= 0
                    && Number.isInteger(data.queuedJobs) && data.queuedJobs >= 0
                    && (data.storeId == null || typeof data.storeId === "string")
                    && Array.isArray(data.capabilities) && data.capabilities.every(cap =>
                        this.isRecord(cap) && typeof cap.backend === "string" && typeof cap.available === "boolean"
                        && Array.isArray(cap.methods) && cap.methods.every(item => typeof item === "string"));
            } else if (method === "GetModelsAsync") {
                valid = Array.isArray(data) && data.every(model => this.isRecord(model) && typeof model.id === "string"
                    && typeof model.name === "string" && Array.isArray(model.backends));
            } else if (method === "GetDatasetsAsync") {
                valid = Array.isArray(data) && data.every(item => this.validDataset(item));
            } else if (method === "GetDatasetsPageAsync") {
                valid = this.validCatalogPage(data, item => this.validDataset(item));
            } else if (method === "UploadDatasetAsync" || method === "GetDatasetAsync") {
                valid = this.validDataset(data);
            } else if (method === "GetJobsAsync") {
                valid = Array.isArray(data) && data.every(item => this.validJob(item));
            } else if (method === "GetJobsPageAsync") {
                valid = this.validCatalogPage(data, item => this.validJob(item));
            } else if (method === "GetEventsAsync") {
                valid = this.isRecord(data) && Array.isArray(data.events)
                    && Number.isSafeInteger(data.nextCursor) && data.nextCursor >= 0 && typeof data.hasMore === "boolean"
                    && data.events.every(event => this.isRecord(event) && Number.isSafeInteger(event.sequence)
                        && event.sequence > 0 && typeof event.timestampUtc === "string"
                        && typeof event.kind === "string" && this.validMetrics(event.metrics));
            } else if (method === "GetValidationAsync" || method === "StartValidationAsync") {
                valid = this.isRecord(data) && ["Idle", "Running", "Succeeded", "Failed"].indexOf(data.status) >= 0
                    && Number.isInteger(data.completed) && data.completed >= 0
                    && Number.isInteger(data.total) && data.total >= 0
                    && Array.isArray(data.results) && data.results.every(result =>
                        this.isRecord(result) && typeof result.id === "string"
                        && typeof result.prompt === "string" && typeof result.response === "string"
                        && typeof result.completedUtc === "string");
            } else {
                valid = this.validJob(data);
            }
            if (!valid) { throw new Error(this.t("Error.InvalidResponse") + " (" + method + ")"); }
            return data;
        },
        validWorker(worker) {
            return this.isRecord(worker) && Number.isInteger(worker.id) && worker.id > 0
                && typeof worker.alias === "string" && !!worker.alias
                && typeof worker.name === "string" && typeof worker.endpoint === "string"
                && Number.isInteger(worker.requestTimeoutSeconds) && worker.requestTimeoutSeconds >= 1
                && worker.requestTimeoutSeconds <= 300 && typeof worker.enabled === "boolean"
                && typeof worker.hasConfiguredSecret === "boolean";
        },
        validCatalogPage(page, validItem) {
            return this.isRecord(page) && Number.isInteger(page.total) && page.total >= 0
                && Number.isInteger(page.offset) && page.offset >= 0 && page.offset <= 100000
                && Number.isInteger(page.limit) && page.limit >= 1 && page.limit <= 200
                && Array.isArray(page.items) && page.items.length === Math.min(page.limit, Math.max(0, page.total - page.offset))
                && page.items.every(validItem);
        },
        validDataset(dataset) {
            return this.isRecord(dataset) && typeof dataset.id === "string" && typeof dataset.name === "string"
                && Number.isInteger(dataset.rows) && dataset.rows > 0
                && typeof dataset.sha256 === "string" && typeof dataset.createdUtc === "string";
        },
        validJob(job) {
            return this.isRecord(job) && typeof job.id === "string" && typeof job.name === "string"
                && ["Queued", "Running", "Cancelling", "Cancelled", "Succeeded", "Failed", "Interrupted"].indexOf(job.state) >= 0
                && this.isRecord(job.request) && this.validMetrics(job.latestMetrics)
                && Number.isSafeInteger(job.lastEventSequence) && job.lastEventSequence >= 0
                && Array.isArray(job.artifacts) && job.artifacts.every(artifact =>
                    this.isRecord(artifact) && typeof artifact.id === "string" && typeof artifact.name === "string"
                    && Number.isFinite(artifact.bytes) && artifact.bytes >= 0);
        },
        async request(method, payload, post, workerAlias) {
            var url = "/api/Senparc.Xncf.AIKernel/AIFineTuningAppService/Xncf.AIKernel_AIFineTuningAppService." + method;
            var alias = workerAlias === undefined ? this.workerAlias : workerAlias;
            var worker = this.workers.find(item => item.alias === alias);
            var config = { customAlert: true, timeout: ((worker ? worker.requestTimeoutSeconds : 30) + 5) * 1000 };
            var response;
            var requestPayload = payload || {};
            if (method !== "GetWorkersAsync" && method !== "SaveWorkerAsync") {
                if (post) {
                    config.params = { workerAlias: alias };
                    if (method === "StartValidationAsync") {
                        config.params.id = requestPayload.id;
                        requestPayload = Object.assign({}, requestPayload);
                        delete requestPayload.id;
                    }
                }
                else { requestPayload = Object.assign({ workerAlias: alias }, requestPayload); }
            }
            try {
                response = post
                    ? await service.post(url, requestPayload, config)
                    : await service.get(url, Object.assign({}, config, { params: requestPayload }));
            } catch (error) {
                var body = error.response && error.response.data;
                var reason = body && (body.errorMessage || body.msg || body.exception);
                var status = error.response && error.response.status;
                throw new Error(status === 401 || status === 403 ? this.t("Error.Authorization")
                    : this.t("Error.Request") + (reason ? ": " + reason : error.message ? ": " + error.message : ""));
            }
            var envelope = response && response.data;
            if (!envelope || envelope.success !== true) {
                throw new Error(envelope && (envelope.errorMessage || envelope.msg || envelope.exception)
                    || this.t("Error.InvalidResponse"));
            }
            return this.validateResponse(method, envelope.data);
        },
        refresh() {
            this.workersPending = true;
            this.catalogPending = true;
            this.requestPoll();
        },
        requestPoll() {
            if (this._disposed) { return; }
            if (this.pollBusy) { this.refreshPending = true; return; }
            clearTimeout(this._pollTimer);
            this.poll();
        },
        async poll() {
            if (this.pollBusy || this._disposed) { return; }
            this.pollBusy = true;
            this.refreshPending = false;
            var errors = [];
            var workerVersion = this.workerVersion;
            try {
                if (this.workersPending) {
                    var workers = await this.request("GetWorkersAsync");
                    if (this._disposed || workerVersion !== this.workerVersion) { return; }
                    this.workers = workers;
                    this.workersPending = false;
                    if (!this.workerAlias || !this.workers.some(worker =>
                        worker.alias === this.workerAlias && worker.enabled && worker.hasConfiguredSecret)) {
                        var defaultWorker = this.workers.find(worker => worker.enabled && worker.hasConfiguredSecret);
                        var alias = defaultWorker ? defaultWorker.alias : "";
                        if (this.workerAlias !== alias) {
                            this.workerAlias = alias;
                            this.selectWorker();
                            return;
                        }
                    }
                }
                if (!this.workerAlias) {
                    this.health = null;
                    this.connectionError = "";
                    return;
                }
                var jobsPage = this.jobsPage;
                var methods = ["HealthAsync", "GetJobsPageAsync"];
                var loadCatalog = this.catalogPending;
                this.catalogPending = false;
                if (loadCatalog) { methods.push("GetModelsAsync", "GetDatasetsPageAsync"); }
                // Wait for every request, including failures, before scheduling another cycle.
                var results = await Promise.all(methods.map(method => {
                    var payload = method === "GetJobsPageAsync" ? { offset: (jobsPage - 1) * 10, limit: 10 }
                        : method === "GetDatasetsPageAsync" ? { offset: 0, limit: 100 } : undefined;
                    return this.request(method, payload).then(
                        value => ({ value: value }), error => ({ error: error }));
                }));
                if (this._disposed || workerVersion !== this.workerVersion) { return; }
                for (var index = 0; index < results.length; index++) {
                    var result = results[index];
                    if (result.error) {
                        errors.push(result.error.message);
                        if (index >= 2) { this.catalogPending = true; }
                    } else if (methods[index] === "HealthAsync") {
                        if (this.health && this.health.storeId && result.value.storeId
                            && this.health.storeId !== result.value.storeId) {
                            this.selectWorker();
                            this.storageNotice = this.t("StoreChanged");
                            return;
                        }
                        this.health = result.value;
                    } else if (methods[index] === "GetJobsPageAsync") {
                        if (jobsPage !== this.jobsPage) { continue; }
                        if (result.value.offset !== (jobsPage - 1) * 10 || result.value.limit !== 10) {
                            throw new Error(this.t("Error.InvalidResponse"));
                        }
                        this.jobs = result.value.items;
                        this.jobsTotal = result.value.total;
                        var lastPage = Math.max(1, Math.ceil(this.jobsTotal / 10));
                        if (this.jobsPage > lastPage) { this.changeJobsPage(lastPage); }
                        if (!this.selectedId && this.jobs.length) { this.selectJob(this.jobs[0], false); }
                    } else if (methods[index] === "GetModelsAsync") {
                        this.models = result.value;
                    } else {
                        var datasetPage = result.value;
                        if (datasetPage.offset !== 0 || datasetPage.limit !== 100) {
                            throw new Error(this.t("Error.InvalidResponse"));
                        }
                        var selectedDatasets = Array.from(new Set([this.form.datasetId, this.form.evalDatasetId]))
                            .filter(id => id && !datasetPage.items.some(item => item.id === id));
                        var selectedRecords = await Promise.all(selectedDatasets.map(id => this.request("GetDatasetAsync", { id: id })));
                        if (this._disposed || workerVersion !== this.workerVersion) { return; }
                        this.datasets = datasetPage.items.concat(selectedRecords);
                        this.datasetTotal = datasetPage.total;
                        this.datasetOffset = datasetPage.items.length;
                    }
                }
                if (this.selectedId) {
                    var selectionVersion = this.selectionVersion;
                    try {
                        await this.pollSelectedJob();
                    } catch (error) {
                        if (selectionVersion === this.selectionVersion && workerVersion === this.workerVersion) {
                            this.detailError = error.message;
                            errors.push(error.message);
                        }
                    }
                }
                if (this._disposed || workerVersion !== this.workerVersion) { return; }
                if (!errors.length) { this.lastConnectedUtc = new Date().toISOString(); }
                this.connectionError = Array.from(new Set(errors)).join("\n");
            } catch (error) {
                if (workerVersion === this.workerVersion) {
                    this.connectionError = error.message || this.t("Error.Request");
                }
            } finally {
                this.pollBusy = false;
                this.now = Date.now();
                if (!this._disposed) {
                    this._pollTimer = setTimeout(() => this.poll(), this.refreshPending ? 0 : 2000);
                }
            }
        },
        selectJob(row, refresh) {
            if (this.selectedId === row.id && this.job) { return; }
            this.selectedId = row.id;
            this.clearJobDetails();
            this.detailLoading = true;
            var url = new URL(window.location.href);
            url.searchParams.set("id", row.id);
            url.searchParams.set("worker", this.workerAlias);
            window.history.replaceState(null, "", url.toString());
            if (refresh !== false) { this.requestPoll(); }
        },
        clearJobDetails() {
            this.selectionVersion++;
            this.job = null;
            this.detailError = "";
            this.detailLoading = false;
            this.events = [];
            this.validation = { status: "Idle", startedUtc: null, finishedUtc: null, completed: 0, total: 0, results: [], error: null };
            this.chartPoints = [];
            this.cursor = 0;
            this.catchingUp = false;
            this.eventsTruncated = false;
            if (this._chart) { this._chart.dispose(); this._chart = null; }
        },
        async pollSelectedJob() {
            var id = this.selectedId;
            var version = this.selectionVersion;
            var alias = this.workerAlias;
            try {
                var job = await this.request("GetJobAsync", { id: id }, false, alias);
                if (this._disposed || this.selectionVersion !== version) { return; }
                this.job = job;
                this.detailError = "";
                for (var page = 0; page < 3; page++) {
                    var before = this.cursor;
                    var batch = await this.request("GetEventsAsync", { id: id, after: before, limit: 200 }, false, alias);
                    if (this._disposed || this.selectionVersion !== version) { return; }
                    this.applyEvents(batch, before);
                    if (!batch.hasMore) { break; }
                }
                if (job.state === "Succeeded") {
                    var validation = await this.request("GetValidationAsync", { id: id }, false, alias);
                    if (this._disposed || this.selectionVersion !== version) { return; }
                    this.validation = validation;
                }
                this.$nextTick(() => {
                    if (this._disposed || this.selectionVersion !== version) { return; }
                    this.renderChart();
                    if (this.followLog && this.$refs.eventLog) {
                        this.$refs.eventLog.scrollTop = this.$refs.eventLog.scrollHeight;
                    }
                });
            } catch (error) {
                if (this.selectionVersion !== version || this._disposed) { return; }
                throw error;
            } finally {
                if (this.selectionVersion === version) { this.detailLoading = false; }
            }
        },
        async startValidation() {
            if (this.validationBusy || !this.job || this.job.state !== "Succeeded") { return; }
            var prompts = this.validationPrompts.split(/\r?\n/).map(item => item.trim()).filter(Boolean);
            if (!prompts.length || prompts.length > 8) {
                this.reportAction(new Error(this.t("ValidationPromptError")));
                return;
            }
            this.validationBusy = true;
            try {
                this.validation = await this.request("StartValidationAsync", {
                    id: this.job.id,
                    maxTokens: this.validationMaxTokens,
                    cases: prompts.map((prompt, index) => ({ id: "case-" + (index + 1), prompt: prompt }))
                }, true);
                this.requestPoll();
            } catch (error) {
                this.reportAction(error);
            } finally {
                this.validationBusy = false;
            }
        },
        selectWorker() {
            this.workerVersion++;
            this.health = null;
            this.models = [];
            this.datasets = [];
            this.datasetTotal = 0;
            this.datasetOffset = 0;
            this.jobs = [];
            this.jobsPage = 1;
            this.jobsTotal = 0;
            this.selectedId = "";
            this.clearJobDetails();
            this.form.modelId = "";
            this.form.datasetId = "";
            this.form.evalDatasetId = null;
            this.catalogPending = true;
            this.connectionError = "";
            this.lastConnectedUtc = null;
            this.actionError = "";
            this.storageNotice = "";
            var url = new URL(window.location.href);
            if (this.workerAlias) { url.searchParams.set("worker", this.workerAlias); }
            else { url.searchParams.delete("worker"); }
            url.searchParams.delete("id");
            window.history.replaceState(null, "", url.toString());
            this.requestPoll();
        },
        changeJobsPage(page) {
            this.jobsPage = page;
            this.jobs = [];
            this.requestPoll();
        },
        async loadMoreDatasets() {
            if (this.datasetMoreBusy || this.pollBusy || this.datasetOffset >= this.datasetTotal) { return; }
            var version = this.workerVersion;
            var offset = this.datasetOffset;
            this.datasetMoreBusy = true;
            try {
                var page = await this.request("GetDatasetsPageAsync", { offset: offset, limit: 100 });
                if (version !== this.workerVersion) { throw new Error(this.t("Error.WorkerChanged")); }
                if (page.offset !== offset || page.limit !== 100 || (!page.items.length && offset < page.total)) {
                    throw new Error(this.t("Error.InvalidResponse"));
                }
                var known = new Set(this.datasets.map(item => item.id));
                this.datasets = this.datasets.concat(page.items.filter(item => !known.has(item.id)));
                this.datasetOffset = page.offset + page.items.length;
                this.datasetTotal = page.total;
            } catch (error) {
                this.reportAction(error);
            } finally {
                this.datasetMoreBusy = false;
            }
        },
        openWorkerEditor(worker) {
            this.workerEditor = worker ? {
                id: worker.id, alias: worker.alias, name: worker.name, endpoint: worker.endpoint,
                requestTimeoutSeconds: worker.requestTimeoutSeconds, enabled: worker.enabled, note: worker.note || ""
            } : { id: 0, alias: "", name: "", endpoint: "", requestTimeoutSeconds: 30, enabled: true, note: "" };
            this.workerEditorVisible = true;
        },
        async saveWorker() {
            if (this.workerSaveBusy) { return; }
            this.workerSaveBusy = true;
            try {
                var previous = this.workers.find(item => item.id === this.workerEditor.id);
                var worker = await this.request("SaveWorkerAsync", this.workerEditor, true);
                var index = this.workers.findIndex(item => item.id === worker.id);
                if (index >= 0) { this.$set(this.workers, index, worker); } else { this.workers.push(worker); }
                this.workersPending = true;
                this.workerEditorVisible = false;
                this.$message.success(this.t("WorkerSaved"));
                if ((previous && previous.alias === this.workerAlias) || !this.workerAlias) {
                    this.workerAlias = worker.enabled && worker.hasConfiguredSecret ? worker.alias : "";
                    this.selectWorker();
                }
                this.requestPoll();
            } catch (error) {
                this.reportAction(error);
            } finally {
                this.workerSaveBusy = false;
            }
        },
        applyEvents(batch, before) {
            if (batch.truncated) { this.eventsTruncated = true; }
            var ordered = batch.events.slice().sort((a, b) => a.sequence - b.sequence);
            var maxSequence = ordered.length ? ordered[ordered.length - 1].sequence : before;
            if (batch.nextCursor < before || batch.nextCursor < maxSequence
                || (batch.hasMore && batch.nextCursor <= before)) {
                throw new Error(this.t("Error.Cursor"));
            }
            var accepted = [];
            var last = before;
            ordered.forEach(event => {
                if (event.sequence <= last) { return; }
                accepted.push(event);
                last = event.sequence;
                var step = this.readMetric(event.metrics, "step");
                var trainLoss = this.readMetric(event.metrics, "trainLoss");
                var evalLoss = this.readMetric(event.metrics, "evalLoss");
                var learningRate = this.readMetric(event.metrics, "learningRate");
                if (step !== null && (trainLoss !== null || evalLoss !== null || learningRate !== null)) {
                    this.chartPoints.push({ step: step, trainLoss: trainLoss, evalLoss: evalLoss, learningRate: learningRate });
                }
            });
            this.events = this.events.concat(accepted).slice(-1000);
            this.chartPoints = this.chartPoints.slice(-1000);
            this.cursor = batch.nextCursor;
            this.catchingUp = batch.hasMore;
        },
        backendCompatible(backend) {
            if (!this.health) { return false; }
            var cap = this.health.capabilities.find(item => item.backend === backend);
            var model = this.models.find(item => item.id === this.form.modelId);
            return !!cap && cap.available && (!model || model.backends.indexOf(backend) >= 0);
        },
        methodCompatible(method) {
            if (!this.health) { return false; }
            var cap = this.health.capabilities.find(item => item.backend === this.form.backend);
            return !!cap && cap.available && cap.methods.indexOf(method) >= 0;
        },
        reportAction(error) {
            this.actionError = error.message || this.t("ActionError");
            this.$message.error(this.actionError);
        },
        validDatasetRow(row) {
            var text = value => typeof value === "string" && !!value.trim()
                && Array.from(value).length <= 65536 && value.indexOf("\0") < 0;
            var keys = Object.keys(row).sort().join(",");
            if (keys === "completion,prompt") { return text(row.prompt) && text(row.completion); }
            if (keys !== "messages" || !Array.isArray(row.messages)
                || row.messages.length < 2 || row.messages.length > 128) { return false; }
            var expected = "user";
            for (var index = 0; index < row.messages.length; index++) {
                var message = row.messages[index];
                if (!this.isRecord(message) || Object.keys(message).sort().join(",") !== "content,role"
                    || !text(message.content)) { return false; }
                if (index === 0 && message.role === "system") { continue; }
                if (message.role !== expected) { return false; }
                expected = expected === "user" ? "assistant" : "user";
            }
            return expected === "user";
        },
        validateDatasetContent(content) {
            if (new TextEncoder().encode(content).length > 2 * 1024 * 1024) {
                throw new Error(this.t("Error.FileSize"));
            }
            var lines = content.split(/\r\n|\r|\n/);
            if (lines[lines.length - 1] === "") { lines.pop(); }
            if (lines.length > 10000) { throw new Error(this.t("Error.RowLimit")); }
            lines.forEach((line, index) => {
                var row;
                try { row = JSON.parse(line); }
                catch (error) {
                    if (error instanceof SyntaxError) {
                        throw new Error(this.t("Error.JsonLine") + " " + (index + 1));
                    }
                    throw error;
                }
                if (!this.isRecord(row) || !this.validDatasetRow(row)) {
                    throw new Error(this.t("Error.DatasetSchema") + " " + (index + 1));
                }
            });
            if (lines.length < 2) { throw new Error(this.t("Error.EmptyDataset")); }
            return lines.length;
        },
        downloadSample() {
            var url = URL.createObjectURL(new Blob([this.sampleContent], { type: "application/x-ndjson;charset=utf-8" }));
            var link = document.createElement("a");
            link.href = url;
            link.download = "fine-tuning-" + this.datasetExample + "-example.jsonl";
            document.body.appendChild(link);
            link.click();
            link.remove();
            setTimeout(() => URL.revokeObjectURL(url), 0);
        },
        async readDataset(event) {
            this.datasetContent = "";
            this.fileName = "";
            this.actionError = "";
            var file = event.target.files && event.target.files[0];
            if (!file) { return; }
            this.fileReading = true;
            try {
                if (!/\.jsonl$/i.test(file.name)) { throw new Error(this.t("Error.FileType")); }
                if (file.size > 2 * 1024 * 1024) { throw new Error(this.t("Error.FileSize")); }
                var buffer = await new Promise((resolve, reject) => {
                    var reader = new FileReader();
                    reader.onload = () => resolve(reader.result);
                    reader.onerror = () => reject(new Error(this.t("Error.FileRead")));
                    reader.onabort = () => reject(new Error(this.t("Error.FileRead")));
                    reader.readAsArrayBuffer(file);
                });
                var content;
                try {
                    content = new TextDecoder("utf-8", { fatal: true }).decode(buffer);
                } catch (error) {
                    if (error instanceof TypeError) { throw new Error(this.t("Error.Utf8")); }
                    throw error;
                }
                var rows = this.validateDatasetContent(content);
                this.datasetContent = content;
                this.fileName = file.name + " (" + rows + " " + this.t("Rows") + ")";
                if (!this.datasetName.trim()) { this.datasetName = file.name.replace(/\.jsonl$/i, "").slice(0, 100); }
            } catch (error) {
                this.reportAction(error);
                event.target.value = "";
            } finally {
                this.fileReading = false;
            }
        },
        async uploadDataset() {
            if (this.uploadBusy) { return; }
            var workerVersion = this.workerVersion;
            this.actionError = "";
            this.uploadBusy = true;
            try {
                if (!this.connectionHealthy) { throw new Error(this.t("Error.NotConnected")); }
                if (!this.datasetName.trim() || !this.datasetContent || this.fileReading) {
                    throw new Error(this.t("Error.DatasetRequired"));
                }
                var dataset = await this.request("UploadDatasetAsync",
                    { name: this.datasetName.trim(), content: this.datasetContent }, true);
                if (workerVersion !== this.workerVersion) { throw new Error(this.t("Error.WorkerChanged")); }
                this.datasets = this.datasets.filter(item => item.id !== dataset.id).concat([dataset]);
                this.datasetTotal++;
                if (!this.form.datasetId) { this.form.datasetId = dataset.id; }
                this.$message.success(this.t("UploadSucceeded"));
                this.datasetContent = "";
                this.fileName = "";
                this.datasetName = "";
                document.getElementById("ft-dataset-file").value = "";
                this.catalogPending = true;
                this.requestPoll();
            } catch (error) {
                this.reportAction(error);
            } finally {
                this.uploadBusy = false;
            }
        },
        validateForm() {
            if (!this.connectionHealthy) { throw new Error(this.t("Error.NotConnected")); }
            if (this.compatibilityError) { throw new Error(this.compatibilityError); }
            if (!this.form.name.trim() || !this.models.some(model => model.id === this.form.modelId)
                || !this.datasets.some(dataset => dataset.id === this.form.datasetId)) {
                throw new Error(this.t("Error.JobRequired"));
            }
            if (this.form.evalDatasetId) {
                var training = this.datasets.find(dataset => dataset.id === this.form.datasetId);
                var evaluation = this.datasets.find(dataset => dataset.id === this.form.evalDatasetId);
                if (!evaluation || evaluation.id === training.id || evaluation.sha256 === training.sha256) {
                    throw new Error(this.t("Error.Heldout"));
                }
            }
            this.parameters.forEach(parameter => {
                var value = this.form[parameter.key];
                if (typeof value !== "number" || !Number.isFinite(value) || value < parameter.min
                    || value > parameter.max || (parameter.precision === undefined && !Number.isInteger(value))) {
                    throw new Error(this.t("Error.Parameter") + ": " + this.t("Parameter." + parameter.key));
                }
            });
            var targets = this.form.targetModules.trim();
            if (targets.length > 512 || (targets !== "all-linear" && !/^[A-Za-z0-9_.]+(?:,[A-Za-z0-9_.]+)*$/.test(targets))) {
                throw new Error(this.t("Error.TargetModules"));
            }
        },
        async createJob() {
            if (this.createBusy) { return; }
            var workerVersion = this.workerVersion;
            this.actionError = "";
            this.createBusy = true;
            try {
                this.validateForm();
                var request = Object.assign({}, this.form, {
                    name: this.form.name.trim(), targetModules: this.form.targetModules.trim(),
                    evalDatasetId: this.form.evalDatasetId || null
                });
                var job = await this.request("CreateJobAsync", request, true);
                if (workerVersion !== this.workerVersion) { throw new Error(this.t("Error.WorkerChanged")); }
                this.jobs = [job].concat(this.jobs.filter(item => item.id !== job.id));
                this.jobsTotal++;
                this.jobsPage = 1;
                this.jobs = this.jobs.slice(0, 10);
                this.selectJob(job);
                this.$message.success(this.t("JobSubmitted"));
            } catch (error) {
                this.reportAction(error);
                this.advancedOpen = ["parameters"];
            } finally {
                this.createBusy = false;
            }
        },
        canCancel(job) {
            return ["Queued", "Running"].indexOf(job.state) >= 0;
        },
        async cancelJob() {
            if (this.cancelBusy || !this.job || !this.canCancel(this.job)) { return; }
            var id = this.job.id;
            var alias = this.workerAlias;
            var version = this.selectionVersion;
            this.cancelBusy = true;
            try {
                try {
                    await this.$confirm(this.t("CancelConfirm"), this.t("CancelJob"), {
                        type: "warning", confirmButtonText: this.t("Confirm"), cancelButtonText: this.t("KeepTraining")
                    });
                } catch (error) {
                    if (error === "cancel" || error === "close") { return; }
                    throw error;
                }
                if (alias !== this.workerAlias) { throw new Error(this.t("Error.WorkerChanged")); }
                var job = await this.request("CancelJobAsync", { id: id }, true, alias);
                if (this.selectionVersion === version) { this.job = job; }
                this.$message.success(this.t("CancelRequested"));
                this.requestPoll();
            } catch (error) {
                this.reportAction(error);
            } finally {
                this.cancelBusy = false;
            }
        },
        readMetric(metrics, key) {
            var bytesKeys = { rss: "processRssBytes", gpuMemory: "gpuMemoryBytes",
                gpuAllocated: "gpuAllocatedBytes", diskFree: "diskFreeBytes", jobOutput: "jobOutputBytes" };
            var bytesKey = bytesKeys[key];
            if (bytesKey && metrics && Object.prototype.hasOwnProperty.call(metrics, bytesKey)) {
                var bytes = metrics[bytesKey];
                return typeof bytes === "number" && Number.isFinite(bytes) ? bytes / 1024 / 1024 : null;
            }
            var aliases = {
                step: ["step", "globalStep", "global_step"], totalSteps: ["totalSteps", "total_steps"],
                trainLoss: ["trainLoss", "loss", "train_loss"], evalLoss: ["evalLoss", "eval_loss"],
                learningRate: ["learningRate", "learning_rate"],
                rss: ["rssMb", "rssMiB", "rss_mb", "rss_mib"],
                cpu: ["processCpuPercent", "cpuPercent", "cpu_percent"],
                gpuUtil: ["gpuUtilizationPercent", "gpuUtilization", "gpuUtilPercent", "gpu_utilization", "gpu_util_percent"],
                gpuMemory: ["gpuMemoryMb", "gpuMemoryMiB", "gpu_memory_mb", "gpu_memory_mib"],
                gpuTemperature: ["gpuTemperatureCelsius", "gpuTemperatureC", "gpu_temperature_c"]
            };
            var names = aliases[key] || [key];
            for (var index = 0; index < names.length; index++) {
                var value = metrics && metrics[names[index]];
                if (typeof value === "number" && Number.isFinite(value)) { return value; }
            }
            return null;
        },
        metric(key) {
            return this.readMetric(this.job && this.job.latestMetrics, key);
        },
        formatMetric(value, unit) {
            if (value === null || value === undefined || !Number.isFinite(value)) { return this.t("Unavailable"); }
            var text = value !== 0 && Math.abs(value) < 0.0001 ? value.toExponential(3)
                : Number(value.toFixed(4)).toLocaleString(undefined, { maximumFractionDigits: 4 });
            return text + (unit ? " " + unit : "");
        },
        formatDate(value) {
            if (!value) { return this.t("Unavailable"); }
            var date = new Date(value);
            return Number.isNaN(date.getTime()) ? this.t("Unavailable") : date.toLocaleString();
        },
        formatBytes(bytes) {
            return bytes < 1024 * 1024 ? (bytes / 1024).toFixed(1) + " KiB" : (bytes / 1024 / 1024).toFixed(1) + " MiB";
        },
        stateText(state) {
            return this.t("State." + state);
        },
        stateType(state) {
            if (state === "Succeeded") { return "success"; }
            if (state === "Failed" || state === "Interrupted") { return "danger"; }
            if (state === "Cancelling") { return "warning"; }
            return "info";
        },
        jobRowClass(context) {
            return context.row.id === this.selectedId ? "ft-selected-row" : "";
        },
        artifactUrl(artifact) {
            var url = new URL(window.location.href);
            var uid = url.searchParams.get("uid");
            url.search = "";
            url.hash = "";
            url.searchParams.set("handler", "Artifact");
            url.searchParams.set("workerAlias", this.workerAlias);
            url.searchParams.set("id", this.job.id);
            url.searchParams.set("artifactId", artifact.id);
            if (uid) { url.searchParams.set("uid", uid); }
            return url.toString();
        },
        renderChart() {
            if (!this.chartAvailable || !this.$refs.trainingChart) { return; }
            if (!this._chart) { this._chart = echarts.init(this.$refs.trainingChart); }
            var series = ["trainLoss", "evalLoss", "learningRate"].map(key => ({
                name: this.t("Metric." + key), type: "line", showSymbol: false,
                connectNulls: false, yAxisIndex: key === "learningRate" ? 1 : 0,
                data: this.chartPoints.filter(point => point[key] !== null).map(point => [point.step, point[key]])
            }));
            this._chart.setOption({
                animation: false,
                tooltip: { trigger: "axis", renderMode: "richText" },
                legend: { data: series.map(item => item.name) },
                grid: { left: 65, right: 85, top: 55, bottom: 55 },
                xAxis: { type: "value", name: this.t("Steps"), minInterval: 1 },
                yAxis: [
                    { type: "value", name: this.t("Loss"), scale: true },
                    { type: "value", name: this.t("Metric.learningRate"), scale: true }
                ],
                series: series
            }, true);
            this._chart.resize();
        }
    }
});
