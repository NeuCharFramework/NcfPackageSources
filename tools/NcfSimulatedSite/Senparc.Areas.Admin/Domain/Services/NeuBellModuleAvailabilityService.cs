/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: NeuBellModuleAvailabilityService.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase;

namespace Senparc.Areas.Admin.Domain.Services;

public interface INeuBellModuleAvailabilityService
{
    Task<IReadOnlySet<string>> GetOpenModuleUidsAsync(
        IEnumerable<string> moduleUids,
        CancellationToken cancellationToken = default);
}

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class NeuBellModuleAvailabilityService : INeuBellModuleAvailabilityService
{
    private readonly XncfRegisterManager _xncfRegisterManager;

    public NeuBellModuleAvailabilityService(IServiceProvider serviceProvider)
    {
        _xncfRegisterManager = new XncfRegisterManager(serviceProvider);
    }

    public async Task<IReadOnlySet<string>> GetOpenModuleUidsAsync(
        IEnumerable<string> moduleUids,
        CancellationToken cancellationToken = default)
    {
        var requestedUids = moduleUids?
            .Where(uid => !string.IsNullOrWhiteSpace(uid))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
        if (requestedUids.Length == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var requestedUidSet = requestedUids.ToHashSet(StringComparer.OrdinalIgnoreCase);
        //Legacy comment translated to English.
        var registeredModules = XncfRegisterManager.RegisterList
            .Where(register => requestedUidSet.Contains(register.Uid))
            .ToArray();
        var openModuleUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var register in registeredModules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            //Legacy comment translated to English.
            if (await _xncfRegisterManager.CheckXncfAvailable(register).ConfigureAwait(false))
            {
                openModuleUids.Add(register.Uid);
            }
        }

        return openModuleUids;
    }
}
