/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: StaticResourceSetting.cs
    File description: StaticResourceSetting related implementation

    Created by: Senparc - 20260213

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

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Config
{
    public class StaticResourceSetting
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string RootDir { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string[] AllowFile { get; set; }
    }
}
