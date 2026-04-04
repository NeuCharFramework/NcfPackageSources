# Version update detection and data protection function description

## 📋 Function Overview

NCF Desktop App now supports intelligent version update detection and a complete data protection mechanism to ensure that users will not lose any data during the update process.

---

## 🆕 New features

### 1. Version update detection

The application will automatically detect when it starts:
- 💾 **Currently installed version**
- 🌐 **Latest version available**
- 🔄 **Version Comparison**

### 2. User confirmation dialog box

When a new version is detected, a friendly confirmation dialog is displayed:

```
检测到 NeuCharFramework 有新版本可用：

当前版本: v1.0.0
最新版本: v1.1.0

是否更新到最新版本？

注意：
• 更新将保留您的数据库和配置文件
• 选择"继续使用当前版本"将跳过更新

[更新]  [继续使用当前版本]
```

### 3. Data protection mechanism

#### Automatic backup and recovery
- 🛡️ **App_Data folder full backup** (contains all SQLite databases)
- ⚙️ **Configuration file backup** (appsettings*.json)
- 🔄 **AutoRecover**: Restore all data immediately after update

#### Protection scope
- ✅ SQLite database files (*.db, *.sqlite, *.sqlite3)
- ✅ All App_Data subdirectories and files
- ✅ Application profiles (with smart conflict detection)
- ✅ Files uploaded by users
- ✅ 🆕 logs folder (all log files, also supports log folder)

---

## 🔄 Workflow

### First installation
```
启动应用
  ↓
检测版本 → 未安装
  ↓
直接下载最新版本
  ↓
启动 NCF
```

### The same version is already installed
```
启动应用
  ↓
检测版本 → 版本相同
  ↓
跳过下载，直接启动
  ↓
启动 NCF
```

### New version detected
```
启动应用
  ↓
检测版本 → 发现新版本
  ↓
显示确认对话框
  ├─ [更新]
  │   ↓
  │  备份数据
  │   ↓
  │  下载新版本
  │   ↓
  │  清理旧文件
  │   ↓
  │  解压新版本
  │   ↓
  │  恢复数据
  │   ↓
  │  启动 NCF
  │
  └─ [继续使用当前版本]
      ↓
     跳过下载
      ↓
     直接启动 NCF
```

---

## 💾 Detailed description of data protection

### Backup phase (PreserveImportantFiles)
1. **Create a temporary backup directory**
- Location:`AppData/backup/`
   
2. **Completely copy the App_Data folder**
   ```
   App_Data/
├── Database.db ← SQLite database
├── Uploads/ ← User upload files
├── Cache/ ← Cache files
└── ... ← Other data files
   ```

3. **🆕 Completely copy the logs folder**
   ```
   logs/
├── app.log ← Application log
├── error.log ← Error log
└── ... ← Other log files
   ```
   
Note: Support both`log/`Folders (backwards compatible)

4. **Backup Configuration File**
   - `appsettings.json`
   - `appsettings.Production.json`
   - `appsettings.Development.json`
- etc...

### Cleanup phase (SafeCleanRuntimeDirectory)
- ✅ Delete old program files
- ❌ **SKIP** App_Data directory
- ❌ **Skip** logs directory (and log directory) 🆕
- ❌ **SKIP** Configuration File
- ✅ Maintain directory structure

### Recovery phase (RestoreImportantFiles)
1. **Restore App_Data Folder**
- Recursively copy all files and subdirectories
- Overwrite files with the same name
   
2. **🆕Restore logs folder**
- Keep all log files
- Facilitates problem tracking
- Also supports log folder (backward compatibility)
   
3. **🆕Smart recovery profile (with conflict detection)**
- Compare old configuration and new configuration content
- If the content is the same, use the new configuration directly
- If the content is different, a confirmation dialog box pops up to let the user choose:
- **Use old configuration**: Keep custom settings, new configuration is backed up as`appsettings.backup-yyyyMMdd-HHmmss.json`
- **Use new configuration**: adopt the new version default settings, save the old configuration as`appsettings.old-yyyyMMdd-HHmmss.json`
   
4. **Clean temporary backup**
- Delete temporary backup directory

---

## 🔒 Security

### File protection rules

#### Reserved file (ShouldPreserveFile)
```csharp
// 1. App_Data 中的所有文件
if (relativePath.StartsWith("App_Data"))
    return true;

// 2. 🆕 logs/log 中的所有文件
if (relativePath.StartsWith("logs") || relativePath.StartsWith("log"))
    return true;

// 3. 所有 appsettings*.json 文件
if (fileName.StartsWith("appsettings") && fileName.EndsWith(".json"))
    return true;
```

#### Reserved directory (ShouldPreserveDirectory)
```csharp
// 1. App_Data 及其所有子目录
if (relativePath.StartsWith("App_Data"))
    return true;

// 2. 🆕 logs/log 及其所有子目录
if (relativePath.StartsWith("logs") || relativePath.StartsWith("log"))
    return true;
```

