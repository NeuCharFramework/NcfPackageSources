# Senparc.Xncf.AIKernel

`Senparc.Xncf.AIKernel` is the NCF AI model and vector configuration module. It provides persistence, administration services, and integration helpers for Senparc AI model settings.

## Features

- Manages `AIModel` and `AIVector` records and their DTO/request models.
- Provides model and vector application services with paging, create, edit, and delete operations.
- Builds `SenparcAiSetting` values and runs model requests through the configured Senparc AI runtime.
- Monitors AI token usage with real-time in-process aggregation (`AITokenMonitorService`) and persistent, queryable usage records (`AITokenUsageService`).
- Separates real-time token totals and run-progress buffers by the resolved tenant; monitor endpoints read only the current tenant's partition.
- Publishes per-run token progress as asynchronous events consumable via `IAsyncEnumerable` (`SubscribeAsync`), with buffered replay for late subscribers.
- Surfaces token usage on the AI model list page: overview cards (total tokens / calls / success-error / average duration), per-model usage columns (calls, total tokens with relative bar, last used), alias search, and a shortcut to the Token Monitor page.
- Synchronizes model metadata from NeuChar services when explicitly requested.
- Provides an administrator-only local fine-tuning console with validated JSONL datasets, worker preflight, durable jobs, loss/resource telemetry, cancellation and downloadable adapters.
- Connects to an independent authenticated Python worker: CPU/CUDA PEFT training in restricted containers, or native Apple Silicon MLX training. Training is disabled until explicitly configured.
- Supports NCF's multi-database context variants and localized module metadata.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AIKernel" Version="0.16.4" />
```

## Key API

- `AIModelService.AddAsync(...)`, `EditAsync(...)`, and `BuildSenparcAiSetting(...)` manage model configuration.
- `AIModelService.RunModelsync(...)` executes a configured model request.
- `AIModelService.UpdateModelsFromNeuCharAsync(...)` refreshes model metadata from NeuChar.
- `AIVectorService.AddAsync(...)` and `EditAsync(...)` manage vector configuration.
- `AITokenMonitorService.Record(...)` accumulates per-call token totals (overall / by model / by day) thread-safely; `GetLiveStats(...)` returns the aggregated `AITokenMonitorStats`.
- `AITokenMonitorService.PublishProgress(...)` and `SubscribeAsync(runId, ...)` expose asynchronous per-run token progress as an `IAsyncEnumerable<AITokenProgressEvent>` with buffered replay.
- `AITokenUsageService.RecordAsync(...)` and `GetStatsAsync(...)` persist usage records and compute aggregate statistics; `AITokenUsageSnapshot.Normalize()` fills a missing total from input + output.
- `AITokenUsageService.GetModelUsageMapAsync(...)` returns per-model usage aggregates (via `TokenUsageAggregator`) used by `AIModelAppService` to attach `AIModelDto.Usage` on the model list.
- `AIModelAppService` and `AIVectorAppService` expose NCF function/application endpoints; `AIModelStudioAppService` is the model-studio integration point.

## Testing

`Senparc.Xncf.AIKernel.Tests` (MSTest) covers token monitoring and the fine-tuning integration:

- `AITokenUsageSnapshotTests` - normalization, total fallback, and empty detection.
- `AITokenProgressEventTests` - completion-state derivation.
- `AITokenMonitorServiceRecordTests` - real-time aggregation (overall / by model / by day), clamping, and concurrency safety.
- `AITokenMonitorServiceProgressTests` and `AITokenMonitorServiceAsyncProgressTests` - buffered replay, live `IAsyncEnumerable` subscription, multi-subscriber delivery, burst/concurrent delivery, and cancellation.
- `FineTuningWorkerClientTests` - AdminOnly policy, private worker authentication, parameter/dataset bounds, cursor telemetry, cancellation, invalid responses and explicit worker failures.
- `FineTuningWorkerPersistenceTests` - actual SQLite Worker-profile create/read/update round trips without storing API keys.
- `FineTuningLiveWorkerTests` - opt-in named-profile typed-client integration against a disposable CPU worker provisioned with `tiny-gpt2`; supply `NCF_TEST_WORKER_ENDPOINT` and `NCF_TEST_WORKER_KEY` to verify actual training, explicit independent validation, events, archive download, paging and cancellation. Without these variables, this integration test is skipped; the companion native-smoke runner can supply them automatically.

```bash
dotnet test src/Extensions/Senparc.Xncf.AIKernel.Tests/Senparc.Xncf.AIKernel.Tests.csproj
```

## Local fine-tuning / 本地模型微调

Open **AIKernel → Local fine-tuning** after installing the module. Model inference configuration and Token Monitor alone do not train models; this feature runs a separate training worker and does not change existing inference behavior.

模块安装后，打开 **AIKernel → 本地模型微调**。模型推理配置及 Token 监控本身不提供训练能力；新增功能通过独立 Worker 执行训练，不改变已有推理行为。

The worker source, container images and initialization instructions are in [tools/AIKernelFineTuning](../../../tools/AIKernelFineTuning/README.md). Configure the NCF host:

Worker 源码、容器镜像及初始化说明位于 [tools/AIKernelFineTuning](../../../tools/AIKernelFineTuning/README.md)。在 NCF 宿主中配置：

```json
{
  "SenparcXncfAIKernel": {
    "FineTuning": {
      "Enabled": false,
      "AllowedHosts": [ "127.0.0.1" ],
      "WorkerApiKeys": { "cpu_lab": "" }
    }
  }
}
```

Set `SenparcXncfAIKernel__FineTuning__WorkerApiKeys__cpu_lab` through an environment variable/secret store to the same value as the worker's `NCF_WORKER_KEY` (at least 32 characters). Create a database Worker profile in the UI with alias `cpu_lab`, endpoint `http://127.0.0.1:8091`, timeout 180 seconds and enabled state. Enable the global switch only after provisioning the Worker. Legacy `WorkerEndpoint`/`WorkerApiKey` settings do not configure these profiles. Never commit real keys. Use private TLS for cross-host connections and do not expose the worker directly to browsers.

