<img src="https://github.com/user-attachments/assets/f4377f39-7f2d-4030-84e1-1095c07f3e84" alt="NeuCharFramework" width="300" >

<!--<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />-->

# NcfPackageSources

[![Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_apis/build/status/NeuCharFramework.NcfPackageSources?branchName=master)](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)

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

2. 打开解决方案
```bash
cd NcfPackageSources
start NcfPackageSources.sln  # Windows
open NcfPackageSources.sln   # macOS
```

3. 还原包并还原工作负载
```bash
dotnet restore
dotnet workload restore
```

4. 编译运行
```bash
dotnet build
dotnet run
```

## 项目结构 / Project Structure

|    文件夹 / Folder    |    说明 / Description         |
|--------------|-----------------|
|  src/Basic       |  必须安装的基础官方库，以 `Separc.Ncf.` 开头 <br> Required basic official libraries, prefixed with `Separc.Ncf.`
|  src/Extensions  |  可选的扩展包，以 `Senparc.Xncf.` 开头 <br> Optional extension packages, prefixed with `Senparc.Xncf.`
|  src/Extensions/System  | 系统模块 <br> System modules

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

| 包名 / Package Name | 描述 / Description | 版本 / Version |
|---------------------|-------------------|----------------|
| Senparc.Ncf.XncfBase | XNCF 模块基础库 | [![Senparc.Ncf.XncfBase][badge-Senparc.Ncf.XncfBase]][nuget-Senparc.Ncf.XncfBase] |
| Senparc.Ncf.Core | 核心基础库 | [![Senparc.Ncf.Core][badge-Senparc.Ncf.Core]][nuget-Senparc.Ncf.Core] |
| Senparc.Ncf.DatabasePlant | 数据库组装厂 | [![Senparc.Ncf.DatabasePlant][badge-Senparc.Ncf.DatabasePlant]][nuget-Senparc.Ncf.DatabasePlant] |
| Senparc.Ncf.Log | 日志模块 | [![Senparc.Ncf.Log][badge-Senparc.Ncf.Log]][nuget-Senparc.Ncf.Log] |
| Senparc.Ncf.AreaBase | Area 基础模块 | [![Senparc.Ncf.AreaBase][badge-Senparc.Ncf.AreaBase]][nuget-Senparc.Ncf.AreaBase] |
| Senparc.Ncf.UnitTestExtension | 单元测试扩展 | [![Senparc.Ncf.UnitTestExtension][badge-Senparc.Ncf.UnitTestExtension]][nuget-Senparc.Ncf.UnitTestExtension] |
| Senparc.Ncf.Mvc.UI | MVC UI 组件 | [![Senparc.Ncf.Mvc.UI][badge-Senparc.Ncf.Mvc.UI]][nuget-Senparc.Ncf.Mvc.UI] |
| Senparc.Ncf.Database | 数据库基础库 | [![Senparc.Ncf.Database][badge-Senparc.Ncf.Database]][nuget-Senparc.Ncf.Database] |
| Senparc.Ncf.Database.MySql.Backup | MySQL 数据库备份 | [![Senparc.Ncf.Database.MySql.Backup][badge-Senparc.Ncf.Database.MySql.Backup]][nuget-Senparc.Ncf.Database.MySql.Backup] |
| Senparc.Ncf.Database.MySql | MySQL 数据库支持 | [![Senparc.Ncf.Database.MySql][badge-Senparc.Ncf.Database.MySql]][nuget-Senparc.Ncf.Database.MySql] |
| Senparc.Ncf.Database.Oracle | Oracle 数据库支持 | [![Senparc.Ncf.Database.Oracle][badge-Senparc.Ncf.Database.Oracle]][nuget-Senparc.Ncf.Database.Oracle] |
| Senparc.Ncf.Database.PostgreSQL | PostgreSQL 数据库支持 | [![Senparc.Ncf.Database.PostgreSQL][badge-Senparc.Ncf.Database.PostgreSQL]][nuget-Senparc.Ncf.Database.PostgreSQL] |
| Senparc.Ncf.Database.Sqlite | SQLite 数据库支持 | [![Senparc.Ncf.Database.Sqlite][badge-Senparc.Ncf.Database.Sqlite]][nuget-Senparc.Ncf.Database.Sqlite] |
| Senparc.Ncf.Database.SqlServer | SQL Server 数据库支持 | [![Senparc.Ncf.Database.SqlServer][badge-Senparc.Ncf.Database.SqlServer]][nuget-Senparc.Ncf.Database.SqlServer] |
| Senparc.Ncf.Database.Dm | 达梦(DM)数据库支持 | [![Senparc.Ncf.Database.Dm][badge-Senparc.Ncf.Database.Dm]][nuget-Senparc.Ncf.Database.Dm] |
| Senparc.Ncf.Database.InMemory | 内存数据库支持 | [![Senparc.Ncf.Database.InMemory][badge-Senparc.Ncf.Database.InMemory]][nuget-Senparc.Ncf.Database.InMemory] |
| Senparc.Ncf.Repository | 仓储层 | [![Senparc.Ncf.Repository][badge-Senparc.Ncf.Repository]][nuget-Senparc.Ncf.Repository] |
| Senparc.Ncf.Service | 服务层 | [![Senparc.Ncf.Service][badge-Senparc.Ncf.Service]][nuget-Senparc.Ncf.Service] |
| Senparc.Ncf.SMS | 短信服务 | [![Senparc.Ncf.SMS][badge-Senparc.Ncf.SMS]][nuget-Senparc.Ncf.SMS] |
| Senparc.Ncf.Utility | 工具类库 | [![Senparc.Ncf.Utility][badge-Senparc.Ncf.Utility]][nuget-Senparc.Ncf.Utility] |

