/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ServiceBase.cs
    File description: ServiceBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Query;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Ncf.Service
{
    public class ServiceBase<T> : ServiceDataBase, IServiceBase<T> where T : class, IEntityBase// global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        public IMapper Mapper { get; set; } //TODO: add in to Wapper

        public IRepositoryBase<T> RepositoryBase { get; set; }
        protected IServiceProvider _serviceProvider => base.ServiceProvider;

        /// <summary>
        /// Initializes a service over the repository for the entity type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="repo">The repository that owns persistence operations for the entity.</param>
        /// <param name="serviceProvider">The provider used to resolve optional infrastructure services such as AutoMapper.</param>
        public ServiceBase(IRepositoryBase<T> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            //_serviceProvider = serviceProvider;
            RepositoryBase = repo;
            Mapper = _serviceProvider.GetService<IMapper>();
        }

        #region Insert & DetectChange

        /// <summary>
        /// Marks an existing entity as modified when manual change detection is in use.
        /// New or detached entities are left to the repository's insert detection rules.
        /// </summary>
        /// <param name="obj">The entity whose tracked state should be inspected.</param>
        public virtual void TryDetectChange(T obj)
        {
            if (!IsInsert(obj))
            {
                RepositoryBase.BaseDB.BaseDataContext.Entry(obj).State = EntityState.Modified;
            }
        }

        /// <summary>
        /// Determines whether the repository should treat an entity as a new record.
        /// </summary>
        /// <param name="obj">The entity to inspect.</param>
        /// <returns><see langword="true"/> when the repository reports an insert state; otherwise, <see langword="false"/>.</returns>
        public virtual bool IsInsert(T obj)
        {
            return RepositoryBase.IsInsert(obj);
        }

        #endregion


        #region GetObject

        /// <summary>
        /// Retrieves the first entity matching a predicate and optional string-based navigation includes.
        /// </summary>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>The first matching entity, or the default value when no entity matches.</returns>
        public virtual T GetObject(Expression<Func<T, bool>> where, params string[] includes)
        {
            return RepositoryBase.GetFirstOrDefaultObject(where, includes);
        }

        /// <summary>
        /// Asynchronously retrieves the first entity matching a predicate and optional string-based navigation includes.
        /// </summary>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with the first matching entity, or the default value.</returns>
        public virtual async Task<T> GetObjectAsync(Expression<Func<T, bool>> where, params string[] includes)
        {
            return await RepositoryBase.GetFirstOrDefaultObjectAsync(where, includes);
        }

        /// <summary>
        /// Retrieves the first matching entity using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed navigation include expression.</param>
        /// <returns>The first matching entity, or the default value when no entity matches.</returns>
        public virtual T GetObject<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return RepositoryBase.GetFirstOrDefaultObject(where, includesNavigationPropertyPathFunc);
        }

        /// <summary>
        /// Asynchronously retrieves the first matching entity using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed navigation include expression.</param>
        /// <returns>A task that completes with the first matching entity, or the default value.</returns>
        public virtual async Task<T> GetObjectAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await RepositoryBase.GetFirstOrDefaultObjectAsync(where, includesNavigationPropertyPathFunc);
        }

        /// <summary>
        /// Retrieves the first entity matching a predicate after applying the requested ordering and includes.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="orderBy">The expression used to order matching entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>The first ordered match, or the default value when no entity matches.</returns>
        public virtual T GetObject<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes)
        {
            return RepositoryBase.GetFirstOrDefaultObject(where, orderBy, orderingType, includes);
        }

        /// <summary>
        /// Asynchronously retrieves the first entity matching a predicate after applying the requested ordering and includes.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="orderBy">The expression used to order matching entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with the first ordered match, or the default value.</returns>
        public virtual async Task<T> GetObjectAsync<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes)
        {
            return await RepositoryBase.GetFirstOrDefaultObjectAsync(where, orderBy, orderingType, includes);
        }

        /// <summary>
        /// Retrieves the first ordered entity using a strongly typed navigation include expression.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="orderBy">The expression used to order matching entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed navigation include expression.</param>
        /// <returns>The first ordered match, or the default value when no entity matches.</returns>
        public virtual T GetObject<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return RepositoryBase.GetFirstOrDefaultObject<TK, TIncludesProperty>(where, orderBy, orderingType, includesNavigationPropertyPathFunc);
        }

        /// <summary>
        /// Asynchronously retrieves the first ordered entity using a strongly typed navigation include expression.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select the entity.</param>
        /// <param name="orderBy">The expression used to order matching entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed navigation include expression.</param>
        /// <returns>A task that completes with the first ordered match, or the default value.</returns>
        public virtual async Task<T> GetObjectAsync<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await RepositoryBase.GetFirstOrDefaultObjectAsync(where, orderBy, orderingType, includesNavigationPropertyPathFunc);
        }

        #endregion

        #region GetFullList

        /// <summary>
        /// Retrieves all matching entities as a paged-list object with an unbounded page request.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A list containing all matching entities and paging metadata.</returns>
        public virtual PagedList<T> GetFullList<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes)
        {
            return this.GetObjectList(0, 0, where, orderBy, orderingType, includes);
        }

        /// <summary>
        /// Asynchronously retrieves all matching entities as a paged-list object with an unbounded page request.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with all matching entities and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetFullListAsync<TK>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes)
        {
            return await this.GetObjectListAsync(0, 0, where, orderBy, orderingType, includes);
        }

        /// <summary>
        /// Asynchronously retrieves all matching entities using a provider-specific order expression.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderField">The textual order expression, such as <c>Id desc</c>.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with all matching entities and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetFullListAsync(Expression<Func<T, bool>> where, string orderField = null, params string[] includes)
        {
            return await RepositoryBase.GetObjectListAsync(where, orderField, 0, 0, includes);
        }

        /// <summary>
        /// Retrieves all matching entities with expression-based ordering and eager-loading.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation property type included by the query.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includesNavigationPropertyPathFunc">The include expression applied to the query.</param>
        /// <returns>All matching entities and paging metadata.</returns>
        public virtual PagedList<T> GetFullList<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return this.GetObjectList<TK, TIncludesProperty>(0, 0, where, orderBy, orderingType, includesNavigationPropertyPathFunc);
        }

        /// <summary>
        /// Asynchronously retrieves all matching entities with expression-based ordering and eager-loading.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>All matching entities and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetFullListAsync<TK, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await RepositoryBase.GetObjectListAsync(where, orderBy, orderingType, 0, 0, includesNavigationPropertyPathFunc);
        }

        /// <summary>
        /// Asynchronously retrieves all matching entities using a textual order expression and typed eager-loading.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderField">The textual order expression.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>All matching entities and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetFullListAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc, string orderField = null)
        {
            return await RepositoryBase.GetObjectListAsync(where, orderField, 0, 0, includesNavigationPropertyPathFunc);
        }

        #endregion

        #region Legacy section

        /// <summary>
        /// Retrieves a page using expression-based ordering and string include paths.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageCount">The maximum number of items in the page.</param>
        /// <param name="where">The entity-selection predicate.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>The requested page and paging metadata.</returns>
        public virtual PagedList<T> GetObjectList<TK>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes)
        {
            return RepositoryBase.GetObjectList(where, orderBy, orderingType, pageIndex, pageCount, includes);
        }

        /// <summary>
        /// Asynchronously retrieves a page using expression-based ordering and string include paths.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageCount">The maximum number of items in the page.</param>
        /// <param name="where">The entity-selection predicate.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>The asynchronously loaded page and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync<TK>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, params string[] includes)
        {
            return await RepositoryBase.GetObjectListAsync(where, orderBy, orderingType, pageIndex, pageCount, includes);
        }

        /// <summary>
        /// Asynchronously retrieves a page using a textual order expression and string include paths.
        /// </summary>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageCount">The maximum number of items in the page.</param>
        /// <param name="where">The entity-selection predicate.</param>
        /// <param name="orderBy">The textual order expression.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>The asynchronously loaded page and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync(int pageIndex, int pageCount, Expression<Func<T, bool>> where, string orderBy, params string[] includes)
        {
            return await RepositoryBase.GetObjectListAsync(where, orderBy, pageIndex, pageCount, includes);
        }

        /// <summary>
        /// Retrieves a page of entities with expression-based ordering and eager-loading.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation property type included by the query.</typeparam>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageCount">The maximum number of items in the page.</param>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The direction applied to <paramref name="orderBy"/>.</param>
        /// <param name="includesNavigationPropertyPathFunc">The include expression applied to the query.</param>
        /// <returns>The requested page and paging metadata.</returns>
        public virtual PagedList<T> GetObjectList<TK, TIncludesProperty>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return RepositoryBase.GetObjectList<TK, TIncludesProperty>(where, orderBy, orderingType, pageIndex, pageCount, includesNavigationPropertyPathFunc);
        }

        /// <summary>
        /// Asynchronously retrieves a page using a textual order expression and typed eager-loading.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageCount">The maximum number of items in the page.</param>
        /// <param name="where">The entity-selection predicate.</param>
        /// <param name="orderBy">The textual order expression.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>The asynchronously loaded page and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync<TIncludesProperty>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, string orderBy, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await RepositoryBase.GetObjectListAsync<TIncludesProperty>(where, orderBy, pageIndex, pageCount, includesNavigationPropertyPathFunc);
        }


        /// <summary>
        /// Asynchronously retrieves a page using expression-based ordering and typed eager-loading.
        /// </summary>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The included navigation property type.</typeparam>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageCount">The maximum number of items in the page.</param>
        /// <param name="where">The entity-selection predicate.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The typed include expression.</param>
        /// <returns>The asynchronously loaded page and paging metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync<TK, TIncludesProperty>(int pageIndex, int pageCount, Expression<Func<T, bool>> where, Expression<Func<T, TK>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await RepositoryBase.GetObjectListAsync<TK, TIncludesProperty>(where, orderBy, orderingType, pageIndex, pageCount, includesNavigationPropertyPathFunc);
        }


        #endregion

        #region GetCount

        /// <summary>
        /// Counts entities matching a predicate, optionally including navigation paths used by the query.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includes">Navigation property paths applied to the query.</param>
        /// <returns>The number of matching entities.</returns>
        public virtual int GetCount(Expression<Func<T, bool>> where, params string[] includes)
        {
            return RepositoryBase.ObjectCount(where, includes);
        }

        /// <summary>
        /// Asynchronously counts entities matching a predicate.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includes">Navigation property paths applied to the query.</param>
        /// <returns>A task that completes with the number of matching entities.</returns>
        public virtual async Task<int> GetCountAsync(Expression<Func<T, bool>> where, params string[] includes)
        {
            return await RepositoryBase.ObjectCountAsync(where, includes);
        }

        /// <summary>
        /// Counts matching entities after applying an include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation property type included by the query.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includesNavigationPropertyPathFunc">The include expression applied to the query.</param>
        /// <returns>The number of matching entities.</returns>
        public virtual int GetCount<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return RepositoryBase.ObjectCount(where, includesNavigationPropertyPathFunc);
        }

        /// <summary>
        /// Asynchronously counts matching entities after applying an include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation property type included by the query.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includesNavigationPropertyPathFunc">The include expression applied to the query.</param>
        /// <returns>A task that completes with the number of matching entities.</returns>
        public virtual async Task<int> GetCountAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await RepositoryBase.ObjectCountAsync(where, includesNavigationPropertyPathFunc);
        }

        #endregion

        #region GetSum

        /// <summary>
        /// Calculates the decimal sum of a selected property for matching entities.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="sum">The decimal property expression to aggregate.</param>
        /// <param name="includes">Navigation property paths applied to the query.</param>
        /// <returns>The aggregate decimal value.</returns>
        public virtual decimal GetSum(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes)
        {
            return RepositoryBase.GetSum(where, sum, includes);
        }


        /// <summary>
        /// Asynchronously calculates the decimal sum of a selected property.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="sum">The decimal property expression to aggregate.</param>
        /// <param name="includes">Navigation property paths applied to the query.</param>
        /// <returns>A task that completes with the aggregate decimal value.</returns>
        public virtual async Task<decimal> GetSumAsync(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes)
        {
            return await RepositoryBase.GetSumAsync(where, sum, includes);
        }

        /// <summary>
        /// Asynchronously calculates a decimal sum with expression-based eager-loading.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation property type included by the query.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="sum">The decimal property expression to aggregate.</param>
        /// <param name="includesNavigationPropertyPathFunc">The include expression applied to the query.</param>
        /// <returns>A task that completes with the aggregate decimal value.</returns>
        public virtual async Task<decimal> GetSumAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await RepositoryBase.GetSumAsync(where, sum, includesNavigationPropertyPathFunc);
        }


        #endregion

        #region SaveObject & SaveChanges

        /// <summary>
        /// Saves one entity and raises the configured after-save event.
        /// Manual change detection is performed when enabled by the repository database settings.
        /// </summary>
        /// <param name="obj">The entity to insert or update.</param>
        public virtual void SaveObject(T obj)
        {
            if (RepositoryBase.BaseDB.ManualDetectChangeObject)
            {
                TryDetectChange(obj);
            }

            RepositoryBase.Save(obj);

            AfterSaveObject?.Invoke(this.BaseData, obj);
        }

        /// <summary>
        /// Asynchronously saves one entity and raises the configured after-save event.
        /// </summary>
        /// <param name="obj">The entity to insert or update.</param>
        /// <returns>A task that completes after persistence and event dispatch finish.</returns>
        public virtual async Task SaveObjectAsync(T obj)
        {
            if (RepositoryBase.BaseDB.ManualDetectChangeObject)
            {
                TryDetectChange(obj);
            }

            await RepositoryBase.SaveAsync(obj).ConfigureAwait(false);

            AfterSaveObject?.Invoke(this.BaseData, obj);
        }

        /// <summary>
        /// Asynchronously saves a sequence of entities and raises the after-save event for each item.
        /// </summary>
        /// <param name="objs">The entities to insert or update.</param>
        /// <returns>A task that completes after all entities are persisted.</returns>
        public virtual async Task SaveObjectListAsync(IEnumerable<T> objs)
        {
            await RepositoryBase.SaveObjectListAsync(objs);

            foreach (var item in objs)
            {
                AfterSaveObject?.Invoke(this.BaseData, item);
            }
        }

        /// <summary>
        /// Commits pending repository changes and raises the after-save-changes event.
        /// </summary>
        public virtual void SaveChanges()
        {
            RepositoryBase.SaveChanges();

            AfterSaveChanges?.Invoke(this.BaseData);
        }

        /// <summary>
        /// Asynchronously commits pending repository changes and raises the after-save-changes event.
        /// </summary>
        /// <returns>A task that completes after the database commit finishes.</returns>
        public virtual async Task SaveChangesAsync()
        {
            await RepositoryBase.SaveChangesAsync().ConfigureAwait(false);

            AfterSaveChanges?.Invoke(this.BaseData);
        }


        #endregion

        #region Delete


        /// <summary>
        /// Finds and deletes the first entity matching a predicate.
        /// </summary>
        /// <param name="predicate">The predicate used to locate the entity.</param>
        public virtual void DeleteObject(Expression<Func<T, bool>> predicate)
        {
            T obj = GetObject(predicate);

            DeleteObject(obj);

            AfterDeleteObject?.Invoke(this.BaseData, obj);
        }

        /// <summary>
        /// Asynchronously finds and deletes the first entity matching a predicate.
        /// </summary>
        /// <param name="predicate">The predicate used to locate the entity.</param>
        /// <returns>A task that completes after deletion and event dispatch.</returns>
        public virtual async Task DeleteObjectAsync(Expression<Func<T, bool>> predicate)
        {
            T obj = await GetObjectAsync(predicate);

            await DeleteObjectAsync(obj);

            AfterDeleteObject?.Invoke(this.BaseData, obj);
        }


        /// <summary>
        /// Deletes one entity and raises the configured after-delete event.
        /// </summary>
        /// <param name="obj">The entity to delete.</param>
        public virtual void DeleteObject(T obj)
        {
            RepositoryBase.Delete(obj);

            AfterDeleteObject?.Invoke(this.BaseData, obj);
        }

        /// <summary>
        /// Asynchronously deletes one entity and raises the configured after-delete event.
        /// </summary>
        /// <param name="obj">The entity to delete.</param>
        /// <returns>A task that completes after deletion and event dispatch.</returns>
        public virtual async Task DeleteObjectAsync(T obj)
        {
            await RepositoryBase.DeleteAsync(obj, true);

            AfterDeleteObject?.Invoke(this.BaseData, obj);
        }

        /// <summary>
        /// Deletes each supplied entity through <see cref="DeleteObject(T)"/>.
        /// </summary>
        /// <param name="objects">The entities to delete.</param>
        public virtual void DeleteAll(IEnumerable<T> objects)
        {
            var list = objects.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                DeleteObject(list[i]);
            }
        }

        /// <summary>
        /// Loads all matching entities and deletes them as a batch.
        /// </summary>
        /// <param name="where">The predicate selecting entities to delete.</param>
        /// <param name="deleteItemAction">An optional callback invoked by the repository for each entity.</param>
        /// <param name="softDelete">Whether the repository should perform a soft delete when supported.</param>
        /// <returns>A task that completes after deletion and after-delete events.</returns>
        public virtual async Task DeleteAllAsync(Expression<Func<T, bool>> where, Action<T> deleteItemAction = null, bool softDelete = false)
        {
            var list = await GetFullListAsync(where);
            await RepositoryBase.DeleteAllAsync(list, deleteItemAction, softDelete);

            foreach (var obj in list)
            {
                AfterDeleteObject?.Invoke(this.BaseData, obj);
            }
        }

        /// <summary>
        /// Deletes the supplied entity sequence as a batch.
        /// </summary>
        /// <param name="objects">The entities to delete.</param>
        /// <param name="deleteItemAction">An optional callback invoked by the repository for each entity.</param>
        /// <param name="softDelete">Whether the repository should perform a soft delete when supported.</param>
        /// <returns>A task that completes after deletion and after-delete events.</returns>
        public virtual async Task DeleteAllAsync(IEnumerable<T> objects, Action<T> deleteItemAction = null, bool softDelete = false)
        {
            await RepositoryBase.DeleteAllAsync(objects, deleteItemAction, softDelete);

            foreach (var obj in objects)
            {
                AfterDeleteObject?.Invoke(this.BaseData, obj);
            }
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Begins a repository transaction for subsequent service operations.
        /// </summary>
        /// <returns>A task that completes after the transaction has been opened.</returns>
        public async Task BeginTransactionAsync()
        {
            await RepositoryBase.BeginTransactionAsync();
        }

        /// <summary>
        /// Begins a transaction, executes a synchronous body, and commits or rolls back as a unit.
        /// </summary>
        /// <param name="action">The operation to execute inside the transaction.</param>
        /// <returns>A task that completes after commit or rollback.</returns>
        /// <exception cref="Exception">Rethrows the body exception after rolling back the transaction.</exception>
        public async Task BeginTransactionAsync(Action action)
        {
            await RepositoryBase.BeginTransactionAsync();
            try
            {
                action();
                CommitTransaction();
            }
            catch (Exception)
            {
                RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Begins a repository transaction for subsequent synchronous service operations.
        /// </summary>
        public void BeginTransaction()
        {
            RepositoryBase.BeginTransaction();
        }

        /// <summary>
        /// Begins a transaction, executes a synchronous body, and commits or rolls back as a unit.
        /// </summary>
        /// <param name="body">The operation to execute inside the transaction.</param>
        /// <param name="rollbackAction">An optional callback invoked with the original exception after rollback.</param>
        /// <exception cref="Exception">Rethrows the body exception after rollback and callback execution.</exception>
        public void BeginTransaction(Action body, Action<Exception> rollbackAction = null)
        {
            BeginTransaction();
            try
            {
                body();
                CommitTransaction();
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                rollbackAction?.Invoke(ex);
                throw;
            }
        }


        /// <summary>
        /// Begins a transaction, awaits an asynchronous body, and commits or rolls back as a unit.
        /// </summary>
        /// <param name="body">The asynchronous operation to execute inside the transaction.</param>
        /// <param name="rollbackAction">An optional callback invoked with the original exception after rollback.</param>
        /// <returns>A task that completes after commit or rollback.</returns>
        /// <exception cref="Exception">Rethrows the body exception after rollback and callback execution.</exception>
        public async Task BeginTransactionAsync(Func<Task> body, Action<Exception> rollbackAction = null)
        {
            await BeginTransactionAsync();
            try
            {
                await body();
                CommitTransaction();
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                rollbackAction?.Invoke(ex);
                throw ex;
            }
        }


        /// <summary>
        /// Begins a transaction, awaits an asynchronous body, and allows the rollback callback
        /// to replace the original exception before it is thrown.
        /// </summary>
        /// <param name="body">The asynchronous operation to execute inside the transaction.</param>
        /// <param name="rollbackAction">A callback that may translate the body exception.</param>
        /// <returns>A task that completes after commit or rollback.</returns>
        /// <exception cref="Exception">Throws the translated rollback exception or the original exception.</exception>
        public async Task BeginTransactionAsync(Func<Task> body, Func<Exception, Exception> rollbackAction)
        {
            await BeginTransactionAsync();
            try
            {
                await body();
                CommitTransaction();
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                throw rollbackAction?.Invoke(ex) ?? ex;
            }
        }


        /// <summary>
        /// Begins a transaction, awaits an asynchronous body, and allows an asynchronous rollback
        /// callback to replace the original exception before it is thrown.
        /// </summary>
        /// <param name="bodyAsync">The asynchronous operation to execute inside the transaction.</param>
        /// <param name="rollbackActionAsync">An asynchronous callback that may translate the body exception.</param>
        /// <returns>A task that completes after commit or rollback.</returns>
        /// <exception cref="Exception">Throws the translated rollback exception or the original exception.</exception>
        public async Task BeginTransactionAsync(Func<Task> bodyAsync, Func<Exception, Task<Exception>> rollbackActionAsync)
        {
            await BeginTransactionAsync();
            try
            {
                await bodyAsync();
                CommitTransaction();
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                throw await rollbackActionAsync?.Invoke(ex) ?? ex;
            }
        }


        /// <summary>
        /// Rolls back the current repository transaction.
        /// </summary>
        public void RollbackTransaction()
        {
            RepositoryBase.RollbackTransaction();
        }

        /// <summary>
        /// Commits the current repository transaction.
        /// </summary>
        public void CommitTransaction()
        {
            RepositoryBase.CommitTransaction();
        }

        #endregion

        #region Mapping

        /// <summary>
        /// Maps one entity to a data-transfer object using the configured AutoMapper instance.
        /// </summary>
        /// <typeparam name="TDto">The destination DTO type.</typeparam>
        /// <param name="entity">The entity to map.</param>
        /// <returns>The mapped DTO.</returns>
        public TDto Mapping<TDto>(T entity)
        {
            return Mapper.Map<TDto>(entity);
        }

        /// <summary>
        /// Maps every entity in a paged list while preserving its paging metadata.
        /// </summary>
        /// <typeparam name="TDto">The destination DTO type.</typeparam>
        /// <param name="pagedList">The source page and its entity items.</param>
        /// <returns>A DTO page with the original page index, counts, and skip count.</returns>
        public PagedList<TDto> Mapping<TDto>(PagedList<T> pagedList)
        {
            var dtoList = pagedList.Select(Mapper.Map<TDto>).ToList();
            return new PagedList<TDto>(dtoList, pagedList.PageIndex, pagedList.PageCount, pagedList.TotalCount, pagedList.SkipCount);
        }

        #endregion

        #region Tenant

        /// <summary>
        /// Applies tenant information to the underlying multi-tenant database context when supported.
        /// </summary>
        /// <param name="requestTenantInfo">The tenant information to assign to the current database context.</param>
        /// <returns><see langword="true"/> when the underlying context accepts tenant information; otherwise, <see langword="false"/>.</returns>
        public bool SetTenantInfo(RequestTenantInfo requestTenantInfo)
        {
            if (this.BaseData.BaseDB.BaseDataContext is ISenparcEntitiesDbContext senparcDB)
            {
                senparcDB.TenantInfo = requestTenantInfo;
                return true;
            }
            return false;
        }


        #endregion
    }
}
