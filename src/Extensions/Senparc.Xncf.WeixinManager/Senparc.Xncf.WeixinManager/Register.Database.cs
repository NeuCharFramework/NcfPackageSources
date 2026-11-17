/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Mapping;

namespace Senparc.Xncf.WeixinManager
{
    public partial class Register : IXncfDatabase  //register the XNCF module database (optional)
    {
        #region Legacy section

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserTag_WeixinUserConfigurationMapping());
            modelBuilder.ApplyConfiguration(new WeixinUserConfigurationMapping());
            modelBuilder.ApplyConfiguration(new UserTagConfigurationMapping());
            modelBuilder.ApplyConfiguration(new WeixinClawConfigurationMapping());
            modelBuilder.ApplyConfiguration(new WeixinClawMessageReceiptConfigurationMapping());
            modelBuilder.ApplyConfiguration(new WeixinClawMessageRecordConfigurationMapping());
            modelBuilder.ApplyConfiguration(new WeixinClawBindingProfileConfigurationMapping());
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //services.AddScoped<MpAccount>();
            services.AddScoped<MpAccountDto>();
            services.AddScoped<MpAccount_CreateOrUpdateDto>();

            //services.AddScoped<WeixinUser>();
            services.AddScoped<WeixinUserDto>();
            services.AddScoped<WeixinClawAccountDto>();
            services.AddScoped<WeixinClawMessageRecordDto>();
            services.AddScoped<WeixinClawConversationDto>();
            services.AddScoped<WeixinClawBindingProfileDto>();

            //services.AddScoped<UserTag>();
            //services.AddScoped<UserTag_WeixinUser>();

            //Legacy comment translated to English.
            //base.AddAutoMapMapping(profile =>
            //{
            //});
        }

        public const string DATABASE_PREFIX = "WeixinManager_";

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type XncfDatabaseDbContextType => typeof(WeixinSenparcEntities);

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);


        #endregion
    }
}
