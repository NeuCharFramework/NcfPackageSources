/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SenparcDbContextFactoryConfig.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：SenparcDbContextFactoryConfig


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.2.0-preview1 新增 MySql、PostgreSQL、Oracle、Dm 的设计时 DbContext 工厂

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>
    /// SenparcDbContextFactory 的公共配置
    /// </summary>
    public static class SenparcDbContextFactoryConfig
    {
        private static string? _rootDirectoryPath = null;

        /// <summary>
        /// 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息
        /// </summary>
        public static string RootDirectoryPath => _rootDirectoryPath ??= ResolveRootDirectoryPath();

        private static string ResolveRootDirectoryPath()
        {
            // NcfPackageSources layout: walk up to tools/NcfSimulatedSite/Senparc.Web (or a sibling Senparc.Web).
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
                {
                    var simulatedSite = Path.Combine(directory.FullName, "tools", "NcfSimulatedSite", "Senparc.Web");
                    if (Directory.Exists(simulatedSite))
                    {
                        return simulatedSite;
                    }
                    var siblingWeb = Path.Combine(directory.FullName, "Senparc.Web");
                    if (Directory.Exists(siblingWeb))
                    {
                        return siblingWeb;
                    }
                }
            }
            return Directory.GetCurrentDirectory();
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：dotnet ef migrations add [更新名称] -p src/Extensions/Senparc.Xncf.NeuCharBoxLive -c NeuCharBoxLiveSenparcEntities_MySql -o Domain/Migrations/MySql（在仓库根目录运行） </para>
    /// </summary>
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<NeuCharBoxLiveSenparcEntities_MySql, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDesignTimeDatabaseConfiguration");
        };

        public SenparcDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：dotnet ef migrations add [更新名称] -p src/Extensions/Senparc.Xncf.NeuCharBoxLive -c NeuCharBoxLiveSenparcEntities_PostgreSQL -o Domain/Migrations/PostgreSQL（在仓库根目录运行） </para>
    /// </summary>
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<NeuCharBoxLiveSenparcEntities_PostgreSQL, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：dotnet ef migrations add [更新名称] -p src/Extensions/Senparc.Xncf.NeuCharBoxLive -c NeuCharBoxLiveSenparcEntities_Oracle -o Domain/Migrations/Oracle（在仓库根目录运行） </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<NeuCharBoxLiveSenparcEntities_Oracle, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：dotnet ef migrations add [更新名称] -p src/Extensions/Senparc.Xncf.NeuCharBoxLive -c NeuCharBoxLiveSenparcEntities_Dm -o Domain/Migrations/Dm（在仓库根目录运行） </para>
    /// </summary>
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<NeuCharBoxLiveSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
        }
    }
}
