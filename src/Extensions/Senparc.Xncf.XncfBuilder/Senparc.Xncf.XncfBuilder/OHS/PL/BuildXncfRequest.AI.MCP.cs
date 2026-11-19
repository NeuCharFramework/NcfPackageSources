/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BuildXncfRequest.AI.MCP.cs
    File description: BuildXncfRequest.AI.MCP related implementation

    Created by: Senparc - 20250524

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.AI.Exceptions;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class BuildXncf_GetFileResponse
    {
        public bool Success{get; set;}
        public string Message { get; set; }
        public string FileName{get; set;}
        public string FileContent{get; set;}
        public string FilePath{get; set;}
        public string Sha256 { get; set; }
    }

    public class BuildXncf_CreateOrUpdateFileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public bool IsNewFile { get; set; }
        public string PreviousSha256 { get; set; }
        public string Sha256 { get; set; }
    }

}
