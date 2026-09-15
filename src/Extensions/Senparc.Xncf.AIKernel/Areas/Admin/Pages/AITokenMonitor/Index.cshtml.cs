/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Token 监控页面
    
    创建标识：Senparc - 20260904

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

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
