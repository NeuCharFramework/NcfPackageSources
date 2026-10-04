/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawAccountDto.cs
    文件功能描述：WeixinClawAccountDto.cs implementation and project behavior.


    创建标识：Senparc - 20260920

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
