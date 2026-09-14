# Admin Chat Native Harness and Trajectory

## What Changed

Admin Chat now uses the native Microsoft Agent Framework Harness through `Senparc.AI.AgentKernel`.

The previous `[[CONTINUE]]` / `[[DONE]]` loop remains only as compatibility code and is no longer the Admin Chat Harness execution path.

Native Harness provides:

- Function invocation loop
- Per-service-call chat history persistence
- Context-window compaction
- Todo tracking
- Plan/execute mode
- Tool approval middleware
- MAF session serialization

Admin Chat keeps file access, file memory, Skills scanning, and Hosted Web Search disabled by default.

## Trajectory

Each Harness request creates one `ADMIN_AdminChatTrajectory` row and append-only `ADMIN_AdminChatTrajectoryEvent` rows.

Event types include:

- `request`
- `resume`
- `fork`
- `assistant.text`
- `tool.call`
- `tool.result`
- `approval.request`
- `approval.response`
- `error`
- `cancelled`

Trajectory payloads are returned only through explicit Trajectory APIs. They are not mixed into normal chat message content.

## User Operations

The Admin Chat page exposes one task-trace entry point:

- **Replay**: opens the stored event stream. Replay is observational and never invokes a tool.
- **Resume**: restores the serialized MAF session and continues from the latest checkpoint.
- **Fork**: creates a new Trajectory with parent lineage and runs the new instruction in a fresh Harness session using the selected event history as context.
- **Search**: searches Trajectory titles, event types, tool names, text, and serialized event payloads.
- **Approve / Reject**: resolves native MAF tool approval requests and continues the same persisted Harness session.

The UI defaults to a simple task flow. Users do not need to configure iteration counts, context windows, or tool orchestration.

## Database Migration

The Admin module includes migrations for:

- SQLite
- SQL Server
- MySQL
- Oracle
- PostgreSQL
- DM

The migration name is `Add_AdminChatHarnessTrajectory`. Apply it through the normal NCF migration mechanism for the selected provider.

The migration creates:

- `ADMIN_AdminChatTrajectory`
- `ADMIN_AdminChatTrajectoryEvent`

It also creates the session/user and trajectory/sequence indexes.

## API Surface

All endpoints use the existing Admin Chat authorization boundary:

```text
GetSessionTrajectoriesAsync
GetTrajectoryAsync
SearchTrajectoriesAsync
ResumeTrajectoryAsync
ForkTrajectoryAsync
RespondTrajectoryApprovalAsync
```

Every read and mutation checks the current administrator's ownership of the Trajectory.

## Resume and Fork Semantics

Resume uses the serialized MAF session state saved after the previous run. This preserves MAF-managed context providers, mode state, todo state, and approval state when the Harness version supports their serialization.

Fork always creates a new Trajectory. Forking from the latest point can use the persisted session state. Forking from an earlier event reconstructs a bounded event summary and starts a new session. External side effects are never undone by replay or fork; a fork is a new execution branch.

## Operational Security

Trajectory content can contain tool arguments and tool results. Only expose it through the authenticated Admin Chat Trajectory endpoints. Do not write full payloads to ordinary frontend errors, browser console logs, or unauthenticated APIs.

Review tool exposure separately from the Harness defaults. A module being attached to a session must not automatically grant a host-mutating FunctionRender unless its `AllowAiInvocation` boundary permits it.

## Verification

Source/build verification does not prove a live authenticated model/tool run. Before release, verify:

1. A real model can create a Harness session.
2. A FunctionRender tool appears and executes through MAF function invocation.
3. A tool approval pauses the run and the Admin UI can approve or reject it.
4. Restarting the Web process allows Resume from stored session state.
5. Replay never executes a tool.
6. Fork creates a new parent-linked Trajectory.
7. Search returns tool names and event text without leaking data to ordinary chat responses.
