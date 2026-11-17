/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatMemoryRequests.cs
    File description: AdminChatMemoryRequests related definitions and implementation

    Created by: Senparc - 20261103

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.OHS.Local.AppService;

public sealed class SaveAdminChatMemoryRequest
{
    public int SessionId { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32)]
    public string Revision { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32000)]
    public string Markdown { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(2000)]
    public string SystemMessage { get; set; }
}

public sealed class SetAdminChatMemoryRequest
{
    public int SessionId { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32)]
    public string Revision { get; set; }
    public bool Enabled { get; set; }
    public AdminChatMemoryMode? Mode { get; set; }
}

public sealed class EditAdminChatMemoryRequest
{
    public int SessionId { get; set; }
    [Required(AllowEmptyStrings = true), MaxLength(32)]
    public string Revision { get; set; }
    public AdminChatMemoryEdit Entry { get; set; }
}

public sealed record AdminChatSystemMessageView(string Revision, string SystemMessage);
