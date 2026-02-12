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
        public EditModel(KnowledgeBaseService knowledgeBaseService,Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            CurrentMenu = "KnowledgeBase";
            _knowledgeBaseService = knowledgeBaseService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }
        public KnowledgeBaseDto KnowledgeBaseDto { get; set; }

        /// <summary>
        /// Handler=Save
        /// </summary>
        /// <returns></returns>
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

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int[] ids)
        {
            var entity = await _knowledgeBaseService.GetFullListAsync(_ => ids.Contains(_.Id));
            await _knowledgeBaseService.DeleteAllAsync(entity);
            IEnumerable<int> unDeleteIds = ids.Except(entity.Select(_ => _.Id));
            return Ok(unDeleteIds);
        }
    }
}
