# XNCF creation, template, and validation workflow

操作步骤以中文技能为准，不要从本文复制命令：

- 创建和接入：`skills/create-xncf-module/SKILL.md` 与 `references/create-and-integrate.md`
- 功能维护：`skills/maintain-xncf-module/SKILL.md` 与 `references/develop-and-maintain.md`
- 打包发布：`skills/publish-xncf-module/SKILL.md` 与 `references/publish.md`
- 模板本身：`references/template-maintenance.md`

下面保留验证证据的边界。文中若出现全 0 GUID 或手写的旧参数，不能拿来执行。

## Contents

- Discover the active toolchain
- Scaffold a module
- Maintain the repository template
- Implement and register capabilities
- Validation ladder
- Evidence boundaries

## Discover the active toolchain

Prefer current repository evidence over remembered commands:

```bash
git status --short --branch
dotnet new list XNCF
dotnet new XNCF --help
rg --files -g 'Register*.cs' -g '*.csproj' -g 'template.json'
```

Read applicable `AGENTS.md`. Inspect the host `Senparc.Web.csproj` target framework and its NuGet package references. Install `Senparc.Xncf.XncfBuilder.Template` from NuGet when creating a module. Do not look for a template in a source tree.

The directories below exist only in the NCF package source repository. Open them solely when the user asked to change the template package itself:

- template authoring source and the sync tool live in that repository;
- the published template id is `Senparc.Xncf.XncfBuilder.Template`.

The simulated-site template source is authoritative for synchronized content. Do not edit only `templates/template1`; a later one-way sync can overwrite it. Account for placeholder transformations such as UID/version before comparing source and target.

## Scaffold a module

Run `skills/develop-xncf-modules/scripts/prepare_xncf.py` and follow `skills/create-xncf-module/SKILL.md`. Do not invent the `dotnet new` arguments or copy a GUID from documentation. The script refuses the zero GUID, `WrongGuid`, duplicate UIDs, and duplicate database prefixes.

After generation, normalize the package version, remove the Color sample unless it is the requested feature, and integrate the project using the profile printed by the script.

## Maintain the repository template

When changing the template:

1. Edit the authoritative source first.
2. Update namespaces, physical paths, `Register` references, localization resources, and template include/exclude conditions together.
3. Compare source and target under the sync tool's transformation/exclusion rules before running broad synchronization.
4. Build the authoritative source.
5. Synchronize the template target deliberately.
6. Pack the template package and enumerate package contents.
7. Install the produced package into an isolated template cache or controlled environment.
8. Generate representative combinations: minimal, Web, Database, Function, Web API, and Sample where supported.
9. Build generated consumers without repository-only references.

A source build proves only source compilation. A successful pack does not prove package contents, installation, generation, consumer build, publication, or runtime behavior.

## Implement and register capabilities

For a database-backed Web XNCF, verify:

- `Register.cs` has stable module metadata, DI, install/update logic, and static assets;
- `Register.Area.cs` has localized menus/routes and server-side authorization;
- `Register.Database.cs` has a unique prefix and model registration;
- Domain entities/mappings/migrations remain owned by this XNCF;
- Application AppServices and DTOs implement use cases;
- OHS/Areas delegate to Application;
- all display text uses resources;
- tests cover invariants, use cases, registration, and contracts.

For cross-XNCF EventBus behavior, verify host registration and assembly scanning. Use request event -> handler -> derived response/correlation waiter only when request/response semantics are actually needed. Bound waits with cancellation/timeouts. Use a separate SSE/streaming adapter for browser progress.

## Validation ladder

Run the narrowest relevant layers and report them separately:

1. **Static**: inspector, UID/prefix uniqueness, dependency direction, resource/route review, `git diff --check`.
2. **Compile**: changed abstractions first, then changed XNCFs, then host. Use `--no-restore` unless dependencies changed or the build proves restore is needed.
3. **Unit/contract tests**: Domain invariants, Application use cases, event/API compatibility.
4. **Database**: migration generation/application for every supported provider in scope; install/update idempotency and data preservation.
5. **Package/template**: pack, inspect archive, isolated install, generate variants, build generated projects.
6. **Runtime**: host discovers module, DI resolves services, routes/static assets/functions work.
7. **Security**: anonymous/unauthorized/authenticated behavior and tenant boundaries.
8. **E2E**: browser/API workflows, cross-module event flow, retries/failures, real persistence.

Useful commands must be adapted to the actual paths:

```bash
python3 <skill-dir>/scripts/inspect_xncf.py <repository-root>
dotnet build <changed-xncf.csproj> --no-restore
dotnet test <test-project.csproj> --no-restore
dotnet build <host-Senparc.Web.csproj> --no-restore
git diff --check
```

Avoid parallel builds when projects share output files and have previously produced file-lock failures. Add `--disable-build-servers -m:1` when repository evidence requires serialized builds.

## Evidence boundaries

State exactly what was proven:

- static structure is not build proof;
- build is not test proof;
- test is not migration proof;
- source/template build is not generated-package proof;
- route existence is not authorization proof;
- unauthenticated rejection is not authenticated success;
- runtime smoke is not full E2E.

