/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawAccountDto.cs
    File description: WeixinClawAccountDto.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw message handling and UI features

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;

public class WeixinClawAccountDto : DtoBase
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(300)]
    public string BaseUrl { get; set; }

    [MaxLength(4096)]
    public string BotToken { get; set; }

    [MaxLength(200)]
    public string IlinkBotId { get; set; }

    [MaxLength(200)]
    public string IlinkUserId { get; set; }

    [MaxLength(100)]
    public string PromptRangeCode { get; set; }

    public bool Enabled { get; set; } = true;

    public string Status { get; set; }
    public string LastError { get; set; }
    public string LastMessageAt { get; set; }
    public string LastConnectedAt { get; set; }
    public string LastMessageFromUserId { get; set; }
    public bool HasMessageContext { get; set; }
}
