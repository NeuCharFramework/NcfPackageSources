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

    /// <summary>
    /// Creates and persists a pending NeuBell webhook delivery log.
    /// </summary>
    /// <param name="eventKind">The event category being delivered.</param>
    /// <param name="httpMethod">The HTTP method used for delivery.</param>
    /// <param name="webHookUrl">The destination URL.</param>
    /// <param name="providerId">The NeuBell provider identifier.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="payload">The serialized request payload.</param>
    /// <param name="adminUserId">The administrator associated with delivery.</param>
    /// <returns>The persisted pending log.</returns>
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

    /// <summary>
    /// Marks a persisted webhook log as finished with delivery outcome and timing data.
    /// Missing log identifiers are ignored.
    /// </summary>
    /// <param name="logId">The webhook log identifier.</param>
    /// <param name="success">Whether delivery succeeded.</param>
    /// <param name="result">The response or failure detail.</param>
    /// <param name="statusCode">The HTTP status code, when available.</param>
    /// <param name="elapsedMilliseconds">The measured delivery duration.</param>
    /// <returns>A task that completes after the update is persisted.</returns>
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
