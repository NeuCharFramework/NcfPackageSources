/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileManagerContracts.cs
    File description: FileManagerContracts.cs implementation and project behavior.

    Created by: Senparc - 20260930

    Modified by: Senparc - 20261005
    Change description: v0.1.0 0.1.0 feat: enhance file management capabilities with new upload stream method and service registration

    Modified by: Senparc - 20261021
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.2.0 add FileId reference to AIFineTuningDataset and implement FineTuningFileReferenceProvider

    Modified by: Senparc - 20261119
    Change description: v0.2.1 Localized file management and upload surfaces

    Modified by: Senparc - 20261127
    Change description: v0.2.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.FileManager.Abstractions;

public static class FileManagerModuleContract
{
    /// <summary>
    /// Gets the stable UID used to identify the FileManager module across integration boundaries.
    /// </summary>
    public const string ModuleUid = "BC7769FE-E094-4EAF-9B1F-D82670D1D691";
}

/// <summary>
/// Classifies the owning feature and retention policy of a managed file.
/// </summary>
public enum FileManagerResourceKind
{
    KnowledgeBase = 100,
    SiteAsset = 200,
    PrivateAttachment = 300,
    FineTuning = 400,
    Skill = 500
}

/// <summary>
/// Describes a stream and metadata submitted to the FileManager import boundary.
/// </summary>
/// <param name="Content">The readable stream containing the file bytes.</param>
/// <param name="FileName">The client-provided file name.</param>
/// <param name="ContentType">The content type to persist with the file.</param>
/// <param name="Length">The declared byte length of <paramref name="Content"/>.</param>
/// <param name="Description">Optional human-readable description.</param>
/// <param name="ResourceKind">The feature owning the imported file.</param>
public sealed record FileManagerImportRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string? Description = null,
    FileManagerResourceKind ResourceKind = FileManagerResourceKind.SiteAsset);

/// <summary>
/// Reports the persisted identity and metadata returned after a FileManager import.
/// </summary>
/// <param name="FileId">The managed file identifier.</param>
/// <param name="FileName">The persisted file name.</param>
/// <param name="ContentType">The persisted content type.</param>
/// <param name="Length">The persisted byte length.</param>
/// <param name="ResourceKind">The persisted resource-kind value.</param>
public sealed record FileManagerImportResult(
    int FileId,
    string FileName,
    string ContentType,
    long Length,
    string ResourceKind);

/// <summary>
/// Contains metadata and an owned readable stream returned by a FileManager read operation.
/// </summary>
/// <param name="FileId">The managed file identifier.</param>
/// <param name="FileName">The persisted file name.</param>
/// <param name="ContentType">The persisted content type.</param>
/// <param name="Length">The byte length of <paramref name="Content"/>.</param>
/// <param name="Content">The readable stream; the caller owns its disposal.</param>
public sealed record FileManagerReadResult(
    int FileId,
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

/// <summary>
/// Identifies one external object that references a managed file.
/// </summary>
/// <param name="FileId">The managed file identifier.</param>
/// <param name="ReferenceType">The kind of external reference.</param>
/// <param name="ReferenceId">The external object's identifier.</param>
/// <param name="Description">A human-readable explanation of the reference.</param>
public sealed record FileManagerReference(
    int FileId,
    string ReferenceType,
    string ReferenceId,
    string Description);

/// <summary>
/// Reports a managed file and all references currently known to prevent or explain deletion.
/// </summary>
/// <param name="FileId">The managed file identifier.</param>
/// <param name="FileName">The persisted file name.</param>
/// <param name="Length">The file length in bytes.</param>
/// <param name="ResourceKind">The owning resource kind.</param>
/// <param name="PhysicalState">The current storage state.</param>
/// <param name="References">The external references associated with the file.</param>
public sealed record FileManagerReferenceReport(
    int FileId,
    string FileName,
    long Length,
    string ResourceKind,
    string PhysicalState,
    IReadOnlyList<FileManagerReference> References);

/// <summary>
/// Provides external references that prevent or explain deletion of a managed file.
/// </summary>
public interface IFileManagerReferenceProvider
{
    /// <summary>
    /// Finds references to a managed file.
    /// </summary>
    /// <param name="fileId">The managed file identifier.</param>
    /// <param name="cancellationToken">Token used to cancel reference lookup.</param>
    /// <returns>A task containing the references currently known for the file.</returns>
    Task<IReadOnlyList<FileManagerReference>> GetReferencesAsync(
        int fileId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the cross-module file import, read, availability, and deletion boundary.
/// </summary>
public interface IFileManagerGateway
{
    /// <summary>
    /// Checks whether the file manager can accept operations.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the availability check.</param>
    /// <returns>A task containing whether the gateway is available.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a file stream into the file manager.
    /// </summary>
    /// <param name="request">The stream, metadata, length, and resource kind to import.</param>
    /// <param name="cancellationToken">Token used to cancel import.</param>
    /// <returns>A task containing the persisted file identity and metadata.</returns>
    Task<FileManagerImportResult> ImportAsync(
        FileManagerImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream for a managed file.
    /// </summary>
    /// <param name="fileId">The managed file identifier.</param>
    /// <param name="cancellationToken">Token used to cancel lookup or stream preparation.</param>
    /// <returns>A task containing file metadata and an owned readable stream.</returns>
    Task<FileManagerReadResult> OpenReadAsync(
        int fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a managed file.
    /// </summary>
    /// <param name="fileId">The managed file identifier.</param>
    /// <param name="cancellationToken">Token used to cancel deletion.</param>
    Task DeleteAsync(
        int fileId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes an import request raised through the NCF integration event bus.
/// </summary>
/// <param name="TenantId">The tenant that owns the import.</param>
/// <param name="AccountId">The account that initiated the import.</param>
/// <param name="MessageId">The source message identifier.</param>
/// <param name="StorageKey">The source storage key.</param>
/// <param name="Content">The imported file bytes.</param>
/// <param name="FileName">The source file name.</param>
/// <param name="ContentType">The source content type.</param>
/// <param name="Description">The import description.</param>
/// <param name="ResourceKind">The feature owning the imported file.</param>
public sealed record FileManagerImportRequestedEvent(
    int TenantId,
    int AccountId,
    string MessageId,
    string StorageKey,
    byte[] Content,
    string FileName,
    string ContentType,
    string Description,
    FileManagerResourceKind ResourceKind = FileManagerResourceKind.PrivateAttachment)
    : IntegrationRequest<FileManagerImportCompletedEvent>;

/// <summary>
/// Reports the result of a FileManager import request on the integration event bus.
/// </summary>
/// <param name="RequestId">The original integration request identifier.</param>
/// <param name="TenantId">The tenant that owns the import.</param>
/// <param name="AccountId">The account that initiated the import.</param>
/// <param name="MessageId">The source message identifier.</param>
/// <param name="StorageKey">The source storage key.</param>
/// <param name="Success">Whether the import completed successfully.</param>
/// <param name="ErrorMessage">The failure message, if any.</param>
/// <param name="Result">The persisted file result.</param>
public sealed record FileManagerImportCompletedEvent(
    Guid RequestId,
    int TenantId,
    int AccountId,
    string MessageId,
    string StorageKey,
    bool Success,
    string ErrorMessage,
    FileManagerImportResult Result)
    : IntegrationResponse(RequestId);
