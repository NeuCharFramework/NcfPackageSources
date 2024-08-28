using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Senparc.Xncf.AIKernel.Domain.Models.Extensions
{
    /// <summary>
    /// NeuChar 平台对应当前用户支持的模型参数
    /// </summary>
    public class NeuCharModel
    {
        public int Id { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 模型权重参考（以text-davinci-003为基准）
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTime AddTime { get; set; }
    }

    public class NeuCharGetModelJsonResult
    {
        ///// <summary>
        ///// 此属性是本地新增
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
