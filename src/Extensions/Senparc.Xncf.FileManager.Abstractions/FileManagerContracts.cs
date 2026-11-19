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
    public const string ModuleUid = "BC7769FE-E094-4EAF-9B1F-D82670D1D691";
}

public enum FileManagerResourceKind
{
    KnowledgeBase = 100,
    SiteAsset = 200,
    PrivateAttachment = 300,
    FineTuning = 400,
    Skill = 500
}

public sealed record FileManagerImportRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string? Description = null,
    FileManagerResourceKind ResourceKind = FileManagerResourceKind.SiteAsset);

public sealed record FileManagerImportResult(
    int FileId,
    string FileName,
    string ContentType,
    long Length,
    string ResourceKind);

public sealed record FileManagerReadResult(
    int FileId,
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

public sealed record FileManagerReference(
    int FileId,
    string ReferenceType,
    string ReferenceId,
    string Description);

public sealed record FileManagerReferenceReport(
    int FileId,
    string FileName,
    long Length,
    string ResourceKind,
    string PhysicalState,
    IReadOnlyList<FileManagerReference> References);

public interface IFileManagerReferenceProvider
{
    Task<IReadOnlyList<FileManagerReference>> GetReferencesAsync(
        int fileId,
        CancellationToken cancellationToken = default);
}

public interface IFileManagerGateway
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<FileManagerImportResult> ImportAsync(
        FileManagerImportRequest request,
        CancellationToken cancellationToken = default);

    Task<FileManagerReadResult> OpenReadAsync(
        int fileId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int fileId,
        CancellationToken cancellationToken = default);
}

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
