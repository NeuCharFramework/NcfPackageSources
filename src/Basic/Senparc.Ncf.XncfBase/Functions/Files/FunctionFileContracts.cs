/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FunctionFileContracts.cs
    文件功能描述：FunctionRender 文件引用与文件存储契约

----------------------------------------------------------------*/

using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Ncf.XncfBase.Functions.Files;

/// <summary>
/// A serializable reference to a file uploaded for a FunctionRender invocation.
/// The reference is safe to persist in JSON, Workflow definitions, and AI tool arguments.
/// </summary>
public sealed class FunctionFileReference
{
    public string FileId { get; set; } = string.Empty;

    public int TenantId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long Length { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// A file stream supplied to the Function file store.
/// </summary>
public sealed class FunctionFileUpload
{
    public Stream Content { get; init; } = null!;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";

    public long? Length { get; init; }
}

/// <summary>
/// An opened Function file. The caller owns and must dispose the returned stream.
/// </summary>
public sealed class FunctionFileContent : IAsyncDisposable
{
    public FunctionFileReference Reference { get; init; } = null!;

    public Stream Content { get; init; } = null!;

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public interface IFunctionFileStore
{
    Task<FunctionFileReference> SaveAsync(
        FunctionFileUpload upload,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FunctionFileReference>> SaveManyAsync(
        IReadOnlyCollection<FunctionFileUpload> uploads,
        CancellationToken cancellationToken = default);

    Task<FunctionFileReference?> GetAsync(
        string fileId,
        CancellationToken cancellationToken = default);

    Task<FunctionFileContent> OpenReadAsync(
        string fileId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string fileId,
        CancellationToken cancellationToken = default);
}

public sealed class FunctionFileStoreOptions
{
    public const string SectionName = "FunctionFiles";

    public string RootPath { get; set; } = Path.Combine(
        Path.GetTempPath(),
        "Senparc.Ncf",
        "FunctionFiles");

    public long MaxFileBytes { get; set; } = 64L * 1024 * 1024;

    public long MaxBatchBytes { get; set; } = 256L * 1024 * 1024;

    public int MaxFilesPerBatch { get; set; } = 16;

    public TimeSpan Retention { get; set; } = TimeSpan.FromHours(2);
}

/// <summary>
/// Default local file store. Hosts can replace this implementation with object storage
/// without changing FunctionRender request contracts. Implementations must preserve
/// TenantId isolation when they replace this store.
/// </summary>
public sealed class LocalFunctionFileStore : IFunctionFileStore
{
    private readonly FunctionFileStoreOptions _options;
    private readonly RequestTenantInfo _requestTenantInfo;

    public LocalFunctionFileStore(
        IOptions<FunctionFileStoreOptions> options,
        RequestTenantInfo requestTenantInfo = null)
    {
        _options = options?.Value ?? new FunctionFileStoreOptions();
        _requestTenantInfo = requestTenantInfo;
        if (string.IsNullOrWhiteSpace(_options.RootPath))
        {
            throw new InvalidOperationException("Function file storage root path is required.");
        }

        if (_options.MaxFileBytes <= 0 ||
            _options.MaxBatchBytes < _options.MaxFileBytes ||
            _options.MaxFilesPerBatch <= 0 ||
            _options.Retention <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Function file storage limits are invalid.");
        }

        Directory.CreateDirectory(_options.RootPath);
    }

    public async Task<FunctionFileReference> SaveAsync(
        FunctionFileUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        var result = await SaveManyAsync(new[] { upload }, cancellationToken).ConfigureAwait(false);
        return result[0];
    }

    public async Task<IReadOnlyList<FunctionFileReference>> SaveManyAsync(
        IReadOnlyCollection<FunctionFileUpload> uploads,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uploads);
        if (uploads.Count == 0)
        {
            throw new InvalidOperationException("At least one Function file is required.");
        }

        if (uploads.Count > _options.MaxFilesPerBatch)
        {
            throw new InvalidOperationException(
                $"A Function upload batch cannot contain more than {_options.MaxFilesPerBatch} files.");
        }

        var declaredTotal = 0L;
        foreach (var upload in uploads)
        {
            ArgumentNullException.ThrowIfNull(upload.Content);
            if (upload.Length is < 0)
            {
                throw new InvalidOperationException("Function file length is invalid.");
            }

            if (upload.Length.HasValue && upload.Length.Value > _options.MaxFileBytes)
            {
                throw new InvalidOperationException(
                    $"A Function file cannot exceed {_options.MaxFileBytes} bytes.");
            }

            declaredTotal = checked(declaredTotal + (upload.Length ?? 0));
            if (declaredTotal > _options.MaxBatchBytes)
            {
                throw new InvalidOperationException(
                    $"A Function upload batch cannot exceed {_options.MaxBatchBytes} bytes.");
            }
        }

        var references = new List<FunctionFileReference>(uploads.Count);
        var savedIds = new List<string>(uploads.Count);
        var tenantId = GetCurrentTenantId();
        var actualTotal = 0L;
        var completed = false;
        try
        {
            foreach (var upload in uploads)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var reference = await SaveOneAsync(upload, tenantId, cancellationToken).ConfigureAwait(false);
                references.Add(reference);
                savedIds.Add(reference.FileId);
                actualTotal = checked(actualTotal + reference.Length);
                if (actualTotal > _options.MaxBatchBytes)
                {
                    throw new InvalidOperationException(
                        $"A Function upload batch cannot exceed {_options.MaxBatchBytes} bytes.");
                }
            }

            completed = true;
            return references;
        }
        finally
        {
            if (!completed)
            {
                foreach (var fileId in savedIds)
                {
                    DeleteFiles(fileId, tenantId);
                }
            }
        }
    }

    public async Task<FunctionFileReference?> GetAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        var normalizedId = NormalizeFileId(fileId);
        var tenantId = GetCurrentTenantId();
        var metadataPath = GetMetadataPath(normalizedId, tenantId);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        await using var stream = new FileStream(
            metadataPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var reference = await JsonSerializer.DeserializeAsync<FunctionFileReference>(
                stream,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (reference == null || reference.TenantId != tenantId)
        {
            return null;
        }

        if (reference.ExpiresAtUtc <= DateTime.UtcNow)
        {
            DeleteFiles(normalizedId, tenantId);
            return null;
        }

        return reference;
    }

    public async Task<FunctionFileContent> OpenReadAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        var normalizedId = NormalizeFileId(fileId);
        var reference = await GetAsync(normalizedId, cancellationToken).ConfigureAwait(false)
                       ?? throw new FileNotFoundException("Function file was not found or has expired.", normalizedId);
        var contentPath = GetContentPath(normalizedId, reference.TenantId);
        var stream = new FileStream(
            contentPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return new FunctionFileContent
        {
            Reference = reference,
            Content = stream
        };
    }

    public async Task DeleteAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedId = NormalizeFileId(fileId);
        var tenantId = GetCurrentTenantId();
        var reference = await GetAsync(normalizedId, cancellationToken).ConfigureAwait(false);
        if (reference != null)
        {
            DeleteFiles(normalizedId, tenantId);
        }

    }

    private async Task<FunctionFileReference> SaveOneAsync(
        FunctionFileUpload upload,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var fileId = Guid.NewGuid().ToString("N");
        var contentPath = GetContentPath(fileId, tenantId);
        var metadataPath = GetMetadataPath(fileId, tenantId);
        var temporaryPath = contentPath + ".tmp";
        var length = 0L;
        var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var filesPersisted = false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(contentPath)!);
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = await upload.Content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    length = checked(length + read);
                    if (length > _options.MaxFileBytes)
                    {
                        throw new InvalidOperationException(
                            $"A Function file cannot exceed {_options.MaxFileBytes} bytes.");
                    }

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    hash.AppendData(buffer, 0, read);
                }
            }

