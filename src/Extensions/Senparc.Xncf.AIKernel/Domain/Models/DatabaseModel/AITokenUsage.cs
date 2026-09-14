/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenUsage.cs
    文件功能描述：AITokenUsage Token 使用记录实体
    
    
    创建标识：Senparc - 20260904

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AIKernel.Models
{
    /// <summary>
    /// Token 使用记录（用于 AIKernel 模块的 Token 监控）
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AITokenUsage))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AITokenUsage : EntityBase<int>
    {
        /// <summary>
        /// 模型代号
        /// </summary>
        [MaxLength(50)]
        public string ModelAlias { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        [MaxLength(100)]
        public string ModelId { get; set; }

        /// <summary>
        /// 部署名称
        /// </summary>
        [MaxLength(150)]
        public string DeploymentName { get; set; }

        /// <summary>
        /// AI 平台
        /// </summary>
        public AiPlatform AiPlatform { get; set; }

        /// <summary>
        /// 模型类别
        /// </summary>
        public ConfigModelType ConfigModelType { get; set; }

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
        /// 本次调用耗时（毫秒）
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// 执行状态：0 成功，1 失败
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 调用来源（如 RunModel、Test、Monitor 等）
        /// </summary>
        [MaxLength(100)]
        public string Source { get; set; }

        /// <summary>
        /// 错误信息（失败时记录）
        /// </summary>
        [MaxLength(500)]
        public string ErrorNote { get; set; }
    }
}
