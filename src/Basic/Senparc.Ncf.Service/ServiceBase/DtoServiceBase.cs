/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DtoServiceBase.cs
    File description: DtoServiceBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service.ServiceBase
{
    public class DtoServiceBase<TEntity, TEntityDto, TEntityInsertObj, TEntityEditObj> : ServiceBase<TEntity>
        where TEntity : class, IEntityBase, new()
        where TEntityDto : IDtoBase
    {
        public DtoServiceBase(IRepositoryBase<TEntity> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {

        }


        public override bool IsInsert(TEntity obj)
        {
            return RepositoryBase.IsInsert(obj);
        }

        public override TEntity GetObject(Expression<Func<TEntity, bool>> where,params string[] includes)
        {
            return RepositoryBase.GetFirstOrDefaultObject(where, includes);
        }

        public override TEntity GetObject<TK>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return RepositoryBase.GetFirstOrDefaultObject(where, orderBy, orderingType, includes);
        }

        public override PagedList<TEntity> GetFullList<TK>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return this.GetObjectList(0, 0, where, orderBy, orderingType, includes);
        }

        /// <summary>
        /// get paginated data
        /// </summary>
        /// <typeparam name="TK"></typeparam>
        ///Legacy comment translated to English.
        /// <param name="pageCount">items per page</param>
        /// <param name="where">condition</param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <param name="includes"></param>
        /// <returns></returns>
        public override PagedList<TEntity> GetObjectList<TK>(int pageIndex, int pageCount, Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return RepositoryBase.GetObjectList(where, orderBy, orderingType, pageIndex, pageCount, includes);
        }


        /// <summary>
        /// get paginated data
        /// </summary>
        /// <typeparam name="TK"></typeparam>
        ///Legacy comment translated to English.
        /// <param name="pageCount">items per page</param>
        /// <param name="where">condition</param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <param name="includes"></param>
        /// <returns></returns>
        public override async Task<PagedList<TEntity>> GetObjectListAsync<TK>(int pageIndex, int pageCount, Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return await RepositoryBase.GetObjectListAsync(where, orderBy, orderingType, pageIndex, pageCount, includes);
        }

        public override int GetCount(Expression<Func<TEntity, bool>> where,params string[] includes)
        {
            return RepositoryBase.ObjectCount(where, includes);
        }

        public override decimal GetSum(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, decimal>> sum,params string[] includes)
        {
            return RepositoryBase.GetSum(where, sum, includes);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="obj"></param>
        public override void TryDetectChange(TEntity obj)
        {
            if (!IsInsert(obj))
            {
                RepositoryBase.BaseDB.BaseDataContext.Entry(obj).State = EntityState.Modified;
            }
        }

        public override void SaveObject(TEntity obj)
        {
            if (RepositoryBase.BaseDB.ManualDetectChangeObject)
            {
                TryDetectChange(obj);
            }
            RepositoryBase.Save(obj);
        }

        public override void DeleteObject(Expression<Func<TEntity, bool>> predicate)
        {
            TEntity obj = GetObject(predicate);
            DeleteObject(obj);
        }

        public override void DeleteObject(TEntity obj)
        {
            RepositoryBase.Delete(obj);
        }

        public override void DeleteAll(IEnumerable<TEntity> objects)
        {
            var list = objects.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                DeleteObject(list[i]);
            }
        }

        //TODO: provideasyncversion
    }
}
