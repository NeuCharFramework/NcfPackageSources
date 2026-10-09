const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");
const { test } = require("node:test");
const root = path.resolve(__dirname, "../../../src/Extensions/Senparc.Xncf.AIKernel");
const source = fs.readFileSync(path.join(root, "wwwroot/js/Admin/AIFineTuning/index.js"), "utf8");

function instance() {
    let options;
    const timers = [];
    const en = fs.readFileSync(path.join(root, "Resources/AIKernelResource.en.resx"), "utf8");
    const zh = fs.readFileSync(path.join(root, "Resources/AIKernelResource.zh-CN.resx"), "utf8");
    const fallback = fs.readFileSync(path.join(root, "Resources/AIKernelResource.resx"), "utf8");
    const context = {
        URL, Date, TextDecoder, TextEncoder, Blob, Number, console,
        window: { location: { href: "http://localhost/Admin/AIFineTuning/Index?uid=module" }, history: { replaceState() {} } },
        document: { getElementById: () => ({ value: "" }) },
        service: {},
        setTimeout: (callback, delay) => { timers.push({ callback, delay }); return timers.length; },
        clearTimeout() {},
        ncfT: key => {
            const name = key.replace("AIKernel.", "");
            for (const resource of [en, zh, fallback]) {
                assert.ok(resource.includes('name="' + name + '"'), "Missing translation: " + key);
            }
            return name;
        },
        Vue: function (value) { options = value; }
    };
    vm.runInNewContext(source, context);
    const app = options.data();
    for (const [key, method] of Object.entries(options.methods)) { app[key] = method.bind(app); }
    for (const [key, method] of Object.entries(options.computed)) {
        Object.defineProperty(app, key, { get: method.bind(app) });
    }
    app.$message = { error() {}, success() {} };
    app.$set = (array, index, value) => { array[index] = value; };
    app.$nextTick = callback => callback();
    app.$refs = {};
    app._disposed = false;
    app._chart = null;
    return { app, context, timers };
}

function job(id = "job-a", metrics = {}) {
    return { id, name: id, state: "Running", createdUtc: "2026-10-02T00:00:00Z",
        request: { backend: "cpu", method: "lora" }, latestMetrics: metrics, lastEventSequence: 0, artifacts: [] };
}

function deferred() {
    let resolve;
    const promise = new Promise(yes => { resolve = yes; });
    return { promise, resolve };
}

function catalog(items = [], offset = 0, limit = 10, total = items.length) {
    return { items, offset, limit, total };
}

test("parameter and metric labels exist in both resource catalogs", () => {
    const { app } = instance();
    for (const parameter of app.parameters) {
        assert.ok(app.form[parameter.key] >= parameter.min && app.form[parameter.key] <= parameter.max);
        app.t("Parameter." + parameter.key);
        app.t("Tip." + parameter.key);
    }
    for (const metric of app.metricCards) { app.t("Metric." + metric.key); }
    app.t("EventsTruncated");
});

test("progress and resource metrics remain truthful, including MLX allocation and missing sensors", () => {
    const { app } = instance();
    app.job = job("a", { step: 4, totalSteps: 10, trainLoss: 0, gpuMemoryBytes: null,
        gpuAllocatedBytes: 1048576, diskFreeBytes: 2097152 });
    assert.equal(app.progressPercent, 40);
    assert.equal(app.metric("trainLoss"), 0);
    assert.equal(app.metric("gpuMemory"), null);
    assert.equal(app.metric("gpuAllocated"), 1);
    assert.equal(app.metric("diskFree"), 2);
    app.job.latestMetrics = {};
    assert.equal(app.progressPercent, null);
});

test("event replay is bounded, monotonic and explicitly identifies retained partial history", () => {
    const { app } = instance();
    const events = Array.from({ length: 1200 }, (_, index) => ({
        sequence: index + 20, kind: "metrics", timestampUtc: "2026-10-02T00:00:00Z",
        metrics: { step: index, trainLoss: 1 / (index + 1) }
    }));
    app.applyEvents({ events, nextCursor: 1219, hasMore: false, truncated: true }, 0);
    assert.equal(app.events.length, 1000);
    assert.equal(app.chartPoints.length, 1000);
    assert.equal(app.cursor, 1219);
    assert.equal(app.eventsTruncated, true);
    assert.throws(() => app.applyEvents({ events: [], nextCursor: 1219, hasMore: true }, 1219));
    app.selectJob(job("b"), false);
    assert.equal(app.eventsTruncated, false);
    assert.equal(app.cursor, 0);
});

