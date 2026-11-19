# Step S3: Docker / Wasm Provider + Orchestrator

## Task Overview
Implement the orchestrator service, TTL cleanup, the Docker CLI provider, and the Wasm stub.

## Goals
- [ ] `SandboxOrchestrator`: Create/List/Destroy/Exec/Extend + quota validation
- [ ] `DockerSandboxRuntime`: label `ncf.sandbox=1`, optional `--rm`, resource limits
- [ ] `WasmSandboxRuntime`: Stub (clear error when unavailable), reserved for phase two
- [ ] `IHostedService`: startup orphan scan and periodic TTL cleanup

## Acceptance Criteria
- [ ] Create returns a readable error without Docker and never falls back to a raw process.
- [ ] The destroy path is idempotent.
