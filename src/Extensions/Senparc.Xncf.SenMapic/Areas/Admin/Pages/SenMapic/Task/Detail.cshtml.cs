using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Areas.Admin.Pages.SenMapic.Task
{
    public class DetailModel : PageModel
    {
        private readonly SenMapicTaskService _taskService;

        public DetailModel(SenMapicTaskService taskService)
        {
            _taskService = taskService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetTaskAsync(int id)
        {
            var task = await _taskService.GetObjectAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return new JsonResult(task);
        }

        public async Task<IActionResult> OnGetUrlListAsync(int id, string domain, int page = 1, int pageSize = 10)
        {
            var query = _taskService.GetTaskItems(id)
                .Where(x => x.Url.Contains(domain));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CrawlTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new JsonResult(new { 
                total,
                items
            });
        }

        public async Task<IActionResult> OnGetStatsAsync(int id)
        {
            var items = await _taskService.GetTaskItems(id).ToListAsync();
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