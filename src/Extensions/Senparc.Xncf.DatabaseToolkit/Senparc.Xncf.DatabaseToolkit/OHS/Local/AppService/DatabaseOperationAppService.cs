/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseOperationAppService.cs
    File description: DatabaseOperationAppService related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.26.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.27.13 Updated module behavior, localization metadata, and English source comments

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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.QueryRecords.Name", "Function.Database.QueryRecords.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> QueryRecords(QueryRecordsRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    //Legacy comment translated to English.
                    if (string.IsNullOrWhiteSpace(request.ModuleName) || string.IsNullOrWhiteSpace(request.TableName))
                    {
                        return DatabaseToolkitResource.Get("Database.ModuleAndTableRequired");
                    }

                    //Legacy comment translated to English.
                    var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                    if (resolvedModule == null)
                    {
                        var available = _metadataProvider.GetAllModuleNames();
                        return DatabaseToolkitResource.Format("Database.ModuleNotFound", "找不到模块“{0}”。可用模块：{1}", request.ModuleName, string.Join(", ", available));
                    }

                    //Legacy comment translated to English.
                    var resolvedTable = _metadataProvider.ResolveEntityName(resolvedModule, request.TableName);
                    if (resolvedTable == null)
                    {
                        var available = _metadataProvider.GetTableNames(resolvedModule);
                        return DatabaseToolkitResource.Format("Database.TableNotFoundAvailable", "找不到表“{0}”（模块“{1}”）。可用实体：{2}", request.TableName, resolvedModule, string.Join(", ", available));
                    }

                    var schema = _metadataProvider.GetSchemaByTable(resolvedModule, resolvedTable);
                    if (schema == null)
                    {
                        return DatabaseToolkitResource.Format("Database.TableNotFound", "找不到表：{0}.{1}", resolvedModule, resolvedTable);
                    }

                    logger.Append(DatabaseToolkitResource.Format("Database.Query.Log", "查询表 {0}.{1}（原始输入：{2}.{3}）", resolvedModule, resolvedTable, request.ModuleName, request.TableName));

                    //Legacy comment translated to English.
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
                    logger.Append(DatabaseToolkitResource.Format("Database.Query.Error", "查询记录时出错：{0}", ex.Message));
                    return DatabaseToolkitResource.Format("Common.Error", "错误：{0}", ex.Message);
                }
            });
        }

        /// <summary>
        /// getdatabasestatisticsinformation
        ///Legacy comment translated to English.
        /// </summary>
        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.Statistics.Name", "Function.Database.Statistics.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> GetStatistics(GetStatisticsRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.ModuleName) || string.IsNullOrWhiteSpace(request.TableName))
                    {
                        return DatabaseToolkitResource.Get("Database.ModuleAndTableRequired");
                    }

                    //Legacy comment translated to English.
                    var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                    if (resolvedModule == null)
                    {
                        var available = _metadataProvider.GetAllModuleNames();
                        return DatabaseToolkitResource.Format("Database.ModuleNotFound", "找不到模块“{0}”。可用模块：{1}", request.ModuleName, string.Join(", ", available));
                    }

                    //Legacy comment translated to English.
                    var resolvedTable = _metadataProvider.ResolveEntityName(resolvedModule, request.TableName);
                    if (resolvedTable == null)
                    {
                        var available = _metadataProvider.GetTableNames(resolvedModule);
                        return DatabaseToolkitResource.Format("Database.TableNotFoundAvailable", "找不到表“{0}”（模块“{1}”）。可用实体：{2}", request.TableName, resolvedModule, string.Join(", ", available));
                    }

                    var schema = _metadataProvider.GetSchemaByTable(resolvedModule, resolvedTable);
                    if (schema == null)
                    {
                        return DatabaseToolkitResource.Format("Database.TableNotFound", "找不到表：{0}.{1}", resolvedModule, resolvedTable);
                    }

                    logger.Append(DatabaseToolkitResource.Format("Database.Statistics.Log", "获取 {0}.{1} 的统计信息（原始输入：{2}.{3}）", resolvedModule, resolvedTable, request.ModuleName, request.TableName));

                    var stats = await _databaseExecutor.GetTableStatisticsAsync(resolvedModule, resolvedTable);

                    return JsonSerializer.Serialize(stats, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                    });
                }
                catch (Exception ex)
                {
                    logger.Append(DatabaseToolkitResource.Format("Database.Statistics.Error", "获取统计信息时出错：{0}", ex.Message));
                    return DatabaseToolkitResource.Format("Common.Error", "错误：{0}", ex.Message);
                }
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public class QueryRecordsRequest : FunctionAppRequestBase
        {
            [LocalizedRequired(typeof(DatabaseToolkitResource), "Validation.Database.ModuleRequired")]
            [MaxLength(200)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Module.Query")]
            public string ModuleName { get; set; }

            [LocalizedRequired(typeof(DatabaseToolkitResource), "Validation.Database.TableRequired")]
            [MaxLength(100)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Table.Query")]
            public string TableName { get; set; }

            [MaxLength(1000)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Filter")]
            public string Filter { get; set; }

            [Range(1, int.MaxValue)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.PageNumber")]
            public int PageNumber { get; set; } = 1;

            [Range(1, 1000)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.PageSize")]
            public int PageSize { get; set; } = 20;

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                await base.LoadData(serviceProvider);
            }
        }

        /// <summary>
        /// getstatisticsinformationrequest
        /// </summary>
        public class GetStatisticsRequest : FunctionAppRequestBase
        {
            [LocalizedRequired(typeof(DatabaseToolkitResource), "Validation.Database.ModuleRequired")]
            [MaxLength(200)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Module.Statistics")]
            public string ModuleName { get; set; }

            [LocalizedRequired(typeof(DatabaseToolkitResource), "Validation.Database.TableRequired")]
            [MaxLength(100)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Table.Statistics")]
            public string TableName { get; set; }

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                await base.LoadData(serviceProvider);
            }
        }
    }
}
