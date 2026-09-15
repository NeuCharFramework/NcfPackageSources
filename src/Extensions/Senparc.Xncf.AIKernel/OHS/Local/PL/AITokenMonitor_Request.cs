/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenMonitor_Request.cs
    文件功能描述：AITokenMonitor 相关请求/响应对象
    
    创建标识：Senparc - 20260904

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using System;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL
{
    /// <summary>
    /// 分页获取 Token 使用记录
    /// </summary>
    public class AITokenUsage_GetListRequest : PagedRequest
    {
        /// <summary>
        /// 模型代号
        /// </summary>
        public string ModelAlias { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// AI 平台
        /// </summary>
        public AiPlatform? AiPlatform { get; set; }

        /// <summary>
        /// 模型类别
        /// </summary>
        public ConfigModelType? ConfigModelType { get; set; }

        /// <summary>
        /// 调用来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 执行状态：0 成功，1 失败
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// 起始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// 运行模型并监控 Token 消耗
    /// </summary>
    public class AITokenMonitor_RunModelRequest
    {
        /// <summary>
        /// 要运行的 AIModel 数据库主键 ID
        /// </summary>
        public int ModelId { get; set; }

        /// <summary>
        /// 用户提示词
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// 系统消息（可选）
        /// </summary>
        public string SystemMessage { get; set; }

        /// <summary>
        /// 调用来源标记（可选，默认 Monitor）
        /// </summary>
        public string Source { get; set; } = "Monitor";

        /// <summary>
        /// 最大输出 Token（可选）
        /// </summary>
        public int MaxTokens { get; set; }

        /// <summary>
        /// 温度（可选，默认 0.7）
        /// </summary>
        public float? Temperature { get; set; }
    }

    /// <summary>
    /// 查询某次运行的进度
    /// </summary>
    public class AITokenMonitor_GetProgressRequest
    {
        /// <summary>
        /// 运行唯一标识
        /// </summary>
        public Guid RunId { get; set; }
    }

    /// <summary>
    /// 运行并监控模型的返回
    /// </summary>
    public class AITokenMonitor_RunModelResponse
    {
        /// <summary>
        /// 运行唯一标识
        /// </summary>
        public Guid RunId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 输出文本
        /// </summary>
        public string Output { get; set; }

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
        /// 推理 Token 数量
        /// </summary>
        public long ReasoningTokens { get; set; }

        /// <summary>
        /// 本次调用耗时（毫秒）
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; }
    }
}
