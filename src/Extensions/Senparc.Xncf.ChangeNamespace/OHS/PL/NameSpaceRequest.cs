/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NameSpaceRequest.cs
    File description: NameSpaceRequest related implementation

    Created by: Senparc - 20211016

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 integrate shared resource localization and improve feature text for the ChangeNamespace module

    Modified by: Senparc - 20261119
    Change description: v0.22.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Senparc.Ncf.XncfBase.Functions.Parameters;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.ChangeNamespace.OHS.PL
{
    public class NameSpace_ChangeRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(300)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Path")]
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.NewNamespace")]
        public string NewNamespace { get; set; }

        public string OldNamespaceKeyword = "Senparc.";//Legacy comment translated to English.
    }

    public class NameSpace_DownloadSourceCodeRequest : FunctionAppRequestBase
    {
        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>z
        [Required]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Source")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(SiteOptions))]
        public string Site { get; set; }

        [JsonIgnore]
        public SelectionList SiteOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[]
        {
                new SelectionItem(Parameters_Site.GitHub.ToString(),Parameters_Site.GitHub.ToString()),
                new SelectionItem(Parameters_Site.Gitee.ToString(),Parameters_Site.Gitee.ToString())
            });

        public enum Parameters_Site
        {
            GitHub,
            Gitee
        }
    }


    public class NameSpace_RestoreRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(300)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Path")]
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.CurrentNamespace")]
        public string MyNamespace { get; set; }
    }
}
