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
    /// <summary>
    /// Provides default installation values and persists database-connection overrides.
    /// </summary>
    public class InstallOptionsService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SenparcCoreSetting _senparcCoreSetting;
        /// <summary>Initializes the installation-options service.</summary>
        /// <param name="serviceProvider">The dependency-injection service provider.</param>
        /// <param name="senparcCoreSetting">The configured core settings.</param>
        public InstallOptionsService(IServiceProvider serviceProvider, IOptions<SenparcCoreSetting> senparcCoreSetting)
        {
            this._serviceProvider = serviceProvider;
            this._senparcCoreSetting = senparcCoreSetting.Value;

        }

        /// <summary>
        /// Generates a default administrator user name with a short random suffix.
        /// </summary>
        /// <returns>The generated administrator user name.</returns>
        public string GetDefaultAdminUserName()
        {
            return $"SenparcCoreAdmin{new Random().Next(100).ToString("00")}";
        }

        /// <summary>
        /// Returns the default system display name.
        /// </summary>
        /// <returns>The default system name.</returns>
        public string GetDefaultSystemName()
        {
            return "NCF - Template Project";
        }

        /// <summary>
        /// Returns the current client database connection string.
        /// </summary>
        /// <returns>The configured connection string.</returns>
        public string GetDbConnectionString()
        {
            //string dbConfigName = SenparcDatabaseConnectionConfigs.GetFullDatabaseName(_senparcCoreSetting.DatabaseName);
            return SenparcDatabaseConnectionConfigs.GetClientConnectionString();
        }

        /// <summary>
        /// Persists a changed database connection string and clears cached configuration.
        /// </summary>
        /// <param name="dbConnectionString">The replacement connection string.</param>
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

            //Legacy comment translated to English.
            MethodCache.ClearMethodCache<ConcurrentDictionary<string, SenparcConfig>>(SenparcDatabaseConnectionConfigs.SENPARC_CONFIG_KEY);

            _ = SenparcDatabaseConnectionConfigs.Configs.ToJson();
        }
    }
}
