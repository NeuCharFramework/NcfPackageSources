/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawAdminBindingService.cs
    File description: WeixinClawAdminBindingService.cs implementation and project behavior.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add WeixinClawBindingProfile management and UI enhancements

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class WeixinClawAdminBindingService : BaseClientService<WeixinClawAdminBinding>
{
    /// <summary>
    /// Initializes persistence for administrator-owned Weixin Claw routing bindings.
    /// </summary>
    /// <param name="repository">The binding repository.</param>
    /// <param name="serviceProvider">The dependency provider used by the base service.</param>
    public WeixinClawAdminBindingService(
        IWeixinClawAdminBindingRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }

    /// <summary>
    /// Finds an enabled binding for one account, sender, and group context after trimming identifiers.
    /// </summary>
    /// <param name="accountId">The Weixin Claw account identifier.</param>
    /// <param name="fromUserId">The sender identifier.</param>
    /// <param name="groupId">The group identifier, or an empty value for direct messages.</param>
    /// <returns>The enabled binding, or <see langword="null"/> when none exists.</returns>
    public async Task<WeixinClawAdminBinding> GetBindingAsync(
        int accountId,
        string fromUserId,
        string groupId)
    {
        var normalizedUserId = fromUserId?.Trim() ?? string.Empty;
        var normalizedGroupId = groupId?.Trim() ?? string.Empty;
        return await GetObjectAsync(item =>
            item.AccountId == accountId
            && item.FromUserId == normalizedUserId
            && item.GroupId == normalizedGroupId
            && item.Enabled).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists enabled bindings that have NeuBell routing enabled.
    /// </summary>
    /// <returns>The matching bindings.</returns>
    public async Task<List<WeixinClawAdminBinding>> GetNeuBellBindingsAsync()
    {
        var bindings = await GetFullListAsync(item =>
            item.Enabled && item.EnableNeuBell).ConfigureAwait(false);
        return bindings.ToList();
    }

    /// <summary>
    /// Stores the latest conversation context token and message timestamp for a binding.
    /// Blank input or a missing binding is ignored.
    /// </summary>
    /// <param name="binding">The binding to update.</param>
    /// <param name="contextToken">The provider conversation context token.</param>
    /// <param name="messageAt">The message timestamp associated with the token.</param>
    /// <returns>A task that completes after persistence.</returns>
    public async Task SaveContextAsync(
        WeixinClawAdminBinding binding,
        string contextToken,
        DateTime messageAt)
    {
        if (binding == null || string.IsNullOrWhiteSpace(contextToken))
        {
            return;
        }

        binding.SetContextToken(contextToken, messageAt);
        await SaveObjectAsync(binding).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates or updates the routing binding for one sender/group context and
    /// persists the selected AI, workflow, and NeuBell options.
    /// </summary>
    /// <param name="accountId">The Weixin Claw account identifier.</param>
    /// <param name="fromUserId">The sender identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="adminUserId">The administrator who owns the binding.</param>
    /// <param name="enableNeuBell">Whether NeuBell notifications are enabled.</param>
    /// <param name="enableWorkflow">Whether workflow routing is enabled.</param>
    /// <param name="workflowId">Optional workflow identifier.</param>
    /// <param name="aiModelId">The selected AI model identifier.</param>
    /// <param name="mode">The binding routing mode.</param>
    /// <returns>The existing updated or newly created binding.</returns>
    public async Task<WeixinClawAdminBinding> BindAsync(
        int accountId,
        string fromUserId,
        string groupId,
        int adminUserId,
        bool enableNeuBell,
        bool enableWorkflow,
        int? workflowId = null,
        int aiModelId = 0,
        int mode = 0)
    {
        var normalizedUserId = fromUserId?.Trim() ?? string.Empty;
        var normalizedGroupId = groupId?.Trim() ?? string.Empty;
        var existing = await GetObjectAsync(item =>
            item.AccountId == accountId
            && item.FromUserId == normalizedUserId
            && item.GroupId == normalizedGroupId).ConfigureAwait(false);

        if (existing != null)
        {
            existing.SetRouting(
                mode,
                aiModelId,
                workflowId,
                enableNeuBell,
                enableWorkflow);
            await SaveObjectAsync(existing).ConfigureAwait(false);
            return existing;
        }

        var binding = new WeixinClawAdminBinding(
            accountId,
            fromUserId,
            groupId,
            adminUserId,
            enableNeuBell,
            enableWorkflow);
        binding.SetRouting(
            mode,
            aiModelId,
            workflowId,
            enableNeuBell,
            enableWorkflow);
        await SaveObjectAsync(binding).ConfigureAwait(false);
        return binding;
    }

    /// <summary>
    /// Retrieves an enabled binding by its database identifier.
    /// </summary>
    /// <param name="id">The binding identifier.</param>
    /// <returns>The enabled binding, or <see langword="null"/> when absent or disabled.</returns>
    public async Task<WeixinClawAdminBinding> GetByIdAsync(int id)
    {
        return await GetObjectAsync(item => item.Id == id && item.Enabled).ConfigureAwait(false);
    }
}
