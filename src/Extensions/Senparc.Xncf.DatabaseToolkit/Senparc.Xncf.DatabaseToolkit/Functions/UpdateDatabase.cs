using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;

namespace Senparc.Xncf.DatabaseToolkit.Functions
{
    public class UpdateDatabase : FunctionBase
    {
        public class UpdateDatabase_Parameters : IFunctionParameter
        {

        }

        //注意：Name 必须在单个 Xncf 模块中唯一！
        public override string Name => "Merge EF Core";

        public override string Description => "使用 Entity Framework Core 的 Code First 模式对数据库进行更新，使数据库和当前运行版本匹配。";

        public override Type FunctionParameterType => typeof(UpdateDatabase_Parameters);

        public UpdateDatabase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<UpdateDatabase_Parameters>(param, (typeParam, sb, result) =>
            {
                RecordLog(sb, "开始获取 ISenparcEntities 对象");
                ISenparcEntitiesDbContext senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntitiesDbContext)) as ISenparcEntitiesDbContext;
                RecordLog(sb, "获取 ISenparcEntities 对象成功");
                RecordLog(sb, "开始重新标记 Merge 状态");
                senparcEntities.ResetMigrate();
                RecordLog(sb, "开始执行 Migrate()");
                senparcEntities.Migrate();
                RecordLog(sb, "执行 Migrate() 结束，操作完成");
                result.Message = "操作完成，立即生效。";
            });
        }
    }
}
