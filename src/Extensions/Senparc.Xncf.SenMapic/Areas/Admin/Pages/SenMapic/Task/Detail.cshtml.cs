/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Detail.cshtml.cs
    File description: Detail.cshtml related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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

        /// <summary>
        /// Initializes the task-detail page model with the service used to read
        /// collected URL records.
        /// </summary>
        /// <param name="taskService">The sitemap task-item service.</param>
        public DetailModel(SenMapicTaskItemService taskService)
        {
            _taskItemService = taskService;
        }

        /// <summary>
        /// Handles the initial task-detail page request. Detail data is loaded
        /// through the JSON handlers on this model.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Returns the task-item record identified by the supplied identifier.
        /// </summary>
        /// <param name="id">The persisted task-item identifier.</param>
        /// <returns>An HTTP 200 JSON result when found; otherwise an HTTP 404 result.</returns>
        public async Task<IActionResult> OnGetTaskAsync(int id)
        {
            var task = await _taskItemService.GetObjectAsync(z => z.Id == id);
            if (task == null)
            {
                return NotFound();
            }
            return new JsonResult(task);
        }

        /// <summary>
        /// Returns the collected URL records for a task, optionally restricted by
        /// a domain substring. The <paramref name="page"/> and
        /// <paramref name="pageSize"/> values are accepted for endpoint compatibility
        /// but are not currently applied to the service query.
        /// </summary>
        /// <param name="id">The sitemap task identifier.</param>
        /// <param name="domain">An optional domain substring used to filter URLs.</param>
        /// <param name="page">The requested page number, retained for API compatibility.</param>
        /// <param name="pageSize">The requested page size, retained for API compatibility.</param>
        /// <returns>A JSON object containing the total matching count and URL items.</returns>
        public async Task<IActionResult> OnGetUrlListAsync(int id, string domain, int page = 1, int pageSize = 10)
        {
            var result = await _taskItemService.GetTaskItems(id, domain);

            return new JsonResult(new
            {
                total = result.TotalCount,
                items = result.ToList()
            });
        }

        /// <summary>
        /// Computes aggregate crawl statistics for all URL records belonging to a task,
        /// including total, successful, failed, and distinct host counts.
        /// </summary>
        /// <param name="id">The sitemap task identifier.</param>
        /// <returns>A JSON object containing the calculated crawl statistics.</returns>
        /// <exception cref="UriFormatException">
        /// May be raised when a stored URL cannot be parsed as a valid URI while
        /// calculating its host.
        /// </exception>
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
