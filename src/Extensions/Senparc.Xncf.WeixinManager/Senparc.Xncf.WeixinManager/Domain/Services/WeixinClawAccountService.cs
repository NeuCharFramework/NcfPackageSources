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

    public async Task<List<WeixinClawAccountDto>> GetDtosAsync()
    {
        var accounts = await GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending).ConfigureAwait(false);
        return accounts.Select(ToDto).ToList();
    }

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

    public string ProtectToken(string token)
    {
        return string.IsNullOrWhiteSpace(token)
            ? null
            : _tokenProtector.Protect(NormalizeToken(token));
    }

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

    public string ProtectContextToken(string contextToken)
    {
        return string.IsNullOrWhiteSpace(contextToken)
            ? null
            : _contextTokenProtector.Protect(contextToken);
    }

    public string UnprotectContextToken(WeixinClawAccount account)
    {
        return UnprotectContextToken(account?.LastMessageContextTokenProtected);
    }

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

    private static string NormalizeToken(string token)
    {
        var value = token?.Trim();
        return value != null
            && value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? value["Bearer ".Length..].Trim()
            : value;
    }

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
