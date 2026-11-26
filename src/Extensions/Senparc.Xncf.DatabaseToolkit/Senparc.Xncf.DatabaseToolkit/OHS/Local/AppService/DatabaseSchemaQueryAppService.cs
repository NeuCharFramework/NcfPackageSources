/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseSchemaQueryAppService.cs
    File description: DatabaseSchemaQueryAppService related implementation

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
    /// Provides FunctionRender access to discovered module, table, and column metadata.
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
        /// Returns schema metadata for all modules, one module, or one table.
        /// </summary>
        /// <param name="request">Optional module and table filters.</param>
        /// <returns>A response containing serialized schema metadata or a localized error.</returns>
        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.Schema.Name", "Function.Database.Schema.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> QueryDatabaseSchema(QuerySchemaRequest request)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(request.ModuleName) && !string.IsNullOrWhiteSpace(request.TableName))
                    {
                        logger.Append(DatabaseToolkitResource.Format("Database.Schema.TableDetailLog", "获取表详情：{0}.{1}", request.ModuleName, request.TableName));

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
        /// Carries optional module and table filters for a schema query.
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
                await base.LoadData(serviceProvider);
            }
        }
    }
}
