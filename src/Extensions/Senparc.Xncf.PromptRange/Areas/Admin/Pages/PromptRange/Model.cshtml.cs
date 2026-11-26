/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Model.cshtml.cs
    File description: Model.cshtml related implementation

    Created by: Senparc - 20231021

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.PromptRange.Areas.Admin.Pages.PromptRange
{
    public class ModelModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        /// <summary>Initializes the PromptRange model page.</summary>
        /// <param name="xncfModuleService">The lazy XNCF module service.</param>
        public ModelModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }

        /// <summary>Initializes the model page request.</summary>
        public void OnGet()
        {
        }
    }
}
