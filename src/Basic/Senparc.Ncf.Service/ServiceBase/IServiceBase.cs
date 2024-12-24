using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public interface IServiceBase<T> : IServiceDataBase, IAutoDI where T : class, IEntityBase// global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        IMapper Mapper { get; set; }
        IRepositoryBase<T> RepositoryBase { get; set; }

        void BeginTransaction();
        void BeginTransaction(Action body, Action<Exception> rollbackAction = null);
        Task BeginTransactionAsync();
        Task BeginTransactionAsync(Action action);
        Task BeginTransactionAsync(Func<Task> body, Action<Exception> rollbackAction = null);
        Task BeginTransactionAsync(Func<Task> body, Func<Exception, Exception> rollbackAction);
        Task BeginTransactionAsync(Func<Task> bodyAsync, Func<Exception, Task<Exception>> rollbackActionAsync);
        void CommitTransaction();
        void DeleteAll(IEnumerable<T> objects);
        Task DeleteAllAsync(Expression<Func<T, bool>> where, Action<T> deleteItemAction = null, bool softDelete = false);
        Task DeleteAllAsync(IEnumerable<T> objects, Action<T> deleteItemAction = null, bool softDelete = false);
        void DeleteObject(Expression<Func<T, bool>> predicate);
        void DeleteObject(T obj);
        Task DeleteObjectAsync(Expression<Func<T, bool>> predicate);
        Task DeleteObjectAsync(T obj);
        int GetCount(Expression<Func<T, bool>> where, params string[] includes);
        int GetCount<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        Task<int> GetCountAsync(Expression<Func<T, bool>> where, params string[] includes);
        Task<int> GetCountAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        PagedList<T> GetFullList<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        PagedList<T> GetFullList<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes);
        Task<PagedList<T>> GetFullListAsync<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes);
        Task<PagedList<T>> GetFullListAsync(Expression<Func<T, bool>> where, string orderField = null, params string[] includes);
        Task<PagedList<T>> GetFullListAsync<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        Task<PagedList<T>> GetFullListAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc, string orderField = null);
        T GetObject(Expression<Func<T, bool>> where, params string[] includes);
        T GetObject<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        T GetObject<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        T GetObject<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes);
        Task<T> GetObjectAsync(Expression<Func<T, bool>> where, params string[] includes);
        Task<T> GetObjectAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        Task<T> GetObjectAsync<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        Task<T> GetObjectAsync<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes);
        PagedList<T> GetObjectList<TK, TIncludesProperty>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        PagedList<T> GetObjectList<TK>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes);
        Task<PagedList<T>> GetObjectListAsync(int pageIndex, int pageCount, Expression<Func<T, bool>> where, string orderBy, params string[] includes);
        Task<PagedList<T>> GetObjectListAsync<TIncludesProperty>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, string orderBy, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        Task<PagedList<T>> GetObjectListAsync<TK, TIncludesProperty>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        Task<PagedList<T>> GetObjectListAsync<TK>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes);
        decimal GetSum(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes);
        Task<decimal> GetSumAsync(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes);
        Task<decimal> GetSumAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc);
        bool IsInsert(T obj);
        void RollbackTransaction();
        void SaveObject(T obj);
        void SaveChanges();

        Task SaveObjectAsync(T obj);
        Task SaveChangesAsync();
        Task SaveObjectListAsync(IEnumerable<T> objs);
        void TryDetectChange(T obj);

        /// <summary>
        /// 使用 Mapper.Map&lt;TDto&gt;(entity) 快速返回
        /// </summary>
        /// <typeparam name="TDto"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        TDto Mapping<TDto>(T entity);
    }
}