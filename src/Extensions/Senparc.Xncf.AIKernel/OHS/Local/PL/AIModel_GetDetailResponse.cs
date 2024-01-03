using Senparc.AI;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL;

public class AIModel_GetIdAndNameResponse
{
    public AIModel_GetIdAndNameResponse()
    {
    }

    public AIModel_GetIdAndNameResponse(AIModel aiModel)
    {
        Id = aiModel.Id;
        Alias = aiModel.Alias;
        Show = aiModel.Show;
    }

    public int Id { get; set; }
    public string Alias { get; set; }
    public bool Show { get; set; }
}

// public class AIModel_GetDetailResponse
// {
//     public int Id { get; set; }
//
//     public string Alias { get; set; }
//
//     public string DeploymentName { get; set; }
//
//     public string Endpoint { get; set; }
//
//     public AiPlatform AiPlatform { get; set; }
//
//     public string OrganizationId { get; set; }
//     
//     public string ApiVersion { get; set; }
//
//     public string Note { get; set; }
//
//     public int MaxToken { get; set; }
//
//     public bool Show { get; set; }
//
//     public AIModel_GetDetailResponse()
//     {
//     }
// }