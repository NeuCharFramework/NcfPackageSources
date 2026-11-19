# Step S2: Domain Session / Quota / Runtime Contracts

## Task Overview
Establish the sandbox domain model and runtime abstractions without coupling them to Docker.

## Goals
- [ ] `SandboxSession` aggregate/entity + status enum
- [ ] `SandboxQuotaPolicy` (per-user/global concurrency, TTL, CPU/memory)
- [ ] `ISandboxRuntime` / `SandboxTemplate` / request-response value objects
- [ ] DbContext registration and multi-database migration placeholders (SQLite first)

## Files Involved
- `Domain/Models/DatabaseModel/SandboxSession.cs`
- `Domain/Models/SandboxRuntime/*.cs`
- `Domain/Services/*.cs`
- `Register.Database.cs`

## Acceptance Criteria
- [ ] Domain does not depend on Areas/OHS.
- [ ] The Runtime interface supports both Docker and Wasm.
