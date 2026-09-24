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
}
