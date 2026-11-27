/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileGenerateResult.cs
    File description: FileGenerateResult related implementation

    Created by: Senparc - 20231003

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Plugins
{
    /// <summary>
    /// Represents one generated source file returned by an XNCF Builder plugin.
    /// </summary>
    public class FileGenerateResult
    {
        /// <summary>
        /// Gets or sets the generated file name relative to the target project.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Gets or sets the generated source code or file content.
        /// </summary>
        public string EntityCode { get; set; }
    }
}
