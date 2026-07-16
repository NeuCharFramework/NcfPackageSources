#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.FirmwareUpdate
{
    /// <summary>
    /// Localization catalog owned and packaged by the FirmwareUpdate module.
    /// </summary>
    public sealed class FirmwareUpdateResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(FirmwareUpdateResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(FirmwareUpdateResource), key, fallback, arguments);
        }
    }
}
