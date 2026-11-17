/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WeixinSenparcEntities.cs
    File description: WeixinSenparcEntities related implementation
    
    
    Created by: Senparc - 20250712
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add WeixinClawBindingProfile management and UI enhancements

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    public class WeixinSenparcEntities : XncfDatabaseDbContext
    {
        public DbSet<MpAccount> MpAccounts { get; set; }
        public DbSet<WeixinUser> WeixinUsers { get; set; }
        public DbSet<UserTag> UserTags { get; set; }
        public DbSet<UserTag_WeixinUser> UserTags_WeixinUsers { get; set; }
        public DbSet<WeixinClawAccount> WeixinClawAccounts { get; set; }
        public DbSet<WeixinClawMessageReceipt> WeixinClawMessageReceipts { get; set; }
        public DbSet<WeixinClawMessageRecord> WeixinClawMessageRecords { get; set; }
        public DbSet<WeixinClawBindingProfile> WeixinClawBindingProfiles { get; set; }

        public WeixinSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }
    }
}
