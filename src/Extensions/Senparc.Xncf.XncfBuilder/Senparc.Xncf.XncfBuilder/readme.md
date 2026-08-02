# Senparc.Xncf.XncfBuilder

`Senparc.Xncf.XncfBuilder` is an NCF module and service layer for generating the starter code and metadata of an XNCF module.

## Features

- Generates module files, application-service interfaces, database entities, and migration scaffolding.
- Reads the XNCF template package and writes multiple generated files with placeholder replacement.
- Provides database migration and template inspection workflows through NCF function requests.
- Includes module-inventory request/response events for cross-module discovery.
- Builds and runs generated modules in an isolated `Senparc.Web` preview process without restarting the current site.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.XncfBuilder" Version="0.26.0-preview3" />
```

For the `dotnet new` workflow, install the companion template package:

```bash
dotnet new install Senparc.Xncf.XncfBuilder.Template
dotnet new xncf -n MyProject --force --UseDatabase true
```

## Key API

- `BuildXncfAppService` handles module-generation operations.
- `BuildXncf_BuildRequest`, `BuildXncf_CreateAppServiceRequest`, and `BuildXncf_CreateDatabaseEntityRequest` describe generation tasks.
- `GenerateAppServiceInterface` and `DatabaseMigrationsAppService` expose focused code-generation workflows.
- `MultiFileCodeGenerator` writes template-driven file sets; `BuildXncfRequestHelper` normalizes generation requests.
- `XncfModulesInventoryRequestHandler` and `XncfModulesInventoryResponseHandler` support module inventory exchange.

## Isolated preview

The Builder function UI provides these operations:

- **Start or update an isolated XNCF preview** publishes `Senparc.Web` with the selected generated module and starts it on a loopback port.
- **View XNCF preview status** reports the URL, process ID, source fingerprint, module DLL hash, and optional recent output.
- **Stop an isolated XNCF preview** terminates the child process and removes its temporary publish directory.

The XncfBuilder module menu also exposes **XNCF Preview Monitor**. This AdminOnly page polls lightweight session snapshots every two seconds, displays queued and active modules together, and loads the latest output only when a session row is expanded. Task history is stored in `XncfBuilderXncfPreviewTask`, while child-host lifecycle data is stored in `XncfBuilderXncfPreviewHost`; only the latest 50 terminal sessions are hydrated into memory. A parent-site restart marks unfinished persisted tasks and hosts as interrupted because an old PID cannot be safely reattached. The two tables require an EF migration, which is intentionally not included in this source change.

Generation also has an opt-in **Start isolated preview after generation** option. Publishing normally uses `--no-restore`, a versioned directory under the operating-system temporary folder, and serial MSBuild execution. The preview service performs one required restore when the new project or a package reference is newer than its assets file. A newly healthy preview replaces the previous preview for the same module; the current `Senparc.Web` process is not restarted.

## AI-assisted closed loop

The persistent XNCF source workspace is the source of truth. AI tools must not edit the temporary preview publish directory. The intended workflow is:

1. Create a module with the NuGet template, or select an existing source module.
2. Read and update files inside that module workspace. A read returns a SHA-256 value; pass it back as `expectedSha256` when updating an existing file to prevent overwriting a concurrent change.
3. Call **Start or update an isolated XNCF preview** only after the requested writes are complete. The preview service fingerprints the module source, verifies that `Senparc.Web.csproj` directly references that exact source project, publishes `Senparc.Web`, verifies the expected module DLL exists, and rejects the result if the source changes during build or startup. A package reference or an old copied DLL is intentionally not accepted as proof that the edited source was built.
4. Start the new process, pass the HTTP health check, and only then stop and delete the previous healthy preview for that module.
5. Inspect or test the preview, then repeat from step 2 for the next change.

The generation-time **Start isolated preview after generation** option is suitable for a template-only smoke test. If AI will continue editing the generated module, leave that option off and start the preview after the AI write phase.

Workspace file access is confined to a uniquely resolved XNCF project under the selected solution. Absolute paths, directory traversal, symbolic-link escapes, directories, NUL-containing content, and text files larger than 4 MB are rejected. Writes use a same-directory temporary file followed by an atomic replacement.

The default `XncfPreview` environment binds only to `127.0.0.1` and overrides the database/cache selection to local SQLite and local cache. Its database is created inside that preview's publish directory. Set a different environment only when intentionally testing against that environment's configuration.

This first preview mode requires a source workspace with the conventional `Senparc.Web/Senparc.Web.csproj` and generated `<Organization>.Xncf.<Module>/<Organization>.Xncf.<Module>.csproj` layout. A runtime-only distribution needs a separately shipped preview host before it can accept generated source modules.

Review generated files before committing them. Generated source can include database and authorization decisions that must be adapted to the host application's domain and security model.
The child process is an isolation boundary for lifecycle, assembly loading, database, and cache—not an operating-system security sandbox. Preview startup removes inherited application environment variables except for a small runtime allowlist, but generated code still runs with the account and network permissions of the parent site and may read files available to that account. Untrusted model output therefore requires review or an additional OS/container sandbox before execution.
