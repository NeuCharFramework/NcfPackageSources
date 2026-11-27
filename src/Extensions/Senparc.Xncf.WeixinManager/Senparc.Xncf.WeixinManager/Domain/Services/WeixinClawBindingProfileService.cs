/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawBindingProfileService.cs
    File description: WeixinClawBindingProfileService.cs implementation and project behavior.

    Created by: Senparc - 20260928

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add WeixinClawBindingProfile management and UI enhancements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
    /// <summary>
    /// Initializes the WeixinClaw binding-profile persistence service.
    /// </summary>
    /// <param name="repo">The binding-profile repository.</param>
    /// <param name="serviceProvider">The dependency provider used by the base service.</param>
    public WeixinClawBindingProfileService(
        IRepositoryBase<WeixinClawBindingProfile> repo,
        IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
    }

    /// <summary>
    /// Loads binding-profile DTOs for one WeixinClaw account.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <returns>Binding profiles ordered by identifier.</returns>
    public async Task<List<WeixinClawBindingProfileDto>> GetDtosAsync(int accountId)
    {
        var list = await GetFullListAsync(
            item => item.WeixinClawAccountId == accountId,
            "Id ASC").ConfigureAwait(false);
        return list.Select(ToDto).ToList();
    }

    /// <summary>
    /// Loads all binding-profile DTOs ordered by identifier.
    /// </summary>
    /// <returns>All binding profiles.</returns>
    public async Task<List<WeixinClawBindingProfileDto>> GetAllDtosAsync()
    {
        var list = await GetFullListAsync(item => true, "Id ASC").ConfigureAwait(false);
        return list.Select(ToDto).ToList();
    }

    /// <summary>
    /// Creates or updates a binding profile after validating account, administrator, and binding-code inputs.
    /// </summary>
    /// <param name="dto">The binding-profile values to persist.</param>
    /// <returns>The persisted binding-profile DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required identifiers or a new binding code are invalid.</exception>
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

    /// <summary>
    /// Resolves an enabled binding profile by account and hashed binding code.
    /// </summary>
    /// <param name="accountId">The WeixinClaw account identifier.</param>
    /// <param name="bindingCode">The plain binding code supplied by the user.</param>
    /// <param name="cancellationToken">The lookup cancellation token.</param>
    /// <returns>The resolver DTO, or <see langword="null"/> when no enabled match exists.</returns>
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

    /// <summary>
    /// Hashes a trimmed binding code using SHA-256 for storage/comparison.
    /// </summary>
    /// <param name="code">The plain binding code.</param>
    /// <returns>The hexadecimal hash, or <see langword="null"/> for blank input.</returns>
    private static string HashCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Projects a persisted binding profile into an administration DTO.
    /// </summary>
    /// <param name="profile">The persisted profile.</param>
    /// <returns>The administration DTO.</returns>
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

    /// <summary>
    /// Projects a persisted binding profile into the runtime resolver contract.
    /// </summary>
    /// <param name="profile">The persisted profile.</param>
    /// <returns>The runtime resolver DTO.</returns>
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
