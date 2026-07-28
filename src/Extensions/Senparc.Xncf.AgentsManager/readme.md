# Senparc.Xncf.AgentsManager

`Senparc.Xncf.AgentsManager` is an NCF module for managing reusable AI-agent templates, chat groups, tasks, collaboration graphs, prompt optimization, and usage records.

## Features

- Stores and manages `AgentTemplate`, `ChatGroup`, `ChatGroupMember`, `ChatTask`, and chat history data.
- Supports group execution, task streaming, agent collaboration graphs, and snapshot responses.
- Provides prompt optimization and prompt-catalyzer application services.
- Exposes local application-service requests and DTOs suitable for NCF function rendering.
- Includes provider-specific EF Core contexts for NCF's multi-database model.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AgentsManager" Version="0.26.0-preview3" />
```

## Key API

- `AgentTemplateService` and `AgentTemplateAppService` create, find, configure, and inspect agent templates.
- `ChatGroupService`, `ChatGroupMemberService`, and `ChatGroupHistoryService` manage group membership and conversation records.
- `ChatTaskService`, `ChatTaskAppService`, and `ChatTaskStreamController` coordinate task execution and streaming output.
- `PromptOptimizationAppService` and `PromptOptimizationAgentBridge` integrate prompt improvement workflows.
- `AgentsTemplateService` and the `AgentGraph*` DTOs support reusable collaboration graphs.

The module depends on the host's AI/agent runtime and authorization configuration. Protect chat history and model usage data, and treat tool/function execution as an explicit permission boundary.
