/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DataSheetSet.cshtml.cs
    File description: DataSheetSet.cshtml related implementation

    Created by: Senparc - 20240821

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.DynamicData.Areas.Admin.Pages.DynamicData
{
    public class DataSheetSetModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public DataSheetSetModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {
        }
    }
}
