/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.Database.cs
    文件功能描述：模块的数据库注册（表前缀与多数据库 DbContext）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 注册设置表映射

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 NeuCharBoxLiveSenparcEntities 基类型的注册移到 AddXncfDatabaseModule（随 base.AddXncfModule 一起执行）

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive
{
    public partial class Register : IXncfDatabase  //注册 XNCF 模块数据库
    {
        #region IXncfDatabase 接口

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public const string DATABASE_PREFIX = "Senparc_NeuCharBoxLive_";

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// 动态获取数据库上下文
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        /// <summary>
        /// The module's own context applies its mappings here. ([XncfAutoConfigurationMapping] on them only feeds NCF's
        /// system SenparcEntities, not this context.)
        /// </summary>
        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new InviteCodeConfigurationMapping());
            modelBuilder.ApplyConfiguration(new ApplicationConfigurationMapping());
            modelBuilder.ApplyConfiguration(new RunConfigurationMapping());
            modelBuilder.ApplyConfiguration(new SettingsConfigurationMapping());
        }

        /// <summary>
        /// Called from base.AddXncfModule, right after NCF registers this module's context under its per-database type
        /// (…_Sqlite, …_SqlServer), which Register.AddXncfModule runs first. EfLiveStore and LiveSettingsService ask for the
        /// base type, which resolves to the one the host's DatabaseType selects.
        /// </summary>
        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            services.AddScoped<NeuCharBoxLiveSenparcEntities>(sp =>
                (NeuCharBoxLiveSenparcEntities)sp.GetRequiredService(MultipleDatabasePool.Instance.GetXncfDbContextType(GetType())));

            //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        }

        #endregion
    }
}
