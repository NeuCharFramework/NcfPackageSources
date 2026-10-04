/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FileManagerContracts.cs
    文件功能描述：FileManagerContracts.cs implementation and project behavior.


    创建标识：Senparc - 20260930

    修改标识：Senparc - 20261005
    修改描述：v0.1.0 0.1.0 feat: enhance file management capabilities with new upload stream method and service registration

----------------------------------------------------------------*/

using System;
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
    PrivateAttachment = 300
}

public sealed record FileManagerImportRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string Description = null,
    FileManagerResourceKind ResourceKind = FileManagerResourceKind.SiteAsset);

public sealed record FileManagerImportResult(
    int FileId,
    string FileName,
    string ContentType,
    long Length,
    string ResourceKind);

public interface IFileManagerGateway
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<FileManagerImportResult> ImportAsync(
        FileManagerImportRequest request,
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
