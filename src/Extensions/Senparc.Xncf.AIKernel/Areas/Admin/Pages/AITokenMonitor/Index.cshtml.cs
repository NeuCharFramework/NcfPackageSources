/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Areas.AITokenMonitor.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly AITokenUsageService aITokenUsage;

        /// <summary>
        /// Initializes the AI token monitor page.
        /// </summary>
        /// <param name="xncfModuleService">The lazy XNCF module service.</param>
        /// <param name="aITokenUsage">The token usage service.</param>
        public Index(Lazy<XncfModuleService> xncfModuleService, AITokenUsageService aITokenUsage) : base(xncfModuleService)
        {
            this.aITokenUsage = aITokenUsage;
        }

        /// <summary>
        /// Initializes the token monitor page request.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
