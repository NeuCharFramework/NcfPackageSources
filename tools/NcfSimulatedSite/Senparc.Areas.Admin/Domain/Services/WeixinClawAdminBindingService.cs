using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class WeixinClawAdminBindingService : BaseClientService<WeixinClawAdminBinding>
{
    public WeixinClawAdminBindingService(
        IWeixinClawAdminBindingRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }

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

    public async Task<List<WeixinClawAdminBinding>> GetNeuBellBindingsAsync()
    {
        var bindings = await GetFullListAsync(item =>
            item.Enabled && item.EnableNeuBell).ConfigureAwait(false);
        return bindings.ToList();
    }

    public async Task<WeixinClawAdminBinding> BindAsync(
        int accountId,
        string fromUserId,
        string groupId,
        int adminUserId,
        bool enableNeuBell,
        bool enableWorkflow)
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
                existing.Mode,
                existing.AiModelId,
                existing.WorkflowId,
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
        await SaveObjectAsync(binding).ConfigureAwait(false);
        return binding;
    }

    public async Task<WeixinClawAdminBinding> GetByIdAsync(int id)
    {
        return await GetObjectAsync(item => item.Id == id && item.Enabled).ConfigureAwait(false);
    }
}
