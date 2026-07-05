/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_TextEmbeddingSearchResponse.cs
    文件功能描述：PromptResult_TextEmbeddingSearchResponse 相关实现
    
    
    创建标识：Senparc - 20260705

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptResult_TextEmbeddingSearchResponse
    {
        public int PromptResultId { get; set; }

        public int VectorDbId { get; set; }

        public string VectorDbAlias { get; set; }

        public string CollectionName { get; set; }

        public string Query { get; set; }

        public int TopK { get; set; }

        public List<PromptResult_TextEmbeddingSearchHitResponse> Hits { get; set; } = new();
    }

    public class PromptResult_TextEmbeddingSearchHitResponse
    {
        public string SourceId { get; set; }

        public string SourceName { get; set; }

        public string SourceLink { get; set; }

        public string Text { get; set; }

        public float Score { get; set; }
    }
}
