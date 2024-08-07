﻿using System;
using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    /// <summary>
    /// 【注意】SenparcEntities 不存放任何实体，也不生成任何迁移文件
    /// </summary>
    public partial class SenparcEntities : SenparcEntitiesBase, ISenparcEntitiesDbContext
    {
        public SenparcEntities(DbContextOptions/*<SenparcEntities>*/ dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region 系统表

            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new AdminUserInfoConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new FeedbackConfigurationMapping());

            #endregion

            #region 其他动态模块的 OnModelCreating 过程注入到当前 DbContext

            Console.WriteLine("============= SenparcEntities 动态加载 Start =============");
            foreach (var databaseRegister in XncfRegisterManager.XncfDatabaseList)
            {
                Console.WriteLine("SenparcEntities 动态加载：" + databaseRegister.GetType().Name + " | DbContextType:" + databaseRegister.TryGetXncfDatabaseDbContextType.Name);
                databaseRegister.OnModelCreating(modelBuilder);
            }
            Console.WriteLine("============= SenparcEntities 动态加载 End =============");

            #endregion

            #region 【核心】全局自动注入（请勿改变此命令位置）

            //注册所有 XncfAutoConfigurationMapping 动态模块
            var dt1 = SystemTime.Now;
            Senparc.Ncf.XncfBase.Register.ApplyAllAutoConfigurationMapping(modelBuilder);
            SenparcTrace.SendCustomLog("SenparcEntities 数据库实体注入", $"耗时：{SystemTime.DiffTotalMS(dt1)}ms");
            Console.WriteLine($"SenparcEntities 数据库实体注入，耗时：{SystemTime.DiffTotalMS(dt1)}ms");
            Console.WriteLine();
            #endregion

            //基类中的系统表处理
            base.OnModelCreating(modelBuilder);
        }
    }
}
