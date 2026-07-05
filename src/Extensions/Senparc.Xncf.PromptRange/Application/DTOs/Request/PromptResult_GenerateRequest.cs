/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_GenerateRequest.cs
    文件功能描述：PromptResult_GenerateRequest 数据传输对象定义
    
    
    创建标识：Senparc - 20260705

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_GenerateRequest
    {
        /// <summary>
        /// 靶道 ID
        /// </summary>
        [Required]
        public int PromptItemId { get; set; }

        /// <summary>
        /// 连发次数
        /// </summary>
        [Required]
        public int NumsOfResults { get; set; } = 1;

        /// <summary>
        /// 对话模式下用户消息（可选）
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        /// 流式会话 ID（可选）
        /// </summary>
        public string StreamId { get; set; }

        /// <summary>
        /// 各模型类型扩展执行参数（可选）
        /// </summary>
        public PromptExecutionOptions ExecutionOptions { get; set; }
    }
}
