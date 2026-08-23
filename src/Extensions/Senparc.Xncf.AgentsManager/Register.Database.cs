/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Database.cs
    文件功能描述：Register.Database 相关实现


    创建标识：Senparc - 20240616

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping;

namespace Senparc.Xncf.AgentsManager
{
    public partial class Register : IXncfDatabase  //注册 XNCF 模块数据库（按需选用）
    {
        #region IXncfDatabase 接口

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public const string DATABASE_PREFIX = "Senparc_AgentsManager_";

        /// <summary>
        /// 数据库前缀
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// 动态获取数据库上下文
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new AreaTemplate_ColorConfigurationMapping());

            modelBuilder.ApplyConfiguration(new ChatGroupConfigurationMapping());
            modelBuilder.ApplyConfiguration(new AgentTemplateConfigurationMapping());
            // 远程 A2A Agent 的映射必须显式注册：当前模块的设计时 DbContext
            // 不会自动扫描新增的 Mapping 类型。缺少此处会导致索引与 NoAction 删除策略
            // 没有写入数据库迁移。
            modelBuilder.ApplyConfiguration(new RemoteAgentConfigurationMapping());
            modelBuilder.ApplyConfiguration(new ChatGroupRemoteMemberConfigurationMapping());
            modelBuilder.ApplyConfiguration(new PublishedA2AAgentConfigurationMapping());
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
            //ex. services.AddScoped(typeof(Color));
        }

        #endregion
    }
}
