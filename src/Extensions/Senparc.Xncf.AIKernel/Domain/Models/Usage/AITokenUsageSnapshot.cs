/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenUsageSnapshot.cs
    文件功能描述：Token 使用快照（轻量值对象）
    
    创建标识：Senparc - 20260904

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// 一次 AI 调用的 Token 使用快照
    /// </summary>
    [Serializable]
    public class AITokenUsageSnapshot
    {
        /// <summary>
        /// 输入（Prompt）Token 数量
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// 输出（Completion）Token 数量
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// 总 Token 数量
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// 缓存命中输入 Token 数量
        /// </summary>
        public long CachedInputTokens { get; set; }

        /// <summary>
        /// 推理（Reasoning / Thinking）Token 数量
        /// </summary>
        public long ReasoningTokens { get; set; }

        /// <summary>
        /// 空快照
        /// </summary>
        public static AITokenUsageSnapshot Empty => new AITokenUsageSnapshot();

        /// <summary>
        /// 是否为空（无任何 Token 数据）
        /// </summary>
        public bool IsEmpty =>
            InputTokens == 0 && OutputTokens == 0 && TotalTokens == 0
            && CachedInputTokens == 0 && ReasoningTokens == 0;

        /// <summary>
        /// 确保 TotalTokens 有效（<=0 时用输入+输出补齐）
        /// </summary>
        public void Normalize()
        {
            InputTokens = Math.Max(0, InputTokens);
            OutputTokens = Math.Max(0, OutputTokens);
            CachedInputTokens = Math.Max(0, CachedInputTokens);
            ReasoningTokens = Math.Max(0, ReasoningTokens);
            if (TotalTokens <= 0)
            {
                TotalTokens = InputTokens + OutputTokens;
            }
        }
    }
}
