using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Senparc.Xncf.AIKernel.Domain.Models.Extensions
{
    /// <summary>
    /// NeuChar platform corresponds to the model parameters supported by the current user
    /// </summary>
    public class NeuCharModel
    {
        public int Id { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Model weight reference (based on text-davinci-003)
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// Whether to activate
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        ///add time
        /// </summary>
        public DateTime AddTime { get; set; }
    }

    public class NeuCharGetModelJsonResult
    {
        ///// <summary>
        ///// This attribute is newly added locally
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
