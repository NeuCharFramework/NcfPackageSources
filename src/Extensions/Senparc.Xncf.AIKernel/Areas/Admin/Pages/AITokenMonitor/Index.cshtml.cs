/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

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

        public Index(Lazy<XncfModuleService> xncfModuleService, AITokenUsageService aITokenUsage) : base(xncfModuleService)
        {
            this.aITokenUsage = aITokenUsage;
        }

        public void OnGet()
        {
        }
    }
}
