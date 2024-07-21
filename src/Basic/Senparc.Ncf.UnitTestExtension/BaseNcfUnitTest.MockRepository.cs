using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.UnitTestExtension
{
    //public partial class BaseNcfUnitTest
    //{
        //public (IClientRepositoryBase<T> Repository, List<T> DataList) GetRespositoryBase<T>() where T : EntityBase
        //{
        //    var dataList = dataLists[typeof(T)].Cast<T>().ToList();

        //    //var mockRepository = new Mock<IClientRepositoryBase<T>>();

        //    //设置 RepositoryBase.BaseDB.ManualDetectChangeObject
        //    var mockBaseDB = new Mock<NcfUnitTestDataDb>();
        //    //mockBaseDB.Setup(z => z.ManualDetectChangeObject).Returns(true);


        //    mockBaseDB.Setup(z => z.DataContext.Set<T>())
        //              .Returns<DbSet<T>>(z =>
        //              {
        //                  var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);
        //                  return mockDbSet.Object;
        //              });

        //    var repo = new ClientRepositoryBase<T>(mockBaseDB.Object);

        //    return (Repository: repo, DataList: dataList);
        //}

        ///// <summary>  
        ///// 获取指定类型的仓储实例，带有预设的 Mock 行为  
        ///// </summary>  
        ///// <typeparam name="T">实体类型</typeparam>  
        ///// <returns>仓储实例</returns>  
        //public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T, TKey>() where T : EntityBase<TKey>
        //{
        //    var repo = GetRespository<T>();

        //    var dataList = repo.DataList;

        //    // Mock Update 方法  
        //    repo.MockRepository.Setup(z => z.Update(It.IsAny<T>()))
        //        .Callback<T>(obj =>
        //        {
        //            var existing = dataList.FirstOrDefault(o => o == obj);
        //            if (existing != null)
        //            {
        //                var index = dataList.IndexOf(existing);
        //                dataList[index] = obj;
        //            }
        //        });

        //    // Mock SaveAsync 方法  
        //    repo.MockRepository.Setup(z => z.SaveAsync(It.IsAny<T>()))
        //         .Returns<T>(obj =>
        //         {
        //             var existing = dataList.FirstOrDefault(o => o == obj);
        //             if (existing != null)
        //             {
        //                 var index = dataList.IndexOf(existing);
        //                 dataList[index] = obj;
        //             }
        //             else
        //             {
        //                 dataList.Add(obj);
        //             }
        //             return Task.CompletedTask;
        //         });

        //    return repo;
        //}

        //public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T, TOrderProperty, TIncludesProperty>() where T : EntityBase
        //{
        //    TryInitSeedData<T>();

        //    var dataList = dataLists[typeof(T)].Cast<T>().ToList();

        //    var mockRepository = new Mock<IClientRepositoryBase<T>>();

        //    //设置 RepositoryBase.BaseDB.ManualDetectChangeObject
        //    var mockBaseDB = new Mock<NcfUnitTestDataDb>();
        //    //mockBaseDB.Setup(z => z.ManualDetectChangeObject).Returns(true);


        //    //mockBaseDB.Setup(z => z.DataContext.Set<T>())
        //    //          .Returns<DbSet<T>>(z =>
        //    //          {
        //    //              var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);
        //    //              return mockDbSet.Object;
        //    //          });


        //    mockBaseDB.SetupProperty(z => z.ManualDetectChangeObject, true);
        //    mockRepository.Setup(z => z.BaseDB).Returns(mockBaseDB.Object);


        //    // Mock GetFirstOrDefaultObjectAsync 方法  
        //    mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
        //        .Returns<Expression<Func<T, bool>>, string[]>((expr, includes) =>
        //        {
        //            var func = expr.Compile();
        //            return Task.FromResult(dataList.FirstOrDefault(func));
        //        });

        //    // Mock GetFirstOrDefaultObject 方法  
        //    mockRepository.Setup(z => z.GetFirstOrDefaultObject(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
        //        .Returns<Expression<Func<T, bool>>, string[]>((expr, includes) =>
        //        {
        //            var func = expr.Compile();
        //            return dataList.FirstOrDefault(func);
        //        });

        //    // Mock GeAll 方法  
        //    mockRepository.Setup(z => z.GeAll(It.IsAny<Expression<Func<T, object>>>(), It.IsAny<OrderingType>(), It.IsAny<string[]>()))
        //        .Returns<Expression<Func<T, object>>, OrderingType, string[]>((orderBy, orderingType, includes) =>
        //        {
        //            return dataList.AsQueryable().OrderBy(orderBy);
        //        });

        //    // Mock ObjectCount 方法  
        //    mockRepository.Setup(z => z.ObjectCount(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
        //        .Returns<Expression<Func<T, bool>>, string[]>((where, includes) =>
        //        {
        //            var func = where.Compile();
        //            return dataList.Count(func);
        //        });

        //    // Mock Add 方法  
        //    mockRepository.Setup(z => z.Add(It.IsAny<T>()))
        //        .Callback<T>(obj =>
        //        {
        //            dataList.Add(obj);
        //        });

        //    // Mock Delete 方法  
        //    mockRepository.Setup(z => z.Delete(It.IsAny<T>(), It.IsAny<bool>()))
        //        .Callback<T, bool>((obj, softDelete) =>
        //        {
        //            dataList.Remove(obj);
        //        });

        //    // Mock SaveChanges 方法  
        //    mockRepository.Setup(z => z.SaveChanges())
        //        .Callback(() => { /* No-op for in-memory data */ });

        //    // Mock DeleteAsync 方法  
        //    mockRepository.Setup(z => z.DeleteAsync(It.IsAny<T>(), It.IsAny<bool>()))
        //        .Returns<T, bool>((obj, softDelete) =>
        //        {
        //            dataList.Remove(obj);
        //            return Task.CompletedTask;
        //        });

        //    // Mock SaveChangesAsync 方法  
        //    mockRepository.Setup(z => z.SaveChangesAsync())
        //        .Returns(Task.CompletedTask);


        //    //// 泛型版本的Mock设置  
        //    //mockRepository.Setup(z => z.GetObjectListAsync<TOrderProperty>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, TOrderProperty>>>(), It.IsAny<OrderingType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string[]>()))
        //    //    .Returns<Expression<Func<T, bool>>, Expression<Func<T, TOrderProperty>>, OrderingType, int, int, string[]>((where, orderBy, orderingType, pageIndex, pageCount, includes) =>
        //    //    {
        //    //        Console.WriteLine("GetObjectListAsync run with TOrderProperty:" + typeof(T).Name);
        //    //        var func = where.Compile();
        //    //        var orderedData = orderingType == OrderingType.Ascending ?
        //    //            dataList.AsQueryable().Where(where).OrderBy(orderBy) :
        //    //            dataList.AsQueryable().Where(where).OrderByDescending(orderBy);
        //    //        return Task.FromResult(new PagedList<T>(orderedData.Skip((pageIndex - 1) * pageCount).Take(pageCount).ToList(), pageIndex, pageCount, dataList.Count));
        //    //    });


        //    // Mock GetObjectListAsync 方法
        //    mockRepository.Setup(z => z.GetObjectListAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, TOrderProperty>>>(), It.IsAny<OrderingType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string[]>()))
        //        .Returns<Expression<Func<T, bool>>, Expression<Func<T, int>>, OrderingType, int, int, string[]>((where, orderBy, orderingType, pageIndex, pageCount, includes) =>
        //        {
        //            var func = where.Compile();
        //            var orderedData = orderingType == OrderingType.Ascending ?
        //                dataList.AsQueryable().Where(where).OrderBy(orderBy) :
        //                dataList.AsQueryable().Where(where).OrderByDescending(orderBy);

        //            var result = pageIndex <= 0 && pageCount <= 0
        //                ? orderedData.ToList()
        //                : orderedData.Skip((pageIndex - 1) * pageCount).Take(pageCount).ToList();

        //            return Task.FromResult(new PagedList<T>(result, pageIndex, pageCount, dataList.Count));
        //        });

        //    //GetObjectListAsync(Expression<Func<T, bool>> where, string OrderbyField, int pageIndex, int pageCount, params string[] includes)
        //    mockRepository.Setup(repo => repo.GetObjectListAsync(
        //        It.IsAny<Expression<Func<T, bool>>>(),
        //        It.IsAny<string>(),
        //        It.IsAny<int>(),
        //        It.IsAny<int>(),
        //        It.IsAny<string[]>()))
        //    .Returns<Expression<Func<T, bool>>, string, int, int, string[]>((where, OrderbyField, pageIndex, pageCount, includes) =>
        //    {
        //        var func = where.Compile();
        //        var orderedData = dataList.AsQueryable().Where(where).OrderByExtension(OrderbyField);

        //        var result = pageIndex <= 0 && pageCount <= 0
        //            ? orderedData.ToList()
        //            : orderedData.Skip((pageIndex - 1) * pageCount).Take(pageCount).ToList();

        //        return Task.FromResult(new PagedList<T>(result, pageIndex, pageCount, dataList.Count));
        //    }); ;

        //    // Mock GetFirstOrDefaultObjectAsync 方法 (带 includesNavigationPropertyPathFunc)  
        //    mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>()))
        //        .Returns<Expression<Func<T, bool>>, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>((where, includesNavigationPropertyPathFunc) =>
        //        {
        //            //TODO:待完善
        //            var func = where.Compile();
        //            return Task.FromResult(dataList.FirstOrDefault(func));
        //        });

        //    mockRepository.Setup(z => z.GetFirstOrDefaultObjectAsync(
        //        It.IsAny<Expression<Func<T, bool>>>(),
        //        It.IsAny<Expression<Func<T, TOrderProperty>>>(),
        //        It.IsAny<OrderingType>(),
        //        It.IsAny<Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>()))
        //    .Returns<Expression<Func<T, bool>>, Expression<Func<T, TOrderProperty>>, OrderingType, Expression<Func<DbSet<T>, IIncludableQueryable<T, TIncludesProperty>>>>(
        //        async (where, orderBy, orderingType, includesNavigationPropertyPathFunc) =>
        //        {
        //            var mockDbSet = UnitTestHelper.CreateMockDbSet(dataList);

        //            // 使用Mock DbSet  
        //            var query = await includesNavigationPropertyPathFunc.Compile()(mockDbSet.Object)
        //                                .OrderBy(orderBy, orderingType)
        //                                .Where(where)
        //                                .OrderBy(orderBy, orderingType).FirstOrDefaultAsync();

        //            return query;
        //        });


        //    // Mock ObjectCountAsync 方法  
        //    mockRepository.Setup(z => z.ObjectCountAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
        //        .Returns<Expression<Func<T, bool>>, string[]>((where, includes) =>
        //        {
        //            var func = where.Compile();
        //            return Task.FromResult(dataList.Count(func));
        //        });

        //    return (MockRepository: mockRepository, DataList: dataList);
        //}

        ///// <summary>  
        ///// 获取指定类型的仓储实例，带有预设的 Mock 行为  
        ///// </summary>  
        ///// <typeparam name="T">实体类型（OrderBy 默认为 int 类型，include 默认为 object 类型）</typeparam>  
        ///// <returns>仓储实例</returns>  
        //public (Mock<IClientRepositoryBase<T>> MockRepository, List<T> DataList) GetRespository<T>() where T : EntityBase
        //{
        //    var result = this.GetRespository<T, int, object>();
        //    return result;
        //}

        ///// <summary>
        ///// 获取指定类型的仓储实例
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <returns></returns>
        //public IClientRepositoryBase<T> GetRespositoryObject<T>() where T : EntityBase
        //{
        //    return this.GetRespository<T>().MockRepository.Object;
        //}

        //public Mock<TInterface> CreateMockForExtendedInterface<TInterface, TBase>(Mock<TBase> baseMock)
        //    where TInterface : class, TBase
        //    where TBase : class
        //{
        //    return baseMock.As<TInterface>();
        //}
//    }
}
