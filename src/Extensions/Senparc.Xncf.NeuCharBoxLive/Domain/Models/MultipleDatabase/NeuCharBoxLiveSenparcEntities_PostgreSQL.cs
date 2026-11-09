/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharBoxLiveSenparcEntities_PostgreSQL.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：NeuCharBoxLiveSenparcEntities_PostgreSQL


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增 PostgreSQL 数据库上下文

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class NeuCharBoxLiveSenparcEntities_PostgreSQL : NeuCharBoxLiveSenparcEntities
    {
        public NeuCharBoxLiveSenparcEntities_PostgreSQL(DbContextOptions<NeuCharBoxLiveSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
}
