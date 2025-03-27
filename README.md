<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />

# NcfPackageSources

[![Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_apis/build/status/NeuCharFramework.NcfPackageSources?branchName=master)](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)

## 目录 / Table of Contents
- [项目介绍 / Introduction](#项目介绍--introduction)
- [开发环境 / Development Environment](#开发环境--development-environment)
- [快速开始 / Quick Start](#快速开始--quick-start)
- [项目结构 / Project Structure](#项目结构--project-structure)
- [模块加载顺序 / Module Loading Order](#模块加载顺序--module-loading-order)
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

💡 在线演示 / Online Demo：[https://www.ncf.pub](https://www.ncf.pub)

📚 官方文档 / Official Docs：[https://doc.ncf.pub](https://doc.ncf.pub)

💬 技术交流群 / Tech QQ Group：<img src="https://sdk.weixin.senparc.com/images/QQ_Group_Avatar/NCF/QQ-Group.jpg" width="380" />
