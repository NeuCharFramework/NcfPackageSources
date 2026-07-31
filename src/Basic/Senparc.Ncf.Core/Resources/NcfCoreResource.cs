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
