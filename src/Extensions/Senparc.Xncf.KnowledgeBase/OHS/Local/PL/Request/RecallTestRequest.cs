/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: RecallTestRequest.cs
    File description: RecallTestRequest related implementation
    
    
    Created by: Senparc - 20260225
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

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
        public int Id { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int TopK { get; set; } = 5;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string RerankMode { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int CandidateCount { get; set; }
    }
}
