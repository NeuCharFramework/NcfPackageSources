/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBasesDetailRequest.cs
    File description: KnowledgeBasesDetailRequest related implementation

    Created by: Senparc - 20250717

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///Legacy comment translated to English.
        /// </summary>
        public string KnowledgeBasesId { get; set; }
        /// <summary>
        /// contenttype
        /// </summary>
        public int ContentType { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }
    }
}
