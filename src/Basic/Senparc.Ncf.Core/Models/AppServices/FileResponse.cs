/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: FileResponse.cs

    File description: FileResponse related implementation

    

    

    Created by: Senparc - 20221027

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
