# XNCF architecture reference

## Contents

- Canonical project roles
- Dependency rules
- Registration and identity
- Data ownership
- UI, API, Function, and localization
- Cross-XNCF communication
- Architecture smells

## Canonical project roles

Follow the current repository/template before introducing new folders. The standard XNCF shape is:

| Location | Responsibility | Must not own |
| --- | --- | --- |
| `Domain/Models` | Aggregates, entities, value objects, domain rules | Razor, HTTP, transport DTOs |
| `Domain/Services` | Domain behavior spanning entities in the same context | UI orchestration, another context's repositories |
| `Application/AppServices` | Use-case orchestration, transaction coordination, response construction | Core invariants that belong in Domain |
| `Application/DTOs/Request`, `Response` | Application boundary models | EF entities or database mappings |
| `Application/Events`, `EventHandlers` | Integration/application events and handlers | Browser streaming protocol |
| `OHS/Local`, `OHS/Remote` | NCF/HTTP/MCP/controller adapters and public host boundary | Business invariants |
| `Areas/Admin/Pages` | Razor/Vue administrative UI | Direct persistence and cross-module orchestration |
| `Resources` | Localized user-visible strings | Business identifiers or protocol values |
| `Register*.cs` | Module identity, DI, Areas, database registration, install/update composition | Use-case implementation |

Use the repository's established migration and mapping folders. Do not force a textbook `Infrastructure` project when the active XNCF template keeps EF mappings and migrations under the module's domain/database tree; preserve the dependency intent even when physical naming differs.

## Dependency rules

Allowed direction:

```text
Areas / OHS / external adapters
              |
              v
         Application
              |
              v
            Domain

Register composes implementations without moving behavior into registration code.
```

Enforce these rules:

- Domain must not import `.Application`, `.OHS`, `.Areas`, Razor, controllers, or another XNCF implementation.
- Application must not import `.Areas` or `.OHS`.
- Transport models must not become domain entities.
- UI must call application/OHS boundaries instead of repositories or EF contexts.
- Avoid static service locators. Register scoped/singleton lifetimes deliberately in `AddXncfModule`.
- Keep mapping declarations in the composition/application boundary; do not hide business transformations in AutoMapper.

## Registration and identity

An XNCF normally has a partial `Register` with these roles:

- `Register.cs`: `[XncfRegister]`, `Name`, stable `Uid`, `Version`, localized `MenuName`/`Description`, install/update/uninstall, DI, static assets.
- `Register.Area.cs`: `IAreaRegister`, home URL, localized menus, Razor authorization configuration, optional runtime compilation.
- `Register.Database.cs`: `IXncfDatabase`, globally unique `DatabaseUniquePrefix`, dynamic context type, model registration.

Rules:

- Never change a released UID to solve a collision; resolve the new/unreleased module instead.
- Version identity and package version must follow repository release conventions.
- Keep install/update idempotent. Do not silently drop data during uninstall; destructive cleanup requires an explicit product decision.
- Apply migrations through the supported XNCF database path and verify every supported provider affected by the change.
- Embed/serve `wwwroot` through the module's established static-file registration when the XNCF ships assets.

## Data ownership

- Assign every mutable table and aggregate one owning XNCF.
- Keep one aggregate transaction within its owner.
- Do not inject another XNCF's repository, service implementation, EF entity, mapping, or context for convenience.
- Store foreign context identifiers as IDs/value snapshots. Obtain current information through a contract or maintain a local read model.
- Avoid cross-XNCF database foreign keys that make installation, versioning, or uninstall order implicit.
- For cross-context workflows, use events and a process manager/saga-like application service. Add an outbox/inbox and idempotency when delivery guarantees require them; do not assume the in-process bus is durable.

## UI, API, Function, and localization

- Treat Areas/Razor/Vue, API controllers, MCP endpoints, and `[FunctionRender]` methods as separate adapters over the same application use cases.
- Authorize on the server at every entry point; button visibility is not authorization.
- Keep request validation at the boundary and invariant validation in Domain.
- Put every user-visible label, validation message, notification, backend display message, title, placeholder, and accessibility text in module resources.
- Preserve protocol values, enum codes, model/provider names, and user data even when translating their display labels.
- Keep localized display names out of business routing, persistence keys, and integration contracts.

## Cross-XNCF communication

Use a dedicated `<Module>.Abstractions` package/project when multiple XNCFs need compile-time contracts. Keep it small and dependency-light.

Good contract contents:

- integration event records;
- command/query request and result contracts;
- stable IDs and value snapshots;
- capability/version declarations where compatibility requires them.

Bad contract contents:

- EF entities, repositories, DbContexts, migrations;
- service implementations;
- UI models and Razor types;
- broad `Common` utilities unrelated to the contract.

Use `IntegrationEvent` and `IIntegrationEventHandler<T>` for asynchronous collaboration when appropriate. Preserve event correlation/parent chain when publishing derived events. Use bounded timeouts for request/response wrappers. Keep SSE/EventSource/stream hubs for browser progress and streaming; they are not domain-event contracts.

## Architecture smells

Review these before accepting a design:

- XNCF A references XNCF B and B references A.
- A module reads or writes another module's tables directly.
- One `Common` XNCF owns unrelated helpers and data.
- Every entity or page is a separate XNCF.
- A single XNCF contains unrelated languages, permissions, data owners, and release cadences.
- Domain imports web/OHS/application namespaces.
- The `Register` class performs business workflows.
- Cross-module messages expose internal entities.
- EventBus is treated as durable delivery, authorization, or process isolation.
- A generated template target is edited while its authoritative source remains unchanged.

