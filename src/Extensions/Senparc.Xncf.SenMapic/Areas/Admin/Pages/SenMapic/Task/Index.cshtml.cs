using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.SenMapic.Domain.Services;

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
            var tasks = await _taskService.GetObjectListAsync(z => true);
            return new JsonResult(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> OnPostCreateAsync([FromBody] SenMapicTaskDto dto)
        {
            var task = await _taskService.CreateTaskAsync(
                dto.Name, dto.StartUrl, dto.MaxThread,
                dto.MaxBuildMinutes, dto.MaxDeep, dto.MaxPageCount);
            
            if (dto.StartImmediately)
            {
                await _taskService.StartTaskAsync(task);
            }
            
            return new JsonResult(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> OnPostStartAsync(int id)
        {
            var task = await _taskService.GetObjectAsync(id);
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
            var task = await _taskService.GetObjectAsync(id);
            if (task != null)
            {
                await _taskService.DeleteObjectAsync(task);
                return new JsonResult(new { success = true });
            }
            return NotFound();
        }
    }

    public class SenMapicTaskDto
    {
        public bool StartImmediately { get; set; }
    }
} 