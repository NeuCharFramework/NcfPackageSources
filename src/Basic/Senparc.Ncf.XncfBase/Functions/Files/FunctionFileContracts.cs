/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionFileContracts.cs
    File description: Related implementation.

    Created by: Senparc - 20261101

    Modified by: Senparc - 20261103
    Change description: v0.28.1 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

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

    /// <summary>
    /// Disposes the caller-owned file stream.
    /// </summary>
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

    /// <summary>
    /// Initializes the default local function-file store and validates its storage limits.
    /// </summary>
    /// <param name="options">The file-store size, retention, and root-path options.</param>
    /// <param name="requestTenantInfo">The optional request tenant context.</param>
    /// <exception cref="InvalidOperationException">Thrown when the root path or limits are invalid.</exception>
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

    /// <summary>
    /// Saves one uploaded Function file under the current tenant boundary.
    /// </summary>
    /// <param name="upload">The readable file upload.</param>
    /// <param name="cancellationToken">Token used to cancel storage.</param>
    /// <returns>A task containing persisted metadata and hash.</returns>
    public async Task<FunctionFileReference> SaveAsync(
        FunctionFileUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        var result = await SaveManyAsync(new[] { upload }, cancellationToken).ConfigureAwait(false);
        return result[0];
    }

    /// <summary>
    /// Saves a bounded batch of uploads atomically enough to clean up partial files on failure.
    /// </summary>
    /// <param name="uploads">The file uploads.</param>
    /// <param name="cancellationToken">Token used to cancel storage.</param>
    /// <returns>A task containing persisted references.</returns>
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

    /// <summary>
    /// Gets non-expired file metadata for the current tenant.
    /// </summary>
    /// <param name="fileId">The 32-character file ID.</param>
    /// <param name="cancellationToken">Token used to cancel metadata reading.</param>
    /// <returns>A task containing metadata, or <see langword="null"/> when missing/expired.</returns>
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

    /// <summary>
    /// Opens a non-expired tenant-scoped file stream; the caller owns the returned stream.
    /// </summary>
    /// <param name="fileId">The 32-character file ID.</param>
    /// <param name="cancellationToken">Token used to cancel metadata lookup.</param>
    /// <returns>A task containing metadata and an open stream.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is missing or expired.</exception>
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

    /// <summary>
    /// Deletes the current-tenant file content and metadata when present.
    /// </summary>
    /// <param name="fileId">The 32-character file ID.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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

    /// <summary>
    /// Streams one upload to a temporary tenant-scoped file, computes SHA-256,
    /// and atomically persists content plus metadata.
    /// </summary>
    /// <param name="upload">The file upload.</param>
    /// <param name="tenantId">The resolved tenant identifier.</param>
    /// <param name="cancellationToken">Token used to cancel storage.</param>
    /// <returns>A task containing the persisted file reference.</returns>
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

    /// <summary>Builds the tenant-scoped content-file path.</summary>
    /// <param name="fileId">The normalized file ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The content path.</returns>
    private string GetContentPath(string fileId, int tenantId) =>
        Path.Combine(_options.RootPath, tenantId.ToString(CultureInfo.InvariantCulture), fileId + ".bin");

    /// <summary>Builds the tenant-scoped metadata-file path.</summary>
    /// <param name="fileId">The normalized file ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The metadata path.</returns>
    private string GetMetadataPath(string fileId, int tenantId) =>
        Path.Combine(_options.RootPath, tenantId.ToString(CultureInfo.InvariantCulture), fileId + ".json");

    /// <summary>Deletes tenant-scoped content and metadata files when present.</summary>
    /// <param name="fileId">The normalized file ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
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

    /// <summary>
    /// Resolves the current tenant ID and rejects unresolved tenant context in multi-tenant mode.
    /// </summary>
    /// <returns>The current tenant ID, or zero when tenant mode/context is not active.</returns>
    /// <exception cref="NcfTenantException">Thrown when a multi-tenant request has no resolved tenant.</exception>
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

    /// <summary>
    /// Removes path components from an uploaded file name and applies a safe fallback.
    /// </summary>
    /// <param name="fileName">The client-provided file name.</param>
    /// <returns>A path-free file name.</returns>
    private static string NormalizeFileName(string fileName)
    {
        var normalized = Path.GetFileName((fileName ?? string.Empty).Trim());
        return string.IsNullOrWhiteSpace(normalized) ? "upload.bin" : normalized;
    }

    /// <summary>
    /// Validates and normalizes a 32-character hexadecimal Function file ID.
    /// </summary>
    /// <param name="fileId">The file ID to normalize.</param>
    /// <returns>A lowercase normalized file ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ID format is invalid.</exception>
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
