/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FirmwareUpdateResource.cs
    File description: FirmwareUpdateResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.FirmwareUpdate
{
    /// <summary>
    /// Localization catalog owned and packaged by the FirmwareUpdate module.
    /// </summary>
    public sealed class FirmwareUpdateResource
    {
        /// <summary>
        /// Resolves a FirmwareUpdate localization key and returns the supplied fallback when
        /// the key is not available in the module resource catalog.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The text to return when the key cannot be localized.</param>
        /// <returns>The localized text or <paramref name="fallback"/>.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(FirmwareUpdateResource), key, fallback);
        }

        /// <summary>
        /// Resolves a FirmwareUpdate localization key and formats the resulting template
        /// with the supplied arguments.
        /// </summary>
        /// <param name="key">The resource key to resolve.</param>
        /// <param name="fallback">The fallback composite format string.</param>
        /// <param name="arguments">Values used to format the resolved or fallback template.</param>
        /// <returns>The localized, formatted text.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(FirmwareUpdateResource), key, fallback, arguments);
        }
    }
}
