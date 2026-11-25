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
    /// <summary>
    /// Provides the standard service operations for an entity while preserving the DTO type
    /// parameters used by higher-level application services.
    /// </summary>
    /// <typeparam name="TEntity">The persisted entity type managed by the service.</typeparam>
    /// <typeparam name="TEntityDto">The DTO type used when exposing the entity.</typeparam>
    /// <typeparam name="TEntityInsertObj">The input type used when creating an entity.</typeparam>
    /// <typeparam name="TEntityEditObj">The input type used when editing an entity.</typeparam>
    public class DtoServiceBase<TEntity, TEntityDto, TEntityInsertObj, TEntityEditObj> : ServiceBase<TEntity>
        where TEntity : class, IEntityBase, new()
        where TEntityDto : IDtoBase
    {
        /// <summary>
        /// Initializes a DTO-aware service with the repository and dependency-injection provider
        /// used by the base service implementation.
        /// </summary>
        /// <param name="repo">The repository used to query and persist <typeparamref name="TEntity"/> instances.</param>
        /// <param name="serviceProvider">The service provider used to resolve service dependencies.</param>
        public DtoServiceBase(IRepositoryBase<TEntity> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {

        }

        /// <inheritdoc/>
        public override bool IsInsert(TEntity obj)
        {
            return RepositoryBase.IsInsert(obj);
        }
        /// <inheritdoc/>
        public override TEntity GetObject(Expression<Func<TEntity, bool>> where,params string[] includes)
        {
            return RepositoryBase.GetFirstOrDefaultObject(where, includes);
        }
        /// <inheritdoc/>
        public override TEntity GetObject<TK>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return RepositoryBase.GetFirstOrDefaultObject(where, orderBy, orderingType, includes);
        }
        /// <inheritdoc/>
        public override PagedList<TEntity> GetFullList<TK>(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return this.GetObjectList(0, 0, where, orderBy, orderingType, includes);
        }

        /// <inheritdoc/>
        public override PagedList<TEntity> GetObjectList<TK>(int pageIndex, int pageCount, Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return RepositoryBase.GetObjectList(where, orderBy, orderingType, pageIndex, pageCount, includes);
        }


        /// <inheritdoc/>
        public override async Task<PagedList<TEntity>> GetObjectListAsync<TK>(int pageIndex, int pageCount, Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return await RepositoryBase.GetObjectListAsync(where, orderBy, orderingType, pageIndex, pageCount, includes);
        }
        /// <inheritdoc/>
        public override int GetCount(Expression<Func<TEntity, bool>> where,params string[] includes)
        {
            return RepositoryBase.ObjectCount(where, includes);
        }
        /// <inheritdoc/>
        public override decimal GetSum(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, decimal>> sum,params string[] includes)
        {
            return RepositoryBase.GetSum(where, sum, includes);
        }

        /// <inheritdoc/>
        public override void TryDetectChange(TEntity obj)
        {
            if (!IsInsert(obj))
            {
                RepositoryBase.BaseDB.BaseDataContext.Entry(obj).State = EntityState.Modified;
            }
        }
        /// <inheritdoc/>
        public override void SaveObject(TEntity obj)
        {
            if (RepositoryBase.BaseDB.ManualDetectChangeObject)
            {
                TryDetectChange(obj);
            }
            RepositoryBase.Save(obj);
        }
        /// <inheritdoc/>
        public override void DeleteObject(Expression<Func<TEntity, bool>> predicate)
        {
            TEntity obj = GetObject(predicate);
            DeleteObject(obj);
        }
        /// <inheritdoc/>
        public override void DeleteObject(TEntity obj)
        {
            RepositoryBase.Delete(obj);
        }
        /// <inheritdoc/>
        public override void DeleteAll(IEnumerable<TEntity> objects)
        {
            var list = objects.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                DeleteObject(list[i]);
            }
        }

    }
}