            if (upload.Length.HasValue && upload.Length.Value != length)
            {
                throw new InvalidOperationException("Function file length does not match Content-Length.");
            }

            var reference = new FunctionFileReference
            {
                FileId = fileId,
                TenantId = tenantId,
                FileName = NormalizeFileName(upload.FileName),
                ContentType = string.IsNullOrWhiteSpace(upload.ContentType)
                    ? "application/octet-stream"
                    : upload.ContentType.Trim(),
                Length = length,
                Sha256 = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
                ExpiresAtUtc = DateTime.UtcNow.Add(_options.Retention)
            };

            File.Move(temporaryPath, contentPath);
            var metadataTemporaryPath = metadataPath + ".tmp";
            try
            {
                await using (var metadata = new FileStream(
                    metadataTemporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await JsonSerializer.SerializeAsync(metadata, reference, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                File.Move(metadataTemporaryPath, metadataPath);
                filesPersisted = true;
            }
            finally
            {
                if (File.Exists(metadataTemporaryPath))
                {
                    File.Delete(metadataTemporaryPath);
                }
            }

            return reference;
        }
        finally
        {
            hash.Dispose();
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            if (!filesPersisted)
            {
                DeleteFiles(fileId, tenantId);
            }
        }
    }

    private string GetContentPath(string fileId, int tenantId) =>
        Path.Combine(_options.RootPath, tenantId.ToString(CultureInfo.InvariantCulture), fileId + ".bin");

    private string GetMetadataPath(string fileId, int tenantId) =>
        Path.Combine(_options.RootPath, tenantId.ToString(CultureInfo.InvariantCulture), fileId + ".json");

    private void DeleteFiles(string fileId, int tenantId)
    {
        var contentPath = GetContentPath(fileId, tenantId);
        var metadataPath = GetMetadataPath(fileId, tenantId);
        if (File.Exists(contentPath))
        {
            File.Delete(contentPath);
        }

        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }
    }

    private int GetCurrentTenantId()
    {
        if (_requestTenantInfo == null)
        {
            return 0;
        }

        if (SiteConfig.SenparcCoreSetting.EnableMultiTenant &&
            (!_requestTenantInfo.MatchSuccess || _requestTenantInfo.Id <= 0))
        {
            throw new NcfTenantException(
                "Function file operations require a successfully resolved tenant scope.");
        }

        return _requestTenantInfo.Id;
    }

    private static string NormalizeFileName(string fileName)
    {
        var normalized = Path.GetFileName((fileName ?? string.Empty).Trim());
        return string.IsNullOrWhiteSpace(normalized) ? "upload.bin" : normalized;
    }

    private static string NormalizeFileId(string fileId)
    {
        var normalized = (fileId ?? string.Empty).Trim();
        if (normalized.Length != 32 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException("Function file ID is invalid.");
        }

        return normalized.ToLowerInvariant();
    }
}
