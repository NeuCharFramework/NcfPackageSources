using Senparc.Xncf.DatabaseToolkit.OHS.Local.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// 从模块程序集中获取实体类
        /// </summary>
        private List<Type> GetEntityTypesFromModule(Assembly assembly)
        {
            var entityTypes = new List<Type>();

            try
            {
                var types = assembly.GetTypes();

                // 查找在 DatabaseModel 命名空间中的类
                foreach (var type in types)
                {
                    if (type.Namespace != null &&
                        (type.Namespace.Contains("DatabaseModel") || 
                         type.Namespace.Contains("Models.DatabaseModel")) &&
                        !type.IsAbstract &&
                        !type.IsInterface &&
                        HasId属性(type)) // 简单检查是否为实体（有 Id 属性）
                    {
                        entityTypes.Add(type);
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return entityTypes;
        }

        /// <summary>
        /// 检查类型是否有 Id 属性（标识实体）
        /// </summary>
        private bool HasId属性(Type type)
        {
            return type.GetProperty("Id") != null;
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
