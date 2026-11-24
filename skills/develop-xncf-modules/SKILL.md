---
name: develop-xncf-modules
description: Route NeuCharFramework XNCF work across creation, integration, feature development, maintenance, bounded-context splits, template upkeep, and release. Use when the user mentions XNCF, NCF module, XncfBuilder, dotnet new XNCF, module install, or publishing an XNCF. Hand the concrete procedure to create-xncf-module, maintain-xncf-module, or publish-xncf-module.
---

# Develop XNCF modules

Identify the workflow first, then open the matching skill. Do not copy an old module instead of using the template.

| Task | Skill |
| --- | --- |
| New module, template generation, solution or host integration | [create-xncf-module](../create-xncf-module/SKILL.md) |
| Use cases, entities, pages, Functions, WebApi, migrations, tests on an existing module | [maintain-xncf-module](../maintain-xncf-module/SKILL.md) |
| Version alignment, changelog, pack, publish | [publish-xncf-module](../publish-xncf-module/SKILL.md) |
| Edit the XNCF template package itself | [references/template-maintenance.md](references/template-maintenance.md) |
| Boundary review, dependencies, split plan without code changes | Rules below plus the reference docs |

Read [references/architecture.md](references/architecture.md) for design. Read [references/bounded-context-splitting.md](references/bounded-context-splitting.md) for splits and context maps. Read [references/creation-and-validation.md](references/creation-and-validation.md) for validation boundaries.

For create and maintain work, user-visible text follows [references/develop-and-maintain.md](references/develop-and-maintain.md#%E6%9C%AC%E5%9C%B0%E5%8C%96): module resources only—no hard-coded strings in C#, Razor, JavaScript, or attributes; keep seven resource files in sync and verify equal key sets.

## Boundaries before projects

One XNCF owns one shared language, invariants, data ownership, and release cadence. Page count, table count, controllers, or folder size are not split criteria. A single aggregate write transaction belongs to one XNCF.

Default to one module. Split only when language diverges, data owners differ, install or release independence is required, security or availability boundaries differ, or dependencies can be frozen behind a stable contract. Draw a context map and get user confirmation before splitting.

Internal dependency direction:

```text
Areas / OHS / adapters -> Application -> Domain
Register composes only; no use cases inside Register
```

Cross-module sharing is limited to IDs, snapshots, command results, and integration events in `.Abstractions`. Do not reference another module's entities, repositories, mappings, or `DbContext`. EventBus is in-process, not a security boundary or durable delivery.

## Identity is fixed after release

- `Register.Uid` is an uppercase GUID. On UID collision with a published module, change the new module, never the old one.
- `DatabaseUniquePrefix` does not change after release. Table names use `Register.DATABASE_PREFIX + nameof(Entity)`.
- `Register.Version` must match csproj `<Version>` (same semantic version).
- Do not keep the template's `1.1.x` history on a new module.

## Where skills run

Skills run against the NCF site the user is developing—often only `Senparc.Web` plus NuGet assemblies, without base-library source in the tree.

- Install the template only from NuGet `Senparc.Xncf.XncfBuilder.Template`.
- Base-library versions come from the host's references; query nuget.org only for packages the host lacks.
- Place new modules beside `Senparc.Web` and reference packages via `PackageReference`.
- Before generation, run `skills/develop-xncf-modules/scripts/prepare_xncf.py` with `--root` set to the site root. Use script output for commands and versions; do not hard-code source-repo paths.

Read [references/template-maintenance.md](references/template-maintenance.md) only when the user explicitly asks to change the template package source. Those directories are not part of ordinary creation.

If the user only wants a plan, stop after the plan—no template install, no project generation, no solution edits.