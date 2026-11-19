/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysButtonRespository.cs
    File description: SysButtonRespository related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview12 Updated repository infrastructure and related localization metadata

----------------------------------------------------------------*/

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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public async Task<int> DeleteButtonsByMenuId(string menuId)
        {
            //Legacy comment translated to English.
            //return await _senparcEntities.Database.ExecuteSqlCommandAsync($"DELETE {nameof(_senparcEntities.SysButtons)} WHERE {nameof(SysButton.MenuId)} = {{0}}", menuId);

            //return await _senparcEntities.Database.ExecuteSqlRawAsync($"DELETE {nameof(_senparcEntitie.SysButtons)} WHERE {nameof(SysButton.MenuId)} = {{0}}", menuId); 
            return await _senparcEntities.Database.ExecuteSqlRawAsync($"DELETE {"SysButtons"} WHERE {nameof(SysButton.MenuId)} = {{0}}", menuId);
        }
    }
}
