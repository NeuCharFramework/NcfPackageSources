/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseItemRequest.cs
    File description: KnowledgeBaseItemRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
    public class KnowledgeBaseItemRequest
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int KnowledgeBasesId { get; set; }
        /// <summary>
        /// contenttype
        /// </summary>
        public ContentType ContentType { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
    }
}
