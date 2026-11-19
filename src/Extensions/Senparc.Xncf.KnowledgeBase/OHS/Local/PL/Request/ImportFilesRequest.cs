/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ImportFilesRequest.cs
    File description: ImportFilesRequest related implementation

    Created by: Senparc - 20251225

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request
{
    public class ImportFilesRequest
    {
        public int knowledgeBaseId { get; set; }
        public List<int> fileIds { get; set; }
    }
}
