[中文版](DOWNLOAD_RESUME_VERSION_CHECK.cn.md)

# 📥Resumable version verification function

**Implementation time**: 2025-11-17
**Status**: ✅ Completed

## 🎯 Function Overview

A **version verification mechanism** has been added to the NCF package download function to ensure that the same version of the file is downloaded when resuming the download to avoid file damage caused by version confusion.

---

## 🔍 Problem background

### Previous implementation
The original breakpoint resume function only judged whether to continue downloading based on file size:
- ✅ Support HTTP Range request
- ✅ Use `FileMode.Append` to append writing
- ❌ **Version consistency not verified**

### Potential risks
If the user downloads half of the file:
1. NCF released a new version
2. User restarts download
3. The program will continue to append the new version of the content to the old version of the file
4. **Result**: Mixed version of corrupted file generated ⚠️

---

## ✅ Solution

### Core Design
Use the `.download` meta-information file to record the download source and ensure version consistency through URL comparison:```
senparc-ncf-template-v1.2.3.zip       # 实际下载文件
senparc-ncf-template-v1.2.3.zip.download  # 元信息文件（记录 URL）
```### Workflow

#### 1️⃣ Start downloading phase```csharp
// 检查是否存在未完成的下载
if (File.Exists(filePath))
{
    // 读取 .download 文件中保存的 URL
    if (File.Exists(downloadInfoPath))
    {
        var savedUrl = await File.ReadAllTextAsync(downloadInfoPath);
        
        // 比对 URL 是否一致（URL 包含版本号）
        if (savedUrl.Trim() == downloadUrl.Trim())
        {
            canResume = true; // ✅ 可以断点续传
        }
        else
        {
            // ⚠️ 版本不一致，删除旧文件重新下载
            File.Delete(filePath);
            File.Delete(downloadInfoPath);
        }
    }
    else
    {
        // ⚠️ 缺少元信息，无法验证版本，删除重新下载
        File.Delete(filePath);
    }
}

// 保存当前下载的 URL 到 .download 文件
if (existingFileSize == 0)
{
    await File.WriteAllTextAsync(downloadInfoPath, downloadUrl);
}
```#### 2️⃣ Download in progress
- If `canResume == true`: continue downloading using HTTP Range request
- Otherwise: start downloading from the beginning

#### 3️⃣ Download completed```csharp
// 清理 .download 元信息文件
if (File.Exists(downloadInfoPath))
{
    File.Delete(downloadInfoPath);
}
```---

## 📊 Scenario test

### Scenario 1: Normal breakpoint resumption```
操作流程:
1. 开始下载 v1.2.3 版本（50%）
2. 程序中断退出
3. 重新启动下载 v1.2.3 版本

预期结果:
✅ 检测到同一版本的未完成下载，可以断点续传
📥 从 52,428,800 字节处继续下载: senparc-ncf-template.zip
```### Scenario 2: Version change```
操作流程:
1. 开始下载 v1.2.3 版本（50%）
2. 程序中断退出
3. NCF 发布新版本 v1.2.4
4. 重新启动下载 v1.2.4 版本

预期结果:
⚠️ 检测到不同版本的文件，删除旧文件
   旧版本: https://example.com/ncf/v1.2.3/package.zip
   新版本: https://example.com/ncf/v1.2.4/package.zip
📥 开始下载: senparc-ncf-template.zip
```### Scenario 3: Meta information file is lost```
操作流程:
1. 开始下载 v1.2.3 版本（50%）
2. 用户手动删除 .download 文件
3. 重新启动下载

预期结果:
⚠️ 未找到下载信息文件，无法确认版本，重新下载
📥 开始下载: senparc-ncf-template.zip
```### Scenario 4: Complete download (no interruption)```
操作流程:
1. 开始下载 v1.2.3 版本
2. 下载成功完成（100%）

