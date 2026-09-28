using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Services;

public sealed class WeixinClawBindingProfileService
    : ServiceBase<WeixinClawBindingProfile>,
      IServiceBase<WeixinClawBindingProfile>,
      IWeixinClawBindingProfileResolver
{
    public WeixinClawBindingProfileService(
        IRepositoryBase<WeixinClawBindingProfile> repo,
        IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
    }

    public async Task<List<WeixinClawBindingProfileDto>> GetDtosAsync(int accountId)
    {
        var list = await GetFullListAsync(
            item => item.WeixinClawAccountId == accountId,
            "Id ASC").ConfigureAwait(false);
        return list.Select(ToDto).ToList();
    }

    public async Task<List<WeixinClawBindingProfileDto>> GetAllDtosAsync()
    {
        var list = await GetFullListAsync(item => true, "Id ASC").ConfigureAwait(false);
        return list.Select(ToDto).ToList();
    }

    public async Task<WeixinClawBindingProfileDto> SaveAsync(
        WeixinClawBindingProfileDto dto)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }
        if (dto.AccountId <= 0)
        {
            throw new ArgumentException("微信账号 Id 无效。", nameof(dto));
        }
        if (dto.AdminUserId <= 0)
        {
            throw new ArgumentException("AdminUserId 必须大于 0。", nameof(dto));
        }
        if (dto.Id <= 0 && string.IsNullOrWhiteSpace(dto.BindingCode))
        {
            throw new ArgumentException("新建绑定配置必须填写绑定码。", nameof(dto));
        }

        var codeHash = HashCode(dto.BindingCode);
        WeixinClawBindingProfile profile;
        if (dto.Id > 0)
        {
            profile = await GetObjectAsync(item =>
                item.Id == dto.Id
                && item.WeixinClawAccountId == dto.AccountId).ConfigureAwait(false);
            if (profile == null)
            {
                throw new InvalidOperationException("绑定配置不存在。");
            }

            profile.Update(
                dto.Name,
                dto.AdminUserId,
                codeHash,
                dto.WorkflowId,
                dto.AiModelId,
                dto.Mode,
                dto.EnableNeuBell,
                dto.EnableWorkflow,
                dto.Enabled);
        }
        else
        {
            profile = new WeixinClawBindingProfile(
                dto.AccountId,
                dto.Name,
                dto.AdminUserId,
                codeHash,
                dto.WorkflowId,
                dto.AiModelId,
                dto.Mode,
                dto.EnableNeuBell,
                dto.EnableWorkflow);
        }

        await SaveObjectAsync(profile).ConfigureAwait(false);
        return ToDto(profile);
    }

    public async Task<WeixinClawBindingProfileResolution> ResolveAsync(
        int accountId,
        string bindingCode,
        CancellationToken cancellationToken = default)
    {
        if (accountId <= 0 || string.IsNullOrWhiteSpace(bindingCode))
        {
            return null;
        }

        var hash = HashCode(bindingCode);
        var profile = await GetObjectAsync(item =>
            item.WeixinClawAccountId == accountId
            && item.BindingCodeHash == hash
            && item.Enabled).ConfigureAwait(false);
        return profile == null ? null : ToResolverDto(profile);
    }

    private static string HashCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static WeixinClawBindingProfileDto ToDto(WeixinClawBindingProfile profile)
    {
        return new WeixinClawBindingProfileDto
        {
            Id = profile.Id,
            AccountId = profile.WeixinClawAccountId,
            Name = profile.Name,
            AdminUserId = profile.AdminUserId,
            WorkflowId = profile.WorkflowId,
            AiModelId = profile.AiModelId,
            Mode = profile.Mode,
            EnableNeuBell = profile.EnableNeuBell,
            EnableWorkflow = profile.EnableWorkflow,
            Enabled = profile.Enabled,
            HasBindingCode = !string.IsNullOrWhiteSpace(profile.BindingCodeHash)
        };
    }

    private static WeixinClawBindingProfileResolution ToResolverDto(WeixinClawBindingProfile profile)
    {
        return new WeixinClawBindingProfileResolution
        {
            Id = profile.Id,
            AccountId = profile.WeixinClawAccountId,
            Name = profile.Name,
            AdminUserId = profile.AdminUserId,
            WorkflowId = profile.WorkflowId,
            AiModelId = profile.AiModelId,
            Mode = profile.Mode,
            EnableNeuBell = profile.EnableNeuBell,
            EnableWorkflow = profile.EnableWorkflow
        };
    }
}
