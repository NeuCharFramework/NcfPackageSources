using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Senparc.Xncf.XncfBuilder
{
    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -Context XncfBuilderEntities</para>
    /// </summary>
    public class SenparcDbContextFactory : SenparcDesignTimeDbContextFactoryBase<XncfBuilderEntities, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //services.AddDatabase<SQLServerDatabaseConfiguration>();//指定其他数据库
        };

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (b, d) => {
            Console.WriteLine(d.ToJson(true));
            d.AssemblyName = "Senparc.Ncf.DatabasePlant";
            base.DbContextOptionsAction(b, d);
        };

        public SenparcDbContextFactory()
            : base(
                 /* Debug模式下项目根目录
                 /* 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息 */
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))
        {

        }
    }
}
