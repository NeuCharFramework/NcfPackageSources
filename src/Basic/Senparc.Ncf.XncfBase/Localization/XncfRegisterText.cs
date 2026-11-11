/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfRegisterText.cs
    文件功能描述：XNCF 注册本地化显示元数据
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.28.2 统一 XNCF Register 显示元数据与多语言资源键

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
