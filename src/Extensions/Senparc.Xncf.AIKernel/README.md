# Senparc.Xncf.AIKernel

`Senparc.Xncf.AIKernel` is the NCF AI model and vector configuration module. It provides persistence, administration services, and integration helpers for Senparc AI model settings.

## Features

- Manages `AIModel` and `AIVector` records and their DTO/request models.
- Provides model and vector application services with paging, create, edit, and delete operations.
- Builds `SenparcAiSetting` values and runs model requests through the configured Senparc AI runtime.
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
- `AIModelAppService` and `AIVectorAppService` expose NCF function/application endpoints; `AIModelStudioAppService` is the model-studio integration point.

The module stores provider settings and may process prompts or model output. Keep API keys in secure configuration, restrict model administration, and apply tenant/data-retention rules before enabling it for users.
