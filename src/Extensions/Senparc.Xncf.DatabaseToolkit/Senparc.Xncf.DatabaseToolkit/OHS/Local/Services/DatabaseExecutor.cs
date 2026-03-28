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

            // 构造 Expression<Func<T, bool>> 类型（而非 Func<T, bool>），与 GetFullListAsync 签名一致
            var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
            var expressionPredicateType = typeof(Expression<>).MakeGenericType(funcType);

            var alwaysTrueMethod = typeof(DatabaseExecutor)
                .GetMethod(nameof(AlwaysTrue), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(entityType);
            var predicate = alwaysTrueMethod?.Invoke(null, null);

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