预期结果:
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```---

## 🔧 Implementation details

### File location
`Services/NcfService.cs` - `DownloadFileAsync` method

### Key code snippets```128:191:Services/NcfService.cs
public async Task DownloadFileAsync(string downloadUrl, string fileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
{
    var filePath = Path.Combine(DownloadsPath, fileName);
    var downloadInfoPath = filePath + ".download"; // 下载信息文件
    
    // 检查是否有未完成的下载（断点续传）
    long existingFileSize = 0;
    bool canResume = false;
    
    if (File.Exists(filePath))
    {
        var fileInfo = new FileInfo(filePath);
        existingFileSize = fileInfo.Length;
        
        // 检查是否有下载信息文件（包含 URL 和版本信息）
        if (File.Exists(downloadInfoPath))
        {
            try
            {
                var savedUrl = await File.ReadAllTextAsync(downloadInfoPath, cancellationToken);
                
                // 比较 URL 是否一致（URL 包含版本号）
                if (savedUrl.Trim() == downloadUrl.Trim())
                {
                    canResume = true;
                    _logger?.LogInformation($"✅ 检测到同一版本的未完成下载，可以断点续传");
                }
                else
                {
                    _logger?.LogWarning($"⚠️ 检测到不同版本的文件，删除旧文件");
                    _logger?.LogInformation($"   旧版本: {savedUrl}");
                    _logger?.LogInformation($"   新版本: {downloadUrl}");
                    
                    // 删除旧文件和下载信息
                    File.Delete(filePath);
                    File.Delete(downloadInfoPath);
                    existingFileSize = 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"⚠️ 无法读取下载信息，重新下载: {ex.Message}");
                File.Delete(filePath);
                if (File.Exists(downloadInfoPath))
                {
                    File.Delete(downloadInfoPath);
                }
                existingFileSize = 0;
            }
        }
        else
        {
            // 没有下载信息文件，无法确认版本，删除重新下载
            _logger?.LogWarning($"⚠️ 未找到下载信息文件，无法确认版本，重新下载");
            File.Delete(filePath);
            existingFileSize = 0;
        }
    }
    
    // 保存下载信息（URL 作为版本标识）
    if (existingFileSize == 0)
    {
        await File.WriteAllTextAsync(downloadInfoPath, downloadUrl, cancellationToken);
    }
```### Log output example

#### Resume upload successfully```
✅ 检测到同一版本的未完成下载，可以断点续传
📥 从 52,428,800 字节处继续下载: senparc-ncf-template.zip
✅ 服务器支持断点续传，继续下载
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```#### Version inconsistent```
⚠️ 检测到不同版本的文件，删除旧文件
   旧版本: https://example.com/ncf/v1.2.3/package.zip
   新版本: https://example.com/ncf/v1.2.4/package.zip
📥 开始下载: senparc-ncf-template.zip
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```---

## 📋 Technical Points

### 1. URL as version identifier
- ✅ NCF download URL contains full version number
- ✅ URL uniqueness ensures version uniqueness
- ✅ Simple and reliable, no extra version field required

### 2. Meta information file design
- **Naming rules**: `Original file name + .download`
- **Content**: Only the original download URL is stored
- **Life Cycle**: Automatically deleted after downloading is completed

### 3. Error handling
- Failed to read meta information file → Delete and re-download (safety first)
- Meta information file does not exist → Delete and re-download
- Unified handling of abnormal situations to avoid file damage

### 4. Performance optimization
- Only read meta information if the file exists
- Use `Trim()` to avoid whitespace interference
- Use `CancellationToken` to support cancellation operations

---

## ✅ Verification Checklist

- [x] Resumable download of the same version works normally
- [x] Different versions automatically delete old files
- [x] Safe handling when meta information files are missing
- [x] Clean meta information files after download is complete
- [x] Log output is clear and easy to understand
- [x] Correct handling of exceptions
- [x] No memory leaks or resource leaks

---

## 🎯 User experience improvements

### Before```
[用户不知道为什么下载失败]
下载完成: senparc-ncf-template.zip
解压失败：文件已损坏 ❌
```### Now```
⚠️ 检测到不同版本的文件，删除旧文件
   旧版本: https://example.com/ncf/v1.2.3/package.zip
   新版本: https://example.com/ncf/v1.2.4/package.zip
📥 开始下载: senparc-ncf-template.zip
✅ 下载完成: senparc-ncf-template.zip
```---

## 📚 Related documents

- [Summary of implementation of breakpoint resume download](./DOWNLOAD_RESUME_IMPLEMENTATION.md)
- [HTTP Range Request Specification](https://developer.mozilla.org/en-US/docs/Web/HTTP/Range_requests)
- [Best Practices for File Downloads](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)

---

## 🔜 Future improvement directions

1. **MD5/SHA256 verification**
   - In addition to URL comparison, add file hash verification
   - Detect whether files have been tampered with during downloading

2. **Download meta information extension**
   - Record download start time
   - Log expected file size
   - Record server ETag (if supported)

3. **Download History Management**
   - Keep recently downloaded version information
   -Supports rollback to previous version

---

**Summary**: Through a simple and effective URL verification mechanism, the problem of version confusion that may be caused by resuming downloads is completely solved, ensuring the integrity and correctness of downloaded files.
