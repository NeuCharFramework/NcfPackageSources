/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIModel_Request.cs
    File description: AIModel_Request related implementation

    Created by: Senparc - 20240103

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
        /// Gets or sets the total matching record count.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// data
        /// </summary>
        public IEnumerable<T> Data { get; set; }

        /// <summary>
        /// Initializes a paged response with total count and result data.
        /// </summary>
        /// <param name="total">The total number of matching records.</param>
        /// <param name="data">The records included in the current page.</param>
        public PagedResponse(int total, IEnumerable<T> data)
        {
            Total = total;
            Data = data;
        }
    }

    public class PagedRequest
    {
        /// <summary>
        /// Gets or sets the requested page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the requested page size.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the textual ordering expression.
        /// </summary>
        public string Order { get; set; } = "Id desc";
    }

    public class AIModel_GetListRequest : PagedRequest
    {
        /// <summary>
        /// code
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// Gets or sets the provider endpoint filter.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the AI platform filter.
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// Gets or sets the provider organization filter.
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// whetherdisplay
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
        /// code
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// deployment name
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// Gets or sets the provider endpoint.
        /// </summary>
        public string Endpoint { get; set; } = "";

        /// <summary>
        /// Gets or sets the AI platform provider.
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// modeltype
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }

        /// <summary>
        /// Gets or sets the provider organization identifier.
        /// </summary>
        public string OrganizationId { get; set; } = "";

        /// <summary>
        /// Gets or sets the provider credential.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// APIversion
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets an administrative note.
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the maximum completion-token budget.
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// whetherdisplay
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// whether shared
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
        /// code
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// whetherdisplay
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// whether shared
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
