/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIModel_Request.cs
    File description: AIModel_Request related implementation

    Created by: Senparc - 20240103

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public string Endpoint { get; set; } = "";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// modeltype
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string OrganizationId { get; set; } = "";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// APIversion
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
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