/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawConversationDto.cs
    File description: WeixinClawConversationDto.cs implementation and project behavior.

    Created by: Senparc - 20260930

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

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
