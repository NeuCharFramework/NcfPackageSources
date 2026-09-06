# Senparc.Xncf.AIKernel

`Senparc.Xncf.AIKernel` is the NCF AI model and vector configuration module. It provides persistence, administration services, and integration helpers for Senparc AI model settings.

## Features

- Manages `AIModel` and `AIVector` records and their DTO/request models.
- Provides model and vector application services with paging, create, edit, and delete operations.
- Builds `SenparcAiSetting` values and runs model requests through the configured Senparc AI runtime.
- Monitors AI token usage with real-time in-process aggregation (`AITokenMonitorService`) and persistent, queryable usage records (`AITokenUsageService`).
- Publishes per-run token progress as asynchronous events consumable via `IAsyncEnumerable` (`SubscribeAsync`), with buffered replay for late subscribers.
- Surfaces token usage on the AI model list page: overview cards (total tokens / calls / success-error / average duration), per-model usage columns (calls, total tokens with relative bar, last used), alias search, and a shortcut to the Token Monitor page.
- Synchronizes model metadata from NeuChar services when explicitly requested.
- Supports NCF's multi-database context variants and localized module metadata.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AIKernel" Version="0.26.0-preview3" />
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

`Senparc.Xncf.AIKernel.Tests` (MSTest) covers the token-monitoring surface:

- `AITokenUsageSnapshotTests` - normalization, total fallback, and empty detection.
- `AITokenProgressEventTests` - completion-state derivation.
- `AITokenMonitorServiceRecordTests` - real-time aggregation (overall / by model / by day), clamping, and concurrency safety.
- `AITokenMonitorServiceProgressTests` and `AITokenMonitorServiceAsyncProgressTests` - buffered replay, live `IAsyncEnumerable` subscription, multi-subscriber delivery, burst/concurrent delivery, and cancellation.

```bash
dotnet test src/Extensions/Senparc.Xncf.AIKernel.Tests/Senparc.Xncf.AIKernel.Tests.csproj
```

The module stores provider settings and may process prompts or model output. Keep API keys in secure configuration, restrict model administration, and apply tenant/data-retention rules before enabling it for users.
