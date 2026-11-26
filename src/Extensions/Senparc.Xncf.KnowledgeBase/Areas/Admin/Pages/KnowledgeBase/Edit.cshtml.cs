/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Edit.cshtml.cs
    File description: Edit.cshtml related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.Extensions;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.Services;
using Senparc.Xncf.KnowledgeBase.Domain.Services;

namespace Senparc.Xncf.KnowledgeBase.Areas.Admin.Pages.KnowledgeBase
{
    public class EditModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly KnowledgeBaseService _knowledgeBaseService;

        /// <summary>
        /// Initializes the KnowledgeBase editor with its persistence service.
        /// </summary>
        /// <param name="knowledgeBaseService">The KnowledgeBase service.</param>
        /// <param name="xncfModuleService">The lazy module service required by the base page.</param>
        public EditModel(KnowledgeBaseService knowledgeBaseService,Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            CurrentMenu = "KnowledgeBase";
            _knowledgeBaseService = knowledgeBaseService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }
        public KnowledgeBaseDto KnowledgeBaseDto { get; set; }

        /// <summary>
        /// Creates or updates the current KnowledgeBase definition from the posted DTO.
        /// </summary>
        /// <param name="knowledgeBaseDto">The submitted KnowledgeBase fields.</param>
        /// <returns>
        /// An HTTP 200 result containing a success flag and message. Null input and
        /// service failures are returned as a failed JSON-style result rather than
        /// propagated as an unhandled page exception.
        /// </returns>
        public async Task<IActionResult> OnPostSaveAsync([FromBody] KnowledgeBase_InsertDto knowledgeBaseDto)
        {
            try
            {
                if (knowledgeBaseDto == null)
                {
                    SenparcTrace.SendCustomLog("KnowledgeBase Save", "接收到的 DTO 为 null");
                    return Ok(new { success = false, msg = "数据为空" });
                }

                SenparcTrace.SendCustomLog("KnowledgeBase Save", $"接收数据：Id={knowledgeBaseDto.Id}, Name={knowledgeBaseDto.Name}");

                await _knowledgeBaseService.CreateOrUpdateAsync(knowledgeBaseDto);
                
                return Ok(new { success = true, data = true, msg = "保存成功" });
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("KnowledgeBase Save Error", ex.ToString());
                return Ok(new { success = false, msg = ex.Message });
            }
        }

        /// <summary>
        /// Deletes each existing KnowledgeBase identified in the request and returns
        /// identifiers that could not be matched.
        /// </summary>
        /// <param name="ids">The KnowledgeBase identifiers selected for deletion.</param>
        /// <returns>An HTTP 200 result containing unmatched identifiers.</returns>
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            var entity = await _knowledgeBaseService.GetFullListAsync(_ => ids.Contains(_.Id));
            foreach (var knowledgeBase in entity)
            {
                await _knowledgeBaseService.DeleteKnowledgeBaseAsync(knowledgeBase.Id);
            }
            IEnumerable<int> unDeleteIds = ids.Except(entity.Select(_ => _.Id));
            return Ok(unDeleteIds);
        }
    }
}
