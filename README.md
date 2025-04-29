<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />

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

[NCF](https://github.com/NeuCharFramework/NCF) 文档地址：[https://www.ncf.pub/docs/](https://www.ncf.pub/docs/)。

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

3. 还原包
```bash
dotnet restore
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

| 包名 / Package Name | 描述 / Description | 版本 / Version |
|---------------------|-------------------|----------------|
| Senparc.AI | 为所有标准接口和基础功能的基础模块 | <img src="https://img.shields.io/nuget/v/Senparc.AI.svg" alt="Senparc.AI"></img> |
| Senparc.Ncf.XncfBase | XNCF 模块基础库 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.XncfBase.svg" alt="Senparc.Ncf.XncfBase"></img> |
| Senparc.Ncf.Core | 核心基础库 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Core.svg" alt="Senparc.Ncf.Core"></img> |
| Senparc.Ncf.DatabasePlant | 数据库组装厂 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.DatabasePlant.svg" alt="Senparc.Ncf.DatabasePlant"></img> |
| Senparc.Ncf.Log | 日志模块 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Log.svg" alt="Senparc.Ncf.Log"></img> |
| Senparc.Ncf.AreaBase | Area 基础模块 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.AreaBase.svg" alt="Senparc.Ncf.AreaBase"></img> |
| Senparc.Ncf.UnitTestExtension | 单元测试扩展 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.UnitTestExtension.svg" alt="Senparc.Ncf.UnitTestExtension"></img> |
| Senparc.Ncf.Mvc.UI | MVC UI 组件 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Mvc.UI.svg" alt="Senparc.Ncf.Mvc.UI"></img> |
| Senparc.Ncf.Database | 数据库基础库 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.svg" alt="Senparc.Ncf.Database"></img> |
| Senparc.Ncf.Database.MySql.Backup | MySQL 数据库备份 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.MySql.Backup.svg" alt="Senparc.Ncf.Database.MySql.Backup"></img> |
| Senparc.Ncf.Database.MySql | MySQL 数据库支持 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.MySql.svg" alt="Senparc.Ncf.Database.MySql"></img> |
| Senparc.Ncf.Database.Oracle | Oracle 数据库支持 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.Oracle.svg" alt="Senparc.Ncf.Database.Oracle"></img> |
| Senparc.Ncf.Database.PostgreSQL | PostgreSQL 数据库支持 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.PostgreSQL.svg" alt="Senparc.Ncf.Database.PostgreSQL"></img> |
| Senparc.Ncf.Database.Sqlite | SQLite 数据库支持 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.Sqlite.svg" alt="Senparc.Ncf.Database.Sqlite"></img> |
| Senparc.Ncf.Database.SqlServer | SQL Server 数据库支持 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.SqlServer.svg" alt="Senparc.Ncf.Database.SqlServer"></img> |
| Senparc.Ncf.Database.Dm | 达梦(DM)数据库支持 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.Dm.svg" alt="Senparc.Ncf.Database.Dm"></img> |
| Senparc.Ncf.Database.InMemory | 内存数据库支持 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Database.InMemory.svg" alt="Senparc.Ncf.Database.InMemory"></img> |
| Senparc.Ncf.Repository | 仓储层 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Repository.svg" alt="Senparc.Ncf.Repository"></img> |
| Senparc.Ncf.Service | 服务层 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Service.svg" alt="Senparc.Ncf.Service"></img> |
| Senparc.Ncf.SMS | 短信服务 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.SMS.svg" alt="Senparc.Ncf.SMS"></img> |
| Senparc.Ncf.Utility | 工具类库 | <img src="https://img.shields.io/nuget/v/Senparc.Ncf.Utility.svg" alt="Senparc.Ncf.Utility"></img> |
| Senparc.Xncf.AgentsManager | 智能体管理器 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.AgentsManager.svg" alt="Senparc.Xncf.AgentsManager"></img> |
| Senparc.Xncf.AIAgentsHub | AI 智能体中心 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.AIAgentsHub.svg" alt="Senparc.Xncf.AIAgentsHub"></img> |
| Senparc.Xncf.AIKernel | AI 内核 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.AIKernel.svg" alt="Senparc.Xncf.AIKernel"></img> |
| Senparc.Xncf.ChangeNamespace | 命名空间修改工具 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.ChangeNamespace.svg" alt="Senparc.Xncf.ChangeNamespace"></img> |
| Senparc.Xncf.Dapr | Dapr 分布式运行时支持 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.Dapr.svg" alt="Senparc.Xncf.Dapr"></img> |
| Senparc.Xncf.DatabaseToolkit | 数据库工具包模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.DatabaseToolkit.svg" alt="Senparc.Xncf.DatabaseToolkit"></img> |
| Senparc.Xncf.DynamicData | 动态数据基础模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.DynamicData.svg" alt="Senparc.Xncf.DynamicData"></img> |
| Senparc.Xncf.FileManager | 文件管理模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.FileManager.svg" alt="Senparc.Xncf.FileManager"></img> |
| Senparc.Xncf.KnowledgeBase | AI 知识库 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.KnowledgeBase.svg" alt="Senparc.Xncf.KnowledgeBase"></img> |
| Senparc.Xncf.MCP | MCF 多租户微服务平台 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.MCP.svg" alt="Senparc.Xncf.MCP"></img> |
| Senparc.Xncf.PromptRange | AI 提示词范围 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.PromptRange.svg" alt="Senparc.Xncf.PromptRange"></img> |
| Senparc.Xncf.SenMapic | SenMapic 爬虫模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.SenMapic.svg" alt="Senparc.Xncf.SenMapic"></img> |
| Senparc.Xncf.Swagger | Swagger 接口文档 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.Swagger.svg" alt="Senparc.Xncf.Swagger"></img> |
| Senparc.Xncf.Terminal | 终端命令模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.Terminal.svg" alt="Senparc.Xncf.Terminal"></img> |
| Senparc.Xncf.XncfBuilder | XNCF 模块生成器 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.XncfBuilder.svg" alt="Senparc.Xncf.XncfBuilder"></img> |
| Senparc.Xncf.AreasBase | 系统区域基础模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.AreasBase.svg" alt="Senparc.Xncf.AreasBase"></img> |
| Senparc.Xncf.Menu | 菜单管理模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.Menu.svg" alt="Senparc.Xncf.Menu"></img> |
| Senparc.Xncf.SystemCore | 系统核心模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.SystemCore.svg" alt="Senparc.Xncf.SystemCore"></img> |
| Senparc.Xncf.SystemManager | 系统管理模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.SystemManager.svg" alt="Senparc.Xncf.SystemManager"></img> |
| Senparc.Xncf.SystemPermission | 系统权限模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.SystemPermission.svg" alt="Senparc.Xncf.SystemPermission"></img> |
| Senparc.Xncf.Tenant | 租户管理模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.Tenant.svg" alt="Senparc.Xncf.Tenant"></img> |
| Senparc.Xncf.Tenant.Interface | 租户接口模块 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.Tenant.Interface.svg" alt="Senparc.Xncf.Tenant.Interface"></img> |
| Senparc.Xncf.XncfModuleManager | XNCF 模块管理器 | <img src="https://img.shields.io/nuget/v/Senparc.Xncf.XncfModuleManager.svg" alt="Senparc.Xncf.XncfModuleManager"></img> |

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

