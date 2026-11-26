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
        /// <summary>
        /// Initializes the KnowledgeBase recall-test page model.
        /// </summary>
        /// <param name="xncfModuleService">The lazy module service required by the base page.</param>
        public RecallTestModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            CurrentMenu = "RecallTest";
        }

        /// <summary>
        /// Handles the initial recall-test page request; test data is loaded by
        /// client-side calls after the page has rendered.
        /// </summary>
        public Task OnGetAsync()
        {
            return Task.CompletedTask;
        }
    }
}
