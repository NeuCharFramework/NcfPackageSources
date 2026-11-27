/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIVector_Request.cs
    File description: AIVector_Request related implementation

    Created by: Senparc - 20250402

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.AI.Interfaces;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    public class AIVector_GetListRequest : PagedRequest
    {
        /// <summary>
        /// code
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the vector database provider type used for filtering.
        /// </summary>
        public VectorDBType VectorDBType { get; set; }

        /// <summary>
        /// whetherdisplay
        /// </summary>
        public bool? Show { get; set; }
    }

    public class AIVector_CreateOrEditRequest
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
        /// model name (required)
        /// </summary>
        public string VectorId { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the provider connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the vector database provider type.
        /// </summary>
        public VectorDBType VectorDBType { get; set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// whether shared
        /// </summary>
        public bool IsShared { get; set; }


        /// <summary>
        /// whether displayed
        /// </summary>
        public bool Show { get; set; }
    }

    public class AIVector_EditRequest
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
}
