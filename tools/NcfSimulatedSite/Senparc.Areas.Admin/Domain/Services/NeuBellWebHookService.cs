/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookService.cs
    文件功能描述：NeuBell WebHook（WebAPI）通知设置服务：
    设置项 CRUD 与启用列表查询

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 纽铃 WebHook（WebAPI）通知设置服务契约（供 Dispatcher 等消费方依赖，便于测试替换）
/// </summary>
public interface INeuBellWebHookService
{
    /// <summary>
    /// 当前全部启用的 WebHook 设置（供 Dispatcher 匹配）
    /// </summary>
    Task<List<NeuBellWebHook>> GetEnabledListAsync();
}

/// <summary>
/// 纽铃 WebHook（WebAPI）通知设置服务
/// </summary>
public sealed class NeuBellWebHookService : BaseClientService<NeuBellWebHook>, INeuBellWebHookService
{
    public NeuBellWebHookService(
        INeuBellWebHookRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }

    /// <summary>
    /// 新增 WebHook 设置
    /// </summary>
    public async Task<(bool success, string message, NeuBellWebHook item)> AddAsync(
        string name,
        string webHookUrl,
        string providerFilter,
        string secret,
        bool notifyOnAdd,
        bool notifyOnRemove,
        int adminUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "名称不能为空", null);
        }
        if (!NeuBellWebHook.TryValidateUrl(webHookUrl, out var urlError))
        {
            return (false, urlError, null);
        }
        if (string.IsNullOrWhiteSpace(providerFilter) == false
            && (providerFilter ?? string.Empty).Trim().Length > 200)
        {
            return (false, "Provider 过滤条件过长", null);
        }

        var item = new NeuBellWebHook(name, webHookUrl, adminUserId);
        item.UpdateInfo(name, webHookUrl, providerFilter, secret, notifyOnAdd, notifyOnRemove, true);
        await SaveObjectAsync(item).ConfigureAwait(false);
        return (true, "创建成功", item);
    }

    /// <summary>
    /// 更新 WebHook 设置
    /// </summary>
    public async Task<(bool success, string message, NeuBellWebHook item)> UpdateAsync(
        int id,
        string name,
        string webHookUrl,
        string providerFilter,
        string secret,
        bool notifyOnAdd,
        bool notifyOnRemove,
        bool isEnabled)
    {
        var item = await GetObjectAsync(z => z.Id == id).ConfigureAwait(false);
        if (item == null)
        {
            return (false, "记录不存在", null);
        }

        if (!string.IsNullOrWhiteSpace(webHookUrl)
            && !NeuBellWebHook.TryValidateUrl(webHookUrl, out var urlError))
        {
            return (false, urlError, null);
        }

        item.UpdateInfo(name, webHookUrl, providerFilter, secret, notifyOnAdd, notifyOnRemove, isEnabled);
        await SaveObjectAsync(item).ConfigureAwait(false);
        return (true, "保存成功", item);
    }

    /// <summary>
    /// 当前全部启用的 WebHook 设置（供 Dispatcher 匹配）
    /// </summary>
    public async Task<List<NeuBellWebHook>> GetEnabledListAsync()
    {
        var list = await GetFullListAsync(
            z => z.IsEnabled,
            z => z.Id,
            OrderingType.Ascending).ConfigureAwait(false);
        return list.ToList();
    }
}
