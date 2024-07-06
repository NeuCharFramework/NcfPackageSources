using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    public class DatabaseUpdateAppService : AppServiceBase
    {
        public DatabaseUpdateAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 显示当前的数据库配置类型
        /// </summary>
        /// <returns></returns>
        [FunctionRender("检测数据库是否需要更新", "使用 Entity Framework Core 的 Code First 模式中的 Migration 功能，检测系统当前数据库是否有未被更新的版本，如果有，请使用“Merge EF Core”方法进行更新。", typeof(Register))]
        public async Task<StringAppResponse> ShowDatabaseConfiguration(/*BaseAppRequest request*/)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                logger.Append("开始获取 ISenparcEntities 对象");
                var senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntitiesDbContext)) as SenparcEntitiesBase;
                logger.Append("获取 ISenparcEntities 对象成功");
                logger.Append("开始检测未安装版本");
                var pendingMigrations = await senparcEntities.Database.GetPendingMigrationsAsync();

                var oldMigrations = await senparcEntities.Database.GetAppliedMigrationsAsync();
                foreach (var item in oldMigrations)
                {
                    response.Data = logger.Append("检测到当前已安装更新：" + item);
                }

                if (pendingMigrations.Count() == 0)
                {
                    response.Data = logger.Append("未检测到官方 NCF 框架更新。");
                }
                else
                {
                    foreach (var item in pendingMigrations)
                    {
                        logger.Append("检测到未安装的新版本：" + item);
                    }
                    response.Data = logger.Append($"检测到 {pendingMigrations.Count()} 个新版本，请使用“Merge EF Core”方法进行更新！");
                }
                return response.Data;
            });
        }

        [FunctionRender("Merge EF Core", "使用 Entity Framework Core 的 Code First 模式对数据库进行更新，使数据库和当前运行版本匹配。", typeof(Register))]
        public async Task<StringAppResponse> UpdateDatabase()
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                logger.Append("开始获取 ISenparcEntities 对象");
                ISenparcEntitiesDbContext senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntitiesDbContext)) as ISenparcEntitiesDbContext;
                logger.Append("获取 ISenparcEntities 对象成功");
                logger.Append("开始重新标记 Merge 状态");
                senparcEntities.ResetMigrate();
                logger.Append("开始执行 Migrate()");
                senparcEntities.Migrate();
                logger.Append("执行 Migrate() 结束，操作完成");
                return logger.Append("操作完成，立即生效。");
            });
        }
    }
}
