---
name: maintain-xncf-module
description: Add and maintain NeuCharFramework XNCF use cases, entities, EF migrations, admin pages, Functions, WebApi, localization, and tests while keeping the module UID and database prefix stable. Use when extending or fixing an existing XNCF.
---

# Maintain an XNCF

Develop in vertical slices inside an existing module. Layer rules and cross-module contracts: [architecture.md](../develop-xncf-modules/references/architecture.md). Commands, migration folders, and localization checklists: [develop-and-maintain.md](../develop-xncf-modules/references/develop-and-maintain.md).

## Before changing code

1. Read `Register.cs`, `Register.Area.cs`, `Register.Database.cs`, and the csproj. Folder names are not a substitute for these files.
2. Confirm the aggregate you change belongs to this module. For cross-module data, add contracts or integration events; do not reference the other module's implementation.
3. Never change `Uid` or `DATABASE_PREFIX` on a published module.
4. Preserve unrelated uncommitted user changes.

## Per use case

1. Model entities, invariants, and domain services in Domain. Entities inherit `EntityBase<TKey>`; tables use `[Table(Register.DATABASE_PREFIX + nameof(Entity))]`.
2. Orchestrate in `Application/AppServices`; put inputs and outputs in `Application/DTOs`. AppService extends `AppServiceBase` and returns `GetResponseAsync` or `GetStringResponseAsync`.
3. When the database shape changes, add migrations for each `Senparc.Ncf.Database.*` provider the host references. Use the site's `Senparc.Web.csproj` as the EF startup project.
4. Pages, Functions, and dynamic WebApi are adapters only. Areas must not touch repositories or `DbContext` directly. Add controllers in `OHS/Local` or `OHS/Remote` only when HTTP shape must be hand-written.
5. Register services in `AddXncfModule`. Menus, permissions, and home pages live in `Register.Area.cs`. User-visible strings go into module resources—not C#, Razor, JavaScript, HTML attributes, or attribute parameters. When adding or removing a resource key, update all seven files (neutral, `zh-CN`, `en`, `es`, `fr`, `ja`, `ru`); Chinese is the source; preserve `{0}` placeholders and `||` parameter separators. Details: [develop-and-maintain.md](../develop-xncf-modules/references/develop-and-maintain.md#%E6%9C%AC%E5%9C%B0%E5%8C%96).
6. Add the smallest useful tests for invariants and use cases. Add host-side checks when registration, routing, migrations, authorization, or EventBus behavior changes.

## Behaviors to preserve

- Install and update migrate through `XncfDatabaseDbContext.MigrateOnInstallAsync` and stay idempotent.
- Uninstall calls the supplied delegate by default. Template "uninstall deletes all module tables" is a demo—do not keep that in product modules without an explicit decision.
- Publish integration events after local state commits. Use outbox when delivery must not be lost; do not treat in-process EventBus as a durable queue.
- Browser progress uses existing SSE or streaming adapters; do not model that as a domain event.
- Do not add file headers to generated files under `Domain/Migrations/<database>/`; regeneration overwrites them. Record functional migration changes in the module release notes.

## Validation

Build the modified module and its test project, then the host. When the schema changes, confirm the same migration exists in every provider folder. When UI changes, confirm equal key sets across seven resource files, no new hard-coded user-visible strings, and successful lookup plus parameter formatting in Chinese and at least one non-Chinese culture. A successful compile does not prove localization or language switching.

```bash
python3 skills/develop-xncf-modules/scripts/inspect_xncf.py <NCF-site-root> --module <project-name> --layers
dotnet test <test-project.csproj> --no-restore
dotnet build <host-Senparc.Web.csproj> --no-restore
```

Report static checks, compile, tests, migrations, and logged-in UI behavior separately. Do not equate compile success with migrations applied.