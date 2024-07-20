using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.UnitTestExtension.Entities;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Ncf.UnitTestExtension.Database
{
    /// <summary>
    /// 模拟 SenparcEntities 或各个 XNCF 模块中的 xxxSenparcEntities。
    /// 在单元测试过程中，这个类将取代 NCF 运行时的 SenparcEntities，集成所有模块的 DbSet 对象
    /// </summary>
    public class NcfUnitTestEntities : BasePoolEntities //DbContext
    {
        bool _baseOnModelCreatingCalled = false;
        DataList _dataList;
        private readonly Dictionary<Type, object> _dbSets = new();

        public NcfUnitTestEntities(DbContextOptions dbContextOptions, IServiceProvider serviceProvider, DataList dataList) : base(dbContextOptions, serviceProvider)
        {
            _dataList = dataList;
        }

        public static IList ConvertList(IList list, Type outType)
        {
            var genericListType = typeof(List<>).MakeGenericType(outType);
            var convertedList = (IList)Activator.CreateInstance(genericListType);

            foreach (var item in list)
            {
                convertedList.Add(Convert.ChangeType(item, outType));
            }
            return convertedList;
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Console.WriteLine("NcfUnitTestEntities OnConfiguring");

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("NcfUnitTestEntities OnModelCreating");

            base.OnModelCreating(modelBuilder);

            _baseOnModelCreatingCalled = true;

            // Get all entity types  
            var entityTypes = modelBuilder.Model.GetEntityTypes();

            Console.WriteLine("entityTypes:" + entityTypes.Select(z => z.ClrType.Name).ToJson(true));

            foreach (var entityType in entityTypes)
            {
                var clrType = entityType.ClrType;

                var data = _dataList.GetType().GetMethod("GetDataList").MakeGenericMethod(clrType).Invoke(_dataList, null) as IList;

                //var data = _dataList.GetList(clrType);
                if (data != null)
                {
                    //ConvertList(data, clrType);

                    Console.WriteLine("clrType：" + clrType.FullName);

                    var entityData = data.AsQueryable();

                    var mockDbSet = CreateMockDbSet(entityData, clrType);

                    // Add the Mock DbSet to the _dbSets dictionary  
                    _dbSets[clrType] = mockDbSet;
                }
                else
                {
                    Console.WriteLine("未找到 clrType 对应数据集：" + clrType.FullName);
                }
            }
        }

        private object CreateMockDbSet(IQueryable entityData, Type clrType)
        {
            var queryableMockType = typeof(Mock<>).MakeGenericType(typeof(IQueryable<>).MakeGenericType(clrType));
            var queryableMock = (Mock)Activator.CreateInstance(queryableMockType);
          
            // Setup the IQueryable properties  
            queryableMock.As<IQueryable>().Setup(m => m.Provider).Returns(entityData.Provider);
            queryableMock.As<IQueryable>().Setup(m => m.Expression).Returns(entityData.Expression);
            queryableMock.As<IQueryable>().Setup(m => m.ElementType).Returns(entityData.ElementType);
            queryableMock.As<IQueryable>().Setup(m => m.GetEnumerator()).Returns(entityData.GetEnumerator());

            var dbSetMockType = typeof(Mock<>).MakeGenericType(typeof(DbSet<>).MakeGenericType(clrType));
            var dbSetMock = (Mock)Activator.CreateInstance(dbSetMockType);

            // Setup the DbSet methods  
            dbSetMock.As<IQueryable>().Setup(m => m.Provider).Returns(entityData.Provider);
            dbSetMock.As<IQueryable>().Setup(m => m.Expression).Returns(entityData.Expression);
            dbSetMock.As<IQueryable>().Setup(m => m.ElementType).Returns(entityData.ElementType);
            dbSetMock.As<IQueryable>().Setup(m => m.GetEnumerator()).Returns(entityData.GetEnumerator());


            //Async
            //var mockAsyncQueryProvider = new Mock<IAsyncQueryProvider>();
            //mockAsyncQueryProvider.Setup(m => m.ExecuteAsync<object>(It.IsAny<Expression>(), It.IsAny<CancellationToken>()))
            //    .Returns<Expression, CancellationToken>((expression, token) => new ValueTask<object>(entityData.Provider.Execute(expression)));
            //mockAsyncQueryProvider.Setup(m => m.ExecuteAsync(It.IsAny<Expression>(), It.IsAny<CancellationToken>()))
            //    .Returns<Expression, CancellationToken>((expression, token) => new ValueTask(entityData.Provider.Execute(expression)));
            //mockAsyncQueryProvider.Setup(m => m.CreateQuery<object>(It.IsAny<Expression>())).Returns<Expression>(expression => entityData.Provider.CreateQuery<object>(expression));
            //mockAsyncQueryProvider.Setup(m => m.CreateQuery(It.IsAny<Expression>())).Returns<Expression>(expression => entityData.Provider.CreateQuery(expression));

            //queryableMock.As<IAsyncEnumerable<object>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(entityData.GetAsyncEnumerator());
            //queryableMock.As<IQueryable>().Setup(m => m.Provider).Returns(mockAsyncQueryProvider.Object);

            //dbSetMock.As<IAsyncEnumerable<object>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(entityData.GetAsyncEnumerator());
            //dbSetMock.As<IQueryable>().Setup(m => m.Provider).Returns(mockAsyncQueryProvider.Object);

            return dbSetMock.Object;
        }

        public override DbSet<TEntity> Set<TEntity>()
        {
            if (_baseOnModelCreatingCalled)
            {
                return _dbSets[typeof(TEntity)] as DbSet<TEntity>;
            }

            //方案二：
            //if (_dbSets.TryGetValue(typeof(TEntity), out var dbSet))
            //{
            //    return dbSet as DbSet<TEntity>;
            //}


            //由于 base.OnModelCreating 可能也会访问到 DbSet<TEntity>，而此时斌没有完成完全的 Mock 初始化，
            //所以这种情况下还是返回原始对象
            return base.Set<TEntity>();
        }
    }

    //public class DbContextMockHelper
    //{
    //    public static Mock<TContext> CreateMockDbContext<TContext>(IEnumerable<object> data)
    //        where TContext : DbContext
    //    {
    //        var mockContext = new Mock<TContext>();
    //        var modelBuilder = new ModelBuilder(new Microsoft.EntityFrameworkCore.Metadata.Conventions.ConventionSet());

    //        // Create an instance of the DbContext to access OnModelCreating  
    //        var contextInstance = Activator.CreateInstance<TContext>();
    //        var onModelCreatingMethod = typeof(TContext).GetMethod("OnModelCreating", BindingFlags.Instance | BindingFlags.NonPublic);
    //        onModelCreatingMethod.Invoke(contextInstance, new object[] { modelBuilder });

    //        // Get all entity types  
    //        var entityTypes = modelBuilder.Model.GetEntityTypes();

    //        // Get the _dbSets field  
    //        var dbSetField = typeof(TContext).GetField("_dbSets", BindingFlags.NonPublic | BindingFlags.Instance);
    //        var dbSets = (Dictionary<Type, object>)dbSetField.GetValue(mockContext.Object);

    //        foreach (var entityType in entityTypes)
    //        {
    //            var clrType = entityType.ClrType;
    //            var entityData = data.Where(d => d.GetType() == clrType).AsQueryable();

    //            // Create Mock DbSet  
    //            var mockSetType = typeof(Mock<>).MakeGenericType(typeof(DbSet<>).MakeGenericType(clrType));
    //            var mockSet = (Mock)Activator.CreateInstance(mockSetType);

    //            // Setup IQueryable methods using reflection  
    //            var queryableType = typeof(IQueryable<>).MakeGenericType(clrType);
    //            var providerProperty = queryableType.GetProperty(nameof(IQueryable.Provider));
    //            var expressionProperty = queryableType.GetProperty(nameof(IQueryable.Expression));
    //            var elementTypeProperty = queryableType.GetProperty(nameof(IQueryable.ElementType));
    //            var getEnumeratorMethod = queryableType.GetMethod(nameof(IQueryable.GetEnumerator));

    //            var mockSetAsQueryable = mockSet.GetType().GetMethod("As").MakeGenericMethod(queryableType).Invoke(mockSet, null);
    //            providerProperty.SetValue(mockSetAsQueryable, entityData.Provider);
    //            expressionProperty.SetValue(mockSetAsQueryable, entityData.Expression);
    //            elementTypeProperty.SetValue(mockSetAsQueryable, entityData.ElementType);
    //            getEnumeratorMethod.Invoke(mockSetAsQueryable, new object[] { entityData.GetEnumerator() });

    //            // Add the Mock DbSet to the DbContext mock  
    //            dbSets[clrType] = mockSet.Object;
    //        }

    //        return mockContext;
    //    }
    //}

}
