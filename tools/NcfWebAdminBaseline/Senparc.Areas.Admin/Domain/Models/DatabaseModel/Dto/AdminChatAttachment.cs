/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatAttachment.cs
    File description: AdminChatAttachment related definitions and implementation

    Created by: Senparc - 20261026

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261125
    Change description: v0.12.29 Improved AdminChat conversation loading and command parameter feedback.

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;

public sealed class AdminChatAttachmentInput
{
    [Required, MaxLength(300)]
    public string RelativePath { get; set; } = "";
    [Required, MaxLength(150)]
    public string ETag { get; set; } = "";
    [MaxLength(150)]
    public string VisionETag { get; set; } = "";
    public bool UseVision { get; set; }
}

public sealed class AdminChatAttachment
{
    public string RelativePath { get; set; } = "";
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string ETag { get; set; } = "";
    public string VisionETag { get; set; } = "";
    public bool UseVision { get; set; }
    public bool CanUseVision => !string.IsNullOrEmpty(VisionETag);
    public bool IsWorkspaceArtifact { get; set; }
    public string ChangeType { get; set; } = "";
    public string ActionType { get; set; } = "";
    public int ActionTargetId { get; set; }
    public string PreviewKind { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string LocalStatus { get; set; } = "";
    public int SectionCount { get; set; }
    public int ExtractedCharacters { get; set; }
    public string Notice { get; set; } = "";
}
