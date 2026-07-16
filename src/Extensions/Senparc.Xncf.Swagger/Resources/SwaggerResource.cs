#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Swagger
{
    /// <summary>
    /// Localization catalog owned and packaged by the Swagger module.
    /// </summary>
    public sealed class SwaggerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SwaggerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SwaggerResource), key, fallback, arguments);
        }
    }
}
