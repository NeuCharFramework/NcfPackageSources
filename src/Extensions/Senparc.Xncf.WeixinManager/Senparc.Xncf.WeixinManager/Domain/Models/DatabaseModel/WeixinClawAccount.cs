/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawAccount.cs
    File description: WeixinClawAccount.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw message handling and UI features

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

[Table(Register.DATABASE_PREFIX + nameof(WeixinClawAccount))]
[Serializable]
/// <summary>
/// Persists a connected Weixin Claw account, protected credentials, polling cursor,
/// conversation context, and runtime status.
/// </summary>
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

    [MaxLength(300)]
    public string LastMessageFromUserId { get; private set; }

    [MaxLength(4096)]
    public string LastMessageContextTokenProtected { get; private set; }

    public DateTime? LastMessageAt { get; private set; }

    public DateTime? LastConnectedAt { get; private set; }

    /// <summary>
    /// Reserved for EF Core materialization.
    /// </summary>
    private WeixinClawAccount()
    {
    }

    /// <summary>
    /// Creates a new enabled account with protected bot token settings.
    /// </summary>
    /// <param name="name">The account display name.</param>
    /// <param name="baseUrl">The normalized iLink base URL.</param>
    /// <param name="botTokenProtected">The protected bot token.</param>
    /// <param name="promptRangeCode">Optional PromptRange binding.</param>
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

    /// <summary>
    /// Updates account connection settings and enablement without replacing an omitted token.
    /// </summary>
    /// <param name="name">The account display name.</param>
    /// <param name="baseUrl">The normalized iLink base URL.</param>
    /// <param name="botTokenProtected">The optional protected bot token.</param>
    /// <param name="promptRangeCode">Optional PromptRange binding.</param>
    /// <param name="enabled">Whether polling is enabled.</param>
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

    /// <summary>
    /// Stores successful QR-login identity and marks the account connected.
    /// </summary>
    /// <param name="botTokenProtected">The protected bot token.</param>
    /// <param name="botId">The iLink bot identifier.</param>
    /// <param name="userId">The iLink user identifier.</param>
    /// <param name="baseUrl">The effective API base URL.</param>
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

    /// <summary>
    /// Stores the protocol update cursor for the next long-poll request.
    /// </summary>
    /// <param name="getUpdatesBuf">The protocol cursor.</param>
    public void SetCursor(string getUpdatesBuf)
    {
        GetUpdatesBuf = getUpdatesBuf;
        SetUpdateTime();
    }

    /// <summary>
    /// Records the latest inbound conversation identity and marks the account running.
    /// </summary>
    /// <param name="fromUserId">The sender user identifier.</param>
    /// <param name="contextTokenProtected">The protected conversation context token.</param>
    public void MarkMessageReceived(string fromUserId = null, string contextTokenProtected = null)
    {
        LastMessageFromUserId = fromUserId;
        LastMessageContextTokenProtected = contextTokenProtected;
        LastMessageAt = DateTime.UtcNow;
        LastError = null;
        Status = "运行中";
        SetUpdateTime();
    }

    /// <summary>
    /// Marks the account polling loop healthy.
    /// </summary>
    public void MarkRunning()
    {
        LastError = null;
        Status = "运行中";
        SetUpdateTime();
    }

    /// <summary>
    /// Marks the account in error state with a bounded error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public void MarkError(string message)
    {
        LastError = message?.Length > 2000 ? message[..2000] : message;
        Status = "错误";
        SetUpdateTime();
    }
}
