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
        /// Get current database connection information
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
        /// Try to get the value of the database connection information, if not, return null
        /// </summary>
        /// <param name="dic">Database information</param>
        /// <param name="name">Names, such as Database, UserName, are not case sensitive</param>
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
        /// Try to get the value of the database connection information, if not, return null
        /// </summary>
        /// <param name="name">Names, such as Database, UserName, are not case sensitive</param>
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
