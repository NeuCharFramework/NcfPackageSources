/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIAgentIntegrationAppService.cs
    File description: AIAgentIntegrationAppService related implementation

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
    /// Provides AI-facing database query guidance and reusable query templates.
    /// </summary>
    public class AIAgentIntegrationAppService : AppServiceBase
    {
        public AIAgentIntegrationAppService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Returns a structured system prompt describing the DatabaseToolkit functions and safety guidance.
        /// </summary>
        /// <returns>A response containing the serialized system-prompt document.</returns>
        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.AgentPrompt.Name", "Function.Database.AgentPrompt.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> GetSystemPrompt()
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    logger.Append("获取 AI Agent 系统提示");

                    var systemPrompt = new
                    {
                        role = "Database Query Assistant",
                        description = "You are a Database Query Assistant that helps users interact with an NCF-based database system.",
                        responsibilities = new[]
                        {
                            "Understand user requests in natural language",
                            "Query database schemas to understand available data",
                            "Generate appropriate database queries based on user needs",
                            "Retrieve and analyze data from the database",
                            "Present results in a clear and structured format"
                        },
                        availableFunctions = new object[]
                        {
                            new
                            {
                                function = "QueryDatabaseSchema",
                                description = "Query available modules, tables, and field information",
                                usage = "Use this function first to understand the database structure",
                                parameters = new object[]
                                {
                                    new { name = "ModuleName", description = "Optional module name for filtering", type = "string" },
                                    new { name = "TableName", description = "Optional table name for detailed schema", type = "string" }
                                }
                            },
                            new
                            {
                                function = "QueryRecords",
                                description = "Query records from a specific table",
                                usage = "Use this function to fetch data based on user requests",
                                parameters = new object[]
                                {
                                    new { name = "ModuleName", description = "Module name", type = "string", required = true },
                                    new { name = "TableName", description = "Table name", type = "string", required = true },
                                    new { name = "Filter", description = "Optional WHERE clause conditions", type = "string" },
                                    new { name = "PageNumber", description = "Page number for pagination", type = "int", @default = 1 },
                                    new { name = "PageSize", description = "Records per page", type = "int", @default = 20 }
                                }
                            },
                            new
                            {
                                function = "GetStatistics",
                                description = "Get statistics and metadata about a table",
                                usage = "Use this function to understand data volume and structure",
                                parameters = new object[]
                                {
                                    new { name = "ModuleName", description = "Module name", type = "string", required = true },
                                    new { name = "TableName", description = "Table name", type = "string", required = true }
                                }
                            }
                        },
                        bestPractices = new[]
                        {
                            "Always start by querying the database schema to understand available data",
                            "Use appropriate filters to limit result sets and improve performance",
                            "Present data in clear, structured formats",
                            "Explain the relationships between tables when relevant",
                            "Provide context about the data origin and accuracy"
                        },
                        guidelines = new[]
                        {
                            "Be respectful of database performance - use pagination for large datasets",
                            "Only query tables that are necessary for answering the user's question",
                            "If a user request seems to involve sensitive data, ask for confirmation",
                            "Always explain what data you're retrieving and why",
                            "If a query returns no results, try alternative approaches or ask clarifying questions"
                        }
                    };

                    return JsonSerializer.Serialize(systemPrompt, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                    });
                }
                catch (Exception ex)
                {
                    logger.Append($"获取系统提示时出错: {ex.Message}");
                    return $"错误: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Returns reusable examples for common DatabaseToolkit queries and filters.
        /// </summary>
        /// <returns>A response containing serialized query templates.</returns>
        [FunctionRender(typeof(DatabaseToolkitResource), "Function.Database.QueryTemplates.Name", "Function.Database.QueryTemplates.Description", typeof(Register))]
        public async Task<AppResponseBase<string>> GetCommonQueryTemplates()
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                try
                {
                    logger.Append("获取常见查询模板");

                    var templates = new
                    {
                        templates = new object[]
                        {
                            new
                            {
                                name = "ListAllRecords",
                                description = "List all records from a table with pagination",
                                example = new
                                {
                                    module = "KnowledgeBase",
                                    table = "KnowledgeBases",
                                    filter = "",
                                    pageNumber = 1,
                                    pageSize = 20
                                },
                                useCase = "Get an overview of all knowledge bases"
                            },
                            new
                            {
                                name = "FilterByCondition",
                                description = "Find records matching specific conditions",
                                example = new
                                {
                                    module = "KnowledgeBase",
                                    table = "KnowledgeBasesDetail",
                                    filter = "Status = 'Active' AND CreatedTime > '2024-01-01'",
                                    pageNumber = 1,
                                    pageSize = 50
                                },
                                useCase = "Find recent active documents"
                            },
                            new
                            {
                                name = "SearchByName",
                                description = "Search records by name or title",
                                example = new
                                {
                                    module = "KnowledgeBase",
                                    table = "KnowledgeBases",
                                    filter = "Name LIKE '%Python%'",
                                    pageNumber = 1,
                                    pageSize = 20
                                },
                                useCase = "Find knowledge bases related to Python"
                            },
                            new
                            {
                                name = "GetTableStatistics",
                                description = "Get statistics about a table",
                                example = new
                                {
                                    module = "KnowledgeBase",
                                    table = "KnowledgeBasesDetail"
                                },
                                useCase = "Understand data volume and distribution"
                            }
                        },
                        sqlPatterns = new object[]
                        {
                            new
                            {
                                pattern = "Status = 'Active'",
                                description = "Filter by status"
                            },
                            new
                            {
                                pattern = "CreatedTime > '2024-01-01'",
                                description = "Filter by date range"
                            },
                            new
                            {
                                pattern = "Name LIKE '%keyword%'",
                                description = "Text search by name or keyword"
                            },
                            new
                            {
                                pattern = "Id IN (1, 2, 3)",
                                description = "Filter by multiple specific IDs"
                            }
                        }
                    };

                    return JsonSerializer.Serialize(templates, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                    });
                }
                catch (Exception ex)
                {
                    logger.Append($"获取查询模板时出错: {ex.Message}");
                    return $"错误: {ex.Message}";
                }
            });
        }
    }
}
