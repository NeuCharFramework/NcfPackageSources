/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseConfigAppService.cs
    File description: DatabaseConfigAppService related implementation

    Created by: Senparc - 20211012

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.26.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.27.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.27.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class DatabaseConfigAppService : AppServiceBase
    {
        /// <summary>
        /// Initializes the database configuration application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public DatabaseConfigAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }


        #region SetConfig

        public class SetConfigFunctionAppRequest : FunctionAppRequestBase
        {
            [Required]
            [MaxLength(300)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Config.Cycle")]
            public int BackupCycleMinutes { get; set; }

            [Required]
            [MaxLength(300)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Config.Path")]
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

        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.Settings.Name", "Function.Database.Settings.Description", typeof(Register))]
        /// <summary>
        /// Saves database backup cycle and path settings.
        /// </summary>
        /// <param name="request">The backup interval and path values.</param>
        /// <returns>A string response describing the saved values.</returns>
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

                var msg = DatabaseToolkitResource.Format("Database.Config.SavedValues", "设置间隔分钟：{0}，路径：{1}", request.BackupCycleMinutes, request.BackupPath);
                logger.Append(msg);
                return msg;
            }, afterFunc: (response, logger) =>
            {
                logger.Append(DatabaseToolkitResource.Get("Database.Config.Saved"));
            },
            saveLogAfterFinished: true,
            saveLogName: DatabaseToolkitResource.Get("Database.Config.SaveLogName"), 
            exceptionHandler: async (ex,response, logger) => { 
            
            
            }
            );
        }

        #endregion


        [ApiBind]
        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.ConfigTypes.Name", "Function.Database.ConfigTypes.Description", typeof(Register))]
        /// <summary>
        /// Reports the active database configuration type.
        /// </summary>
        /// <returns>A string response containing configuration and database type.</returns>
        public async Task<StringAppResponse> ShowDatabaseConfiguration()
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
                var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
                return logger.Append(DatabaseToolkitResource.Format("Database.Config.Current", "当前 DatabaseConfiguration：{0}，数据库类型：{1}", currentDatabaseConfiguration.GetType().Name, currentDatabaseConfiguration.MultipleDatabaseType));
            });
        }
    }
}