### Error handling
- ✅ Backup failed: record warning and continue updating
- ✅ Recovery failed: record warning, but does not affect startup
- ✅ Cleanup failed: record errors and clean manually

---

## 📊 User experience

### Log output example

#### First installation
```
[10:30:00] 🚀 开始启动 NCF...
[10:30:01] ℹ️ 首次安装，将下载最新版本
[10:30:02] 正在下载 NCF-v1.0.0...
[10:30:10] ✅ 下载完成
[10:30:11] 正在提取文件...
[10:30:15] ✅ 文件提取完成
[10:30:16] 🌐 启动 NCF 进程...
```

#### New version detected (user chooses to update, no configuration conflicts)
```
[10:30:00] 🔍 检查最新版本...
[10:30:01] 📋 最新版本: v1.1.0
[10:30:01] 💾 当前已安装版本: v1.0.0
[10:30:01] 🆕 发现新版本可用！
[10:30:01]    当前版本: v1.0.0
[10:30:01]    最新版本: v1.1.0
[10:30:05] ✅ 用户选择更新到最新版本
[10:30:06] 🛡️ 开始保护重要文件...
[10:30:06] ✅ App_Data 文件夹已备份
[10:30:06] ✅ logs 文件夹已备份
[10:30:06] ✅ 已备份配置文件: appsettings.json
[10:30:06] ✅ 重要文件保护完成
[10:30:07] 🧹 开始安全清理 Runtime 目录...
[10:30:08] 正在下载 NCF-v1.1.0...
[10:30:15] ✅ 下载完成
[10:30:16] 正在提取文件...
[10:30:20] 🔄 开始恢复重要文件...
[10:30:20] ✅ App_Data 文件夹已恢复
[10:30:20] ✅ logs 文件夹已恢复
[10:30:20] ℹ️ 配置文件内容相同，无需处理: appsettings.json
[10:30:20] ✅ 重要文件恢复完成
[10:30:21] 🌐 启动 NCF 进程...
```

#### 🆕 New version detected (user chooses to update, there is a configuration conflict)
```
[10:30:00] 🔍 检查最新版本...
[10:30:01] 📋 最新版本: v1.1.0
[10:30:01] 💾 当前已安装版本: v1.0.0
[10:30:01] 🆕 发现新版本可用！
[10:30:05] ✅ 用户选择更新到最新版本
[10:30:06] 🛡️ 开始保护重要文件...
[10:30:15] ✅ 下载完成
[10:30:16] 正在提取文件...
[10:30:20] 🔄 开始恢复重要文件...
[10:30:20] ✅ App_Data 文件夹已恢复
[10:30:20] ✅ logs 文件夹已恢复
[10:30:21] ⚠️ 检测到配置文件冲突: appsettings.json
[10:30:21]    旧文件大小: 1523 字符
[10:30:21]    新文件大小: 1687 字符
[10:30:21] ⚠️ 配置文件冲突: appsettings.json
[10:30:21]    需要用户决策...
[10:30:25] ✅ 用户选择：使用旧配置覆盖
[10:30:25] 📦 已存档新版本配置文件: appsettings.backup-20251116-103025.json
[10:30:25] ✅ 已恢复旧配置文件: appsettings.json
[10:30:26] ✅ 重要文件恢复完成
[10:30:27] 🌐 启动 NCF 进程...
```

#### New version detected (user chooses to continue using current version)
```
[10:30:00] 🔍 检查最新版本...
[10:30:01] 📋 最新版本: v1.1.0
[10:30:01] 💾 当前已安装版本: v1.0.0
[10:30:01] 🆕 发现新版本可用！
[10:30:01]    当前版本: v1.0.0
[10:30:01]    最新版本: v1.1.0
[10:30:05] ℹ️ 用户选择继续使用当前版本
[10:30:05] ⏭️ 跳过下载和提取，使用现有版本
[10:30:06] 🌐 启动 NCF 进程...
```

#### Same version
```
[10:30:00] 🔍 检查最新版本...
[10:30:01] 📋 最新版本: v1.0.0
[10:30:01] 💾 当前已安装版本: v1.0.0
[10:30:01] ✅ 当前已是最新版本
[10:30:02] ⏭️ 跳过下载和提取，使用现有版本
[10:30:03] 🌐 启动 NCF 进程...
```

---

## 🛠️Technical implementation

### Core method

#### NcfService.cs
- `GetInstalledVersionAsync()`- Get the currently installed version
- `PreserveImportantFilesAsync()`- Back up important files (App_Data + logs + configuration)
- `SafeCleanRuntimeDirectoryAsync()`- Safely clean directories
- `RestoreImportantFilesAsync()`- Recover important files
- `RestoreAppSettingsFilesAsync()`- 🆕 Smart recovery profiles (with conflict detection)
- `HandleAppSettingsConflictAsync()`- 🆕 Handle configuration file conflicts
- `ShouldPreserveFile()`- Determine whether to retain files (including logs folder)
- `ShouldPreserveDirectory()`- Determine whether to retain the directory (including the logs folder)
- `OnAppSettingsConflict`- 🆕 Configuration file conflict handling callback

