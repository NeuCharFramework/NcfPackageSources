# Senparc.Xncf.PromptRange

`Senparc.Xncf.PromptRange` is an NCF module for organizing prompts, model configurations, prompt results, chat history, scoring, and prompt usage insights.

## Features

- Manages prompt ranges, prompt items, model records, and generated results.
- Supports prompt tree/list operations, import/export, score feedback, chat continuation, and history views.
- Provides text embedding search request/response models and multimodal execution options for text, images, audio, speech-to-text, and text-to-speech.
- Emits prompt initialization and optimization events through the companion abstractions package.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.PromptRange" Version="0.26.0-preview3" />
```

## Key API

- `PromptRangeService`/`PromptRangeAppService` manage prompt ranges and dashboard data.
- `PromptItemService`/`PromptItemAppService` manage prompt trees, versions, import/export, and range-name responses.
- `LlmModelService`/`LlmModelAppService` manage available language-model metadata.
- `PromptResultService`/`PromptResultAppService` manage generated results, robot/human scores, chat continuation, and feedback.
- `PromptTextEmbeddingExecutionOptions`, `PromptImageController`, `PromptAudioController`, and `PromptStreamController` expose execution integration points.
- `PromptResult_TextEmbeddingSearchRequest` and `PromptResult_TextEmbeddingSearchResponse` model vector-search calls.

The module records prompts, model usage, scores, and generated output. Apply tenant isolation, data retention, PII redaction, provider quotas, and authorization at the host boundary.
