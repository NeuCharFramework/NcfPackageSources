[English](README.md) | 中文

# NcfPackageSources

[Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)

**国际社群：** [Discord](https://discord.gg/Jkk4nYgABw) · [LinkedIn](https://www.linkedin.com/company/111970639/)

## 目录 / Table of Contents

- [项目介绍 / Introduction](#项目介绍--introduction)
- [开发环境 / Development Environment](#开发环境--development-environment)
- [快速开始 / Quick Start](#快速开始--quick-start)
- [项目结构 / Project Structure](#项目结构--project-structure)
- [模块加载顺序 / Module Loading Order](#模块加载顺序--module-loading-order)
- [可用的 Nuget 包 / Available NuGet Packages](#可用的-nuget-包--available-nuget-packages)
- [贡献指南 / Contributing](#贡献指南--contributing)
- [许可证 / License](#许可证--license)

## 项目介绍 / Introduction

本项目为 [NCF（NeuCharFramework）](https://github.com/NeuCharFramework/NCF) 模板官方包的核心基础库源码。

当您使用 [NCF](https://github.com/NeuCharFramework/NCF) 模板开发项目时，核心基础库将为您提供一系列基础能力的支撑，通常情况下您无需关心这些库的具体实现，只需要引用即可。

当您需要了解、修改或调试相关基础库时，您可以通过本项目获取源码。

[NCF](https://github.com/NeuCharFramework/NCF) 文档地址：[https://doc.ncf.pub/](https://doc.ncf.pub/)。

## 开发环境 / Development Environment

- Visual Studio 2019+ 或 VS Code 最新版本
- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- 支持的数据库：
  - SQLite
  - MySQL
  - SQL Server (2012+)
  - PostgreSQL
  - Oracle
  - DM（达梦）

## 快速开始 / Quick Start

1. 克隆仓库

```bash
git clone https://github.com/NeuCharFramework/NcfPackageSources.git
```

1. 打开解决方案

```bash
cd NcfPackageSources
start NcfPackageSources.sln  # Windows
open NcfPackageSources.sln   # macOS
```

1. 还原包

```bash
dotnet restore
```

1. 还原工作负载（可选）

```bash
dotnet workload restore
```

1. 编译运行

```bash
dotnet build
dotnet run
```

## 项目结构 / Project Structure


| 文件夹 / Folder          | 说明 / Description                                                                             |
| --------------------- | -------------------------------------------------------------------------------------------- |
| src/Basic             | 必须安装的基础官方库，以 `Separc.Ncf.` 开头 Required basic official libraries, prefixed with `Separc.Ncf.` |
| src/Extensions        | 可选的扩展包，以 `Senparc.Xncf.` 开头 Optional extension packages, prefixed with `Senparc.Xncf.`       |
| src/Extensions/System | 系统模块 System modules                                                                          |


## 模块加载顺序 / Module Loading Order

使用 `[XncfOrder(x)]` 特性指定模块的加载顺序，为降序排列，数字越大越在前：

The `[XncfOrder(x)]` attribute specifies the loading order of modules in descending order, larger numbers load first:

- `0`：默认值，可以不用设置 / Default value, no need to set
- `1` ~ `5000`：需要预加载的重要模块 / Important modules that need preloading
- `5000+`：系统及基础模块 / System and basic modules
- `58xx`：AI 相关基础模块 / AI-related basic modules
- `59xx`：系统底层基础模块 / System underlying basic modules

## 可用的 Nuget 包 / Available NuGet Packages

### 基础库 / Basic Libraries


| 包名 / Package Name                 | 描述 / Description | 版本 / Version                                                                                          |
| --------------------------------- | ---------------- | ----------------------------------------------------------------------------------------------------- |
| Senparc.Ncf.XncfBase              | XNCF 模块基础库       | [Senparc.Ncf.XncfBase](https://www.nuget.org/packages/Senparc.Ncf.XncfBase)                           |
| Senparc.Ncf.Core                  | 核心基础库            | [Senparc.Ncf.Core](https://www.nuget.org/packages/Senparc.Ncf.Core)                                   |
| Senparc.Ncf.DatabasePlant         | 数据库组装厂           | [Senparc.Ncf.DatabasePlant](https://www.nuget.org/packages/Senparc.Ncf.DatabasePlant)                 |
| Senparc.Ncf.Log                   | 日志模块             | [Senparc.Ncf.Log](https://www.nuget.org/packages/Senparc.Ncf.Log)                                     |
| Senparc.Ncf.AreaBase              | Area 基础模块        | [Senparc.Ncf.AreaBase](https://www.nuget.org/packages/Senparc.Ncf.AreaBase)                           |
| Senparc.Ncf.UnitTestExtension     | 单元测试扩展           | [Senparc.Ncf.UnitTestExtension](https://www.nuget.org/packages/Senparc.Ncf.UnitTestExtension)         |
| Senparc.Ncf.Mvc.UI                | MVC UI 组件        | [Senparc.Ncf.Mvc.UI](https://www.nuget.org/packages/Senparc.Ncf.Mvc.UI)                               |
| Senparc.Ncf.Database              | 数据库基础库           | [Senparc.Ncf.Database](https://www.nuget.org/packages/Senparc.Ncf.Database)                           |
| Senparc.Ncf.Database.MySql.Backup | MySQL 数据库备份      | [Senparc.Ncf.Database.MySql.Backup](https://www.nuget.org/packages/Senparc.Ncf.Database.MySql.Backup) |
| Senparc.Ncf.Database.MySql        | MySQL 数据库支持      | [Senparc.Ncf.Database.MySql](https://www.nuget.org/packages/Senparc.Ncf.Database.MySql)               |
| Senparc.Ncf.Database.Oracle       | Oracle 数据库支持     | [Senparc.Ncf.Database.Oracle](https://www.nuget.org/packages/Senparc.Ncf.Database.Oracle)             |
| Senparc.Ncf.Database.PostgreSQL   | PostgreSQL 数据库支持 | [Senparc.Ncf.Database.PostgreSQL](https://www.nuget.org/packages/Senparc.Ncf.Database.PostgreSQL)     |
| Senparc.Ncf.Database.Sqlite       | SQLite 数据库支持     | [Senparc.Ncf.Database.Sqlite](https://www.nuget.org/packages/Senparc.Ncf.Database.Sqlite)             |
| Senparc.Ncf.Database.SqlServer    | SQL Server 数据库支持 | [Senparc.Ncf.Database.SqlServer](https://www.nuget.org/packages/Senparc.Ncf.Database.SqlServer)       |
| Senparc.Ncf.Database.Dm           | 达梦(DM)数据库支持      | [Senparc.Ncf.Database.Dm](https://www.nuget.org/packages/Senparc.Ncf.Database.Dm)                     |
| Senparc.Ncf.Database.InMemory     | 内存数据库支持          | [Senparc.Ncf.Database.InMemory](https://www.nuget.org/packages/Senparc.Ncf.Database.InMemory)         |
| Senparc.Ncf.Repository            | 仓储层              | [Senparc.Ncf.Repository](https://www.nuget.org/packages/Senparc.Ncf.Repository)                       |
| Senparc.Ncf.Service               | 服务层              | [Senparc.Ncf.Service](https://www.nuget.org/packages/Senparc.Ncf.Service)                             |
| Senparc.Ncf.SMS                   | 短信服务             | [Senparc.Ncf.SMS](https://www.nuget.org/packages/Senparc.Ncf.SMS)                                     |
| Senparc.Ncf.Utility               | 工具类库             | [Senparc.Ncf.Utility](https://www.nuget.org/packages/Senparc.Ncf.Utility)                             |


### 扩展 XNCF 模块 / Extension XNCF Modules


| 包名 / Package Name              | 描述 / Description | 版本 / Version                                                                                    |
| ------------------------------ | ---------------- | ----------------------------------------------------------------------------------------------- |
| Senparc.Xncf.AgentsManager     | 智能体管理器           | [Senparc.Xncf.AgentsManager](https://www.nuget.org/packages/Senparc.Xncf.AgentsManager)         |
| Senparc.Xncf.AIAgentsHub       | AI 智能体中心         | [Senparc.Xncf.AIAgentsHub](https://www.nuget.org/packages/Senparc.Xncf.AIAgentsHub)             |
| Senparc.Xncf.AIKernel          | AI 内核            | [Senparc.Xncf.AIKernel](https://www.nuget.org/packages/Senparc.Xncf.AIKernel)                   |
| Senparc.Xncf.ChangeNamespace   | 命名空间修改工具         | [Senparc.Xncf.ChangeNamespace](https://www.nuget.org/packages/Senparc.Xncf.ChangeNamespace)     |
| Senparc.Xncf.Dapr              | Dapr 分布式运行时支持    | [Senparc.Xncf.Dapr](https://www.nuget.org/packages/Senparc.Xncf.Dapr)                           |
| Senparc.Xncf.DatabaseToolkit   | 数据库工具包模块         | [Senparc.Xncf.DatabaseToolkit](https://www.nuget.org/packages/Senparc.Xncf.DatabaseToolkit)     |
| Senparc.Xncf.DynamicData       | 动态数据基础模块         | [Senparc.Xncf.DynamicData](https://www.nuget.org/packages/Senparc.Xncf.DynamicData)             |
| Senparc.Xncf.FileManager       | 文件管理模块           | [Senparc.Xncf.FileManager](https://www.nuget.org/packages/Senparc.Xncf.FileManager)             |
| Senparc.Xncf.KnowledgeBase     | AI 知识库           | [Senparc.Xncf.KnowledgeBase](https://www.nuget.org/packages/Senparc.Xncf.KnowledgeBase)         |
| Senparc.Xncf.MCP               | MCF 多租户微服务平台     | [Senparc.Xncf.MCP](https://www.nuget.org/packages/Senparc.Xncf.MCP)                             |
| Senparc.Xncf.PromptRange       | AI 提示词靶场         | [Senparc.Xncf.PromptRange](https://www.nuget.org/packages/Senparc.Xncf.PromptRange)             |
| Senparc.Xncf.SenMapic          | SenMapic 爬虫模块    | [Senparc.Xncf.SenMapic](https://www.nuget.org/packages/Senparc.Xncf.SenMapic)                   |
| Senparc.Xncf.Swagger           | Swagger 接口文档     | [Senparc.Xncf.Swagger](https://www.nuget.org/packages/Senparc.Xncf.Swagger)                     |
| Senparc.Xncf.Terminal          | 终端命令模块           | [Senparc.Xncf.Terminal](https://www.nuget.org/packages/Senparc.Xncf.Terminal)                   |
| Senparc.Xncf.XncfBuilder       | XNCF 模块生成器       | [Senparc.Xncf.XncfBuilder](https://www.nuget.org/packages/Senparc.Xncf.XncfBuilder)             |
| Senparc.Xncf.AreasBase         | 系统区域基础模块         | [Senparc.Xncf.AreasBase](https://www.nuget.org/packages/Senparc.Xncf.AreasBase)                 |
| Senparc.Xncf.Menu              | 菜单管理模块           | [Senparc.Xncf.Menu](https://www.nuget.org/packages/Senparc.Xncf.Menu)                           |
| Senparc.Xncf.SystemCore        | 系统核心模块           | [Senparc.Xncf.SystemCore](https://www.nuget.org/packages/Senparc.Xncf.SystemCore)               |
| Senparc.Xncf.SystemManager     | 系统管理模块           | [Senparc.Xncf.SystemManager](https://www.nuget.org/packages/Senparc.Xncf.SystemManager)         |
| Senparc.Xncf.SystemPermission  | 系统权限模块           | [Senparc.Xncf.SystemPermission](https://www.nuget.org/packages/Senparc.Xncf.SystemPermission)   |
| Senparc.Xncf.Tenant            | 多租户管理模块          | [Senparc.Xncf.Tenant](https://www.nuget.org/packages/Senparc.Xncf.Tenant)                       |
| Senparc.Xncf.Tenant.Interface  | 多租户接口模块          | [Senparc.Xncf.Tenant.Interface](https://www.nuget.org/packages/Senparc.Xncf.Tenant.Interface)   |
| Senparc.Xncf.XncfModuleManager | XNCF 模块管理器       | [Senparc.Xncf.XncfModuleManager](https://www.nuget.org/packages/Senparc.Xncf.XncfModuleManager) |


## 贡献指南 / Contributing

我们欢迎开发者为 NCF 贡献代码。如果您想要贡献，请：

We welcome developers to contribute to NCF. If you want to contribute, please:

1. Fork 本仓库 / Fork this repository
2. 创建您的特性分支 / Create your feature branch
3. 提交您的改动 / Commit your changes
4. 推送到分支 / Push to the branch
5. 创建 Pull Request / Create a Pull Request

## 许可证 / License

Apache License Version 2.0

详细请参考 / For details, please refer to: [LICENSE](LICENSE)

---

