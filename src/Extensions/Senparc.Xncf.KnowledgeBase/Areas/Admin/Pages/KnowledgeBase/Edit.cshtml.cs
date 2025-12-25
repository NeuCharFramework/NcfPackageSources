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

namespace Senparc.Xncf.KnowledgeBase.Areas.Admin.Pages.KnowledgeBases
{
    public class EditModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly KnowledgeBasesService _knowledgeBasesService;
        public EditModel(KnowledgeBasesService knowledgeBasesService,Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            CurrentMenu = "KnowledgeBases";
            _knowledgeBasesService = knowledgeBasesService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }
        public KnowledgeBasesDto KnowledgeBasesDto { get; set; }

        /// <summary>
        /// Handler=Save
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostSaveAsync([FromBody] KnowledgeBasesDto knowledgeBasesDto)
        {
            if (knowledgeBasesDto == null)
            {
                return Ok(false);
            }
            await _knowledgeBasesService.CreateOrUpdateAsync(knowledgeBasesDto);
            return Ok(true);
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] string[] ids)
        {
            var entity = await _knowledgeBasesService.GetFullListAsync(_ => ids.Contains(_.Id));
            await _knowledgeBasesService.DeleteAllAsync(entity);
            IEnumerable<string> unDeleteIds = ids.Except(entity.Select(_ => _.Id));
            return Ok(unDeleteIds);
        }
    }
}
