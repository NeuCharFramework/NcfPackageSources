using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Abstractions;

public static class FileManagerModuleContract
{
    public const string ModuleUid = "BC7769FE-E094-4EAF-9B1F-D82670D1D691";
}

public enum FileManagerResourceKind
{
    KnowledgeBase = 100,
    SiteAsset = 200
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
