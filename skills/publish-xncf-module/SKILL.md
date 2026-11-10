---
name: publish-xncf-module
description: Align an XNCF Register version with its NuGet package version, changelog headers, and PackageReleaseNotes, then pack a Release nupkg. Use when the user asks to version, pack, or publish an XNCF. Do not run dotnet nuget push unless the user explicitly asks.
---

# 发布 XNCF

版本号、C# 文件头和 `<PackageReleaseNotes>` 的格式以 [senparc-version-updater](../../codex-skills/senparc-version-updater/SKILL.md) 为准。当前工作区里有那个技能时，先跑它的扫描脚本，再改版本。没有那个技能时，仍然让 `Register.Version` 与 csproj `<Version>` 相同，并追加同样格式的更新说明。细节见 [publish.md](../develop-xncf-modules/references/publish.md)。

## 发布前核对

- `Register.Version` 与 csproj `<Version>` 是同一个版本。
- 新模块的更新说明从创世条目开始，不保留模板项目的 `v1.1.x` 历史。
- `Uid` 和 `DATABASE_PREFIX` 与上一版已发布包一致。
- 模块 csproj 在 `Release` 下可以打包。不要留下指向站点外部、实际不存在的 `OutputPath`。
- 只有模块位于 NCF 包源码仓库时，合并到 `master` 才可能被仓库流水线推送。普通站点里的模块不会自动进入那条流水线。

## 打包

用户要求打包时：

```bash
dotnet pack <模块.csproj> -c Release
```

检查生成的 nupkg 内含预期的程序集、资源和 `icon`。模板包是另一个项目，按 [template-maintenance.md](../develop-xncf-modules/references/template-maintenance.md) 处理，不要把模块包和模板包的版本混用。

## 推送

本地 `dotnet nuget push` 只在用户明确要求时执行。没有明确要求时，打包和版本更新就是交付物。

不要用源码编译成功代替包内容正确，也不要用包已生成代替 nuget.org 已接受。
