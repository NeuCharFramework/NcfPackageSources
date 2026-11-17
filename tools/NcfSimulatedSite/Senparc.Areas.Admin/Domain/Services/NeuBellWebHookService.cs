/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: NeuBellWebHookService.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260914
    Change description: v0.7.1 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

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
///Legacy comment translated to English.
/// </summary>
public interface INeuBellWebHookService
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    Task<List<NeuBellWebHook>> GetEnabledListAsync();
}

/// <summary>
///Legacy comment translated to English.
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
    /// add WebHook settings
    /// </summary>
    public async Task<(bool success, string message, NeuBellWebHook item)> AddAsync(
        string name,
        string webHookUrl,
        string providerFilter,
        string secret,
        bool notifyOnAdd,
        bool notifyOnRemove,
        int adminUserId,
        string httpMethod = null,
        string bodyTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "名称不能为空", null);
        }
        if (!NeuBellWebHook.TryValidateUrlTemplate(webHookUrl, out var urlError))
        {
            return (false, urlError, null);
        }
        if (!NeuBellWebHook.TryValidateHttpMethod(httpMethod, out _, out var methodError))
        {
            return (false, methodError, null);
        }
        if (!string.IsNullOrWhiteSpace(providerFilter)
            && (providerFilter ?? string.Empty).Trim().Length > 200)
        {
            return (false, "Provider 过滤条件过长", null);
        }
        if (!string.IsNullOrWhiteSpace(bodyTemplate)
            && bodyTemplate.Length > NeuBellWebHook.MaxBodyTemplateLength)
        {
            return (false, $"请求体模板过长（最多 {NeuBellWebHook.MaxBodyTemplateLength} 个字符）", null);
        }

        var item = new NeuBellWebHook(name, webHookUrl, adminUserId);
        item.UpdateInfo(name, webHookUrl, providerFilter, secret, notifyOnAdd, notifyOnRemove, true,
            httpMethod, bodyTemplate);
        await SaveObjectAsync(item).ConfigureAwait(false);
        return (true, "创建成功", item);
    }

    /// <summary>
    /// update WebHook settings
    /// </summary>
    public async Task<(bool success, string message, NeuBellWebHook item)> UpdateAsync(
        int id,
        string name,
        string webHookUrl,
        string providerFilter,
        string secret,
        bool notifyOnAdd,
        bool notifyOnRemove,
        bool isEnabled,
        string httpMethod = null,
        string bodyTemplate = null)
    {
        var item = await GetObjectAsync(z => z.Id == id).ConfigureAwait(false);
        if (item == null)
        {
            return (false, "记录不存在", null);
        }

        if (!string.IsNullOrWhiteSpace(webHookUrl)
            && !NeuBellWebHook.TryValidateUrlTemplate(webHookUrl, out var urlError))
        {
            return (false, urlError, null);
        }
        if (httpMethod != null
            && !NeuBellWebHook.TryValidateHttpMethod(httpMethod, out _, out var methodError))
        {
            return (false, methodError, null);
        }
        if (bodyTemplate != null
            && bodyTemplate.Length > NeuBellWebHook.MaxBodyTemplateLength)
        {
            return (false, $"请求体模板过长（最多 {NeuBellWebHook.MaxBodyTemplateLength} 个字符）", null);
        }

        item.UpdateInfo(name, webHookUrl, providerFilter, secret, notifyOnAdd, notifyOnRemove, isEnabled,
            httpMethod, bodyTemplate);
        await SaveObjectAsync(item).ConfigureAwait(false);
        return (true, "保存成功", item);
    }

    /// <summary>
    ///Legacy comment translated to English.
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
