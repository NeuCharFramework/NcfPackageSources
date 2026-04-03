using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.Services
{
    /// <summary>
    ///Universal Database Executor
    /// Provide module/table level data query and statistical capabilities
    /// </summary>
    public class DatabaseExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DatabaseSchemaMetadataProvider _metadataProvider;

        public DatabaseExecutor(IServiceProvider serviceProvider, DatabaseSchemaMetadataProvider metadataProvider)
        {
            _serviceProvider = serviceProvider;
            _metadataProvider = metadataProvider;
        }

        public async Task<object> QueryRecordsAsync(string moduleName, string tableName, string filter, int pageNumber, int pageSize)
        {
            var schema = _metadataProvider.GetSchemaByTable(moduleName, tableName);
            if (schema == null)
            {
                return new { total = 0, data = Array.Empty<object>(), message = "schema not found" };
            }

            var entityType = ResolveType(schema.EntityFullName);
            if (entityType == null)
            {
                return new { total = 0, data = Array.Empty<object>(), message = "entity type not found" };
            }

            var allRows = await GetAllRowsAsync(entityType).ConfigureAwait(false);
            var total = allRows.Count;

            var skip = Math.Max(0, (pageNumber - 1) * pageSize);
            var pageData = allRows.Skip(skip).Take(Math.Max(1, pageSize)).ToList();

            return new
            {
                module = moduleName,
                table = tableName,
                filter,
                pageNumber,
                pageSize,
                total,
                data = pageData
            };
        }

        public async Task<object> GetTableStatisticsAsync(string moduleName, string tableName)
        {
            var schema = _metadataProvider.GetSchemaByTable(moduleName, tableName);
            if (schema == null)
            {
                return new { module = moduleName, table = tableName, total = 0, message = "schema not found" };
            }

            var entityType = ResolveType(schema.EntityFullName);
            if (entityType == null)
            {
                return new { module = moduleName, table = tableName, total = 0, message = "entity type not found" };
            }

            var allRows = await GetAllRowsAsync(entityType).ConfigureAwait(false);

            return new
            {
                module = moduleName,
                table = tableName,
                total = allRows.Count,
                columnCount = schema.Columns.Count,
                columns = schema.Columns.Select(c => new
                {
                    name = c.ColumnName,
                    type = c.ColumnType,
                    isPrimaryKey = c.IsPrimaryKey,
                    isRequired = c.IsRequired
                })
            };
        }

        private async Task<List<object>> GetAllRowsAsync(Type entityType)
        {
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);
            var serviceInstance = _serviceProvider.GetService(serviceType);
            if (serviceInstance == null)
            {
                return new List<object>();
            }

            // Constructs type Expression<Func<T, bool>> (instead of Func<T, bool>), consistent with GetFullListAsync signature
            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var expressionPredicateType = typeof(Expression<>).MakeGenericType(funcType);

            var alwaysTrueMethod = typeof(DatabaseExecutor)
                .GetMethod(nameof(AlwaysTrue), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(entityType);
            var predicate = alwaysTrueMethod?.Invoke(null, null);

            // All parameter types (including optional parameters) must be provided, otherwise GetMethod cannot locate the correct overload
            var method = serviceType.GetMethod("GetFullListAsync",
                new[] { expressionPredicateType, typeof(string), typeof(string[]) });
            if (method == null)
            {
                return new List<object>();
            }

            // All parameters must be passed in when calling (optional parameters must also be provided explicitly)
            var task = method.Invoke(serviceInstance,
                new object[] { predicate, null, Array.Empty<string>() }) as Task;
            if (task == null)
            {
                return new List<object>();
            }

            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty?.GetValue(task) as IEnumerable;
            return result?.Cast<object>().ToList() ?? new List<object>();
        }

        private static Expression<Func<T, bool>> AlwaysTrue<T>()
        {
            return _ => true;
        }

        private static Type ResolveType(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            var directType = Type.GetType(fullTypeName);
            if (directType != null)
            {
                return directType;
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
