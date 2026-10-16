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
- `Skills/SkillContracts.cs` defines the portable Skill catalog, execution, and package contracts.
- `Skills/SkillExceptions.cs` defines explicit not-found and access-denied failures.

## Skill portability

PromptRange exposes its versioned prompt tree as Skills through `ISkillCatalog` and
`ISkillExecutor`. A Skill uses a stable SkillCode and a concrete PromptCode version
(`RangeName-Tactic-Aiming`). Consumers such as AdminChat and AgentsManager should
reference this abstractions package rather than PromptRange database entities.

Portable Skill packages are ZIP streams with a `skill.json` manifest and the
Semantic Kernel-compatible `config.json`/`skprompt.txt` files. Secrets, provider
credentials, and executable binaries must not be included in a package.

The PromptRange module registers the `ISkillCatalog`, `ISkillExecutor`, and
`ISkillPackageService` implementations. Consumers may resolve these interfaces
from dependency injection without referencing PromptRange entities.

External installation accepts a local ZIP upload or an HTTPS package URL. The
package is validated for its manifest, size, archive paths, and executable
content, then installed as `Draft`. An administrator must explicitly publish
the imported PromptCode through the Skill management API before AdminChat or
AgentsManager can execute it.

AdminChat exposes three Skill operations: list available Skills, load a selected
Skill for the current turn, and execute it. After an administrator explicitly
requests installation, AdminChat may call the HTTPS installation function; it
only creates a Draft and never publishes or executes the newly installed Skill
automatically.

Event payloads may contain prompts and model output. Apply authorization, tenant ownership, redaction, and retention controls before publishing or persisting them.
