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
