/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileResponse.cs
    File description: FileResponse related implementation

    Created by: Senparc - 20221027

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Ncf.Core.Models.AppServices
{
    public class FileResponse
    {
        public Stream Stream { get; set; }
        public string ContentType { get; set; }
    }
}
