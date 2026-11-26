/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PageSet.cshtml.cs
    File description: PageSet.cshtml related implementation

    Created by: Senparc - 20240821

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.DynamicData.Areas.Admin.Pages.DynamicData
{
    public class PageSetModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        /// <summary>Initializes the DynamicData page settings page.</summary>
        /// <param name="xncfModuleService">The lazy module service used by the base page model.</param>
        public PageSetModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>Initializes the page settings page request.</summary>
        public void OnGet()
        {
        }
    }
}
