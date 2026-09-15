/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenModelUsage.cs
    文件功能描述：单个模型的 Token 使用聚合（用于模型列表页展示）
    
    创建标识：Senparc - 20260905

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// 单个模型的 Token 使用聚合
    /// </summary>
    [Serializable]
    public class AITokenModelUsage
    {
        /// <summary>
        /// 调用次数
        /// </summary>
        public long Calls { get; set; }

        /// <summary>
        /// 输入（Prompt）Token 合计
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// 输出（Completion）Token 合计
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// 总 Token 合计
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// 最近使用时间
        /// </summary>
        public DateTime? LastUseTime { get; set; }
    }
}
