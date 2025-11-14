# NCF Desktop App 文件保护机制

## 📋 概述

本文档描述了 NCF Desktop App 客户端更新程序的文件保护机制，确保在更新过程中重要的用户数据和配置文件不会丢失。

## 🛡️ 保护机制

### 保护的文件和文件夹

1. **App_Data 文件夹**
   - 完整保护整个 `App_Data` 文件夹及其所有内容
   - 包括数据库文件、用户配置、缓存等重要数据

2. **appsettings*.json 文件**
   - 保护所有以 `appsettings` 开头、以 `.json` 结尾的配置文件
   - 包括 `appsettings.json`、`appsettings.Development.json` 等
   - 支持子目录中的配置文件

### 备份策略

1. **临时备份**
   - 更新前将重要文件备份到 `backup` 文件夹（与 `runtime` 并列）
   - 更新完成后自动恢复文件
   - 临时备份在恢复后自动清理

2. **永久备份**
   - 为每个 `appsettings*.json` 文件创建带时间戳的备份
   - 格式：`appsettings.json.20241211_143025.bak`
   - 保存在 `backup` 文件夹中，不会自动删除

## 🔄 更新流程

### 原始流程（有问题）
```
1. 完全删除 runtime 文件夹 ❌
2. 重新创建 runtime 文件夹
3. 解压新版本文件
```

### 新的安全流程 ✅
```
1. 保护重要文件（PreserveImportantFilesAsync）
   - 备份 App_Data 文件夹到临时位置
   - 备份所有 appsettings*.json 文件
   - 创建带时间戳的永久备份

2. 安全清理（SafeCleanRuntimeDirectoryAsync）
   - 删除非重要文件
   - 保留 App_Data 文件夹
   - 保留 appsettings*.json 文件

3. 解压新版本文件（ExtractZipWithCorrectPathsAsync）
   - 正常解压新版本到 runtime 文件夹

4. 恢复重要文件（RestoreImportantFilesAsync）
   - 恢复 App_Data 文件夹
   - 恢复 appsettings*.json 文件
   - 清理临时备份
```

## 📁 文件夹结构

```
AppData/
├── Runtime/                    # NCF 运行时文件夹
│   ├── App_Data/              # 🛡️ 用户数据（受保护）
│   │   ├── Database/
│   │   ├── Logs/
│   │   └── UserFiles/
│   ├── appsettings.json       # 🛡️ 主配置文件（受保护）
│   ├── appsettings.Development.json  # 🛡️ 开发配置（受保护）
│   ├── Senparc.Web.dll       # 🔄 应用程序文件（会更新）
│   └── ...                    # 🔄 其他程序文件（会更新）
├── backup/                    # 备份文件夹
│   ├── appsettings.json.20241211_143025.bak
│   ├── appsettings.Development.json.20241211_143025.bak
│   └── ...
└── Downloads/                 # 下载文件夹
    └── NCF-v1.2.3-win-x64.zip
```

## 🔧 实现细节

### 核心方法

1. **PreserveImportantFilesAsync()**
   - 创建备份目录
   - 复制 App_Data 文件夹到临时位置
   - 备份所有 appsettings*.json 文件

2. **SafeCleanRuntimeDirectoryAsync()**
   - 遍历所有文件和文件夹
   - 使用 `ShouldPreserveFile()` 和 `ShouldPreserveDirectory()` 判断是否保留
   - 只删除非重要文件

3. **RestoreImportantFilesAsync()**
   - 从临时位置恢复 App_Data 文件夹
   - 恢复 appsettings*.json 文件
   - 清理临时备份

### 判断逻辑

```csharp
// 文件保护判断
private bool ShouldPreserveFile(string filePath)
{
    var relativePath = Path.GetRelativePath(NcfRuntimePath, filePath);
    var fileName = Path.GetFileName(filePath);
    
    // 保留 App_Data 文件夹中的所有文件
    if (relativePath.StartsWith("App_Data", StringComparison.OrdinalIgnoreCase))
        return true;
    
    // 保留 appsettings*.json 文件
    if (fileName.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) && 
        fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        return true;
    
    return false;
}

// 目录保护判断
private bool ShouldPreserveDirectory(string directoryPath)
{
    var relativePath = Path.GetRelativePath(NcfRuntimePath, directoryPath);
    
    // 保留 App_Data 文件夹
    if (relativePath.Equals("App_Data", StringComparison.OrdinalIgnoreCase) ||
        relativePath.StartsWith("App_Data" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        return true;
    
    return false;
}
```

## 🚨 错误处理

- 所有文件操作都包含异常处理
- 备份失败不会阻止更新继续进行
- 恢复失败会记录警告日志但不会中断程序
- 临时备份清理失败不会影响应用程序运行

## 📝 日志记录

更新过程中会记录详细的日志信息：

```
🛡️ 开始保护重要文件...
✅ App_Data 文件夹已备份
✅ 已备份配置文件: appsettings.json
✅ 重要文件保护完成
🧹 开始安全清理 Runtime 目录...
✅ Runtime 目录安全清理完成
🔄 开始恢复重要文件...
✅ App_Data 文件夹已恢复
✅ 已恢复配置文件: appsettings.json
🧹 临时备份已清理
✅ 重要文件恢复完成
```

## 🧪 测试建议

1. **创建测试数据**
   - 在 App_Data 文件夹中放置测试文件
   - 修改 appsettings.json 添加自定义配置

2. **执行更新测试**
   - 运行客户端更新程序
   - 验证 App_Data 文件夹内容完整
   - 验证 appsettings.json 配置保持不变

3. **检查备份文件**
   - 确认 backup 文件夹中存在带时间戳的备份文件
   - 验证备份文件内容正确

## 🔒 安全性

- 备份文件存储在用户数据目录，具有适当的访问权限
- 临时备份在恢复后立即清理，减少敏感数据暴露
- 永久备份文件可用于手动恢复配置

## 📞 故障排除

如果更新后发现配置丢失：

1. 检查 `backup` 文件夹中的时间戳备份文件
2. 手动复制备份文件到 runtime 文件夹
3. 重命名备份文件去掉时间戳后缀
4. 重启应用程序

---

**注意**: 此机制确保用户数据的安全性，但建议用户定期备份重要数据到外部位置。

