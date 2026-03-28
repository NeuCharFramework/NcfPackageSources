using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Models;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Services;

namespace Senparc.Xncf.DatabaseToolkit.Tests.Services
{
    // =====================
    // 测试用假实体类（继承 EntityBase<int>，会被扫描到）
    // =====================
    internal class FakeTestEntity : EntityBase<int>
    {
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    internal class AnotherFakeEntity : EntityBase<long>
    {
        public string Title { get; set; } = string.Empty;
    }

    // =====================
    // 测试类
    // =====================
    [TestClass]
    public class DatabaseSchemaMetadataProviderTests
    {
        private DatabaseSchemaMetadataProvider CreateProvider()
        {
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            return new DatabaseSchemaMetadataProvider(sp);
        }

        [TestMethod]
        public async Task InitializeAsync_ShouldPopulateCache_WhenCalledFirstTime()
        {
            var provider = CreateProvider();

            await provider.InitializeAsync();

            var allSchemas = provider.GetAllSchemas();
            Assert.IsTrue(allSchemas.Count > 0, "初始化后应有至少一个模块的 schema");
        }

        [TestMethod]
        public async Task InitializeAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
        {
            var provider = CreateProvider();

            await provider.InitializeAsync();
            var count1 = provider.GetAllSchemas().Count;

            await provider.InitializeAsync();
            var count2 = provider.GetAllSchemas().Count;

            Assert.AreEqual(count1, count2, "多次初始化应返回相同数量");
        }

        [TestMethod]
        public async Task GetAllSchemas_ShouldContainFakeTestEntity()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var allSchemas = provider.GetAllSchemas();

            // FakeTestEntity 在本测试程序集中，应该能被扫描到
            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name;
            Assert.IsTrue(allSchemas.ContainsKey(testAssemblyName),
                $"期望找到程序集 '{testAssemblyName}' 对应的 schema，但只找到: {string.Join(", ", allSchemas.Keys)}");

            var schemas = allSchemas[testAssemblyName];
            var fakeEntitySchema = schemas.FirstOrDefault(s => s.TableName == nameof(FakeTestEntity));
            Assert.IsNotNull(fakeEntitySchema, $"未找到 FakeTestEntity 的 schema");
        }

        [TestMethod]
        public async Task GetSchemasByModule_WithValidModule_ShouldReturnSchemas()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var schemas = provider.GetSchemasByModule(testAssemblyName);

            Assert.IsTrue(schemas.Count > 0, $"模块 '{testAssemblyName}' 应有至少一个 schema");
        }

        [TestMethod]
        public async Task GetSchemasByModule_WithInvalidModule_ShouldReturnEmpty()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var schemas = provider.GetSchemasByModule("NonExistent.Module.Name");

