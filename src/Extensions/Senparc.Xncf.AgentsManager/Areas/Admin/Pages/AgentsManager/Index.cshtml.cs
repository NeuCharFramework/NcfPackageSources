/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml page model implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext add the standardized file header

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.AgentsManager.Areas.AgentsManager.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {
        }
    }
}
