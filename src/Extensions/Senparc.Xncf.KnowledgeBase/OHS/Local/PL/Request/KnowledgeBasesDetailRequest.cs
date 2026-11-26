/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBasesDetailRequest.cs
    File description: KnowledgeBasesDetailRequest related implementation

    Created by: Senparc - 20250717

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
    public class KnowledgeBasesDetailRequest
    {
        /// <summary>
        /// Gets or sets the knowledge-base identifier represented as request text.
        /// </summary>
        public string KnowledgeBasesId { get; set; }
        /// <summary>
        /// Gets or sets the content-type discriminator.
        /// </summary>
        public int ContentType { get; set; }
        /// <summary>
        /// Gets or sets the content or chunk text.
        /// </summary>
        public string Content { get; set; }
    }
}
