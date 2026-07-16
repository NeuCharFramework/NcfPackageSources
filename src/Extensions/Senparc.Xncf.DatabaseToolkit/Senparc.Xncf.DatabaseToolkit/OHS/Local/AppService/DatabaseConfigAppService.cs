/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseConfigAppService.cs
    文件功能描述：DatabaseConfigAppService 相关实现
    
    
    创建标识：Senparc - 20211012
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    public class DatabaseConfigAppService : AppServiceBase
    {
        public DatabaseConfigAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }


        #region SetConfig

        public class SetConfigFunctionAppRequest : FunctionAppRequestBase
        {
            [Required]
            [MaxLength(300)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.Config.Cycle")]
            public int BackupCycleMinutes { get; set; }

            [Required]
            [MaxLength(300)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.Config.Path")]
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

        [FunctionRender(typeof(NcfBuiltInResource), "Function.Database.Settings.Name", "Function.Database.Settings.Description", typeof(Register))]
        public async Task<StringAppResponse> SetConfig(SetConfigFunctionAppRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var configService = base.ServiceProvider.GetService<ServiceBase<DbConfig>>();
                var config = configService.GetObject(z => true);
                if (config == null)
                {
                    config = new DbConfig(request.BackupCycleMinutes, request.BackupPath);
                }
                else
                {
                    //configService.Mapper.Map(request, config);
                    config.SetConfig(request.BackupCycleMinutes, request.BackupPath);
                }
                configService.SaveObject(config);

                var msg = NcfBuiltInResource.Format("Database.Config.SavedValues", "设置间隔分钟：{0}，路径：{1}", request.BackupCycleMinutes, request.BackupPath);
                logger.Append(msg);
                return msg;
            }, afterFunc: (response, logger) =>
            {
                logger.Append(NcfBuiltInResource.Get("Database.Config.Saved"));
            },
            saveLogAfterFinished: true,
            saveLogName: NcfBuiltInResource.Get("Database.Config.SaveLogName"), 
            exceptionHandler: async (ex,response, logger) => { 
            
            
            }
            );
        }

        #endregion


        [ApiBind]
        [FunctionRender(typeof(NcfBuiltInResource), "Function.Database.ConfigTypes.Name", "Function.Database.ConfigTypes.Description", typeof(Register))]
        public async Task<StringAppResponse> ShowDatabaseConfiguration()
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
                var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
                return logger.Append(NcfBuiltInResource.Format("Database.Config.Current", "当前 DatabaseConfiguration：{0}，数据库类型：{1}", currentDatabaseConfiguration.GetType().Name, currentDatabaseConfiguration.MultipleDatabaseType));
            });
        }
    }
}
