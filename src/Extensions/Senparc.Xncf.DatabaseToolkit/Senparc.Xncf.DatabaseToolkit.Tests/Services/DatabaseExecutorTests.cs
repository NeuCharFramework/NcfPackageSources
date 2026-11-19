using Microsoft.Extensions.DependencyInjection;
using Moq;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Services;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Senparc.Xncf.DatabaseToolkit.Tests.Services
{
    [TestClass]
    public class DatabaseExecutorTests
    {
        private static readonly string TestAssemblyModuleName =
            typeof(FakeTestEntity).Assembly.GetName().Name!;

        private static DatabaseSchemaMetadataProvider CreateInitializedProvider()
        {
            var sp = new ServiceCollection().BuildServiceProvider();
            var provider = new DatabaseSchemaMetadataProvider(sp);
            provider.InitializeAsync().GetAwaiter().GetResult();
            return provider;
        }

        //Legacy comment translated to English.

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [TestMethod]
        public void AlwaysTrue_ShouldReturn_ExpressionPredicate_NotRawFunc()
        {
            var alwaysTrueMethod = typeof(DatabaseExecutor)
                .GetMethod("AlwaysTrue", BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(FakeTestEntity));

            var result = alwaysTrueMethod.Invoke(null, null);

            Assert.IsNotNull(result, "AlwaysTrue<T> 方法应存在且返回非 null");
            Assert.IsInstanceOfType(result, typeof(Expression<Func<FakeTestEntity, bool>>),
                "AlwaysTrue<T> 必须返回 Expression<Func<T,bool>>，" +
                "否则无法作为参数传给 GetFullListAsync(Expression<Func<T,bool>>, ...)");
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [TestMethod]
        public void GetFullListAsync_ShouldBeLocatable_WithExpressionPredicateAndAllParams()
        {
            var entityType = typeof(FakeTestEntity);
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);

            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var expressionPredicateType = typeof(Expression<>).MakeGenericType(funcType);

            //Legacy comment translated to English.
            var method = serviceType.GetMethod("GetFullListAsync",
                new[] { expressionPredicateType, typeof(string), typeof(string[]) });

            Assert.IsNotNull(method,
                "修复后应能通过反射找到 " +
                "GetFullListAsync(Expression<Func<T,bool>>, string, string[]) 重载");
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [TestMethod]
        public void GetFullListAsync_ShouldNotBeFound_WhenUsingRawFuncAndSingleParam()
        {
            var entityType = typeof(FakeTestEntity);
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);

            //Legacy comment translated to English.
            var wrongPredicateType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));

            var method = serviceType.GetMethod("GetFullListAsync", new[] { wrongPredicateType });

            Assert.IsNull(method,
                "用未包装的 Func<T,bool> 查找 GetFullListAsync 时不应找到方法（对照旧 Bug）");
        }

        //Legacy comment translated to English.

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [TestMethod]
        public async Task QueryRecordsAsync_WhenServiceNotInDI_ReturnsZeroTotal()
        {
            var metadataProvider = CreateInitializedProvider();
            var emptyDiProvider = new ServiceCollection().BuildServiceProvider();

            var executor = new DatabaseExecutor(emptyDiProvider, metadataProvider);

            //Legacy comment translated to English.
            var raw = await executor.QueryRecordsAsync(
                TestAssemblyModuleName, nameof(FakeTestEntity), "", 1, 10);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(raw));

            Assert.AreEqual(0, doc.RootElement.GetProperty("total").GetInt32(),
                "ServiceBase 未注册时应静默返回空结果 total=0，不抛异常");
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [TestMethod]
        public async Task QueryRecordsAsync_WhenServiceRegistered_ReturnsDataFromGetFullListAsync()
        {
            // --- Arrange ---
            var mockRepo = new Mock<IRepositoryBase<FakeTestEntity>>();

            //Legacy comment translated to English.
            var mockInnerSp = new Mock<IServiceProvider>();

            var testItems = new List<FakeTestEntity>
            {
                new() { Name = "Alpha" },
                new() { Name = "Beta" }
            };
            var pagedList = new PagedList<FakeTestEntity>(testItems, 1, 10, testItems.Count);

            //Legacy comment translated to English.
            var mockService = new Mock<ServiceBase<FakeTestEntity>>(
                mockRepo.Object, mockInnerSp.Object);
            mockService
                .Setup(s => s.GetObjectListAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<FakeTestEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<string[]>()))
                .ReturnsAsync(pagedList);

            //Legacy comment translated to English.
            var services = new ServiceCollection();
            services.AddScoped<ServiceBase<FakeTestEntity>>(_ => mockService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var metadataProvider = CreateInitializedProvider();
            var executor = new DatabaseExecutor(serviceProvider, metadataProvider);

            // --- Act ---
            var raw = await executor.QueryRecordsAsync(
                TestAssemblyModuleName, nameof(FakeTestEntity), "", 1, 10);

            // --- Assert ---
            //Legacy comment translated to English.
            using var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(raw));
            Assert.AreEqual(2, doc2.RootElement.GetProperty("total").GetInt32(),
                "应返回 mock 中的 2 条记录");

            mockService.Verify(
                s => s.GetObjectListAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<FakeTestEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<string[]>()),
                Times.Once,
                "GetObjectListAsync 应在 QueryRecordsAsync 执行中被准确调用一次");
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [TestMethod]
        public async Task GetTableStatisticsAsync_WhenServiceRegistered_ReturnsCorrectStats()
        {
            var mockRepo = new Mock<IRepositoryBase<FakeTestEntity>>();
            var mockInnerSp = new Mock<IServiceProvider>();

            var emptyList = new PagedList<FakeTestEntity>(new List<FakeTestEntity>(), 1, 10, 0);
            var mockService = new Mock<ServiceBase<FakeTestEntity>>(
                mockRepo.Object, mockInnerSp.Object);
            mockService
                .Setup(s => s.GetFullListAsync(
                    It.IsAny<Expression<Func<FakeTestEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<string[]>()))
                .ReturnsAsync(emptyList);

            var services = new ServiceCollection();
            services.AddScoped<ServiceBase<FakeTestEntity>>(_ => mockService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var metadataProvider = CreateInitializedProvider();
            var executor = new DatabaseExecutor(serviceProvider, metadataProvider);

            var statsRaw = await executor.GetTableStatisticsAsync(
                TestAssemblyModuleName, nameof(FakeTestEntity));
            using var statsDoc = JsonDocument.Parse(JsonSerializer.Serialize(statsRaw));

            Assert.AreEqual(0, statsDoc.RootElement.GetProperty("total").GetInt32(),
                "空列表时 total 应为 0");
            Assert.IsTrue(statsDoc.RootElement.GetProperty("columnCount").GetInt32() > 0,
                "FakeTestEntity 含 Name/Amount 等属性，columnCount 应大于 0");
        }
    }
}
