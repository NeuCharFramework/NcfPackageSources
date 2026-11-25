<img src="https://github.com/user-attachments/assets/f4377f39-7f2d-4030-84e1-1095c07f3e84" alt="NeuCharFramework" width="300">

# NcfPackageSources

[![Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_apis/build/status/NeuCharFramework.NcfPackageSources?branchName=master)](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)

English | [Chinese](readme.cn.md)

The official source repository for the core libraries and XNCF modules used by [NeuCharFramework (NCF)](https://github.com/NeuCharFramework/NCF).

Most application developers can consume the published NuGet packages without working in this repository. Use this repository when you need to inspect, debug, extend, or contribute to the libraries behind the NCF application template.

- NCF documentation: <https://doc.ncf.pub/>
- NCF application template: <https://github.com/NeuCharFramework/NCF>
- NuGet packages: <https://www.nuget.org/profiles/Senparc>

## Table of Contents

- [What Is in This Repository](#what-is-in-this-repository)
- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Repository Structure](#repository-structure)
- [XNCF Module Loading Order](#xncf-module-loading-order)
- [Building and Testing](#building-and-testing)
- [Running the Integration Baseline](#running-the-integration-baseline)
- [Workspace Runtime](#workspace-runtime)
- [NuGet Packages](#nuget-packages)
- [Packaging and Publishing](#packaging-and-publishing)
- [Contributing](#contributing)
- [License](#license)

## What Is in This Repository

NCF separates reusable infrastructure from modular business capabilities:

- **Basic libraries (`Senparc.Ncf.*`)** provide the framework foundation, including XNCF registration, data access, services, repositories, logging, UI support, database providers, and shared contracts.
- **Extension modules (`Senparc.Xncf.*`)** provide optional capabilities such as AI, agents, knowledge bases, workflows, MCP, files, database tooling, multi-tenancy, and administration.
- **System modules** are XNCF modules required by an NCF application, including installation, menus, permissions, system management, and tenant support.
- **Abstraction packages** expose contracts that other modules can reference without depending on the corresponding implementation assembly.

## Requirements

- [.NET SDK 10.x](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 with current .NET support, or the latest VS Code version
- Git
- Network access to NuGet.org for the first restore

The primary solution targets .NET 10. A small number of legacy, template, or tooling projects use other targets for their specific purpose; their project files remain the source of truth.

Supported database packages include:

- SQLite
- MySQL
- SQL Server 2012+
- PostgreSQL
- Oracle
- DM (Dameng)
- In-memory provider for tests and development

`NuGet.Config` uses NuGet.org and the repository-local `local-nuget` directory. Local packages are optional but may be needed by some development workflows.

## Quick Start

```bash
git clone https://github.com/NeuCharFramework/NcfPackageSources.git
cd NcfPackageSources
dotnet restore src/NcfPackageSources.sln
dotnet build src/NcfPackageSources.sln --no-restore
```

To open the package-source solution in Visual Studio:

```text
src/NcfPackageSources.sln
```

The larger integration solution is:

```text
src/NcfPackageSources_Include_NcfWebAdminBaseline.sln
```

Use the first solution for package development and the second when a change must be validated against the Web Admin baseline.

## Repository Structure

| Path | Purpose |
| --- | --- |
| `src/Basic/` | Required NCF libraries prefixed with `Senparc.Ncf.` |
| `src/Extensions/` | Optional and domain-focused XNCF modules prefixed with `Senparc.Xncf.` |
| `src/Extensions/System/` | XNCF modules that provide core system capabilities |
| `src/DevelopmentTools/` | Integration samples and development helpers |
| `tools/NcfWebAdminBaseline/` | Runnable NCF Web/Admin baseline used for integration and source-of-truth development |
| `tools/NcfWebAdminSyncTool/` | Synchronizes baseline content into template/package projects |
| `tools/BuildXncfBuilderTemplate/` | Prepares `Senparc.Xncf.XncfBuilder.Template` placeholders |
| `docs/` | Architecture notes, implementation guides, migration notes, and troubleshooting documents |
| `skills/` | Repository-maintained workflows for creating, maintaining, packaging, and upgrading XNCF modules |
| `azure-pipelines.yml` | Release build, package validation, and NuGet publishing pipeline |

See [the extensions overview](src/Extensions/readme.md) and [the tools overview](tools/readme.md) for additional details.

## XNCF Module Loading Order

Use `[XncfOrder(x)]` to control module registration priority. Modules are loaded in descending order: a larger number is loaded first.

| Value | Meaning |
| --- | --- |
| `0` | Default; the attribute can be omitted |
| `1`-`5000` | Important modules that need early loading |
| `5000+` | System and foundation modules |
| `58xx` | AI-related foundation modules |
| `59xx` | Lower-level system modules |

When assigning an order, consider dependencies as well as UI visibility. A module must not depend on another module that is registered after it.

## Building and Testing

Build the normal package-source solution:

```bash
dotnet restore src/NcfPackageSources.sln
dotnet build src/NcfPackageSources.sln --no-restore
```

Build only a changed module when developing iteratively:

```bash
dotnet build src/Extensions/Senparc.Xncf.KnowledgeBase/Senparc.Xncf.KnowledgeBase.csproj --no-restore
```

Run all test projects included in the solution:

```bash
dotnet test src/NcfPackageSources.sln --no-restore
```

Or run a focused test project:

```bash
dotnet test src/Extensions/Senparc.Xncf.AIKernel.Tests/Senparc.Xncf.AIKernel.Tests.csproj --no-restore
```

For test projects not included in the main solution, invoke the specific `.csproj` directly. Add new tests close to the feature they cover and keep package metadata projects non-packable where appropriate.

## Running the Integration Baseline

Use the Aspire AppHost for the integrated services, dashboard, health checks, traces, metrics, and logs:

```bash
dotnet restore tools/NcfWebAdminBaseline/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
dotnet run --project tools/NcfWebAdminBaseline/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
```

To run the Web application directly:

```bash
dotnet run --project tools/NcfWebAdminBaseline/Senparc.Web/Senparc.Web.csproj
```

The baseline directly references the XNCF module sources, making it suitable for end-to-end debugging. See [its detailed guide](tools/NcfWebAdminBaseline/readme.md) before changing baseline or template synchronization behavior.

If XNCF Builder generated files conflict after template changes, remove only this generated directory and regenerate it through the normal Builder workflow:

```bash
rm -rf src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder/Generated
```

## Workspace Runtime

The Workspace runtime project is named `Senparc.Xncf.WorkspaceAgent`. The directory, assembly, namespace, and Docker entry point use the same name; keep the XNCF naming convention with exactly two dots around `Xncf`.

`Workspace` resolves the Agent's actual output through its project reference and propagates the `.dll`, `.deps.json`, and `.runtimeconfig.json` files into Admin and host build/publish outputs. Do not construct Agent paths from the host `$(Configuration)`, because solution dependencies can build with different configurations. A custom `SenparcXncfWorkspace:AgentPath` must include the matching filename.

Workspace native-agent execution is disabled by default. Docker is the supported isolated execution alternative. Admin Chat Python execution also requires the related Workspace CLI option and a bound Workspace.

## NuGet Packages

The package name follows the project namespace. Basic libraries start with `Senparc.Ncf.` and XNCF modules start with `Senparc.Xncf.`.

### Core Libraries

| Package | Purpose |
| --- | --- |
| `Senparc.Ncf.XncfBase` | XNCF module registration and base abstractions |
| `Senparc.Ncf.Core` | Core framework infrastructure |
| `Senparc.Ncf.AreaBase` | ASP.NET Core Area support for XNCF modules |
| `Senparc.Ncf.Database` | Shared database infrastructure |
| `Senparc.Ncf.DatabasePlant` | Database provider assembly and migration support |
| `Senparc.Ncf.Repository` | Repository-layer support |
| `Senparc.Ncf.Service` | Service-layer support |
| `Senparc.Ncf.Log` | Logging integration |
| `Senparc.Ncf.Mvc.UI` | MVC UI components |
| `Senparc.Ncf.Utility` | Shared utilities |
| `Senparc.Ncf.SMS` | SMS support |
| `Senparc.Ncf.UnitTestExtension` | Unit-test helpers |
| `Senparc.Ncf.IsolatedRuntime` | Shared isolated-runtime infrastructure |

Database providers are published separately as `Senparc.Ncf.Database.MySql`, `Senparc.Ncf.Database.Sqlite`, `Senparc.Ncf.Database.SqlServer`, `Senparc.Ncf.Database.PostgreSQL`, `Senparc.Ncf.Database.Oracle`, `Senparc.Ncf.Database.Dm`, `Senparc.Ncf.Database.InMemory`, and `Senparc.Ncf.Database.MySql.Backup`.

### Extension Modules

Major extension packages include:

- AI and agents: `Senparc.Xncf.AIKernel`, `Senparc.Xncf.AIAgentsHub`, `Senparc.Xncf.AgentsManager`, `Senparc.Xncf.PromptRange`, and `Senparc.Xncf.KnowledgeBase`
- Integration: `Senparc.Xncf.Connectors`, `Senparc.Xncf.MCP`, `Senparc.Xncf.Dapr`, `Senparc.Xncf.FileManager`, and `Senparc.Xncf.DesktopBridge`
- Orchestration and runtime: `Senparc.Xncf.NeuCharWorkflow`, `Senparc.Xncf.Sandbox`, `Senparc.Xncf.Workspace`, and `Senparc.Xncf.WorkspaceAgent`
- Data and administration: `Senparc.Xncf.DynamicData`, `Senparc.Xncf.DatabaseToolkit`, `Senparc.Xncf.FirmwareUpdate`, `Senparc.Xncf.WeixinManager`, and `Senparc.Xncf.SenMapic`
- Developer tooling: `Senparc.Xncf.XncfBuilder`, `Senparc.Xncf.XncfBuilder.Template`, `Senparc.Xncf.ChangeNamespace`, `Senparc.Xncf.Terminal`, and `Senparc.Xncf.Swagger`

System packages include `Senparc.Xncf.Installer`, `Senparc.Xncf.Installer.Interface`, `Senparc.Xncf.AreasBase`, `Senparc.Xncf.Menu`, `Senparc.Xncf.SystemCore`, `Senparc.Xncf.SystemManager`, `Senparc.Xncf.SystemPermission`, `Senparc.Xncf.Tenant`, `Senparc.Xncf.Tenant.Interface`, and `Senparc.Xncf.XncfModuleManager`.

### Abstraction Packages

Abstraction packages can be referenced without depending on a module implementation:

- `Senparc.Ncf.Shared.Abstractions`
- `Senparc.Xncf.AgentsManager.Abstractions`
- `Senparc.Xncf.AIKernel.Abstractions`
- `Senparc.Xncf.Connectors.Abstractions`
- `Senparc.Xncf.FileManager.Abstractions`
- `Senparc.Xncf.MCP.Abstractions`
- `Senparc.Xncf.NeuCharWorkflow.Abstractions`
- `Senparc.Xncf.PromptRange.Abstractions`
- `Senparc.Xncf.Sandbox.Abstractions`
- `Senparc.Xncf.Workspace.Abstractions`
- `Senparc.Xncf.XncfBuilder.Abstractions`

Browse package versions on the [Senparc NuGet profile](https://www.nuget.org/profiles/Senparc) or search by package ID on [NuGet.org](https://www.nuget.org/packages).

## Packaging and Publishing

The Azure DevOps pipeline builds `src/NcfPackageSources.sln` in Release mode. Projects with `GeneratePackageOnBuild` enabled produce `.nupkg` files, which the pipeline discovers and pushes to NuGet.org.

When adding a package:

1. Enable Release package generation in its project.
2. Add every published project and required abstraction project to `src/NcfPackageSources.sln`.
3. Ensure the solution has a Release build mapping for the project.
4. Build in Release mode and inspect the generated `.nupkg` before publishing.
5. Verify that assemblies, static or embedded assets, the icon, package metadata, dependencies, and README content are present.

A project reference alone does not create a NuGet package for the referenced project. If a dependency is missing from the solution, MSBuild may fall back to another configuration and skip Release-only packaging.

The pipeline validates the evaluated package ID, version, and output path for required abstraction packages before it pushes anything. Validation or build failures stop publishing. Existing versions are skipped with `--skip-duplicate`, so the pipeline can be rerun after a missing package has been fixed.

## Contributing

Contributions are welcome.

1. Fork this repository and create a focused feature or fix branch.
2. Keep changes within the relevant `Senparc.Ncf.*` or `Senparc.Xncf.*` module.
3. Follow the patterns already used by nearby services, repositories, entities, DTOs, and registrations.
4. Add or update tests for behavior you introduce or fix.
5. Build the smallest affected project first, then validate the solution or integration baseline when dependencies require it.
6. Update package release notes and version metadata only when preparing the package release.
7. Open a Pull Request describing the change, validation performed, and any migration or compatibility impact.

Repository-specific automation and maintenance instructions are documented in [skills/README.md](skills/README.md). Please do not commit generated output, local databases, secrets, API keys, or machine-specific paths.

## License

Licensed under the Apache License 2.0. See [LICENSE](LICENSE).
