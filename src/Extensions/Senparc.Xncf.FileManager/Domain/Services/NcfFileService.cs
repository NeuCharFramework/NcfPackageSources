/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfFileService.cs
    文件功能描述：统一管理知识库源文件和站点静态资源的物理存储、元数据及公开访问边界


    创建标识：Senparc - 20250112

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview1 完善文件资源边界、安全删除策略与静态资源管理

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Domain.Services;

public sealed class NcfFileReadResult
{
    public NcfFileReadResult(NcfFile file, Stream stream)
    {
        File = file;
        Stream = stream;
    }

    public NcfFile File { get; }
    public Stream Stream { get; }
}

public class NcfFileService : ServiceBase<NcfFile>
{
    public const long MaxFileSizeBytes = 50L * 1024 * 1024;
    public const long MaxTotalUploadBytes = 100L * 1024 * 1024;
    public const int MaxFilesPerUpload = 20;

    private readonly string _baseFilePath;

    public NcfFileService(IRepositoryBase<NcfFile> repo, IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
        _baseFilePath = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "NcfFiles");
        Directory.CreateDirectory(_baseFilePath);
    }

    /// <summary>
    /// Lists only one resource scope. This is intentional: an asset picker must
    /// never accidentally show documents that are meant for a knowledge base.
    /// </summary>
    public async Task<PagedList<NcfFileDto>> GetFilesAsync(
        int page,
        int pageSize,
        int? folderId,
        NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
    {
        EnsureValidScope(resourceScope);
        var result = (await GetObjectListAsync(
                page,
                pageSize,
                z => z.FolderId == folderId && z.ResourceScope == resourceScope,
                z => z.Id,
                OrderingType.Descending,
                null))
            .ToDtoPagedList<NcfFile, NcfFileDto>(this);

        foreach (var dto in result)
        {
            dto.PublicUrl = GetPublicAssetUrl(dto);
        }

        return result;
    }

    /// <summary>
    /// Aggregates enterprise-document (knowledge-base) usage for the admin dashboard.
    /// </summary>
    public async Task<object> GetDashboardStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var scope = NcfFileResourceScope.KnowledgeBase;
        var files = await GetObjectListAsync(
            0,
            0,
            z => z.ResourceScope == scope,
            z => z.UploadTime,
            OrderingType.Ascending,
            null);

        var end = (endDate ?? DateTime.Today).Date;
        var start = (startDate ?? end.AddDays(-6)).Date;
        if (start > end)
        {
            (start, end) = (end, start);
        }

        // Cap trend points to avoid huge payloads for long ranges.
        var daySpan = (int)(end - start).TotalDays + 1;
        if (daySpan > 366)
        {
            start = end.AddDays(-365);
            daySpan = 366;
        }

        var trend = new List<object>(daySpan);
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var dayEnd = day.AddDays(1);
            var cum = files.Where(f => f.UploadTime < dayEnd).ToList();
            trend.Add(new
            {
                date = day.ToString("MM-dd"),
                fullDate = day.ToString("yyyy-MM-dd"),
                fileCount = cum.Count,
                totalSizeBytes = cum.Sum(f => f.FileSize)
            });
        }

        static string CategoryOf(NcfFile file)
        {
            var ext = (file.FileExtension ?? string.Empty).Trim().ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".ico" or ".avif")
                return "图片";
            if (ext is ".mp4" or ".avi" or ".mov" or ".wmv" or ".mkv" or ".webm")
                return "视频";
            if (ext is ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".m4a")
                return "音频";
            if (file.FileType is FileType.Text or FileType.Word or FileType.PowerPoint or FileType.Excel or FileType.Code
                || ext is ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".md" or ".json" or ".xml" or ".csv")
                return "文档";
            return "其他";
        }

        var groups = files
            .GroupBy(CategoryOf)
            .Select(g => new { name = g.Key, count = g.Count(), sizeBytes = g.Sum(x => x.FileSize) })
            .ToList();

        var colors = new Dictionary<string, string>
        {
            ["文档"] = "#95de64",
            ["图片"] = "#597ef7",
            ["视频"] = "#ffd666",
            ["音频"] = "#ff9c6e",
            ["其他"] = "#91d5ff"
        };

        var order = new[] { "文档", "图片", "视频", "音频", "其他" };
        var sizeSlices = order.Select(name =>
        {
            var g = groups.FirstOrDefault(x => x.name == name);
            var bytes = g?.sizeBytes ?? 0L;
            return new
            {
                name,
                value = Math.Round(bytes / (1024d * 1024d), 2), // MB for chart readability
                sizeBytes = bytes,
                color = colors[name]
            };
        }).Where(x => x.sizeBytes > 0 || x.name == "文档" || x.name == "其他").ToList();

        var countSlices = order.Select(name =>
        {
            var g = groups.FirstOrDefault(x => x.name == name);
            return new
            {
                name,
                value = g?.count ?? 0,
                color = colors[name]
            };
        }).Where(x => x.value > 0 || x.name == "文档" || x.name == "其他").ToList();

        var totalBytes = files.Sum(f => f.FileSize);
        var totalCount = files.Count;

        string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes}B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024d:F2}KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024d):F2}MB";
            return $"{bytes / (1024d * 1024d * 1024d):F2}GB";
        }

        return new
        {
            statsCutoff = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalCount,
            totalSizeBytes = totalBytes,
            totalSizeLabel = FormatSize(totalBytes),
            enterpriseUsed = FormatSize(totalBytes),
            orgUsages = new[]
            {
                new { name = "山西米立信息技术有限公司", size = FormatSize(totalBytes) },
                new { name = "企业文档根目录", size = FormatSize(0) },
                new { name = "知识库资料", size = FormatSize(totalBytes) }
            },
            sizeTotalLabel = FormatSize(totalBytes).Replace(" ", ""),
            countTotalLabel = totalCount.ToString(),
            sizeSlices,
            countSlices,
            capacityTrend = trend
        };
    }

    /// <summary>
    /// Backward-compatible overload: callers predating the resource boundary
    /// keep creating knowledge-base sources.
    /// </summary>
    public Task<NcfFile> UploadFileAsync(IFormFile file, int? folderId = null)
    {
        return UploadFileAsync(file, NcfFileResourceScope.KnowledgeBase, folderId);
    }

    public async Task<NcfFile> UploadFileAsync(
        IFormFile file,
        NcfFileResourceScope resourceScope,
        int? folderId = null)
    {
        if (file == null || file.Length <= 0)
        {
            throw new ArgumentException("上传文件不能为空。", nameof(file));
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"单个文件不能超过 {MaxFileSizeBytes / 1024 / 1024} MB。");
        }

        EnsureValidScope(resourceScope);
        await ValidateFolderAsync(folderId, resourceScope);

        var originalFileName = Path.GetFileName((file.FileName ?? string.Empty).Replace('\\', '/'));
        var fileExtension = NcfFileResourcePolicy.NormalizeExtension(originalFileName);
        if (!NcfFileResourcePolicy.IsAllowedExtension(resourceScope, fileExtension))
        {
            throw new InvalidOperationException(resourceScope == NcfFileResourceScope.KnowledgeBase
                ? "知识库文件仅支持可安全提取的文本和 Office Open XML 格式。"
                : "站点静态资源仅支持图片、音视频和字体格式；不接受 HTML、SVG、JavaScript 或压缩包。" );
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            originalFileName = $"upload{fileExtension}";
        }
        if (originalFileName.Length > 250)
        {
            originalFileName = originalFileName[..250];
        }

        var now = DateTime.Now;
        var datePath = string.Join('/', NcfFileResourcePolicy.GetStorageRoot(resourceScope), now.Year.ToString(), now.Month.ToString("00"));
        var fullPath = Path.Combine(_baseFilePath, datePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(fullPath);

        var storageFileName = Guid.NewGuid().ToString("N");
        var physicalPath = Path.Combine(fullPath, storageFileName + fileExtension);

        try
        {
            var contentHash = await CopyAndHashAsync(file, physicalPath);
            var ncfFile = new NcfFile
            {
                FileName = originalFileName,
                StorageFileName = storageFileName,
                FilePath = datePath,
                FileSize = file.Length,
                FileExtension = fileExtension,
                FileType = GetFileType(fileExtension),
                ContentType = NcfFileResourcePolicy.GetContentType(fileExtension),
                ContentHash = contentHash,
                ResourceScope = resourceScope,
                AccessLevel = NcfFileAccessLevel.Private,
                UploadTime = now,
                FolderId = folderId
            };

            await SaveObjectAsync(ncfFile);
            return ncfFile;
        }
        catch (Exception ex)
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
            SenparcTrace.BaseExceptionLog(ex);
            throw;
        }
    }

    public async Task UpdateFileNoteAsync(int id, string note)
    {
        var file = await GetObjectAsync(z => z.Id == id);
        if (file == null)
        {
            throw new InvalidOperationException($"文件不存在：{id}");
        }

        note = note?.Trim();
        if (note?.Length > 300)
        {
            throw new ArgumentException("文件备注不能超过 300 个字符。", nameof(note));
        }

        file.Description = note;
        await SaveObjectAsync(file);
    }

    public async Task SetSiteAssetPublicationAsync(int id, bool publish)
    {
        var file = await GetObjectAsync(z => z.Id == id)
            ?? throw new InvalidOperationException($"文件不存在：{id}");

        if (publish)
        {
            NcfFileResourcePolicy.EnsureCanPublish(file);
        }
        else if (file.ResourceScope != NcfFileResourceScope.SiteAsset)
        {
            throw new InvalidOperationException("只有站点静态资源可以修改公开状态。");
        }

        file.AccessLevel = publish ? NcfFileAccessLevel.Public : NcfFileAccessLevel.Private;
        await SaveObjectAsync(file);
    }

    public async Task DeleteFileAsync(int id)
    {
        var file = await GetObjectAsync(z => z.Id == id);
        if (file == null)
        {
            return;
        }

        // Consumers own their references. Run their guards before moving the
        // physical file so a rejected deletion cannot leave a broken source.
        foreach (var guard in ServiceProvider.GetServices<INcfFileDeletionGuard>())
        {
            await guard.EnsureCanDeleteAsync(file);
        }

        var fullPath = ResolvePhysicalPath(file);
        string stagedPath = null;
        if (File.Exists(fullPath))
        {
            stagedPath = fullPath + $".deleting-{Guid.NewGuid():N}";
            File.Move(fullPath, stagedPath);
        }

        try
        {
            await DeleteObjectAsync(file);
            if (stagedPath != null && File.Exists(stagedPath))
            {
                File.Delete(stagedPath);
            }
        }
        catch
        {
            if (stagedPath != null && File.Exists(stagedPath) && !File.Exists(fullPath))
            {
                File.Move(stagedPath, fullPath);
            }
            throw;
        }
    }

    public async Task<(byte[] FileBytes, string FileName)> GetFileBytes(int id)
    {
        var file = await GetObjectAsync(z => z.Id == id);
        if (file == null)
        {
            return (Array.Empty<byte>(), "文件不存在！");
        }

        var fullPath = ResolvePhysicalPath(file);
        if (!File.Exists(fullPath))
        {
            return (Array.Empty<byte>(), "文件不存在！");
        }

        var bytes = await File.ReadAllBytesAsync(fullPath);
        return (bytes, file.FileName);
    }

    /// <summary>
    /// Opens a file only after its metadata and resource boundary have been
    /// validated. The caller owns the returned stream and must let ASP.NET Core
    /// dispose it after the response completes.
    /// </summary>
    public async Task<NcfFileReadResult> OpenReadAsync(int id, bool requirePublicSiteAsset = false)
    {
        var file = await GetObjectAsync(z => z.Id == id);
        if (file == null || (requirePublicSiteAsset &&
                             (file.ResourceScope != NcfFileResourceScope.SiteAsset ||
                              file.AccessLevel != NcfFileAccessLevel.Public)))
        {
            return null;
        }

        var fullPath = ResolvePhysicalPath(file);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return new NcfFileReadResult(file, stream);
    }

    /// <summary>
    /// Reads and extracts plain text only from knowledge-base sources.
    /// </summary>
    public async Task<NcfFileTextExtractionResult> GetExtractedTextAsync(int id)
    {
        var file = await GetObjectAsync(z => z.Id == id);
        if (file == null)
        {
            throw new FileNotFoundException($"文件记录不存在：{id}");
        }

        if (file.ResourceScope != NcfFileResourceScope.KnowledgeBase)
        {
            throw new InvalidOperationException("站点静态资源不能作为知识库来源。");
        }

        var fileInfo = await GetFileBytes(id);
        if (fileInfo.FileBytes.Length == 0)
        {
            throw new FileNotFoundException($"文件物理内容不存在：{file.FileName}");
        }

        return NcfFileTextExtractor.Extract(fileInfo.FileBytes, file.FileExtension, file.FileName);
    }

    public static string GetPublicAssetUrl(NcfFile file)
    {
        return file == null
            ? null
            : GetPublicAssetUrl(file.Id, file.ResourceScope, file.AccessLevel, file.ContentHash);
    }

    public static string GetPublicAssetUrl(NcfFileDto file)
    {
        return file == null
            ? null
            : GetPublicAssetUrl(file.Id, file.ResourceScope, file.AccessLevel, file.ContentHash);
    }

    private static string GetPublicAssetUrl(
        int id,
        NcfFileResourceScope resourceScope,
        NcfFileAccessLevel accessLevel,
        string contentHash)
    {
        if (resourceScope != NcfFileResourceScope.SiteAsset ||
            accessLevel != NcfFileAccessLevel.Public ||
            string.IsNullOrWhiteSpace(contentHash) || contentHash.Length < 16)
        {
            return null;
        }

        return $"/assets/{id}/{contentHash[..16].ToLowerInvariant()}";
    }

    private async Task ValidateFolderAsync(int? folderId, NcfFileResourceScope resourceScope)
    {
        if (!folderId.HasValue)
        {
            return;
        }

        var folderService = ServiceProvider.GetRequiredService<NcfFolderService>();
        var folder = await folderService.GetObjectAsync(z => z.Id == folderId.Value);
        if (folder == null)
        {
            throw new InvalidOperationException($"目标文件夹不存在：{folderId.Value}");
        }

        if (folder.ResourceScope != resourceScope)
        {
            throw new InvalidOperationException("目标文件夹的资源用途与当前上传用途不一致。");
        }
    }

    private static async Task<string> CopyAndHashAsync(IFormFile file, string physicalPath)
    {
        using var sha256 = SHA256.Create();
        await using var output = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
        await using var hashStream = new CryptoStream(output, sha256, CryptoStreamMode.Write, leaveOpen: false);
        await file.CopyToAsync(hashStream);
        hashStream.FlushFinalBlock();
        return Convert.ToHexString(sha256.Hash!);
    }

    private string ResolvePhysicalPath(NcfFile file)
    {
        if (file == null || string.IsNullOrWhiteSpace(file.FilePath) ||
            string.IsNullOrWhiteSpace(file.StorageFileName) || string.IsNullOrWhiteSpace(file.FileExtension) ||
            !NcfFileResourcePolicy.IsValidScope(file.ResourceScope))
        {
            throw new InvalidOperationException("文件物理路径信息无效。");
        }

        var pathParts = file.FilePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var expectedRoot = NcfFileResourcePolicy.GetStorageRoot(file.ResourceScope);
        var isLegacyKnowledgeBasePath = file.ResourceScope == NcfFileResourceScope.KnowledgeBase && pathParts.Length == 2;
        var isScopedPath = pathParts.Length == 3 && string.Equals(pathParts[0], expectedRoot, StringComparison.Ordinal);
        var yearIndex = isScopedPath ? 1 : 0;

        if ((!isLegacyKnowledgeBasePath && !isScopedPath) ||
            pathParts[yearIndex].Length != 4 ||
            !int.TryParse(pathParts[yearIndex], out _) ||
            !int.TryParse(pathParts[yearIndex + 1], out var month) ||
            month is < 1 or > 12 ||
            !Guid.TryParseExact(file.StorageFileName, "N", out _) ||
            !NcfFileResourcePolicy.IsAllowedStoredExtension(file.ResourceScope, file.FileExtension))
        {
            throw new InvalidOperationException("文件物理路径元数据非法。");
        }

        var basePath = Path.GetFullPath(_baseFilePath);
        var fullPath = Path.GetFullPath(Path.Combine(
            basePath,
            file.FilePath.Replace('/', Path.DirectorySeparatorChar),
            file.StorageFileName + file.FileExtension));
        var expectedPrefix = basePath.EndsWith(Path.DirectorySeparatorChar)
            ? basePath
            : basePath + Path.DirectorySeparatorChar;

        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullPath.StartsWith(expectedPrefix, pathComparison))
        {
            throw new InvalidOperationException("文件物理路径越界。");
        }

        return fullPath;
    }

    private static void EnsureValidScope(NcfFileResourceScope resourceScope)
    {
        if (!NcfFileResourcePolicy.IsValidScope(resourceScope))
        {
            throw new ArgumentOutOfRangeException(nameof(resourceScope));
        }
    }

    private static FileType GetFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" or ".log" or ".md" or ".markdown" or ".csv" or ".tsv" or ".yaml" or ".yml" => FileType.Text,
            ".doc" or ".docx" => FileType.Word,
            ".ppt" or ".pptx" => FileType.PowerPoint,
            ".xls" or ".xlsx" => FileType.Excel,
            ".cs" or ".js" or ".html" or ".htm" or ".css" or ".xml" or ".json" or ".ts" or ".sql" => FileType.Code,
            _ => FileType.Other,
        };
    }
}
