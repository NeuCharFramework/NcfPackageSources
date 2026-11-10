---
name: create-xncf-module
description: Scaffold a NeuCharFramework XNCF with dotnet new XNCF from the Senparc.Xncf.XncfBuilder.Template NuGet package, generate its GUID and database prefix, align package references with the host, and add it beside Senparc.Web. Use when the user asks to create, initialize, or integrate an XNCF module, including on a published NCF site that has only Senparc.Web and NuGet assemblies.
---

# 创建并接入 XNCF

在包含 `Senparc.Web` 的 NCF 站点里用 NuGet 上的 `Senparc.Xncf.XncfBuilder.Template` 生成项目。不要到源码仓库的 `tools/` 或 `src/` 目录找模板和基础库。命名、GUID、数据库前缀和包版本由脚本生成。生成后的清理见 [create-and-integrate.md](../develop-xncf-modules/references/create-and-integrate.md)。

用户只要求方案时，不要加 `--execute` 或 `--install-template`。

## 1. 确认宿主

`--root` 是 NCF 站点根目录，而不是包源码仓库。站点里要能找到 `Senparc.Web/Senparc.Web.csproj`。已发布的站点只有这个 Web 项目和 NuGet 程序集，这是正常情况。

确定：

- `OrgName`、`XncfName`：匹配 `[A-Za-z][A-Za-z0-9]*`。项目名是 `<OrgName>.Xncf.<XncfName>`。
- 一句话描述、菜单名、图标。
- 能力开关：`--web`、`--database`、`--function`、`--webapi`。默认不要 `--sample`。
- 起始版本默认 `0.1.0`。
- 新项目放在 `Senparc.Web` 的同级目录。脚本会按找到的宿主决定路径。

多个模块时，先用 [develop-xncf-modules](../develop-xncf-modules/SKILL.md) 做边界判断。

## 2. 生成计划

```bash
python3 skills/develop-xncf-modules/scripts/prepare_xncf.py \
  --root <NCF站点根> \
  --org Senparc \
  --name Ordering \
  --description "订单生命周期" \
  --web --database
```

标准输出是一份 JSON。用其中的 `packages`、`templateInstall` 和 `command`，不要自己写一套路径。

- `packages` 优先取宿主 `Senparc.Web` 已引用或已还原的 Senparc 包版本，宿主没有的包再向 nuget.org 查询最新版本。
- `templateInstall` 安装 `Senparc.Xncf.XncfBuilder.Template`。不要用本地源码目录代替这个包。
- 出现 `collisions` 时停止。不要使用全 0 GUID 或 `WrongGuid`。
- 找不到 `Senparc.Web.csproj` 时，脚本要求显式传入 `--output`。先确认 `--root` 是站点根目录。

## 3. 安装模板并生成

用户确认可以创建后：

```bash
python3 skills/develop-xncf-modules/scripts/prepare_xncf.py \
  --root <NCF站点根> \
  --org Senparc \
  --name Ordering \
  --description "订单生命周期" \
  --web --database \
  --execute
```

`--execute` 在本机还没有 `XNCF` 模板时，会先执行 `templateInstall`。这会更新当前机器的 `dotnet new` 缓存，并从 NuGet 下载模板包。

生成目录必须是新目录。

## 4. 生成后立刻核对

```bash
rg -n "Template_|WrongGuid" <新项目目录> -g '!**/bin/**' -g '!**/obj/**'
```

然后按 [create-and-integrate.md](../develop-xncf-modules/references/create-and-integrate.md)：

1. csproj `<Version>` 改成与 `Register.Version` 相同，并换成这个模块自己的创世说明。
2. 打开数据库时替换 Color 示例，包括种子数据、死菜单和卸载删表演示。
3. 把指向站点外部的 `ProjectReference` 改成 JSON 里 `packages` 的 `PackageReference`。删掉不存在的 `OutputPath` 和图标路径。
4. 把项目加入宿主解决方案的 `XncfModules`，并 `dotnet add` 到 `host.webProject`。

## 5. 验证

```bash
dotnet restore <新模块.csproj>
dotnet build <新模块.csproj> --no-restore
dotnet build <host.webProject> --no-restore
```

报告要分开写：模板包安装、项目生成、包引用修正、模块编译、宿主编译。生成成功不等于模块已在站点里安装，也不等于迁移已执行。
