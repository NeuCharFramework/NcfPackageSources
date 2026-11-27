/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseSchemaMetadataProvider.cs
    File description: DatabaseSchemaMetadataProvider related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.27.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.27.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
    /// Discovers entity schemas from loaded XNCF assemblies and exposes metadata
    /// used by database browsing and query surfaces.
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
        /// Scans loaded assemblies and caches entity/table metadata.
        /// Initialization is idempotent; reflection failures are isolated per assembly.
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

                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                System.Diagnostics.Debug.WriteLine($"DatabaseSchemaMetadataProvider initialization error: {ex.Message}");
            }
        }

        //Legacy comment translated to English.
        private static readonly string EntityBaseTypeName = "Senparc.Ncf.Core.Models.EntityBase";

        /// <summary>
        /// Resolves an assembly's concrete <see cref="EntityBase"/> descendants.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>Concrete entity types found in the assembly.</returns>
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
                //Legacy comment translated to English.
            }

            return entityTypes;
        }

        /// <summary>
        /// Determines whether a type derives from the NCF entity base, including generic base definitions.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns><see langword="true"/> when the type is an entity descendant.</returns>
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
        /// Builds table and column metadata from an entity type's attributes and public properties.
        /// </summary>
        /// <param name="entityType">The entity type to describe.</param>
        /// <param name="moduleName">The containing assembly/module name.</param>
        /// <returns>Schema metadata, or <see langword="null"/> when reflection metadata cannot be read.</returns>
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

                //Legacy comment translated to English.
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

                // getallpublicproperty
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    //Legacy comment translated to English.
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
        /// Determines whether a property is excluded from database browsing metadata.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns><see langword="true"/> when the property should be omitted.</returns>
        private bool ShouldSkipProperty(string propertyName)
        {
            var skipProperties = new[] { "ConcurrencyStamp" };
            return skipProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the simple CLR type name, using the generic type definition name for generics.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>The display type name.</returns>
        private string GetSimpleTypeName(Type type)
        {
            if (type.IsGenericType)
                return type.GetGenericTypeDefinition().Name;
            return type.Name;
        }

        /// <summary>
        /// Determines whether a property has a Required data-annotation attribute.
        /// </summary>
        /// <param name="prop">The property to inspect.</param>
        /// <returns><see langword="true"/> when the property is required.</returns>
        private bool IsRequiredProperty(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes();
            return attributes.Any(a => a.GetType().Name == "RequiredAttribute");
        }

        /// <summary>
        /// Reads a MaxLength data-annotation value when present.
        /// </summary>
        /// <param name="prop">The property to inspect.</param>
        /// <returns>The maximum length, or <see langword="null"/> when unspecified.</returns>
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
        /// Determines whether a CLR type can be used as a database filter field.
        /// </summary>
        /// <param name="type">The property type.</param>
        /// <returns><see langword="true"/> for supported scalar filter types.</returns>
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
        /// Resolves a module input by exact name, case-insensitive name, suffix,
        /// registered module metadata, or unique substring.
        /// </summary>
        /// <param name="input">The module name or UID fragment.</param>
        /// <returns>The resolved assembly name, or <see langword="null"/> when ambiguous/unmatched.</returns>
        public string ResolveModuleName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            EnsureInitialized();

            // Reject ambiguous matches rather than silently selecting an arbitrary entity.
            if (_schemaCache.ContainsKey(input))
                return input;

            //Legacy comment translated to English.
            var caseInsensitive = _schemaCache.Keys
                .FirstOrDefault(k => string.Equals(k, input, StringComparison.OrdinalIgnoreCase));
            if (caseInsensitive != null)
                return caseInsensitive;

            //Legacy comment translated to English.
            var suffix = _schemaCache.Keys
                .Where(k => k.EndsWith("." + input, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (suffix.Count == 1)
                return suffix[0];

            //Legacy comment translated to English.
            //Legacy comment translated to English.
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

            //Legacy comment translated to English.
            var contains = _schemaCache.Keys
                .Where(k => k.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (contains.Count == 1)
                return contains[0];

            return null;
        }

        /// <summary>
        /// Resolves an entity/table name within a resolved module using scored short/full-name matching.
        /// </summary>
        /// <param name="resolvedModuleName">The resolved module assembly name.</param>
        /// <param name="inputEntityName">The entity name or full type name supplied by the caller.</param>
        /// <returns>The unambiguous table name, or <see langword="null"/>.</returns>
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

            //Legacy comment translated to English.
            if (scored.Count == 1 || scored[1].Score < scored[0].Score)
                return scored[0].Schema.TableName;

            return null;
        }

        /// <summary>
        /// Scores a candidate entity name against short and fully qualified input names.
        /// </summary>
        private static int ScoreEntityMatch(DatabaseSchemaMetadata schema, string shortInput, string fullInput)
        {
            var schemaShort = GetShortTypeName(schema.TableName ?? string.Empty);
            var schemaFull = schema.EntityFullName ?? string.Empty;

            if (string.Equals(schemaShort, shortInput, StringComparison.OrdinalIgnoreCase))
                return 100;

            if (string.Equals(schemaFull, fullInput, StringComparison.OrdinalIgnoreCase))
                return 90;

            if (schemaShort.StartsWith(shortInput, StringComparison.OrdinalIgnoreCase) ||
                shortInput.StartsWith(schemaShort, StringComparison.OrdinalIgnoreCase))
                return 60;

            if (schemaShort.Contains(shortInput, StringComparison.OrdinalIgnoreCase))
                return 40;

            if (shortInput.Contains(schemaShort, StringComparison.OrdinalIgnoreCase))
                return 30;

            return 0;
        }

        /// <summary>
        /// Returns all visible table schemas grouped by owning module.
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
        /// Returns all visible schemas for one resolved module name.
        /// </summary>
        /// <param name="moduleName">The canonical module name.</param>
        /// <returns>The visible schemas, or an empty list when the module is unknown.</returns>
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
        /// Returns canonical names of modules with discovered schemas.
        /// </summary>
        public IReadOnlyList<string> GetAllModuleNames()
        {
            EnsureInitialized();
            return _schemaCache.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns visible table names for one module.
        /// </summary>
        /// <param name="moduleName">The canonical module name.</param>
        /// <returns>The visible table names, or an empty list when the module is unknown.</returns>
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
        /// Resolves one visible table schema by module and table/entity name.
        /// </summary>
        /// <param name="moduleName">The canonical module name.</param>
        /// <param name="tableName">The short or fully qualified entity name.</param>
        /// <returns>The schema DTO, or <see langword="null"/> when no match exists.</returns>
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
        /// Resolves the fully qualified entity type name for a module/table pair.
        /// </summary>
        /// <param name="moduleName">The canonical module name.</param>
        /// <param name="tableName">The short or fully qualified entity name.</param>
        /// <returns>The entity type name, or <see langword="null"/> when no match exists.</returns>
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
        /// Projects internal schema metadata into the public schema DTO contract.
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

        /// <summary>
        /// Ensures schema metadata has been initialized before a synchronous lookup.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            InitializeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Matches a user-supplied entity name against short and fully qualified schema names.
        /// </summary>
        /// <param name="schema">The schema metadata to compare.</param>
        /// <param name="inputName">The user-supplied entity name.</param>
        /// <returns><see langword="true"/> when either short or full names match.</returns>
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

        /// <summary>
        /// Removes the namespace prefix from a type/table name.
        /// </summary>
        /// <param name="typeName">The full or short type name.</param>
        /// <returns>The final dot-separated segment.</returns>
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
