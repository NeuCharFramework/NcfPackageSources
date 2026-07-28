/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseExecutor.cs
    文件功能描述：DatabaseExecutor 相关实现
    
    
    创建标识：Senparc - 20260327
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260729
    修改描述：v0.27.0-preview3 增加数据库分页筛选执行能力并限制请求范围

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
    /// 通用数据库执行器
    /// 提供模块/表级别的数据查询与统计能力
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

        private async Task<List<object>> GetAllRowsAsync(Type entityType, string filter = null)
        {
            var serviceType = typeof(ServiceBase<>).MakeGenericType(entityType);
            var serviceInstance = _serviceProvider.GetService(serviceType);
            if (serviceInstance == null)
            {
                return new List<object>();
            }

            // 构造 Expression<Func<T, bool>> 类型（而非 Func<T, bool>），与 GetFullListAsync 签名一致
            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var expressionPredicateType = typeof(Expression<>).MakeGenericType(funcType);

            var predicate = BuildFilterPredicate(entityType, filter);

            // 必须提供全部参数类型（包括可选参数），否则 GetMethod 无法定位到正确重载
            var method = serviceType.GetMethod("GetFullListAsync",
                new[] { expressionPredicateType, typeof(string), typeof(string[]) });
            if (method == null)
            {
                return new List<object>();
            }

            // 调用时须传入全部参数（可选参数也必须明确提供）
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

        private static Expression<Func<T, bool>> AlwaysTrue<T>()
        {
            return _ => true;
        }

        private static object BuildFilterPredicate(Type entityType, string filter)
        {
            return typeof(DatabaseExecutor)
                .GetMethod(nameof(BuildFilterPredicateGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(entityType)
                ?.Invoke(null, new object[] { filter });
        }

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
