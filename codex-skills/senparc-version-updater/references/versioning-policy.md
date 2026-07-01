# 版本号与发布说明策略

## 版本号决策

按项目改动强度选择升级级别：

1. `major`：破坏性变更（兼容性中断、公共接口删除或行为不兼容）。
2. `minor`：新增非破坏能力（新增特性、扩展公共接口、增强功能）。
3. `patch`：修复与维护（bugfix、重构、测试、文档、被动依赖升级）。
4. 版本号必须保持单调不下降：更新后的 `<Version>` 不得小于更新前版本。
5. 在同一个未合并窗口内，允许版本号保持不变（用于合并追加发布说明），但禁止回退到更低版本。

## 提交范围基线（master/main）

版本判断与变更汇总必须基于 `master/main` 对比窗口：

1. 基线分支优先级：`origin/master` -> `origin/main` -> `master` -> `main`。
2. 使用 `merge-base(HEAD, 基线分支)` 作为起点提交（`comparison_base`）。
3. 本次更新内容 = `comparison_base..HEAD` 的全部提交 + 当前未提交改动。
3.1 变更文件统计范围必须为“全仓库”，不能只限制在单个目标项目目录。
4. 如果无法解析 `master/main` 或无法计算 merge-base，流程必须报错终止，不允许降级到“最近一次修改 .csproj”的窗口。

## 未合并分支的版本升级规则

对于尚未合并到 `master/main` 的提交窗口（同一个 `comparison_base..HEAD`）：

1. 版本号最多升级一次。
2. 后续新增提交不再重复升级版本号。
3. 后续新增提交的内容应合并追加到同一个版本号下的 `<PackageReleaseNotes>` 中。
4. 仅当合并到 `master/main` 后进入新的比较窗口时，才再次按 `major/minor/patch` 进行下一次版本升级。

## 受影响项目覆盖规则

1. 需要根据 `changed_cs_files` 反向映射出对应项目集合（`changed_csprojs`）。
2. `changed_csprojs` 中的每个项目都必须更新自身 `.csproj`（版本与发布说明）。
3. 不允许只更新单一主项目并忽略其他已变更代码所在项目。

## 预览版处理

当当前版本包含 `-preview.N` 后缀时：

1. 先按 `major/minor/patch` 计算基础版本号。
2. 保持 `preview` 通道，并将 `N` 递增（无数字时从 `1` 开始）。

示例：

1. `1.4.2-preview.3` + `patch` -> `1.4.3-preview.4`
2. `1.4.2-preview.3` + `minor` -> `1.5.0-preview.4`
3. `1.4.2` + `patch` -> `1.4.3`

## PackageReleaseNotes 追加格式

追加到 `<PackageReleaseNotes>` 节点末尾，保持缩进一致：

```xml
[YYYY-MM-DD] vX.Y.Z <一行总结>
1、<变更点一>
2、<变更点二>
```

内容要求：

1. 必须描述当前项目的功能性变更（功能新增、行为调整、兼容性适配、缺陷修复、交互优化、依赖升级影响等）。
2. 禁止写流程追踪文本或窗口标识文本（如 `Repository baseline sync`、`sync window`、`comparison_base..HEAD` 等）。
3. 同一未合并窗口内允许合并多次提交内容，但描述仍需保持“项目功能变化”视角，而非“流程同步”视角。
4. `<PackageReleaseNotes>` 标签必须位于 `<Version>` 标签之后。
5. `<PackageReleaseNotes>` 不要求紧邻 `<Version>`；若当前已在其后（包括人为调整位置），无需改动位置。
6. 若 `<PackageReleaseNotes>` 在 `<Version>` 之前，必须调整到 `<Version>` 之后并保持内容与缩进。

对于被动升级项目，建议使用：

```xml
[YYYY-MM-DD] vX.Y.Z Dependency update from <ProjectName> to <SourceVersion>
```

## 递归依赖升级规则

1. 主项目按实际改动升级（major/minor/patch）。
2. 被动依赖项目默认执行 `patch` 升级。
3. 被动依赖项目若缺失 `<Version>`，需先补齐 `<Version>` 后再追加发布说明。
4. 若被动项目再次被其他项目引用，继续递归升级，直到链路结束。
5. 使用 visited 集合避免循环引用导致死循环。
