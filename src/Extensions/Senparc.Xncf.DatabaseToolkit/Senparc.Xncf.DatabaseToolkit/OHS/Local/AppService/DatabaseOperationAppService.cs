using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Models;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    /// <summary>
    /// Database operation AppService
    /// Provide functions to perform database query, update, delete and other operations
    /// </summary>
    public class DatabaseOperationAppService : AppServiceBase
    {
        private readonly DatabaseSchemaMetadataProvider _metadataProvider;
        private readonly DatabaseExecutor _databaseExecutor;

        public DatabaseOperationAppService(IServiceProvider serviceProvider, 
            DatabaseSchemaMetadataProvider metadataProvider, 
            DatabaseExecutor databaseExecutor)
            : base(serviceProvider)
        {
            _metadataProvider = metadataProvider;
            _databaseExecutor = databaseExecutor;
        }

        /// <summary>
        /// Query database records
        /// Query data in the table based on specified conditions and fields
        /// </summary>
        [FunctionRender("查询数据库记录", "查询指定表中的数据，支持条件过滤和字段选择", typeof(Register))]
        public async Task<AppResponseBase<string>> QueryRecords(QueryRecordsRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    // Validate modules and tables
                    if (string.IsNullOrWhiteSpace(request.ModuleName) || string.IsNullOrWhiteSpace(request.TableName))
                    {
                        return "模块名称和表名称不能为空";
                    }

                    // Fuzzy parsing module name
                    var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                    if (resolvedModule == null)
                    {
                        var available = _metadataProvider.GetAllModuleNames();
                        return $"找不到模块 '{request.ModuleName}'。可用模块：{string.Join(", ", available)}";
                    }

                    // Fuzzy parsing of entity names
                    var resolvedTable = _metadataProvider.ResolveEntityName(resolvedModule, request.TableName);
                    if (resolvedTable == null)
                    {
                        var available = _metadataProvider.GetTableNames(resolvedModule);
                        return $"找不到表 '{request.TableName}'（模块 '{resolvedModule}'）。可用实体：{string.Join(", ", available)}";
                    }

                    var schema = _metadataProvider.GetSchemaByTable(resolvedModule, resolvedTable);
                    if (schema == null)
                    {
                        return $"找不到表: {resolvedModule}.{resolvedTable}";
                    }

                    logger.Append($"查询表 {resolvedModule}.{resolvedTable}（原始输入: {request.ModuleName}.{request.TableName}）");

                    // Execute query
                    var result = await _databaseExecutor.QueryRecordsAsync(
                        resolvedModule,
                        resolvedTable,
                        request.Filter,
                        request.PageNumber,
                        request.PageSize);

                    return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                    });
                }
                catch (Exception ex)
                {
                    logger.Append($"查询记录时出错: {ex.Message}");
                    return $"错误: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Get database statistics
        /// Get statistical information such as the number of rows and minimum/maximum values ​​of the specified table
        /// </summary>
        [FunctionRender("获取统计信息", "获取数据库表的统计信息，如行数、数据大小等", typeof(Register))]
        public async Task<AppResponseBase<string>> GetStatistics(GetStatisticsRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.ModuleName) || string.IsNullOrWhiteSpace(request.TableName))
                    {
                        return "模块名称和表名称不能为空";
                    }

                    // Fuzzy parsing module name
                    var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                    if (resolvedModule == null)
                    {
                        var available = _metadataProvider.GetAllModuleNames();
                        return $"找不到模块 '{request.ModuleName}'。可用模块：{string.Join(", ", available)}";
                    }

                    // Fuzzy parsing of entity names
                    var resolvedTable = _metadataProvider.ResolveEntityName(resolvedModule, request.TableName);
                    if (resolvedTable == null)
                    {
                        var available = _metadataProvider.GetTableNames(resolvedModule);
                        return $"找不到表 '{request.TableName}'（模块 '{resolvedModule}'）。可用实体：{string.Join(", ", available)}";
                    }

                    var schema = _metadataProvider.GetSchemaByTable(resolvedModule, resolvedTable);
                    if (schema == null)
                    {
                        return $"找不到表: {resolvedModule}.{resolvedTable}";
                    }

                    logger.Append($"获取 {resolvedModule}.{resolvedTable} 的统计信息（原始输入: {request.ModuleName}.{request.TableName}）");

                    var stats = await _databaseExecutor.GetTableStatisticsAsync(resolvedModule, resolvedTable);

                    return JsonSerializer.Serialize(stats, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                    });
                }
                catch (Exception ex)
                {
                    logger.Append($"获取统计信息时出错: {ex.Message}");
                    return $"错误: {ex.Message}";
                }
            });
        }

        /// <summary>
        ///Query database record request
        /// </summary>
        public class QueryRecordsRequest : FunctionAppRequestBase
        {
            [Required(ErrorMessage = "模块名称不能为空")]
            [MaxLength(200)]
            [Description("模块名称||必须。指定要查询的模块，如 'KnowledgeBase'、'AIKernel' 等")]
            public string ModuleName { get; set; }

            [Required(ErrorMessage = "表名称不能为空")]
            [MaxLength(100)]
            [Description("表名称||必须。指定要查询的表名称")]
            public string TableName { get; set; }

            [MaxLength(1000)]
            [Description("过滤条件||可选。SQL WHERE 子句条件，如 'Name = \"test\"' 或 'Id > 10'")]
            public string Filter { get; set; }

            [Range(1, int.MaxValue)]
            [Description("页码||可选。分页查询的页码，默认为 1，最小值为 1")]
            public int PageNumber { get; set; } = 1;

            [Range(1, 1000)]
            [Description("每页数量||可选。分页查询的每页数量，默认为 20，范围 1-1000")]
            public int PageSize { get; set; } = 20;

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                await base.LoadData(serviceProvider);
            }
        }

        /// <summary>
        /// Get statistics request
        /// </summary>
        public class GetStatisticsRequest : FunctionAppRequestBase
        {
            [Required(ErrorMessage = "模块名称不能为空")]
            [MaxLength(200)]
            [Description("模块名称||必须。指定要查询统计信息的模块")]
            public string ModuleName { get; set; }

            [Required(ErrorMessage = "表名称不能为空")]
            [MaxLength(100)]
            [Description("表名称||必须。指定要查询统计信息的表名称")]
            public string TableName { get; set; }

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                await base.LoadData(serviceProvider);
            }
        }
    }
}
