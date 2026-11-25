# 在已有 XNCF 中开发和维护

## 先看真实注册

每个模块由局部类 `Register` 组成：

- `Register.cs`：`[XncfRegister]`、`Name`、`Uid`、`Version`、菜单、描述、安装更新卸载、DI、嵌入静态文件。
- `Register.Area.cs`：首页、菜单、Razor 授权、运行时编译路径。
- `Register.Database.cs`：`DATABASE_PREFIX`、`DatabaseUniquePrefix`、`TryGetXncfDatabaseDbContextType`、模型注册。

服务继承 `ServiceBase<T>`，仓储继承 `RepositoryBase<T>`，领域异常使用 `NcfExceptionBase`。I/O 和外部调用使用异步方法。

## 垂直切片放在哪里

| 变更 | 位置 |
| --- | --- |
| 实体、值对象、不变量 | `Domain/Models` |
| 跨实体的领域行为 | `Domain/Services` |
| 用例编排 | `Application/AppServices` |
| 用例输入输出 | `Application/DTOs/Request`、`Application/DTOs/Response` |
| 集成事件 | `Application/Events`、`Application/EventHandlers` |
| 管理页面 | `Areas/Admin/Pages/<模块名>` |
| 必须手写的 HTTP 或 MCP 适配 | `OHS/Local` 或 `OHS/Remote` |
| 用户可见文案 | `Resources/<模块名>Resource*.resx` |
| 组合根 | 对应的 `Register*.cs`，不把流程写进 `Register` |

动态 WebApi 会把 AppService 方法暴露成接口。Function 使用 `[FunctionRender]`。产品模块跟随邻近模块，把名称和描述放进资源键，例如 `FunctionRender(typeof(<模块>Resource), "功能键", "说明键", typeof(Register))`。模板示例里的中文字面量只用于演示。

会修改数据、发外部请求或执行安装迁移的 Function 保持默认，不设置 `AllowAiInvocation = true`。

授权写在服务端入口。菜单可见不是授权。`AuthorizeConfig` 里配置页面约定，页面本身再检查当前用户是否允许访问。

## 数据库迁移

只为宿主已经引用的数据库提供者生成迁移：

| 目录与 `-c` 后缀 | 设计时类 | NuGet 包 |
| --- | --- | --- |
| `Sqlite` | `<模块>SenparcEntities_Sqlite` | `Senparc.Ncf.Database.Sqlite` |
| `SqlServer` | `<模块>SenparcEntities_SqlServer` | `Senparc.Ncf.Database.SqlServer` |
| `MySql` | `<模块>SenparcEntities_MySql` | `Senparc.Ncf.Database.MySql` |
| `PostgreSQL` | `<模块>SenparcEntities_PostgreSQL` | `Senparc.Ncf.Database.PostgreSQL` |
| `Oracle` | `<模块>SenparcEntities_Oracle` | `Senparc.Ncf.Database.Oracle` |
| `Dm` | `<模块>SenparcEntities_Dm` | `Senparc.Ncf.Database.Dm` |

文件名可能写成 `*_SQLite.cs`，类名和 `-c` 参数仍是 `_Sqlite`，输出目录是 `Domain/Migrations/Sqlite`。不要把迁移生成到注释里旧的 `Domain/Migrations/Migrations.Sqlite`。

模块 csproj 需要设计时包：

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

启动项目是站点的 `Senparc.Web.csproj`。已发布站点没有独立的数据库停机坪源码项目，数据库提供程序已经随宿主的 NuGet 包还原。

```bash
dotnet ef migrations add <变更名> \
  -c <模块>SenparcEntities_Sqlite \
  -p <模块.csproj> \
  -s <宿主 Senparc.Web.csproj> \
  -o Domain/Migrations/Sqlite
```

把 `Sqlite` 换成宿主实际引用的其他提供者。每个被引用的提供者都生成成功后，才认为模型变更完成。生成后的 migration、Designer 和 `*ModelSnapshot.cs` 都留在 `Domain/Migrations/<提供者>/`。不要手改快照来补一个提供者。

