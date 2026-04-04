# 📥 断点续传版本验证功能

**实现时间**: 2025-11-17  
**状态**: ✅ 已完成

## 🎯 功能概述

为 NCF 程序包下载功能增加了**版本验证机制**，确保断点续传时下载的是同一版本的文件，避免版本混淆导致的文件损坏问题。

---

## 🔍 问题背景

### 之前的实现
最初的断点续传功能仅基于文件大小判断是否继续下载：
- ✅ 支持 HTTP Range 请求
- ✅ 使用 `FileMode.Append` 追加写入
- ❌ **未验证版本一致性**

### 潜在风险
如果用户下载了一半文件后：
1. NCF 发布了新版本
2. 用户重新启动下载
3. 程序会继续追加新版本的内容到旧版本文件
4. **结果**: 生成混合版本的损坏文件 ⚠️

---

## ✅ 解决方案

### 核心设计
使用 `.download` 元信息文件记录下载源，通过 URL 比对确保版本一致性：

```
senparc-ncf-template-v1.2.3.zip       # 实际下载文件
senparc-ncf-template-v1.2.3.zip.download  # 元信息文件（记录 URL）
```

### 工作流程

#### 1️⃣ 开始下载阶段
```csharp
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
```

#### 2️⃣ 下载进行中
- 如果 `canResume == true`：使用 HTTP Range 请求继续下载
- 否则：从头开始下载

#### 3️⃣ 下载完成
```csharp
// 清理 .download 元信息文件
if (File.Exists(downloadInfoPath))
{
    File.Delete(downloadInfoPath);
}
```

---

## 📊 场景测试

### 场景 1: 正常断点续传
```
操作流程:
1. 开始下载 v1.2.3 版本（50%）
2. 程序中断退出
3. 重新启动下载 v1.2.3 版本

预期结果:
✅ 检测到同一版本的未完成下载，可以断点续传
📥 从 52,428,800 字节处继续下载: senparc-ncf-template.zip
```

### 场景 2: 版本变更
```
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
```

### 场景 3: 元信息文件丢失
```
操作流程:
1. 开始下载 v1.2.3 版本（50%）
2. 用户手动删除 .download 文件
3. 重新启动下载

预期结果:
⚠️ 未找到下载信息文件，无法确认版本，重新下载
📥 开始下载: senparc-ncf-template.zip
```

### 场景 4: 完整下载（无中断）
```
操作流程:
1. 开始下载 v1.2.3 版本
2. 下载成功完成（100%）

预期结果:
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```

---

## 🔧 实现细节

### 文件位置
`Services/NcfService.cs` - `DownloadFileAsync` 方法

### 关键代码段

```128:191:Services/NcfService.cs
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
```

### 日志输出示例

#### 成功断点续传
```
✅ 检测到同一版本的未完成下载，可以断点续传
📥 从 52,428,800 字节处继续下载: senparc-ncf-template.zip
✅ 服务器支持断点续传，继续下载
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```

#### 版本不一致
```
⚠️ 检测到不同版本的文件，删除旧文件
   旧版本: https://example.com/ncf/v1.2.3/package.zip
   新版本: https://example.com/ncf/v1.2.4/package.zip
📥 开始下载: senparc-ncf-template.zip
✅ 下载完成: senparc-ncf-template.zip
🧹 已清理下载信息文件
```

---

## 📋 技术要点

### 1. URL 作为版本标识
- ✅ NCF 下载 URL 包含完整版本号
- ✅ URL 唯一性保证版本唯一性
- ✅ 简单可靠，无需额外版本字段

### 2. 元信息文件设计
- **命名规则**: `原文件名 + .download`
- **内容**: 仅存储原始下载 URL
- **生命周期**: 下载完成后自动删除

### 3. 错误处理
- 元信息文件读取失败 → 删除重新下载（安全优先）
- 元信息文件不存在 → 删除重新下载
- 异常情况统一处理，避免文件损坏

### 4. 性能优化
- 仅在文件存在时才读取元信息
- 使用 `Trim()` 避免空白字符干扰
- 使用 `CancellationToken` 支持取消操作

---

## ✅ 验证清单

- [x] 同一版本断点续传正常工作
- [x] 不同版本自动删除旧文件
- [x] 元信息文件缺失时安全处理
- [x] 下载完成后清理元信息文件
- [x] 日志输出清晰易懂
- [x] 异常情况正确处理
- [x] 无内存泄漏或资源泄漏

---

## 🎯 用户体验改进

### 之前
```
[用户不知道为什么下载失败]
下载完成: senparc-ncf-template.zip
解压失败：文件已损坏 ❌
```

### 现在
```
⚠️ 检测到不同版本的文件，删除旧文件
   旧版本: https://example.com/ncf/v1.2.3/package.zip
   新版本: https://example.com/ncf/v1.2.4/package.zip
📥 开始下载: senparc-ncf-template.zip
✅ 下载完成: senparc-ncf-template.zip
```

---

## 📚 相关文档

- [断点续传实现总结](./DOWNLOAD_RESUME_IMPLEMENTATION.md)
- [HTTP Range 请求规范](https://developer.mozilla.org/en-US/docs/Web/HTTP/Range_requests)
- [文件下载最佳实践](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)

---

## 🔜 未来改进方向

1. **MD5/SHA256 校验**
   - 除了 URL 比对，增加文件哈希校验
   - 检测文件是否在下载过程中被篡改

2. **下载元信息扩展**
   - 记录下载开始时间
   - 记录预期文件大小
   - 记录服务器 ETag（如果支持）

3. **下载历史管理**
   - 保留最近下载的版本信息
   - 支持回退到之前的版本

---

**总结**: 通过简单而有效的 URL 验证机制，彻底解决了断点续传可能导致的版本混淆问题，确保下载文件的完整性和正确性。

