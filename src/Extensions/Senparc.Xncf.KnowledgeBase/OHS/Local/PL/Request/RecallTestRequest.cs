/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RecallTestRequest.cs
    File description: RecallTestRequest related implementation

    Created by: Senparc - 20260225

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request
{
    public class RecallTestRequest
    {
        /// <summary>Gets or sets the knowledge-base identifier to query.</summary>
        public int Id { get; set; }
        /// <summary>
        /// The query text used for retrieval.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of candidates returned before reranking.
        /// </summary>
        public int TopK { get; set; } = 5;

        /// <summary>
        /// Gets or sets the reranking mode requested by the caller.
        /// </summary>
        public string RerankMode { get; set; }

        /// <summary>
        /// Gets or sets the candidate pool size used by the retrieval pipeline.
        /// </summary>
        public int CandidateCount { get; set; }
    }
}
