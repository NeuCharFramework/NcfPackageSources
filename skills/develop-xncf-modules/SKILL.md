---
name: develop-xncf-modules
description: Create, extend, refactor, and validate NeuCharFramework XNCF modules. Use when Codex needs to scaffold an XNCF, add Domain/Application/OHS/Areas/database/function/EventBus capabilities, plan modular development inside one XNCF, split a business system into multiple XNCFs using DDD bounded contexts, define cross-XNCF contracts and data ownership, or review XNCF architecture and dependency boundaries.
---

# Develop XNCF Modules

Build XNCF modules around business boundaries, not pages or tables. Preserve the repository's current NCF conventions while keeping domain ownership, dependencies, and validation evidence explicit.

## Route the task

Classify the request before editing:

1. **Create**: scaffold one or more new XNCFs.
2. **Extend**: add a use case, aggregate, page, API, Function, database capability, or integration to an existing XNCF.
3. **Split**: decompose a system or oversized XNCF into bounded contexts and multiple XNCFs.
4. **Review**: report architecture, coupling, migration, or validation findings without changing code.
5. **Maintain the template**: change the XNCF template or generator itself.

Read [references/architecture.md](references/architecture.md) for every implementation or review. Read [references/bounded-context-splitting.md](references/bounded-context-splitting.md) for split, merge, dependency, or context-map decisions. Read [references/creation-and-validation.md](references/creation-and-validation.md) before scaffolding, changing the template, packaging, or claiming completion.

## Establish the baseline

1. Read all applicable `AGENTS.md` files.
2. Run `git status --short --branch`; preserve unrelated and user-owned changes.
3. Locate the solution, host `Senparc.Web.csproj`, candidate XNCFs, template version, target framework, and package/project reference convention.
4. Run the bundled inspector from the skill directory:

   ```bash
   python3 scripts/inspect_xncf.py <repository-root>
   ```

   Add `--module <name>` for focused output and `--layers` for optional Domain/Application namespace checks. Treat direct XNCF references as review signals, not automatic defects. Confirm each finding in source before changing code.
5. Inspect current `Register.cs`, `Register.Area.cs`, `Register.Database.cs`, project files, resources, tests, and analogous modules. Do not infer architecture from directory names alone.

If the user requested an audit or proposal first, deliver the complete proposal and wait for confirmation before editing.

## Model the business before choosing projects

Capture these facts for each capability:

- business purpose and ubiquitous language;
- commands, invariants, aggregates, and transaction boundary;
- authoritative data owner and read-model consumers;
- actors, authorization boundary, and tenant behavior;
- external systems and anti-corruption needs;
- lifecycle, release cadence, optional installation, scale, and failure isolation;
- synchronous and asynchronous dependencies.

Produce a context map before creating multiple XNCFs. Include proposed XNCF, owned aggregates/tables, exposed contracts, dependencies, consistency model, UI/API entry points, and migration sequence.

## Choose one XNCF or several

Default to one XNCF while capabilities share the same language, invariants, data owner, permissions, and lifecycle. Split only when there is positive boundary evidence.

Strong split signals include:

- different bounded contexts or conflicting meanings for the same terms;
- independent aggregate and transaction ownership;
- optional installation or independent release/versioning;
- distinct security, tenancy, availability, or scaling requirements;
- external integration that deserves an anti-corruption boundary;
- a dependency that can be expressed as a stable contract rather than shared internals.

Do not split merely by entity, table, CRUD page, controller, team preference, or folder size. Keep one aggregate's write transaction inside one XNCF. See [references/bounded-context-splitting.md](references/bounded-context-splitting.md) for the scoring and migration rules.

## Design dependencies before code

Use this dependency direction inside an XNCF:

```text
Areas / OHS / adapters -> Application -> Domain
Register / composition root -> all required implementation layers
```

Keep Domain independent of Areas, OHS, Razor, transport DTOs, and another XNCF's implementation. Let Application orchestrate use cases; keep business invariants in aggregates/domain services. Use OHS and Areas as adapters, not as the business layer.

Across XNCFs:

- expose minimal contracts in a small `.Abstractions` project when compile-time sharing is necessary;
- exchange IDs, value snapshots, commands/results, and integration events—not entities, repositories, EF configurations, or `DbContext`;
- prefer EventBus for in-process asynchronous module collaboration and explicit APIs/application contracts for queries;
- use SSE/stream hubs for browser streaming, not as a replacement for domain integration events;
- design idempotency, retries, timeouts, and failure ownership explicitly;
- avoid dependency cycles; introduce a contract, orchestrator/process manager, or boundary correction instead.

Treat EventBus as a communication mechanism, not a security or process-isolation boundary.

## Scaffold safely

1. Prefer the installed XNCF template or XncfBuilder over hand-copying an old module.
2. Run `dotnet new XNCF --help` and inspect the installed template before choosing switches; do not guess stale package versions or options.
3. Use explicit organization, module name, stable uppercase GUID/UID, semantic version, menu name, icon, description, target framework, and required feature switches.
4. Generate into a new, explicit directory. Review generated files before adding them to a solution or host.
5. Keep each XNCF UID and `DatabaseUniquePrefix` globally unique and stable after release.
6. Add only the capabilities needed now: Web/Areas, Database, Function, or Web API. Remove sample code that is not part of the product.

Never install or update templates/packages, modify a solution, or add host references when the user requested only a plan or audit.

## Implement vertical slices

For each use case:

1. Define domain behavior and invariants.
2. Add application request/response DTOs and orchestration.
3. Add persistence mappings/migrations owned by this XNCF.
4. Add OHS/API/Function and Areas/UI adapters.
5. Register services and mappings in the partial `Register` composition root.
6. Add localization for all user-visible text and authorization for all entry points.
7. Add unit/contract tests at the lowest meaningful layer; add host integration tests when registration, routing, database, authorization, EventBus, or static assets are involved.

Avoid generic `Common`, `Manager`, or shared-database buckets. Name code after the bounded context and use case.

## Validate proportionately

Run narrow checks first, then the host. Follow repository instructions; do not restore again unless package references changed or a no-restore build demonstrates that restore is required.

At minimum verify:

- the inspector has no unexplained duplicate identity, database-prefix, layer, or cycle findings;
- changed XNCF projects build with `--no-restore` when allowed;
- tests covering modified domain/application/contracts pass;
- the host builds and discovers the module;
- install/update and migrations are checked when database shape changed;
- resource files, JavaScript, routes, authorization, and static assets are checked when UI changed;
- generated package contents and an independently generated consumer are checked before claiming template/package readiness.

Report proof boundaries separately: static inspection, build, tests, package generation, runtime registration, authenticated behavior, and E2E are not interchangeable.

## Deliver the result

Summarize:

- chosen bounded contexts and why they are one or multiple XNCFs;
- dependency direction and cross-XNCF contracts;
- data/transaction ownership and consistency model;
- files/modules changed;
- validations passed and warnings;
- runtime, authenticated, package, migration, or E2E evidence still missing.
