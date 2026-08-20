/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WeixinSenparcEntities.cs
    文件功能描述：WeixinSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

        public WeixinSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }
    }
}
