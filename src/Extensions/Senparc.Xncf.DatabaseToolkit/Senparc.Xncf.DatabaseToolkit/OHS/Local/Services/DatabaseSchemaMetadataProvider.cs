using Senparc.Ncf.XncfBase;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.Services
{
    /// <summary>
    /// 数据库架构元数据提供者
    /// 用于获取和管理数据库结构信息
    /// </summary>
    public class DatabaseSchemaMetadataProvider
    {
        private Dictionary<string, List<DatabaseSchemaMetadata>> _schemaCache;
        private bool _isInitialized = false;

        public DatabaseSchemaMetadataProvider(IServiceProvider serviceProvider)
        {
            _schemaCache = new Dictionary<string, List<DatabaseSchemaMetadata>>();
        }

        /// <summary>
        /// 初始化元数据缓存
        /// 扫描所有已注册的 XNCF 模块中的实体类
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                var modules = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var module in modules)
                {
                    var moduleName = module.GetName().Name;
                    if (string.IsNullOrEmpty(moduleName))
                        continue;

                    var moduleSchemas = new List<DatabaseSchemaMetadata>();

                    // 获取模块中的所有实体类
                    // 查找 DatabaseModel 或 Models 命名空间下的实体
                    var entityTypes = GetEntityTypesFromModule(module);

                    foreach (var entityType in entityTypes)
                    {
                        var schema = CreateSchemaFromEntityType(entityType, moduleName);
                        if (schema != null)
                        {
                            moduleSchemas.Add(schema);
                        }
                    }

                    if (moduleSchemas.Count > 0)
                    {
                        _schemaCache[moduleName] = moduleSchemas;
                    }
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                // 记录错误但继续，避免启动失败
                System.Diagnostics.Debug.WriteLine($"DatabaseSchemaMetadataProvider initialization error: {ex.Message}");
            }
        }

        // EntityBase 的完全限定类型名，避免强制引用 Senparc.Ncf.Core 程序集
        private static readonly string EntityBaseTypeName = "Senparc.Ncf.Core.Models.EntityBase";

        /// <summary>
        /// 从模块程序集中获取实体类（继承自 EntityBase 的非抽象类）
        /// </summary>
        private List<Type> GetEntityTypesFromModule(Assembly assembly)
        {
            var entityTypes = new List<Type>();

            try
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (!type.IsAbstract &&
                        !type.IsInterface &&
                        IsEntityBaseSubclass(type))
                    {
                        entityTypes.Add(type);
                    }
                }
            }
            catch
            {
                // 忽略错误（如无法加载依赖项的程序集）
            }

            return entityTypes;
        }

        /// <summary>
        /// 判断类型是否继承自 Senparc.Ncf.Core.Models.EntityBase
        /// </summary>
        private static bool IsEntityBaseSubclass(Type type)
        {
            var current = type.BaseType;
            while (current != null && current != typeof(object))
            {
                var fullName = current.FullName ?? string.Empty;
                var baseName = current.IsGenericType
                    ? (current.GetGenericTypeDefinition().FullName ?? string.Empty)
                    : fullName;
                if (baseName.StartsWith(EntityBaseTypeName, StringComparison.Ordinal))
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        /// <summary>
        /// 从实体类型创建架构元数据
        /// </summary>
        private DatabaseSchemaMetadata CreateSchemaFromEntityType(Type entityType, string moduleName)
        {
            try
            {
                var schema = new DatabaseSchemaMetadata
                {
                    ModuleName = moduleName,
                    TableName = entityType.Name,
                    EntityFullName = entityType.FullName,
                    DisplayName = entityType.Name,
                    IsVisible = true
                };

                // 获取类的 XML 注释（如果存在）
                var attributes = entityType.GetCustomAttributes();
                var descriptionAttr = attributes.FirstOrDefault(a => a.GetType().Name == "DescriptionAttribute");
                if (descriptionAttr != null)
                {
                    var valueProperty = descriptionAttr.GetType().GetProperty("Description");
                    if (valueProperty != null)
                    {
                        schema.Description = valueProperty.GetValue(descriptionAttr) as string;
                    }
                }

                // 获取所有公共属性
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    // 跳过某些不应该在查询中暴露的属性
                    if (ShouldSkipProperty(prop.Name))
                        continue;

                    var columnMetadata = new DatabaseColumnMetadata
                    {
                        ColumnName = prop.Name,
                        ColumnType = GetSimpleTypeName(prop.PropertyType),
                        DisplayName = prop.Name,
                        IsPrimaryKey = prop.Name == "Id",
                        IsRequired = IsRequiredProperty(prop),
                        MaxLength = GetMaxLength(prop),
                        IsFilterable = IsFilterableType(prop.PropertyType),
                        IsVisible = true
                    };

                    schema.Columns.Add(columnMetadata);
                }

                return schema;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 判断是否应该跳过某个属性
        /// </summary>
        private bool ShouldSkipProperty(string propertyName)
        {
            var skipProperties = new[] { "ConcurrencyStamp" };
            return skipProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取简化的类型名称
        /// </summary>
        private string GetSimpleTypeName(Type type)
        {
            if (type.IsGenericType)
                return type.GetGenericTypeDefinition().Name;
            return type.Name;
        }

        /// <summary>
        /// 判断属性是否为必需字段
        /// </summary>
        private bool IsRequiredProperty(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes();
            return attributes.Any(a => a.GetType().Name == "RequiredAttribute");
        }

        /// <summary>
        /// 获取属性的最大长度
        /// </summary>
        private int? GetMaxLength(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes();
            var maxLengthAttr = attributes.FirstOrDefault(a => a.GetType().Name == "MaxLengthAttribute");
            if (maxLengthAttr != null)
            {
                var lengthProperty = maxLengthAttr.GetType().GetProperty("Length");
                if (lengthProperty != null)
                {
                    if (int.TryParse(lengthProperty.GetValue(maxLengthAttr)?.ToString(), out int length))
                    {
                        return length;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 判断类型是否可以用于过滤
        /// </summary>
        private bool IsFilterableType(Type type)
        {
            var filterableTypes = new[] 
            { 
                typeof(string), typeof(int), typeof(long), typeof(double), 
                typeof(decimal), typeof(bool), typeof(DateTime), typeof(Guid)
            };

            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            return filterableTypes.Contains(underlyingType);
        }

        /// <summary>
        /// 将用户输入的模块名解析为缓存中的精确 key。
        /// 匹配顺序：精确 → 大小写不敏感 → 后缀（"AIKernel"→"Senparc.Xncf.AIKernel"）
        ///           → XncfRegisterManager.Name/MenuName → 包含（唯一时）
        /// 返回 null 表示未找到。
        /// </summary>
        public string ResolveModuleName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            EnsureInitialized();

            // 1. 精确匹配
            if (_schemaCache.ContainsKey(input))
                return input;

            // 2. 大小写不敏感精确匹配
            var caseInsensitive = _schemaCache.Keys
                .FirstOrDefault(k => string.Equals(k, input, StringComparison.OrdinalIgnoreCase));
            if (caseInsensitive != null)
                return caseInsensitive;

            // 3. 后缀匹配（如 "AIKernel" 匹配 "Senparc.Xncf.AIKernel"）
            var suffix = _schemaCache.Keys
                .Where(k => k.EndsWith("." + input, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (suffix.Count == 1)
                return suffix[0];

            // 4. 通过 XncfRegisterManager.RegisterList 匹配 Name / MenuName
            //    优先使用已注册模块的官方 Name 及人性化 MenuName 进行匹配
            var registerMatches = XncfRegisterManager.RegisterList
                .Where(r =>
                    string.Equals(r.Name, input, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.MenuName, input, StringComparison.OrdinalIgnoreCase) ||
                    (r.Name?.Contains(input, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.MenuName?.Contains(input, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            foreach (var register in registerMatches)
            {
                var assemblyName = register.GetType().Assembly.GetName().Name;
                if (!string.IsNullOrEmpty(assemblyName) && _schemaCache.ContainsKey(assemblyName))
                    return assemblyName;
            }

            // 5. 包含匹配（最宽松，仅在唯一匹配时使用）
            var contains = _schemaCache.Keys
                .Where(k => k.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (contains.Count == 1)
                return contains[0];

            return null;
        }

        /// <summary>
        /// 在已解析模块中模糊匹配实体名（表名）。
        /// 匹配优先级（高→低）：精确 > 大小写不敏感 > 前缀/被包含 > 包含
        /// 唯一最高分匹配时返回精确 TableName，否则返回 null。
        /// </summary>
        public string ResolveEntityName(string resolvedModuleName, string inputEntityName)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(resolvedModuleName) || string.IsNullOrWhiteSpace(inputEntityName))
                return null;

            if (!_schemaCache.TryGetValue(resolvedModuleName, out var schemas))
                return null;

            var shortInput = GetShortTypeName(inputEntityName.Trim());
            var fullInput = inputEntityName.Trim();

            var scored = schemas
                .Where(s => s.IsVisible)
                .Select(s => (Schema: s, Score: ScoreEntityMatch(s, shortInput, fullInput)))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            if (scored.Count == 0)
                return null;

            // 唯一最高分 → 返回；多个相同最高分 → 歧义，返回 null
            if (scored.Count == 1 || scored[1].Score < scored[0].Score)
                return scored[0].Schema.TableName;

            return null;
        }

        /// <summary>
        /// 对单个 schema 与输入实体名的匹配程度打分（0 = 不匹配）
        /// </summary>
        private static int ScoreEntityMatch(DatabaseSchemaMetadata schema, string shortInput, string fullInput)
        {
            var schemaShort = GetShortTypeName(schema.TableName ?? string.Empty);
            var schemaFull = schema.EntityFullName ?? string.Empty;

            // 精确匹配（短名）
            if (string.Equals(schemaShort, shortInput, StringComparison.OrdinalIgnoreCase))
                return 100;

            // 精确匹配（全限定名）
            if (string.Equals(schemaFull, fullInput, StringComparison.OrdinalIgnoreCase))
                return 90;

            // 前缀：schema名 以 input 开头，或 input 以 schema名 开头
            if (schemaShort.StartsWith(shortInput, StringComparison.OrdinalIgnoreCase) ||
                shortInput.StartsWith(schemaShort, StringComparison.OrdinalIgnoreCase))
                return 60;

            // 包含：schema名包含 input
            if (schemaShort.Contains(shortInput, StringComparison.OrdinalIgnoreCase))
                return 40;

            // 包含：input 包含 schema名
            if (shortInput.Contains(schemaShort, StringComparison.OrdinalIgnoreCase))
                return 30;

            return 0;
        }

        /// <summary>
        /// 获取所有模块的架构信息
        /// </summary>
        public Dictionary<string, List<DatabaseSchemaDto>> GetAllSchemas()
        {
            EnsureInitialized();
            var result = new Dictionary<string, List<DatabaseSchemaDto>>();

            foreach (var kvp in _schemaCache)
            {
                result[kvp.Key] = kvp.Value
                    .Where(s => s.IsVisible)
                    .Select(ConvertToDto)
                    .ToList();
            }

            return result;
        }

        /// <summary>
        /// 获取指定模块的架构信息
        /// </summary>
        public List<DatabaseSchemaDto> GetSchemasByModule(string moduleName)
        {
            EnsureInitialized();
            if (_schemaCache.TryGetValue(moduleName, out var schemas))
            {
                return schemas
                    .Where(s => s.IsVisible)
                    .Select(ConvertToDto)
                    .ToList();
            }

            return new List<DatabaseSchemaDto>();
        }

        /// <summary>
        /// 获取所有已知模块名称（缓存 key 列表），用于错误提示。
        /// </summary>
        public IReadOnlyList<string> GetAllModuleNames()
        {
            EnsureInitialized();
            return _schemaCache.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取指定模块下所有可见实体的短名称，用于错误提示。
        /// </summary>
        public IReadOnlyList<string> GetTableNames(string moduleName)
        {
            EnsureInitialized();
            if (!_schemaCache.TryGetValue(moduleName, out var schemas))
                return Array.Empty<string>();
            return schemas
                .Where(s => s.IsVisible)
                .Select(s => GetShortTypeName(s.TableName ?? string.Empty))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// 获取指定表的架构信息
        /// </summary>
        public DatabaseSchemaDto GetSchemaByTable(string moduleName, string tableName)
        {
            EnsureInitialized();
            if (_schemaCache.TryGetValue(moduleName, out var schemas))
            {
                var schema = schemas.FirstOrDefault(s =>
                    s.IsVisible && IsEntityNameMatch(s, tableName));
                return schema != null ? ConvertToDto(schema) : null;
            }

            return null;
        }

        /// <summary>
        /// 获取实体的完全限定名
        /// </summary>
        public string GetEntityFullName(string moduleName, string tableName)
        {
            EnsureInitialized();
            if (_schemaCache.TryGetValue(moduleName, out var schemas))
            {
                var schema = schemas.FirstOrDefault(s => IsEntityNameMatch(s, tableName));
                return schema?.EntityFullName;
            }

            return null;
        }

        /// <summary>
        /// 转换为 DTO
        /// </summary>
        private DatabaseSchemaDto ConvertToDto(DatabaseSchemaMetadata metadata)
        {
            return new DatabaseSchemaDto
            {
                ModuleName = metadata.ModuleName,
                TableName = metadata.TableName,
                EntityFullName = metadata.EntityFullName,
                DisplayName = metadata.DisplayName,
                Description = metadata.Description,
                Columns = metadata.Columns
                    .Where(c => c.IsVisible)
                    .Select(c => new DatabaseColumnDto
                    {
                        ColumnName = c.ColumnName,
                        ColumnType = c.ColumnType,
                        DisplayName = c.DisplayName,
                        Description = c.Description,
                        IsPrimaryKey = c.IsPrimaryKey,
                        IsRequired = c.IsRequired,
                        MaxLength = c.MaxLength,
                        IsFilterable = c.IsFilterable,
                        IsVisible = c.IsVisible
                    })
                    .ToList(),
                IsVisible = metadata.IsVisible
            };
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            InitializeAsync().GetAwaiter().GetResult();
        }

        private static bool IsEntityNameMatch(DatabaseSchemaMetadata schema, string inputName)
        {
            if (string.IsNullOrWhiteSpace(inputName))
            {
                return false;
            }

            var normalizedInput = inputName.Trim();
            var shortInput = GetShortTypeName(normalizedInput);
            var schemaShortName = GetShortTypeName(schema.TableName);
            var schemaFullName = schema.EntityFullName ?? string.Empty;

            return string.Equals(schemaShortName, shortInput, StringComparison.OrdinalIgnoreCase)
                || string.Equals(schemaFullName, normalizedInput, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetShortTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return string.Empty;
            }

            var trimmed = typeName.Trim();
            var index = trimmed.LastIndexOf('.');
            return index >= 0 ? trimmed[(index + 1)..] : trimmed;
        }
    }
}
