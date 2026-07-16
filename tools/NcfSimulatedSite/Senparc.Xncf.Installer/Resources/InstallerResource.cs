using System.Globalization;
using System.Resources;

namespace Senparc.Xncf.Installer
{
    /// <summary>
    /// Marker class for Installer module localization resources.
    /// Resource files are stored in Resources/InstallerResource.{culture}.resx
    ///
    /// Usage in Razor views: @inject IStringLocalizer&lt;InstallerResource&gt; IR
    /// Usage in code:        IStringLocalizer&lt;InstallerResource&gt; localizer (via DI)
    ///
    /// Supported cultures: zh-CN (default), en, ja, fr, es, ru
    /// To add a new language: copy InstallerResource.en.resx, rename to InstallerResource.{culture}.resx,
    /// translate the values, and add the culture code to NcfLocalizationOptions.SupportedCultures.
    /// </summary>
    public class InstallerResource
    {
        private static readonly ResourceManager ResourceManager =
            new("Senparc.Xncf.Installer.InstallerResource", typeof(InstallerResource).Assembly);

        public static string Get(string key, string fallback = null)
        {
            return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback ?? key;
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key, fallback), arguments ?? []);
        }
    }
}
