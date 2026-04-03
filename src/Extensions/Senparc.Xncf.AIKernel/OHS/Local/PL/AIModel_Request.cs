using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    public class PagedResponse<T>
    {
        /// <summary>
        ///Total
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// data
        /// </summary>
        public IEnumerable<T> Data { get; set; }

        public PagedResponse(int total, IEnumerable<T> data)
        {
            Total = total;
            Data = data;
        }
    }

    public class PagedRequest
    {
        /// <summary>
        ///page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// size per page
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// sort
        /// </summary>
        public string Order { get; set; } = "Id desc";
    }

    public class AIModel_GetListRequest : PagedRequest
    {
        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        ///deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        ///endpoint
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// AI platform
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        ///Organization ID
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// Whether to display
        /// </summary>
        public bool? Show { get; set; }
    }

    public class AIModel_CreateOrEditRequest
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        ///deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        ///endpoint
        /// </summary>
        public string Endpoint { get; set; } = "";

        /// <summary>
        /// AI platform
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// model type
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }

        /// <summary>
        ///Organization ID
        /// </summary>
        public string OrganizationId { get; set; } = "";

        /// <summary>
        ///API key
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        ///API version
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Remark
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///max tokens
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// Whether to display
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShared { get; set; }
    }

    public class AIModel_EditRequest
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Whether to display
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShared { get; set; }
    }

    public class AIModel_UpdateNeuCharModelsRequest
    {
        [Required]
        public int DeveloperId { get; set; }
        [Required]
        public string ApiKey { get; set; }
    }
}