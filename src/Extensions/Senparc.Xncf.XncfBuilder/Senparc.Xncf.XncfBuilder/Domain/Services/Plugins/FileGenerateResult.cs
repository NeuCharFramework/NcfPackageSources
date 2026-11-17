/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: FileGenerateResult.cs

    File description: FileGenerateResult related implementation

    

    

    Created by: Senparc - 20231003

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Plugins
{
    /// <summary>
    /// filegenerateresult
    /// </summary>
    public class FileGenerateResult
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// codeorfilecontent
        /// </summary>
        public string EntityCode { get; set; }
    }
}
