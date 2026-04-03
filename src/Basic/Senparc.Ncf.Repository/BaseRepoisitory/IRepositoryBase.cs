using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Senparc.Ncf.Repository
{
    public interface IRepositoryBase<T> : IDataBase, IAutoDI where T : class, IEntityBase// global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        bool IsInsert(T obj);

        IQueryable<T> GeAll<TOrderProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes);
        IQueryable<T> GeAll<TOrderProperty, TIncludesProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>
        /// Get paginated list
        /// </summary>
        /// <param name="where">Search conditions</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageCount">No paging when pageCount is less than or equal to 0</param>
        /// <returns></returns>
        PagedList<T> GetObjectList<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, params string[] includes);

        PagedList<T> GetObjectList<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);


        Task<PagedList<T>> GetObjectListAsync<TOrderProperty>(Expression<Func<T, bool>> where,
          Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount,
         params string[] includes);

        Task<PagedList<T>> GetObjectListAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where,
          Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount,
          Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);


        //Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<T, TIncludesProperty>> includesNavigationPropertyPathFunc);

        T GetFirstOrDefaultObject(Expression<Func<T, bool>> where, params string[] includes);
        T GetFirstOrDefaultObject<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        T GetFirstOrDefaultObject<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes);
        T GetFirstOrDefaultObject<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);


        int ObjectCount(Expression<Func<T, bool>> where, params string[] includes);
        int ObjectCount<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        decimal GetSum(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes);
        decimal GetSum<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        void Add(T obj);

        void Update(T obj);

        /// <summary>
        /// This method will automatically determine whether to perform Update (Update) or Add (Add)
        /// </summary>
        /// <param name="obj"></param>
        void Save(T obj);

        void Delete(T obj, bool softDelete = false);

        void SaveChanges();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task SaveAsync(T obj);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="softDelete"></param>
        /// <returns></returns>
        Task DeleteAsync(T obj, bool softDelete = false);

        Task SaveChangesAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        Task<T> GetFirstOrDefaultObjectAsync(Expression<Func<T, bool>> where, params string[] includes);
        Task<T> GetFirstOrDefaultObjectAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes);
        Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        Task<int> ObjectCountAsync(Expression<Func<T, bool>> where, params string[] includes);
        Task<int> ObjectCountAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        Task<decimal> GetSumAsync(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes);
        Task<decimal> GetSumAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>
        /// Batch delete
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="deleteItemAction">The operation of deleting each object</param>
        /// <param name="softDelete"></param>
        /// <returns></returns>
        Task DeleteAllAsync(IEnumerable<T> objs, Action<T> deleteItemAction = null, bool softDelete = false);

        /// <summary>
        /// Batch add
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="softDelete"></param>
        /// <returns></returns>
        Task AddAllAsync(IEnumerable<T> objs);

        /// <summary>
        ///Dynamic field sorting
        /// </summary>
        /// <param name="where"></param>
        /// <param name="OrderbyField">xxx desc, bbb asc</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageCount"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        Task<PagedList<T>> GetObjectListAsync(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, params string[] includes);
        Task<PagedList<T>> GetObjectListAsync<TIncludesProperty>(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        Task SaveObjectListAsync(IEnumerable<T> objs);

        /// <summary>
        /// start transaction
        /// </summary>
        /// <returns></returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// start transaction
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel);

        /// <summary>
        /// start transaction
        /// </summary>
        /// <returns></returns>
        void BeginTransaction();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        void BeginTransaction(System.Data.IsolationLevel isolationLevel);

        /// <summary>
        /// rollback transaction
        /// </summary>
        /// <returns></returns>
        void RollbackTransaction();

        /// <summary>
        /// Commit transaction
        /// </summary>
        void CommitTransaction();
    }
}