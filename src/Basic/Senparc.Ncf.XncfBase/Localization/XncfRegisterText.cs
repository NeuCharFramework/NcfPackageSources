/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfRegisterText.cs
    File description: localized display metadata for XNCF registration

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.28.2 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.XncfBase;

/// <summary>
/// Localized display metadata for an XNCF register.
/// The resource key and Chinese source text are declared once; the value is
/// resolved for the current UI culture whenever the register is read.
/// </summary>
public sealed class XncfRegisterText
{
    private readonly Type _resourceType;
    private readonly string _keyPrefix;
    private readonly string _menuNameFallback;
    private readonly string _descriptionFallback;

    private XncfRegisterText(
        Type resourceType,
        string keyPrefix,
        string menuNameFallback,
        string descriptionFallback)
    {
        _resourceType = resourceType;
        _keyPrefix = keyPrefix;
        _menuNameFallback = menuNameFallback;
        _descriptionFallback = descriptionFallback;
    }

    public string MenuName => ResourceStringLocalizer.Get(
        _resourceType,
        $"{_keyPrefix}.MenuName",
        _menuNameFallback);

    public string Description => ResourceStringLocalizer.Get(
        _resourceType,
        $"{_keyPrefix}.Description",
        _descriptionFallback);

    public static XncfRegisterText Create<TResource>(
        string keyPrefix,
        string menuNameFallback,
        string descriptionFallback)
        where TResource : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(menuNameFallback);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionFallback);

        return new XncfRegisterText(
            typeof(TResource),
            keyPrefix,
            menuNameFallback,
            descriptionFallback);
    }
}
