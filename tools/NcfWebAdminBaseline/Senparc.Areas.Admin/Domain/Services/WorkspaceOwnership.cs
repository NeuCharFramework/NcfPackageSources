/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceOwnership.cs
    File description: Related implementation.

    Created by: Senparc - 20261110

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Domain.Services;

public static class WorkspaceOwnership
{
    /// <summary>
    /// Filters workspace descriptors to those owned by a positive administrator identifier.
    /// </summary>
    /// <param name="workspaces">The candidate workspace descriptors.</param>
    /// <param name="ownerId">The administrator owner identifier.</param>
    /// <returns>Owned workspace descriptors, or an empty list for invalid input.</returns>
    public static IReadOnlyList<WorkspaceDescriptor> SelectOwned(
        IEnumerable<WorkspaceDescriptor> workspaces,
        int ownerId)
    {
        if (ownerId <= 0 || workspaces == null)
        {
            return Array.Empty<WorkspaceDescriptor>();
        }

        return workspaces
            .Where(workspace => IsOwnedBy(workspace, ownerId))
            .ToArray();
    }

    /// <summary>
    /// Determines whether a workspace has a valid identifier and belongs to the owner.
    /// </summary>
    /// <param name="workspace">The workspace descriptor.</param>
    /// <param name="ownerId">The expected owner identifier.</param>
    /// <returns><see langword="true"/> when ownership is valid.</returns>
    public static bool IsOwnedBy(WorkspaceDescriptor workspace, int ownerId) =>
        workspace != null
        && ownerId > 0
        && !string.IsNullOrWhiteSpace(workspace.Id)
        && workspace.OwnerId == ownerId;
}
