using System.Globalization;
using System.Resources;

namespace Senparc.Xncf.Accounts
{
    /// <summary>
    /// Marker class for Accounts module localization resources.
    /// </summary>
    public class AccountsResource
    {
        private static readonly ResourceManager ResourceManager =
            new("Senparc.Xncf.Accounts.AccountsResource", typeof(AccountsResource).Assembly);

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
