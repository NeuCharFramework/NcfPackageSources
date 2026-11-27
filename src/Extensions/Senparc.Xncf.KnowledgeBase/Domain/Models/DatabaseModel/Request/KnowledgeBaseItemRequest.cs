/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseItemRequest.cs
    File description: KnowledgeBaseItemRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request
{
    /// <summary>Represents editable source content for one knowledge-base item.</summary>
    public class KnowledgeBaseItemRequest
    {
        /// <summary>Gets or sets the owning knowledge-base identifier.</summary>
        public int KnowledgeBasesId { get; set; }
        /// <summary>
        /// contenttype
        /// </summary>
        public ContentType ContentType { get; set; }
        /// <summary>Gets or sets the source content.</summary>
        public string Content { get; set; }
    }
}
