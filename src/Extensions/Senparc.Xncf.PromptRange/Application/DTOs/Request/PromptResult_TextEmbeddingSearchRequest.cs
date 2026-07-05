/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_TextEmbeddingSearchRequest.cs
    文件功能描述：PromptResult_TextEmbeddingSearchRequest 数据传输对象定义
    
    
    创建标识：Senparc - 20260705

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_TextEmbeddingSearchRequest
    {
        /// <summary>
        /// PromptResult ID（必须为 TextEmbedding 类型结果）
        /// </summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>
        /// 查询文本
        /// </summary>
        [Required]
        public string Query { get; set; }

        /// <summary>
        /// TopK（默认 3，范围 1-20）
        /// </summary>
        public int TopK { get; set; } = 3;
    }
}
