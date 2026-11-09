/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.Database.cs
    文件功能描述：NeuCharBox Live 模块：Register.Database


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

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
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        }

        #endregion
    }
}
