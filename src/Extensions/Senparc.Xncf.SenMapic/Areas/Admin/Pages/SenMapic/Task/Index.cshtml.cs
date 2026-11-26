/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

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

        /// <summary>
        /// Initializes the task-list page model with the service used to query and
        /// mutate sitemap crawl tasks.
        /// </summary>
        /// <param name="taskService">The sitemap task service.</param>
        public IndexModel(SenMapicTaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>
        /// Handles the initial page request. The page data is loaded by the client
        /// through the JSON handlers on this model.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Returns all persisted sitemap tasks in ascending identifier order.
        /// </summary>
        /// <returns>A JSON result containing the task collection.</returns>
        [HttpGet]
        public async Task<IActionResult> OnGetListAsync()
        {
            var tasks = await _taskService.GetObjectListAsync(0, 0, z => true,z=>z.Id, Ncf.Core.Enums.OrderingType.Ascending);
            return new JsonResult(tasks);
        }

        /// <summary>
        /// Creates a sitemap crawl task and optionally starts it immediately when
        /// requested by <see cref="SenMapicTask_CreateUpdateDto.StartImmediately"/>.
        /// </summary>
        /// <param name="dto">The task name, crawl limits, root URL, and start preference.</param>
        /// <returns>A JSON success result after creation, or an exception from task persistence.</returns>
        /// <exception cref="NullReferenceException">
        /// May be raised when the request body is unavailable and model binding supplies
        /// a <see langword="null"/> DTO.
        /// </exception>
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

        /// <summary>
        /// Starts the sitemap crawl associated with the specified task identifier.
        /// </summary>
        /// <param name="id">The persisted sitemap task identifier.</param>
        /// <returns>A JSON success result when the task exists; otherwise an HTTP 404 result.</returns>
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

        /// <summary>
        /// Deletes the sitemap task identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The persisted sitemap task identifier.</param>
        /// <returns>A JSON success result when deletion succeeds; otherwise an HTTP 404 result.</returns>
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
