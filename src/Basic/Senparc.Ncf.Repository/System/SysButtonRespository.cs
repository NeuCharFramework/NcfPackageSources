using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Respository
{
    public interface ISysButtonRespository : IClientRepositoryBase<SysButton>
    {
        /// <summary>
        /// 删除某个页面下的所有按钮
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        Task<int> DeleteButtonsByMenuId(string menuId);
    }

    public class SysButtonRespository : ClientRepositoryBase<SysButton>, ISysButtonRespository
    {
        private readonly SenparcEntitiesBase _senparcEntities;

        public SysButtonRespository(INcfDbData db) : base(db)
        {
            _senparcEntities = db.BaseDataContext as SenparcEntitiesBase;
        }

        /// <summary>
        /// 删除某个页面下的所有按钮
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public async Task<int> DeleteButtonsByMenuId(string menuId)
        {
            //升级至 EF Core 5.0 后方法无效
            //return await _senparcEntities.Database.ExecuteSqlCommandAsync($"DELETE {nameof(_senparcEntities.SysButtons)} WHERE {nameof(SysButton.MenuId)} = {{0}}", menuId);

            return await _senparcEntities.Database.ExecuteSqlRawAsync($"DELETE {nameof(_senparcEntities.SysButtons)} WHERE {nameof(SysButton.MenuId)} = {{0}}", menuId);
        }
    }
}
