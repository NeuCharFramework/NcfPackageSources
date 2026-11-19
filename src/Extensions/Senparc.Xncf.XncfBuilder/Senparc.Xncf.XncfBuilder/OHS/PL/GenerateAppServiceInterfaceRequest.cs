/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: GenerateAppServiceInterfaceRequest.cs
    File description: GenerateAppServiceInterfaceRequest related implementation

    Created by: Senparc - 20220205

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class GenerateAppServiceInterface_GenerateRequest : FunctionAppRequestBase
    {
        [Required]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.TargetPath")]
        public string TargetProjectPath { get; set; }

        [Required]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.Namespace")]
        public string NamespacePrefix { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.ClassPattern")]
        public string ClassNamePattern { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.MethodPattern")]
        public string MethodNamePattern { get; set; }

        [Required]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.DestinationPath")]
        public string DestinationProjectPath { get; set; }

     
    }
}
