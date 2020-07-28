using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;

namespace Senparc.Xncf.DatabaseToolkit.Functions
{
    public class ExportSQL : FunctionBase
    {
        public class ExportSQL_Parameters : IFunctionParameter
        {

        }

        //注意：Name 必须在单个 Xncf 模块中唯一！
        public override string Name => "导出当前数据库 SQL 脚本";

        public override string Description => "导出当前站点正在使用的所有表的 SQL 脚本";

        public override Type FunctionParameterType => typeof(ExportSQL_Parameters);

        public ExportSQL(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<ExportSQL_Parameters>(param, (typeParam, sb, result) =>
             {
                 RecordLog(sb, "开始获取 ISenparcEntities 对象");
                 var senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntities)) as SenparcEntitiesBase;
                 RecordLog(sb, "获取 ISenparcEntities 对象成功");
                 RecordLog(sb, "开始生成 SQL ");
                 var sql = senparcEntities.Database.GenerateCreateScript();
                 RecordLog(sb, "SQL 已生成：");
                 RecordLog(sb, $"============ NCF Database {SystemTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  ============");
                 RecordLog(sb, "\r\n\r\n" + sql + "\r\n\r\n");
                 RecordLog(sb, $"============ NCF Database {SystemTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  ============");
                 result.Message = "SQL 已生成，请下载日志文件！";
             });
        }
    }
}
