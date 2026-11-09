/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharBoxLiveResource.cs
    文件功能描述：模块的本地化资源入口：按键读取与格式化多语言文字


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 类注释改为本模块（原为模板遗留的 TemplateSimulated）

----------------------------------------------------------------*/

#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.NeuCharBoxLive
{
    /// <summary>
    /// Localization catalog owned and packaged by the NeuCharBox Live module.
    /// </summary>
    public sealed class NeuCharBoxLiveResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(NeuCharBoxLiveResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(NeuCharBoxLiveResource), key, fallback, arguments);
        }
    }
}
