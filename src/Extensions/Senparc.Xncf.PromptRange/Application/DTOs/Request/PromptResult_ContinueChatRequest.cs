/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_ContinueChatRequest.cs
    文件功能描述：PromptResult_ContinueChatRequest 数据传输对象定义
    
    
    创建标识：Senparc - 20251213
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>
    /// 继续聊天请求
    /// </summary>
    public class PromptResult_ContinueChatRequest
    {
        /// <summary>
        /// PromptResult 的 ID
        /// </summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>
        /// 用户消息
        /// </summary>
        [Required]
        public string UserMessage { get; set; }

        /// <summary>
        /// 流式输出会话 ID（可选）
        /// </summary>
        public string StreamId { get; set; }
    }
}
