/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20250325

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.MCP.Areas.MCP.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        /// <summary>
        /// Initializes the MCP administration page model.
        /// </summary>
        /// <param name="xncfModuleService">The lazy module service required by the base page.</param>
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>
        /// Handles the initial MCP administration page request.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