test("polling cannot overlap and reconnect uses the persisted event cursor", async () => {
    const { app, timers } = instance();
    const gate = deferred();
    let calls = 0;
    app.catalogPending = false;
    app.workersPending = false;
    app.workerAlias = "cpu-lab";
    app.request = async method => {
        calls++;
        if (method === "HealthAsync") { await gate.promise; throw new Error("offline"); }
        return catalog();
    };
    const pending = app.poll();
    await app.poll();
    assert.equal(calls, 2);
    assert.equal(timers.length, 0);
    gate.resolve();
    await pending;
    assert.equal(app.connectionError, "offline");
    assert.equal(timers.length, 1);
    app.selectedId = "a";
    app.cursor = 42;
    app.request = async (method, payload) => {
        if (method === "GetJobAsync") { return job("a"); }
        assert.equal(payload.after, 42);
        return { events: [], nextCursor: 42, hasMore: false };
    };
    await app.pollSelectedJob();
    assert.equal(app.cursor, 42);
});

test("job switching prevents stale requests from updating another job", async () => {
    const { app } = instance();
    const gate = deferred();
    app.selectedId = "a";
    app.request = () => gate.promise;
    const pending = app.pollSelectedJob();
    app.selectJob(job("b"), false);
    gate.resolve(job("a"));
    await pending;
    assert.equal(app.selectedId, "b");
    assert.equal(app.job, null);
});

test("error envelopes and malformed worker contracts are never success-shaped fallbacks", async () => {
    const { app, context } = instance();
    context.service.get = async () => ({ data: { success: false, errorMessage: "Worker disabled" } });
    await assert.rejects(app.request("HealthAsync"), /Worker disabled/);
    context.service.get = async () => ({ data: { success: true, data: {} } });
    await assert.rejects(app.request("HealthAsync"), /InvalidResponse/);
});

test("artifact links preserve module UID without exposing the worker or its key", () => {
    const { app } = instance();
    app.job = job("job-a");
    const url = new URL(app.artifactUrl({ id: "adapter" }));
    assert.equal(url.pathname, "/Admin/AIFineTuning/Index");
    assert.equal(url.searchParams.get("handler"), "Artifact");
    assert.equal(url.searchParams.get("uid"), "module");
    assert.equal(url.searchParams.get("artifactId"), "adapter");
    assert.equal(url.searchParams.has("WorkerApiKey"), false);
});

