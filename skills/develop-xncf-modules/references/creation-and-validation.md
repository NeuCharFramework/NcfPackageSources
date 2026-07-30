# XNCF creation, template, and validation workflow

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

Read applicable `AGENTS.md`. Inspect the host target framework and existing package/project references. Do not install or update a template from the network without authorization.

In NcfPackageSources, inspect these current anchors when relevant:

- authoritative template source: `tools/NcfSimulatedSite/Template_OrgName.Xncf.Template_XncfName`;
- generated template target: `src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.Template/templates/template1`;
- generator: `src/Extensions/Senparc.Xncf.XncfBuilder`;
- sync implementation: `tools/NcfSimulatedSiteSyncTool`.

The simulated-site template source is authoritative for synchronized content. Do not edit only `templates/template1`; a later one-way sync can overwrite it. Account for placeholder transformations such as UID/version before comparing source and target.

## Scaffold a module

Use the current help output to supply all required symbols. A representative invocation is:

```bash
dotnet new XNCF \
  -n Contoso.Xncf.Ordering \
  -o src/Extensions/Contoso.Xncf.Ordering \
  --OrgName Contoso \
  --XncfName Ordering \
  --Guid 00000000-0000-0000-0000-000000000000 \
  --Version 0.1.0 \
  --MenuName Ordering \
  --Icon "fa fa-cubes" \
  --Description "Order lifecycle management" \
  --Web true \
  --Database true
```

Replace the example GUID with a newly generated explicit GUID. Add `--Function true`, `--UseWebApi true`, `--Sample true`, target framework, and NCF package versions only when supported/required by the active template. Never copy the zero GUID.

After generation:

1. Review every generated file and remove irrelevant samples.
2. Confirm assembly/root namespace, UID, version, menu/description resources, database prefix, and static assets.
3. Add the project to the intended solution and host only after checking paths and dependency direction.
4. Prefer package references for independently distributed XNCFs. Use repository project references only when the checkout's conventions require source integration.

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

