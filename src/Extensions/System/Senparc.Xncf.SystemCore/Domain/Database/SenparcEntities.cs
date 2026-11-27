/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcEntities.cs
    File description: SenparcEntities related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.14.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.14.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
    /// Root system database context that dynamically applies the model configuration
    /// contributed by registered XNCF database modules.
    /// </summary>
    public partial class SenparcEntities : SenparcEntitiesBase, ISenparcEntitiesDbContext
    {
        /// <summary>
        /// Initializes the system database context with provider options and service access.
        /// </summary>
        /// <param name="dbContextOptions">The provider-specific EF Core options.</param>
        /// <param name="serviceProvider">The service provider used by the base context.</param>
        public SenparcEntities(DbContextOptions/*<SenparcEntities>*/ dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// The method enumerates registered database modules and delegates their
        /// model configuration to the shared EF Core model builder.
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Legacy section

            //after implementing the [XncfAutoConfigurationMapping] attribute, this runs automatically and no manual registration is needed
            //modelBuilder.ApplyConfiguration(new AdminUserInfoConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new FeedbackConfigurationMapping());

            #endregion

            #region Legacy section

            var dt1 = SystemTime.Now;
            Console.WriteLine();
            Console.WriteLine("============= SenparcEntities 动态加载 Start =============");
            var databaseList = XncfRegisterManager.XncfDatabaseList;

            // Calculate column widths once so the diagnostic table remains readable.
            int maxRegisterLength = databaseList.Max(db => db.GetType().FullName.Length);
            int maxDbContextTypeLength = databaseList.Max(db => db.TryGetXncfDatabaseDbContextType.Name.Length);
            Console.WriteLine($"{"Register".PadRight(maxRegisterLength)} | {"DbContextType"}");
            Console.WriteLine($"{new string('-',maxRegisterLength)} | {new string('-', maxDbContextTypeLength)}");

            foreach (var databaseRegister in databaseList)
            {
                string typeName = databaseRegister.GetType().FullName;
                string dbContextTypeName = databaseRegister.TryGetXncfDatabaseDbContextType.Name;

                Console.WriteLine($"{typeName.PadRight(maxRegisterLength)} | {dbContextTypeName}");

                databaseRegister.OnModelCreating(modelBuilder);
            }
            Console.WriteLine($"============= SenparcEntities 动态加载 End ({SystemTime.DiffTotalMS(dt1)}ms) =============");
            Console.WriteLine();
            #endregion

            #region Legacy section

            var dt2 = SystemTime.Now;
            Console.WriteLine("============= SenparcEntities 数据库实体注入 Start =============");

            //Legacy comment translated to English.
            Senparc.Ncf.XncfBase.Register.ApplyAllAutoConfigurationMapping(modelBuilder);

            var diffDt2 = SystemTime.DiffTotalMS(dt2);
            SenparcTrace.SendCustomLog("SenparcEntities 数据库实体注入结束", $"耗时：{diffDt2}ms");
            Console.WriteLine($"============= SenparcEntities 数据库实体注入 End（{diffDt2}ms） =============");
            Console.WriteLine();
            #endregion

            //Legacy comment translated to English.
            base.OnModelCreating(modelBuilder);
        }
    }
}
