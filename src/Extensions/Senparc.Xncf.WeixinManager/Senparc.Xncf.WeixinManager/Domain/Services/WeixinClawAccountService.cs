/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawAccountService.cs
    File description: WeixinClawAccountService.cs implementation and project behavior.

    Created by: Senparc - 20260920

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add comprehensive tests for MCP endpoint management and background tenant scope

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Services;

public class WeixinClawAccountService : ServiceBase<WeixinClawAccount>, IServiceBase<WeixinClawAccount>
{
    private readonly IDataProtector _tokenProtector;
    private readonly IDataProtector _contextTokenProtector;

    /// <summary>
    /// Initializes Weixin Claw account persistence and data-protection services.
    /// </summary>
    /// <param name="repo">Repository for account records.</param>
    /// <param name="serviceProvider">Dependency-injection provider.</param>
    /// <param name="dataProtectionProvider">Provider used to protect bot/context tokens.</param>
    public WeixinClawAccountService(
        IRepositoryBase<WeixinClawAccount> repo,
        IServiceProvider serviceProvider,
        IDataProtectionProvider dataProtectionProvider) : base(repo, serviceProvider)
    {
        _tokenProtector = dataProtectionProvider.CreateProtector(
            "Senparc.Xncf.WeixinManager.WeixinClaw.BotToken.v1");
        _contextTokenProtector = dataProtectionProvider.CreateProtector(
            "Senparc.Xncf.WeixinManager.WeixinClaw.ContextToken.v1");
    }

    /// <summary>
    /// Retrieves all Weixin Claw account records as DTOs.
    /// </summary>
    /// <returns>Account DTOs ordered by identifier.</returns>
    public async Task<List<WeixinClawAccountDto>> GetDtosAsync()
    {
        var accounts = await GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending).ConfigureAwait(false);
        return accounts.Select(ToDto).ToList();
    }

    /// <summary>
    /// Retrieves enabled tenant/account pairs that have protected bot tokens for polling.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    /// <returns>Tenant/account pairs eligible for background polling.</returns>
    public async Task<List<(int TenantId, int AccountId)>> GetPollingAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await RepositoryBase.GeAll(z => z.Id, OrderingType.Ascending)
            .AsNoTracking()
            .Where(z => z.Enabled && !string.IsNullOrWhiteSpace(z.BotTokenProtected))
            .Select(z => new { z.TenantId, z.Id })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return accounts.Select(z => (z.TenantId, z.Id)).ToList();
    }

    /// <summary>
    /// Maps account persistence data to a DTO without exposing protected token values.
    /// </summary>
    /// <param name="account">The account entity.</param>
    /// <returns>The account DTO.</returns>
    public WeixinClawAccountDto ToDto(WeixinClawAccount account)
    {
        return new WeixinClawAccountDto
        {
            Id = account.Id,
            Name = account.Name,
            BaseUrl = account.BaseUrl,
            IlinkBotId = account.IlinkBotId,
            IlinkUserId = account.IlinkUserId,
            PromptRangeCode = account.PromptRangeCode,
            Enabled = account.Enabled,
            Status = account.Status,
            LastError = account.LastError,
            LastMessageAt = account.LastMessageAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            LastConnectedAt = account.LastConnectedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            LastMessageFromUserId = account.LastMessageFromUserId,
            HasMessageContext = !string.IsNullOrWhiteSpace(account.LastMessageFromUserId)
                && !string.IsNullOrWhiteSpace(account.LastMessageContextTokenProtected)
        };
    }

    /// <summary>
    /// Protects a bot token for persistence.
    /// </summary>
    /// <param name="token">The plaintext bot token.</param>
    /// <returns>The protected token, or <see langword="null"/> for empty input.</returns>
    public string ProtectToken(string token)
    {
        return string.IsNullOrWhiteSpace(token)
            ? null
            : _tokenProtector.Protect(NormalizeToken(token));
    }

    /// <summary>
    /// Unprotects an account bot token; invalid protection data returns <see langword="null"/>.
    /// </summary>
    /// <param name="account">The account containing the protected token.</param>
    /// <returns>The normalized bot token, or <see langword="null"/>.</returns>
    public string UnprotectToken(WeixinClawAccount account)
    {
        if (account == null || string.IsNullOrWhiteSpace(account.BotTokenProtected))
        {
            return null;
        }

        try
        {
            return NormalizeToken(_tokenProtector.Unprotect(account.BotTokenProtected));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Protects the conversation context token for persistence.
    /// </summary>
    /// <param name="contextToken">The plaintext context token.</param>
    /// <returns>The protected token, or <see langword="null"/> for empty input.</returns>
    public string ProtectContextToken(string contextToken)
    {
        return string.IsNullOrWhiteSpace(contextToken)
            ? null
            : _contextTokenProtector.Protect(contextToken);
    }

    /// <summary>
    /// Unprotects the last stored account context token.
    /// </summary>
    /// <param name="account">The account containing the protected context token.</param>
    /// <returns>The context token, or <see langword="null"/> when unavailable.</returns>
    public string UnprotectContextToken(WeixinClawAccount account)
    {
        return UnprotectContextToken(account?.LastMessageContextTokenProtected);
    }

    /// <summary>
    /// Unprotects a persisted context token; invalid protection data returns <see langword="null"/>.
    /// </summary>
    /// <param name="protectedContextToken">The protected context token.</param>
    /// <returns>The plaintext context token, or <see langword="null"/>.</returns>
    public string UnprotectContextToken(string protectedContextToken)
    {
        if (string.IsNullOrWhiteSpace(protectedContextToken))
        {
            return null;
        }

        try
        {
            return _contextTokenProtector.Unprotect(protectedContextToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Trims a token and removes an optional Bearer scheme prefix.
    /// </summary>
    /// <param name="token">The stored or supplied token.</param>
    /// <returns>The normalized token value.</returns>
    private static string NormalizeToken(string token)
    {
        var value = token?.Trim();
        return value != null
            && value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? value["Bearer ".Length..].Trim()
            : value;
    }

    /// <summary>
    /// Creates or updates account settings, normalizing the base URL and protecting the bot token.
    /// </summary>
    /// <param name="dto">The account settings DTO.</param>
    /// <returns>The persisted account entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when updating a missing account or creating without a usable token.</exception>
    public async Task<WeixinClawAccount> SaveSettingsAsync(WeixinClawAccountDto dto)
    {
        WeixinClawAccount account;
        var baseUrl = WeixinClaw.WeixinClawApi.NormalizeBaseUrl(dto.BaseUrl);
        if (dto.Id > 0)
        {
            account = await GetObjectAsync(z => z.Id == dto.Id).ConfigureAwait(false);
            if (account == null)
            {
                throw new InvalidOperationException("个人微信账号不存在。");
            }

            account.UpdateSettings(
                dto.Name?.Trim(),
                baseUrl,
                ProtectToken(dto.BotToken),
                dto.PromptRangeCode?.Trim(),
                dto.Enabled);
        }
        else
        {
            var protectedToken = ProtectToken(dto.BotToken);
            if (string.IsNullOrWhiteSpace(protectedToken))
            {
                throw new InvalidOperationException("新建个人微信账号必须提供 bot token，或使用扫码连接。");
            }

            account = new WeixinClawAccount(
                dto.Name?.Trim(),
                baseUrl,
                protectedToken,
                dto.PromptRangeCode?.Trim());
        }

        await SaveObjectAsync(account).ConfigureAwait(false);
        return account;
    }
}