迁移在模块安装和更新时由 `MigrateOnInstallAsync` 执行。本地 `dotnet ef database update` 只作为开发验证，不代替安装路径。

## 本地化

所有最终展示给用户的文案都必须通过模块资源管理，包括：

- 页面文本、标题、按钮、标签、占位符、提示、确认框、通知和错误消息；
- 菜单、模块名称、模块说明、首页入口和权限展示名；
- Function、WebApi、DTO 元数据中供用户阅读的名称与说明；
- JavaScript 动态生成的界面文案和客户端提示。

不要把用户文案直接写入 Razor、C#、JavaScript、HTML 属性、特性参数或日志。开发日志、机器协议值、枚举/模型/路由名称、数据库持久化值、URL、品牌标识和用户输入不属于翻译对象。

模块资源类是 `Resources/<模块名>Resource.cs`，通过 `ResourceStringLocalizer` 读取。新增用户可见文案时，同步修改：

- `<模块名>Resource.resx`
- `<模块名>Resource.zh-CN.resx`
- `<模块名>Resource.en.resx`
- `<模块名>Resource.es.resx`
- `<模块名>Resource.fr.resx`
- `<模块名>Resource.ja.resx`
- `<模块名>Resource.ru.resx`

中文资源是基准：无文化后缀的资源和 `zh-CN` 资源使用中文；`en`、`es`、`fr`、`ja`、`ru` 文件必须提供对应语言翻译。七个资源文件必须包含相同的 `data name` 集合，禁止只在某一种语言中新增键、用另一种语言临时占位或删除键。

使用约定：

- Razor 通过 `IStringLocalizer<<模块名>Resource>` 或现有模块局部符号读取资源。
- C# 首选 DI 注入的强类型 localizer；没有注入入口的代码按模块现有方式使用 `ResourceStringLocalizer`。
- Function 名称和说明使用 `FunctionRender` 的资源类型与键名，而不是中文字面量。
- 前端脚本通过模块自己的 `_xxxLocalizationScripts.cshtml` 和 `Frontend.*` 资源键输出字典；共用 partial 名称可能造成 Razor 编译冲突，模块 partial 必须保持唯一。
- 保留资源值中的复合格式占位符（如 `{0}`、`{1:F2}`）、转义内容和参数分隔符 `||`。翻译时只能调整周边自然语言，不能改变占位符顺序、格式说明或参数协议。
- 需要新增一种系统支持语言时，才同时调整 `NcfLocalizationOptions.SupportedCultures`。普通模块文案变更只同步已有七个资源文件。

实现后至少检查：

1. 代码中不再有新增的用户可见硬编码文案。
2. 七个资源文件的键集合完全一致，新增和删除操作同时覆盖全部语言。
3. 中文和至少一种非中文文化的读取结果正确，带参数的资源格式化结果包含相同参数。
4. 页面或模块安装行为变化时，在宿主中验证 Razor 编译、菜单/Function 元数据和运行时切换语言。

## 测试与审查

测试项目放在模块旁边，命名 `<模块>.Tests`，参考 `Senparc.Xncf.XncfBuilder.Tests`。测试项目不写发布版本号。

提交前运行：

```bash
python3 skills/develop-xncf-modules/scripts/inspect_xncf.py <仓库根> --module <模块名> --layers
dotnet test <模块>.Tests/<模块>.Tests.csproj --no-restore
dotnet build <模块.csproj> --no-restore
```

`--layers` 把 Domain 引用 Application、OHS、Areas，以及 Application 引用 OHS、Areas 标成审查项。确认是源码里的 using，而不是生成文件后，再调整依赖方向。

宿主需要一起验证时，编译 JSON 计划或站点里的 `Senparc.Web.csproj`。界面或安装行为有变化时，在该站点里安装或更新模块后再看页面、Function 和迁移结果。
