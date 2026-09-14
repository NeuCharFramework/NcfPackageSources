/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenProgressEvent.cs
    文件功能描述：Token 监控的异步进度事件
    
    创建标识：Senparc - 20260904

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// Token 监控进度状态
    /// </summary>
    public enum AITokenProgressStatus
    {
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 0,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 1,
        /// <summary>
        /// 失败
        /// </summary>
        Failed = 2,
    }

    /// <summary>
    /// 一次模型调用运行过程中的异步进度事件
    /// <para>
    /// 由 <see cref="Services.AITokenMonitorService"/> 发布，可通过
    /// <see cref="Services.AITokenMonitorService.SubscribeAsync(Guid, System.Threading.CancellationToken)"/> 以
    /// IAsyncEnumerable 形式订阅，实现"异步进度"。
    /// </para>
    /// </summary>
    [Serializable]
    public class AITokenProgressEvent
    {
        /// <summary>
        /// 本次运行唯一标识
        /// </summary>
        public Guid RunId { get; set; }

        /// <summary>
        /// 模型代号
        /// </summary>
        public string ModelAlias { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// 当前进度状态
        /// </summary>
        public AITokenProgressStatus Status { get; set; } = AITokenProgressStatus.Running;

        /// <summary>
        /// 输入（Prompt）Token 数量
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// 已生成（输出）Token 数量
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// 总 Token 数量
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// 已生成文本的预览（截断）
        /// </summary>
        public string OutputPreview { get; set; }

        /// <summary>
        /// 已耗时（毫秒）
        /// </summary>
        public int ElapsedMs { get; set; }

        /// <summary>
        /// 是否已结束（Completed / Failed）
        /// </summary>
        public bool IsComplete => Status == AITokenProgressStatus.Completed || Status == AITokenProgressStatus.Failed;

        /// <summary>
        /// 错误信息（失败时）
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 事件时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
