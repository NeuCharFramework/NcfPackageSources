/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InstallCultureHelper.cs
    File description: Apply UI culture during NCF first-time installation.

    Modified by: Senparc - 20261127
    Change description: v0.6.0.1 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Globalization;
using Senparc.Ncf.Utility.Helpers;

namespace Senparc.Xncf.Installer.Domain.Services
{
    /// <summary>
    /// Ensures module menu metadata is resolved under the culture chosen on the install page.
    /// </summary>
    public static class InstallCultureHelper
    {
        /// <summary>
        /// Temporarily applies a normalized installation UI culture and restores the previous culture on disposal.
        /// </summary>
        /// <param name="cultureName">The requested culture name.</param>
        /// <returns>A disposable culture scope, or a no-op scope when the culture is invalid.</returns>
        public static IDisposable Apply(string cultureName)
        {
            if (!NcfLocalizationOptions.TryNormalizeCulture(cultureName, out var normalizedCulture))
            {
                return NoOpDisposable.Instance;
            }

            var culture = CultureInfo.GetCultureInfo(normalizedCulture);
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Console.WriteLine($"[NCF Installer] Apply install culture: {normalizedCulture}");

            return new CultureScope(previousCulture, previousUiCulture);
        }

        private sealed class CultureScope : IDisposable
        {
            private readonly CultureInfo _previousCulture;
            private readonly CultureInfo _previousUiCulture;

            public CultureScope(CultureInfo previousCulture, CultureInfo previousUiCulture)
            {
                _previousCulture = previousCulture;
                _previousUiCulture = previousUiCulture;
            }

            /// <inheritdoc/>
            /// <remarks>
            /// Restores both the culture used for general formatting and the UI
            /// culture used for localized resource lookup.
            /// </remarks>
            public void Dispose()
            {
                CultureInfo.CurrentCulture = _previousCulture;
                CultureInfo.CurrentUICulture = _previousUiCulture;
            }
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();

            /// <inheritdoc/>
            /// <remarks>
            /// Intentionally performs no work because the invalid-culture path
            /// did not modify the ambient culture.
            /// </remarks>
            public void Dispose()
            {
            }
        }
    }
}
