# 发布 XNCF 与模板包

## 模块包

官方模块 csproj 使用 `Microsoft.NET.Sdk.Razor`，`Release` 时 `GeneratePackageOnBuild` 为 true。包身份来自项目文件：

- `<Version>` 是 NuGet 版本。
- `<AssemblyName>` 通常就是包名，例如 `Senparc.Xncf.KnowledgeBase`。
- `<Description>`、`<Title>`、`<Authors>`、`<PackageTags>` 来自模板替换，发布前读一遍，去掉模板残留。
- `<PackageReleaseNotes>` 按时间顺序列出这个模块的变化。格式遵循 `codex-skills/senparc-version-updater`。

`Register.Version` 是 NCF 安装器看到的模块版本，必须与 `<Version>` 相同。只改其中一个会让管理界面和 NuGet 包对不上。

已发布模块的版本只能增加。同一条尚未合并到 `master` 的开发线里，版本更新技能允许只合并更新说明、不再次抬版本。新模块第一次发布使用 `0.1.0`，除非用户指定别的版本。

文件头注释也由版本更新技能处理。这些文件不加头注释：

- `*.Generated.cs`
- 测试项目
- `Domain/Migrations/<数据库>/` 下的迁移、Designer 和 ModelSnapshot

迁移仍然算模块的功能变更，写进该模块的 `<PackageReleaseNotes>`。

## 打包命令

```bash
dotnet pack <模块.csproj> -c Release
```

打包后解开 nupkg，确认有程序集、`wwwroot` 嵌入资源、图标和 nuspec 中的版本。`dotnet build` 成功只说明源码能编译。

流水线只在模块位于 NCF 包源码仓库并且合并到 `master` 时才会自动推送。普通站点里开发的模块不会走那条流水线。用户没有明确要求时，两种情况都不在本地执行 `dotnet nuget push`。

## 模板包

`Senparc.Xncf.XncfBuilder.Template` 是模板包，不是运行模块。它的 NuGet 版本看 `<PackageVersion>`，当前与 `<Version>` 不是同一个数。发布模板时增加 `<PackageVersion>`，不要只改 `<Version>`。

模板发布的完整顺序写在 [template-maintenance.md](template-maintenance.md)。母版能编译、模板项目能 pack，都还不能说明 `dotnet new XNCF` 生成的项目能编译。
