/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SwaggerEntities.cs
    文件功能描述：SwaggerEntities 相关实现
    
    
    创建标识：Senparc - 20210724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Swagger.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Swagger
{
    public class SwaggerEntities : XncfDatabaseDbContext
    {
        public SwaggerEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<Config> Configs { get; set; }
    }
}
