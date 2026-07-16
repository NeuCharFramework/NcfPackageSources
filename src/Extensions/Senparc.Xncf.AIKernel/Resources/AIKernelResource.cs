#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AIKernel
{
    /// <summary>
    /// Localization catalog owned and packaged by the AIKernel module.
    /// </summary>
    public sealed class AIKernelResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AIKernelResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AIKernelResource), key, fallback, arguments);
        }
    }
}
