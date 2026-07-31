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
- **View XNCF preview status** reports the URL, process ID, and optional recent output.
- **Stop an isolated XNCF preview** terminates the child process and removes its temporary publish directory.

Generation also has an opt-in **Start isolated preview after generation** option. Publishing normally uses `--no-restore`, a versioned directory under the operating-system temporary folder, and serial MSBuild execution. The preview service performs one required restore when the new project or a package reference is newer than its assets file. A newly healthy preview replaces the previous preview for the same module; the current `Senparc.Web` process is not restarted.

The default `XncfPreview` environment binds only to `127.0.0.1` and overrides the database/cache selection to local SQLite and local cache. Its database is created inside that preview's publish directory. Set a different environment only when intentionally testing against that environment's configuration.

This first preview mode requires a source workspace with the conventional `Senparc.Web/Senparc.Web.csproj` and generated `<Organization>.Xncf.<Module>/<Organization>.Xncf.<Module>.csproj` layout. A runtime-only distribution needs a separately shipped preview host before it can accept generated source modules.

Review generated files before committing them. Generated source can include database and authorization decisions that must be adapted to the host application's domain and security model.
The child process is an isolation boundary for lifecycle and assembly loading, not an operating-system security sandbox. Review untrusted generated code before running it.
