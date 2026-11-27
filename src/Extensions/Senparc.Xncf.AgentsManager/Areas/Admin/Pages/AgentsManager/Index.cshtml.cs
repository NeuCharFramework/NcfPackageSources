/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml page model implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext add the standardized file header

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.AgentsManager.Areas.AgentsManager.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        /// <summary>
        /// Initializes the AgentsManager administration page model.
        /// </summary>
        /// <param name="xncfModuleService">The lazily resolved XNCF module service.</param>
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>
        /// Handles the initial GET request for the AgentsManager administration page.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
