using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Database;
using Senparc.Ncf.Log;
using System;
using System.Collections.Concurrent;

namespace Senparc.Ncf.Core.Config
{
    /// <summary>
    /// 数据库连接信息配置
    /// </summary>
    public static class SenparcDatabaseConnectionConfigs
    {
        public const string SENPARC_CONFIG_KEY = "__SENPARC_DATABASE_CONNECTION_CONFIG_KEY";
        public static ConcurrentDictionary<string, SenparcConfig> Configs
        {
            get
            {
                Func<ConcurrentDictionary<string, SenparcConfig>> func = () =>
                {
                    ConcurrentDictionary<string, SenparcConfig> configs = new ConcurrentDictionary<string, SenparcConfig>();
                    try
                    {
                        XmlDataContext xmlCtx = new XmlDataContext(SiteConfig.SenparcConfigDirctory);
                        var list = xmlCtx.GetXmlList<SenparcConfig>();
                        list.ForEach(z => configs[z.Name] = z);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("=== NCF === 读取数据库配置错误：" + e.ToString());
                        LogUtility.WebLogger.ErrorFormat("SenparcConfigs.Configs 读取错误：" + e.Message, e);
                    }
                    return configs;
                };
                //ICommonDataCache<SenparcConfig> dataCache = new CommonDataCache<SenparcConfig>(SENPARC_CONFIG_KEY, func);
                //return dataCache.Data;

                var cacheData = MethodCache.GetMethodCache(SENPARC_CONFIG_KEY, func, 60 * 999);
                return cacheData;
            }
        }

        /// <summary>
        /// 主站客户数据库连接字符串（根据当前配置数据库动态获得）
        /// </summary>
        public static string GetClientConnectionString()
        {
            var databaseName = GetFullDatabaseName(Config.SiteConfig.SenparcCoreSetting.DatabaseName ?? "Client");

            if (SenparcDatabaseConnectionConfigs.Configs != null && SenparcDatabaseConnectionConfigs.Configs.ContainsKey(databaseName))
            {
                return SenparcDatabaseConnectionConfigs.Configs[databaseName].ConnectionStringFull;
            }
            else
            {
                var code = 0;
                if (SenparcDatabaseConnectionConfigs.Configs == null)
                {
                    code = 101;
                }
                else if (!SenparcDatabaseConnectionConfigs.Configs.ContainsKey(databaseName))
                {
                    code = 102;
                    Console.WriteLine("SenparcDatabaseConnectionConfigs.Configs 配置：" + SenparcDatabaseConnectionConfigs.Configs.ToJson(true));
                }
                throw new NcfExceptionBase($"无法找到数据库配置：{databaseName}，请在 SenparcConfig.config 中进行配置（ErrCode: {code}）！");
            }
        }

        /// <summary>
        /// 获取完整名称
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static string GetFullDatabaseName(string databaseName)
        {
            var currentDatabaseType = DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
            var fullDatabaseName = $"{databaseName}-{currentDatabaseType}";
            return fullDatabaseName;
        }
    }
}
