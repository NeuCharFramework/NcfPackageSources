using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Xncf.SystemCore.Domain.Database;
using Senparc.Ncf.UnitTestExtension.Database;

namespace Senparc.Ncf.UnitTestExtension
{
    [TestClass]
    public class BaseNcfUnitTest
    {
        protected IServiceProvider _serviceProvider;
        protected Dictionary<Type, List<object>> dataLists = new Dictionary<Type, List<object>>();

        public BaseNcfUnitTest() : this(null, null) { }

        /// <summary>  
        /// 构造函数，用于初始化服务提供者和种子数据  
        /// </summary>  
        /// <param name="servicesRegister">在启动时注册 ServiceCollection 的委托</param>  
        /// <param name="initSeedData">初始化种子数据的委托</param>  
        public BaseNcfUnitTest(Action<IServiceCollection> servicesRegister = null, Action<Dictionary<Type, List<object>>> initSeedData = null)
        {
            initSeedData?.Invoke(dataLists);
            IServiceCollection services = new ServiceCollection();
            servicesRegister?.Invoke(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>  
        /// 尝试初始化指定类型的种子数据  
        /// </summary>  
        /// <typeparam name="T">实体类型</typeparam>  
        public void TryInitSeedData<T>() where T : EntityBase
        {
            if (!dataLists.ContainsKey(typeof(T)))
            {
                dataLists[typeof(T)] = new List<object>();
            }
        }

        /// <summary>  
        /// 获取指定类型的仓储实例，带有预设的 Mock 行为  
        /// </summary>  
        /// <typeparam name="T">实体类型</typeparam>  
        /// <returns>仓储实例</returns>  
        public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T, TKey>() where T : EntityBase<TKey>
        {
            var repo = GetRespository<T>();

            var dataList = repo.DataList;

            // Mock Update 方法  
            repo.MockRepository.Setup(z => z.Update(It.IsAny<T>()))
                .Callback<T>(obj =>
                {
                    var existing = dataList.FirstOrDefault(o => o == obj);
                    if (existing != null)
                    {
                        var index = dataList.IndexOf(existing);
                        dataList[index] = obj;
                    }
                });

            // Mock SaveAsync 方法  
            repo.MockRepository.Setup(z => z.SaveAsync(It.IsAny<T>()))
                 .Returns<T>(obj =>
                 {
                     var existing = dataList.FirstOrDefault(o => o == obj);
                     if (existing != null)
                     {
                         var index = dataList.IndexOf(existing);
                         dataList[index] = obj;
                     }
                     else
                     {
                         dataList.Add(obj);
                     }
                     return Task.CompletedTask;
                 });

            return repo;
        }

        /// <summary>  
        /// 获取指定类型的仓储实例，带有预设的 Mock 行为  
        /// </summary>  
        /// <typeparam name="T">实体类型</typeparam>  
        /// <returns>仓储实例</returns>  
        public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T>() where T : EntityBase
        {
            TryInitSeedData<T>();

            var mockRepository = new Mock<IClientRepositoryBase<T>>();

            //设置 RepositoryBase.BaseDB.ManualDetectChangeObject
            var mockBaseDB = new Mock<NcfUnitTestDataDb>();
            //mockBaseDB.Setup(z => z.ManualDetectChangeObject).Returns(true);
            mockBaseDB.SetupProperty(z => z.ManualDetectChangeObject, true);
            mockRepository.Setup(z => z.BaseDB).Returns(new NcfUnitTestDataDb(this._serviceProvider));

            var dataList = dataLists[typeof(T)].Cast<T>().ToList();

            // Mock GetFirstOrDefaultObjectAsync 方法  
            mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((expr, includes) =>
                {
                    var func = expr.Compile();
                    return Task.FromResult(dataList.FirstOrDefault(func));
                });

            // Mock GetFirstOrDefaultObject 方法  
            mockRepository.Setup(z => z.GetFirstOrDefaultObject(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((expr, includes) =>
                {
                    var func = expr.Compile();
                    return dataList.FirstOrDefault(func);
                });

            // Mock GeAll 方法  
            mockRepository.Setup(z => z.GeAll(It.IsAny<Expression<Func<T, object>>>(), It.IsAny<OrderingType>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, object>>, OrderingType, string[]>((orderBy, orderingType, includes) =>
                {
                    return dataList.AsQueryable().OrderBy(orderBy);
                });

            // Mock ObjectCount 方法  
            mockRepository.Setup(z => z.ObjectCount(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((where, includes) =>
                {
                    var func = where.Compile();
                    return dataList.Count(func);
                });

            // Mock Add 方法  
            mockRepository.Setup(z => z.Add(It.IsAny<T>()))
                .Callback<T>(obj =>
                {
                    dataList.Add(obj);
                });

            // Mock Delete 方法  
            mockRepository.Setup(z => z.Delete(It.IsAny<T>(), It.IsAny<bool>()))
                .Callback<T, bool>((obj, softDelete) =>
                {
                    dataList.Remove(obj);
                });

            // Mock SaveChanges 方法  
            mockRepository.Setup(z => z.SaveChanges())
                .Callback(() => { /* No-op for in-memory data */ });

            // Mock DeleteAsync 方法  
            mockRepository.Setup(z => z.DeleteAsync(It.IsAny<T>(), It.IsAny<bool>()))
                .Returns<T, bool>((obj, softDelete) =>
                {
                    dataList.Remove(obj);
                    return Task.CompletedTask;
                });

            // Mock SaveChangesAsync 方法  
            mockRepository.Setup(z => z.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Add mocks for other methods similarly...  

            // Mock GetObjectListAsync 方法  
            mockRepository.Setup(z => z.GetObjectListAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, object>>>(), It.IsAny<OrderingType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, Expression<Func<T, object>>, OrderingType, int, int, string[]>((where, orderBy, orderingType, pageIndex, pageCount, includes) =>
                {
                    var func = where.Compile();
                    var orderedData = orderingType == OrderingType.Ascending ?
                        dataList.AsQueryable().Where(where).OrderBy(orderBy) :
                        dataList.AsQueryable().Where(where).OrderByDescending(orderBy);
                    return Task.FromResult(new PagedList<T>(orderedData.Skip((pageIndex - 1) * pageCount).Take(pageCount).ToList(), pageIndex, pageCount, dataList.Count));
                });

            // Mock GetFirstOrDefaultObjectAsync 方法 (带 includesNavigationPropertyPathFunc)  
            mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<DbSet<T>, IIncludableQueryable<T, object>>>>()))
                .Returns<Expression<Func<T, bool>>, Expression<Func<DbSet<T>, IIncludableQueryable<T, object>>>>((where, includesNavigationPropertyPathFunc) =>
                {
                    var func = where.Compile();
                    return Task.FromResult(dataList.FirstOrDefault(func));
                });

            // Mock ObjectCountAsync 方法  
            mockRepository.Setup(z => z.ObjectCountAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .Returns<Expression<Func<T, bool>>, string[]>((where, includes) =>
                {
                    var func = where.Compile();
                    return Task.FromResult(dataList.Count(func));
                });

            return (MockRepository: mockRepository, DataList: dataList);
        }

        public Mock<TInterface> CreateMockForExtendedInterface<TInterface, TBase>(Mock<TBase> baseMock)
            where TInterface : class, TBase
            where TBase : class
        {
            return baseMock.As<TInterface>();
        }
    }
}
