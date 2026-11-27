/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PreviewMonitor.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260802

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.41.7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using Senparc.Xncf.XncfBuilder.Domain.Services.Development;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Areas.XncfBuilder.Pages
{
    public sealed class PreviewMonitor : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IXncfPreviewService _previewService;
        private readonly IXncfDevelopmentJobService _developmentJobService;
        private readonly RequestTenantInfo _requestTenantInfo;

        /// <summary>
        /// Initializes the XNCF Builder preview/development monitor page.
        /// </summary>
        /// <param name="xncfModuleService">The lazy XNCF module service.</param>
        /// <param name="previewService">The preview session service.</param>
        /// <param name="developmentJobService">The isolated development job service.</param>
        /// <param name="requestTenantInfo">The current request tenant context.</param>
        public PreviewMonitor(
            Lazy<XncfModuleService> xncfModuleService,
            IXncfPreviewService previewService,
            IXncfDevelopmentJobService developmentJobService,
            RequestTenantInfo requestTenantInfo)
            : base(xncfModuleService)
        {
            _previewService = previewService;
            _developmentJobService = developmentJobService;
            _requestTenantInfo = requestTenantInfo;
        }

        /// <summary>
        /// Loads the current XNCF registration for the monitor page.
        /// </summary>
        public void OnGet()
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            _ = XncfRegister;
        }

        /// <summary>
        /// Returns preview stages, sessions, development jobs, and persistence status.
        /// </summary>
        /// <returns>An HTTP 200 JSON monitor state.</returns>
        public async Task<IActionResult> OnGetStateAsync()
        {
            object developmentJobs;
            object developmentStatus;
            var developmentPersistence = _developmentJobService.GetPersistenceStatus();
            if (!developmentPersistence.IsAvailable
                && developmentPersistence.RetryAfter.HasValue
                && developmentPersistence.RetryAfter.Value > DateTimeOffset.UtcNow)
            {
                // Do not turn the 2-second monitor refresh into repeated failed database calls
                // while an administrator is still applying the new XncfBuilder table migration.
                developmentJobs = Array.Empty<XncfDevelopmentJobInfo>();
                developmentStatus = developmentPersistence;
            }
            else try
            {
                developmentJobs = await _developmentJobService
                    .GetRecentAsync(100, HttpContext.RequestAborted, _requestTenantInfo)
                    .ConfigureAwait(false);
                developmentStatus = _developmentJobService.GetPersistenceStatus();
            }
            catch (Exception ex)
            {
                // The monitor must remain usable while the administrator has not yet applied the
                // new table migration. Preview sessions and the rest of Admin must not be blocked.
                developmentJobs = Array.Empty<XncfDevelopmentJobInfo>();
                developmentStatus = new
                {
                    IsAvailable = false,
                    StatusMessage = "隔离开发任务表尚未就绪；请执行 XncfBuilder 最新数据库 Migration。",
                    ErrorMessage = ex.Message
                };
            }

            return new JsonResult(new
            {
                ServerTime = DateTimeOffset.Now,
                StageDefinitions = XncfPreviewPresentation.GetStageDefinitions(),
                HostStatusDefinitions = XncfPreviewPresentation.GetHostStatusDefinitions(),
                PipelineStages = XncfPreviewPresentation.GetPipelineStageDefinitions(),
                PersistenceStatus = _previewService.GetPersistenceStatus(),
                Sessions = _previewService.GetSessions(),
                DevelopmentJobs = developmentJobs,
                DevelopmentStatus = developmentStatus
            });
        }

        /// <summary>
        /// Returns bounded recent output for one preview session.
        /// </summary>
        /// <param name="sessionId">The preview session identifier.</param>
        /// <returns>An HTTP 200 output result or HTTP 404.</returns>
        public IActionResult OnGetSessionOutput(string sessionId)
        {
            var session = _previewService.GetSession(sessionId, includeOutput: true);
            return session == null
                ? NotFound(new { Message = "预览会话不存在或历史记录已过期。" })
                : new JsonResult(new
                {
                    session.SessionId,
                    session.UpdatedAt,
                    session.RecentOutput
                });
        }

        /// <summary>
        /// Requests graceful stop of one preview session.
        /// </summary>
        /// <param name="sessionId">The preview session identifier.</param>
        /// <returns>An HTTP JSON result describing stop acceptance.</returns>
        public async Task<IActionResult> OnPostStopAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { Message = "必须提供预览会话 ID。" });
            }

            var stopped = await _previewService.StopAsync(sessionId, cancellationToken: HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return new JsonResult(new
            {
                Success = stopped,
                Message = stopped ? "停止请求已完成。" : "会话不存在、已停止或已经结束。"
            });
        }

        /// <summary>
        /// Applies an approved isolated development job after confirmation validation.
        /// </summary>
        /// <param name="jobId">The isolated development job identifier.</param>
        /// <param name="confirmationPhrase">The administrator confirmation phrase.</param>
        /// <returns>An HTTP JSON result or a mapped bad-request error.</returns>
        public async Task<IActionResult> OnPostApplyDevelopmentAsync(string jobId, string confirmationPhrase)
        {
            if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(confirmationPhrase))
            {
                return BadRequest(new { Message = "必须提供任务 ID 和确认短语。" });
            }

            try
            {
                var result = await _developmentJobService
                    .ApplyApprovedJobAsync(jobId, confirmationPhrase, HttpContext.RequestAborted, _requestTenantInfo)
                    .ConfigureAwait(false);
                return new JsonResult(new { Success = true, Message = result.StatusMessage });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Discards an isolated development job and releases its workspace/preview resources.
        /// </summary>
        /// <param name="jobId">The isolated development job identifier.</param>
        /// <returns>An HTTP JSON result or a mapped bad-request error.</returns>
        public async Task<IActionResult> OnPostDiscardDevelopmentAsync(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest(new { Message = "必须提供开发任务 ID。" });
            }

            try
            {
                var result = await _developmentJobService
                    .DiscardAsync(jobId, HttpContext.RequestAborted, _requestTenantInfo)
                    .ConfigureAwait(false);
                return new JsonResult(new { Success = true, Message = result.StatusMessage });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
