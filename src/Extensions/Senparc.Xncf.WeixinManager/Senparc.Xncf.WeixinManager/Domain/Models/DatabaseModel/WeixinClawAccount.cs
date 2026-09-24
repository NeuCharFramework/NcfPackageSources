using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(WeixinClawAccount))]
[Serializable]
public class WeixinClawAccount : EntityBase<int>
{
    [Required, MaxLength(100)]
    public string Name { get; private set; }

    [Required, MaxLength(300)]
    public string BaseUrl { get; private set; }

    [MaxLength(4096)]
    public string BotTokenProtected { get; private set; }

    [MaxLength(200)]
    public string IlinkBotId { get; private set; }

    [MaxLength(200)]
    public string IlinkUserId { get; private set; }

    [MaxLength(100000)]
    public string GetUpdatesBuf { get; private set; }

    [MaxLength(100)]
    public string PromptRangeCode { get; private set; }

    public bool Enabled { get; private set; }

    [Required, MaxLength(50)]
    public string Status { get; private set; }

    [MaxLength(2000)]
    public string LastError { get; private set; }

    public DateTime? LastMessageAt { get; private set; }

    public DateTime? LastConnectedAt { get; private set; }

    private WeixinClawAccount()
    {
    }

    public WeixinClawAccount(string name, string baseUrl, string botTokenProtected, string promptRangeCode)
    {
        Name = name;
        BaseUrl = baseUrl;
        BotTokenProtected = botTokenProtected;
        PromptRangeCode = promptRangeCode;
        Enabled = true;
        Status = "未连接";
        SetUpdateTime();
    }

    public void UpdateSettings(string name, string baseUrl, string botTokenProtected, string promptRangeCode, bool enabled)
    {
        Name = name;
        BaseUrl = baseUrl;
        if (!string.IsNullOrWhiteSpace(botTokenProtected))
        {
            BotTokenProtected = botTokenProtected;
        }
        PromptRangeCode = promptRangeCode;
        Enabled = enabled;
        if (!enabled)
        {
            Status = "已停用";
        }
        SetUpdateTime();
    }

    public void SetAuthenticated(string botTokenProtected, string botId, string userId, string baseUrl)
    {
        BotTokenProtected = botTokenProtected;
        IlinkBotId = botId;
        IlinkUserId = userId;
        BaseUrl = baseUrl;
        Enabled = true;
        Status = "已连接";
        LastError = null;
        LastConnectedAt = DateTime.UtcNow;
        SetUpdateTime();
    }

    public void SetCursor(string getUpdatesBuf)
    {
        GetUpdatesBuf = getUpdatesBuf;
        SetUpdateTime();
    }

    public void MarkMessageReceived()
    {
        LastMessageAt = DateTime.UtcNow;
        LastError = null;
        Status = "运行中";
        SetUpdateTime();
    }

    public void MarkRunning()
    {
        LastError = null;
        Status = "运行中";
        SetUpdateTime();
    }

    public void MarkError(string message)
    {
        LastError = message?.Length > 2000 ? message[..2000] : message;
        Status = "错误";
        SetUpdateTime();
    }
}
