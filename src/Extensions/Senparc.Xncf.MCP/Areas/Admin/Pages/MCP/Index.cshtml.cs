/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20250325

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.MCP.Areas.MCP.Pages
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
