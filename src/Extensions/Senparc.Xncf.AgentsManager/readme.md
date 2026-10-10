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
- Includes an interactive 3D Agent Studio with team desks, an agent lounge, a task board, and a same-screen inspector.

## Agent Studio

Open **Agents → 3D view** (`#tab=first&view=three`). The existing panel, statistics and full detail pages remain available.

- Drag an enabled local or remote A2A agent from the roster or directly in the 3D scene to a team desk / group drop target. Membership is appended idempotently; other members, roles, enable state and context-sharing settings are preserved. This adds membership rather than moving the agent out of its other teams.
- Use **+** or drop agents into the **Team tray**, then **Form team & run**. Choose a local, non-human owner and entry agent, a task title, instructions and an optional chat model. You can create just the team or submit a task immediately. A saved team is retained when task submission fails, so retry does not create another team.
- Select agents, desks or task cards to inspect and manage them without leaving the studio. The inspector supports editing/enabling agents and groups, removing ordinary members, starting group tasks, reading bounded conversation pages, stopping active tasks and archiving completed tasks. Owner/entry agents must be replaced through the group editor before removal.
- New agents, groups (including empty groups), membership changes, task statuses and names refresh every three seconds while the studio is visible. Waiting, running, paused, finished, cancelled and failed tasks are filterable; pending human requests link to the existing approval/input detail page.
- Task submission waits for a persisted task ID, but model execution remains asynchronous. Initialization failures before task creation are returned to the composer; watch task status/output for the actual execution result. Membership edits affect future executions; already running tasks retain their captured participants.
- Orbit empty space, scroll to zoom, or use **Overview** to fit the room. Reduced-motion preferences are respected. If WebGL is unavailable, the roster, inspector and team/task operations remain usable.

Studio APIs (authenticated application-service routes):

- `CreateStudioTeam(StudioTeamRequest)`: validates all participants/roles before transactionally saving the group and members.
- `SetStudioParticipant(groupId, participantKey, remove)`: incrementally adds/removes a `local:{id}` or `remote:{id}` participant under a per-group cache lock.
- `StartStudioTask(ChatGroup_RunGroupRequest, chatGroupId)`: returns the persisted task reference without waiting for model completion, and propagates initialization failures.
- `GetAgentGraphSnapshot`: includes lightweight non-archived `Tasks` alongside agents, groups, links and active collaborations.

### Studio regression checks

Run frontend state tests with `node --test src/Extensions/Senparc.Xncf.AgentsManagerTests/Frontend/studio.test.cjs`.
For browser checks, start `node src/Extensions/Senparc.Xncf.AgentsManagerTests/Frontend/studio.fixture.cjs`, then run
`playwright-cli -s=ncf-studio open http://127.0.0.1:51961/` and
`playwright-cli -s=ncf-studio run-code --filename=src/Extensions/Senparc.Xncf.AgentsManagerTests/Frontend/studio.browser.spec.js`.
The fixture uses real studio views/scripts and mock APIs; it never writes to the host database or calls an LLM.
Run `AgentStudioTests.Studio_*` through VSTest with `EnableMSTestRunner=false`, `TestingPlatformDotnetTestSupport=false`,
`OutputType=Library`, `CopyLocalLockFileAssemblies=true` and `GenerateRuntimeConfigurationFiles=true`; the existing executable entry point otherwise runs only its manually listed smoke checks.

The studio reuses the existing Vue, Element UI, Three.js and OrbitControls assets. The containing Razor page
must register the MVC Tag Helpers so `_Studio`, localization scripts and versioned asset URLs are rendered,
rather than leaving a literal `<partial>` tag in the otherwise empty 3D branch.
Switching back to the panel or statistics view disposes the renderer; returning to 3D recreates it.
Run `AgentStudioRenderingTests` with the same VSTest properties to verify the compiled page and script/style sections.
For a browser regression with the original Vue lifecycle and hash navigation, set `NCF_STUDIO_RENDERED_PAGE`
to the same absolute temporary JSON path when running this test and the fixture, then execute
`playwright-cli -s=ncf-studio run-code --filename=src/Extensions/Senparc.Xncf.AgentsManagerTests/Frontend/studio.rendering.browser.spec.js`.
This additional check uses the actual compiled page output, not a manually inserted partial.

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

## Publish a local Agent as an A2A service

An existing local `AgentTemplate` can also be exposed to another NCF installation, or to any A2A-compatible client, without coupling the two systems. Open the local Agent editor and select **配置 / 发布为 A2A Agent**. The publishing configuration is stored in the additive `PublishedA2AAgent` table; it does not change existing AgentTemplate, ChatGroup, or ChatGroupMember records.

When enabled, the standard discovery URL is:

```text
https://your-public-host/a2a/{public-agent-key}/.well-known/agent-card.json
```

The Agent Card advertises the standard A2A JSON-RPC interface at `https://your-public-host/a2a/{public-agent-key}`. A second NCF system can paste the discovery URL into its **远程 A2A 智能体** configuration, test the card, then add that remote Agent to a ChatGroup.

For reverse-proxy deployments, configure the externally visible base address so the Agent Card never advertises an internal host name:

```json
{
  "A2A": {
    "PublicBaseUrl": "https://your-public-host",
    "InboundSecrets": {
      "partner-ncf": "replace-with-deployment-secret"
    }
  }
}
```

`Bearer Token` and custom-header incoming authentication read only from `A2A:InboundSecrets:{AuthSecretKey}`. The secret body is not stored in the database, returned by the admin API, included in logs, or put into the Agent Card. Publishing is disabled by default. Function Calling and MCP tools are also disabled by default for A2A requests; enabling them is an explicit per-published-Agent decision for trusted callers only.

The A2A boundary instructs the local Agent to return shareable conclusions rather than hidden reasoning, system prompt text, secrets, or tool traces. It cannot prevent a model from intentionally placing sensitive content in a normal response, so public Agent descriptions, prompts, connected knowledge bases, and enabled tools still require a deliberate data-access review.

Choose `research-agent-token` as the remote agent's deployment key name. The default mixed-group policy forwards only the initial instruction and the current round's bounded text conclusion. It removes tool calls, raw provider representations, usage data, and earlier history from the participant broadcast. This reduces exposure but does not transform an agent's voluntarily emitted text into a guaranteed semantic summary; prompts therefore require a concise shared conclusion and prohibit chain-of-thought/private content.
