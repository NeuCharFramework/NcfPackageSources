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

    public static bool IsOwnedBy(WorkspaceDescriptor workspace, int ownerId) =>
        workspace != null
        && ownerId > 0
        && !string.IsNullOrWhiteSpace(workspace.Id)
        && workspace.OwnerId == ownerId;
}
