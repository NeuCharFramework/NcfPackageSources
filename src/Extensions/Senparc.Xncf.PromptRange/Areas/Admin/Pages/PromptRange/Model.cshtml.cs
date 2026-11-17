/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Model.cshtml.cs

    File description: Model.cshtml related implementation

    

    

    Created by: Senparc - 20231021

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.PromptRange.Areas.Admin.Pages.PromptRange
{
    public class ModelModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public ModelModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }

        public void OnGet()
        {
        }
    }
}
