/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: INcfFileDeletionGuard.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Domain.Services;

/// <summary>
/// Optional cross-module guard for a file deletion. FileManager owns the file
/// lifecycle, while consumers such as KnowledgeBase can register a guard to
/// prevent removal of a source that is still referenced.
/// </summary>
public interface INcfFileDeletionGuard
{
    Task EnsureCanDeleteAsync(NcfFile file);
}
