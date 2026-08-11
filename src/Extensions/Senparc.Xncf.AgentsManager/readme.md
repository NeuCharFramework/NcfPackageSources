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
- Supports A2A remote agents as first-class ChatGroup members while preserving the existing local `AgentTemplate` and `ChatGroupMember` data path.
- Supports per-group context sharing: legacy full history (local-only), instruction plus bounded text conclusion (the secure mixed-group default), or instruction only.

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

## A2A remote agent configuration

Use the **远程 A2A 智能体** entry on the Agent management page to register an Agent Card root URL or a `/.well-known/agent-card.json` URL, test card discovery, and add it to a ChatGroup alongside local agents. The current phase keeps the group owner and entry agent local; the remote agent participates in normal collaboration rounds.

Remote authentication is deliberately reference-only. The database stores `AuthSecretKey`, never a token value. Configure the real secret in the deployment configuration, for example:

```json
{
  "A2A": {
    "Secrets": {
      "research-agent-token": "replace-with-deployment-secret"
    }
  }
}
```

Choose `research-agent-token` as the remote agent's deployment key name. The default mixed-group policy forwards only the initial instruction and the current round's bounded text conclusion. It removes tool calls, raw provider representations, usage data, and earlier history from the participant broadcast. This reduces exposure but does not transform an agent's voluntarily emitted text into a guaranteed semantic summary; prompts therefore require a concise shared conclusion and prohibit chain-of-thought/private content.
