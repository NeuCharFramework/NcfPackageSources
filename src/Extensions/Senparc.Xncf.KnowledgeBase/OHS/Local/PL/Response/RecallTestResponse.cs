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

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Response
{
    public class RecallTestResponse
    {
        ///Legacy comment translated to English.
        public int Rank { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int OriginalRank { get; set; }

        ///Legacy comment translated to English.
        public double? Score { get; set; }

        ///Legacy comment translated to English.
        public double? RerankScore { get; set; }

        ///Legacy comment translated to English.
        public string RerankMode { get; set; }

        public string Content { get; set; }
        public int ContentLength { get; set; }
        public string RecallTime { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string SourceName { get; set; }
        public string SourceLink { get; set; }
    }
}
