/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：BuildXncfAppService.Preview.cs
    文件功能描述：XNCF 独立预览管理接口


    创建标识：Senparc - 20260801

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

    修改标识：Senparc - 20260815
    修改描述：v0.41.0 增强隔离开发任务与 Sandbox 预览流程

    修改标识：Senparc - 20260822
    修改描述：v0.41.0 优化 XncfBuilder 预览任务与工作区服务

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    public partial class BuildXncfAppService
    {
        // This legacy preview starts a child process from the supplied host source tree. It is
        // intentionally excluded from AI; development jobs preview only their isolated snapshot.
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Preview.Name", "Function.XncfBuilder.Preview.Description", typeof(Register), AllowAiInvocation = false)]
        public async Task<StringAppResponse> Preview(BuildXncf_PreviewRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Data = XncfBuilderResource.Get("XncfBuilder.Preview.RequestMissing");
                    return null;
                }

                try
                {
                    var previewService = ServiceProvider.GetRequiredService<IXncfPreviewService>();
                    var session = await previewService.StartAsync(
                        new XncfPreviewStartOptions
                        {
                            SolutionFilePath = request.SlnFilePath,
                            ModuleProjectName = request.ModuleProjectName,
                            Port = request.Port,
                            StartupTimeoutSeconds = request.StartupTimeoutSeconds,
                            EnvironmentName = request.EnvironmentName
                        },
                        message => logger.Append(message),
                        this.CancellationToken).ConfigureAwait(false);

                    response.Success = true;
                    response.Data = BuildPreviewStartedHtml(
                        session,
                        previewService.GetPersistenceStatus());
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.StateCode = 101;
                    response.ErrorMessage = ex.Message;
                    response.Data = WebUtility.HtmlEncode(ex.Message);
                    logger.Append($"Preview Exception: {ex.Message}");
                }

                return null;
            }).ConfigureAwait(false);
        }

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.PreviewStatus.Name", "Function.XncfBuilder.PreviewStatus.Description", typeof(Register))]
        public async Task<StringAppResponse> PreviewStatus(BuildXncf_PreviewStatusRequest request)
        {
            return await this.GetStringResponseAsync((response, logger) =>
            {
                var previewService = ServiceProvider.GetRequiredService<IXncfPreviewService>();
                var sessions = previewService.GetSessions(request?.IncludeOutput == true);
                var persistenceStatus = previewService.GetPersistenceStatus();
                response.Success = true;
                var html = new StringBuilder();
                AppendPersistenceWarning(html, persistenceStatus);

                if (sessions.Count == 0)
                {
                    html.Append(WebUtility.HtmlEncode(
                        XncfBuilderResource.Get("XncfBuilder.Preview.NoSessions")));
                    response.Data = html.ToString();
                    return Task.FromResult<string>(null);
                }

                foreach (var session in sessions)
                {
                    html.Append("<p><strong>")
                        .Append(WebUtility.HtmlEncode(session.ModuleProjectName))
                        .Append("</strong><br />")
                        .Append(WebUtility.HtmlEncode(session.SessionId))
                        .Append("<br />")
                        .Append(WebUtility.HtmlEncode(XncfPreviewPresentation.GetStageLabel(session.Stage)))
                        .Append(" · ")
                        .Append(session.ProgressPercent)
                        .Append("%<br />")
                        .Append(WebUtility.HtmlEncode(session.StatusMessage))
                        .Append("<br />Host: ")
                        .Append(WebUtility.HtmlEncode(XncfPreviewPresentation.GetHostStatusLabel(session.HostStatus)));

                    if (!string.IsNullOrWhiteSpace(session.ErrorMessage))
                    {
                        html.Append("<br /><span style=\"color:#c00\">")
                            .Append(WebUtility.HtmlEncode(session.ErrorMessage))
                            .Append("</span>");
                    }

                    if (!string.IsNullOrWhiteSpace(session.Url))
                    {
                        html.Append("<br /><a href=\"")
                            .Append(WebUtility.HtmlEncode(session.Url))
                            .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">")
                            .Append(WebUtility.HtmlEncode(session.Url))
                            .Append("</a>");
                    }

                    if (session.ProcessId > 0)
                    {
                        html.Append("<br />PID ").Append(session.ProcessId);
                    }

                    if (!string.IsNullOrWhiteSpace(session.SolutionFilePath))
                    {
                        html.Append("<br />Source solution: ")
                            .Append(WebUtility.HtmlEncode(session.SolutionFilePath));
                    }

                    if (!string.IsNullOrWhiteSpace(session.PublishDirectory))
                    {
                        html.Append("<br />Published preview directory: ")
                            .Append(WebUtility.HtmlEncode(session.PublishDirectory));
                    }

                    if (session.ProcessStartedAt.HasValue)
                    {
                        html.Append("<br />Host started: ")
                            .Append(WebUtility.HtmlEncode(session.ProcessStartedAt.Value.ToString("O")));
                    }

                    if (session.HealthyAt.HasValue)
                    {
                        html.Append("<br />Host healthy: ")
                            .Append(WebUtility.HtmlEncode(session.HealthyAt.Value.ToString("O")));
                    }

                    if (session.ExitCode.HasValue)
                    {
                        html.Append("<br />ExitCode: ").Append(session.ExitCode.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(session.SourceFingerprint))
                    {
                        html.Append("<br />Source SHA-256: ")
                            .Append(WebUtility.HtmlEncode(session.SourceFingerprint));
                    }

                    if (!string.IsNullOrWhiteSpace(session.ModuleAssemblySha256))
                    {
                        html.Append("<br />Module DLL SHA-256: ")
                            .Append(WebUtility.HtmlEncode(session.ModuleAssemblySha256));
                    }

                    html.Append("</p>");

                    if (!string.IsNullOrWhiteSpace(session.RecentOutput))
                    {
                        html.Append("<pre>")
                            .Append(WebUtility.HtmlEncode(session.RecentOutput))
                            .Append("</pre>");
                    }
                }

                response.Data = html.ToString();
                return Task.FromResult<string>(null);
            }).ConfigureAwait(false);
        }

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.StopPreview.Name", "Function.XncfBuilder.StopPreview.Description", typeof(Register), AllowAiInvocation = false)]
        public async Task<StringAppResponse> StopPreview(BuildXncf_StopPreviewRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(request?.SessionId))
                {
                    response.Success = false;
                    response.Data = XncfBuilderResource.Get("XncfBuilder.Preview.SessionRequired");
                    return null;
                }

                var previewService = ServiceProvider.GetRequiredService<IXncfPreviewService>();
                var stopped = await previewService.StopAsync(
                    request.SessionId,
                    message => logger.Append(message),
                    this.CancellationToken).ConfigureAwait(false);
                response.Success = stopped;
                response.Data = stopped
                    ? XncfBuilderResource.Get("XncfBuilder.Preview.StopSucceeded")
                    : XncfBuilderResource.Get("XncfBuilder.Preview.SessionNotFound");
                return null;
            }).ConfigureAwait(false);
        }

        private static string BuildPreviewStartedHtml(
            XncfPreviewSessionInfo session,
            XncfPreviewPersistenceInfo persistenceStatus)
        {
            var url = WebUtility.HtmlEncode(session.Url);
            var html = new StringBuilder();
            AppendPersistenceWarning(html, persistenceStatus);
            html.Append(XncfBuilderResource.Get("XncfBuilder.Preview.StartSucceeded"))
                .Append("<br />")
                .Append($"Session: {WebUtility.HtmlEncode(session.SessionId)}<br />")
                .Append($"{WebUtility.HtmlEncode(XncfPreviewPresentation.GetStageLabel(session.Stage))} · {session.ProgressPercent}%<br />")
                .Append($"Host: {WebUtility.HtmlEncode(XncfPreviewPresentation.GetHostStatusLabel(session.HostStatus))}<br />")
                .Append($"PID: {session.ProcessId}<br />")
                .Append($"Source SHA-256: {WebUtility.HtmlEncode(session.SourceFingerprint)}<br />")
                .Append($"Module DLL SHA-256: {WebUtility.HtmlEncode(session.ModuleAssemblySha256)}<br />")
                .Append($"<a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">{url}</a>");
            return html.ToString();
        }

        private static void AppendPersistenceWarning(
            StringBuilder html,
            XncfPreviewPersistenceInfo persistenceStatus)
        {
            if (persistenceStatus?.IsAvailable != false)
            {
                return;
            }

            html.Append("<p style=\"color:#b26a00\"><strong>持久化未就绪：</strong>")
                .Append(WebUtility.HtmlEncode(persistenceStatus.StatusMessage))
                .Append("</p>");
        }
    }
}