#### MainWindowViewModel.cs
- `CheckLatestVersionAsync()`- Check the latest version
- `CheckAndConfirmUpdateAsync()`- Check and confirm updates
- `ShowConfirmDialogAsync()`- Show confirmation dialog
- `HandleAppSettingsConflictAsync()`- 🆕 Handle profile conflict dialog

### Data storage location

#### Windows
```
C:\Users\{Username}\AppData\Roaming\NcfDesktopApp\
├── Runtime\              ← NCF 运行文件
│   └── App_Data\        ← 数据库和配置（被保护）
├── Downloads\           ← 临时下载文件
└── backup\              ← 临时备份（更新时）
```

#### macOS/Linux
```
~/.config/NcfDesktopApp/
├── Runtime/
│   └── App_Data/
├── Downloads/
└── backup/
```

---

## ✅ Test verification

### Test scenario

1. **First installation test** ✅
- No existing version
- Automatically download the latest version

2. **Same version test** ✅
- current version = latest version
- Skip download and launch directly

3. **Version update test (select update)** ✅
- current version < latest version
- User selected updates
- Backup, clean, decompress, restore
- Data integrity verification

4. **Version update test (choose to skip)** ✅
- current version < latest version
- User chooses to continue using the current version
- Start directly without downloading

5. **Data Protection Test** ✅
-Create test database file
- Perform updates
- Verify data integrity

6. **Error handling test** ✅
- Backup failure scenario
- Network disconnection scenario
- Insufficient disk space scenario

---

## 🔧 Configuration options

### Automatically clean downloaded files
Configurable in settings:
- ✅ **Auto Clean** (Default): Automatically delete downloaded ZIP files after updates
- ❌ **NO CLEAN**: Keep downloaded files for offline use

---

## 📝 Best Practices

### User suggestions
1. **Regular Updates**: Update to the latest version promptly to get the latest features and security fixes
2. **Manual Backup**: Although the app will be backed up automatically, it is recommended to manually back up the App_Data folder regularly
3. **Test update**: Before updating the production environment, it is recommended to verify it in the test environment first.

### Developer recommendations
1. **Version number specification**: Use semantic versioning (Semantic Versioning)
2. **Backward Compatibility**: Ensure the database schema is backward compatible
3. **Migration script**: If you need to modify the database structure, provide a migration script

---

## 🐛 Troubleshooting

### FAQ

#### Q: Data lost after update?
**A**: Check the logs to confirm that the backup and recovery steps were executed successfully.
Temporary backup location:`AppData/backup/`

#### Q: How to recover from update failure?
**A**: 
1. Check whether the temporary backup exists
2. Manual copy`backup/App_Data`arrive`Runtime/App_Data`
3. Restart the application

#### Q: How to force a re-download?
**A**: 
1. Stop NCF
2. Delete`Runtime`folder
3. Delete`Downloads`ZIP file in folder
4. Restart the application

---

## 📚 Related documents

- [FILE_PROTECTION_MECHANISM.md](./FILE_PROTECTION_MECHANISM.md) - Detailed description of file protection mechanism
- [README.md](./README.md) - General description of the project
- [build-tool/TESTING_GUIDE.md](./build-tool/TESTING_GUIDE.md) - Testing Guide

---

## 🎯 Future improvements

### Planning function
- [ ] Incremental update: only download changed files
- [ ] Automatic backup history: keep multiple versions of backups
- [ ] Rollback function: roll back to the previous version with one click
- [ ] Update log display: display version update content
- [ ] Silent update option: background automatic update (optional)

---

---

## 🆕 Version 1.1.0 updates

### New features
1. **🆕 logs folder protection**
- Automatic backup and restore`logs/`folder
- Support both`log/`Folders (backwards compatible)
- Keep all log files for easy problem tracking

2. **🆕 Intelligent conflict detection of configuration files**
- Automatically compare old configuration and new configuration content
- Automatically use the new configuration when the content is the same
- A user selection dialog box pops up when the content is different
- No matter which one you choose, the other one will be backed up

3. **🆕 Configuration file backup naming optimization**
- When using old configuration:`appsettings.backup-yyyyMMdd-HHmmss.json`
- When retaining new configuration:`appsettings.old-yyyyMMdd-HHmmss.json`
- Easy identification and traceability

### Dialog example

#### Configuration file conflict dialog box
```
检测到配置文件冲突：

文件名: appsettings.json

旧配置大小: 1523 字符
新配置大小: 1687 字符

选择"使用旧配置"将保留您的自定义设置
选择"使用新配置"将使用新版本的默认设置

注意：
• 使用旧配置：新版本配置将备份为 appsettings.json.backup-[日期].json
• 使用新配置：旧配置将另存为 appsettings.json.old-[日期].json

[使用旧配置]  [使用新配置]
```

---

**Last updated**: 2025-11-16
**Version**: 1.1.0

