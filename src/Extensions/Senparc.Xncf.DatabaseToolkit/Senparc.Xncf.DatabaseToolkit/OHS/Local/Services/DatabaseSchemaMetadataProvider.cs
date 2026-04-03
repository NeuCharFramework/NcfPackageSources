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
    /// Database schema metadata provider
    /// Used to obtain and manage database structure information
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
        /// Initialize metadata cache
        /// Scan all registered XNCF modules for entity classes
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

                    // Get all entity classes in the module
                    // Find entities under the DatabaseModel or Models namespace
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
                // Log error but continue to avoid startup failure
                System.Diagnostics.Debug.WriteLine($"DatabaseSchemaMetadataProvider initialization error: {ex.Message}");
            }
        }

        // Fully qualified type name of EntityBase to avoid forcing a reference to the Senparc.Ncf.Core assembly
        private static readonly string EntityBaseTypeName = "Senparc.Ncf.Core.Models.EntityBase";

        /// <summary>
        /// Get the entity class (non-abstract class inherited from EntityBase) from the module assembly
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
                // Ignore errors (such as the dependency's assembly cannot be loaded)
            }

            return entityTypes;
        }

        /// <summary>
        /// Determine whether the type inherits from Senparc.Ncf.Core.Models.EntityBase
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
        /// Create schema metadata from entity types
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

                // Get the XML annotation for a class (if present)
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

                // Get all public properties
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    // Skip certain properties that should not be exposed in queries
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
        /// Determine whether an attribute should be skipped
        /// </summary>
        private bool ShouldSkipProperty(string propertyName)
        {
            var skipProperties = new[] { "ConcurrencyStamp" };
            return skipProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get the simplified type name
        /// </summary>
        private string GetSimpleTypeName(Type type)
        {
            if (type.IsGenericType)
                return type.GetGenericTypeDefinition().Name;
            return type.Name;
        }

        /// <summary>
        /// Determine whether the attribute is a required field
        /// </summary>
        private bool IsRequiredProperty(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes();
            return attributes.Any(a => a.GetType().Name == "RequiredAttribute");
        }

        /// <summary>
        /// Get the maximum length of the attribute
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
        /// Determine whether the type can be used for filtering
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
        /// Resolve the module name entered by the user into the exact key in the cache.
        /// Matching order: Exact → Case-insensitive → Suffix ("AIKernel" → "Senparc.Xncf.AIKernel")
        /// → XncfRegisterManager.Name/MenuName → Contains (when unique)
        /// Returns null if not found.
        /// </summary>
        public string ResolveModuleName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            EnsureInitialized();

            // 1. Exact match
            if (_schemaCache.ContainsKey(input))
                return input;

            // 2. Case-insensitive exact matching
            var caseInsensitive = _schemaCache.Keys
                .FirstOrDefault(k => string.Equals(k, input, StringComparison.OrdinalIgnoreCase));
            if (caseInsensitive != null)
                return caseInsensitive;

            // 3. Suffix matching (such as "AIKernel" matches "Senparc.Xncf.AIKernel")
            var suffix = _schemaCache.Keys
                .Where(k => k.EndsWith("." + input, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (suffix.Count == 1)
                return suffix[0];

            // 4. Match Name / MenuName through XncfRegisterManager.RegisterList
            //    Priority is given to using the official Name and humanized MenuName of the registered module for matching.
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

            // 5. Inclusive match (the loosest, only used when there is a unique match)
            var contains = _schemaCache.Keys
                .Where(k => k.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (contains.Count == 1)
                return contains[0];

            return null;
        }

        /// <summary>
        /// Fuzzy matches entity names (table names) in parsed modules.
        /// Match priority (high → low): exact > case insensitive > prefix/included > contains
        /// Returns the exact TableName if the unique highest score matches, otherwise returns null.
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

            // Unique highest score → return; multiple identical highest scores → ambiguous, return null
            if (scored.Count == 1 || scored[1].Score < scored[0].Score)
                return scored[0].Schema.TableName;

            return null;
        }

        /// <summary>
        /// Score how well a single schema matches the input entity name (0 = no match)
        /// </summary>
        private static int ScoreEntityMatch(DatabaseSchemaMetadata schema, string shortInput, string fullInput)
        {
            var schemaShort = GetShortTypeName(schema.TableName ?? string.Empty);
            var schemaFull = schema.EntityFullName ?? string.Empty;

            // Exact match (short name)
            if (string.Equals(schemaShort, shortInput, StringComparison.OrdinalIgnoreCase))
                return 100;

            // Exact match (fully qualified name)
            if (string.Equals(schemaFull, fullInput, StringComparison.OrdinalIgnoreCase))
                return 90;

            // Prefix: schema name starts with input, or input starts with schema name
            if (schemaShort.StartsWith(shortInput, StringComparison.OrdinalIgnoreCase) ||
                shortInput.StartsWith(schemaShort, StringComparison.OrdinalIgnoreCase))
                return 60;

            // Contains: schema name contains input
            if (schemaShort.Contains(shortInput, StringComparison.OrdinalIgnoreCase))
                return 40;

            // Contains: input contains schema name
            if (shortInput.Contains(schemaShort, StringComparison.OrdinalIgnoreCase))
                return 30;

            return 0;
        }

        /// <summary>
        /// Get the architecture information of all modules
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
        /// Get the architecture information of the specified module
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
        /// Get all known module names (cache key list) for error prompts.
        /// </summary>
        public IReadOnlyList<string> GetAllModuleNames()
        {
            EnsureInitialized();
            return _schemaCache.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// Get the short names of all visible entities under the specified module for error prompts.
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
        /// Get the schema information of the specified table
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
        /// Get the fully qualified name of the entity
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
        /// Convert to DTO
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
