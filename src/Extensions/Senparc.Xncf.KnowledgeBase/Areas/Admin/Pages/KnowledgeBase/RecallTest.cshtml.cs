/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RecallTest.cshtml.cs
    File description: RecallTest.cshtml related implementation

    Created by: Senparc - 20260215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;

namespace Senparc.Xncf.KnowledgeBase.Areas.Admin.Pages.KnowledgeBase
{
    public class RecallTestModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public RecallTestModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            CurrentMenu = "RecallTest";
        }

        public Task OnGetAsync()
        {
            return Task.CompletedTask;
        }
    }
}
