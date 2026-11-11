/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorsResource.cs
    文件功能描述：XNCF 注册本地化显示元数据
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 统一 XNCF Register 显示元数据与多语言资源键

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Connectors;

public sealed class ConnectorsResource
{
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(ConnectorsResource), key, fallback);
}
