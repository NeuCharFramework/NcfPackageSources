/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharModel.cs
    File description: NeuCharModel related implementation

    Created by: Senparc - 20240827

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260718
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260722
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260724
    Change description: v0.14.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Senparc.Xncf.AIKernel.Domain.Models.Extensions
{
    /// <summary>
    /// Represents one model definition returned by the NeuChar model catalog.
    /// </summary>
    public class NeuCharModel
    {
        /// <summary>Gets or sets the remote model identifier.</summary>
        public int Id { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// modeltype
        /// </summary>
        public Senparc.AI.ConfigModel ModelType { get; set; }

        /// <summary>
        /// model API version
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>Gets or sets the model weight used by the remote catalog.</summary>
        public float Weight { get; set; }

        /// <summary>Gets or sets whether the remote model is enabled.</summary>
        public bool Enable { get; set; }

        /// <summary>
        /// creation time
        /// </summary>
        public DateTime AddTime { get; set; }
    }

    /// <summary>
    /// Represents the response envelope returned by the NeuChar model catalog endpoint.
    /// </summary>
    public class NeuCharGetModelJsonResult
    {
        /// <summary>Gets or sets whether the remote request succeeded.</summary>
        public bool Success { get; set; }
        /// <summary>Gets or sets the returned model catalog payload.</summary>
        public AIModel_GetNeuCharModelsResponse_Result Result { get; set; }

        /// <summary>Contains the model entries returned by the catalog.</summary>
        public class AIModel_GetNeuCharModelsResponse_Result
        {
            /// <summary>Gets or sets the returned model definitions.</summary>
            public List<NeuCharModel> Data { get; set; }
        }
    }
}
