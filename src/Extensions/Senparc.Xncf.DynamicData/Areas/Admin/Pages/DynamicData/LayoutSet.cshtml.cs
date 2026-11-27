/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LayoutSet.cshtml.cs
    File description: LayoutSet.cshtml related implementation

    Created by: Senparc - 20240909

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

    Modified by: Senparc - 20261127
    Change description: v0.7.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.DynamicData.Areas.Admin.Pages.DynamicData
{
    public class LayoutSetModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        /// <summary>Initializes the DynamicData layout settings page.</summary>
        /// <param name="xncfModuleService">The lazy module service used by the base page model.</param>
        public LayoutSetModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>Initializes the layout settings page request.</summary>
        public void OnGet()
        {
        }
    }
}
