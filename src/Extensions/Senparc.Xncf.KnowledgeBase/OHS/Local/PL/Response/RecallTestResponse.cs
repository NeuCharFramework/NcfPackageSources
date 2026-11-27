/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: RecallTestResponse.cs
    File description: Related implementation.

    Created by: Senparc - 20260225

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

﻿/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RecallTestResponse.cs
    File description: Related implementation.

    Created by: Senparc - 20260225

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)



    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Response
{
    /// <summary>
    /// Represents one retrieval candidate returned by a knowledge-base recall test.
    /// </summary>
    public class RecallTestResponse
    {
        /// <summary>Gets or sets the rank after retrieval and optional reranking.</summary>
        public int Rank { get; set; }

        /// <summary>Gets or sets the original vector-search rank before reranking.</summary>
        public int OriginalRank { get; set; }

        /// <summary>Gets or sets the initial retrieval score.</summary>
        public double? Score { get; set; }

        /// <summary>Gets or sets the reranker score, when reranking was enabled.</summary>
        public double? RerankScore { get; set; }

        /// <summary>Gets or sets the reranking mode used for this candidate.</summary>
        public string RerankMode { get; set; }

        /// <summary>Gets or sets the retrieved content.</summary>
        public string Content { get; set; }
        /// <summary>Gets or sets the content length.</summary>
        public int ContentLength { get; set; }
        /// <summary>Gets or sets the formatted retrieval timestamp.</summary>
        public string RecallTime { get; set; }
        /// <summary>Gets or sets retrieval elapsed time in milliseconds.</summary>
        public long ElapsedMilliseconds { get; set; }
        /// <summary>Gets or sets the source display name.</summary>
        public string SourceName { get; set; }
        /// <summary>Gets or sets the source link.</summary>
        public string SourceLink { get; set; }
    }
}
