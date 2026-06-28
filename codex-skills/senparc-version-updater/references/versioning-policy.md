# 版本号与发布说明策略

## 版本号决策

按项目改动强度选择升级级别：

1. `major`：破坏性变更（兼容性中断、公共接口删除或行为不兼容）。
2. `minor`：新增非破坏能力（新增特性、扩展公共接口、增强功能）。
3. `patch`：修复与维护（bugfix、重构、测试、文档、被动依赖升级）。

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
3. 若被动项目再次被其他项目引用，继续递归升级，直到链路结束。
4. 使用 visited 集合避免循环引用导致死循环。
