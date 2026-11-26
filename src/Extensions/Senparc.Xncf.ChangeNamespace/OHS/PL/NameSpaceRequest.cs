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
    /// <summary>Requests a recursive namespace replacement in a source tree.</summary>
    public class NameSpace_ChangeRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(300)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Path")]
        /// <summary>Gets or sets the source directory to scan.</summary>
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.NewNamespace")]
        /// <summary>Gets or sets the replacement namespace.</summary>
        public string NewNamespace { get; set; }

        /// <summary>Gets the namespace prefix targeted by the transformation.</summary>
        public string OldNamespaceKeyword = "Senparc.";
    }

    /// <summary>Requests source-code download from a supported repository.</summary>
    public class NameSpace_DownloadSourceCodeRequest : FunctionAppRequestBase
    {
        /// <summary>
        /// Gets or sets the source repository selected for download.
        /// </summary>
        [Required]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Source")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(SiteOptions))]
        /// <summary>Gets or sets the selected source site.</summary>
        public string Site { get; set; }

        [JsonIgnore]
        /// <summary>Gets the UI options for supported source sites.</summary>
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
        /// <summary>Gets or sets the source directory to restore.</summary>
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.CurrentNamespace")]
        /// <summary>Gets or sets the namespace currently present in the source.</summary>
        public string MyNamespace { get; set; }
    }
}
