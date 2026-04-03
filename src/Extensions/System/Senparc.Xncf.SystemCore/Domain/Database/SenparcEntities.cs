using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    /// <summary>
    /// [Note] SenparcEntities does not store any entities or generate any migration files.
    /// </summary>
    public partial class SenparcEntities : SenparcEntitiesBase, ISenparcEntitiesDbContext
    {
        public SenparcEntities(DbContextOptions/*<SenparcEntities>*/ dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region 系统表

            //After implementing the [XncfAutoConfigurationMapping] feature, it can be executed automatically without adding it manually.
            //modelBuilder.ApplyConfiguration(new AdminUserInfoConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new FeedbackConfigurationMapping());

            #endregion

            #region 其他动态模块的 OnModelCreating 过程注入到当前 DbContext

            var dt1 = SystemTime.Now;
            Console.WriteLine();
            Console.WriteLine("============= SenparcEntities 动态加载 Start =============");
            var databaseList = XncfRegisterManager.XncfDatabaseList;

            // Calculate the maximum length of all type names  
            int maxRegisterLength = databaseList.Max(db => db.GetType().FullName.Length);
            int maxDbContextTypeLength = databaseList.Max(db => db.TryGetXncfDatabaseDbContextType.Name.Length);
            Console.WriteLine($"{"Register".PadRight(maxRegisterLength)} | {"DbContextType"}");
            Console.WriteLine($"{new string('-',maxRegisterLength)} | {new string('-', maxDbContextTypeLength)}");

            foreach (var databaseRegister in databaseList)
            {
                string typeName = databaseRegister.GetType().FullName;
                string dbContextTypeName = databaseRegister.TryGetXncfDatabaseDbContextType.Name;

                // Use string formatting to align output  
                Console.WriteLine($"{typeName.PadRight(maxRegisterLength)} | {dbContextTypeName}");

                databaseRegister.OnModelCreating(modelBuilder);
            }
            Console.WriteLine($"============= SenparcEntities 动态加载 End ({SystemTime.DiffTotalMS(dt1)}ms) =============");
            Console.WriteLine();
            #endregion

            #region 【核心】全局自动注入（请勿改变此命令位置）

            var dt2 = SystemTime.Now;
            Console.WriteLine("============= SenparcEntities 数据库实体注入 Start =============");

            //Register all XncfAutoConfigurationMapping dynamic modules
            Senparc.Ncf.XncfBase.Register.ApplyAllAutoConfigurationMapping(modelBuilder);

            var diffDt2 = SystemTime.DiffTotalMS(dt2);
            SenparcTrace.SendCustomLog("SenparcEntities 数据库实体注入结束", $"耗时：{diffDt2}ms");
            Console.WriteLine($"============= SenparcEntities 数据库实体注入 End（{diffDt2}ms） =============");
            Console.WriteLine();
            #endregion

            //System table handling in base classes
            base.OnModelCreating(modelBuilder);
        }
    }
}
