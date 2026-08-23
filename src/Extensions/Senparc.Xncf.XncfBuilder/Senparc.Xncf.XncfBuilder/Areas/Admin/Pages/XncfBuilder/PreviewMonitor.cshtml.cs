/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PreviewMonitor.cshtml.cs
    文件功能描述：XNCF 独立预览流水线监控后台

    创建标识：Senparc - 20260802

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

    修改标识：Senparc - 20260822
    修改描述：v0.41.0 优化 XncfBuilder 预览任务与工作区服务

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
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

        public PreviewMonitor(
            Lazy<XncfModuleService> xncfModuleService,
            IXncfPreviewService previewService,
            IXncfDevelopmentJobService developmentJobService)
            : base(xncfModuleService)
        {
            _previewService = previewService;
            _developmentJobService = developmentJobService;
        }

        public void OnGet()
        {
            // 仅初次打开页面时触发基类的 XNCF 可用性校验。高频状态接口
            // 不重复触发该校验，避免轮询放大缓存/数据库访问。
            _ = XncfRegister;
        }

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
                    .GetRecentAsync(100, HttpContext.RequestAborted)
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

        public async Task<IActionResult> OnPostApplyDevelopmentAsync(string jobId, string confirmationPhrase)
        {
            if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(confirmationPhrase))
            {
                return BadRequest(new { Message = "必须提供任务 ID 和确认短语。" });
            }

            try
            {
                var result = await _developmentJobService
                    .ApplyApprovedJobAsync(jobId, confirmationPhrase, HttpContext.RequestAborted)
                    .ConfigureAwait(false);
                return new JsonResult(new { Success = true, Message = result.StatusMessage });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostDiscardDevelopmentAsync(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest(new { Message = "必须提供开发任务 ID。" });
            }

            try
            {
                var result = await _developmentJobService
                    .DiscardAsync(jobId, HttpContext.RequestAborted)
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