通过环境变量或密钥存储设置 `SenparcXncfAIKernel__FineTuning__WorkerApiKeys__cpu_lab`，与 Worker 的 `NCF_WORKER_KEY` 一致（至少 32 字符）。在 UI 中创建数据库 Worker 配置：别名 `cpu_lab`、端点 `http://127.0.0.1:8091`、超时 180 秒、启用状态。准备好 Worker 后再启用全局开关。旧 `WorkerEndpoint`/`WorkerApiKey` 不用于这些配置。不要提交真实密钥；跨主机使用私网 TLS，不要将 Worker 直接暴露给浏览器。

For an existing installation, upgrade the AIKernel module through module management so its database migrations add the Worker profile table. New source changes do not imply a package has already been published.

已有安装需通过模块管理升级 AIKernel，应用数据库迁移以新增 Worker 配置表。源码更新不代表对应包已发布。

The UI opens a five-stage tutorial by default, links to both language guides, offers downloadable four-row JSONL examples, validates dataset structure/limits with line numbers, and explains every advanced parameter. Worker switching clears scoped model/dataset/job selections and rejects stale responses; requests follow the selected profile's timeout. Start with the default 10-step smoke test, then evaluate separately before inference deployment.

UI 默认展开五阶段教程，提供中英文文档入口、四行 JSONL 样例下载、带行号的数据结构/边界校验和全部高级参数解释。切换 Worker 会清空其模型/数据集/任务选择并拒绝旧响应，请求遵循所选配置的超时。先运行默认 10 步冒烟测试，再独立评估后部署推理。

Module pages load `_AIKernelLocalizationScripts` rather than the Admin host's generic partial, so frontend labels resolve to real localized text. An unconfigured Worker is shown as a guided setup state; actual API failures remain errors. Rebuild/restart the host after updating the module.

模块页面使用专属 `_AIKernelLocalizationScripts`，避免与 Admin 宿主同名 partial 冲突而显示资源键。尚未配置 Worker 时展示操作引导，真实 API 故障仍显示错误。更新模块后需重新构建并重启宿主。

Functionality is the acceptance criterion, not the appearance of the UI. The backend validates offline models/data, executes real adapter updates/evaluation, persists jobs/events and exports compatible artifacts. The UI consumes Worker 1.1+ paging APIs to expose records beyond the legacy first 100 and uses persistent `storeId` to invalidate cached resources when storage changes. Legacy array endpoints remain available for existing clients. The global switch blocks NCF access; it does not terminate running Worker children.

验收以功能为中心，不以页面外观代替训练正确性。后端负责离线模型/数据校验、真实适配器更新与验证、任务/事件持久化及兼容产物导出。UI 使用 Worker 1.1+ 分页接口操作超过旧接口最近 100 条的历史，并以持久化 `storeId` 在存储变化时清空旧资源缓存。旧数组接口保留兼容。全局开关阻断 NCF 接入，并不终止已运行的 Worker 子进程。

NCF upload/create/cancel APIs take the typed JSON DTO as their first parameter and `workerAlias` from query. The frontend follows that contract and validates profile-save responses as Worker DTOs, not job DTOs. Tests generate the actual dynamic controllers to verify binding; no third-party framework source is patched.

NCF 上传/提交/取消 API 的第一个参数为 JSON DTO，`workerAlias` 来自 query。前端遵循该契约，并按 Worker DTO 而非任务 DTO 校验配置保存结果。测试实际生成动态 Controller 验证绑定，不修改第三方框架源码。

Verified on 2026-10-07: native CPU LoRA and native MLX LoRA/quantized-LoRA training, evaluation, exports and running/queued cancellation; all three adapters were reloaded for inference with nonzero parameter/output changes. Named-profile .NET live integration and SQLite persistence also passed. This verifies infrastructure, not useful model quality or CUDA hardware.

2026-10-07 已实际验证原生 CPU LoRA、原生 MLX LoRA/量化 LoRA 的训练、验证、导出及运行/排队取消；三种 adapter 均重载推理，确认参数和输出真实变化。命名配置的 .NET 实时集成及 SQLite 持久化也已通过。这证明基础设施，不代表业务模型质量或 CUDA 硬件已验收。

CPU and NVIDIA CUDA training use dedicated fixed-purpose containers, not arbitrary commands in the existing short-lived Sandbox templates. Docker Desktop cannot expose Apple Metal GPUs; MLX runs in a native macOS environment with separate OS permissions. Training outputs are adapters, not automatically published inference models. Evaluate, review and deploy through an appropriate serving runtime before adding an AIModel inference configuration.

CPU 与 NVIDIA CUDA 使用专用固定功能容器，而非在现有短时 Sandbox 模板中执行任意命令。Docker Desktop 无法提供 Apple Metal GPU，MLX 需在独立权限的原生 macOS 环境中运行。训练产物为适配器，不会自动发布为推理模型；应先评估、审核并通过适合的推理运行时部署，再添加 AIModel 配置。

Full bilingual concepts, operating procedures and production guidance are maintained in NcfDocs: [English](https://doc.ncf.pub/NcfPackageSources/xncf/aikernel-local-fine-tuning.html) / [中文](https://doc.ncf.pub/zh/NcfPackageSources/xncf/aikernel-local-fine-tuning.html).

The module stores provider settings and may process prompts or model output. Keep API keys in secure configuration, restrict model administration, and apply tenant/data-retention rules before enabling it for users.
