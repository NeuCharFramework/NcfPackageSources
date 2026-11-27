/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIModel_Response.cs
    File description: AIModel_Response related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Collections.Generic;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Models.Extensions;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL;

//public class AIModel_GetIdAndNameResponse
//{
//    public AIModel_GetIdAndNameResponse()
//    {
//    }

//    public AIModel_GetIdAndNameResponse(AIModel aiModel)
//    {
//        Id = aiModel.Id;
//        Alias = aiModel.Alias;
//        Show = aiModel.Show;
//    }

//    public int Id { get; set; }
//    public string Alias { get; set; }
//    public bool Show { get; set; }
//}

public class AIModel_GetDetailResponse
{
    public int Id { get; set; }

    public string Alias { get; set; }

    public string DeploymentName { get; set; }

    public string Endpoint { get; set; }

    public AiPlatform AiPlatform { get; set; }

    public string OrganizationId { get; set; }
    
    public string ApiVersion { get; set; }

    public string Note { get; set; }

    public int MaxToken { get; set; }

    public bool Show { get; set; }

    /// <summary>
    /// Initializes an empty response for model binding.
    /// </summary>
    public AIModel_GetDetailResponse()
    {
    }

    /// <summary>
    /// Initializes the response from an AI model entity.
    /// </summary>
    /// <param name="model">The source AI model.</param>
    public AIModel_GetDetailResponse(AIModel model)
    {
        Id = model.Id;
        Alias = model.Alias;
        DeploymentName = model.DeploymentName;
        Endpoint = model.Endpoint;
        AiPlatform = model.AiPlatform;
        OrganizationId = model.OrganizationId;
        ApiVersion = model.ApiVersion;
        Note = model.Note;
        MaxToken = model.MaxToken;
        Show = model.Show;
    }

    /// <summary>
    /// Initializes the response from an AI model DTO.
    /// </summary>
    /// <param name="dto">The source AI model DTO.</param>
    public AIModel_GetDetailResponse(AIModelDto dto)
    {
        Id = dto.Id;
        Alias = dto.Alias;
        DeploymentName = dto.DeploymentName;
        Endpoint = dto.Endpoint;
        AiPlatform = dto.AiPlatform;
        OrganizationId = dto.OrganizationId;
        ApiVersion = dto.ApiVersion;
        Note = dto.Note;
        MaxToken = dto.MaxToken;
        Show = dto.Show;
    }
}

