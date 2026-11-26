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
        /// Gets or sets the physical root directory containing static resources.
        /// </summary>
        public string RootDir { get; set; }

        /// <summary>
        /// Gets or sets the public request path used to serve static resources.
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// Gets or sets the maximum permitted resource size.
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// Gets or sets the public access URL for the resource root.
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// Gets or sets the file extensions permitted for static resources.
        /// </summary>
        public string[] AllowFile { get; set; }
    }
}
