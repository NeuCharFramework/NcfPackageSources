/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseOperationAppService.cs
    文件功能描述：DatabaseOperationAppService 相关实现
    
    
    创建标识：Senparc - 20260327
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
    /// 数据库操作 AppService
    /// 提供执行数据库查询、更新、删除等操作的 Function
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
        /// 查询数据库记录
        /// 根据指定条件和字段查询表中的数据
        /// </summary>
        [FunctionRender(typeof(NcfBuiltInResource), "Function.Database.QueryRecords.Name", "Function.Database.QueryRecords.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> QueryRecords(QueryRecordsRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    // 验证模块和表
                    if (string.IsNullOrWhiteSpace(request.ModuleName) || string.IsNullOrWhiteSpace(request.TableName))
                    {
                        return NcfBuiltInResource.Get("Database.ModuleAndTableRequired");
                    }

                    // 模糊解析模块名
                    var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                    if (resolvedModule == null)
                    {
                        var available = _metadataProvider.GetAllModuleNames();
                        return NcfBuiltInResource.Format("Database.ModuleNotFound", "找不到模块“{0}”。可用模块：{1}", request.ModuleName, string.Join(", ", available));
                    }

                    // 模糊解析实体名
                    var resolvedTable = _metadataProvider.ResolveEntityName(resolvedModule, request.TableName);
                    if (resolvedTable == null)
                    {
                        var available = _metadataProvider.GetTableNames(resolvedModule);
                        return NcfBuiltInResource.Format("Database.TableNotFoundAvailable", "找不到表“{0}”（模块“{1}”）。可用实体：{2}", request.TableName, resolvedModule, string.Join(", ", available));
                    }

                    var schema = _metadataProvider.GetSchemaByTable(resolvedModule, resolvedTable);
                    if (schema == null)
                    {
                        return NcfBuiltInResource.Format("Database.TableNotFound", "找不到表：{0}.{1}", resolvedModule, resolvedTable);
                    }

                    logger.Append(NcfBuiltInResource.Format("Database.Query.Log", "查询表 {0}.{1}（原始输入：{2}.{3}）", resolvedModule, resolvedTable, request.ModuleName, request.TableName));

                    // 执行查询
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
                    logger.Append(NcfBuiltInResource.Format("Database.Query.Error", "查询记录时出错：{0}", ex.Message));
                    return NcfBuiltInResource.Format("Common.Error", "错误：{0}", ex.Message);
                }
            });
        }

        /// <summary>
        /// 获取数据库统计信息
        /// 获取指定表的行数、最小/最大值等统计信息
        /// </summary>
        [FunctionRender(typeof(NcfBuiltInResource), "Function.Database.Statistics.Name", "Function.Database.Statistics.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> GetStatistics(GetStatisticsRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.ModuleName) || string.IsNullOrWhiteSpace(request.TableName))
                    {
                        return NcfBuiltInResource.Get("Database.ModuleAndTableRequired");
                    }

                    // 模糊解析模块名
                    var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                    if (resolvedModule == null)
                    {
                        var available = _metadataProvider.GetAllModuleNames();
                        return NcfBuiltInResource.Format("Database.ModuleNotFound", "找不到模块“{0}”。可用模块：{1}", request.ModuleName, string.Join(", ", available));
                    }

                    // 模糊解析实体名
                    var resolvedTable = _metadataProvider.ResolveEntityName(resolvedModule, request.TableName);
                    if (resolvedTable == null)
                    {
                        var available = _metadataProvider.GetTableNames(resolvedModule);
                        return NcfBuiltInResource.Format("Database.TableNotFoundAvailable", "找不到表“{0}”（模块“{1}”）。可用实体：{2}", request.TableName, resolvedModule, string.Join(", ", available));
                    }

                    var schema = _metadataProvider.GetSchemaByTable(resolvedModule, resolvedTable);
                    if (schema == null)
                    {
                        return NcfBuiltInResource.Format("Database.TableNotFound", "找不到表：{0}.{1}", resolvedModule, resolvedTable);
                    }

                    logger.Append(NcfBuiltInResource.Format("Database.Statistics.Log", "获取 {0}.{1} 的统计信息（原始输入：{2}.{3}）", resolvedModule, resolvedTable, request.ModuleName, request.TableName));

                    var stats = await _databaseExecutor.GetTableStatisticsAsync(resolvedModule, resolvedTable);

                    return JsonSerializer.Serialize(stats, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                    });
                }
                catch (Exception ex)
                {
                    logger.Append(NcfBuiltInResource.Format("Database.Statistics.Error", "获取统计信息时出错：{0}", ex.Message));
                    return NcfBuiltInResource.Format("Common.Error", "错误：{0}", ex.Message);
                }
            });
        }

        /// <summary>
        /// 查询数据库记录请求
        /// </summary>
        public class QueryRecordsRequest : FunctionAppRequestBase
        {
            [LocalizedRequired(typeof(NcfBuiltInResource), "Validation.Database.ModuleRequired")]
            [MaxLength(200)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.Module.Query")]
            public string ModuleName { get; set; }

            [LocalizedRequired(typeof(NcfBuiltInResource), "Validation.Database.TableRequired")]
            [MaxLength(100)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.Table.Query")]
            public string TableName { get; set; }

            [MaxLength(1000)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.Filter")]
            public string Filter { get; set; }

            [Range(1, int.MaxValue)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.PageNumber")]
            public int PageNumber { get; set; } = 1;

            [Range(1, 1000)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.PageSize")]
            public int PageSize { get; set; } = 20;

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                await base.LoadData(serviceProvider);
            }
        }

        /// <summary>
        /// 获取统计信息请求
        /// </summary>
        public class GetStatisticsRequest : FunctionAppRequestBase
        {
            [LocalizedRequired(typeof(NcfBuiltInResource), "Validation.Database.ModuleRequired")]
            [MaxLength(200)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.Module.Statistics")]
            public string ModuleName { get; set; }

            [LocalizedRequired(typeof(NcfBuiltInResource), "Validation.Database.TableRequired")]
            [MaxLength(100)]
            [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.Database.Table.Statistics")]
            public string TableName { get; set; }

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                await base.LoadData(serviceProvider);
            }
        }
    }
}
