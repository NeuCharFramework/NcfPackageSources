# Senparc.Xncf.AgentsManager

`Senparc.Xncf.AgentsManager` is an NCF module for managing reusable AI-agent templates, chat groups, tasks, collaboration graphs, prompt optimization, and usage records.

## Features

- Stores and manages `AgentTemplate`, `ChatGroup`, `ChatGroupMember`, `ChatTask`, and chat history data.
- Supports group execution, task streaming, agent collaboration graphs, and snapshot responses.
- Provides prompt optimization and prompt-catalyzer application services.
- Lets each AgentTemplate bind to one published KnowledgeBase without a cross-module database foreign key.
- Shows per-Agent completed conversation rounds/tasks, prompt/completion/total tokens, average response time, and last activity on the management page.
- Retrieves bounded KnowledgeBase context before model execution, labels it as untrusted external data, and falls back to the model when retrieval is unavailable.
- Exposes local application-service requests and DTOs suitable for NCF function rendering.
- Includes provider-specific EF Core contexts for NCF's multi-database model.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AgentsManager" Version="0.13.0-preview8" />
```

## Key API

- `AgentTemplateService` and `AgentTemplateAppService` create, find, configure, and inspect agent templates.
- `ChatGroupService`, `ChatGroupMemberService`, and `ChatGroupHistoryService` manage group membership and conversation records.
- `ChatTaskService`, `ChatTaskAppService`, and `ChatTaskStreamController` coordinate task execution and streaming output.
- `PromptOptimizationAppService` and `PromptOptimizationAgentBridge` integrate prompt improvement workflows.
- `AgentsTemplateService` and the `AgentGraph*` DTOs support reusable collaboration graphs.

## KnowledgeBase-first RAG policy

KnowledgeBase-first is implemented as retrieve-first, conditionally augment, not as a forced answer override. A bound Agent queries its published collection with the user's request, receives at most five chunks and 6,000 characters, and is instructed to use only relevant evidence and identify gaps or conflicts. Empty or failed retrieval is fail-open so an unavailable vector service does not stop the whole multi-Agent task.

This policy is appropriate because it improves domain grounding without pretending every query belongs to the knowledge base. It still needs production evaluation for similarity thresholds/reranking, document ACLs, prompt-injection filtering, and citation rendering. Token totals are based on provider usage metadata stored with completed chat history; providers that do not return usage cannot be estimated exactly.
