using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;

namespace Senparc.Xncf.AIAgentsHub.Domain.Services
{
    public class InstallOptionsService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SenparcCoreSetting _senparcCoreSetting;
        public InstallOptionsService(IServiceProvider serviceProvider, IOptions<SenparcCoreSetting> senparcCoreSetting)
        {
            this._serviceProvider = serviceProvider;
            this._senparcCoreSetting = senparcCoreSetting.Value;

            //初始化Options的默认值

        }

        /// <summary>
        /// 获取自动生成的管理员用户名称
        /// </summary>
        /// <returns></returns>
        public string GetDefaultAdminUserName()
        {
            return $"SenparcCoreAdmin{new Random().Next(100).ToString("00")}";
        }

        /// <summary>
        /// 获取默认的系统名称
        /// </summary>
        /// <returns></returns>
        public string GetDefaultSystemName()
        {
            return "NCF - Template Project";
        }

        /// <summary>
        /// 读取配置中目标数据库连接字符串
        /// </summary>
        /// <returns></returns>
        public string GetDbConnectionString()
        {
            //string dbConfigName = SenparcDatabaseConnectionConfigs.GetFullDatabaseName(_senparcCoreSetting.DatabaseName);
            return SenparcDatabaseConnectionConfigs.GetClientConnectionString();
        }

        /// <summary>
        /// 修改配置及缓存中目标数据库连接字符串
        /// </summary>
        public void ResetDbConnectionString(string dbConnectionString)
        {
            if (dbConnectionString == GetDbConnectionString())
            {
                return;
            }

            string dbConfigName = SenparcDatabaseConnectionConfigs.GetFullDatabaseName(_senparcCoreSetting.DatabaseName);

            try
            {
                XmlDataContext xmlCtx = new XmlDataContext(SiteConfig.SenparcConfigDirctory);
                var list = xmlCtx.GetXmlList<SenparcConfig>();
                var modifyItem = list.FirstOrDefault(z => z.Name == dbConfigName);
                if (modifyItem == null)
                {
                    throw new NcfExceptionBase($"找不到数据库配置：{dbConfigName}");
                }
                modifyItem.ConnectionStringFull = dbConnectionString;

                xmlCtx.Save<SenparcConfig>(list);
            }
            catch (Exception e)
            {
                Console.WriteLine("=== NCF === 修改数据库配置错误：" + e.ToString());
                LogUtility.WebLogger.ErrorFormat("SenparcConfigs.Configs 修改错误：" + e.Message, e);
            }

            //清空数据库配置缓存
            MethodCache.ClearMethodCache<ConcurrentDictionary<string, SenparcConfig>>(SenparcDatabaseConnectionConfigs.SENPARC_CONFIG_KEY);

            _ = SenparcDatabaseConnectionConfigs.Configs.ToJson();
        }
    }
}
