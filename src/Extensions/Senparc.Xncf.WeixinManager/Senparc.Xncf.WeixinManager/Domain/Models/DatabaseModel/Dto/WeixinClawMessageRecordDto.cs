using System;
using System.Collections.Generic;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;

public sealed class WeixinClawMessageRecordDto
{
    public int Id { get; set; }
    public string Direction { get; set; }
    public string MessageId { get; set; }
    public long? Seq { get; set; }
    public string FromUserId { get; set; }
    public string ToUserId { get; set; }
    public string Status { get; set; }
    public int MessageType { get; set; }
    public string Text { get; set; }
    public string Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WeixinClawMessageMediaDto> MediaItems { get; set; } = new();
}

public sealed class WeixinClawMessageMediaDto
{
    public string Kind { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string Url { get; set; }
    public string PlaybackUrl { get; set; }
    public string Error { get; set; }
}
