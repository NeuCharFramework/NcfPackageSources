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
        /// 获取当前数据库连接信息
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetCurrentConnectionInfo()
        {
            var connectionStr = SenparcDatabaseConnectionConfigs.ClientConnectionString;
            var list = connectionStr.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(z =>
            {
                var item = z.Split('=');
                return new KeyValuePair<string, string>(item[0], item[1]);
            });
            return new Dictionary<string, string>(list, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 尝试获取数据库连接信息的值，如果获取不到，则返回 null
        /// </summary>
        /// <param name="dic">数据库信息</param>
        /// <param name="name">名称，如 Database、UserName，不区分大小写</param>
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
        /// 尝试获取数据库连接信息的值，如果获取不到，则返回 null
        /// </summary>
        /// <param name="name">名称，如 Database、UserName，不区分大小写</param>
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
