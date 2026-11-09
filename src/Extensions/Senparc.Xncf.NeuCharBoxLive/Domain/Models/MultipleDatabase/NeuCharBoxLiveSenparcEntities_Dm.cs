/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharBoxLiveSenparcEntities_Dm.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：NeuCharBoxLiveSenparcEntities_Dm


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.2.0-preview1 新增 Dm 数据库上下文

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class NeuCharBoxLiveSenparcEntities_Dm : NeuCharBoxLiveSenparcEntities
    {
        public NeuCharBoxLiveSenparcEntities_Dm(DbContextOptions<NeuCharBoxLiveSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
}
