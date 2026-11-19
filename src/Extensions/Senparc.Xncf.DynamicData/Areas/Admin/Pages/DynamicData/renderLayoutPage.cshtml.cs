/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: renderLayoutPage.cshtml.cs
    File description: renderLayoutPage.cshtml related implementation

    Created by: Senparc - 20240924

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
    public class renderLayoutPageModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public renderLayoutPageModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {
        }
    }
}
