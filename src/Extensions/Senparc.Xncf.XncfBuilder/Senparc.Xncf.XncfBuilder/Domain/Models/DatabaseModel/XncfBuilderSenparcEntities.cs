/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfBuilderSenparcEntities.cs
    文件功能描述：XncfBuilderSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder
{
    public class XncfBuilderSenparcEntities : XncfDatabaseDbContext, IMultipleMigrationDbContext
    {
        public XncfBuilderSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Config> Configs { get; set; }

        public DbSet<XncfPreviewTask> XncfPreviewTasks { get; set; }

        public DbSet<XncfPreviewHost> XncfPreviewHosts { get; set; }

        /// <summary>
        /// Persistent source of truth for isolated create/modify/validate/merge jobs.
        /// Migration is intentionally supplied by the host administrator together with the
        /// existing XncfBuilder migrations.
        /// </summary>
        public DbSet<XncfDevelopmentJob> XncfDevelopmentJobs { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point

    }
}
