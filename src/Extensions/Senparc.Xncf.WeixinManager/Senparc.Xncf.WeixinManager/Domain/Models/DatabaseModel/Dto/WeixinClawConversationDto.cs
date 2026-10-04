/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawConversationDto.cs
    文件功能描述：WeixinClawConversationDto.cs implementation and project behavior.


    创建标识：Senparc - 20260930

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;

public sealed class WeixinClawConversationDto
{
    public string PeerUserId { get; set; }
    public string LastText { get; set; }
    public string LastStatus { get; set; }
    public DateTime LastCreatedAt { get; set; }
    public int MessageCount { get; set; }
}
