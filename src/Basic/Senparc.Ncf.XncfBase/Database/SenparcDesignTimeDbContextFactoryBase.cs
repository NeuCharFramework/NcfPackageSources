using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.XncfBase.Database
{
    /// <summary>
    /// 提供给数据库 Migration 使用的 DesignTimeDbContextFactory
    /// </summary>
    /// <typeparam name="TSenparcEntities"></typeparam>
    public abstract class SenparcDesignTimeDbContextFactoryBase<TSenparcEntities, TXncfDatabaseRegister>
        : IDesignTimeDbContextFactory<TSenparcEntities>
            where TSenparcEntities : XncfDatabaseDbContext
            where TXncfDatabaseRegister : class, IXncfDatabase, new()
    {
        public virtual string RootDictionaryPath => Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\");

        public virtual SenparcSetting SenparcSetting => new SenparcSetting();

        public virtual string SqlConnectionStr => SenparcDatabaseConfigs.ClientConnectionString ?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

        public virtual TSenparcEntities GetInstance(DbContextOptions<TSenparcEntities> dbContextOptions)
        {
            //获取 XncfDatabase 对象
            var databaseRegister = Activator.CreateInstance(typeof(TXncfDatabaseRegister)) as TXncfDatabaseRegister;
            //获取 XncfSenparcEntities 实例
            var xncfSenparcEntities = Activator.CreateInstance(databaseRegister.XncfDatabaseDbContextType, new object[] { dbContextOptions }) as TSenparcEntities;
            return xncfSenparcEntities;
        }

        public SenparcDesignTimeDbContextFactoryBase()
        {
            //if (!Senparc.CO2NET.RegisterServices.RegisterServiceExtension.SenparcGlobalServicesRegistered)
            //{
            //    //未执行 AddSenparcGlobalServices 注册，执行注册过程
            //    Host.CreateDefaultBuilder()
            //      .ConfigureWebHostDefaults(webBuilder =>
            //      {
            //          webBuilder.ConfigureServices((hostBuilder, services) =>
            //          {
            //              services.AddMemoryCache();//使用本地缓需要添加
            //          services.AddSenparcGlobalServices(hostBuilder.Configuration);
            //          });
            //      }).Build();
            //}

            Senparc.Ncf.Core.Register.TryRegisterMiniCore();
        }

        public virtual TSenparcEntities CreateDbContext(string[] args)
        {
            //修复 https://github.com/NeuCharFramework/NCF/issues/13 发现的问题（在非Web环境下无法得到网站根目录路径）

            IRegisterService co2netRegister = RegisterService.Start(SenparcSetting);
            CO2NET.Config.RootDictionaryPath = RootDictionaryPath;

            var register = System.Activator.CreateInstance<TXncfDatabaseRegister>() as TXncfDatabaseRegister;

            //配置数据库
            var builder = new DbContextOptionsBuilder<TSenparcEntities>();
            builder.UseSqlServer(SqlConnectionStr, b =>
            {
                register.DbContextOptionsAction(b, null);
                b.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: new int[] { 2 });
            });

            //还可以补充更多的数据库类型

            return GetInstance(builder.Options);
        }
    }
}
