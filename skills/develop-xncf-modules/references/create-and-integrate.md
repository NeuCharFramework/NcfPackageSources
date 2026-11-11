# 从模板创建并接入 XNCF

## 模板和基础库都来自 NuGet

创建模块时，宿主是用户的 NCF 站点：一个 `Senparc.Web` 项目，加上已经通过 NuGet 还原的程序集。不要打开包源码仓库，也不要使用其中的目录作为模板或引用。

安装模板：

```bash
dotnet new install Senparc.Xncf.XncfBuilder.Template::<脚本给出的版本> --force
dotnet new list XNCF
dotnet new XNCF --help
```

`prepare_xncf.py` 把这条命令放在 JSON 的 `templateInstall` 里。版本来自 nuget.org 上该包的最新版本，除非用户指定 `--template-version`。帮助里没有的参数不要传。

基础库版本放在 JSON 的 `packages`：

- 宿主 csproj 里实际生效的 `PackageReference` 优先。
- 其次是宿主 `obj/project.assets.json` 里已还原的版本。
- 宿主没有的包，再查询 `https://api.nuget.org/v3-flatcontainer/<包名>/index.json` 的最后一个版本。

不要使用模板参数默认值 `0.28.1` 或 `0.23.9` 覆盖这些结果。

## 参数实际替换了什么

`template.json` 的短名是 `XNCF`。这些参数会替换源码中的占位符：

| 参数 | 写入位置 |
| --- | --- |
| `--OrgName`、`--XncfName` | 项目名、命名空间、文件名。`sourceName` 为 `Template_OrgName.Xncf.Template_XncfName` |
| `--Guid` | `Register.Uid`。使用大写 GUID |
| `--Version` | 只替换 `Register.Version` 的 `Template_Version` |
| `--MenuName`、`--Icon`、`--Description` | `Register` 的菜单、图标、描述，以及 csproj 的 Title、Description、Copyright、Authors |
| `--TargetFramework` | 仅当文件里仍有 `Template_TargetFramework`。模板里的目标框架经常写死，生成后要改成宿主的 `host.targetFramework` |
| `--Database`、`--Sample` | 替换 `XncfHasDatabase`、`XncfIsSample`，并决定条件编译 `XNCF_HAS_DATABASE` |
| `--XncfBaseVersion`、`--NcfAreaBaseVersion` | 脚本会传入与宿主一致的版本。当前模板可能不会把它们写进生效的 `PackageReference`，所以生成后仍要按下一节改 csproj |
| `--IntegrationToNcf` | 模板接受该参数，但不会替你添加宿主引用 |

`--Version` 不会改 csproj `<Version>`。生成结果可能带着模板包自己的版本和更新历史。新模块必须把 `<Version>` 改成与 `Register.Version` 相同的值，并把 `<PackageReleaseNotes>` 换成这个模块自己的创世说明。

## 开关会留下什么

| 开关 | 生成内容 | 生成后处理 |
| --- | --- | --- |
| `--Web true` | `Areas`、`Register.Area.cs`、静态资源嵌入 | 保留。模块与 `Senparc.Web` 同级时，模板里的 `LibraryPath` 可用 |
| `--Database true` | `Register.Database.cs`、Color 实体、迁移、`ColorAppService` | Color 是演示聚合。产品模型确定后替换它 |
| `--Sample true` | 数据库示例页面和静态文件 | 只有明确要保留演示时才打开 |
| `--Function true` | `MyFuctionAppService`、计算示例、EventBus 回环自检 | 产品功能另写 |
| `--UseWebApi true` | `ApiAppService` 示例 | 按真实用例替换 |

`--Database true` 且 `--Sample false` 时，示例页面可能被排除，但 `Register.Area.cs` 仍可能保留“数据列表示例”“数据库操作示例”菜单。删除指向不存在页面的菜单项。

Color 相关清理至少包括：

- `Domain/Models/DatabaseModel/Color.cs` 及其 DTO、映射
- `Domain/Services/ColorService.cs`
- `Application/AppServices/ColorAppService.cs`
- `Register.cs` 里的 Color 种子、DI 和 AutoMapper 配置
- `Register.UninstallAsync` 中删除全部表的演示。产品模块默认只调用传入的卸载委托
- 已生成的 Color 迁移。模块尚未发布时，在真实模型就绪后按宿主实际使用的数据库提供者重新生成

未发布模块可以删除示例迁移后重做。已发布模块只追加迁移。

## 生成后的项目引用

模板可能带有指向源码树的 `ProjectReference`，例如 `src/Basic` 或 `src/Extensions`。已发布站点里这些路径不存在。全部改成 `PackageReference`，版本使用 JSON 的 `packages`：

- `Senparc.Ncf.AreaBase`
- `Senparc.Xncf.AreasBase`
- 打开数据库时再加 `Senparc.Ncf.DatabasePlant`
- 设计时迁移再加 `Microsoft.EntityFrameworkCore.Design`，`PrivateAssets` 为 `all`

同时处理这些路径：

- 删除指向站点外部的 `OutputPath`，让 Release 包落在项目默认输出目录。
- 若打包图标的相对路径不存在，改为模板已经复制到项目里的 `icon.jpg`。
- `LibraryPath` 保持“从 `Senparc.Web/wwwroot` 上两级再进入模块目录”。模块必须放在 `Senparc.Web` 同级，这个相对路径才成立。

接入宿主，路径以 JSON 的 `host` 为准：

```bash
dotnet sln <host.solution> add <项目.csproj> --solution-folder XncfModules
dotnet add <host.webProject> reference <项目.csproj>
```

宿主通过项目引用加载带 `[XncfRegister]` 的程序集。只把项目放进目录、不引用 `Senparc.Web`，模块不会出现在站点里。

用户明确要求把模块加进 NCF 包源码仓库时，再另外把这些 `PackageReference` 换成该仓库里已有项目的 `ProjectReference`。默认不要这么做。

## 生成后搜索

```bash
rg -n "Template_|WrongGuid|00000000-0000-0000-0000-000000000000" <项目目录> -g '!**/bin/**' -g '!**/obj/**'
python3 skills/develop-xncf-modules/scripts/inspect_xncf.py <NCF站点根> --module <项目名>
```

`Register.Uid` 必须是本次新生成的大写 GUID。`DATABASE_PREFIX` 应为 `<Org>_<Name>_`，并且当前站点内唯一。

## 编译

```bash
dotnet restore <项目.csproj>
dotnet build <项目.csproj> --no-restore
dotnet build <host.webProject> --no-restore
```

引用变更后的第一次构建允许还原。不要并行启动多个会抢同一输出目录的构建。
