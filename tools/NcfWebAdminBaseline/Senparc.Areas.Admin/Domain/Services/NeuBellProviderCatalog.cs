/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellProviderCatalog.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class NeuBellProviderCatalog
{
    private readonly IReadOnlyList<INeuBellProvider> _providers;
    private readonly INeuBellModuleAvailabilityService _moduleAvailabilityService;
    private readonly ILogger<NeuBellProviderCatalog> _logger;

    public NeuBellProviderCatalog(
        IEnumerable<INeuBellProvider> providers,
        INeuBellModuleAvailabilityService moduleAvailabilityService,
        ILogger<NeuBellProviderCatalog> logger)
    {
        _providers = providers
            .GroupBy(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _moduleAvailabilityService = moduleAvailabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Returns providers whose declaring XNCF modules are currently open.
    /// Providers without a module UID or when module availability cannot be read
    /// are excluded to keep the notification surface fail-closed.
    /// </summary>
    /// <param name="cancellationToken">Token used to query module availability.</param>
    /// <returns>The available provider instances in stable provider-ID order.</returns>
    public async Task<IReadOnlyList<INeuBellProvider>> GetAvailableProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var declaredProviders = _providers
            .Where(provider => !string.IsNullOrWhiteSpace(provider.ModuleUid))
            .ToArray();
        //Legacy comment translated to English.
        foreach (var provider in _providers.Except(declaredProviders))
        {
            _logger.LogDebug(
                "纽铃 Provider {ProviderId} 未声明 ModuleUid，已忽略以避免未安装模块参与 Footer。",
                provider.ProviderId);
        }

        if (declaredProviders.Length == 0)
        {
            return Array.Empty<INeuBellProvider>();
        }

        try
        {
            var openModuleUids = await _moduleAvailabilityService
                .GetOpenModuleUidsAsync(
                    declaredProviders.Select(provider => provider.ModuleUid),
                    cancellationToken)
                .ConfigureAwait(false);
            return declaredProviders
                .Where(provider => openModuleUids.Contains(provider.ModuleUid))
                .ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            //Legacy comment translated to English.
            _logger.LogWarning(ex, "读取 XNCF 模块开放状态失败，纽铃 Provider 将按安全方式全部禁用。");
            return Array.Empty<INeuBellProvider>();
        }
    }
}
