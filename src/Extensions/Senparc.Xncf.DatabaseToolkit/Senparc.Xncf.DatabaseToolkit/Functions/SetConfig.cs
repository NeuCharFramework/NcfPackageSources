using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Senparc.CO2NET.Trace;

namespace Senparc.Xncf.DatabaseToolkit.Functions
{
    public class SetConfig : FunctionBase
    {
        public class SetConfig_Parameters : FunctionParameterLoadDataBase, IFunctionParameter
        {
            [Required]
            [MaxLength(300)]
            [Description("自动备份周期（分钟）||0 则为不自动备份")]
            public int BackupCycleMinutes { get; set; }
            [Required]
            [MaxLength(300)]
            [Description("备份路径||本地物理路径，如：E:\\Senparc\\Scf\\NCF.bak")]
            public string BackupPath { get; set; }

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                var configService = serviceProvider.GetService<ServiceBase<DbConfig>>();
                var config = await configService.GetObjectAsync(z => true);
                if (config != null)
                {
                    BackupCycleMinutes = config.BackupCycleMinutes;
                    BackupPath = config.BackupPath;
                }
            }
        }

        //注意：Name 必须在单个 Xscf 模块中唯一！
        public override string Name => "设置参数";

        public override string Description => "设置备份间隔时间、备份文件路径等参数";

        public override Type FunctionParameterType => typeof(SetConfig_Parameters);

        public SetConfig(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<SetConfig_Parameters>(param, (typeParam, sb, result) =>
             {
                 //RecordLog(sb, "开始获取 ISenparcEntities 对象");
                 //var senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntities)) as SenparcEntitiesBase;
                 //RecordLog(sb, "获取 ISenparcEntities 对象成功");

                 var configService = base.ServiceProvider.GetService<ServiceBase<DbConfig>>();
                 var config = configService.GetObject(z => true);
                 if (config == null)
                 {
                     config = new DbConfig(typeParam.BackupCycleMinutes, typeParam.BackupPath);
                 }
                 else
                 {
                     configService.Mapper.Map(typeParam, config);
                 }
                 configService.SaveObject(config);

                 result.Message = "设置已保存！";
             });
        }
    }
}
