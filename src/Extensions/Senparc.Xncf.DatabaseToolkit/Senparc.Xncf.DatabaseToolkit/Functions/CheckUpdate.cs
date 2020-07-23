using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Linq;

namespace Senparc.Xncf.DatabaseToolkit.Functions
{
    public class CheckUpdate : FunctionBase
    {
        public class CheckUpdate_Parameters : IFunctionParameter
        {

        }

        //注意：Name 必须在单个 Xscf 模块中唯一！
        public override string Name => "检测数据库是否需要更新";

        public override string Description => "使用 Entity Framework Core 的 Code First 模式中的 Migration 功能，检测系统当前数据库是否有未被更新的版本，如果有，请使用“Merge EF Core”方法进行更新。";

        public override Type FunctionParameterType => typeof(CheckUpdate_Parameters);

        public CheckUpdate(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<CheckUpdate_Parameters>(param, (typeParam, sb, result) =>
            {
                RecordLog(sb, "开始获取 ISenparcEntities 对象");
                var senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntities)) as SenparcEntitiesBase;
                RecordLog(sb, "获取 ISenparcEntities 对象成功");
                RecordLog(sb, "开始检测未安装版本");
                var pendingMigrations = senparcEntities.Database.GetPendingMigrations();

                var oldMigrations = senparcEntities.Database.GetAppliedMigrations();
                foreach (var item in oldMigrations)
                {
                    result.Message = "检测到当前已安装更新：" + item;
                }

                if (pendingMigrations.Count() == 0)
                {
                    result.Message = "未检测到官方 NCF 框架更新。";
                }
                else
                {
                    foreach (var item in pendingMigrations)
                    {
                        RecordLog(sb, "检测到未安装的新版本：" + item);
                    }
                    result.Message = $"检测到 {pendingMigrations.Count()} 个新版本，请使用“Merge EF Core”方法进行更新！";
                }
                RecordLog(sb, result.Message);
            });
        }
    }
}
