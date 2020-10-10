using Microsoft.Extensions.DependencyInjection;
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
    public class SenparcDbContextFactory_XncfBuilderEntities : SenparcDesignTimeDbContextFactoryBase<XncfBuilderEntities, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //services.AddDatabase<SQLServerDatabaseConfiguration>();//指定其他数据库
        };

        public SenparcDbContextFactory_XncfBuilderEntities()
            : base(
                 /* Debug模式下项目根目录
                 /* 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息 */
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))
        {

        }
    }
}
