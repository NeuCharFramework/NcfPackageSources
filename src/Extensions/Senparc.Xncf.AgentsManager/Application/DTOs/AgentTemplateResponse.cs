/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentTemplateResponse.cs
    File description: AgentTemplateResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.14.0-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.15.0-preview20 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Application.Dtos
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
        /// Gets the current embedding status displayed for the knowledge-base option.
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
