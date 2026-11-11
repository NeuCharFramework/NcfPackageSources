/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ReloadPageResource.cs
    文件功能描述：ReloadPageResource 相关实现

    创建标识：Senparc - 20261111
    修改标识：Senparc - 20261111
    修改描述：v0.12.14 将 ReloadPage Register 显示元数据接入统一资源本地化

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.ReloadPage;

public sealed class ReloadPageResource
{
    public static string Get(string key, string? fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(ReloadPageResource), key, fallback);
    }

    public static string Format(string key, string fallback, params object[] arguments)
    {
        return ResourceStringLocalizer.Format(typeof(ReloadPageResource), key, fallback, arguments);
    }
}
