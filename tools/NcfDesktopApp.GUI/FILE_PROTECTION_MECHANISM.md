# NCF Desktop App file protection mechanism

## 📋 Overview

This document describes the file protection mechanism of the NCF Desktop App client updater to ensure that important user data and configuration files are not lost during the update process.

## 🛡️ Protection mechanism

### Protected files and folders

1. **App_Data folder**
- Complete protection of the entire`App_Data`folder and all its contents
-Including database files, user configuration, cache and other important data

2. **appsettings*.json file**
- Protect all`appsettings`start with`.json`configuration file at the end
- include`appsettings.json`、`appsettings.Development.json`wait
- Support configuration files in subdirectories

### Backup strategy

1. **Temporary Backup**
- Back up important files to`backup`folder (with`runtime`tied)
- Automatically restore files after the update is complete
- Temporary backups are automatically cleaned after restoration

2. **Permanent Backup**
- for each`appsettings*.json`Create timestamped backup of files
- Format:`appsettings.json.20241211_143025.bak`
- Save in`backup`folder and will not be automatically deleted

## 🔄 Update process

### Original process (with problems)
```
1. 完全删除 runtime 文件夹 ❌
2. 重新创建 runtime 文件夹
3. 解压新版本文件
```

### New Security Process ✅
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

## 📁 Folder structure

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

## 🔧 Implementation details

### Core method

1. **PreserveImportantFilesAsync()**
- Create backup directory
- Copy the App_Data folder to a temporary location
- Back up all appsettings*.json files

2. **SafeCleanRuntimeDirectoryAsync()**
- Iterate through all files and folders
- use`ShouldPreserveFile()`and`ShouldPreserveDirectory()`Determine whether to retain
- Delete only non-important files

3. **RestoreImportantFilesAsync()**
- Restore App_Data folder from temporary location
- Restore appsettings*.json files
- Clean temporary backups

### Judgment logic

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

## 🚨 Error handling

- All file operations include exception handling
- Backup failure will not prevent the update from proceeding
- If recovery fails, a warning log will be recorded but the program will not be interrupted.
- Failure to clean temporary backups will not affect application operation

## 📝 Logging

Detailed log information will be recorded during the update process:

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

## 🧪 Testing suggestions

1. **Create test data**
- Place test files in App_Data folder
- Modify appsettings.json to add custom configuration

2. **Execute update test**
- Run client updater
- Verify that the contents of the App_Data folder are complete
- Verify that the appsettings.json configuration remains unchanged

3. **Check backup files**
- Verify that a timestamped backup file exists in the backup folder
- Verify that the backup file contents are correct

## 🔒 Security

- Backup files are stored in the user data directory, with appropriate access rights
- Temporary backups are cleaned immediately after restoration, reducing exposure of sensitive data
- Permanent backup files can be used to manually restore configurations

## 📞 Troubleshooting

If you find that the configuration is missing after updating:

1. Check`backup`Timestamp backup files in folder
2. Manually copy the backup file to the runtime folder
3. Rename the backup file to remove the timestamp suffix
4. Restart the application

---

**Note**: This mechanism ensures the security of user data, but it is recommended that users regularly back up important data to an external location.

