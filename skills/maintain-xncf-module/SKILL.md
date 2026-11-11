---
name: maintain-xncf-module
description: Add and maintain NeuCharFramework XNCF use cases, entities, EF migrations, admin pages, Functions, WebApi, localization, and tests while keeping the module UID and database prefix stable. Use when extending or fixing an existing XNCF.
---

# 维护 XNCF

在已有模块里按垂直切片开发。层职责、依赖方向和跨模块契约以 [architecture.md](../develop-xncf-modules/references/architecture.md) 为准。命令、迁移目录和本地化清单见 [develop-and-maintain.md](../develop-xncf-modules/references/develop-and-maintain.md)。

## 动代码前

1. 读该模块的 `Register.cs`、`Register.Area.cs`、`Register.Database.cs` 和 csproj。目录名不能代替这些文件。
2. 确认要改的聚合属于这个模块。需要跨模块数据时，加契约或集成事件，不引用对方实现。
3. 已发布模块禁止修改 `Uid` 和 `DATABASE_PREFIX`。
4. 保留用户已有且与本次任务无关的改动。

## 一个用例的顺序

1. 在 Domain 写实体、不变量和领域服务。实体继承 `EntityBase<TKey>`，表名使用 `[Table(Register.DATABASE_PREFIX + nameof(实体))]`。
2. 在 `Application/AppServices` 编排用例，输入输出放在 `Application/DTOs`。AppService 继承 `AppServiceBase`，返回 `GetResponseAsync` 或 `GetStringResponseAsync`。
3. 有数据库变更时，为宿主已经引用的 `Senparc.Ncf.Database.*` 提供者生成迁移。`dotnet ef` 的启动项目是该站点的 `Senparc.Web.csproj`。
4. 页面、Function、动态 WebApi 只做适配。Areas 不直接访问仓储或 `DbContext`。只有必须手写 HTTP 形状时才在 `OHS/Local` 或 `OHS/Remote` 加 Controller。
5. 在 `AddXncfModule` 注册服务。菜单、权限和首页放在 `Register.Area.cs`。用户可见文案进入模块资源，不写死在页面或特性里。
6. 为不变量和用例补最低层的测试。注册、路由、迁移、授权或 EventBus 有变化时再补宿主侧验证。

## 必须守住的行为

- 安装和更新通过 `XncfDatabaseDbContext.MigrateOnInstallAsync` 迁移，并保持幂等。
- 卸载默认只调用传入的卸载委托。模板里“卸载即删除本模块全部表”是演示，产品模块没有明确决定时不要保留。
- 集成事件在本地状态提交成功后发布。需要不丢消息时再设计 outbox，不要把进程内 EventBus 当成持久队列。
- 给浏览器推送进度用现有 SSE 或流式适配器，不要把它写成领域事件。
- 自动生成的 `Domain/Migrations/<数据库>/` 文件不要加文件头注释，下次生成会覆盖。功能变化记在模块更新说明里。

## 验证

先编译被改的模块和它的测试项目，再编译宿主。数据库形状变了就检查六个迁移目录都有同一次变更。界面变了就检查资源键在默认、`zh-CN`、`en`、`es`、`fr`、`ja`、`ru` 资源里都存在。

```bash
python3 skills/develop-xncf-modules/scripts/inspect_xncf.py <NCF站点根> --module <项目名> --layers
dotnet test <测试项目.csproj> --no-restore
dotnet build <宿主 Senparc.Web.csproj> --no-restore
```

静态检查、编译、测试、迁移和登录后的页面行为要分开报告。不要用编译成功代替迁移已执行。
