/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RepositoryBase.cs
    File description: RepositoryBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261001
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.10-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview12 Updated repository infrastructure and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility.ExpressionExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Senparc.Ncf.Repository
{
    public class RepositoryBase<T> : DataBase, IRepositoryBase<T> where T : EntityBase //global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        protected string _entitySetName;

        //public RepositoryBase() :
        //    this(null)
        //{
        //}

        /// <summary>
        /// Initializes a repository for the entity type <typeparamref name="T"/> and resolves its mapped entity-set name.
        /// </summary>
        /// <param name="db">The database abstraction that owns the Entity Framework context.</param>
        public RepositoryBase(INcfDbData db) : base(db)
        {
            //System.Web.HttpContext.Current.Response.Write("-"+this.GetType().Name + "<br />");
            // The database abstraction is retained as the repository's active persistence boundary.

            base.BaseDB = db;
            // ObjectFactory.GetInstance<INcfDbData>();

            EntitySetKeysDictionary keys = EntitySetKeys.GetAllEntitySetInfo();
            _entitySetName = keys[typeof(T)].SetName;
        }

        //public BaseRepository() { }


        #region Legacy section


        /// <summary>
        /// Determines whether an entity is new according to its tracked Entity Framework state.
        /// </summary>
        /// <param name="obj">The entity to inspect.</param>
        /// <returns><see langword="true"/> for added or detached entities; otherwise, <see langword="false"/>.</returns>
        public virtual bool IsInsert(T obj)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry = BaseDB.BaseDataContext.Entry(obj);
            return entry.State == EntityState.Added || entry.State == EntityState.Detached;
            //return obj.EntityKey == null || obj.EntityKey.EntityKeyValues == null;

            //entry.IsKeySet
        }

        /// <summary>
        /// Creates an ordered query for all entities, including optional string-based navigation paths.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <param name="orderBy">The expression used to order the query.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>An executable query for the matching entity set.</returns>
        public virtual IQueryable<T> GeAll<TOrderProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return BaseDB.BaseDataContext.Set<T>()
                        //.SqlQuery(sql)
                        //.CreateQuery<T>(sql)
                        .Includes(includes)
                        .OrderBy(orderBy, orderingType).AsQueryable();
        }

        /// <summary>
        /// Creates an ordered query for all entities using a strongly typed navigation include expression.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="orderBy">The expression used to order the query.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed navigation include expression.</param>
        /// <returns>An executable query for the matching entity set.</returns>
        public virtual IQueryable<T> GeAll<TOrderProperty, TIncludesProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType,
            /*[JetBrains.Annotations.NotNull]*/Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //Expression<Func<TEntity, TIncludesProperty>> navigationPropertyPath

            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                          .OrderBy(orderBy, orderingType).AsQueryable();
        }


        /// <summary>
        /// Retrieves a filtered, ordered page of entities using string-based navigation includes.
        /// A non-positive page size returns the unbounded query result.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="pageIndex">The one-based page index used when paging is enabled.</param>
        /// <param name="pageCount">The number of items per page; a non-positive value disables paging.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A page containing the selected entities and paging metadata.</returns>
        public virtual PagedList<T> GetObjectList<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            int skipCount = Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
            int totalCount = -1;
            List<T> result = null;
            //var query = BaseDB.BaseDataContext.CreateQuery<T>(sql).Includes(includes).OrderBy(orderBy, orderingType);//.Includes(includes);
            IQueryable<T> resultList = BaseDB.BaseDataContext
                                        .Set<T>()
                                        //.CreateQuery<T>(sql)
                                        .Includes(includes)
                                        .Where(where)
                                        .OrderBy(orderBy, orderingType);//.Includes(includes);

            if (pageCount > 0 && pageIndex > 0)
            {
                resultList = resultList.Skip(skipCount).Take(pageCount);
                totalCount = this.ObjectCount(where, null); //whereList.Count();
            }
            //else
            //{
            //    resultList = query.;
            //}

            //try
            {
                result = resultList.ToList();
            }
            //Legacy comment translated to English.
            //{
            //Legacy comment translated to English.
            //    //result = query.Includes(includes)
            //Legacy comment translated to English.
            //    //            .Skip(skipCount).Take(pageCount)
            //Legacy comment translated to English.
            //    //            .ToList();
            //    AdminLogUtility.WebLogger.Warn("EF ArgumentException", ex);
            //    throw;
            //}
            //catch (NotSupportedException ex)//System.Reflection.TargetException
            //{
            //    result = resultList.Where(where.Compile()).ToList();
            //    AdminLogUtility.WebLogger.Warn("EF NotSupportedException", ex);
            //    throw;
            //}
            //catch (Exception ex)
            //{
            //    result = resultList.Where(where.Compile()).ToList();
            //    AdminLogUtility.WebLogger.Warn("EF Exception", ex);
            //    throw;
            //}

            PagedList<T> list = new PagedList<T>(result, pageIndex, pageCount, totalCount, skipCount);
            return list;
        }
        /// <summary>
        /// Retrieves a filtered, ordered page using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="pageIndex">The one-based page index used when paging is enabled.</param>
        /// <param name="pageCount">The number of items per page; a non-positive value disables paging.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>A page containing the selected entities and paging metadata.</returns>
        public virtual PagedList<T> GetObjectList<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            int skipCount = Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
            int totalCount = -1;
            List<T> result = null;
            //var query = BaseDB.BaseDataContext.CreateQuery<T>(sql).Includes(includes).OrderBy(orderBy, orderingType);//.Includes(includes);
            IQueryable<T> resultList = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .OrderBy(orderBy, orderingType)
                                        .Where(where)
                                        .OrderBy(orderBy, orderingType);//.Includes(includes);

            if (pageCount > 0 && pageIndex > 0)
            {
                resultList = resultList.Skip(skipCount).Take(pageCount);
                totalCount = this.ObjectCount(where, null); //whereList.Count();
            }
            //else
            //{
            //    resultList = query.;
            //}

            //try
            {
                result = resultList.ToList();
            }
            //Legacy comment translated to English.
            //{
            //Legacy comment translated to English.
            //    //result = query.Includes(includes)
            //Legacy comment translated to English.
            //    //            .Skip(skipCount).Take(pageCount)
            //Legacy comment translated to English.
            //    //            .ToList();
            //    AdminLogUtility.WebLogger.Warn("EF ArgumentException", ex);
            //    throw;
            //}
            //catch (NotSupportedException ex)//System.Reflection.TargetException
            //{
            //    result = resultList.Where(where.Compile()).ToList();
            //    AdminLogUtility.WebLogger.Warn("EF NotSupportedException", ex);
            //    throw;
            //}
            //catch (Exception ex)
            //{
            //    result = resultList.Where(where.Compile()).ToList();
            //    AdminLogUtility.WebLogger.Warn("EF Exception", ex);
            //    throw;
            //}

            PagedList<T> list = new PagedList<T>(result, pageIndex, pageCount, totalCount, skipCount);
            return list;
        }

        /// <summary>
        /// Asynchronously retrieves a filtered, ordered page using string-based navigation includes.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="pageIndex">The one-based page index used when paging is enabled.</param>
        /// <param name="pageCount">The number of items per page; a non-positive value disables paging.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with the selected page and metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, params string[] includes)
        {
            int skipCount = Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
            int totalCount = -1;
            List<T> result = null;
            IQueryable<T> resultList = BaseDB.BaseDataContext
                                               .Set<T>()
                                               .Includes(includes)
                                               .Where(where)
                                               .OrderBy(orderBy, orderingType);//.Includes(includes);
            if (pageCount > 0 && pageIndex > 0)
            {
                resultList = resultList.Skip(skipCount).Take(pageCount);
                totalCount = this.ObjectCount(where, null);
            }

            result = await resultList.ToListAsync();

            PagedList<T> list = new PagedList<T>(result, pageIndex, pageCount, totalCount, skipCount);
            return list;
        }

        /// <summary>
        /// Asynchronously retrieves a filtered, ordered page using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="pageIndex">The one-based page index used when paging is enabled.</param>
        /// <param name="pageCount">The number of items per page; a non-positive value disables paging.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>A task that completes with the selected page and metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            int skipCount = Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
            int totalCount = -1;
            List<T> result = null;
            IQueryable<T> resultList = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                               .OrderBy(orderBy, orderingType)
                                               .Where(where)
                                               .OrderBy(orderBy, orderingType);//.Includes(includes);
            if (pageCount > 0 && pageIndex > 0)
            {
                resultList = resultList.Skip(skipCount).Take(pageCount);
                totalCount = this.ObjectCount(where, null);
            }

            result = await resultList.ToListAsync();

            PagedList<T> list = new PagedList<T>(result, pageIndex, pageCount, totalCount, skipCount);
            return list;
        }


        /// <summary>
        /// Returns the first entity matching a predicate with optional string-based includes.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>The first matching entity, or the default value when none exists.</returns>
        public virtual T GetFirstOrDefaultObject(Expression<Func<T, bool>> where, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).FirstOrDefault(where);
        }

        /// <summary>
        /// Returns the first matching entity using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>The first matching entity, or the default value when none exists.</returns>
        public virtual T GetFirstOrDefaultObject<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                       .FirstOrDefault(where);
        }

        /// <summary>
        /// Returns the first matching entity after applying expression-based ordering.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>The first ordered match, or the default value when none exists.</returns>
        public virtual T GetFirstOrDefaultObject<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).Where(where).OrderBy(orderBy, orderingType).FirstOrDefault();
        }

        /// <summary>
        /// Returns the first ordered match using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>The first ordered match, or the default value when none exists.</returns>
        public virtual T GetFirstOrDefaultObject<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            Console.WriteLine("RepositoryBase GetFirstOrDefaultObject");
            Console.WriteLine("Data:" + BaseDB.BaseDataContext.Set<T>().ToList().ToJson(true, new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));

            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .OrderBy(orderBy, orderingType)
                                        .Where(where).OrderBy(orderBy, orderingType).FirstOrDefault();
        }

        /// <summary>
        /// Finds an entity by an integer primary-key value.
        /// </summary>
        /// <param name="id">The integer primary-key value.</param>
        /// <returns>The matching entity, or the default value when it is not tracked or stored.</returns>
        public virtual T GetObjectById(int id)
        {
            T obj = BaseDB.BaseDataContext
                .Set<T>()
                .Find(id);

            return obj;
        }

        /// <summary>
        /// Finds an entity by a 64-bit integer primary-key value.
        /// </summary>
        /// <param name="id">The 64-bit integer primary-key value.</param>
        /// <returns>The matching entity, or the default value when it is not tracked or stored.</returns>
        public virtual T GetObjectById(long id)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            //T obj = BaseDB.BaseDataContext
            //     .Set<T>()
            //    //.CreateQuery<T>(sql)
            //     .Includes(includes).Where("it.Id = @id", new ObjectParameter("id", id)).FirstOrDefault();
            T obj = BaseDB.BaseDataContext.Set<T>().Find(id);
            return obj;
        }

        //public virtual T GetObjectByGuid(Guid guid,params string[] includes)
        //{
        //    string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
        //    T obj = BaseDB.BaseDataContext
        //         .Set<T>()
        //        //.CreateQuery<T>(sql)
        //         .Includes(includes).Where("it.Guid = @guid", new ObjectParameter("guid", guid)).FirstOrDefault();
        //    return obj;
        //}

        /// <summary>
        /// Counts entities matching a predicate.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includes">Navigation property paths applied before counting.</param>
        /// <returns>The number of matching entities.</returns>
        public virtual int ObjectCount(Expression<Func<T, bool>> where, params string[] includes)
        {
            string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            int count = 0;
            IQueryable<T> query = BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes);
            //try
            {
                count = query.Count(where);
            }
            //catch (NotSupportedException ex)
            //{
            //    count = query.Count(where.Compile());
            //}
            //catch (Exception ex)
            //{
            //    count = query.Count(where.Compile());
            //    throw;
            //}
            return count;
        }

        /// <summary>
        /// Counts matching entities after applying a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>The number of matching entities.</returns>
        public virtual int ObjectCount<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            int count = 0;
            IQueryable<T> query = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>());
            //try
            {
                count = query.Count(where);
            }
            //catch (NotSupportedException ex)
            //{
            //    count = query.Count(where.Compile());
            //}
            //catch (Exception ex)
            //{
            //    count = query.Count(where.Compile());
            //    throw;
            //}
            return count;
        }

        /// <summary>
        /// Calculates a decimal aggregate over matching entities.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="sum">The decimal property expression to aggregate.</param>
        /// <param name="includes">Navigation property paths applied to the query.</param>
        /// <returns>The aggregate decimal value.</returns>
        public virtual decimal GetSum(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            decimal result = BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).Where(where).Sum(sum);
            return result;
        }

        /// <summary>
        /// Calculates a decimal aggregate using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="sum">The decimal property expression to aggregate.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>The aggregate decimal value.</returns>
        public virtual decimal GetSum<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            decimal result = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .Where(where).Sum(sum);
            return result;
        }
        //public virtual object ObjectSum(Expression<Func<T, bool>> where, Func<T,object> sumBy, string[] includes)
        //{
        //    string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
        //    object result= _db.DataContext.CreateQuery<T>(sql).Includes(includes).Where(where).Sum(sumBy);
        //    return result;
        //}


        /// <summary>
        /// Adds an entity to the current data context and immediately commits the change.
        /// </summary>
        /// <param name="obj">The entity to add.</param>
        public virtual void Add(T obj)
        {
            BaseDB.BaseDataContext.Set<T>().Add(obj);
            this.SaveChanges();
        }

        /// <summary>
        /// Commits changes for an existing entity already tracked by the current data context.
        /// </summary>
        /// <param name="obj">The entity whose pending changes should be committed.</param>
        public virtual void Update(T obj)
        {
            //_db.DataContext.ApplyPropertyChanges(_entitySetName, obj);
            this.SaveChanges();
        }

        /// <summary>
        /// Inserts or updates an entity based on <see cref="IsInsert(T)"/> and updates its timestamps.
        /// </summary>
        /// <param name="obj">The entity to persist.</param>
        public virtual void Save(T obj)
        {
            if (this.IsInsert(obj))
            {
                obj.AddTime = obj.LastUpdateTime = SystemTime.Now.LocalDateTime;
                this.Add(obj);
            }
            else
            {
                obj.LastUpdateTime = SystemTime.Now.LocalDateTime;
                this.Update(obj);
            }
        }

        /// <summary>
        /// Commits pending changes in the underlying Entity Framework context.
        /// </summary>
        public virtual void SaveChanges()
        {
            BaseDB.BaseDataContext.SaveChanges();//TODO: SaveOptions.
        }

        /// <summary>
        /// Deletes an entity permanently or marks its soft-delete flag according to <paramref name="softDelete"/>.
        /// </summary>
        /// <param name="obj">The entity to delete.</param>
        /// <param name="softDelete">When <see langword="true"/>, sets the entity flag instead of removing the row.</param>
        public virtual void Delete(T obj, bool softDelete = false)
        {
            if (softDelete)
            {
                obj.Flag = true;
            }
            else
            {
                BaseDB.BaseDataContext.Set<T>().Remove(obj);
            }
            this.SaveChanges();
        }

        //public virtual void DeleteAll(IEnumerable<T> objs)
        //{
        //    //foreach (var obj in objs)
        //    //{
        //    //    _db.DataContext.DeleteObject(obj);
        //    //}
        //    //this.SaveChanges();
        //    var list = objs.ToList();
        //    for (int i = 0; i < list.Count; i++)
        //    {
        //        this.Delete(list[i]);
        //    }
        //}

        #endregion

        /// <summary>
        /// Asynchronously commits pending changes in the underlying Entity Framework context.
        /// </summary>
        /// <returns>A task that completes after the database commit.</returns>
        public virtual async Task SaveChangesAsync()
        {
            await BaseDB.BaseDataContext.SaveChangesAsync().ConfigureAwait(false);//TODO: SaveOptions.
        }

        /// <summary>
        /// Asynchronously inserts or updates an entity based on <see cref="IsInsert(T)"/>.
        /// </summary>
        /// <param name="obj">The entity to persist.</param>
        /// <returns>A task that completes after the database commit.</returns>
        public virtual async Task SaveAsync(T obj)
        {
            if (this.IsInsert(obj))
            {
                obj.AddTime = obj.LastUpdateTime = SystemTime.Now.LocalDateTime;
                await this.AddAsync(obj).ConfigureAwait(false);
            }
            else
            {
                obj.LastUpdateTime = SystemTime.Now.LocalDateTime;
                await this.UpdateAsync(obj).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously returns the first entity matching a predicate with optional includes.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with the first match or the default value.</returns>
        public async Task<T> GetFirstOrDefaultObjectAsync(Expression<Func<T, bool>> where, params string[] includes)
        {
            return await BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).FirstOrDefaultAsync(where);
        }

        /// <summary>
        /// Asynchronously returns the first match using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>A task that completes with the first match or the default value.</returns>
        public async Task<T> GetFirstOrDefaultObjectAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .FirstOrDefaultAsync(where);
        }

        /// <summary>
        /// Asynchronously returns the first ordered match with optional string-based includes.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with the first ordered match or the default value.</returns>
        public virtual async Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return await BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).Where(where).OrderBy(orderBy, orderingType).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Asynchronously returns the first ordered match using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TOrderProperty">The ordering key type.</typeparam>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="orderBy">The expression used to order entities.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>A task that completes with the first ordered match or the default value.</returns>
        public virtual async Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return await includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .OrderBy(orderBy, orderingType)
                                        .Where(where).OrderBy(orderBy, orderingType).FirstOrDefaultAsync();
        }

        //public virtual async Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<T, TIncludesProperty>> includesNavigationPropertyPathFunc)
        //{
        //    return await BaseDB.BaseDataContext
        //     .Set<T>()
        //     .Include(includesNavigationPropertyPathFunc).Where(where).OrderBy(orderBy, orderingType).FirstOrDefaultAsync();
        //}



        /// <summary>
        /// Asynchronously counts entities matching a predicate with optional includes.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includes">Navigation property paths applied before counting.</param>
        /// <returns>A task that completes with the number of matching entities.</returns>
        public virtual async Task<int> ObjectCountAsync(Expression<Func<T, bool>> where, params string[] includes)
        {
            string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            int count = 0;
            IQueryable<T> query = BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes);
            //try
            {
                count = await query.CountAsync(where).ConfigureAwait(false);
            }
            //catch (NotSupportedException ex)
            //{
            //    count = query.Count(where.Compile());
            //}
            //catch (Exception ex)
            //{
            //    count = query.Count(where.Compile());
            //    throw;
            //}
            return count;
        }

        /// <summary>
        /// Asynchronously counts matching entities using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>A task that completes with the number of matching entities.</returns>
        public virtual async Task<int> ObjectCountAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            int count = 0;
            IQueryable<T> query = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>());
            //try
            {
                count = await query.CountAsync(where).ConfigureAwait(false);
            }
            //catch (NotSupportedException ex)
            //{
            //    count = query.Count(where.Compile());
            //}
            //catch (Exception ex)
            //{
            //    count = query.Count(where.Compile());
            //    throw;
            //}
            return count;
        }

        /// <summary>
        /// Asynchronously calculates a decimal aggregate with optional includes.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="sum">The decimal property expression to aggregate.</param>
        /// <param name="includes">Navigation property paths applied to the query.</param>
        /// <returns>A task that completes with the aggregate decimal value.</returns>
        public virtual async Task<decimal> GetSumAsync(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            var query = BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes);

            decimal result = await query.Where(where).SumAsync(sum);
            return result;
        }

        /// <summary>
        /// Asynchronously calculates a decimal aggregate using a strongly typed include expression.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="sum">The decimal property expression to aggregate.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>A task that completes with the aggregate decimal value.</returns>
        public virtual async Task<decimal> GetSumAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            var query = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>());

            decimal result = await query.Where(where).SumAsync(sum);
            return result;
        }


        /// <summary>
        /// Asynchronously adds an entity and commits the change.
        /// </summary>
        /// <param name="obj">The entity to add.</param>
        /// <returns>A task that completes after the database commit.</returns>
        public virtual async Task AddAsync(T obj)
        {
            BaseDB.BaseDataContext.Set<T>().Add(obj);
            await this.SaveChangesAsync();
        }

        /// <summary>
        /// Asynchronously commits changes for an existing tracked entity.
        /// </summary>
        /// <param name="obj">The entity whose pending changes should be committed.</param>
        /// <returns>A task that completes after the database commit.</returns>
        public virtual async Task UpdateAsync(T obj)
        {
            //_db.DataContext.ApplyPropertyChanges(_entitySetName, obj);
            await this.SaveChangesAsync();
        }
        /// <summary>
        /// Asynchronously deletes an entity permanently or marks its soft-delete flag.
        /// </summary>
        /// <param name="obj">The entity to delete.</param>
        /// <param name="softDelete">When <see langword="true"/>, sets the entity flag instead of removing the row.</param>
        /// <returns>A task that completes after the deletion is committed.</returns>
        public virtual async Task DeleteAsync(T obj, bool softDelete = false)
        {
            if (softDelete)
            {
                obj.Flag = true;
            }
            else
            {
                BaseDB.BaseDataContext.Set<T>().Remove(obj);
            }
            await this.SaveChangesAsync();
        }
        /// <summary>
        /// Asynchronously deletes a sequence of entities and commits the batch once.
        /// </summary>
        /// <param name="objs">The entities to delete.</param>
        /// <param name="deleteItemAction">An optional callback invoked after each entity is prepared for deletion.</param>
        /// <param name="softDelete">When <see langword="true"/>, marks each entity instead of removing rows.</param>
        /// <returns>A task that completes after the batch is committed.</returns>
        public virtual async Task DeleteAllAsync(IEnumerable<T> objs, Action<T> deleteItemAction = null, bool softDelete = false)
        {
            foreach (var obj in objs)
            {
                if (softDelete)
                {
                    obj.Flag = true;
                }
                else
                {
                    BaseDB.BaseDataContext.Set<T>().Remove(obj);
                }

                deleteItemAction?.Invoke(obj);
            }
            await this.SaveChangesAsync();
        }
        /// <summary>
        /// Adds a sequence of entities and commits the batch once.
        /// </summary>
        /// <param name="objs">The entities to add.</param>
        /// <returns>A task that completes after the batch is committed.</returns>
        public virtual async Task AddAllAsync(IEnumerable<T> objs)
        {
            //foreach (var obj in objs)
            //{
            //Legacy comment translated to English.
            //}
            BaseDB.BaseDataContext.Set<T>().AddRange(objs);
            await this.SaveChangesAsync();
        }


        /// <summary>
        /// Asynchronously retrieves a page using a textual order expression and string includes.
        /// </summary>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="OrderbyField">The textual order expression, such as <c>Id DESC, Name ASC</c>.</param>
        /// <param name="pageIndex">The one-based page index used when paging is enabled.</param>
        /// <param name="pageCount">The number of items per page; a non-positive value disables paging.</param>
        /// <param name="includes">Navigation property paths to eager-load.</param>
        /// <returns>A task that completes with the selected page and metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, params string[] includes)
        {
            int skipCount = Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
            int totalCount = -1;
            List<T> result = null;
            IQueryable<T> resultList = BaseDB.BaseDataContext
                                               .Set<T>()
                                               .Includes(includes)
                                               .Where(where);
            resultList = resultList.OrderByExtension(OrderbyField);
            if (pageCount > 0 && pageIndex > 0)
            {
                resultList = resultList.Skip(skipCount).Take(pageCount);
                totalCount = this.ObjectCount(where, null);
            }

            result = await resultList.ToListAsync();

            PagedList<T> list = new PagedList<T>(result, pageIndex, pageCount, totalCount, skipCount);
            return list;
        }

        /// <summary>
        /// Asynchronously retrieves a page using a textual order expression and typed includes.
        /// </summary>
        /// <typeparam name="TIncludesProperty">The navigation shape returned by the include expression.</typeparam>
        /// <param name="where">The predicate used to select entities.</param>
        /// <param name="OrderbyField">The textual order expression.</param>
        /// <param name="pageIndex">The one-based page index used when paging is enabled.</param>
        /// <param name="pageCount">The number of items per page; a non-positive value disables paging.</param>
        /// <param name="includesNavigationPropertyPathFunc">The strongly typed include expression.</param>
        /// <returns>A task that completes with the selected page and metadata.</returns>
        public virtual async Task<PagedList<T>> GetObjectListAsync<TIncludesProperty>(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            int skipCount = Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
            int totalCount = -1;
            List<T> result = null;
            IQueryable<T> resultList = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .Where(where);
            resultList = resultList.OrderByExtension(OrderbyField);
            if (pageCount > 0 && pageIndex > 0)
            {
                resultList = resultList.Skip(skipCount).Take(pageCount);
                totalCount = this.ObjectCount(where, null);
            }

            result = await resultList.ToListAsync();

            PagedList<T> list = new PagedList<T>(result, pageIndex, pageCount, totalCount, skipCount);
            return list;
        }

        /// <summary>
        /// Saves a mixed sequence of new and existing entities, marking existing entries as modified.
        /// </summary>
        /// <param name="objs">The entities to insert or update.</param>
        /// <returns>A task that completes after the batch is committed.</returns>
        public async Task SaveObjectListAsync(IEnumerable<T> objs)
        {
            if (!objs.Any())
            {
                return;
            }
            ICollection<T> addList = new List<T>();
            foreach (var item in objs)
            {
                if (this.IsInsert(item))
                {
                    addList.Add(item);
                }
                else
                {
                    BaseDB.BaseDataContext.Entry<T>(item).State = EntityState.Modified;
                }
            }

            BaseDB.BaseDataContext.Set<T>().AddRange(addList);
            await this.SaveChangesAsync();
        }

        /// <summary>
        /// Persists only selected mapped properties of a tracked, already persisted entity.
        /// Primary-key properties and unmapped names are rejected to avoid changing identity metadata.
        /// </summary>
        /// <param name="obj">The tracked entity whose selected properties should be persisted.</param>
        /// <param name="propertyNames">The mapped, non-key property names to update.</param>
        /// <returns>A task that completes after the selected properties are committed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when no property names are supplied or a name is not an updatable mapped property.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the entity is detached, new, or marked for deletion.</exception>
        public virtual async Task SavePropertiesAsync(T obj, params string[] propertyNames)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (propertyNames == null || propertyNames.Length == 0)
            {
                throw new ArgumentException("At least one property is required.", nameof(propertyNames));
            }

            var context = BaseDB.BaseDataContext;
            var entry = context.Entry(obj);
            if (entry.State == EntityState.Detached || entry.State == EntityState.Added || entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException("Only tracked, persisted entities support partial updates.");
            }

            var properties = new HashSet<string>(propertyNames, StringComparer.Ordinal);
            foreach (var name in properties)
            {
                var property = entry.Metadata.FindProperty(name);
                if (property == null || property.IsPrimaryKey())
                {
                    throw new ArgumentException($"Property '{name}' is not an updatable mapped property.", nameof(propertyNames));
                }
            }

            context.ChangeTracker.DetectChanges();
            foreach (var property in entry.Properties)
            {
                property.IsModified = properties.Contains(property.Metadata.Name);
            }

            await SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Begins a database transaction using the provider's default isolation level.
        /// </summary>
        public void BeginTransaction()
        {
            BaseDB.BaseDataContext.Database.BeginTransaction();
        }

        /// <summary>
        /// Begins a database transaction; the supplied isolation level is accepted for API compatibility.
        /// </summary>
        /// <param name="isolationLevel">The requested transaction isolation level.</param>
        public void BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            BaseDB.BaseDataContext.Database.BeginTransaction();
        }

        /// <summary>
        /// Asynchronously begins a database transaction using the provider's default isolation level.
        /// </summary>
        /// <returns>A task that completes after the transaction has begun.</returns>
        public async Task BeginTransactionAsync()
        {
            await BaseDB.BaseDataContext.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Asynchronously begins a transaction at the requested isolation level.
        /// </summary>
        /// <param name="isolationLevel">The requested transaction isolation level.</param>
        /// <returns>A task that completes after the transaction has begun.</returns>
        public async Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel)
        {
            await BaseDB.BaseDataContext.Database.BeginTransactionAsync(isolationLevel);
        }

        /// <summary>
        /// Rolls back the active database transaction.
        /// </summary>
        public void RollbackTransaction()
        {
            BaseDB.BaseDataContext.Database.RollbackTransaction();
        }

        /// <summary>
        /// Commits the active database transaction.
        /// </summary>
        public void CommitTransaction()
        {
            BaseDB.BaseDataContext.Database.CommitTransaction();
        }
    }
}
