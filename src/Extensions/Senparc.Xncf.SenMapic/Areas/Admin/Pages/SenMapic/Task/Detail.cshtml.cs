using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Senparc.Xncf.SenMapic.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Areas.Admin.Pages.SenMapic.Task
{
    public class DetailModel : PageModel
    {
        private readonly SenMapicTaskItemService _taskItemService;

        public DetailModel(SenMapicTaskItemService taskService)
        {
            _taskItemService = taskService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetTaskAsync(int id)
        {
            var task = await _taskItemService.GetObjectAsync(z => z.Id == id);
            if (task == null)
            {
                return NotFound();
            }
            return new JsonResult(task);
        }

        public async Task<IActionResult> OnGetUrlListAsync(int id, string domain, int page = 1, int pageSize = 10)
        {
            var result = await _taskItemService.GetTaskItems(id, domain);

            return new JsonResult(new
            {
                total = result.TotalCount,
                items = result.ToList()
            });
        }

        public async Task<IActionResult> OnGetStatsAsync(int id)
        {
            var items = await _taskItemService.GetTaskItems(id, null);
            var stats = new
            {
                totalUrls = items.Count,
                successUrls = items.Count(x => x.StatusCode >= 200 && x.StatusCode < 400),
                failedUrls = items.Count(x => x.StatusCode >= 400 || !string.IsNullOrEmpty(x.ErrorMessage)),
                domains = items.Select(x => new Uri(x.Url).Host).Distinct().ToList()
            };
            return new JsonResult(stats);
        }
    }
}