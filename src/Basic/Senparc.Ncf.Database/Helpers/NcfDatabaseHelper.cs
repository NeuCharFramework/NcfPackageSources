/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfDatabaseHelper.cs
    File description: NcfDatabaseHelper related implementation

    Created by: Senparc - 20211110

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="dic">databaseinformation</param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string TryGetConnectionValue(Dictionary<string, string> dic, string name)
        {
            if (dic.TryGetValue(name, out var value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
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
