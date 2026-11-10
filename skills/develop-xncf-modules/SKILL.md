---
name: develop-xncf-modules
description: Route NeuCharFramework XNCF work across creation, integration, feature development, maintenance, bounded-context splits, template upkeep, and release. Use when the user mentions XNCF, NCF module, XncfBuilder, dotnet new XNCF, module install, or publishing an XNCF. Hand the concrete procedure to create-xncf-module, maintain-xncf-module, or publish-xncf-module.
---

# 开发 XNCF 模块

先判断任务属于哪一条链路，再读取对应技能。不要手抄一个旧模块来代替模板生成。

| 任务 | 技能 |
| --- | --- |
| 新建模块、从模板生成、接入解决方案或宿主 | [create-xncf-module](../create-xncf-module/SKILL.md) |
| 给已有模块加用例、实体、页面、Function、WebApi、迁移、测试 | [maintain-xncf-module](../maintain-xncf-module/SKILL.md) |
| 对齐版本、更新日志、打包、发布 | [publish-xncf-module](../publish-xncf-module/SKILL.md) |
| 修改 XNCF 模板本身 | [references/template-maintenance.md](references/template-maintenance.md) |
| 只审查边界、依赖、拆分，不改代码 | 本文件的判断规则，加上下面两份参考 |

设计判断读 [references/architecture.md](references/architecture.md)。拆分、合并、上下文地图读 [references/bounded-context-splitting.md](references/bounded-context-splitting.md)。验证证据的边界读 [references/creation-and-validation.md](references/creation-and-validation.md)。

## 先定边界，再定项目

一个 XNCF 对应一组共享语言、不变量、数据所有者和发布节奏的能力。页面、表、控制器、目录大小都不是拆分理由。一个聚合的写事务只属于一个 XNCF。

默认做一个模块。只有在语言冲突、数据所有者不同、可独立安装或发布、安全或可用性边界不同、或者依赖可以收成稳定契约时，才拆成多个。拆分前先给出上下文地图，并等用户确认。

模块内部依赖方向：

```text
Areas / OHS / 适配器 -> Application -> Domain
Register 只做组合，不写用例
```

跨模块只共享 `.Abstractions` 里的 ID、快照、命令结果和集成事件。不要引用另一个模块的实体、仓储、映射或 `DbContext`。EventBus 是进程内通信，不是安全边界，也不是持久投递。

## 身份一旦发布就固定

- `Register.Uid` 是大写 GUID。已发布模块的 UID 冲突时，改新模块，不改旧模块。
- `DatabaseUniquePrefix` 发布后不改。表名使用 `Register.DATABASE_PREFIX + nameof(实体)`。
- `Register.Version` 与 csproj `<Version>` 使用同一个语义化版本。
- 不要把模板自带的 `1.1.x` 更新历史留在新模块里。

## 运行位置

技能运行在用户正在开发的 NCF 站点上。这个站点可能只有 `Senparc.Web` 项目和 NuGet 上的程序集，没有基础库源码。

- 模板只安装 NuGet 包 `Senparc.Xncf.XncfBuilder.Template`。
- 基础库版本从宿主已引用的包读取；宿主没有的包再查询 nuget.org。
- 新模块放在 `Senparc.Web` 同级，并用 `PackageReference` 引用这些包。
- 生成前先跑 `skills/develop-xncf-modules/scripts/prepare_xncf.py`，`--root` 传站点根目录。用脚本输出的命令和版本，不要写死源码仓库路径。

只有用户明确要求修改模板包的源码时，才阅读 [references/template-maintenance.md](references/template-maintenance.md)。那份文档里的目录不属于普通创建流程。

用户只要方案时，给出方案后停止，不安装模板、不生成项目、不改解决方案。
