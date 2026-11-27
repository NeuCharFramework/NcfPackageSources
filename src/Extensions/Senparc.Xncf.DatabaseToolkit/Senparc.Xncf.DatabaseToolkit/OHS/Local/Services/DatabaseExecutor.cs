/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseExecutor.cs
    File description: DatabaseExecutor related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.27.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.27.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.27.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
    /// Executes reflection-based, schema-guided database queries without exposing arbitrary SQL.
    /// </summary>
    public class DatabaseExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DatabaseSchemaMetadataProvider _metadataProvider;

        /// <summary>
        /// Initializes the database query executor.
        /// </summary>
        /// <param name="serviceProvider">Provider used to resolve typed services.</param>
        /// <param name="metadataProvider">Schema metadata used to resolve modules and entities.</param>
        public DatabaseExecutor(IServiceProvider serviceProvider, DatabaseSchemaMetadataProvider metadataProvider)
        {
            _serviceProvider = serviceProvider;
            _metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Queries a bounded page of entity rows using a generated string-property contains predicate.
        /// </summary>
        /// <param name="moduleName">The module name or resolvable module input.</param>
        /// <param name="tableName">The entity/table name.</param>
        /// <param name="filter">Optional text applied to all readable string properties.</param>
        /// <param name="pageNumber">The one-based page number.</param>
        /// <param name="pageSize">The requested page size, bounded to 100.</param>
        /// <returns>A query result object containing metadata, total count, and rows.</returns>
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

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var pageResult = await GetPagedRowsAsync(entityType, filter, pageNumber, pageSize).ConfigureAwait(false);

            return new
            {
                module = moduleName,
                table = tableName,
                filter,
                pageNumber,
                pageSize,
                total = pageResult.Total,
                data = pageResult.Rows
            };
        }

        /// <summary>
        /// Returns row count and filterable column metadata for a resolved entity/table.
        /// </summary>
        /// <param name="moduleName">The module name or resolvable module input.</param>
        /// <param name="tableName">The entity/table name.</param>
        /// <returns>A statistics object, or a schema/type-not-found message.</returns>
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

        /// <summary>
        /// Loads all rows for a reflected entity service, optionally applying a string-property filter.
        /// </summary>
        /// <param name="entityType">The entity CLR type.</param>
        /// <param name="filter">Optional filter text.</param>
        /// <returns>Materialized entity rows.</returns>
        private async Task<List<object>> GetAllRowsAsync(Type entityType, string filter = null)
        {
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);
            var serviceInstance = _serviceProvider.GetService(serviceType);
            if (serviceInstance == null)
            {
                return new List<object>();
            }

            //Legacy comment translated to English.
            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var expressionPredicateType = typeof(Expression<>).MakeGenericType(funcType);

            var predicate = BuildFilterPredicate(entityType, filter);

            //Legacy comment translated to English.
            var method = serviceType.GetMethod("GetFullListAsync",
                new[] { expressionPredicateType, typeof(string), typeof(string[]) });
            if (method == null)
            {
                return new List<object>();
            }

            //Legacy comment translated to English.
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

        /// <summary>
        /// Loads a filtered page through the reflected ServiceBase paging overload.
        /// </summary>
        /// <param name="entityType">The entity CLR type.</param>
        /// <param name="filter">Optional string-property filter.</param>
        /// <param name="pageNumber">The requested page number.</param>
        /// <param name="pageSize">The requested page size.</param>
        /// <returns>Materialized rows and database-reported total count.</returns>
        private async Task<(List<object> Rows, int Total)> GetPagedRowsAsync(
            Type entityType,
            string filter,
            int pageNumber,
            int pageSize)
        {
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);
            var serviceInstance = _serviceProvider.GetService(serviceType);
            if (serviceInstance == null)
            {
                return (new List<object>(), 0);
            }

            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var expressionPredicateType = typeof(Expression<>).MakeGenericType(funcType);
            var predicate = BuildFilterPredicate(entityType, filter);

            // Use the repository's paged overload so filtering, Skip/Take, and
            // TotalCount are evaluated by the database instead of in memory.
            var method = serviceType.GetMethod("GetObjectListAsync",
                new[] { typeof(int), typeof(int), expressionPredicateType, typeof(string), typeof(string[]) });
            if (method == null)
            {
                return (new List<object>(), 0);
            }

            var task = method.Invoke(serviceInstance,
                new object[] { pageNumber, pageSize, predicate, "Id desc", Array.Empty<string>() }) as Task;
            if (task == null)
            {
                return (new List<object>(), 0);
            }

            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            var pagedResult = resultProperty?.GetValue(task);
            var rows = (pagedResult as IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
            var totalValue = pagedResult?.GetType().GetProperty("TotalCount")?.GetValue(pagedResult);
            var total = totalValue is int count ? count : rows.Count;
            return (rows, total);
        }

        /// <summary>
        /// Creates a predicate that accepts every entity.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An always-true predicate.</returns>
        private static Expression<Func<T, bool>> AlwaysTrue<T>()
        {
            return _ => true;
        }

        /// <summary>
        /// Builds a closed generic string-property predicate for a reflected entity type.
        /// </summary>
        /// <param name="entityType">The entity CLR type.</param>
        /// <param name="filter">The filter text.</param>
        /// <returns>An expression object compatible with the entity service.</returns>
        private static object BuildFilterPredicate(Type entityType, string filter)
        {
            return typeof(DatabaseExecutor)
                .GetMethod(nameof(BuildFilterPredicateGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(entityType)
                ?.Invoke(null, new object[] { filter });
        }

        /// <summary>
        /// Builds an OR predicate across all readable string properties of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="filter">The filter text.</param>
        /// <returns>An always-true predicate for blank input, otherwise a string contains predicate.</returns>
        private static Expression<Func<T, bool>> BuildFilterPredicateGeneric<T>(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return _ => true;
            }

            var parameter = Expression.Parameter(typeof(T), "row");
            var filterValue = Expression.Constant(filter.Trim(), typeof(string));
            Expression body = Expression.Constant(false);
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.PropertyType == typeof(string)))
            {
                var propertyValue = Expression.Property(parameter, property);
                var hasValue = Expression.NotEqual(propertyValue, Expression.Constant(null, typeof(string)));
                var contains = Expression.Call(propertyValue, containsMethod, filterValue);
                body = Expression.OrElse(body, Expression.AndAlso(hasValue, contains));
            }

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        /// <summary>
        /// Resolves a type by assembly-qualified name or by searching loaded assemblies.
        /// </summary>
        /// <param name="fullTypeName">The type name to resolve.</param>
        /// <returns>The resolved type, or <see langword="null"/> when unavailable.</returns>
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
