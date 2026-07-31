/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfCoreResource.cs
    文件功能描述：提供 NCF 核心验证与授权消息的共享本地化资源访问入口
    
    
    创建标识：Senparc - 20260730
    
    修改标识：Senparc - 20260731
    修改描述：v0.27.0-preview4 新增核心验证与授权消息的共享本地化资源入口

----------------------------------------------------------------*/
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.Core;

/// <summary>
/// Shared localization catalog for core validation messages.
/// </summary>
public sealed class NcfCoreResource
{
    public static string Get(string key, string fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(NcfCoreResource), key, fallback);
    }
}
