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
        /// Get paging data
        /// </summary>
        /// <typeparam name="TK"></typeparam>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageCount">Number per page</param>
        /// <param name="where">Condition</param>
        /// <param name="orderBy">Sort field</param>
        /// <param name="orderingType">Forward sequence | Flashback</param>
        /// <param name="includes"></param>
        /// <returns></returns>
        public override PagedList<TEntity> GetObjectList<TK>(int pageIndex, int pageCount, Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, TK>> orderBy, OrderingType orderingType,params string[] includes)
        {
            return RepositoryBase.GetObjectList(where, orderBy, orderingType, pageIndex, pageCount, includes);
        }


        /// <summary>
        /// Get paging data
        /// </summary>
        /// <typeparam name="TK"></typeparam>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageCount">Number per page</param>
        /// <param name="where">Condition</param>
        /// <param name="orderBy">Sort field</param>
        /// <param name="orderingType">Forward sequence | Flashback</param>
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
        /// Force the entity to Modified state
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

        //TODO: Provide an asynchronous version
    }
}
