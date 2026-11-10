# 维护 XNCF 模板

只在用户明确要求修改 NuGet 模板包 `Senparc.Xncf.XncfBuilder.Template` 的源码时使用本文件。创建或维护业务模块时不要打开这些目录；模板通过 `dotnet new install Senparc.Xncf.XncfBuilder.Template` 获取。

## 权威源

编辑包源码仓库中的：

`tools/NcfSimulatedSite/Template_OrgName.Xncf.Template_XncfName`

不要只编辑：

`src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.Template/templates/template1`

同步工具会用母版覆盖副本。`tools/NcfSimulatedSiteSyncTool` 在 `--template-only` 时只做这次模板同步，不会把模拟站点同步到外部 NCF 目录。

占位符必须保留给 `dotnet new` 替换，包括 `Template_OrgName`、`Template_XncfName`、`Template_Guid`、`Template_Version`、`Template_MenuName`、`Template_Icon`、`Template_Description`、`Template_Database`、`Template_Sample`。不要把母版里的这些标记替换成某个真实模块的值。

## 改模板时一起改的地方

- `.template.config/template.json` 的参数、默认值和 `sources.modifiers` 排除规则。
- 条件编译 `XNCF_HAS_DATABASE` 与 csproj 里的 `XncfHasDatabase`、`XncfIsSample`。
- 被排除的页面在 `Register.Area.cs` 里的菜单项。排除文件后不能留下死菜单。
- 资源文件的全部语言。
- 模板项目的 `<PackageVersion>` 和 `<PackageReleaseNotes>`。

当前母版的 csproj 目标框架直接写 `net10.0`，`--TargetFramework` 不会改变它。基础库 `PackageReference` 被注释掉了，`--XncfBaseVersion` 和 `--NcfAreaBaseVersion` 也不会改引用。如果要让这两个参数重新生效，把占位符放回实际生效的引用，并在生成结果里验证。

## 同步、打包、试生成

1. 编译母版项目。
2. 运行同步工具的 `--template-only`。
3. 对比母版和 `templates/template1`，确认占位符还在，`bin`、`obj` 没有被打进去。
4. 打包模板项目：

```bash
dotnet pack src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.Template/Senparc.Xncf.XncfBuilder.Template.csproj -c Release
```

模板项目设置了 `IncludeBuildOutput` 为 false、`IncludeSymbols` 为 false。空的符号包会在 SDK 10 上触发 NU5017，不要把符号包打开。

5. 在临时目录安装刚打出的 nupkg，不要用机器上已经缓存的旧模板做验证。
6. 至少生成两组：只打开 `--Web`，以及同时打开 `--Web --Database --Function --UseWebApi`。两组都要能编译。
7. 检查数据库组没有残留 `Template_`，并且没有把模板的 `<Version>1.1.x` 误当成业务模块版本的唯一来源。业务模块的版本归一化属于创建技能，不在模板里写死成 `0.1.0`，否则母版自身无法作为示例项目编译出稳定版本。

母版编译通过，只证明母版能编译。包能装上并且生成项目能编译，才证明这次模板改动有效。
