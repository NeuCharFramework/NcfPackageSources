[中文版](README.cn.md)

# NcfPackageSources

[Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)

## Table of contents

- [Introduction](#introduction)
- [Development environment](#development-environment)
- [Quick start](#quick-start)
- [Project structure](#project-structure)
- [Module loading order](#module-loading-order)
- [Available NuGet packages](#available-nuget-packages)
- [Contributing](#contributing)
- [Community](#community)
- [License](#license)

## Introduction

This repository contains the core foundational libraries for the official [NCF (NeuCharFramework)](https://github.com/NeuCharFramework/NCF) template packages.

When you build applications with the [NCF](https://github.com/NeuCharFramework/NCF) template, these libraries provide foundational capabilities. Normally you only reference them; you do not need to work inside their implementation.

Use this repository when you need to understand, modify, or debug those libraries.

[NCF](https://github.com/NeuCharFramework/NCF) documentation: [https://doc.ncf.pub/](https://doc.ncf.pub/).

## Development environment

- Visual Studio 2019+ or current VS Code
- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- Supported databases:
  - SQLite
  - MySQL
  - SQL Server (2012+)
  - PostgreSQL
  - Oracle
  - DM (Dameng)

## Quick start

1. Clone the repository

```bash
git clone https://github.com/NeuCharFramework/NcfPackageSources.git
```

1. Open the solution

```bash
cd NcfPackageSources
start NcfPackageSources.sln  # Windows
open NcfPackageSources.sln   # macOS
```

1. Restore packages

```bash
dotnet restore
```

1. Restore workloads (optional)

```bash
dotnet workload restore
```

1. Build and run

```bash
dotnet build
dotnet run
```

## Project structure


| Folder                | Description                                                     |
| --------------------- | --------------------------------------------------------------- |
| src/Basic             | Required basic official libraries, prefixed with `Senparc.Ncf.` |
| src/Extensions        | Optional extension packages, prefixed with `Senparc.Xncf.`      |
| src/Extensions/System | System modules                                                  |


## Module loading order

The `[XncfOrder(x)]` attribute sets module load order in **descending** order (larger numbers load first):

- `0`: default; omit if not needed
- `1`–`5000`: important modules that should preload
- `5000+`: system and basic modules
- `58xx`: AI-related foundational modules
- `59xx`: low-level system foundational modules

## Available NuGet packages

### Basic libraries


| Package                           | Description                      | Version                                                                   |
| --------------------------------- | -------------------------------- | ------------------------------------------------------------------------- |
| Senparc.Ncf.XncfBase              | XNCF module base                 | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.XncfBase)              |
| Senparc.Ncf.Core                  | Core library                     | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Core)                  |
| Senparc.Ncf.DatabasePlant         | Database plant / migrations host | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.DatabasePlant)         |
| Senparc.Ncf.Log                   | Logging                          | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Log)                   |
| Senparc.Ncf.AreaBase              | Area base                        | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.AreaBase)              |
| Senparc.Ncf.UnitTestExtension     | Unit test extensions             | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.UnitTestExtension)     |
| Senparc.Ncf.Mvc.UI                | MVC UI components                | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Mvc.UI)                |
| Senparc.Ncf.Database              | Database base                    | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database)              |
| Senparc.Ncf.Database.MySql.Backup | MySQL backup                     | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.MySql.Backup) |
| Senparc.Ncf.Database.MySql        | MySQL provider                   | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.MySql)        |
| Senparc.Ncf.Database.Oracle       | Oracle provider                  | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.Oracle)       |
| Senparc.Ncf.Database.PostgreSQL   | PostgreSQL provider              | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.PostgreSQL)   |
| Senparc.Ncf.Database.Sqlite       | SQLite provider                  | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.Sqlite)       |
| Senparc.Ncf.Database.SqlServer    | SQL Server provider              | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.SqlServer)    |
| Senparc.Ncf.Database.Dm           | Dameng (DM) provider             | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.Dm)           |
| Senparc.Ncf.Database.InMemory     | In-memory database               | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Database.InMemory)     |
| Senparc.Ncf.Repository            | Repository layer                 | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Repository)            |
| Senparc.Ncf.Service               | Service layer                    | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Service)               |
| Senparc.Ncf.SMS                   | SMS                              | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.SMS)                   |
| Senparc.Ncf.Utility               | Utilities                        | [NuGet](https://www.nuget.org/packages/Senparc.Ncf.Utility)               |


### Extension XNCF modules


| Package                        | Description           | Version                                                                |
| ------------------------------ | --------------------- | ---------------------------------------------------------------------- |
| Senparc.Xncf.AgentsManager     | Agents manager        | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.AgentsManager)     |
| Senparc.Xncf.AIAgentsHub       | AI agents hub         | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.AIAgentsHub)       |
| Senparc.Xncf.AIKernel          | AI kernel             | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.AIKernel)          |
| Senparc.Xncf.ChangeNamespace   | Namespace change tool | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.ChangeNamespace)   |
| Senparc.Xncf.Dapr              | Dapr support          | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.Dapr)              |
| Senparc.Xncf.DatabaseToolkit   | Database toolkit      | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.DatabaseToolkit)   |
| Senparc.Xncf.DynamicData       | Dynamic data          | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.DynamicData)       |
| Senparc.Xncf.FileManager       | File manager          | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.FileManager)       |
| Senparc.Xncf.KnowledgeBase     | AI knowledge base     | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.KnowledgeBase)     |
| Senparc.Xncf.MCP               | MCP module            | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.MCP)               |
| Senparc.Xncf.PromptRange       | Prompt range          | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.PromptRange)       |
| Senparc.Xncf.SenMapic          | SenMapic crawler      | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.SenMapic)          |
| Senparc.Xncf.Swagger           | Swagger / OpenAPI     | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.Swagger)           |
| Senparc.Xncf.Terminal          | Terminal commands     | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.Terminal)          |
| Senparc.Xncf.XncfBuilder       | XNCF module builder   | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.XncfBuilder)       |
| Senparc.Xncf.AreasBase         | Areas base            | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.AreasBase)         |
| Senparc.Xncf.Menu              | Menu management       | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.Menu)              |
| Senparc.Xncf.SystemCore        | System core           | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.SystemCore)        |
| Senparc.Xncf.SystemManager     | System manager        | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.SystemManager)     |
| Senparc.Xncf.SystemPermission  | System permissions    | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.SystemPermission)  |
| Senparc.Xncf.Tenant            | Multi-tenant          | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.Tenant)            |
| Senparc.Xncf.Tenant.Interface  | Tenant interfaces     | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.Tenant.Interface)  |
| Senparc.Xncf.XncfModuleManager | XNCF module manager   | [NuGet](https://www.nuget.org/packages/Senparc.Xncf.XncfModuleManager) |


## Contributing

1. Fork this repository
2. Create a feature branch
3. Commit your changes
4. Push the branch
5. Open a Pull Request

## Community

- [Discord](https://discord.gg/Jkk4nYgABw)
- [LinkedIn](https://www.linkedin.com/company/111970639/)

## License

Apache License Version 2.0. See [LICENSE](LICENSE).

---

