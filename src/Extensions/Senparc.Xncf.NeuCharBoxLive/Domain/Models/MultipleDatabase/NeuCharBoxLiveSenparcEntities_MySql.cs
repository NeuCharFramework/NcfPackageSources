/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharBoxLiveSenparcEntities_MySql.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：NeuCharBoxLiveSenparcEntities_MySql


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.2.0-preview1 新增 MySql 数据库上下文

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class NeuCharBoxLiveSenparcEntities_MySql : NeuCharBoxLiveSenparcEntities
    {
        public NeuCharBoxLiveSenparcEntities_MySql(DbContextOptions<NeuCharBoxLiveSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
}
