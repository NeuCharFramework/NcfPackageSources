/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseSchemaQueryAppService.cs
    文件功能描述：DatabaseSchemaQueryAppService 相关实现
    
    
    创建标识：Senparc - 20260327
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.26.0-preview2 为 DatabaseToolkit 模块接入统一资源本地化并优化功能文案

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
    /// 数据库架构元数据查询 AppService
    /// 提供查询数据库结构信息的 Function
    /// </summary>
    public class DatabaseSchemaQueryAppService : AppServiceBase
    {
        private readonly DatabaseSchemaMetadataProvider _metadataProvider;

        public DatabaseSchemaQueryAppService(IServiceProvider serviceProvider, DatabaseSchemaMetadataProvider metadataProvider)
            : base(serviceProvider)
        {
            _metadataProvider = metadataProvider;
        }

        /// <summary>
        /// 查询可用的模块和表结构
        /// 返回所有可查询的模块及其表的结构信息
        /// </summary>
        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.Schema.Name", "Function.Database.Schema.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> QueryDatabaseSchema(QuerySchemaRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    // 如果指定了模块名和表名，获取详细信息
                    if (!string.IsNullOrWhiteSpace(request.ModuleName) && !string.IsNullOrWhiteSpace(request.TableName))
                    {
                        logger.Append(DatabaseToolkitResource.Format("Database.Schema.TableDetailLog", "获取表详情：{0}.{1}", request.ModuleName, request.TableName));

                        // 先尝试精确匹配，再尝试模糊匹配
                        var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                        if (resolvedModule == null)
                        {
                            var allModules = _metadataProvider.GetAllSchemas().Keys.OrderBy(k => k).ToList();
                            var suggestions = allModules.Where(m =>
                                m.Contains(request.ModuleName, StringComparison.OrdinalIgnoreCase)).ToList();
                            var hint = suggestions.Count > 0
                                ? DatabaseToolkitResource.Format("Database.Schema.SuggestedModules", "\n可能匹配的模块：{0}", string.Join(", ", suggestions))
                                : DatabaseToolkitResource.Format("Database.Schema.AllModules", "\n所有可用模块：{0}", string.Join(", ", allModules));
                            return DatabaseToolkitResource.Format("Database.Schema.ModuleNotFoundWithHint", "找不到模块：“{0}”{1}", request.ModuleName, hint);
                        }

                        var schema = _metadataProvider.GetSchemaByTable(resolvedModule, request.TableName);
                        if (schema == null)
                        {
                            var availableTables = _metadataProvider.GetSchemasByModule(resolvedModule)
                                .Select(s => s.TableName).OrderBy(t => t).ToList();
                            var tableList = availableTables.Count > 0
                                ? string.Join(", ", availableTables)
                                : DatabaseToolkitResource.Get("Database.Schema.NoAvailableTables");
                            return DatabaseToolkitResource.Format("Database.Schema.EntityNotFound", "在模块“{0}”中找不到实体“{1}”。\n可用实体：{2}", resolvedModule, request.TableName, tableList);
                        }

                        var result = new
                        {
                            module = schema.ModuleName,
                            table = schema.TableName,
                            entityType = schema.EntityFullName,
                            displayName = schema.DisplayName,
                            description = schema.Description,
                            columns = schema.Columns.Select(c => new
                            {
                                name = c.ColumnName,
                                type = c.ColumnType,
                                displayName = c.DisplayName,
                                description = c.Description,
                                isPrimaryKey = c.IsPrimaryKey,
                                isRequired = c.IsRequired,
                                maxLength = c.MaxLength,
                                isFilterable = c.IsFilterable
                            })
                        };

                        return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                        });
                    }

                    // 如果只指定了模块名，获取该模块的所有表
                    if (!string.IsNullOrWhiteSpace(request.ModuleName))
                    {
                        logger.Append(DatabaseToolkitResource.Format("Database.Schema.ModuleTablesLog", "获取模块 {0} 的所有表", request.ModuleName));

                        var resolvedModule = _metadataProvider.ResolveModuleName(request.ModuleName);
                        if (resolvedModule == null)
                        {
                            var allModules = _metadataProvider.GetAllSchemas().Keys.OrderBy(k => k).ToList();
                            var suggestions = allModules.Where(m =>
                                m.Contains(request.ModuleName, StringComparison.OrdinalIgnoreCase)).ToList();
                            var hint = suggestions.Count > 0
                                ? DatabaseToolkitResource.Format("Database.Schema.SuggestedModules", "\n可能匹配的模块：{0}", string.Join(", ", suggestions))
                                : DatabaseToolkitResource.Format("Database.Schema.AllModules", "\n所有可用模块：{0}", string.Join(", ", allModules));
                            return DatabaseToolkitResource.Format("Database.Schema.ModuleNotFoundWithHint", "找不到模块：“{0}”{1}", request.ModuleName, hint);
                        }

                        var schemas = _metadataProvider.GetSchemasByModule(resolvedModule);
                        if (schemas.Count == 0)
                        {
                            return DatabaseToolkitResource.Format("Database.Schema.ModuleHasNoTables", "模块“{0}”中没有可查询的表", resolvedModule);
                        }

                        var result = new
                        {
                            module = request.ModuleName,
                            tableCount = schemas.Count,
                            tables = schemas.Select(s => new
                            {
                                name = s.TableName,
                                displayName = s.DisplayName,
                                description = s.Description,
                                columnCount = s.Columns.Count,
                                columns = s.Columns.Select(c => new { name = c.ColumnName, type = c.ColumnType })
                            })
                        };

                        return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                        });
                    }

                    // 获取所有模块和表的概览
                    logger.Append(DatabaseToolkitResource.Get("Database.Schema.OverviewLog"));
                    
                    var allSchemas = _metadataProvider.GetAllSchemas();
                    
                    if (allSchemas.Count == 0)
                    {
                        return DatabaseToolkitResource.Get("Database.Schema.NoModules");
                    }

                    var overview = new
                    {
                        totalModules = allSchemas.Count,
                        totalTables = allSchemas.Values.Sum(x => x.Count),
                        modules = allSchemas.Select(kvp => new
                        {
                            name = kvp.Key,
                            tableCount = kvp.Value.Count,
                            tables = kvp.Value.Select(s => new
                            {
                                name = s.TableName,
                                displayName = s.DisplayName,
                                description = s.Description
                            })
                        })
                    };

                    return JsonSerializer.Serialize(overview, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                    });
                }
                catch (Exception ex)
                {
                    logger.Append(DatabaseToolkitResource.Format("Database.Schema.Error", "查询数据库结构时出错：{0}", ex.Message));
                    return DatabaseToolkitResource.Format("Common.Error", "错误：{0}", ex.Message);
                }
            });
        }

        /// <summary>
        /// 查询数据库架构请求
        /// </summary>
        public class QuerySchemaRequest : FunctionAppRequestBase
        {
            [MaxLength(200)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Schema.Module")]
            public string ModuleName { get; set; }

            [MaxLength(100)]
            [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.Database.Schema.Table")]
            public string TableName { get; set; }

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                // 可选：在此处加载默认数据
                await base.LoadData(serviceProvider);
            }
        }
    }
}
