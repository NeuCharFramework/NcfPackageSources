<img src="https://github.com/user-attachments/assets/f4377f39-7f2d-4030-84e1-1095c07f3e84" alt="NeuCharFramework" width="300">

# NcfPackageSources

[![Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_apis/build/status/NeuCharFramework.NcfPackageSources?branchName=master)](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)

[English](README.md) | 中文

用于 [NeuCharFramework（NCF）](https://github.com/NeuCharFramework/NCF) 的核心库与 XNCF 模块官方源码仓库。

大多数应用开发者可以直接使用已发布的 NuGet 包，无需修改本仓库。当需要查看、调试、扩展或参与维护 NCF 应用模板背后的基础库时，请使用此仓库。

- NCF 文档：<https://doc.ncf.pub/>
- NCF 应用模板：<https://github.com/NeuCharFramework/NCF>
- NuGet 包：<https://www.nuget.org/profiles/Senparc>

## 目录

- [仓库内容](#仓库内容)
- [环境要求](#环境要求)
- [快速开始](#快速开始)
- [仓库结构](#仓库结构)
- [XNCF 模块加载顺序](#xncf-模块加载顺序)
- [构建和测试](#构建和测试)
- [运行集成基线](#运行集成基线)
- [Workspace 运行时](#workspace-运行时)
- [NuGet 包](#nuget-包)
- [打包和发布](#打包和发布)
- [贡献指南](#贡献指南)
- [许可证](#许可证)

## 仓库内容

NCF 将可复用基础设施与模块化业务能力分离：

- **基础库（`Senparc.Ncf.*`）** 提供框架基础，包括 XNCF 注册、数据访问、服务、仓储、日志、UI 支持、数据库提供程序和共享契约。
- **扩展模块（`Senparc.Xncf.*`）** 提供可选能力，例如 AI、智能体、知识库、工作流、MCP、文件、数据库工具、多租户和管理后台。
- **系统模块** 是 NCF 应用所需的 XNCF 模块，包括安装器、菜单、权限、系统管理和租户支持。
- **抽象包** 暴露跨模块接口，使其他模块无需依赖对应实现程序集即可完成引用。

## 环境要求

- [.NET SDK 10.x](https://dotnet.microsoft.com/download/dotnet/10.0)
- 支持当前 .NET 版本的 Visual Studio 2022，或最新版本 VS Code
- Git
- 首次还原时可访问 NuGet.org

主解决方案以 .NET 10 为目标。少量历史兼容、模板或工具项目会根据用途使用其他目标框架，应以对应项目文件为准。

支持的数据库包包括：

- SQLite
- MySQL
- SQL Server 2012+
- PostgreSQL
- Oracle
- DM（达梦）
- 用于测试和开发的内存提供程序

`NuGet.Config` 使用 NuGet.org 和仓库本地的 `local-nuget` 目录。部分开发流程可能需要本地包，但并非所有流程都必需。

## 快速开始

```bash
git clone https://github.com/NeuCharFramework/NcfPackageSources.git
cd NcfPackageSources
dotnet restore src/NcfPackageSources.sln
dotnet build src/NcfPackageSources.sln --no-restore
```

在 Visual Studio 中打开包源码解决方案：

```text
src/NcfPackageSources.sln
```

包含集成基线的更大解决方案为：

```text
src/NcfPackageSources_Include_NcfWebAdminBaseline.sln
```

日常包开发使用第一个解决方案；当改动需要针对 Web Admin 基线进行验证时使用第二个解决方案。

## 仓库结构

| 路径 | 用途 |
| --- | --- |
| `src/Basic/` | 必需的 NCF 基础库，以 `Senparc.Ncf.` 开头 |
| `src/Extensions/` | 可选或领域相关的 XNCF 模块，以 `Senparc.Xncf.` 开头 |
| `src/Extensions/System/` | 提供核心系统能力的 XNCF 模块 |
| `src/DevelopmentTools/` | 集成示例和开发辅助工具 |
| `tools/NcfWebAdminBaseline/` | 可运行的 NCF Web/Admin 基线，用于集成和权威源码开发 |
| `tools/NcfWebAdminSyncTool/` | 将基线内容同步到模板和打包项目 |
| `tools/BuildXncfBuilderTemplate/` | 准备 `Senparc.Xncf.XncfBuilder.Template` 的占位符 |
| `docs/` | 架构说明、实现指南、迁移记录和故障排查文档 |
| `skills/` | 用于创建、维护、打包和升级 XNCF 模块的仓库工作流 |
| `azure-pipelines.yml` | Release 构建、包验证和 NuGet 发布流水线 |

更多详情请查看[扩展模块说明](src/Extensions/readme.md)和[工具说明](tools/readme.cn.md)。

## XNCF 模块加载顺序

使用 `[XncfOrder(x)]` 控制模块注册优先级。模块按降序加载，数字越大越先加载。

| 值 | 含义 |
| --- | --- |
| `0` | 默认值，可以不设置该特性 |
| `1`-`5000` | 需要提前加载的重要模块 |
| `5000+` | 系统及基础模块 |
| `58xx` | AI 相关基础模块 |
| `59xx` | 系统底层基础模块 |

设置加载顺序时既要考虑依赖关系，也要考虑界面可见性。模块不得依赖注册顺序晚于自己的其他模块。

## 构建和测试

构建常规包源码解决方案：

```bash
dotnet restore src/NcfPackageSources.sln
dotnet build src/NcfPackageSources.sln --no-restore
```

开发迭代时只构建已修改的模块：

```bash
dotnet build src/Extensions/Senparc.Xncf.KnowledgeBase/Senparc.Xncf.KnowledgeBase.csproj --no-restore
```

运行解决方案中包含的全部测试项目：

```bash
dotnet test src/NcfPackageSources.sln --no-restore
```

或运行特定测试项目：

```bash
dotnet test src/Extensions/Senparc.Xncf.AIKernel.Tests/Senparc.Xncf.AIKernel.Tests.csproj --no-restore
```

对于未加入主解决方案的测试项目，应直接调用对应的 `.csproj`。新增测试应靠近所覆盖的功能，并根据需要将测试、工具或元数据项目设为不打包。

## 运行集成基线

使用 Aspire AppHost 启动集成服务、仪表板、健康检查、链路、指标和日志：

```bash
dotnet restore tools/NcfWebAdminBaseline/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
dotnet run --project tools/NcfWebAdminBaseline/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
```

直接运行 Web 应用：

```bash
dotnet run --project tools/NcfWebAdminBaseline/Senparc.Web/Senparc.Web.csproj
```

该基线直接引用 XNCF 模块源码，适合端到端调试。修改基线或模板同步行为前，请先阅读[基线详细指南](tools/NcfWebAdminBaseline/readme.md)。

如果模板修改后出现 XNCF Builder 生成文件冲突，只删除下面的生成目录，再通过正常 Builder 流程重新生成：

```bash
rm -rf src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder/Generated
```

## Workspace 运行时

Workspace 运行时项目统一命名为 `Senparc.Xncf.WorkspaceAgent`。目录、程序集、命名空间和 Docker 入口使用相同名称；请保持 XNCF 命名约定，即 `Xncf` 两侧总共只有两个点。

`Workspace` 通过项目引用解析 Agent 的实际输出路径，并将 `.dll`、`.deps.json` 和 `.runtimeconfig.json` 传递到 Admin 与宿主的构建/发布输出。不要使用宿主的 `$(Configuration)` 拼接 Agent 路径，因为解决方案中的依赖项目可能采用不同配置。自定义 `SenparcXncfWorkspace:AgentPath` 时必须同步更新文件名。

Workspace NativeAgent 默认禁止执行。Docker 是受支持的隔离执行方式。AdminChat Python 执行还需要启用对应的 Workspace CLI 配置并绑定 Workspace。

## NuGet 包

包名沿用项目命名空间。基础库以 `Senparc.Ncf.` 开头，XNCF 模块以 `Senparc.Xncf.` 开头。

### 核心库

| 包名 | 用途 |
| --- | --- |
| `Senparc.Ncf.XncfBase` | XNCF 模块注册和基础抽象 |
| `Senparc.Ncf.Core` | 核心框架基础设施 |
| `Senparc.Ncf.AreaBase` | 为 XNCF 模块提供 ASP.NET Core Area 支持 |
| `Senparc.Ncf.Database` | 共享数据库基础设施 |
| `Senparc.Ncf.DatabasePlant` | 数据库提供程序程序集和迁移支持 |
| `Senparc.Ncf.Repository` | 仓储层支持 |
| `Senparc.Ncf.Service` | 服务层支持 |
| `Senparc.Ncf.Log` | 日志集成 |
| `Senparc.Ncf.Utility` | 共享工具类 |
| `Senparc.Ncf.SMS` | 短信支持 |
| `Senparc.Ncf.UnitTestExtension` | 单元测试辅助工具 |
| `Senparc.Ncf.IsolatedRuntime` | 共享隔离运行时基础设施 |

数据库提供程序单独发布为 `Senparc.Ncf.Database.MySql`、`Senparc.Ncf.Database.Sqlite`、`Senparc.Ncf.Database.SqlServer`、`Senparc.Ncf.Database.PostgreSQL`、`Senparc.Ncf.Database.Oracle`、`Senparc.Ncf.Database.Dm`、`Senparc.Ncf.Database.InMemory` 和 `Senparc.Ncf.Database.MySql.Backup`。

### 扩展模块

主要扩展包包括：

- AI 与智能体：`Senparc.Xncf.AIKernel`、`Senparc.Xncf.AIAgentsHub`、`Senparc.Xncf.AgentsManager`、`Senparc.Xncf.PromptRange`、`Senparc.Xncf.KnowledgeBase`
- 集成能力：`Senparc.Xncf.Connectors`、`Senparc.Xncf.MCP`、`Senparc.Xncf.Dapr`、`Senparc.Xncf.FileManager`、`Senparc.Xncf.DesktopBridge`
- 编排与运行时：`Senparc.Xncf.NeuCharWorkflow`、`Senparc.Xncf.Sandbox`、`Senparc.Xncf.Workspace`、`Senparc.Xncf.WorkspaceAgent`
- 数据与管理：`Senparc.Xncf.DynamicData`、`Senparc.Xncf.DatabaseToolkit`、`Senparc.Xncf.FirmwareUpdate`、`Senparc.Xncf.WeixinManager`、`Senparc.Xncf.SenMapic`
- 开发工具：`Senparc.Xncf.XncfBuilder`、`Senparc.Xncf.XncfBuilder.Template`、`Senparc.Xncf.Terminal`、`Senparc.Xncf.Swagger`

系统包包括 `Senparc.Xncf.Installer`、`Senparc.Xncf.Installer.Interface`、`Senparc.Xncf.AreasBase`、`Senparc.Xncf.Menu`、`Senparc.Xncf.SystemCore`、`Senparc.Xncf.SystemManager`、`Senparc.Xncf.SystemPermission`、`Senparc.Xncf.Tenant`、`Senparc.Xncf.Tenant.Interface` 和 `Senparc.Xncf.XncfModuleManager`。

### 抽象包

抽象包可以在不依赖模块实现的情况下单独引用：

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

可在 [Senparc NuGet 主页](https://www.nuget.org/profiles/Senparc)查看包版本，或在 [NuGet.org](https://www.nuget.org/packages)按包 ID 搜索。

## 打包和发布

Azure DevOps 流水线以 Release 配置构建 `src/NcfPackageSources.sln`。启用 `GeneratePackageOnBuild` 的项目会生成 `.nupkg`，随后由流水线自动发现并推送到 NuGet.org。

新增可发布包时：

1. 在项目中启用 Release 自动打包。
2. 将所有待发布项目和必需的抽象项目加入 `src/NcfPackageSources.sln`。
3. 确保解决方案中已启用该项目的 Release 构建映射。
4. 以 Release 配置构建，并在发布前检查生成的 `.nupkg`。
5. 验证程序集、静态或嵌入资源、图标、包元数据、依赖项和 README 内容均已包含。

只添加项目引用不会为被引用项目生成 NuGet 包。如果依赖未加入解决方案，MSBuild 可能回退到其他配置并跳过仅在 Release 时执行的打包。

流水线会在推送前验证必需抽象包经 MSBuild 求值得到的包 ID、版本和输出路径。验证或构建失败会阻止发布。已经存在的版本通过 `--skip-duplicate` 跳过，因此修复缺失包后可以安全重新运行流水线。

## 贡献指南

欢迎参与贡献。

1. Fork 本仓库并创建聚焦于某项功能或修复的分支。
2. 将修改限制在相关的 `Senparc.Ncf.*` 或 `Senparc.Xncf.*` 模块内。
3. 遵循邻近代码已有的服务、仓储、实体、DTO 和注册模式。
4. 为新增或修复的行为添加或更新测试。
5. 优先构建最小受影响项目，再根据依赖关系验证解决方案或集成基线。
6. 仅在准备包发布时更新包发布说明和版本元数据。
7. 提交 Pull Request，并说明改动、已执行的验证以及迁移或兼容性影响。

仓库相关的自动化和维护说明见 [skills/README.md](skills/README.md)。请勿提交生成输出、本地数据库、密钥、API Key 或机器特定路径。

## 许可证

基于 Apache License 2.0 许可。详见 [LICENSE](LICENSE)。
