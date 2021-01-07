using Template_OrgName.Xncf.Template_XncfName.Models.DatabaseModel;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;

namespace Template_OrgName.Xncf.Template_XncfName
{
    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// </summary>
    public class SenparcDbContextFactory : SenparcDesignTimeDbContextFactoryBase<Template_XncfNameSenparcEntities, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //services.AddDatabase<SQLServerDatabaseConfiguration>();//指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database", "Senparc.Ncf.Database.SQLite", "SQLiteMemoryDatabaseConfiguration");
        };

        /// <summary>
        /// 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息
        /// </summary>
        private static string RootDictionaryPath
        {
            get
            {
                var projectPath = Path.GetFullPath("..\\..\\..\\", AppContext.BaseDirectory);//项目根目录

                var webPath = Path.GetFullPath("..\\Senparc.Web",/*找到 Web目录，以获取统一的数据库连接字符串配置*/
                                               projectPath);
                if (Directory.Exists(webPath))
                {
                    return webPath;//优先使用Web统一配置
                }

                return projectPath;
            }
        }

        public SenparcDbContextFactory()
            : base(RootDictionaryPath)
        {
        }
    }
}
