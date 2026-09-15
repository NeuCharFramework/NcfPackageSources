/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenUsageDto.cs
    文件功能描述：AITokenUsageDto 相关实现
    
    创建标识：Senparc - 20260904

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Models;
using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto
{
    public class AITokenUsageDto : DtoBase
    {
        /// <summary>
        /// 主键 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 模型代号
        /// </summary>
        public string ModelAlias { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// 部署名称
        /// </summary>
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
        /// 调用来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorNote { get; set; }

        public AITokenUsageDto()
        {
        }

        public AITokenUsageDto(AITokenUsage aiTokenUsage)
        {
            Id = aiTokenUsage.Id;
            ModelAlias = aiTokenUsage.ModelAlias;
            ModelId = aiTokenUsage.ModelId;
            DeploymentName = aiTokenUsage.DeploymentName;
            AiPlatform = aiTokenUsage.AiPlatform;
            ConfigModelType = aiTokenUsage.ConfigModelType;
            InputTokens = aiTokenUsage.InputTokens;
            OutputTokens = aiTokenUsage.OutputTokens;
            TotalTokens = aiTokenUsage.TotalTokens;
            CachedInputTokens = aiTokenUsage.CachedInputTokens;
            ReasoningTokens = aiTokenUsage.ReasoningTokens;
            DurationMs = aiTokenUsage.DurationMs;
            Status = aiTokenUsage.Status;
            Source = aiTokenUsage.Source;
            ErrorNote = aiTokenUsage.ErrorNote;

            AddTime = aiTokenUsage.AddTime;
            LastUpdateTime = aiTokenUsage.LastUpdateTime;
            TenantId = aiTokenUsage.TenantId;
            Remark = aiTokenUsage.Remark;
            AdminRemark = aiTokenUsage.AdminRemark;
        }
    }
}
