/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PreviewMonitor.cshtml.cs
    文件功能描述：XNCF 独立预览流水线监控后台

    创建标识：Senparc - 20260802

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Areas.XncfBuilder.Pages
{
    public sealed class PreviewMonitor : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly IXncfPreviewService _previewService;

        public PreviewMonitor(
            Lazy<XncfModuleService> xncfModuleService,
            IXncfPreviewService previewService)
            : base(xncfModuleService)
        {
            _previewService = previewService;
        }

        public void OnGet()
        {
            // 仅初次打开页面时触发基类的 XNCF 可用性校验。高频状态接口
            // 不重复触发该校验，避免轮询放大缓存/数据库访问。
            _ = XncfRegister;
        }

        public IActionResult OnGetState()
        {
            return new JsonResult(new
            {
                ServerTime = DateTimeOffset.Now,
                StageDefinitions = XncfPreviewPresentation.GetStageDefinitions(),
                HostStatusDefinitions = XncfPreviewPresentation.GetHostStatusDefinitions(),
                PipelineStages = XncfPreviewPresentation.GetPipelineStageDefinitions(),
                PersistenceStatus = _previewService.GetPersistenceStatus(),
                Sessions = _previewService.GetSessions()
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
    }
}
