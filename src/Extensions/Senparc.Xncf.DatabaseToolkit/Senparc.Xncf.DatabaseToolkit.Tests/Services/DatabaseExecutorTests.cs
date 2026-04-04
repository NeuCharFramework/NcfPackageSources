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

        // ===================== Bug 修复验证测试 =====================

        /// <summary>
        /// Bug 4 修复验证：AlwaysTrue&lt;T&gt; 必须返回 Expression&lt;Func&lt;T,bool&gt;&gt;（而非 Func&lt;T,bool&gt;）
        /// 否则传给 GetFullListAsync 的参数类型不匹配，反射调用会失败。
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
        /// Bug 1+2 修复验证：使用正确的 Expression&lt;Func&lt;T,bool&gt;&gt; + 全参数类型
        /// 可以通过反射定位到 GetFullListAsync 正确重载。
        /// </summary>
        [TestMethod]
        public void GetFullListAsync_ShouldBeLocatable_WithExpressionPredicateAndAllParams()
        {
            var entityType = typeof(FakeTestEntity);
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);

            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var expressionPredicateType = typeof(Expression<>).MakeGenericType(funcType);

            // 必须提供全部参数类型（含可选参数），否则 GetMethod 无法定位
            var method = serviceType.GetMethod("GetFullListAsync",
                new[] { expressionPredicateType, typeof(string), typeof(string[]) });

            Assert.IsNotNull(method,
                "修复后应能通过反射找到 " +
                "GetFullListAsync(Expression<Func<T,bool>>, string, string[]) 重载");
        }

        /// <summary>
        /// 旧代码 Bug 对照：用 Func&lt;T,bool&gt; + 仅 1 个参数类型时，GetMethod 找不到方法。
        /// 此测试反向验证原始错误确实存在。
        /// </summary>
        [TestMethod]
        public void GetFullListAsync_ShouldNotBeFound_WhenUsingRawFuncAndSingleParam()
        {
            var entityType = typeof(FakeTestEntity);
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);

            // 原始错误：Func<T,bool>（非 Expression<>），且只提供 1 个参数类型
            var wrongPredicateType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));

            var method = serviceType.GetMethod("GetFullListAsync", new[] { wrongPredicateType });

            Assert.IsNull(method,
                "用未包装的 Func<T,bool> 查找 GetFullListAsync 时不应找到方法（对照旧 Bug）");
        }

        // ===================== 功能集成测试 =====================

        /// <summary>
        /// 当 ServiceBase&lt;T&gt; 未注册到 DI 时，QueryRecordsAsync 应返回 total=0 而不是抛异常。
        /// </summary>
        [TestMethod]
        public async Task QueryRecordsAsync_WhenServiceNotInDI_ReturnsZeroTotal()
        {
            var metadataProvider = CreateInitializedProvider();
            var emptyDiProvider = new ServiceCollection().BuildServiceProvider();

            var executor = new DatabaseExecutor(emptyDiProvider, metadataProvider);

            // 匿名类型跨程序集时 dynamic 访问会失败，改用 JsonDocument
            var raw = await executor.QueryRecordsAsync(
                TestAssemblyModuleName, nameof(FakeTestEntity), "", 1, 10);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(raw));

            Assert.AreEqual(0, doc.RootElement.GetProperty("total").GetInt32(),
                "ServiceBase 未注册时应静默返回空结果 total=0，不抛异常");
        }

        /// <summary>
        /// 完整流程验证（覆盖 Bug 1/2/3/4）：
        /// 注册了带有 Mock 数据的 ServiceBase 后，QueryRecordsAsync 能正确通过反射调用
        /// GetFullListAsync 并返回数据。
        /// </summary>
        [TestMethod]
        public async Task QueryRecordsAsync_WhenServiceRegistered_ReturnsDataFromGetFullListAsync()
        {
            // --- Arrange ---
            var mockRepo = new Mock<IRepositoryBase<FakeTestEntity>>();

            // ServiceBase 构造函数内会调用 _serviceProvider.GetService<IMapper>()，返回 null 即可
            var mockInnerSp = new Mock<IServiceProvider>();

            var testItems = new List<FakeTestEntity>
            {
                new() { Name = "Alpha" },
                new() { Name = "Beta" }
            };
            var pagedList = new PagedList<FakeTestEntity>(testItems, 1, 10, testItems.Count);

            // Mock<ServiceBase<T>>：Moq 会调用真实构造函数，并拦截虚方法 GetFullListAsync
            var mockService = new Mock<ServiceBase<FakeTestEntity>>(
                mockRepo.Object, mockInnerSp.Object);
            mockService
                .Setup(s => s.GetFullListAsync(
                    It.IsAny<Expression<Func<FakeTestEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<string[]>()))
                .ReturnsAsync(pagedList);

            // 将 mock 注册到 DI（DatabaseExecutor 使用 ServiceBase<T> 作为 DI key）
            var services = new ServiceCollection();
            services.AddScoped<ServiceBase<FakeTestEntity>>(_ => mockService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var metadataProvider = CreateInitializedProvider();
            var executor = new DatabaseExecutor(serviceProvider, metadataProvider);

            // --- Act ---
            var raw = await executor.QueryRecordsAsync(
                TestAssemblyModuleName, nameof(FakeTestEntity), "", 1, 10);

            // --- Assert ---
            // 匿名类型跨程序集时 dynamic 访问会失败，改用 JsonDocument
            using var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(raw));
            Assert.AreEqual(2, doc2.RootElement.GetProperty("total").GetInt32(),
                "应返回 mock 中的 2 条记录");

            mockService.Verify(
                s => s.GetFullListAsync(
                    It.IsAny<Expression<Func<FakeTestEntity, bool>>>(),
                    It.IsAny<string?>(),
                    It.IsAny<string[]>()),
                Times.Once,
                "GetFullListAsync 应在 QueryRecordsAsync 执行中被准确调用一次");
        }

        /// <summary>
        /// GetTableStatisticsAsync 同样依赖修复后的 GetAllRowsAsync，应能获取正确统计数据。
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
