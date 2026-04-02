<img src="https://github.com/user-attachments/assets/f4377f39-7f2d-4030-84e1-1095c07f3e84" alt="NeuCharFramework" width="300" >

<!--<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />-->

# NcfPackageSources

[![Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_apis/build/status/NeuCharFramework.NcfPackageSources?branchName=master)](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)

## Table of Contents
- [Introduction](#introduction)
- [Development Environment](#development-environment)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Module Loading Order](#module-loading-order)
- [Available NuGet Packages](#available-nuget-packages)
- [Contributing](#contributing)
- [License](#license)

## Introduction

This project contains the core library source code for the official packages of the [NCF (NeuCharFramework)](https://github.com/NeuCharFramework/NCF) template.

When you develop a project using the [NCF](https://github.com/NeuCharFramework/NCF) template, the core libraries provide a set of foundational capabilities. In most cases, you do not need to care about their internal implementation — just add a reference.

When you need to understand, modify, or debug these core libraries, you can obtain the source code from this project.

[NCF](https://github.com/NeuCharFramework/NCF) documentation: [https://doc.ncf.pub/](https://doc.ncf.pub/).

## Development Environment

- Visual Studio 2019+ or the latest version of VS Code
- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- Supported databases:
  - SQLite
  - MySQL
  - SQL Server (2012+)
  - PostgreSQL
  - Oracle
  - DM (Dameng)

## Quick Start

1. Clone the repository

```bash
git clone https://github.com/NeuCharFramework/NcfPackageSources.git
```

2. Open the solution

```bash
cd NcfPackageSources
start NcfPackageSources.sln  # Windows
open NcfPackageSources.sln   # macOS
```

3. Restore packages

```bash
dotnet restore
```

4. Restore workloads (optional)

```bash
dotnet workload restore
```

5. Build and run

```bash
dotnet build
dotnet run
```

## Project Structure

| Folder | Description |
|--------|-------------|
| src/Basic | Required basic official libraries, prefixed with `Senparc.Ncf.` |
| src/Extensions | Optional extension packages, prefixed with `Senparc.Xncf.` |
| src/Extensions/System | System modules |

## Module Loading Order

Use the `[XncfOrder(x)]` attribute to specify the loading order of modules. The order is descending — higher numbers load first:

- `0`: Default value, no need to set
- `1` ~ `5000`: Important modules that need preloading
- `5000+`: System and basic modules
- `58xx`: AI-related basic modules
- `59xx`: System underlying basic modules

## 可用的 Nuget 包 / Available NuGet Packages

### Basic Libraries

| Package Name | Description | Version |
|---|---|---|
| Senparc.Ncf.XncfBase | XNCF Module Base Library | [![Senparc.Ncf.XncfBase][badge-Senparc.Ncf.XncfBase]][nuget-Senparc.Ncf.XncfBase] |
| Senparc.Ncf.Core | Core Base Library | [![Senparc.Ncf.Core][badge-Senparc.Ncf.Core]][nuget-Senparc.Ncf.Core] |
| Senparc.Ncf.DatabasePlant | Database Assembly Plant | [![Senparc.Ncf.DatabasePlant][badge-Senparc.Ncf.DatabasePlant]][nuget-Senparc.Ncf.DatabasePlant] |
| Senparc.Ncf.Log | Logging Module | [![Senparc.Ncf.Log][badge-Senparc.Ncf.Log]][nuget-Senparc.Ncf.Log] |
| Senparc.Ncf.AreaBase | Area Base Module | [![Senparc.Ncf.AreaBase][badge-Senparc.Ncf.AreaBase]][nuget-Senparc.Ncf.AreaBase] |
| Senparc.Ncf.UnitTestExtension | Unit Test Extensions | [![Senparc.Ncf.UnitTestExtension][badge-Senparc.Ncf.UnitTestExtension]][nuget-Senparc.Ncf.UnitTestExtension] |
| Senparc.Ncf.Mvc.UI | MVC UI Components | [![Senparc.Ncf.Mvc.UI][badge-Senparc.Ncf.Mvc.UI]][nuget-Senparc.Ncf.Mvc.UI] |
| Senparc.Ncf.Database | Database Base Library | [![Senparc.Ncf.Database][badge-Senparc.Ncf.Database]][nuget-Senparc.Ncf.Database] |
| Senparc.Ncf.Database.MySql.Backup | MySQL Database Backup | [![Senparc.Ncf.Database.MySql.Backup][badge-Senparc.Ncf.Database.MySql.Backup]][nuget-Senparc.Ncf.Database.MySql.Backup] |
| Senparc.Ncf.Database.MySql | MySQL Database Support | [![Senparc.Ncf.Database.MySql][badge-Senparc.Ncf.Database.MySql]][nuget-Senparc.Ncf.Database.MySql] |
| Senparc.Ncf.Database.Oracle | Oracle Database Support | [![Senparc.Ncf.Database.Oracle][badge-Senparc.Ncf.Database.Oracle]][nuget-Senparc.Ncf.Database.Oracle] |
| Senparc.Ncf.Database.PostgreSQL | PostgreSQL Database Support | [![Senparc.Ncf.Database.PostgreSQL][badge-Senparc.Ncf.Database.PostgreSQL]][nuget-Senparc.Ncf.Database.PostgreSQL] |
| Senparc.Ncf.Database.Sqlite | SQLite Database Support | [![Senparc.Ncf.Database.Sqlite][badge-Senparc.Ncf.Database.Sqlite]][nuget-Senparc.Ncf.Database.Sqlite] |
| Senparc.Ncf.Database.SqlServer | SQL Server Database Support | [![Senparc.Ncf.Database.SqlServer][badge-Senparc.Ncf.Database.SqlServer]][nuget-Senparc.Ncf.Database.SqlServer] |
| Senparc.Ncf.Database.Dm | DM (Dameng) Database Support | [![Senparc.Ncf.Database.Dm][badge-Senparc.Ncf.Database.Dm]][nuget-Senparc.Ncf.Database.Dm] |
| Senparc.Ncf.Database.InMemory | In-Memory Database Support | [![Senparc.Ncf.Database.InMemory][badge-Senparc.Ncf.Database.InMemory]][nuget-Senparc.Ncf.Database.InMemory] |
| Senparc.Ncf.Repository | Repository Layer | [![Senparc.Ncf.Repository][badge-Senparc.Ncf.Repository]][nuget-Senparc.Ncf.Repository] |
| Senparc.Ncf.Service | Service Layer | [![Senparc.Ncf.Service][badge-Senparc.Ncf.Service]][nuget-Senparc.Ncf.Service] |
| Senparc.Ncf.SMS | SMS Service | [![Senparc.Ncf.SMS][badge-Senparc.Ncf.SMS]][nuget-Senparc.Ncf.SMS] |
| Senparc.Ncf.Utility | Utility Library | [![Senparc.Ncf.Utility][badge-Senparc.Ncf.Utility]][nuget-Senparc.Ncf.Utility] |

### Extension XNCF Modules

| Package Name | Description | Version |
|---|---|---|
| Senparc.Xncf.AgentsManager | AI Agents Manager | [![Senparc.Xncf.AgentsManager][badge-Senparc.Xncf.AgentsManager]][nuget-Senparc.Xncf.AgentsManager] |
| Senparc.Xncf.AIAgentsHub | AI Agents Hub | [![Senparc.Xncf.AIAgentsHub][badge-Senparc.Xncf.AIAgentsHub]][nuget-Senparc.Xncf.AIAgentsHub] |
| Senparc.Xncf.AIKernel | AI Kernel | [![Senparc.Xncf.AIKernel][badge-Senparc.Xncf.AIKernel]][nuget-Senparc.Xncf.AIKernel] |
| Senparc.Xncf.ChangeNamespace | Namespace Rename Tool | [![Senparc.Xncf.ChangeNamespace][badge-Senparc.Xncf.ChangeNamespace]][nuget-Senparc.Xncf.ChangeNamespace] |
| Senparc.Xncf.Dapr | Dapr Distributed Runtime Support | [![Senparc.Xncf.Dapr][badge-Senparc.Xncf.Dapr]][nuget-Senparc.Xncf.Dapr] |
| Senparc.Xncf.DatabaseToolkit | Database Toolkit Module | [![Senparc.Xncf.DatabaseToolkit][badge-Senparc.Xncf.DatabaseToolkit]][nuget-Senparc.Xncf.DatabaseToolkit] |
| Senparc.Xncf.DynamicData | Dynamic Data Base Module | [![Senparc.Xncf.DynamicData][badge-Senparc.Xncf.DynamicData]][nuget-Senparc.Xncf.DynamicData] |
| Senparc.Xncf.FileManager | File Management Module | [![Senparc.Xncf.FileManager][badge-Senparc.Xncf.FileManager]][nuget-Senparc.Xncf.FileManager] |
| Senparc.Xncf.KnowledgeBase | AI Knowledge Base | [![Senparc.Xncf.KnowledgeBase][badge-Senparc.Xncf.KnowledgeBase]][nuget-Senparc.Xncf.KnowledgeBase] |
| Senparc.Xncf.MCP | MCP Multi-tenant Microservice Platform | [![Senparc.Xncf.MCP][badge-Senparc.Xncf.MCP]][nuget-Senparc.Xncf.MCP] |
| Senparc.Xncf.PromptRange | AI Prompt Testing Range | [![Senparc.Xncf.PromptRange][badge-Senparc.Xncf.PromptRange]][nuget-Senparc.Xncf.PromptRange] |
| Senparc.Xncf.SenMapic | SenMapic Web Crawler Module | [![Senparc.Xncf.SenMapic][badge-Senparc.Xncf.SenMapic]][nuget-Senparc.Xncf.SenMapic] |
| Senparc.Xncf.Swagger | Swagger API Documentation | [![Senparc.Xncf.Swagger][badge-Senparc.Xncf.Swagger]][nuget-Senparc.Xncf.Swagger] |
| Senparc.Xncf.Terminal | Terminal Command Module | [![Senparc.Xncf.Terminal][badge-Senparc.Xncf.Terminal]][nuget-Senparc.Xncf.Terminal] |
| Senparc.Xncf.XncfBuilder | XNCF Module Generator | [![Senparc.Xncf.XncfBuilder][badge-Senparc.Xncf.XncfBuilder]][nuget-Senparc.Xncf.XncfBuilder] |
| Senparc.Xncf.AreasBase | System Areas Base Module | [![Senparc.Xncf.AreasBase][badge-Senparc.Xncf.AreasBase]][nuget-Senparc.Xncf.AreasBase] |
| Senparc.Xncf.Menu | Menu Management Module | [![Senparc.Xncf.Menu][badge-Senparc.Xncf.Menu]][nuget-Senparc.Xncf.Menu] |
| Senparc.Xncf.SystemCore | System Core Module | [![Senparc.Xncf.SystemCore][badge-Senparc.Xncf.SystemCore]][nuget-Senparc.Xncf.SystemCore] |
| Senparc.Xncf.SystemManager | System Management Module | [![Senparc.Xncf.SystemManager][badge-Senparc.Xncf.SystemManager]][nuget-Senparc.Xncf.SystemManager] |
| Senparc.Xncf.SystemPermission | System Permission Module | [![Senparc.Xncf.SystemPermission][badge-Senparc.Xncf.SystemPermission]][nuget-Senparc.Xncf.SystemPermission] |
| Senparc.Xncf.Tenant | Multi-tenant Management Module | [![Senparc.Xncf.Tenant][badge-Senparc.Xncf.Tenant]][nuget-Senparc.Xncf.Tenant] |
| Senparc.Xncf.Tenant.Interface | Multi-tenant Interface Module | [![Senparc.Xncf.Tenant.Interface][badge-Senparc.Xncf.Tenant.Interface]][nuget-Senparc.Xncf.Tenant.Interface] |
| Senparc.Xncf.XncfModuleManager | XNCF Module Manager | [![Senparc.Xncf.XncfModuleManager][badge-Senparc.Xncf.XncfModuleManager]][nuget-Senparc.Xncf.XncfModuleManager] |

## Contributing

We welcome developers to contribute to NCF. If you want to contribute, please:

1. Fork this repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

Apache License Version 2.0

For details, please refer to: [LICENSE](LICENSE)

---

<!-- 引用定义 -->
[badge-Senparc.Ncf.XncfBase]: https://img.shields.io/nuget/v/Senparc.Ncf.XncfBase.svg
[nuget-Senparc.Ncf.XncfBase]: https://www.nuget.org/packages/Senparc.Ncf.XncfBase
[badge-Senparc.Ncf.Core]: https://img.shields.io/nuget/v/Senparc.Ncf.Core.svg
[nuget-Senparc.Ncf.Core]: https://www.nuget.org/packages/Senparc.Ncf.Core
[badge-Senparc.Ncf.DatabasePlant]: https://img.shields.io/nuget/v/Senparc.Ncf.DatabasePlant.svg
[nuget-Senparc.Ncf.DatabasePlant]: https://www.nuget.org/packages/Senparc.Ncf.DatabasePlant
[badge-Senparc.Ncf.Log]: https://img.shields.io/nuget/v/Senparc.Ncf.Log.svg
[nuget-Senparc.Ncf.Log]: https://www.nuget.org/packages/Senparc.Ncf.Log
[badge-Senparc.Ncf.AreaBase]: https://img.shields.io/nuget/v/Senparc.Ncf.AreaBase.svg
[nuget-Senparc.Ncf.AreaBase]: https://www.nuget.org/packages/Senparc.Ncf.AreaBase
[badge-Senparc.Ncf.UnitTestExtension]: https://img.shields.io/nuget/v/Senparc.Ncf.UnitTestExtension.svg
[nuget-Senparc.Ncf.UnitTestExtension]: https://www.nuget.org/packages/Senparc.Ncf.UnitTestExtension
[badge-Senparc.Ncf.Mvc.UI]: https://img.shields.io/nuget/v/Senparc.Ncf.Mvc.UI.svg
[nuget-Senparc.Ncf.Mvc.UI]: https://www.nuget.org/packages/Senparc.Ncf.Mvc.UI
[badge-Senparc.Ncf.Database]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.svg
[nuget-Senparc.Ncf.Database]: https://www.nuget.org/packages/Senparc.Ncf.Database
[badge-Senparc.Ncf.Database.MySql.Backup]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.MySql.Backup.svg
[nuget-Senparc.Ncf.Database.MySql.Backup]: https://www.nuget.org/packages/Senparc.Ncf.Database.MySql.Backup
[badge-Senparc.Ncf.Database.MySql]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.MySql.svg
[nuget-Senparc.Ncf.Database.MySql]: https://www.nuget.org/packages/Senparc.Ncf.Database.MySql
[badge-Senparc.Ncf.Database.Oracle]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.Oracle.svg
[nuget-Senparc.Ncf.Database.Oracle]: https://www.nuget.org/packages/Senparc.Ncf.Database.Oracle
[badge-Senparc.Ncf.Database.PostgreSQL]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.PostgreSQL.svg
[nuget-Senparc.Ncf.Database.PostgreSQL]: https://www.nuget.org/packages/Senparc.Ncf.Database.PostgreSQL
[badge-Senparc.Ncf.Database.Sqlite]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.Sqlite.svg
[nuget-Senparc.Ncf.Database.Sqlite]: https://www.nuget.org/packages/Senparc.Ncf.Database.Sqlite
[badge-Senparc.Ncf.Database.SqlServer]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.SqlServer.svg
[nuget-Senparc.Ncf.Database.SqlServer]: https://www.nuget.org/packages/Senparc.Ncf.Database.SqlServer
[badge-Senparc.Ncf.Database.Dm]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.Dm.svg
[nuget-Senparc.Ncf.Database.Dm]: https://www.nuget.org/packages/Senparc.Ncf.Database.Dm
[badge-Senparc.Ncf.Database.InMemory]: https://img.shields.io/nuget/v/Senparc.Ncf.Database.InMemory.svg
[nuget-Senparc.Ncf.Database.InMemory]: https://www.nuget.org/packages/Senparc.Ncf.Database.InMemory
[badge-Senparc.Ncf.Repository]: https://img.shields.io/nuget/v/Senparc.Ncf.Repository.svg
[nuget-Senparc.Ncf.Repository]: https://www.nuget.org/packages/Senparc.Ncf.Repository
[badge-Senparc.Ncf.Service]: https://img.shields.io/nuget/v/Senparc.Ncf.Service.svg
[nuget-Senparc.Ncf.Service]: https://www.nuget.org/packages/Senparc.Ncf.Service
[badge-Senparc.Ncf.SMS]: https://img.shields.io/nuget/v/Senparc.Ncf.SMS.svg
[nuget-Senparc.Ncf.SMS]: https://www.nuget.org/packages/Senparc.Ncf.SMS
[badge-Senparc.Ncf.Utility]: https://img.shields.io/nuget/v/Senparc.Ncf.Utility.svg
[nuget-Senparc.Ncf.Utility]: https://www.nuget.org/packages/Senparc.Ncf.Utility

[badge-Senparc.Xncf.AgentsManager]: https://img.shields.io/nuget/v/Senparc.Xncf.AgentsManager.svg
[nuget-Senparc.Xncf.AgentsManager]: https://www.nuget.org/packages/Senparc.Xncf.AgentsManager
[badge-Senparc.Xncf.AIAgentsHub]: https://img.shields.io/nuget/v/Senparc.Xncf.AIAgentsHub.svg
[nuget-Senparc.Xncf.AIAgentsHub]: https://www.nuget.org/packages/Senparc.Xncf.AIAgentsHub
[badge-Senparc.Xncf.AIKernel]: https://img.shields.io/nuget/v/Senparc.Xncf.AIKernel.svg
[nuget-Senparc.Xncf.AIKernel]: https://www.nuget.org/packages/Senparc.Xncf.AIKernel
[badge-Senparc.Xncf.ChangeNamespace]: https://img.shields.io/nuget/v/Senparc.Xncf.ChangeNamespace.svg
[nuget-Senparc.Xncf.ChangeNamespace]: https://www.nuget.org/packages/Senparc.Xncf.ChangeNamespace
[badge-Senparc.Xncf.Dapr]: https://img.shields.io/nuget/v/Senparc.Xncf.Dapr.svg
[nuget-Senparc.Xncf.Dapr]: https://www.nuget.org/packages/Senparc.Xncf.Dapr
[badge-Senparc.Xncf.DatabaseToolkit]: https://img.shields.io/nuget/v/Senparc.Xncf.DatabaseToolkit.svg
[nuget-Senparc.Xncf.DatabaseToolkit]: https://www.nuget.org/packages/Senparc.Xncf.DatabaseToolkit
[badge-Senparc.Xncf.DynamicData]: https://img.shields.io/nuget/v/Senparc.Xncf.DynamicData.svg
[nuget-Senparc.Xncf.DynamicData]: https://www.nuget.org/packages/Senparc.Xncf.DynamicData
[badge-Senparc.Xncf.FileManager]: https://img.shields.io/nuget/v/Senparc.Xncf.FileManager.svg
[nuget-Senparc.Xncf.FileManager]: https://www.nuget.org/packages/Senparc.Xncf.FileManager
[badge-Senparc.Xncf.KnowledgeBase]: https://img.shields.io/nuget/v/Senparc.Xncf.KnowledgeBase.svg
[nuget-Senparc.Xncf.KnowledgeBase]: https://www.nuget.org/packages/Senparc.Xncf.KnowledgeBase
[badge-Senparc.Xncf.MCP]: https://img.shields.io/nuget/v/Senparc.Xncf.MCP.svg
[nuget-Senparc.Xncf.MCP]: https://www.nuget.org/packages/Senparc.Xncf.MCP
[badge-Senparc.Xncf.PromptRange]: https://img.shields.io/nuget/v/Senparc.Xncf.PromptRange.svg
[nuget-Senparc.Xncf.PromptRange]: https://www.nuget.org/packages/Senparc.Xncf.PromptRange
[badge-Senparc.Xncf.SenMapic]: https://img.shields.io/nuget/v/Senparc.Xncf.SenMapic.svg
[nuget-Senparc.Xncf.SenMapic]: https://www.nuget.org/packages/Senparc.Xncf.SenMapic
[badge-Senparc.Xncf.Swagger]: https://img.shields.io/nuget/v/Senparc.Xncf.Swagger.svg
[nuget-Senparc.Xncf.Swagger]: https://www.nuget.org/packages/Senparc.Xncf.Swagger
[badge-Senparc.Xncf.Terminal]: https://img.shields.io/nuget/v/Senparc.Xncf.Terminal.svg
[nuget-Senparc.Xncf.Terminal]: https://www.nuget.org/packages/Senparc.Xncf.Terminal
[badge-Senparc.Xncf.XncfBuilder]: https://img.shields.io/nuget/v/Senparc.Xncf.XncfBuilder.svg
[nuget-Senparc.Xncf.XncfBuilder]: https://www.nuget.org/packages/Senparc.Xncf.XncfBuilder
[badge-Senparc.Xncf.AreasBase]: https://img.shields.io/nuget/v/Senparc.Xncf.AreasBase.svg
[nuget-Senparc.Xncf.AreasBase]: https://www.nuget.org/packages/Senparc.Xncf.AreasBase
[badge-Senparc.Xncf.Menu]: https://img.shields.io/nuget/v/Senparc.Xncf.Menu.svg
[nuget-Senparc.Xncf.Menu]: https://www.nuget.org/packages/Senparc.Xncf.Menu
[badge-Senparc.Xncf.SystemCore]: https://img.shields.io/nuget/v/Senparc.Xncf.SystemCore.svg
[nuget-Senparc.Xncf.SystemCore]: https://www.nuget.org/packages/Senparc.Xncf.SystemCore
[badge-Senparc.Xncf.SystemManager]: https://img.shields.io/nuget/v/Senparc.Xncf.SystemManager.svg
[nuget-Senparc.Xncf.SystemManager]: https://www.nuget.org/packages/Senparc.Xncf.SystemManager
[badge-Senparc.Xncf.SystemPermission]: https://img.shields.io/nuget/v/Senparc.Xncf.SystemPermission.svg
[nuget-Senparc.Xncf.SystemPermission]: https://www.nuget.org/packages/Senparc.Xncf.SystemPermission
[badge-Senparc.Xncf.Tenant]: https://img.shields.io/nuget/v/Senparc.Xncf.Tenant.svg
[nuget-Senparc.Xncf.Tenant]: https://www.nuget.org/packages/Senparc.Xncf.Tenant
[badge-Senparc.Xncf.Tenant.Interface]: https://img.shields.io/nuget/v/Senparc.Xncf.Tenant.Interface.svg
[nuget-Senparc.Xncf.Tenant.Interface]: https://www.nuget.org/packages/Senparc.Xncf.Tenant.Interface
[badge-Senparc.Xncf.XncfModuleManager]: https://img.shields.io/nuget/v/Senparc.Xncf.XncfModuleManager.svg
[nuget-Senparc.Xncf.XncfModuleManager]: https://www.nuget.org/packages/Senparc.Xncf.XncfModuleManager

