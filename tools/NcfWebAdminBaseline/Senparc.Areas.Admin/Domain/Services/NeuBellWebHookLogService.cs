/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellWebHookLogService.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260910

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

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using System;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public interface INeuBellWebHookLogService
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    Task<NeuBellWebHookLog> CreatePendingAsync(
        string eventKind,
        string httpMethod,
        string webHookUrl,
        string providerId,
        string title,
        string payload,
        int adminUserId);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    Task CompleteAsync(
        int logId,
        bool success,
        string result,
        int? statusCode,
        long elapsedMilliseconds);
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class NeuBellWebHookLogService : BaseClientService<NeuBellWebHookLog>, INeuBellWebHookLogService
{
    public NeuBellWebHookLogService(
        INeuBellWebHookLogRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }

    public async Task<NeuBellWebHookLog> CreatePendingAsync(
        string eventKind,
        string httpMethod,
        string webHookUrl,
        string providerId,
        string title,
        string payload,
        int adminUserId)
    {
        var log = new NeuBellWebHookLog(
            eventKind,
            httpMethod,
            webHookUrl,
            providerId,
            title,
            payload,
            adminUserId);
        await SaveObjectAsync(log).ConfigureAwait(false);
        return log;
    }

    public async Task CompleteAsync(
        int logId,
        bool success,
        string result,
        int? statusCode,
        long elapsedMilliseconds)
    {
        var log = await GetObjectAsync(z => z.Id == logId).ConfigureAwait(false);
        if (log == null)
        {
            return;
        }
        log.MarkFinished(success, result, statusCode, elapsedMilliseconds);
        await SaveObjectAsync(log).ConfigureAwait(false);
    }
}
