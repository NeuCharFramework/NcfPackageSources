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

## Reusable prompt picker

Admin's shared `v-prompt-range` directive provides an on-demand picker for text inputs,
including both AdminChat composers and the Footer's embedded chat. See the
[integration guide](../../../tools/NcfSimulatedSite/Senparc.Areas.Admin/readme.md#promptrange-可复用提示词选择器).

The following GET methods on `PromptItemAppService` use the existing AdminOnly policy
and preserve tenant/soft-delete filters on both ranges and items:

- `GetSelectorOptionsAsync`: distinct range/lane metadata for Prompt-kind ranges.
- `SearchSelectorAsync`: `rangeId` (0 = all), exact `tactic`, `keyword` (up to 200 characters),
  `pageIndex` (1-based), `pageSize` (1–50, default 10). Returns `items` and `totalCount`;
  each item's `content` is a preview of at most 240 characters. Searches name, content,
  range alias and full version. `sortBy=score` (default) orders by current score descending,
  with unrated items last and newer IDs breaking ties; `sortBy=promptCode` orders by the full
  PromptCode string ascending, with newer IDs breaking ties. Sorting runs before pagination.
  Items and full-content responses include `currentScore` (`-1` means unrated);
  `evalAvgScore` and `evalMaxScore` remain available for older clients as result statistics.
  The picker displays the current score to one decimal place and resets to page 1 when sorting changes.
- `GetSelectorPromptAsync(id)`: fetches full content only for the selected prompt.

Drafts are labeled and selectable. Selection inserts literal content only; it does not
execute prompts/skills, expand variables, copy model settings or send a chat message.

## Explicit PromptCode references and shared scoring

`IPromptReferenceService` (in the abstractions package) validates exact text references
and reads/scores a concrete full PromptCode. It does not rate a range or create usage-rating
records. Each version keeps its AI and manual source scores; **the last successfully saved
score wins, regardless of source**. `FinalScore` is only the cached current value, not a
third scoring dimension. Existing PromptResult manual/AI scoring also updates that version.
New scores must be between 0 and 10 with at most one decimal place.

Before a version receives its first shared score, its old average result score is used
for compatibility; historical result scores have no reliable cross-result rating timestamp.
Once rated, that average never overrides the shared current value.

- `GetReferenceInfoAsync(id)` returns the full code, source/current scores and benchmark availability.
- POST `ScoreReferenceAsync({ id, score })` saves the shared manual score under the AdminOnly policy.
- `ScoreWithAIAsync(id, context, result)` is a server-side provider operation for authorized hosts.
  It reuses PromptRange's existing benchmark evaluator, requires a nonempty benchmark/result,
  and limits both context and result to 64,000 characters. Failed evaluation leaves scores unchanged.
  Hosts must verify ownership and bind context/result to a stored explicit reference before calling.
- Reference validation requires exact content and version, ordered nonoverlapping UTF-16 spans,
  and at most 32 references. Tenant/soft-delete filters apply to both the item and its range.

Upgrade the PromptRange and Admin databases before using this integration.
`Add_PromptCode_Scores` and `Add_AdminChat_PromptReferences` migrations cover SQLite,
SQL Server, MySQL, PostgreSQL, Oracle and Dm. They add nullable columns only; old messages
remain plain text. Generating these migration files does not apply them to a running database.
