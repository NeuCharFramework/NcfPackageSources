# 📥 Test Guide for Resume Breakpoint Function

**Version**: v1.1.0
**Test Date**: 2025-11-17

---

## 🎯 Test target

Verify the resumable download functionality of the NCF package to ensure:
1. ✅ You can continue downloading after it is interrupted
2. ✅ The version verification mechanism is working normally
3. ✅ Properly manage meta-information files
4. ✅ The log output is clear and easy to understand.

---

## 📋 Test scenario

### Scenario 1: Normal breakpoint resume (same version)

**Test steps**:
1. Start NCF Desktop App
2. Click to download the NCF package on the "Settings" page
3. Wait for the download progress to reach between **30%-50%**
4. Force close the application (kill the process)
5. Restart the application
6. Click again to download the NCF package

**Expected results**:
```
✅ 检测到同一版本的未完成下载，可以断点续传
📥 从 XX,XXX,XXX 字节处继续下载: senparc-ncf-template.zip
✅ 服务器支持断点续传，继续下载
[进度] XX% → 100%
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```

**Verification Point**:
- [ ] Log says "An incomplete download of the same version was detected"
- [ ] Download resumes where it left off, rather than starting at 0%
- [ ] After downloading is complete`.download`Automatic file deletion
- [ ] The final file can be decompressed normally

---

### Scenario 2: Version change (simulating new version release)

**Test steps**:
1. Download NCF package to **50%**
2. Close the application
3. Manual editing`.download`File, modify the version number in the URL (simulate a new version)
   ```
Original content: https://example.com/.../v1.2.3/...
Modify to: https://example.com/.../v1.2.4/...
   ```
4. Restart the application
5. Click to download the NCF package

**Expected results**:
```
⚠️ 检测到不同版本的文件，删除旧文件
   旧版本: https://example.com/.../v1.2.3/...
   新版本: https://example.com/.../v1.2.4/...
📥 开始下载: senparc-ncf-template.zip
[进度] 0% → 100%
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```

**Verification Point**:
- [ ] Log says "Different versions of files detected"
- [ ] shows URL comparison between old and new versions
- [ ] old files and`.download`File deleted
- [ ] Download restarts from 0%

---

### Scenario 3: Meta information file is lost

**Test steps**:
1. Download the NCF package to **40%**
2. Close the application
3. Manual deletion`.download`File (keep partially downloaded master file)
4. Restart the application
5. Click to download the NCF package

**Expected results**:
```
⚠️ 未找到下载信息文件，无法确认版本，重新下载
📥 开始下载: senparc-ncf-template.zip
[进度] 0% → 100%
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```

**Verification Point**:
- [ ] The log shows "Download information file not found"
- [ ] Old partial files are deleted
- [ ] Download restarts from 0%
- [ ] new`.download`File is created

---

### Scenario 4: Complete download (no interruption)

**Test steps**:
1. Start NCF Desktop App
2. Click to download the NCF package
3. Wait for the download to complete (without interruption)

**Expected results**:
```
📥 开始下载: senparc-ncf-template.zip
[进度] 0% → 100%
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```

**Verification Point**:
- [ ] Download progress increases smoothly
- [ ] After downloading is complete`.download`File deleted
- [ ] Leave only the final zip file
- [ ] files can be decompressed normally

---

### Scenario 5: The server does not support resumption of downloads (downgrade processing)

**Test steps**:
1. Download NCF package to **30%**
2. Close the application
3. (Assuming the server returns 200 instead of 206)
4. Restart and download

**Expected results**:
```
⚠️ 服务器不支持断点续传，重新下载
📥 开始下载: senparc-ncf-template.zip
[进度] 0% → 100%
✅ 下载完成: senparc-ncf-template.zip
```

**Verification Point**:
- [ ] The log shows "The server does not support resumable downloads"
- [ ] Program automatically downgrades to full download
- [ ] Download completed successfully

---

## 🔍 File system check

### Files that should exist during the download process:
```
Downloads/
├── senparc-ncf-template.zip         # 部分下载的文件
└── senparc-ncf-template.zip.download # 元信息文件（存储 URL）
```

### Files that should exist after the download is complete:
```
Downloads/
└── senparc-ncf-template.zip         # 完整的压缩包
```

### `.download`Example of file content:
```
https://gitee.com/NeuCharFramework/NcfPackageSources/releases/download/v1.2.3/senparc-ncf-template.zip
```

---

## 📊 Test Checklist

### Function verification
- [ ] Resumable download of the same version works normally
- [ ] Different versions automatically delete old files
- [ ] Safe handling when meta information files are missing
- [ ] Clean temporary files after download is complete
- [ ] Downgrade processing when the server does not support Range

### Log output verification
- [ ] There is a clear prompt when starting the download
- [ ] Display the comparison between the old and new versions when the versions are inconsistent.
- [ ] Download progress updated in real time
- [ ] There will be a clear prompt when the download is completed.
- [ ] Clean temporary files with logging

###Exception handling verification
- [ ] Return to normal after network interruption
- [ ] There is an error message for file permission issues
- [ ] There is an error message when there is insufficient disk space.

### File integrity verification
- [ ] The downloaded zip file can be decompressed normally
- [ ] The decompressed file content is complete
- [ ] No file corruption caused by version confusion

---

## 🐛 Known issues and limitations

### limit
1. **URL as version identifier**
- Dependency URL contains version information
- Unable to detect when the URL remains unchanged but the content changes (extreme case)

2. **Server Support**
- The server must support HTTP Range requests to resume the download.
- Automatically downgrade to full download when not supported

3. **Meta Information File**
- User manual deletion`.download`File will cause re-download
- This is a security-first design to avoid version confusion

---

## 🎯 Success Criteria

All test scenarios pass and meet the following conditions:
- ✅ The resume function is working properly
- ✅ Version verification mechanism effectively prevents file corruption
- ✅ Log output is clear and easy to understand
- ✅ Exceptional situations are handled gracefully
- ✅ The downloaded file is intact and intact

---

## 📝 Test record template

```markdown
## 测试记录

**测试人员**: _________  
**测试时间**: _________  
**应用版本**: v1.1.0

| 场景 | 测试结果 | 备注 |
|------|---------|------|
| 场景 1: 正常断点续传 | ✅ / ❌ |  |
| 场景 2: 版本变更 | ✅ / ❌ |  |
| 场景 3: 元信息丢失 | ✅ / ❌ |  |
| 场景 4: 完整下载 | ✅ / ❌ |  |
| 场景 5: 服务器降级 | ✅ / ❌ |  |

**整体评价**: _________  
**发现的问题**: _________  
**改进建议**: _________
```

---

**Document Update**: 2025-11-17
**Related Documents**: [DOWNLOAD_RESUME_VERSION_CHECK.md](./DOWNLOAD_RESUME_VERSION_CHECK.md)

