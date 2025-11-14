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

namespace Senparc.Xncf.KnowledgeBase.Areas.Admin.Pages.KnowledgeBasesDetail
{
    public class EditModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly KnowledgeBasesDetailService _knowledgeBasesDetailService;
        public EditModel(KnowledgeBasesDetailService knowledgeBasesDetailService,Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            CurrentMenu = "KnowledgeBasesDetail";
            _knowledgeBasesDetailService = knowledgeBasesDetailService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }
        public KnowledgeBasesDetailDto KnowledgeBasesDetailDto { get; set; }

        /// <summary>
        /// Handler=Save
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostSaveAsync([FromBody] KnowledgeBasesDetailDto knowledgeBasesDetailDto)
        {
            if (knowledgeBasesDetailDto == null)
            {
                return Ok(false);
            }
            await _knowledgeBasesDetailService.CreateOrUpdateAsync(knowledgeBasesDetailDto);
            return Ok(true);
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] string[] ids)
        {
            var entity = await _knowledgeBasesDetailService.GetFullListAsync(_ => ids.Contains(_.Id));
            await _knowledgeBasesDetailService.DeleteAllAsync(entity);
            IEnumerable<string> unDeleteIds = ids.Except(entity.Select(_ => _.Id));
            return Ok(unDeleteIds);
        }
    }
}