test("tutorial, troubleshooting and all literal UI labels have bilingual and fallback resources", () => {
    const { app } = instance();
    const page = fs.readFileSync(path.join(root, "Areas/Admin/Pages/AIFineTuning/Index.cshtml"), "utf8");
    for (const match of (source + page).matchAll(/\bt\(["']([A-Za-z0-9_.]+)["']\)/g)) {
        if (!match[1].endsWith(".")) { app.t(match[1]); }
    }
    for (const step of ["Worker", "Data", "Training", "Monitor", "Publish"]) {
        app.t("Guide." + step + "Title");
        app.t("Guide." + step);
    }
    for (const topic of ["Connection", "Dataset", "Memory", "Interrupted"]) {
        app.t("Guide.Error." + topic);
    }
    assert.ok(page.includes("WorkerApiKeys:&lt;alias&gt;"));
    assert.ok(!page.includes("Enabled / WorkerEndpoint / WorkerApiKey"));
    assert.equal(app.helpOpen[0], "workflow");
});

test("both downloadable examples are valid distinct JSONL and downloads preserve their exact content", async () => {
    const { app, context, timers } = instance();
    let blob, clicked = false, removed = false, revoked = false;
    const link = { click() { clicked = true; }, remove() { removed = true; } };
    class DownloadURL extends URL {}
    DownloadURL.createObjectURL = value => { blob = value; return "blob:example"; };
    DownloadURL.revokeObjectURL = value => { assert.equal(value, "blob:example"); revoked = true; };
    context.URL = DownloadURL;
    context.document.createElement = () => link;
    context.document.body = { appendChild() {} };
    for (const format of ["prompt", "messages"]) {
        app.datasetExample = format;
        assert.equal(app.validateDatasetContent(app.sampleContent), 4);
        assert.equal(new Set(app.sampleContent.trim().split("\n")).size, 4);
        app.downloadSample();
        assert.equal(await blob.text(), app.sampleContent);
        assert.equal(link.download, "fine-tuning-" + format + "-example.jsonl");
    }
    assert.ok(clicked && removed);
    timers.forEach(timer => timer.callback());
    assert.ok(revoked);
});

test("dataset validation enforces actual schema, row and UTF-8 byte thresholds with line errors", () => {
    const { app } = instance();
    const row = '{"prompt":"x","completion":"y"}';
    assert.equal(app.validateDatasetContent(Array(10000).fill(row).join("\n")), 10000);
    assert.throws(() => app.validateDatasetContent(Array(10001).fill(row).join("\n")), /RowLimit/);
    assert.throws(() => app.validateDatasetContent(row), /EmptyDataset/);
    assert.throws(() => app.validateDatasetContent(row + "\n\n" + row), /JsonLine.*2/);
    assert.throws(() => app.validateDatasetContent("\u4e2d".repeat(700000)), /FileSize/);
    const maximumRows = Array.from({ length: 31 }, () => ({ prompt: "x".repeat(65536), completion: "y" }));
    maximumRows.push({ prompt: "", completion: "y" });
    const encode = () => maximumRows.map(row => JSON.stringify(row)).join("\n");
    maximumRows[31].prompt = "x".repeat(2 * 1024 * 1024 - new TextEncoder().encode(encode()).length);
    assert.equal(new TextEncoder().encode(encode()).length, 2097152);
    assert.equal(app.validateDatasetContent(encode()), 32);
    assert.throws(() => app.validateDatasetContent(encode() + "\n"), /FileSize/);
    for (const invalid of [
        '{"prompt":"x","completion":"y","extra":true}',
        '{"prompt":" ","completion":"y"}',
        '{"messages":[{"role":"user","content":"x"},{"role":"system","content":"y"}]}',
        '{"messages":[{"role":"tool","content":"x"},{"role":"assistant","content":"y"}]}',
        '{"messages":[{"role":"user","content":"x"},{"role":"assistant","content":"y","extra":true}]}',
        JSON.stringify({ prompt: "x".repeat(65537), completion: "y" }),
        JSON.stringify({ prompt: "x\0", completion: "y" })
    ]) {
        assert.throws(() => app.validateDatasetContent(row + "\n" + invalid), /DatasetSchema.*2/);
    }
    assert.equal(app.validDatasetRow({
        messages: [{ role: "system", content: "help" }, { role: "user", content: "x" }, { role: "assistant", content: "y" }]
    }), true);
    assert.equal(app.validDatasetRow({ prompt: "\u{1f600}".repeat(65536), completion: "y" }), true);
});

test("switching Workers resets scoped selections, charts and cursors and discards old poll responses", async () => {
    const { app } = instance();
    const gate = deferred();
    app.workerAlias = "cpu-lab";
    app.workersPending = false;
    app.catalogPending = false;
    app.form.modelId = "old-model";
    app.form.datasetId = "old-data";
    app.form.evalDatasetId = "old-eval";
    app.cursor = 99;
    app.chartPoints = [{ step: 1 }];
    app.eventsTruncated = true;
    let disposed = false;
    app._chart = { dispose() { disposed = true; } };
    app.request = method => method === "HealthAsync" ? gate.promise : Promise.resolve([]);
    const polling = app.poll();
    app.workerAlias = "mlx-lab";
    app.selectWorker();
    gate.resolve({ status: "ok", version: "old", activeJobs: 0, queuedJobs: 0, capabilities: [] });
    await polling;
    assert.equal(app.health, null);
    assert.equal(app.form.modelId, "");
    assert.equal(app.form.datasetId, "");
    assert.equal(app.form.evalDatasetId, null);
    assert.equal(app.cursor, 0);
    assert.equal(app.chartPoints.length, 0);
    assert.equal(app.eventsTruncated, false);
    assert.ok(disposed);
    assert.equal(app.catalogPending, true);
});

test("Worker switching also invalidates outstanding details and cancel confirmations", async () => {
    const { app } = instance();
    const details = deferred();
    app.workerAlias = "cpu-lab";
    app.selectedId = "same-id";
    app.request = () => details.promise;
    const pendingDetails = app.pollSelectedJob();
    app.pollBusy = true;
    app.workerAlias = "mlx-lab";
    app.selectWorker();
    details.resolve(job("same-id"));
    await pendingDetails;
    assert.equal(app.job, null);
    app.job = job("same-id");
    app.selectedId = "same-id";
    const confirmation = deferred();
    app.$confirm = () => confirmation.promise;
    app.request = () => { throw new Error("Must not cancel another Worker's job"); };
    const cancellation = app.cancelJob();
    app.workerAlias = "cpu-lab";
    app.selectWorker();
    confirmation.resolve();
    await cancellation;
    assert.match(app.actionError, /WorkerChanged/);
    assert.equal(app.job, null);
});

test("browser request timeout follows the selected database Worker profile", async () => {
    const { app, context } = instance();
    app.workerAlias = "cpu-lab";
    app.workers = [{ alias: "cpu-lab", requestTimeoutSeconds: 180 }];
    context.service.get = async (url, config) => {
        assert.equal(config.timeout, 185000);
        assert.equal(config.params.workerAlias, "cpu-lab");
        return { data: { success: true, data: {
            status: "ok", version: "1", activeJobs: 0, queuedJobs: 0, capabilities: []
        } } };
    };
    await app.request("HealthAsync");
});

test("completed uploads and submissions cannot populate another Worker's catalog", async () => {
    for (const action of ["uploadDataset", "createJob"]) {
        const { app } = instance();
        app.workerAlias = "cpu-lab";
        app.pollBusy = true;
        app.health = { status: "ok", storeId: "test-store", capabilities: [{ backend: "cpu", available: true, methods: ["lora"] }] };
        app.lastConnectedUtc = new Date().toISOString();
        app.models = [{ id: "model", backends: ["cpu"] }];
        app.datasets = [{ id: "data", sha256: "a".repeat(64) }];
        app.form.name = "test";
        app.form.modelId = "model";
        app.form.datasetId = "data";
        app.datasetName = "test";
        app.datasetContent = app.sampleContent;
        const gate = deferred();
        app.request = () => gate.promise;
        const pending = app[action]();
        app.workerAlias = "mlx-lab";
        app.selectWorker();
        gate.resolve(action === "uploadDataset" ? { id: "old-data" } : job("old-job"));
        await pending;
        assert.equal(app.datasets.length, 0);
        assert.equal(app.jobs.length, 0);
        assert.equal(app.selectedId, "");
        assert.match(app.actionError, /WorkerChanged/);
    }
});

test("server job paging reaches old records without a second client-side slice", async () => {
    const { app } = instance();
    app.workerAlias = "cpu-lab";
    app.workersPending = false;
    app.catalogPending = false;
    app.jobsPage = 11;
    app.pollSelectedJob = async () => {};
    app.request = async (method, payload) => {
        if (method === "HealthAsync") { return { status: "ok", storeId: "same-store" }; }
        assert.equal(method, "GetJobsPageAsync");
        assert.equal(payload.offset, 100);
        assert.equal(payload.limit, 10);
        return catalog(Array.from({ length: 10 }, (_, index) => job("old-" + index)), 100, 10, 135);
    };
    await app.poll();
    assert.equal(app.jobsTotal, 135);
    assert.equal(app.jobsPage, 11);
    assert.equal(app.pagedJobs.length, 10);
    assert.equal(app.connectionError, "");
});

test("dataset paging loads older records and refresh preserves selected historical datasets", async () => {
    const { app } = instance();
    const dataset = index => ({ id: "data-" + index, name: "data-" + index, rows: 2,
        sha256: "a".repeat(64), createdUtc: "2026-10-07T00:00:00Z" });
    app.datasets = Array.from({ length: 100 }, (_, index) => dataset(index));
    app.datasetOffset = 100;
    app.datasetTotal = 101;
    app.request = async (method, payload) => {
        assert.equal(method, "GetDatasetsPageAsync");
        assert.equal(payload.offset, 100);
        return catalog([dataset(100)], 100, 100, 101);
    };
    await app.loadMoreDatasets();
    assert.equal(app.datasets.length, 101);
    assert.equal(app.datasetOffset, 101);
    app.form.datasetId = "data-100";
    app.workerAlias = "cpu-lab";
    app.workersPending = false;
    app.catalogPending = true;
    app.request = async (method, payload) => {
        if (method === "HealthAsync") { return { status: "ok", storeId: "same-store" }; }
        if (method === "GetJobsPageAsync") { return catalog(); }
        if (method === "GetModelsAsync") { return []; }
        if (method === "GetDatasetAsync") {
            assert.equal(payload.id, "data-100");
            return dataset(100);
        }
        return catalog(Array.from({ length: 100 }, (_, index) => dataset(index)), 0, 100, 101);
    };
    await app.poll();
    assert.equal(app.datasets.length, 101);
    assert.equal(app.form.datasetId, "data-100");
    assert.equal(app.datasetOffset, 100);
    assert.equal(app.connectionError, "");
});

test("replacing storage on the same Worker resets scoped resources and reports the change", async () => {
    const { app } = instance();
    app.health = { storeId: "old-store", status: "ok" };
    app.workerAlias = "cpu-lab";
    app.workersPending = false;
    app.catalogPending = false;
    app.selectedId = "old-job";
    app.form.datasetId = "old-data";
    app.cursor = 99;
    app.request = async method => method === "HealthAsync"
        ? { status: "ok", storeId: "new-store" } : catalog([job("new-job")]);
    await app.poll();
    assert.equal(app.selectedId, "");
    assert.equal(app.form.datasetId, "");
    assert.equal(app.cursor, 0);
    assert.equal(app.health, null);
    assert.match(app.storageNotice, /StoreChanged/);
});

test("catalog contracts and unhealthy or old Workers cannot enable submission", () => {
    const { app } = instance();
    assert.throws(() => app.validateResponse("GetJobsPageAsync", catalog([], 0, 10, 1)), /InvalidResponse/);
    app.health = { status: "stopping", capabilities: [{ backend: "cpu", available: true, methods: ["lora"] }] };
    assert.match(app.compatibilityError, /WorkerNotReady/);
    app.health.status = "ok";
    assert.match(app.compatibilityError, /WorkerVersionMismatch/);
});

test("creating and editing Workers validates Worker DTOs rather than job DTOs", async () => {
    const { app, context } = instance();
    app.pollBusy = true;
    app.workerEditorVisible = true;
    app.workerEditor = { id: 0, alias: "cpu_lab", name: "CPU lab", endpoint: "http://127.0.0.1:8091",
        requestTimeoutSeconds: 180, enabled: true, note: "" };
    context.service.post = async (url, payload) => ({
        data: { success: true, data: { ...payload, id: 1, hasConfiguredSecret: true } }
    });
    await app.saveWorker();
    assert.equal(app.actionError, "");
    assert.equal(app.workers.length, 1);
    assert.equal(app.workerAlias, "cpu_lab");
    assert.equal(app.workerEditorVisible, false);
    app.openWorkerEditor(app.workers[0]);
    app.workerEditor.name = "Updated CPU lab";
    app.workerEditor.requestTimeoutSeconds = 60;
    await app.saveWorker();
    assert.equal(app.actionError, "");
    assert.equal(app.workers.length, 1);
    assert.equal(app.workers[0].name, "Updated CPU lab");
    assert.equal(app.workers[0].requestTimeoutSeconds, 60);
    assert.throws(() => app.validateResponse("SaveWorkerAsync", job()), /InvalidResponse/);
});

test("NCF mutation APIs route Worker aliases through query and keep JSON bodies typed", async () => {
    const { app, context } = instance();
    app.workerAlias = "cpu_lab";
    context.service.post = async (url, payload, config) => {
        assert.equal(config.params.workerAlias, "cpu_lab");
        assert.equal(Object.hasOwn(payload, "workerAlias"), false);
        if (url.endsWith("UploadDatasetAsync")) {
            assert.equal(payload.name, "training");
            return { data: { success: true, data: { id: "data", name: "training", rows: 2,
                sha256: "a".repeat(64), createdUtc: "2026-10-07T00:00:00Z" } } };
        }
        return { data: { success: true, data: job() } };
    };
    await app.request("UploadDatasetAsync", { name: "training", content: app.sampleContent }, true);
    await app.request("CreateJobAsync", app.form, true);
    await app.request("CancelJobAsync", { id: "job-a" }, true);
});
