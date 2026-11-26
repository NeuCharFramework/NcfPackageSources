/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfDatabaseHelper.cs
    File description: NcfDatabaseHelper related implementation

    Created by: Senparc - 20211110

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Database.Helpers
{
    public static class NcfDatabaseHelper
    {
        /// <summary>
        /// Reads the current client connection string and splits it into case-insensitive key/value pairs.
        /// </summary>
        /// <returns>The parsed connection-string entries.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the configured client connection string cannot be resolved.</exception>
        public static Dictionary<string, string> GetCurrentConnectionInfo()
        {
            var connectionStr = SenparcDatabaseConnectionConfigs.GetClientConnectionString();
            var list = connectionStr.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(z =>
            {
                var item = z.Split('=');
                return new KeyValuePair<string, string>(item[0], item[1]);
            });
            return new Dictionary<string, string>(list, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns one named value from a parsed connection-string dictionary.
        /// </summary>
        /// <param name="dic">The parsed connection-string entries.</param>
        /// <param name="name">The key to look up.</param>
        /// <returns>The value, or <see langword="null"/> when the key is absent.</returns>
        public static string TryGetConnectionValue(Dictionary<string, string> dic, string name)
        {
            if (dic.TryGetValue(name, out var value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Returns one named value from the current client connection configuration.
        /// </summary>
        /// <param name="name">The connection-string key to look up.</param>
        /// <returns>The value, or <see langword="null"/> when the key is absent.</returns>
        public static string TryGetConnectionValue(string name)
        {
            var dic = GetCurrentConnectionInfo();
            if (dic.TryGetValue(name, out var value))
            {
                return value;
            }
            return null;
        }

    }
}
