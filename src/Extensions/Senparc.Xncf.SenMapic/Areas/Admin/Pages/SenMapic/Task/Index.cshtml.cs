/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml 相关实现
    
    
    创建标识：Senparc - 20250114
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.SenMapic.Domain.Services;
using Senparc.Xncf.SenMapic.Models.DatabaseModel.Dto;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Areas.Admin.Pages.SenMapic.Task
{
    public class IndexModel : PageModel
    {
        private readonly SenMapicTaskService _taskService;

        public IndexModel(SenMapicTaskService taskService)
        {
            _taskService = taskService;
        }

        public void OnGet()
        {
        }

        [HttpGet]
        public async Task<IActionResult> OnGetListAsync()
        {
            var tasks = await _taskService.GetObjectListAsync(0, 0, z => true,z=>z.Id, Ncf.Core.Enums.OrderingType.Ascending);
            return new JsonResult(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> OnPostCreateAsync([FromBody] SenMapicTask_CreateUpdateDto dto)
        {
            var task = await _taskService.CreateTaskAsync(
                dto.Name, dto.StartUrl, dto.MaxThread,
                dto.MaxBuildMinutes, dto.MaxDeep, dto.MaxPageCount, true);

            if (dto.StartImmediately)
            {
                await _taskService.StartTaskAsync(task);
            }

            return new JsonResult(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> OnPostStartAsync(int id)
        {
            var task = await _taskService.GetObjectAsync(z=>z.Id  == id);
            if (task != null)
            {
                await _taskService.StartTaskAsync(task);
                return new JsonResult(new { success = true });
            }
            return NotFound();
        }

        [HttpDelete]
        public async Task<IActionResult> OnDeleteAsync(int id)
        {
            var task = await _taskService.GetObjectAsync(z=>z.Id ==id);
            if (task != null)
            {
                await _taskService.DeleteObjectAsync(task);
                return new JsonResult(new { success = true });
            }
            return NotFound();
        }
    }

    
}