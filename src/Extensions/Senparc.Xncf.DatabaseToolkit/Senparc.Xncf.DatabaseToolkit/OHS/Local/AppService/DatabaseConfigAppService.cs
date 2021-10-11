using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    public class DatabaseConfigAppService : BaseAppService
    {
        /// <summary>
        /// 显示当前的数据库配置类型
        /// </summary>
        /// <returns></returns>
        [FunctionRender]
        public BaseAppResponse<string> ShowDatabaseConfiguration()
        {
            return AppServiceHelper.GetResponse<BaseAppResponse<string>, string>(response =>
            {
                var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
                var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
                return $"当前 DatabaseConfiguration：{currentDatabaseConfiguration.GetType().Name}，数据库类型：{currentDatabaseConfiguration.MultipleDatabaseType}";
            });
        }
    }
}