### 扩展 XNCF 模块 / Extension XNCF Modules

| 包名 / Package Name | 描述 / Description | 版本 / Version |
|---------------------|-------------------|----------------|
| Senparc.Xncf.AgentsManager | 智能体管理器 | [![Senparc.Xncf.AgentsManager][badge-Senparc.Xncf.AgentsManager]][nuget-Senparc.Xncf.AgentsManager] |
| Senparc.Xncf.AIAgentsHub | AI 智能体中心 | [![Senparc.Xncf.AIAgentsHub][badge-Senparc.Xncf.AIAgentsHub]][nuget-Senparc.Xncf.AIAgentsHub] |
| Senparc.Xncf.AIKernel | AI 内核 | [![Senparc.Xncf.AIKernel][badge-Senparc.Xncf.AIKernel]][nuget-Senparc.Xncf.AIKernel] |
| Senparc.Xncf.ChangeNamespace | 命名空间修改工具 | [![Senparc.Xncf.ChangeNamespace][badge-Senparc.Xncf.ChangeNamespace]][nuget-Senparc.Xncf.ChangeNamespace] |
| Senparc.Xncf.Dapr | Dapr 分布式运行时支持 | [![Senparc.Xncf.Dapr][badge-Senparc.Xncf.Dapr]][nuget-Senparc.Xncf.Dapr] |
| Senparc.Xncf.DatabaseToolkit | 数据库工具包模块 | [![Senparc.Xncf.DatabaseToolkit][badge-Senparc.Xncf.DatabaseToolkit]][nuget-Senparc.Xncf.DatabaseToolkit] |
| Senparc.Xncf.DynamicData | 动态数据基础模块 | [![Senparc.Xncf.DynamicData][badge-Senparc.Xncf.DynamicData]][nuget-Senparc.Xncf.DynamicData] |
| Senparc.Xncf.FileManager | 文件管理模块 | [![Senparc.Xncf.FileManager][badge-Senparc.Xncf.FileManager]][nuget-Senparc.Xncf.FileManager] |
| Senparc.Xncf.KnowledgeBase | AI 知识库 | [![Senparc.Xncf.KnowledgeBase][badge-Senparc.Xncf.KnowledgeBase]][nuget-Senparc.Xncf.KnowledgeBase] |
| Senparc.Xncf.MCP | MCF 多租户微服务平台 | [![Senparc.Xncf.MCP][badge-Senparc.Xncf.MCP]][nuget-Senparc.Xncf.MCP] |
| Senparc.Xncf.PromptRange | AI 提示词靶场 | [![Senparc.Xncf.PromptRange][badge-Senparc.Xncf.PromptRange]][nuget-Senparc.Xncf.PromptRange] |
| Senparc.Xncf.SenMapic | SenMapic 爬虫模块 | [![Senparc.Xncf.SenMapic][badge-Senparc.Xncf.SenMapic]][nuget-Senparc.Xncf.SenMapic] |
| Senparc.Xncf.Swagger | Swagger 接口文档 | [![Senparc.Xncf.Swagger][badge-Senparc.Xncf.Swagger]][nuget-Senparc.Xncf.Swagger] |
| Senparc.Xncf.Terminal | 终端命令模块 | [![Senparc.Xncf.Terminal][badge-Senparc.Xncf.Terminal]][nuget-Senparc.Xncf.Terminal] |
| Senparc.Xncf.XncfBuilder | XNCF 模块生成器 | [![Senparc.Xncf.XncfBuilder][badge-Senparc.Xncf.XncfBuilder]][nuget-Senparc.Xncf.XncfBuilder] |
| Senparc.Xncf.AreasBase | 系统区域基础模块 | [![Senparc.Xncf.AreasBase][badge-Senparc.Xncf.AreasBase]][nuget-Senparc.Xncf.AreasBase] |
| Senparc.Xncf.Menu | 菜单管理模块 | [![Senparc.Xncf.Menu][badge-Senparc.Xncf.Menu]][nuget-Senparc.Xncf.Menu] |
| Senparc.Xncf.SystemCore | 系统核心模块 | [![Senparc.Xncf.SystemCore][badge-Senparc.Xncf.SystemCore]][nuget-Senparc.Xncf.SystemCore] |
| Senparc.Xncf.SystemManager | 系统管理模块 | [![Senparc.Xncf.SystemManager][badge-Senparc.Xncf.SystemManager]][nuget-Senparc.Xncf.SystemManager] |
| Senparc.Xncf.SystemPermission | 系统权限模块 | [![Senparc.Xncf.SystemPermission][badge-Senparc.Xncf.SystemPermission]][nuget-Senparc.Xncf.SystemPermission] |
| Senparc.Xncf.Tenant | 多租户管理模块 | [![Senparc.Xncf.Tenant][badge-Senparc.Xncf.Tenant]][nuget-Senparc.Xncf.Tenant] |
| Senparc.Xncf.Tenant.Interface | 多租户接口模块 | [![Senparc.Xncf.Tenant.Interface][badge-Senparc.Xncf.Tenant.Interface]][nuget-Senparc.Xncf.Tenant.Interface] |
| Senparc.Xncf.XncfModuleManager | XNCF 模块管理器 | [![Senparc.Xncf.XncfModuleManager][badge-Senparc.Xncf.XncfModuleManager]][nuget-Senparc.Xncf.XncfModuleManager] |

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