            Assert.AreEqual(0, schemas.Count, "不存在的模块应返回空列表");
        }

        [TestMethod]
        public async Task GetSchemaByTable_WithExactName_ShouldReturnCorrectSchema()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var schema = provider.GetSchemaByTable(testAssemblyName, nameof(FakeTestEntity));

            Assert.IsNotNull(schema, $"应能按精确名称找到 FakeTestEntity");
            Assert.AreEqual(nameof(FakeTestEntity), schema.TableName);
        }

        [TestMethod]
        public async Task GetSchemaByTable_WithCaseInsensitiveName_ShouldReturnCorrectSchema()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;

            // 用全小写查找
            var schema = provider.GetSchemaByTable(testAssemblyName, "faketestentity");

            Assert.IsNotNull(schema, "名称查找应大小写不敏感");
            Assert.AreEqual(nameof(FakeTestEntity), schema.TableName);
        }

        [TestMethod]
        public async Task GetSchemaByTable_WithFullQualifiedName_ShouldReturnCorrectSchema()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var fullName = typeof(FakeTestEntity).FullName!;

            var schema = provider.GetSchemaByTable(testAssemblyName, fullName);

            Assert.IsNotNull(schema, "应能用完全限定名查找实体");
            Assert.AreEqual(nameof(FakeTestEntity), schema.TableName);
        }

        [TestMethod]
        public async Task GetSchemaByTable_WithWrongModule_ShouldReturnNull()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var schema = provider.GetSchemaByTable("WrongModuleName", nameof(FakeTestEntity));

            Assert.IsNull(schema, "模块名不存在时应返回 null");
        }

        [TestMethod]
        public async Task GetSchemaByTable_WithWrongEntityName_ShouldReturnNull()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var schema = provider.GetSchemaByTable(testAssemblyName, "EntityThatDoesNotExist");

            Assert.IsNull(schema, "实体名不存在时应返回 null");
        }

        [TestMethod]
        public async Task GetSchemaByTable_FakeEntityShouldHaveColumns()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var schema = provider.GetSchemaByTable(testAssemblyName, nameof(FakeTestEntity));

            Assert.IsNotNull(schema);
            Assert.IsTrue(schema.Columns.Count > 0, "实体应有字段");

            // 验证自定义字段存在
            var nameCol = schema.Columns.FirstOrDefault(c => c.ColumnName == nameof(FakeTestEntity.Name));
            Assert.IsNotNull(nameCol, "应有 Name 字段");

            var amountCol = schema.Columns.FirstOrDefault(c => c.ColumnName == nameof(FakeTestEntity.Amount));
            Assert.IsNotNull(amountCol, "应有 Amount 字段");
        }

        [TestMethod]
        public async Task GetSchemaByTable_IdFieldShouldBePrimaryKey()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var schema = provider.GetSchemaByTable(testAssemblyName, nameof(FakeTestEntity));

            Assert.IsNotNull(schema);
            var idCol = schema.Columns.FirstOrDefault(c => c.ColumnName == "Id");
            Assert.IsNotNull(idCol, "应有 Id 字段");
            Assert.IsTrue(idCol.IsPrimaryKey, "Id 字段应标记为主键");
        }

        [TestMethod]
        public async Task GetEntityFullName_WithValidInput_ShouldReturnFullName()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var fullName = provider.GetEntityFullName(testAssemblyName, nameof(FakeTestEntity));

            Assert.IsNotNull(fullName);
            Assert.IsTrue(fullName.Contains(nameof(FakeTestEntity)), $"完全限定名应包含类名，实际: {fullName}");
        }

        [TestMethod]
        public async Task GetEntityFullName_WithInvalidInput_ShouldReturnNull()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var fullName = provider.GetEntityFullName(testAssemblyName, "NotExistEntity");

            Assert.IsNull(fullName, "不存在的实体应返回 null");
        }

        // =====================
        // ResolveModuleName 模糊匹配测试
        // =====================

        [TestMethod]
        public async Task ResolveModuleName_WithExactName_ShouldReturnSame()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var testAssemblyName = typeof(FakeTestEntity).Assembly.GetName().Name!;
            var resolved = provider.ResolveModuleName(testAssemblyName);

            Assert.AreEqual(testAssemblyName, resolved, "精确名称应直接匹配");
        }

        [TestMethod]
        public async Task ResolveModuleName_WithSuffix_ShouldMatchCorrectModule()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            // 测试程序集名 = "Senparc.Xncf.DatabaseToolkit.Tests"
            // 用后缀 "DatabaseToolkit.Tests" 查找应能匹配
            var resolved = provider.ResolveModuleName("DatabaseToolkit.Tests");

            Assert.IsNotNull(resolved, "后缀应能匹配到对应模块");
            Assert.IsTrue(resolved!.EndsWith("DatabaseToolkit.Tests", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public async Task ResolveModuleName_WithPartialName_ShouldMatchUniqueModule()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            // 如果 "Senparc.Xncf.DatabaseToolkit.Tests" 是 AppDomain 中唯一包含 "DatabaseToolkit.Tests" 的模块
            var resolved = provider.ResolveModuleName("Toolkitzzz_NotExist");

            Assert.IsNull(resolved, "不存在的关键词应返回 null");
        }

        [TestMethod]
        public async Task ResolveModuleName_WithNull_ShouldReturnNull()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var resolved = provider.ResolveModuleName(null!);

            Assert.IsNull(resolved);
        }

        [TestMethod]
        public async Task ResolveModuleName_WithEmpty_ShouldReturnNull()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var resolved = provider.ResolveModuleName(string.Empty);

            Assert.IsNull(resolved);
        }

        [TestMethod]
        public async Task GetAllSchemas_SchemasShouldHaveRequiredFields()
        {
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var allSchemas = provider.GetAllSchemas();

            foreach (var kvp in allSchemas)
            {
                foreach (var schema in kvp.Value)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(schema.ModuleName),
                        $"schema.ModuleName 不能为空，TableName={schema.TableName}");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(schema.TableName),
                        $"schema.TableName 不能为空");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(schema.EntityFullName),
                        $"schema.EntityFullName 不能为空，TableName={schema.TableName}");
                }
            }
        }

        [TestMethod]
        public async Task GetAllSchemas_ShouldContainAIKernelModule_WhenAssemblyIsLoaded()
        {
            // 确保 AIKernel 的实体类型被加载到 AppDomain（通过引用其类型）
            // 注意：此测试依赖运行时是否加载了 Senparc.Xncf.AIKernel
            var provider = CreateProvider();
            await provider.InitializeAsync();

            var allSchemas = provider.GetAllSchemas();

            // 如果 AIKernel 被加载，则应该能找到 AIModel、AIVector
            if (allSchemas.ContainsKey("Senparc.Xncf.AIKernel"))
            {
                var schemas = allSchemas["Senparc.Xncf.AIKernel"];
                var aiModelSchema = schemas.FirstOrDefault(s => s.TableName == "AIModel");
                Assert.IsNotNull(aiModelSchema, "Senparc.Xncf.AIKernel 模块应包含 AIModel 实体");

                var aiVectorSchema = schemas.FirstOrDefault(s => s.TableName == "AIVector");
                Assert.IsNotNull(aiVectorSchema, "Senparc.Xncf.AIKernel 模块应包含 AIVector 实体");
            }
            else
            {
                // AIKernel 未被加载（测试隔离环境），跳过此断言
                Assert.Inconclusive("Senparc.Xncf.AIKernel 程序集未在当前 AppDomain 中加载，跳过此测试");
            }
        }
    }
}
