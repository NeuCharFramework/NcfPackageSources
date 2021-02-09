using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.CO2NET;
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

        public RepositoryBase(INcfDbData db) : base(db)
        {
            //System.Web.HttpContext.Current.Response.Write("-"+this.GetType().Name + "<br />");
            //DB = db ?? ObjectFactory.GetInstance<INcfDbData>();//如果没有定义，取默认数据库

            base.BaseDB = db;
            // ObjectFactory.GetInstance<INcfDbData>();

            EntitySetKeysDictionary keys = EntitySetKeys.GetAllEntitySetInfo();
            _entitySetName = keys[typeof(T)].SetName;
        }

        //public BaseRepository() { }


        #region IBaseRepository<T> 成员


        public virtual bool IsInsert(T obj)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T> entry = BaseDB.BaseDataContext.Entry(obj);
            return entry.State == EntityState.Added || entry.State == EntityState.Detached; //TODO:EF5、Core 验证正确性
            //return obj.EntityKey == null || obj.EntityKey.EntityKeyValues == null;

            //entry.IsKeySet
        }

        public virtual IQueryable<T> GeAll<TOrderProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return BaseDB.BaseDataContext.Set<T>()
                        //.SqlQuery(sql)
                        //.CreateQuery<T>(sql)
                        .Includes(includes)
                        .OrderBy(orderBy, orderingType).AsQueryable();
        }

        public virtual IQueryable<T> GeAll<TOrderProperty, TIncludesProperty>(Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType,
            /*[JetBrains.Annotations.NotNull]*/Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //Expression<Func<TEntity, TIncludesProperty>> navigationPropertyPath

            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                          .OrderBy(orderBy, orderingType).AsQueryable();
        }


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
            //catch (ArgumentException ex)//DbArithmeticExpression 参数必须具有数值通用类型。
            //{
            //    //通常是ordery by的问题 TODO:重新整理是否需要Skip等操作
            //    //result = query.Includes(includes)
            //    //            .OrderByIEnumerable(orderBy.Compile(), orderingType)//改用非延时地方法，效率最低
            //    //            .Skip(skipCount).Take(pageCount)
            //    //            .Where(where.Compile())//保险起见用where.Compile()，但是会影响效率
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
            //catch (ArgumentException ex)//DbArithmeticExpression 参数必须具有数值通用类型。
            //{
            //    //通常是ordery by的问题 TODO:重新整理是否需要Skip等操作
            //    //result = query.Includes(includes)
            //    //            .OrderByIEnumerable(orderBy.Compile(), orderingType)//改用非延时地方法，效率最低
            //    //            .Skip(skipCount).Take(pageCount)
            //    //            .Where(where.Compile())//保险起见用where.Compile()，但是会影响效率
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


        public virtual T GetFirstOrDefaultObject(Expression<Func<T, bool>> where, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).FirstOrDefault(where);
        }

        public virtual T GetFirstOrDefaultObject<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                       .FirstOrDefault(where);
        }

        public virtual T GetFirstOrDefaultObject<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).Where(where).OrderBy(orderBy, orderingType).FirstOrDefault();
        }

        public virtual T GetFirstOrDefaultObject<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .OrderBy(orderBy, orderingType)
                                        .Where(where).OrderBy(orderBy, orderingType).FirstOrDefault();
        }

        public virtual T GetObjectById(int id)
        {
            T obj = BaseDB.BaseDataContext
                .Set<T>()
                .Find(id);

            return obj;
        }

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

        public virtual decimal GetSum(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            decimal result = BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).Where(where).Sum(sum);
            return result;
        }

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


        public virtual void Add(T obj)
        {
            BaseDB.BaseDataContext.Set<T>().Add(obj);
            this.SaveChanges();
        }

        public virtual void Update(T obj)
        {
            //_db.DataContext.ApplyPropertyChanges(_entitySetName, obj);
            this.SaveChanges();
        }

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

        public virtual void SaveChanges()
        {
            BaseDB.BaseDataContext.SaveChanges();//TODO: SaveOptions.
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="softDelete">是否使用软删除</param>
        public virtual void Delete(T obj, bool softDelete = false)
        {
            if (softDelete)
            {
                obj.Flag = true;//软删除
            }
            else
            {
                BaseDB.BaseDataContext.Set<T>().Remove(obj);//硬删除
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

        public virtual async Task SaveChangesAsync()
        {
            await BaseDB.BaseDataContext.SaveChangesAsync().ConfigureAwait(false);//TODO: SaveOptions.
        }

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
        /// 
        /// </summary>
        /// <param name="where"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        public async Task<T> GetFirstOrDefaultObjectAsync(Expression<Func<T, bool>> where, params string[] includes)
        {
            return await BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).FirstOrDefaultAsync(where);
        }

        public async Task<T> GetFirstOrDefaultObjectAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            return await includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .FirstOrDefaultAsync(where);
        }

        public virtual async Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, params string[] includes)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return await BaseDB.BaseDataContext
                 .Set<T>()
                 //.CreateQuery<T>(sql)
                 .Includes(includes).Where(where).OrderBy(orderBy, orderingType).FirstOrDefaultAsync();
        }

        public virtual async Task<T> GetFirstOrDefaultObjectAsync<TOrderProperty, TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, TOrderProperty>> orderBy, OrderingType orderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            return await includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>())
                                        .OrderBy(orderBy, orderingType)
                                        .Where(where).OrderBy(orderBy, orderingType).FirstOrDefaultAsync();
        }


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

        public virtual async Task<decimal> GetSumAsync<TIncludesProperty>(Expression<Func<T, bool>> where, Expression<Func<T, decimal>> sum, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>> includesNavigationPropertyPathFunc)
        {
            //string sql = string.Format("SELECT VALUE c FROM {0} AS c ", _entitySetName);
            var query = includesNavigationPropertyPathFunc.Compile()(BaseDB.BaseDataContext.Set<T>());

            decimal result = await query.Where(where).SumAsync(sum);
            return result;
        }


        public virtual async Task AddAsync(T obj)
        {
            BaseDB.BaseDataContext.Set<T>().Add(obj);
            await this.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T obj)
        {
            //_db.DataContext.ApplyPropertyChanges(_entitySetName, obj);
            await this.SaveChangesAsync();
        }


        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="softDelete">是否使用软删除</param>
        public virtual async Task DeleteAsync(T obj, bool softDelete = false)
        {
            if (softDelete)
            {
                obj.Flag = true;//软删除
            }
            else
            {
                BaseDB.BaseDataContext.Set<T>().Remove(obj);//硬删除
            }
            await this.SaveChangesAsync();
        }


        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="softDelete"></param>
        /// <returns></returns>
        public virtual async Task DeleteAllAsync(IEnumerable<T> objs, bool softDelete = false)
        {
            foreach (var obj in objs)
            {
                if (softDelete)
                {
                    obj.Flag = true;//软删除
                }
                else
                {
                    BaseDB.BaseDataContext.Set<T>().Remove(obj);//硬删除
                }
            }
            await this.SaveChangesAsync();
        }


        /// <summary>
        /// 批量添加, 待优化
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="softDelete"></param>
        /// <returns></returns>
        public virtual async Task AddAllAsync(IEnumerable<T> objs)
        {
            //foreach (var obj in objs)
            //{
            //    //BaseDB.BaseDataContext.Set<T>().(obj);//硬删除
            //}
            BaseDB.BaseDataContext.Set<T>().AddRange(objs);
            await this.SaveChangesAsync();
        }


        /// <summary>
        /// 动态排序
        /// </summary>
        /// <param name="where"></param>
        /// <param name="OrderbyField">xxx DESC, bbb ASC</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageCount"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
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
        /// 开启事物
        /// </summary>
        /// <returns></returns>
        public void BeginTransaction()
        {
            BaseDB.BaseDataContext.Database.BeginTransaction();
        }

        /// <summary>
        /// 开启事物
        /// </summary>
        /// <returns></returns>
        public void BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            BaseDB.BaseDataContext.Database.BeginTransaction();
        }

        /// <summary>
        /// 开启事物
        /// </summary>
        /// <returns></returns>
        public async Task BeginTransactionAsync()
        {
            await BaseDB.BaseDataContext.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// 开启事物
        /// </summary>
        /// <returns></returns>
        public async Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel)
        {
            await BaseDB.BaseDataContext.Database.BeginTransactionAsync(isolationLevel);
        }

        /// <summary>
        /// 回滚事物
        /// </summary>
        /// <returns></returns>
        public void RollbackTransaction()
        {
            BaseDB.BaseDataContext.Database.RollbackTransaction();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommitTransaction()
        {
            BaseDB.BaseDataContext.Database.CommitTransaction();
        }
    }
}
