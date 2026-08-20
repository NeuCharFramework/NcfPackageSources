/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharModel.cs
    文件功能描述：NeuCharModel 相关实现
    
    
    创建标识：Senparc - 20240827
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260718
    修改描述：同步 NeuChar 算力模型类型

    修改标识：Senparc - 20260722
    修改描述：同步 NeuChar 算力模型 API 版本

    修改标识：Senparc - 20260724
    修改描述：v0.14.0-preview5 同步 NeuChar 模型 API 版本并优化模型信息复制交互

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
        /// 模型类型
        /// </summary>
        public Senparc.AI.ConfigModel ModelType { get; set; }

        /// <summary>
        /// 模型 API 版本
        /// </summary>
        public string ApiVersion { get; set; }

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
