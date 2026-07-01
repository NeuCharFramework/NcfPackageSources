# 版本号与发布说明策略

## 版本号决策

按项目改动强度选择升级级别：

1. `major`：破坏性变更（兼容性中断、公共接口删除或行为不兼容）。
2. `minor`：新增非破坏能力（新增特性、扩展公共接口、增强功能）。
3. `patch`：修复与维护（bugfix、重构、测试、文档、被动依赖升级）。

## 提交范围基线（master/main）

版本判断与变更汇总必须基于 `master/main` 对比窗口：

1. 基线分支优先级：`origin/master` -> `origin/main` -> `master` -> `main`。
2. 使用 `merge-base(HEAD, 基线分支)` 作为起点提交（`comparison_base`）。
3. 本次更新内容 = `comparison_base..HEAD` 的全部提交 + 当前未提交改动。
4. 如果无法解析 `master/main` 或无法计算 merge-base，流程必须报错终止，不允许降级到“最近一次修改 .csproj”的窗口。

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
