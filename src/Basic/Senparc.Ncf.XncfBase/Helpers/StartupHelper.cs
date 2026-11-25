/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: StartupHelper.cs
    File description: StartupHelper related implementation

    Created by: Senparc - 20240107

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.XncfBase.Helpers
{
    public class StartupHelper
    {
        /// <summary>
        /// Creates a database register and formats its XNCF name/version for startup output.
        /// Failures are converted to a development fallback string because startup banners
        /// must not prevent application initialization.
        /// </summary>
        /// <typeparam name="TXncfDatabaseRegister">The database register type to instantiate.</typeparam>
        /// <returns>The formatted XNCF identity or a development fallback description.</returns>
        internal static string GetXncfVersion<TXncfDatabaseRegister>()
          where TXncfDatabaseRegister : class, IXncfDatabase, new()
        {
            try
            {
                var register = System.Activator.CreateInstance<TXncfDatabaseRegister>() as TXncfDatabaseRegister;
                if (register is IXncfRegister xncfRegister)
                {
                    return $" / XNCF {xncfRegister.Name} {xncfRegister.Version}";
                }
                return "Dev";
            }
            catch
            {
                return "Dev / Not IXncfRegister";
            }
        }
    }
}
