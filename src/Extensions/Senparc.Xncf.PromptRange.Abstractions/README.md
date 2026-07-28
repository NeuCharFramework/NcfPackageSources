# Senparc.Xncf.PromptRange.Abstractions

`Senparc.Xncf.PromptRange.Abstractions` contains the event contracts shared by PromptRange and optional prompt-optimization or evaluation modules.

## Features

- Prompt initialization request/response events.
- Prompt optimization request/response events with model-parameter context.
- Prompt-test completion events carrying output, score, and evaluation metadata.
- Immutable records that can cross NCF EventBus boundaries without a PromptRange implementation reference.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.PromptRange.Abstractions" Version="0.2.2-preview2" />
```

## Key API

- `PromptInitRequestEvent` and `PromptInitResponseEvent` correlate prompt initialization.
- `PromptOptimizationRequestEvent` and `PromptOptimizationResponseEvent` carry optimization input, output, score, and error state.
- `OptimizationContext` and `OptimizedParameters` describe model-generation parameters.
- `PromptTestFinishedEvent` records a completed prompt test and evaluation result.

Event payloads may contain prompts and model output. Apply authorization, tenant ownership, redaction, and retention controls before publishing or persisting them.
