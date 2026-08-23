/*-----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplateResponse.cs
    文件功能描述：AgentTemplateResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

    修改标识：Senparc - 20260815
    修改描述：v0.15.0-preview20 增强 AgentTemplate、ChatGroup 与发布型 A2A 的取消和请求处理

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 增强 Agent 工作流校验、函数绑定与任务管理交互

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class AgentTemplate_GetListResponse
    {
        public PagedList<AgentTemplateSimpleStatusDto> List { get; set; }
    }

    public class AgentTemplate_GetItemResponse
    {
        public AgentTemplateDto AgentTemplate { get; set; }
    }

    public class AgentTemplate_GetItemStatusResponse
    {
        public AgentTemplateStatusDto AgentTemplateStatus { get; set; }
    }

    public class KnowledgeBaseOptionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// published：已发布的隔离集合，可安全用于 Agent RAG；
        /// legacy：旧版切片已向量化，但未升级为隔离集合；
        /// pending：尚无可用向量。
        /// </summary>
        public string EmbeddingStatus { get; set; }
        public bool IsEmbedded { get; set; }
    }

    public class AgentFunctionBindingCatalogResponse
    {
        public List<AgentFunctionBindingOptionResponse> Functions { get; set; } = new();
        public List<AgentFunctionBindingOptionResponse> Plugins { get; set; } = new();
        public List<AgentFunctionBindingOptionResponse> Workflows { get; set; } = new();
        public List<AgentFunctionBindingDto> CurrentBindings { get; set; } = new();
    }

    public class AgentFunctionBindingOptionResponse : AgentFunctionBindingDto
    {
        public bool Available { get; set; }
        public string ModuleName { get; set; }
        public string ModuleVersion { get; set; }
        public int ParameterCount { get; set; }
    }
}
