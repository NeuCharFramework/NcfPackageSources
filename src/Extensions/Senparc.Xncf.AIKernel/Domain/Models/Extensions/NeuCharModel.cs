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
    ///Legacy comment translated to English.
    /// </summary>
    public class NeuCharModel
    {
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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// creation time
        /// </summary>
        public DateTime AddTime { get; set; }
    }

    public class NeuCharGetModelJsonResult
    {
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //public string Message { get; set; }
        public bool Success { get; set; }
        public AIModel_GetNeuCharModelsResponse_Result Result { get; set; }

        public class AIModel_GetNeuCharModelsResponse_Result
        {
            public List<NeuCharModel> Data { get; set; }
        }
    }
}
