/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminMenuLocalizationService.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// Resolves built-in menu labels without changing user-defined menu data.
/// </summary>
public sealed class AdminMenuLocalizationService
{
    private static readonly IReadOnlyDictionary<string, string> BuiltInResourceKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["1"] = "Common.系统管理",
            ["1.1"] = "SystemMenu.AdministratorManagement",
            ["1.2"] = "SystemMenu.RoleManagement",
            ["1.3"] = "Menu.管理",
            ["1.4"] = "SystemMenu.SystemInfo",
            ["1.5"] = "SystemMenu.TenantInfo",
            ["2"] = "SystemMenu.ExtensionModules",
            ["2.1"] = "SystemMenu.ModuleManagement"
        };

    private readonly IStringLocalizer<AdminResource> _localizer;

    public AdminMenuLocalizationService(IStringLocalizer<AdminResource> localizer)
    {
        _localizer = localizer;
    }

    public string Resolve(SysMenuDto menu)
    {
        ArgumentNullException.ThrowIfNull(menu);

        var register = FindRegister(menu.Url);
        if (register is not null)
        {
            return register.MenuName;
        }

        var stableId = NormalizeBuiltInId(menu.Id);
        if (stableId is not null &&
            BuiltInResourceKeys.TryGetValue(stableId, out var resourceKey))
        {
            return _localizer[resourceKey].Value;
        }

        return menu.MenuName;
    }

    public SysMenuDto ResolveInPlace(SysMenuDto menu)
    {
        menu.MenuName = Resolve(menu);
        return menu;
    }

    private static IXncfRegister? FindRegister(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return XncfRegisterManager.RegisterList.FirstOrDefault(register =>
            url.Contains($"uid={register.Uid}", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeBuiltInId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var candidate = id.Trim();
        var separator = candidate.LastIndexOf('-');
        if (separator >= 0 && separator < candidate.Length - 1)
        {
            candidate = candidate[(separator + 1)..];
        }

        return BuiltInResourceKeys.ContainsKey(candidate) ? candidate : null;
    }
}
