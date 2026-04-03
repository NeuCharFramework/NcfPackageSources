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
    public partial class Register : IXncfDatabase  //Register the XNCF module database (optional)
    {
        #region IXncfDatabase 接口

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserTag_WeixinUserConfigurationMapping());
            modelBuilder.ApplyConfiguration(new WeixinUserConfigurationMapping());
            modelBuilder.ApplyConfiguration(new UserTagConfigurationMapping());
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //services.AddScoped<MpAccount>();
            services.AddScoped<MpAccountDto>();
            services.AddScoped<MpAccount_CreateOrUpdateDto>();

            //services.AddScoped<WeixinUser>();
            services.AddScoped<WeixinUserDto>();

            //services.AddScoped<UserTag>();
            //services.AddScoped<UserTag_WeixinUser>();

            //AutoMap mapping cannot be done here, because when the execution reaches here, the relevant process has already been executed.
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
