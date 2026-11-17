/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptResult_TextEmbeddingSearchResponse.cs
    File description: PromptResult_TextEmbeddingSearchResponse related implementation
    
    
    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705
    Change description: v0.16.4-preview3 Updated module behavior and related support.*/

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
